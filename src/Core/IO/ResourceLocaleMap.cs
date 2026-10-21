namespace Electron2D;

internal sealed class ResourceLocaleMap(ResourceLocaleRemap[] records)
{
    private readonly Dictionary<string, ResourceLocaleRemap[]> _sources = records.GroupBy(item => item.SourcePath, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
    internal bool IsEmpty => _sources.Count == 0;
    internal IEnumerable<string> Paths => _sources.Keys;
    internal bool Contains(string path) => _sources.ContainsKey(path);
    internal bool Matches(ResourceLocaleRemap[] other) => records.Length == other.Length && records.Zip(other).All(pair => pair.First.SourcePath == pair.Second.SourcePath && pair.First.Locale == pair.Second.Locale && pair.First.LocalizedPath == pair.Second.LocalizedPath);
    internal string Resolve(string path, string locale)
    {
        if (!_sources.TryGetValue(path, out var choices)) return path;
        var selected = path; var best = 0;
        foreach (var choice in choices)
        {
            var score = TranslationServer.CompareLocales(locale, choice.Locale);
            if (score <= 0 || score < best) continue;
            selected = choice.LocalizedPath; best = score;
            if (score == 10) break;
        }
        selected = ResourceUID.EnsurePath(selected);
        return selected == path || FileAccess.FileExists(selected) ? selected : path;
    }
}
