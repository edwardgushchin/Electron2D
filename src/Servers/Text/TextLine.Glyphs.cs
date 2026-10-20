namespace Electron2D;

public sealed partial class TextLine
{
    internal readonly record struct GlyphSpan(int Start, int End, int Index, string Language, TextObjectKey? Object);
    private readonly List<GlyphSpan> _glyphSpans = [];
    internal List<GlyphSpan> ServerGlyphSpans()
    {
        var source = ServerShapingSource ?? this;
        if (!ReferenceEquals(source, this)) return source.ServerGlyphSpans();
        _glyphSpans.Clear(); var si = 0; var oi = 0;
        while (si < _spans.Count || oi < _objects.Count)
        {
            if (si < _spans.Count && (oi == _objects.Count || _spans[si].At < _objects[oi].At))
            { var span = _spans[si++]; _glyphSpans.Add(new(span.At + ServerOffset, span.At + span.Length + ServerOffset, _glyphSpans.Count, span.Language, null)); }
            else
            { var item = _objects[oi++]; _glyphSpans.Add(new(item.At + ServerOffset, item.At + item.Length + ServerOffset, _glyphSpans.Count, string.Empty, item.Key)); }
        }
        return _glyphSpans;
    }
    internal static GlyphSpan FindGlyphSpan(List<GlyphSpan> spans, int start, int end)
    {
        var low = 0; var high = spans.Count;
        while (low < high) { var mid = low + (high - low) / 2; if (spans[mid].End <= start) low = mid + 1; else high = mid; }
        return low < spans.Count && spans[low].Start <= start && spans[low].End >= end ? spans[low] : new(0, 0, -1, string.Empty, null);
    }
}
