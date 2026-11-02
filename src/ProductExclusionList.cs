namespace UKHO.S100CoverageTool;

public static class ProductExclusionList
{
    public static string GetFileNameWithoutExtension(string catalogFileName)
    {
        var path = Uri.TryCreate(catalogFileName, UriKind.Absolute, out var uri) && uri.IsFile
            ? uri.LocalPath
            : catalogFileName;

        return Path.GetFileNameWithoutExtension(path);
    }

    public static HashSet<string> Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return File.ReadLines(filePath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}