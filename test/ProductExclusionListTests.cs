using NUnit.Framework;
using Shouldly;
using UKHO.S100CoverageTool;

namespace UKHO.S100CoverageTool.Tests;

[TestFixture]
public sealed class ProductExclusionListTests
{
    // Product datasets use different extensions, so exclusions must identify the shared filename stem.
    [TestCase(
        "file:/S-101/DATASET_FILES/CA00/chart/3/chart.000",
        "chart",
        TestName = "GetFileNameWithoutExtension_WithS101FileUri_ReturnsBasenameWithoutExtension")]
    [TestCase(
        "file:/S-102/DATASET_FILES/CA00/surface/28/surface.h5",
        "surface",
        TestName = "GetFileNameWithoutExtension_WithS102FileUri_ReturnsBasenameWithoutExtension")]
    [TestCase(
        "catalog/datasets/current-chart",
        "current-chart",
        TestName = "GetFileNameWithoutExtension_WithoutExtension_ReturnsBasename")]
    public void GetFileNameWithoutExtension_WithCatalogPath_ReturnsBasenameWithoutExtension(
        string catalogFileName,
        string expectedFileName)
    {
        // Act
        var result = ProductExclusionList.GetFileNameWithoutExtension(catalogFileName);

        // Assert
        result.ShouldBe(expectedFileName);
    }
}