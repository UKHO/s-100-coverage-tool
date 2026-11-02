using NetTopologySuite;
using NetTopologySuite.Geometries;
using NUnit.Framework;
using Shouldly;
using UKHO.S100CoverageTool;

namespace UKHO.S100CoverageTool.Tests;

[TestFixture]
public sealed class CollectionSuggesterTests
{
    private static readonly GeometryFactory GeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();

    [Test]
    public void Suggest_WhenMultipleCollectionsMatch_ReturnsFirstS104Dataset()
    {
        // Returning the first qualifying S-104 dataset keeps the search bounded and predictable.
        // Arrange
        var (datasets, _) = CreateMultipleMatchScenario();

        // Act
        var result = new CollectionSuggester().Suggest(datasets);

        // Assert
        var actualS104FileName = result?.S104Dataset.FileName;
        actualS104FileName.ShouldBe("first-s104");
    }

    [Test]
    public void Suggest_WhenS101CoverageRequiresMultipleDatasets_ReturnsCombinedS101Coverage()
    {
        // Adjacent S-101 datasets must collectively contain both overlapping product areas.
        // Arrange
        var (datasets, _) = CreateMultipleMatchScenario();

        // Act
        var result = new CollectionSuggester().Suggest(datasets);

        // Assert
        var actualS101FileNames = result?.S101Datasets.Select(dataset => dataset.FileName).ToArray() ?? [];
        actualS101FileNames.ShouldBe(["s101-left", "s101-right"]);
    }

    [Test]
    public void Suggest_WhenS102CoverageRequiresMultipleDatasets_ReturnsCombinedS102Coverage()
    {
        // Adjacent S-102 datasets must collectively contain both overlapping product areas.
        // Arrange
        var (datasets, _) = CreateMultipleMatchScenario();

        // Act
        var result = new CollectionSuggester().Suggest(datasets);

        // Assert
        var actualS102FileNames = result?.S102Datasets.Select(dataset => dataset.FileName).ToArray() ?? [];
        actualS102FileNames.ShouldBe(["s102-left", "s102-right"]);
    }

    [Test]
    public void Suggest_WhenFirstCollectionMatches_StopsAfterOnePair()
    {
        // Stopping at the first qualifying pair avoids unnecessary work across the remaining catalog.
        // Arrange
        var (datasets, checkedPairs) = CreateMultipleMatchScenario();

        // Act
        var result = new CollectionSuggester().Suggest(
            datasets,
            (checkedCount, _) => checkedPairs.Add(checkedCount));

        // Assert
        checkedPairs[^1].ShouldBe(1);
    }

    [Test]
    public void Suggest_WhenS101CoverageDoesNotContainTheCombinedArea_ReturnsNull()
    {
        // A narrow gap in S-101 coverage must reject the pair even when the polygons overlap most of the area.
        // Arrange
        var (s104, s111) = CreateOverlappingPair();
        var partialS101 = CreateDataset("S-101", "partial-s101", CreateRectangle(0, 0, 2.9, 2));
        var completeS102 = CreateDataset("S-102", "complete-s102", CreateRectangle(0, 0, 3, 2));

        // Act
        var result = new CollectionSuggester().Suggest([s104, s111, partialS101, completeS102]);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void Suggest_WhenS102CoverageDoesNotContainTheCombinedArea_ReturnsNull()
    {
        // A narrow gap in S-102 coverage must reject the pair even when the polygons overlap most of the area.
        // Arrange
        var (s104, s111) = CreateOverlappingPair();
        var completeS101 = CreateDataset("S-101", "complete-s101", CreateRectangle(0, 0, 3, 2));
        var partialS102 = CreateDataset("S-102", "partial-s102", CreateRectangle(0, 0, 2.9, 2));

        // Act
        var result = new CollectionSuggester().Suggest([s104, s111, completeS101, partialS102]);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void Suggest_WhenS104AndS111DoNotOverlap_ReturnsNull()
    {
        // Complete supporting coverage cannot make a pair valid when the required products never overlap.
        // Arrange
        var s104 = CreateDataset("S-104", "s104", CreateRectangle(0, 0, 1, 1));
        var s111 = CreateDataset("S-111", "s111", CreateRectangle(2, 0, 3, 1));
        var completeS101 = CreateDataset("S-101", "complete-s101", CreateRectangle(0, 0, 3, 1));
        var completeS102 = CreateDataset("S-102", "complete-s102", CreateRectangle(0, 0, 3, 1));

        // Act
        var result = new CollectionSuggester().Suggest([s104, s111, completeS101, completeS102]);

        // Assert
        result.ShouldBeNull();
    }

    private static (CatalogDataset[] Datasets, List<long> CheckedPairs) CreateMultipleMatchScenario()
    {
        var s104 = CreateDataset("S-104", "first-s104", CreateRectangle(0, 0, 2, 2));
        var laterS104 = CreateDataset("S-104", "later-s104", CreateRectangle(0, 0, 2, 2));
        var s111 = CreateDataset("S-111", "s111", CreateRectangle(1, 0, 3, 2));
        var s101Left = CreateDataset("S-101", "s101-left", CreateRectangle(0, 0, 1.5, 2));
        var s101Right = CreateDataset("S-101", "s101-right", CreateRectangle(1.5, 0, 3, 2));
        var s102Left = CreateDataset("S-102", "s102-left", CreateRectangle(0, 0, 1.5, 2));
        var s102Right = CreateDataset("S-102", "s102-right", CreateRectangle(1.5, 0, 3, 2));

        return (
            [s104, laterS104, s111, s101Left, s101Right, s102Left, s102Right],
            []);
    }

    private static (CatalogDataset S104, CatalogDataset S111) CreateOverlappingPair()
    {
        return (
            CreateDataset("S-104", "s104", CreateRectangle(0, 0, 2, 2)),
            CreateDataset("S-111", "s111", CreateRectangle(1, 0, 3, 2)));
    }

    private static CatalogDataset CreateDataset(string productIdentifier, string fileName, Geometry coverage)
    {
        return new CatalogDataset(productIdentifier, fileName, null, coverage);
    }

    private static Polygon CreateRectangle(double minX, double minY, double maxX, double maxY)
    {
        var coordinates = new[]
        {
            new Coordinate(minX, minY),
            new Coordinate(maxX, minY),
            new Coordinate(maxX, maxY),
            new Coordinate(minX, maxY),
            new Coordinate(minX, minY)
        };

        return GeometryFactory.CreatePolygon(coordinates);
    }
}