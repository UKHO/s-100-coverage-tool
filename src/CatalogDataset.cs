using NetTopologySuite.Geometries;

namespace UKHO.S100CoverageTool;

public sealed record CatalogDataset(
    string ProductIdentifier,
    string FileName,
    GeographicBounds? Bounds,
    Geometry? CoverageGeometry);

public sealed record GeographicBounds(
    decimal WestLongitude,
    decimal EastLongitude,
    decimal SouthLatitude,
    decimal NorthLatitude);

public sealed record SuggestedCollection(
    CatalogDataset S104Dataset,
    CatalogDataset S111Dataset,
    IReadOnlyList<CatalogDataset> S101Datasets,
    IReadOnlyList<CatalogDataset> S102Datasets);