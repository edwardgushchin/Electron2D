namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Copies the complete visual glyph records, independently of display trimming.</summary>
    /// <param name="shaped">Live shaped buffer.</param><returns>A caller-owned snapshot in visual order.</returns>
    /// <remarks>Ranges use global scalar coordinates. Source identities are borrowed; no native pointers are exposed.</remarks>
    public TextGlyph[] ShapedTextGetGlyphs(RID shaped)
    { var v = View(shaped); return v.Layout.ServerGlyphSnapshot(v.Line, v.Offset, v.Source.ServerGlyphSpans()); }
    /// <summary>Copies complete glyph records in stable logical scalar order.</summary>
    /// <param name="shaped">Live shaped buffer.</param><returns>An independent snapshot; visual drawing order is unchanged.</returns>
    public TextGlyph[] ShapedTextSortLogical(RID shaped)
        => ShapedTextGetGlyphs(shaped).OrderBy(static glyph => glyph.Start).ThenByDescending(static glyph => glyph.Count).ThenBy(static glyph => (glyph.Flags & TextGraphemeFlags.Virtual) != 0).ToArray();
    /// <summary>Copies only the current display ellipsis records.</summary>
    /// <param name="shaped">Live shaped buffer.</param><returns>An independent array, empty without an active ellipsis.</returns>
    public TextGlyph[] ShapedTextGetEllipsisGlyphs(RID shaped)
    { var v = DisplayGlyphView(shaped); return v.Layout.ServerGlyphSnapshot(v.Line, v.Offset, v.Source.ServerGlyphSpans(), true); }
    /// <summary>Returns the number of current display ellipsis records, excluding repeat expansion.</summary>
    /// <param name="shaped">Live shaped buffer.</param><returns>Zero without an active ellipsis.</returns>
    public int ShapedTextGetEllipsisGlyphCount(RID shaped)
    { var v = DisplayGlyphView(shaped); return v.Layout.ServerEllipsisGlyphCount(v.Line); }
    /// <summary>Finds the visual glyph record for an inline object.</summary>
    /// <param name="shaped">Live shaped buffer.</param><param name="key">Borrowed inline-object identity.</param>
    /// <returns>The buffer-local visual record index, or minus one when absent or partially sliced.</returns>
    public int ShapedTextGetObjectGlyph(RID shaped, TextObjectKey key)
    {
        ArgumentNullException.ThrowIfNull(key); var v = View(shaped); var range = v.Source.ServerObjectRange(key);
        return range.X < 0 ? -1 : v.Layout.ServerObjectGlyph(v.Line, range.X);
    }
    /// <summary>Returns the number of contiguous visual runs with one span, source, size and direction.</summary>
    /// <param name="shaped">Live shaped buffer.</param><returns>Zero for an empty buffer.</returns>
    public int ShapedGetRunCount(RID shaped)
    { var v = View(shaped); return v.Layout.ServerRuns(v.Line, v.Offset, v.Source.ServerGlyphSpans()).Count; }
    /// <summary>Returns one visual run's original source text.</summary>
    /// <param name="shaped">Live shaped buffer.</param><param name="index">Zero-based visual run index.</param><returns>Copied original text in logical order, including object replacement scalars.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The run index is absent.</exception>
    public string ShapedGetRunText(RID shaped, int index)
    { var v = View(shaped); var run = Run(v, index); return v.Source.ServerSliceText(run.Range.X - v.Offset, run.Range.Y - v.Offset); }
    /// <summary>Returns one run's global scalar range, with an exclusive end.</summary>
    /// <param name="shaped">Live buffer.</param><param name="index">Visual run index.</param><returns>Inclusive start and exclusive end.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The run index is absent.</exception>
    public Vector2i ShapedGetRunRange(RID shaped, int index) => Run(View(shaped), index).Range;
    /// <summary>Returns the first and last cluster-leader indices belonging to a visual run.</summary>
    /// <param name="shaped">Live buffer.</param><param name="index">Visual run index.</param><returns>Inclusive buffer-local visual record boundaries. The last leader's Count includes its continuations.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The run index is absent.</exception>
    public Vector2i ShapedGetRunGlyphRange(RID shaped, int index) => Run(View(shaped), index).GlyphRange;
    /// <summary>Returns the actual source identity used by a visual run.</summary>
    /// <param name="shaped">Live buffer.</param><param name="index">Visual run index.</param><returns>A borrowed font identity, or empty for objects and missing replacements.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The run index is absent.</exception>
    public RID ShapedGetRunFontRID(RID shaped, int index) => Run(View(shaped), index).Font;
    /// <summary>Returns the logical font size used by a visual run.</summary>
    /// <param name="shaped">Live buffer.</param><param name="index">Visual run index.</param><returns>Logical pixel font size.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The run index is absent.</exception>
    public int ShapedGetRunFontSize(RID shaped, int index) => Run(View(shaped), index).Size;
    /// <summary>Returns a visual run's authored language tag.</summary>
    /// <param name="shaped">Live buffer.</param><param name="index">Visual run index.</param><returns>The span language, or empty for inline objects.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The run index is absent.</exception>
    public string ShapedGetRunLanguage(RID shaped, int index) => Run(View(shaped), index).Span.Language;
    /// <summary>Returns a visual run's resolved writing direction.</summary>
    /// <param name="shaped">Live buffer.</param><param name="index">Visual run index.</param><returns>LTR or RTL.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The run index is absent.</exception>
    public TextDirection ShapedGetRunDirection(RID shaped, int index) => Run(View(shaped), index).Direction;
    /// <summary>Returns a visual run's borrowed inline-object identity.</summary>
    /// <param name="shaped">Live buffer.</param><param name="index">Visual run index.</param><returns>The original token, or null for ordinary text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The run index is absent.</exception>
    public TextObjectKey? ShapedGetRunObject(RID shaped, int index) => Run(View(shaped), index).Span.Object;

    private static TextLayout.ServerTextRun Run(BufferView view, int index)
    {
        var runs = view.Layout.ServerRuns(view.Line, view.Offset, view.Source.ServerGlyphSpans());
        if ((uint)index >= (uint)runs.Count) throw new ArgumentOutOfRangeException(nameof(index));
        return runs[index];
    }
    private static BufferView DisplayGlyphView(RID shaped)
    {
        var v = View(shaped); var owner = TextBuffers.Resolve(shaped);
        return owner.Owner is TextParagraph paragraph ? paragraph.ServerDisplayGlyphView(owner.Line, shaped) : v with { Layout = v.Source.ServerPreparedDisplay };
    }
}
