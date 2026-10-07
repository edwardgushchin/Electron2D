using System.Text;

namespace Electron2D;

internal readonly record struct TextLayoutStyleSpan(int Start, int End, Font Font, int Size, string Language = "", int Group = 0);
internal readonly record struct TextLayoutInlineObject(int At, Vector2 Size, InlineAlignment Alignment, int Index, float Baseline = -1);
internal sealed class TextGlyphDrawing
{
    internal Font? Font;
    internal FontData? Face;
    internal uint Index;
    internal int Size, Count, ObjectIndex, OutlineSize;
    internal Vector2i Range;
    internal TextGraphemeFlags Flags;
    internal Vector2 Position, Offset;
    internal float Advance;
    internal Color Color;
    internal Transform Transform;
    internal bool Visible, Outline, Missing;
}

internal sealed partial class TextLayout
{
    private readonly List<Font> _richReadFonts = [];
    private readonly List<Font.ReadScope> _richReads = [];
    private void BeginRichBuild(Font font, TextLayoutOptions? options) { if (options?.Styles is not { } styles) return; _richReadFonts.Clear(); _richReadFonts.Add(font); for (var i = 0; i < styles.Count; i++) if (!_richReadFonts.Contains(styles[i].Font)) _richReadFonts.Add(styles[i].Font); _richReadFonts.Sort(static (a, b) => a.InstanceID.CompareTo(b.InstanceID)); foreach (var item in _richReadFonts) _richReads.Add(item.BeginRead()); }
    private void EndRichBuild() { for (var i = _richReads.Count - 1; i >= 0; i--) _richReads[i].Dispose(); _richReads.Clear(); }
    private readonly TextGlyphDrawing _richDrawing = new();
    private TextLayoutStyleSpan[] _richStyles = [];
    private TextLayoutInlineObject[] _richObjects = [];
    private TextLayoutStyleSpan StyleAt(int scalar) => _options.Styles is not null && (uint)scalar < _count
        ? _richStyles[scalar] : new(0, _count, _font, Key.FontSize, _options.Language);
    private int GlyphSize(Glyph glyph) => glyph.Size > 0 ? glyph.Size : Key.FontSize;
    private void PrepareRichStyles()
    {
        Ensure(ref _richStyles, _count); Ensure(ref _richObjects, _count);
        var ordinary = new TextLayoutStyleSpan(0, _count, _font, Key.FontSize, _options.Language);
        for (var i = 0; i < _count; i++) { _richStyles[i] = ordinary; _richObjects[i] = new(-1, default, default, -1); }
        if (_options.Styles is not null) for (var at = 0; at < _options.Styles.Count; at++)
            { var span = _options.Styles[at]; Font.ValidateSize(span.Size); for (var i = Math.Max(0, span.Start); i < Math.Min(_count, span.End); i++) _richStyles[i] = span; }
        if (_options.Objects is not null) for (var at = 0; at < _options.Objects.Count; at++)
            { var item = _options.Objects[at]; if ((uint)item.At >= _count || !item.Size.IsFinite() || item.Size.X < 0 || item.Size.Y < 0) throw new ArgumentException("Invalid inline object geometry."); _richObjects[item.At] = item; }
    }

