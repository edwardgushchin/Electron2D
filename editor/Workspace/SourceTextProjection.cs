namespace Electron2D.Editor;

/// <summary>Maps a normalized code view's changed range back to the source's original line endings.</summary>
internal static class SourceTextProjection
{
    internal static string Normalize(string source) => source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    internal static string Apply(string original, string baseline, string edited)
    {
        if (baseline == edited) return original;
        if (Normalize(original) != baseline) throw new InvalidOperationException("The source view no longer describes its observed revision.");
        var start = 0;
        while (start < baseline.Length && start < edited.Length && baseline[start] == edited[start]) start++;
        var suffix = 0;
        while (suffix < baseline.Length - start && suffix < edited.Length - start && baseline[^(suffix + 1)] == edited[^(suffix + 1)]) suffix++;
        var from = Offset(original, start); var to = Offset(original, baseline.Length - suffix);
        var ending = Ending(original, from);
        return original[..from] + edited[start..(edited.Length - suffix)].Replace("\n", ending, StringComparison.Ordinal) + original[to..];
    }
    private static int Offset(string original, int normalized)
    {
        var index = 0;
        for (var count = 0; count < normalized; count++)
        {
            if (original[index++] == '\r' && index < original.Length && original[index] == '\n') index++;
        }
        return index;
    }
    private static string Ending(string original, int from)
    {
        for (var i = from; i < original.Length; i++)
        {
            if (original[i] == '\r') return i + 1 < original.Length && original[i + 1] == '\n' ? "\r\n" : "\r";
            if (original[i] == '\n') return "\n";
        }
        for (var i = from - 1; i >= 0; i--)
        {
            if (original[i] == '\n') return i > 0 && original[i - 1] == '\r' ? "\r\n" : "\n";
            if (original[i] == '\r') return "\r";
        }
        return "\n";
    }
}
