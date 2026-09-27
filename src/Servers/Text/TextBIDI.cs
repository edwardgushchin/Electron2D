using Electron2D.TextFormatting.Unicode;

namespace Electron2D;

/// <summary>Resolves paragraph embedding levels using reusable Unicode algorithm storage.</summary>
/// <remarks>Instances are owned by one layout operation/thread. Level indices count Unicode scalars;
/// returned storage is valid until the next Resolve call. Glyph shaping and line reordering remain separate.</remarks>
internal sealed class TextBIDI
{
    private readonly BidiData _data = new();
    private readonly BidiAlgorithm _algorithm = new();
    private int _paragraphLevel;

    /// <summary>Gets resolved levels for the last paragraph, in logical scalar order.</summary>
    internal ReadOnlySpan<sbyte> Levels => _algorithm.ResolvedLevels.Span;
    /// <summary>Gets the last resolved paragraph direction, zero for LTR or one for RTL.</summary>
    internal int ParagraphLevel => _paragraphLevel;

    /// <summary>Resolves one paragraph with an explicit LTR/RTL level or automatic first-strong direction.</summary>
    /// <param name="text">UTF-16 paragraph text.</param>
    /// <param name="paragraphLevel">Zero for LTR, one for RTL, or two for automatic direction.</param>
    /// <exception cref="ArgumentOutOfRangeException">The paragraph level is not zero, one or two.</exception>
    internal void Resolve(ReadOnlySpan<char> text, sbyte paragraphLevel = 2)
    {
        if (paragraphLevel is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(paragraphLevel));
        _data.Reset(); _data.ParagraphEmbeddingLevel = paragraphLevel; _data.Append(text);
        // The imported BidiData reuse hints can remain false after Reset. Unknown hints require a fresh scan.
        _algorithm.Process(_data.Classes, _data.PairedBracketTypes, _data.PairedBracketValues, paragraphLevel, null, null, null, null);
        _paragraphLevel = _data.Length == 0 ? paragraphLevel == 1 ? 1 : 0 : _algorithm.ResolvedParagraphEmbeddingLevel;
        var levels = _algorithm.ResolvedLevels.Span;
        var bracket = -1; var index = 0; var codepoints = new CodepointEnumerator(text);
        while (codepoints.MoveNext(out var codepoint))
        {
            if (_data.PairedBracketTypes[index] != BidiPairedBracketType.None) bracket = index;
            // The pinned core's N0 bracket routine writes the correct following-NSM direction into its
            // original-class buffer instead of its working buffer. Those changed input entries precisely
            // identify its matched-bracket NSM runs, including X9 controls skipped by the core's run map.
            // Complete N0 in our result: the bracket and these marks share explicit state and type, hence
            // I1/I2 require identical levels. Other original classes and unmarked NSMs are untouched.
            if (bracket >= 0 && codepoint.BiDiClass == BidiClass.NonspacingMark && _data.Classes[index] is BidiClass.LeftToRight or BidiClass.RightToLeft)
                levels[index] = levels[bracket];
            index++;
        }
    }
}