    internal int HitCharacter(Vector2 point)
    {
        if (_lines.Count == 0) return 0; var lineIndex = 0; for (var i = 0; i < _lines.Count; i++) { lineIndex = i; if (point.Y < _lines[i].CrossOffset + _lines[i].Height) break; }
        var line = _lines[lineIndex]; var result = line.Start; var distance = float.PositiveInfinity;
        for (var i = line.GlyphStart; i < line.GlyphStart + line.GlyphCount; i++) { var glyph = _glyphs[i]; var left = glyph.Position.X - glyph.Offset.X; var right = left + glyph.Advance * glyph.Repeat; var rtl = (_levels[Math.Min(glyph.Start, Math.Max(0, _count - 1))] & 1) != 0; var nearLeft = Math.Abs(point.X - left); var nearRight = Math.Abs(point.X - right); if (nearLeft < distance) { distance = nearLeft; result = rtl ? glyph.End : glyph.Start; } if (nearRight < distance) { distance = nearRight; result = rtl ? glyph.Start : glyph.End; } }
        return result;
    }
    internal void DrawRich(CanvasItem canvas, Vector2 baseline, Action<TextGlyphDrawing> renderer, int firstLine = 0,
        int maxLines = -1, int visibleCharacters = -1, int visibleBehavior = 1, bool outline = false, Rect2? clip = null, int outlineSize = 0, Action<TextGlyphDrawing>? foreground = null)
    {
        if (IsBusy) throw new InvalidOperationException("An active text layout cannot draw recursively.");
        if (firstLine < 0 || firstLine >= _lines.Count) return;
        _active++;
        try
        {
            var end = maxLines < 0 ? _lines.Count : Math.Min(_lines.Count, firstLine + maxLines);
            var initial = _lines[firstLine]; var origin = baseline - new Vector2(0, initial.CrossOffset + initial.Ascent);
            var total = 0; for (var i = firstLine; i < end; i++) total += _lines[i].GlyphCount;
            var budget = 0;
            for (var i = firstLine; i < end; i++)
            {
                var line = _lines[i]; var removed = 0f;
                for (var j = line.GlyphStart; j < line.GlyphStart + line.GlyphCount; j++)
                {
                    var glyph = _glyphs[j]; var reverse = visibleBehavior == 4 || visibleBehavior == 2 && line.ParagraphLevel == 1;
                    if (visibleCharacters >= 0 && (visibleBehavior <= 1 ? glyph.End > visibleCharacters : reverse ? budget < total - visibleCharacters : budget >= visibleCharacters)) { budget++; continue; }
                    budget++;
                    var count = 0; if (j == line.GlyphStart || _glyphs[j - 1].Start != glyph.Start) for (var k = j; k < line.GlyphStart + line.GlyphCount && _glyphs[k].Start == glyph.Start; k++) count++;
                    var state = _richDrawing; var style = StyleAt(glyph.Start); var objectIndex = glyph.ObjectIndex;
                    using var read = style.Font.BeginRead();
                    state.Font = style.Font; state.Face = glyph.Face; state.Size = GlyphSize(glyph); state.Index = glyph.Index;
                    state.Count = count; state.Range = new(glyph.Start, glyph.End); state.Advance = glyph.Advance * glyph.Repeat;
                    state.Position = origin + glyph.Position - new Vector2(removed, 0); state.Offset = Vector2.Zero;
                    state.Transform = Transform.Identity.Translated(state.Position); state.Color = Colors.White; state.Visible = true; state.Outline = outline;
                    state.OutlineSize = outlineSize; state.ObjectIndex = objectIndex; state.Missing = glyph.Missing;
                    var scalar = glyph.Start < _count ? new Rune((int)_scalars[glyph.Start]) : Rune.ReplacementChar;
                    state.Flags = (glyph.Face is not null && glyph.Index != 0 ? TextGraphemeFlags.Valid : 0) |
                        ((_levels[Math.Min(glyph.Start, Math.Max(0, _count - 1))] & 1) != 0 ? TextGraphemeFlags.RTL : 0) |
                        (glyph.Virtual ? TextGraphemeFlags.Virtual : 0) | (glyph.Space ? TextGraphemeFlags.Space : 0) |
                        (glyph.Tab ? TextGraphemeFlags.Tab : 0) | (glyph.Elongation ? TextGraphemeFlags.Elongation : 0) |
                        (objectIndex >= 0 ? TextGraphemeFlags.EmbeddedObject : 0) |
                        (Rune.IsPunctuation(scalar) ? TextGraphemeFlags.Punctuation : 0) |
                        (scalar.Value == '_' ? TextGraphemeFlags.Underscore : 0) |
                        (scalar.Value == 0x00ad ? TextGraphemeFlags.SoftHyphen : 0) |
                        ((glyph.Flags & 1) != 0 ? TextGraphemeFlags.Connected : 0) |
                        ((glyph.Flags & 4) != 0 ? TextGraphemeFlags.SafeToInsertTatweel : 0);
                    renderer(state);
                    if (!state.Visible) { removed += state.Advance; continue; }
                    if (objectIndex >= 0 || glyph.Tab) continue;
                    if (!state.Color.IsFinite() || !state.Offset.IsFinite() || !state.Transform.IsFinite()) throw new InvalidOperationException("A glyph effect produced invalid geometry or color.");
                    var transform = state.Transform.TranslatedLocal(-state.Position); var transformed = transform != Transform.Identity;
                    if (transformed) canvas.DrawSetTransformMatrix(transform);
                    try
                    {
                        if (glyph.Missing) { if (!outline) TextMissingGlyph.Draw(canvas, state.Size, state.Position + state.Offset, state.Index, state.Color, clip); continue; }
                        if (glyph.Face is null || state.Index == 0) continue;
                        for (var repeat = 0; repeat < glyph.Repeat; repeat++)
                        { var position = state.Position + state.Offset + new Vector2(repeat * glyph.Advance, 0); var image = glyph.Face.GetGlyph(state.Index, state.Size, state.OutlineSize, 0, position, out var raster); DrawGlyph(canvas, image, raster, state.Color, !outline && glyph.Face.ModulateColorGlyphs, clip); }
                    }
                    finally { if (transformed) canvas.DrawSetTransformMatrix(Transform.Identity); }
                    foreground?.Invoke(state);
                }
            }
        }
        finally { _active--; }
    }
}
