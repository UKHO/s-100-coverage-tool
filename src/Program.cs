using UKHO.S100CoverageTool;

var catalogPath = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine("data", "CATALOG.XML"));
var exclusionListPath = Path.GetFullPath(args.Length > 1 ? args[1] : Path.Combine("data", "excluded-products.txt"));

if (!File.Exists(catalogPath))
{
    Console.Error.WriteLine($"Catalog file not found: {catalogPath}");
    Console.Error.WriteLine("Usage: S100Coverage [path-to-catalog.xml] [path-to-exclusions.txt]");
    return 1;
}

var datasets = new ExchangeCatalogReader().Read(catalogPath);
Console.WriteLine($"Loaded {datasets.Count} datasets from {catalogPath}");

var excludedFileNames = ProductExclusionList.Load(exclusionListPath);
var originalDatasetCount = datasets.Count;
datasets = datasets
    .Where(dataset => !excludedFileNames.Contains(
        ProductExclusionList.GetFileNameWithoutExtension(dataset.FileName)))
    .ToArray();
Console.WriteLine($"Excluded {originalDatasetCount - datasets.Count} dataset(s) by filename using {exclusionListPath}");

foreach (var productGroup in datasets.GroupBy(dataset => dataset.ProductIdentifier).OrderBy(group => group.Key))
{
    var datasetsWithBounds = productGroup.Count(dataset => dataset.Bounds is not null);
    Console.WriteLine($"{productGroup.Key}: {productGroup.Count()} dataset(s), {datasetsWithBounds} with geographic bounds");
}

Console.WriteLine("Searching for the first matching collection...");
var suggestion = new CollectionSuggester().Suggest(datasets, (checkedPairs, totalPairs) =>
{
    Console.Write($"\rChecked {checkedPairs:N0} of {totalPairs:N0} S-104/S-111 pairs.");
});
Console.WriteLine();

if (suggestion is null)
{
    Console.WriteLine("No matching collection found.");
    return 0;
}

Console.WriteLine("Found a matching collection:");
Console.WriteLine($"S-104: {suggestion.S104Dataset.FileName}");
Console.WriteLine($"S-111: {suggestion.S111Dataset.FileName}");
foreach (var s101Dataset in suggestion.S101Datasets)
{
    Console.WriteLine($"S-101: {s101Dataset.FileName}");
}

foreach (var s102Dataset in suggestion.S102Datasets)
{
    Console.WriteLine($"S-102: {s102Dataset.FileName}");
}

return 0;