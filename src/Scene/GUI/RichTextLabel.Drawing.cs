namespace Electron2D;

public partial class RichTextLabel
{
    private Vector2 ContentOrigin
    {
        get { var surplus = Math.Max(0, _textRect.Size.Y - _content.Y); var y = _vertical switch { VerticalAlignment.Center => surplus / 2, VerticalAlignment.Bottom => surplus, _ => 0 }; return _textRect.Position + new Vector2(0, y - (float)_scroll.Value); }
    }
    private void DrawRichDocument()
    {
        ObserveWorker(); if ((!_finished && _drawParagraphs.Count == 0) || _themeValues == null) { if (_threaded && _elapsed - _pendingSince >= _progressDelay / 1000d) { var width = Math.Max(1, Size.X); DrawRect(new(0, 0, width, 4), GetThemeColor("default_color")); } return; }
        _drawing = true; try
        {
            _themeValues.Normal?.Draw(this, new(Vector2.Zero, Size)); if (HasFocus()) GetThemeStyleBox("focus")?.Draw(this, new(Vector2.Zero, Size)); var origin = ContentOrigin;
            foreach (var paragraph in _drawParagraphs)
            {
                _currentDraw = paragraph; _drawOrigin = origin + paragraph.Origin + new Vector2(0, paragraph.Y); if (_drawOrigin.Y + paragraph.Height < _textRect.Position.Y || _drawOrigin.Y > _textRect.End.Y) continue;
                var format = paragraph.Parts.Count > 0 ? paragraph.Parts[0].Format : paragraph.Format; var indent = format.Indent * _tabSize * _themeValues.Fonts[0].GetCharSize(' ', _themeValues.Sizes[0]).X; _drawOrigin.X += indent;
                if (paragraph.Marker.Length > 0) paragraph.MarkerLayout.Draw(this, new(_drawOrigin.X - paragraph.MarkerLayout.Size.X - 4, _drawOrigin.Y + paragraph.Layout.FirstAscent), _themeValues.DefaultColor, clipRect: _textRect);
                foreach (var part in paragraph.Parts)
                {
                    if (part.Kind == PartKind.Dropcap) { var pos = origin + paragraph.Origin + new Vector2(part.Rect.Position.X, paragraph.Y + part.Rect.Position.Y + part.DropLayout.FirstAscent); if (part.DropOutline > 0) part.DropLayout.Draw(this, pos, part.DropOutlineColor, outline: part.DropOutline, outlinePass: true, clipRect: _textRect); part.DropLayout.Draw(this, pos, part.Color, clipRect: _textRect); }
                    if (part.Table is { } table) { var tableOrigin = origin + paragraph.Origin + new Vector2(part.Rect.Position.X, paragraph.Y + part.Rect.Position.Y); for (var c = 0; c < table.Cells.Count; c++) { var cell = table.Cells[c]; var row = c / table.Columns.Length; var rect = new Rect2(tableOrigin + cell.Rect.Position, cell.Rect.Size); var bg = (row % 2 == 0 ? cell.Even : cell.Odd) ?? GetThemeColor(row % 2 == 0 ? "table_even_row_bg" : "table_odd_row_bg"); if (bg.A > 0) DrawRect(rect, bg); var border = cell.Border ?? GetThemeColor("table_border"); if (border.A > 0) DrawRect(rect, border, false); } }
                }
                if (_selectionFrom >= 0 && _selectionTo > _selectionFrom) { for (var scalar = 0; scalar < paragraph.Styles.Length; scalar++) { if (paragraph.Offsets[scalar] < _selectionFrom || paragraph.Offsets[scalar] >= _selectionTo) continue; var bounds = paragraph.Layout.GetCharacterBounds(scalar); if (bounds.Size.X > 0) DrawRect(new(_drawOrigin + bounds.Position, bounds.Size), GetThemeColor("selection_color")); } }
                var shadow = GetThemeColor("font_shadow_color"); if (shadow.A > 0) { _drawShadow = true; _drawOutline = true; paragraph.Layout.DrawRich(this, _drawOrigin + new Vector2(0, paragraph.Layout.FirstAscent), _glyphRenderer, visibleCharacters: GlyphBudget(paragraph), visibleBehavior: (int)GlyphDirection(), outline: true, clip: _textRect); _drawOutline = false; paragraph.Layout.DrawRich(this, _drawOrigin + new Vector2(0, paragraph.Layout.FirstAscent), _glyphRenderer, visibleCharacters: GlyphBudget(paragraph), visibleBehavior: (int)GlyphDirection(), clip: _textRect); _drawShadow = false; }
                _drawOutline = true; paragraph.Layout.DrawRich(this, _drawOrigin + new Vector2(0, paragraph.Layout.FirstAscent), _glyphRenderer, visibleCharacters: GlyphBudget(paragraph), visibleBehavior: (int)GlyphDirection(), outline: true, clip: _textRect, outlineSize: _themeValues.OutlineSize);
                _drawOutline = false; paragraph.Layout.DrawRich(this, _drawOrigin + new Vector2(0, paragraph.Layout.FirstAscent), _glyphRenderer, visibleCharacters: GlyphBudget(paragraph), visibleBehavior: (int)GlyphDirection(), clip: _textRect, foreground: _glyphForeground);
            }
        }
        finally { _drawing = false; _currentDraw = null; }
    }
    private TextVisibleCharactersBehavior GlyphDirection() => _visibleBehavior == TextVisibleCharactersBehavior.GlyphsAuto ? (IsLayoutRTL() ? TextVisibleCharactersBehavior.GlyphsRTL : TextVisibleCharactersBehavior.GlyphsLTR) : _visibleBehavior;
    private int GlyphBudget(Paragraph paragraph) { if (_visibleCharacters < 0 || _visibleBehavior <= TextVisibleCharactersBehavior.CharsAfterShaping) return -1; var budget = (int)Math.Floor(_totalGlyphs * _visibleRatio); return GlyphDirection() == TextVisibleCharactersBehavior.GlyphsRTL ? Math.Max(0, paragraph.GlyphStart + paragraph.Layout.GlyphCount - (_totalGlyphs - budget)) : Math.Max(0, budget - paragraph.GlyphStart); }
    private void DrawRichGlyph(TextGlyphDrawing glyph)
    {
        var paragraph = _currentDraw!; var local = Math.Min(glyph.Range.X, Math.Max(0, paragraph.Styles.Length - 1)); var style = paragraph.Styles.Length > 0 ? paragraph.Styles[local] : paragraph.Format; var from = paragraph.Offsets[Math.Min(glyph.Range.X, paragraph.Styles.Length)]; var to = paragraph.Offsets[Math.Min(glyph.Range.Y, paragraph.Styles.Length)];
        if (_visibleCharacters >= 0 && _visibleBehavior <= TextVisibleCharactersBehavior.CharsAfterShaping && to > _visibleCharacters) { glyph.Visible = false; glyph.Advance = 0; return; }
        glyph.Color = _drawOutline ? style.OutlineColor ?? _themeValues!.OutlineColor : style.Color ?? _themeValues!.DefaultColor; glyph.OutlineSize = _drawOutline ? (style.OutlineSize >= 0 ? style.OutlineSize : _themeValues!.OutlineSize) : 0;
        if (_drawShadow) glyph.OutlineSize = _drawOutline ? GetThemeConstant("shadow_outline_size") : 0;
        if (_drawOutline && glyph.OutlineSize <= 0) { glyph.Visible = false; glyph.Advance = 0; return; }
        if (!_drawOutline && !_drawShadow && _selectionFrom >= 0 && from >= _selectionFrom && from < _selectionTo) { var selected = GetThemeColor("font_selected_color"); if (selected.A > 0) glyph.Color = selected; }
        var customOK = true; var faded = false;
        for (var fx = style.Effects; fx != null; fx = fx.Parent)
        {
            var elapsed = _elapsed - fx.Started; double Number(string name, double fallback) => fx.Env.TryGetNumber(name, out var n) ? n : fallback; var relative = from - fx.Start;
            switch (fx.Kind)
            {
                case "fade": if (faded) break; faded = true; var begin = Number("start", 0); var length = Number("length", 10); glyph.Color.A = length <= 0 ? relative < begin ? 1 : 0 : (float)Math.Clamp(1 - (relative - begin) / length, 0, 1); break;
                case "wave": if (!Connected(fx, glyph)) fx.PreviousOffset = new(0, (float)(Math.Sin(elapsed * Number("freq", 1) + glyph.Position.X / 50) * Number("amp", 20) / 10)); glyph.Offset += fx.PreviousOffset; break;
                case "tornado": if (!Connected(fx, glyph)) { var phase = elapsed * Number("freq", 1) + glyph.Position.X / 50; var radius = Number("radius", 10); fx.PreviousOffset = new((float)(Math.Sin(phase) * radius), (float)(Math.Cos(phase) * radius)); } glyph.Offset += fx.PreviousOffset; break;
                case "shake": if (!Connected(fx, glyph)) { var rate = Math.Max(0, Number("rate", 24)); var phase = elapsed * rate; var tick = (long)Math.Floor(phase); var strength = Number("level", Number("strength", 5)); var prior = ShakeOffset(fx.Seed, from, tick - 1, strength); var next = ShakeOffset(fx.Seed, from, tick, strength); fx.PreviousOffset = prior.Lerp(next, (float)Math.Clamp((phase - tick) * 2, 0, 1)); } glyph.Offset += fx.PreviousOffset; break;
                case "rainbow": var rainbow = Rainbow(Math.Max(0, Number("freq", 1)) * Math.Abs(elapsed * Number("speed", 1) + glyph.Position.X / 50), (float)Number("sat", .8), (float)Number("val", .8)); rainbow.A = glyph.Color.A; glyph.Color = rainbow; break;
                case "pulse": var pulse = fx.Env.TryGet<Color>("color", out var target) ? target : Colors.White; var frequency = Number("freq", 1); var weight = frequency > 0 ? (float)Mathf.Ease(Mathf.PingPong(elapsed, 1 / frequency) * frequency, Number("ease", -2)) : 0; glyph.Color = glyph.Color.Lerp(glyph.Color * pulse, weight); break;
                case "custom": if (!customOK || fx.Effect is not { IsDisposed: false }) break; var state = fx.State; state.GlyphFace = glyph.Face; state.Font = glyph.Font; state.Env = fx.Env; state.GlyphIndex = checked((int)glyph.Index); state.GlyphFlags = glyph.Flags; state.GlyphCount = glyph.Count; state.Range = new(from, to); state.RelativeIndex = relative; state.ElapsedTime = elapsed; state.Color = glyph.Color; state.Offset = glyph.Offset; state.Transform = glyph.Transform; state.Visible = glyph.Visible; state.Outline = _drawOutline || _drawShadow; try { customOK = fx.Effect.ProcessEffect(state); } finally { state.GlyphFace = null; } glyph.Color = state.Color; glyph.Offset = state.Offset; glyph.Transform = state.Transform; glyph.Index = unchecked((uint)state.GlyphIndex); glyph.Visible &= state.Visible; break;
            }
            fx.PreviousScalar = from;
        }
        if (_drawShadow) { var shadow = GetThemeColor("font_shadow_color"); shadow.A *= glyph.Color.A; glyph.Color = shadow; glyph.Offset += new Vector2(GetThemeConstant("shadow_offset_x"), GetThemeConstant("shadow_offset_y")); }
        if (!glyph.Visible) return;
        if (glyph.ObjectIndex >= 0)
        {
            if (_drawOutline || _drawShadow) return; var part = paragraph.Inline[glyph.ObjectIndex]; var rect = new Rect2(_drawOrigin + part.Rect.Position, part.Rect.Size);
            if (part.Kind == PartKind.Image && part.Texture is { IsDisposed: false } texture) { var source = part.Region.Size.X > 0 && part.Region.Size.Y > 0 ? part.Region : new Rect2(Vector2.Zero, texture.GetSize()); if (part.Pad) { var imageSize = new Vector2(Math.Min(rect.Size.X, source.Size.X), Math.Min(rect.Size.Y, source.Size.Y)); rect = new(rect.Position + (rect.Size - imageSize) / 2, imageSize); } DrawTextureRectRegion(texture, rect, source, part.Color); }
            else if (part.Kind == PartKind.Rule) { var align = int.TryParse(part.Text, out var a) ? (HorizontalAlignment)a : HorizontalAlignment.Center; var x = align == HorizontalAlignment.Right ? _textRect.Size.X - rect.Size.X : align == HorizontalAlignment.Center ? (_textRect.Size.X - rect.Size.X) / 2 : 0; var ruleRect = new Rect2(new(_drawOrigin.X + x, rect.Position.Y), rect.Size); var icon = GetThemeIcon("horizontal_rule"); if (icon is { IsDisposed: false }) DrawTextureRect(icon, ruleRect, false, part.Color); else DrawRect(ruleRect, part.Color); }
            return;
        }
        if (_drawOutline || _drawShadow) return; var bounds = new Rect2(new(glyph.Position.X, glyph.Position.Y - glyph.Size * .8f), new(Math.Max(0, glyph.Advance), glyph.Size)); if (style.BG is { } bg && bg.A > 0) DrawRect(HighlightBounds(bounds), bg);
        var underline = style.Underline || style.Meta != null && _metaUnderlined && (style.MetaUnderline == MetaUnderline.Always || style.MetaUnderline == MetaUnderline.OnHover && ReferenceEquals(style.Meta, _hoverMeta));
        if (underline) { var color = style.UnderlineColor is { A: > 0 } u ? u : glyph.Color; if (style.UnderlineColor is not { A: > 0 }) color.A *= GetThemeConstant("underline_alpha") / 100f; DrawLine(new(bounds.Position.X, glyph.Position.Y + glyph.Font!.GetUnderlinePosition(glyph.Size)), new(bounds.End.X, glyph.Position.Y + glyph.Font!.GetUnderlinePosition(glyph.Size)), color, Math.Max(1, glyph.Font!.GetUnderlineThickness(glyph.Size))); }
        if (style.Hint.Length > 0 && _hintUnderlined) { var color = glyph.Color; color.A *= GetThemeConstant("underline_alpha") / 100f; var y = glyph.Position.Y + glyph.Font!.GetUnderlinePosition(glyph.Size); DrawDashedLine(new(bounds.Position.X, y), new(bounds.End.X, y), color, Math.Max(1, glyph.Font.GetUnderlineThickness(glyph.Size)), 2); }
        if (style.Strike) { var color = style.StrikeColor is { A: > 0 } s ? s : glyph.Color; if (style.StrikeColor is not { A: > 0 }) color.A *= GetThemeConstant("strikethrough_alpha") / 100f; DrawLine(new(bounds.Position.X, bounds.Position.Y + bounds.Size.Y / 2), new(bounds.End.X, bounds.Position.Y + bounds.Size.Y / 2), color); }

    }
    private Rect2 HighlightBounds(Rect2 bounds) { var padding = new Vector2(GetThemeConstant("text_highlight_h_padding"), GetThemeConstant("text_highlight_v_padding")); return new(bounds.Position - padding, bounds.Size + padding * 2); }
    private void DrawRichForeground(TextGlyphDrawing glyph) { if (_drawOutline || _drawShadow || !glyph.Visible) return; var paragraph = _currentDraw!; var style = paragraph.Styles[Math.Min(glyph.Range.X, paragraph.Styles.Length - 1)]; if (style.FG is { } fg && fg.A > 0) DrawRect(HighlightBounds(new(new(glyph.Position.X, glyph.Position.Y - glyph.Size * .8f), new(Math.Max(0, glyph.Advance), glyph.Size))), fg); }
    private static Vector2 ShakeOffset(uint salt, int scalar, long tick, double strength) { var seed = unchecked(salt + (uint)(scalar * 0x9e3779b9 + tick * 0x85ebca6b)); seed ^= seed >> 16; seed *= 0x7feb352d; seed ^= seed >> 15; var angle = seed / (double)uint.MaxValue * Math.Tau; return new((float)(Math.Sin(angle) * strength / 10), (float)(Math.Cos(angle) * strength / 10)); }
    private static Color Rainbow(double hue, float saturation, float value) { var rgb = OkColor.HSVToSRGB((float)(hue % 1), saturation, value); return new(rgb.R, rgb.G, rgb.B); }
    private static bool Connected(FX fx, TextGlyphDrawing glyph) { var connected = (!fx.Env.TryGet<bool>("connected", out var flag) || flag) && (!fx.Env.TryGetNumber("connected", out var number) || number != 0); return fx.PreviousScalar >= 0 && (glyph.Count == 0 || connected && (glyph.Flags & TextGraphemeFlags.Connected) != 0); }
    private static string Letters(int value) { var result = ""; while (value > 0) { value--; result = (char)('a' + value % 26) + result; value /= 26; } return result; }
    private static string Roman(int value) { var text = ""; foreach (var pair in new[] { (1000, "m"), (900, "cm"), (500, "d"), (400, "cd"), (100, "c"), (90, "xc"), (50, "l"), (40, "xl"), (10, "x"), (9, "ix"), (5, "v"), (4, "iv"), (1, "i") }) while (value >= pair.Item1) { text += pair.Item2; value -= pair.Item1; } return text; }
}
