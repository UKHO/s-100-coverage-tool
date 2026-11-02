using NetTopologySuite.Geometries;

namespace UKHO.S100CoverageTool;

public sealed class CollectionSuggester
{
    public SuggestedCollection? Suggest(
        IEnumerable<CatalogDataset> datasets,
        Action<long, long>? reportProgress = null)
    {
        var datasetsWithCoverage = datasets
            .Where(dataset => dataset.CoverageGeometry is not null)
            .ToArray();
        var s101Datasets = ForProduct(datasetsWithCoverage, "S-101");
        var s102Datasets = ForProduct(datasetsWithCoverage, "S-102");
        var s104Datasets = ForProduct(datasetsWithCoverage, "S-104");
        var s111Datasets = ForProduct(datasetsWithCoverage, "S-111");
        var totalPairs = (long)s104Datasets.Length * s111Datasets.Length;
        var checkedPairs = 0L;
        var progressInterval = Math.Max(1, totalPairs / 10);
        reportProgress?.Invoke(checkedPairs, totalPairs);

        foreach (var s104Dataset in s104Datasets)
        {
            var s104Coverage = s104Dataset.CoverageGeometry!;

            foreach (var s111Dataset in s111Datasets)
            {
                checkedPairs++;
                if (checkedPairs % progressInterval == 0)
                {
                    reportProgress?.Invoke(checkedPairs, totalPairs);
                }

                var s111Coverage = s111Dataset.CoverageGeometry!;
                if (!s104Coverage.EnvelopeInternal.Intersects(s111Coverage.EnvelopeInternal)
                    || !s104Coverage.Intersects(s111Coverage))
                {
                    continue;
                }

                var combinedCoverage = s104Coverage.Union(s111Coverage);
                var s102Candidates = s102Datasets
                    .Where(dataset => dataset.CoverageGeometry!.EnvelopeInternal.Intersects(combinedCoverage.EnvelopeInternal))
                    .ToArray();
                var selectedS102Datasets = SelectCoveringDatasets(combinedCoverage, s102Candidates);
                if (selectedS102Datasets is null)
                {
                    continue;
                }

                var s101Candidates = s101Datasets
                    .Where(dataset => dataset.CoverageGeometry!.EnvelopeInternal.Intersects(combinedCoverage.EnvelopeInternal))
                    .ToArray();
                var selectedS101Datasets = SelectCoveringDatasets(combinedCoverage, s101Candidates);
                if (selectedS101Datasets is not null)
                {
                    reportProgress?.Invoke(checkedPairs, totalPairs);
                    return new SuggestedCollection(
                        s104Dataset,
                        s111Dataset,
                        selectedS101Datasets,
                        selectedS102Datasets);
                }
            }
        }

        reportProgress?.Invoke(checkedPairs, totalPairs);
        return null;
    }

    private static CatalogDataset[] ForProduct(IEnumerable<CatalogDataset> datasets, string productIdentifier)
    {
        return datasets
            .Where(dataset => string.Equals(dataset.ProductIdentifier, productIdentifier, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static IReadOnlyList<CatalogDataset>? SelectCoveringDatasets(
        Geometry requiredCoverage,
        IReadOnlyList<CatalogDataset> candidates)
    {
        var remainingCoverage = requiredCoverage;
        var availableCandidates = candidates.ToList();
        var selectedDatasets = new List<CatalogDataset>();

        while (!remainingCoverage.IsEmpty)
        {
            CatalogDataset? bestCandidate = null;
            var greatestCoverageGain = 0d;

            foreach (var candidate in availableCandidates)
            {
                var candidateCoverage = candidate.CoverageGeometry!;
                if (!candidateCoverage.EnvelopeInternal.Intersects(remainingCoverage.EnvelopeInternal))
                {
                    continue;
                }

                var coverageGain = remainingCoverage.Intersection(candidateCoverage).Area;
                if (coverageGain > greatestCoverageGain)
                {
                    greatestCoverageGain = coverageGain;
                    bestCandidate = candidate;
                }
            }

            if (bestCandidate is null)
            {
                return null;
            }

            selectedDatasets.Add(bestCandidate);
            remainingCoverage = remainingCoverage.Difference(bestCandidate.CoverageGeometry!);
            availableCandidates.Remove(bestCandidate);
        }

        return selectedDatasets;
    }
}