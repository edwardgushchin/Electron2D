namespace Electron2D;

public abstract partial class TextServer
{
    internal readonly ref struct GlyphRead
    {
        private readonly ImplementationCall _call;
        internal readonly ReadOnlySpan<TextGlyph> Glyphs;
        internal GlyphRead(TextServer owner, RID shaped, bool ellipsis, bool logical)
        {
            _call = owner.EnterImplementation(shaped, false);
            try
            {
                var dispatcher = _call.Dispatcher; var range = dispatcher.OnShapedTextGetRange(shaped);
                if (range.X < 0 || range.Y < range.X) throw new InvalidOperationException("The text implementation returned an invalid scalar buffer range.");
                Glyphs = ellipsis ? dispatcher.OnShapedTextGetEllipsisGlyphs(shaped) : logical ? dispatcher.OnShapedTextSortLogical(shaped) : dispatcher.OnShapedTextGetGlyphs(shaped);
                for (var i = 0; i < Glyphs.Length; i++)
                {
                    var glyph = Glyphs[i];
                    if (!glyph.Offset.IsFinite() || !float.IsFinite(glyph.Advance) || !float.IsFinite(glyph.Advance * glyph.Repeat) || glyph.Repeat < 0 || glyph.Repeat > 1048576 || glyph.Count < 0 || glyph.Count > Glyphs.Length - i || glyph.Index < 0 || glyph.SpanIndex < -1 || glyph.FontSize < 0 || glyph.FontRID != default && glyph.FontSize == 0 || ((int)glyph.Flags & ~16383) != 0 ||
                        (ellipsis ? glyph.Start != -1 || glyph.End != -1 : glyph.Start < range.X || glyph.End < glyph.Start || glyph.End > range.Y))
                        throw new InvalidOperationException("The text implementation returned an invalid shaped glyph record.");
                }
            }
            catch (Exception failure) { _call.Dispose(failure); throw; }
        }
        public void Dispose() => _call.Dispose();
    }
    internal GlyphRead ReadGlyphs(RID shaped, bool ellipsis = false, bool logical = false) => new(this, shaped, ellipsis, logical);

    internal readonly ref struct SelectionRead
    {
        private readonly ImplementationCall _call;
        internal readonly ReadOnlySpan<Vector2> Ranges;
        internal SelectionRead(TextServer owner, RID shaped, int start, int end)
        {
            _call = owner.EnterImplementation(shaped, false);
            try
            {
                Ranges = _call.Dispatcher.OnShapedTextGetSelection(shaped, start, end);
                foreach (var range in Ranges) if (!range.IsFinite()) throw new InvalidOperationException("The text implementation returned nonfinite selection geometry.");
            }
            catch (Exception failure) { _call.Dispose(failure); throw; }
        }
        public void Dispose() => _call.Dispose();
    }
    internal SelectionRead ReadSelection(RID shaped, int start, int end) => new(this, shaped, start, end);

    private void DrawCallbackBuffer(RID shaped, CanvasItem canvas, Vector2 position, float clipL, float clipR,
        int outline, Color color, float oversampling, bool outlinePass)
    {
        Font.ValidateDraw(canvas, position, color, oversampling);
        if (!float.IsFinite(clipL) || !float.IsFinite(clipR)) throw new ArgumentOutOfRangeException(nameof(clipL));
        var vertical = ShapedTextGetOrientation(shaped) == TextOrientation.Vertical;
        var rtl = ShapedTextGetInferredDirection(shaped) == TextDirection.RTL;
        var boxes = ShapedTextGetPreserveControl(shaped) || ShapedTextGetPreserveInvalid(shaped);
        var trim = ShapedTextGetTrimPos(shaped); var ellipsis = ShapedTextGetEllipsisPos(shaped);
        using var visual = ReadGlyphs(shaped); using var ending = ReadGlyphs(shaped, true);
        var advance = 0f;
        if (rtl && ellipsis >= 0)
            for (var i = ending.Glyphs.Length - 1; i >= 0; i--)
                DrawCallbackGlyph(ending.Glyphs[i], canvas, position, vertical, ref advance, clipL, clipR, outline, color, oversampling, outlinePass, false);
        for (var i = 0; i < visual.Glyphs.Length; i++)
        {
            if (trim >= 0 && (rtl ? i < trim : i >= trim)) continue;
            DrawCallbackGlyph(visual.Glyphs[i], canvas, position, vertical, ref advance, clipL, clipR, outline, color, oversampling, outlinePass, boxes);
        }
        if (!rtl && ellipsis >= 0)
            foreach (var glyph in ending.Glyphs)
                DrawCallbackGlyph(glyph, canvas, position, vertical, ref advance, clipL, clipR, outline, color, oversampling, outlinePass, false);
    }
    private void DrawCallbackGlyph(TextGlyph glyph, CanvasItem canvas, Vector2 position, bool vertical, ref float advance,
        float clipL, float clipR, int outline, Color color, float oversampling, bool outlinePass, bool boxes)
    {
        if (!float.IsFinite(glyph.Advance) || !glyph.Offset.IsFinite() || glyph.Repeat < 0 || glyph.Repeat > 1048576)
            throw new InvalidOperationException("The text implementation returned invalid glyph geometry.");
        for (var repeat = 0; repeat < glyph.Repeat; repeat++)
        {
            var pen = advance; advance += glyph.Advance;
            if (!float.IsFinite(advance)) throw new InvalidOperationException("Shaped glyph drawing exceeds finite advance geometry.");
            if (clipL >= 0 && pen < clipL || clipR >= 0 && advance > clipR) continue;
            var at = position + (vertical ? new Vector2(0, pen) : new Vector2(pen, 0)) + glyph.Offset;
            if (glyph.FontRID != default && glyph.Index == 0 || (glyph.Flags & TextGraphemeFlags.Tab) != 0) continue;
            if (glyph.FontRID != default)
            {
                if (outlinePass) FontDrawGlyphOutline(glyph.FontRID, canvas, glyph.FontSize, outline, at, glyph.Index, color, oversampling);
                else FontDrawGlyph(glyph.FontRID, canvas, glyph.FontSize, at, glyph.Index, color, oversampling);
            }
            else if (boxes && !outlinePass && (glyph.Flags & (TextGraphemeFlags.Virtual | TextGraphemeFlags.EmbeddedObject)) == 0)
                DrawHexCodeBox(canvas, glyph.FontSize, at, glyph.Index, color);
        }
    }
}
