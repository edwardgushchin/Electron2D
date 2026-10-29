using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed partial class TextLayout
{
    internal readonly record struct ServerTextRun(Vector2i Range, Vector2i GlyphRange, RID Font, int Size,
        TextDirection Direction, TextLine.GlyphSpan Span);
    private readonly List<ServerTextRun> _serverTextRuns = [];

    private TextGlyph SnapshotGlyph(int index, int first, int limit, int offset, List<TextLine.GlyphSpan> spans, bool ellipsis)
    {
        var glyph = _glyphs[index]; var leader = index == first || !SameSnapshotCluster(_glyphs[index - 1], glyph);
        var count = 0;
        if (leader) for (var i = index; i < limit && SameSnapshotCluster(glyph, _glyphs[i]); i++) count++;
        var flags = ellipsis ? TextGraphemeFlags.Virtual | (glyph.Space ? TextGraphemeFlags.Space | TextGraphemeFlags.BreakSoft : TextGraphemeFlags.Punctuation) | (glyph.RTL ? TextGraphemeFlags.RTL : 0) : ServerGraphemeFlags(glyph);
        if (glyph.ProviderCount is { } providerCount) count = providerCount;
        if (glyph.ProviderFlags is { } providerFlags) flags = providerFlags;
        else if (!leader) flags &= TextGraphemeFlags.Connected | TextGraphemeFlags.SafeToInsertTatweel;
        var span = glyph.ProviderSpanIndex ?? (ellipsis ? -1 : TextLine.FindGlyphSpan(spans, glyph.Start + offset, glyph.End + offset).Index);
        return new(ellipsis ? -1 : glyph.Start + offset, ellipsis ? -1 : glyph.End + offset, glyph.Repeat, count,
            flags, glyph.Offset, glyph.Advance, glyph.SourceRID, glyph.ProviderFlags is not null ? glyph.Size : glyph.ObjectIndex >= 0 ? 0 : GlyphSize(glyph), checked((int)glyph.Index), span);
    }
    private static bool SameSnapshotCluster(Glyph left, Glyph right) => left.Start == right.Start && left.End == right.End &&
        left.Virtual == right.Virtual && left.ObjectIndex == right.ObjectIndex && left.Ellipsis == right.Ellipsis && left.SourceRID == right.SourceRID;

    private readonly List<TextGlyph> _visualSnapshot = [], _logicalSnapshot = [], _ellipsisSnapshot = [];
    private readonly List<(TextGlyph Glyph, int Order)> _logicalGlyphOrder = [];
    internal ReadOnlySpan<TextGlyph> ServerGlyphSpan(int lineIndex, int offset, List<TextLine.GlyphSpan> spans, bool ellipsis = false, bool logical = false)
    {
        var result = ellipsis ? _ellipsisSnapshot : logical ? _logicalSnapshot : _visualSnapshot;
        result.Clear();
        if (_lines.Count == 0) return [];
        var line = _lines[lineIndex]; var end = line.GlyphStart + line.GlyphCount;
        result.EnsureCapacity(line.GlyphCount);
        for (var i = line.GlyphStart; i < end; i++)
            if (_glyphs[i].Ellipsis == ellipsis) result.Add(SnapshotGlyph(i, line.GlyphStart, end, offset, spans, ellipsis));
        if (logical)
        {
            _logicalGlyphOrder.Clear(); _logicalGlyphOrder.EnsureCapacity(result.Count);
            for (var i = 0; i < result.Count; i++) _logicalGlyphOrder.Add((result[i], i));
            _logicalGlyphOrder.Sort(static (left, right) =>
            {
                var order = left.Glyph.Start.CompareTo(right.Glyph.Start); if (order != 0) return order;
                order = right.Glyph.Count.CompareTo(left.Glyph.Count); if (order != 0) return order;
                order = ((left.Glyph.Flags & TextGraphemeFlags.Virtual) != 0).CompareTo((right.Glyph.Flags & TextGraphemeFlags.Virtual) != 0);
                return order != 0 ? order : left.Order.CompareTo(right.Order);
            });
            for (var i = 0; i < result.Count; i++) result[i] = _logicalGlyphOrder[i].Glyph;
        }
        return CollectionsMarshal.AsSpan(result);
    }
    internal int ServerObjectGlyph(int lineIndex, int start)
    {
        if (_lines.Count == 0) return -1; var line = _lines[lineIndex];
        for (var i = line.GlyphStart; i < line.GlyphStart + line.GlyphCount; i++) if (_glyphs[i].ObjectIndex >= 0 && _glyphs[i].Start == start) return i - line.GlyphStart;
        return -1;
    }
    internal int ServerEllipsisGlyphCount(int lineIndex)
    {
        if (_lines.Count == 0) return 0; var line = _lines[lineIndex]; var count = 0;
        for (var i = line.GlyphStart; i < line.GlyphStart + line.GlyphCount; i++) if (_glyphs[i].Ellipsis) count++;
        return count;
    }
    internal List<ServerTextRun> ServerRuns(int lineIndex, int offset, List<TextLine.GlyphSpan> spans)
    {
        _serverTextRuns.Clear(); if (_lines.Count == 0) return _serverTextRuns;
        var line = _lines[lineIndex]; var end = line.GlyphStart + line.GlyphCount;
        for (var i = line.GlyphStart; i < end;)
        {
            var glyph = SnapshotGlyph(i, line.GlyphStart, end, offset, spans, false); var local = i - line.GlyphStart;
            i += Math.Max(1, glyph.Count);
            if (glyph.SpanIndex < 0 || _glyphs[i - 1].Ellipsis) continue;
            var span = TextLine.FindGlyphSpan(spans, glyph.Start, glyph.End);
            var direction = (glyph.Flags & TextGraphemeFlags.RTL) != 0 ? TextDirection.RTL : TextDirection.LTR;
            if (_serverTextRuns.Count > 0 && _serverTextRuns[^1] is var before && before.Font == glyph.FontRID &&
                before.Size == glyph.FontSize && before.Direction == direction && before.Span.Index == span.Index)
                _serverTextRuns[^1] = before with { Range = new(Math.Min(before.Range.X, glyph.Start), Math.Max(before.Range.Y, glyph.End)), GlyphRange = new(before.GlyphRange.X, local) };
            else _serverTextRuns.Add(new(new(glyph.Start, glyph.End), new(local, local), glyph.FontRID, glyph.FontSize, direction, span));
        }
        return _serverTextRuns;
    }
}
