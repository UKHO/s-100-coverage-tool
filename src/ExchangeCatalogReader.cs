using System.Globalization;
using System.Xml.Linq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;

namespace UKHO.S100CoverageTool;

public sealed class ExchangeCatalogReader
{
    private static readonly XNamespace ExchangeCatalogNamespace = "http://www.iho.int/s100/xc/5.2";
    private static readonly XNamespace GcoNamespace = "http://standards.iso.org/iso/19115/-3/gco/1.0";
    private static readonly XNamespace GeographicExtensionsNamespace = "http://standards.iso.org/iso/19115/-3/gex/1.0";
    private static readonly XNamespace GmlNamespace = "http://www.opengis.net/gml/3.2";
    private static readonly GeometryFactory GeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();

    public IReadOnlyList<CatalogDataset> Read(string filePath)
    {
        var document = XDocument.Load(filePath);

        return document
            .Descendants(ExchangeCatalogNamespace + "S100_DatasetDiscoveryMetadata")
            .Select(ReadDataset)
            .ToArray();
    }

    private static CatalogDataset ReadDataset(XElement element)
    {
        var productIdentifier = element
            .Element(ExchangeCatalogNamespace + "productSpecification")?
            .Element(ExchangeCatalogNamespace + "productIdentifier")?
            .Value ?? "Unknown";
        var fileName = element.Element(ExchangeCatalogNamespace + "fileName")?.Value ?? string.Empty;
        var bounds = ReadBounds(element.Element(ExchangeCatalogNamespace + "boundingBox"));
        var coverageGeometry = ReadCoverageGeometry(element, bounds);

        return new CatalogDataset(productIdentifier, fileName, bounds, coverageGeometry);
    }

    private static Geometry? ReadCoverageGeometry(XElement element, GeographicBounds? bounds)
    {
        var polygons = element
            .Descendants(GmlNamespace + "Polygon")
            .Select(ReadPolygon)
            .ToArray();

        if (polygons.Length > 0)
        {
            return polygons.Length == 1 ? polygons[0] : UnaryUnionOp.Union(polygons);
        }

        return bounds is null ? null : CreateBoundsPolygon(bounds);
    }

    private static Polygon ReadPolygon(XElement polygonElement)
    {
        var exterior = polygonElement.Element(GmlNamespace + "exterior")
            ?? throw new FormatException("A coverage polygon is missing its exterior ring.");
        var shell = ReadLinearRing(exterior);
        var holes = polygonElement
            .Elements(GmlNamespace + "interior")
            .Select(ReadLinearRing)
            .ToArray();

        return GeometryFactory.CreatePolygon(shell, holes);
    }

    private static LinearRing ReadLinearRing(XElement ringElement)
    {
        var positionList = ringElement.Element(GmlNamespace + "LinearRing")?
            .Element(GmlNamespace + "posList")?.Value;

        if (string.IsNullOrWhiteSpace(positionList))
        {
            throw new FormatException("A coverage polygon ring is missing its position list.");
        }

        var values = positionList.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (values.Length < 6 || values.Length % 2 != 0)
        {
            throw new FormatException("A coverage polygon ring must contain at least three coordinate pairs.");
        }

        var coordinates = new List<Coordinate>(values.Length / 2 + 1);
        for (var index = 0; index < values.Length; index += 2)
        {
            var latitude = double.Parse(values[index], CultureInfo.InvariantCulture);
            var longitude = double.Parse(values[index + 1], CultureInfo.InvariantCulture);
            coordinates.Add(new Coordinate(longitude, latitude));
        }

        if (!coordinates[0].Equals2D(coordinates[^1]))
        {
            coordinates.Add(coordinates[0].Copy());
        }

        return GeometryFactory.CreateLinearRing(coordinates.ToArray());
    }

    private static Polygon CreateBoundsPolygon(GeographicBounds bounds)
    {
        var coordinates = new[]
        {
            new Coordinate((double)bounds.WestLongitude, (double)bounds.SouthLatitude),
            new Coordinate((double)bounds.EastLongitude, (double)bounds.SouthLatitude),
            new Coordinate((double)bounds.EastLongitude, (double)bounds.NorthLatitude),
            new Coordinate((double)bounds.WestLongitude, (double)bounds.NorthLatitude),
            new Coordinate((double)bounds.WestLongitude, (double)bounds.SouthLatitude)
        };

        return GeometryFactory.CreatePolygon(coordinates);
    }

    private static GeographicBounds? ReadBounds(XElement? boundingBox)
    {
        if (boundingBox is null)
        {
            return null;
        }

        var west = ReadCoordinate(boundingBox, "westBoundLongitude");
        var east = ReadCoordinate(boundingBox, "eastBoundLongitude");
        var south = ReadCoordinate(boundingBox, "southBoundLatitude");
        var north = ReadCoordinate(boundingBox, "northBoundLatitude");

        return west.HasValue && east.HasValue && south.HasValue && north.HasValue
            ? new GeographicBounds(west.Value, east.Value, south.Value, north.Value)
            : null;
    }

    private static decimal? ReadCoordinate(XElement boundingBox, string elementName)
    {
        var value = boundingBox
            .Element(GeographicExtensionsNamespace + elementName)?
            .Element(GcoNamespace + "Decimal")?
            .Value;

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var coordinate)
            ? coordinate
            : null;
    }
}