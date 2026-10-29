using Electron2D.TextFormatting.Unicode;

namespace Electron2D;

internal sealed partial class TextLayout
{
    private readonly List<int> _serverWordGlyphs = [];
    private TextGraphemeFlags ServerGraphemeFlags(Glyph glyph)
    {
        if (glyph.ProviderFlags is { } providerFlags) return providerFlags;
        var flags = (glyph.Face is not null && glyph.Index != 0 || glyph.ObjectIndex >= 0 ? TextGraphemeFlags.Valid : 0) |
            (glyph.RTL ? TextGraphemeFlags.RTL : 0) | (glyph.Virtual ? TextGraphemeFlags.Virtual : 0) |
            (glyph.Space ? TextGraphemeFlags.Space : 0) | (glyph.Tab ? TextGraphemeFlags.Tab : 0) |
            (glyph.Virtual && glyph.Space ? TextGraphemeFlags.BreakSoft : 0) |
            (glyph.Elongation ? TextGraphemeFlags.Elongation : 0) | (glyph.ObjectIndex >= 0 ? TextGraphemeFlags.EmbeddedObject : 0) |
            ((glyph.Flags & 1) != 0 ? TextGraphemeFlags.Connected : 0) | ((glyph.Flags & 4) != 0 ? TextGraphemeFlags.SafeToInsertTatweel : 0);
        if (glyph.Start < 0 || glyph.Start >= _count || _scalars[glyph.Start] == 0xfffc) return flags;
        var scalar = _scalars[glyph.Start]; var whitespace = NativeTextBreak.StringWhitespace(scalar);
        if (whitespace || glyph.Space) flags |= TextGraphemeFlags.Space;
        if (scalar is 9 or 11 || glyph.Tab) flags |= TextGraphemeFlags.Tab;
        if (scalar == '_') flags |= TextGraphemeFlags.Underscore;
        if (scalar == 0x00ad) flags |= TextGraphemeFlags.SoftHyphen;
        if (IsCustomPunctuation(new System.Text.Rune(scalar))) flags |= TextGraphemeFlags.Punctuation;
        if (IsHardBreak(scalar)) flags |= TextGraphemeFlags.BreakHard;
        else if (glyph.Virtual && glyph.Space || glyph.End <= _count && _breaks[glyph.End] && (whitespace || scalar == 0x00ad)) flags |= TextGraphemeFlags.BreakSoft;
        return flags;
    }
    internal int[] ServerWordBreaks(int start, int end, int offset, TextGraphemeFlags delimiters, TextGraphemeFlags skipped)
    {
        if (_providerLayout is { } state)
        {
            using var consumer = TextServerManager.BeginConsumerRead(state.Owner); using var use = state.BeginUse();
            return ProviderBreakRanges(state.Owner.ShapedTextGetWordBreaks(ProviderRangeBuffer(state, start, end), delimiters, skipped), state, start, end, offset);
        }
        _serverWordGlyphs.Clear();
        for (var i = 0; i < _glyphs.Count; i++)
        {
            var glyph = _glyphs[i];
            if (glyph.Start >= start && glyph.Start < end && glyph.End >= glyph.Start && glyph.End <= end) _serverWordGlyphs.Add(i);
        }
        _serverWordGlyphs.Sort(_serverGlyphOrder ??= CompareServerGlyphs);
        var result = new List<int>(); var word = start; var previous = -1;
        foreach (var index in _serverWordGlyphs)
        {
            var glyph = _glyphs[index];
            if (previous >= 0 && _glyphs[previous] is var before && before.Start == glyph.Start && before.End == glyph.End && before.Virtual == glyph.Virtual && before.ObjectIndex == glyph.ObjectIndex) continue;
            previous = index; var flags = ServerGraphemeFlags(glyph);
            if ((flags & delimiters) == 0 || (flags & skipped) != 0) continue;
            var boundary = Math.Max(word, glyph.Start);
            if (word < boundary) { result.Add(word + offset); result.Add(boundary + offset); }
            // Virtual justification records mark a source boundary, not a removable character.
            word = Math.Max(word, glyph.Virtual ? glyph.Start : glyph.End);
        }
        if (_serverWordGlyphs.Count > 0 && word < end) { result.Add(word + offset); result.Add(end + offset); }
        return result.ToArray();
    }
    private static bool IsPinnedPunctuation(uint scalar) => new Codepoint(scalar).GeneralCategory is >= GeneralCategory.Punctuation and <= GeneralCategory.OpenPunctuation;
}
