using System.Text;
namespace Electron2D;

public partial class RichTextLabel
{
    private sealed record ThemeValues(Font[] Fonts, int[] Sizes, int LineSpacing, int ParagraphSpacing, int HSeparation, int VSeparation, int OutlineSize, Color DefaultColor, Color OutlineColor, StyleBox? Normal);
    private ThemeValues? _themeValues;
    private Frame _renderRoot = new();
    private readonly Dictionary<Font, long> _fontVersions = [];
    private bool _revealDirty, _notifyFinished;
    private Rect2 _fullTextRect;
    private bool _publishing;
    private int _workerReadyParagraphs, _workerPublishedParagraphs;
    private void RevealChanged() { if (_visibleBehavior == TextVisibleCharactersBehavior.CharsBeforeShaping) { _revealDirty = true; _finished = false; } else if (_scrollFollowingVisible) FollowReveal(); QueueRedraw(); }
    private void FollowReveal() { var line = FindCharacterLine(_visibleCharacters); if (line >= 0) _scroll.Value = Math.Max(0, _lines[line].Y - _textRect.Size.Y + LoadedLineHeight(_lines[line])); }
    private TextDirection _effectiveDirection;
    private ThemeValues CaptureTheme()
    {
        string[] names = ["normal", "bold", "italics", "bold_italics", "mono"]; var fonts = new Font[5]; var sizes = new int[5]; for (var i = 0; i < 5; i++) { fonts[i] = GetThemeFont(names[i] + "_font") ?? GetThemeDefaultFont() ?? throw new InvalidOperationException("Rich text requires a font."); sizes[i] = GetThemeFontSize(names[i] + "_font_size"); if (sizes[i] < 1) sizes[i] = GetThemeDefaultFontSize(); }
        return new(fonts, sizes, GetThemeConstant("line_separation"), GetThemeConstant("paragraph_separation"), GetThemeConstant("table_h_separation"), GetThemeConstant("table_v_separation"), GetThemeConstant("outline_size"), GetThemeColor("default_color"), GetThemeColor("font_outline_color"), GetThemeStyleBox("normal"));
    }
    private static Frame CloneFrame(Frame original)
    {
        var frame = new Frame(); frame.Paragraphs.Clear(); foreach (var source in original.Paragraphs) { var paragraph = new Paragraph { Alignment = source.Alignment, Direction = source.Direction, Language = source.Language, Parser = source.Parser, Justification = source.Justification, Tabs = source.Tabs, Format = source.Format }; frame.Paragraphs.Add(paragraph); foreach (var part in source.Parts) { var copy = new Part(part.Kind, part.Format) { Text = part.Text, Texture = part.Texture, Width = part.Width, Height = part.Height, Color = part.Color, Alignment = part.Alignment, Region = part.Region, Key = part.Key, Pad = part.Pad, WidthPercent = part.WidthPercent, HeightPercent = part.HeightPercent, Tooltip = part.Tooltip, Alt = part.Alt, WidthUnit = part.WidthUnit, HeightUnit = part.HeightUnit, DropFont = part.DropFont, DropSize = part.DropSize, DropOutline = part.DropOutline, DropOutlineColor = part.DropOutlineColor, DropMargins = part.DropMargins }; paragraph.Parts.Add(copy); if (part.Table is { } old) { var table = new Table(old.Columns.Length) { AlignRow = old.AlignRow, Name = old.Name }; copy.Table = table; for (var c = 0; c < old.Columns.Length; c++) { var column = old.Columns[c]; table.Columns[c].Expand = column.Expand; table.Columns[c].Ratio = column.Ratio; table.Columns[c].Shrink = column.Shrink; table.Columns[c].Name = column.Name; } foreach (var oldCell in old.Cells) { var cell = new Cell { Minimum = oldCell.Minimum, Maximum = oldCell.Maximum, Padding = oldCell.Padding, Odd = oldCell.Odd, Even = oldCell.Even, Border = oldCell.Border }; var child = CloneFrame(oldCell.Frame); cell.Frame.Paragraphs.Clear(); cell.Frame.Paragraphs.AddRange(child.Paragraphs); table.Cells.Add(cell); } } } }
        return frame;
    }
    private Font PartFont(Format format, ThemeValues theme) => format.Font ?? theme.Fonts[(int)format.Role];
    private int PartSize(Format format, ThemeValues theme) => format.Size > 0 ? format.Size : theme.Sizes[(int)format.Role];
    private void PrepareFrame(Frame frame, ThemeValues theme, ref int global)
    {
        for (var index = 0; index < frame.Paragraphs.Count; index++)
        {
            var paragraph = frame.Paragraphs[index]; paragraph.Start = global; var listFormat = paragraph.Parts.Count > 0 ? paragraph.Parts[0].Format : paragraph.Format;
            if (listFormat.List is { } list) { var number = index + 1; paragraph.Marker = list switch { ListType.Dots => listFormat.Bullet, ListType.Letters => Letters(number), ListType.Roman => Roman(number), _ => number.ToString(System.Globalization.CultureInfo.InvariantCulture) }; if (list != ListType.Dots) paragraph.Marker += "."; if (listFormat.Capitalize) paragraph.Marker = paragraph.Marker.ToUpperInvariant(); paragraph.MarkerLayout.Build(theme.Fonts[0], new(paragraph.Marker, theme.Sizes[0], 0, HorizontalAlignment.Left, -1, 0, 0, TextDirection.LTR, TextOrientation.Horizontal)); }
            var text = new StringBuilder(); var styles = new List<Format>(); var offsets = new List<int>();
            foreach (var part in paragraph.Parts)
            {
                if (part.Kind == PartKind.Marker) continue; if (part.Format.Effects != null) _hasFX = true; part.Start = global; part.Local = styles.Count; part.Count = 0; var font = PartFont(part.Format, theme); _fontVersions[font] = font.GetContentGeneration();
                if (part.Kind == PartKind.Text) { text.Append(part.Text); foreach (var rune in part.Text.EnumerateRunes()) { styles.Add(part.Format); offsets.Add(global++); part.Count++; } }
                else if (part.Kind == PartKind.Dropcap) { var capFont = part.DropFont ?? font; _fontVersions[capFont] = capFont.GetContentGeneration(); part.DropLayout.Build(capFont, new(part.Text, part.DropSize, 0, HorizontalAlignment.Left, -1, 0, 0, _direction, TextOrientation.Horizontal)); }
                else { if (part.Table is { } table) foreach (var cell in table.Cells) PrepareFrame(cell.Frame, theme, ref global); else if (part.Kind == PartKind.Image) global++; part.Count = global - part.Start; text.Append('\ufffc'); styles.Add(part.Format); offsets.Add(part.Start); }
            }
            offsets.Add(global); paragraph.Text = text.ToString(); paragraph.Styles = styles.ToArray(); paragraph.Offsets = offsets.ToArray(); paragraph.Count = global - paragraph.Start + (index + 1 < frame.Paragraphs.Count ? 1 : 0);
            paragraph.Spans.Clear(); for (var at = 0; at < styles.Count;) { var next = at + 1; while (next < styles.Count && ReferenceEquals(styles[next], styles[at])) next++; var format = styles[at]; paragraph.Spans.Add(new(at, next, PartFont(format, theme), PartSize(format, theme), format.Language.Length > 0 ? format.Language : paragraph.Language.Length > 0 ? paragraph.Language : _language, at)); at = next; }
            ParseStructuredText(paragraph.Parser == StructuredTextParser.Default ? _parser : paragraph.Parser, _parserOptions, paragraph.Text, paragraph.Contexts);
            if (index + 1 < frame.Paragraphs.Count) global++;
        }
    }
    private void StopWorker()
    { if (_worker == null) return; try { _worker.GetAwaiter().GetResult(); } catch (Exception error) { _workerError = error; } _worker = null; }
    private void EnsureRichLayout()
    {
        CheckRich(); if (_building || _publishing) return; ObserveWorker(); if (_worker is { IsCompleted: false }) return; foreach (var pair in _fontVersions) if (pair.Key.IsDisposed || pair.Key.GetContentGeneration() != pair.Value) { _dirty = true; break; }
        if (!_dirty && !_revealDirty) return;
        if (_worker is { IsCompleted: false }) return;
        _building = true; try
        {
            _effectiveDirection = _direction == TextDirection.Inherited ? (IsLayoutRTL() ? TextDirection.RTL : TextDirection.LTR) : _direction; if (_dirty || _themeValues == null) _themeValues = CaptureTheme(); var theme = _themeValues!; var margin = theme.Normal?.GetMinimumSize() ?? Vector2.Zero; var offset = theme.Normal?.GetOffset() ?? Vector2.Zero; var width = Math.Max(1, Size.X - margin.X); _textRect = new(offset, new(width, Math.Max(1, Size.Y - margin.Y))); _fullTextRect = _textRect;
            if (_dirty) { _hasFX = false; _fontVersions.Clear(); _renderRoot = CloneFrame(_main); var global = 0; PrepareFrame(_renderRoot, theme, ref global); _totalCharacters = CountFrame(_main); }
            _dirty = _revealDirty = false; var root = _renderRoot; var actualWidth = width - (_scrollActive ? _scroll.GetBoundMinimumSize().X : 0); actualWidth = Math.Max(1, actualWidth);
            if (_threaded) { _finished = false; _workerError = null; _workerReadyParagraphs = _workerPublishedParagraphs = 0; _drawParagraphs.Clear(); _lines.Clear(); _content = Vector2.Zero; _worker = StartWorker(root, actualWidth, theme); SetInternalProcessing(true, false); }
            else { ShapeFrame(root, actualWidth, theme, 0); PublishLayout(); }
        }
        finally { _building = false; }
        DeliverFinished();
    }
    private Task StartWorker(Frame root, float width, ThemeValues theme) => Task.Run(() => { try { ShapeFrame(root, width, theme, 0, true); } catch (Exception error) { _workerError = error; } });
    private void ObserveWorker() { if (_worker is { IsCompleted: false }) { var ready = Volatile.Read(ref _workerReadyParagraphs); if (ready > _workerPublishedParagraphs) { _workerPublishedParagraphs = ready; PublishLayout(ready, false); } return; } if (_worker is not { IsCompleted: true }) return; _worker.GetAwaiter().GetResult(); _worker = null; if (_workerError != null) { var error = _workerError; _workerError = null; throw new InvalidOperationException("Rich-text shaping failed.", error); } PublishLayout(); DeliverFinished(); }
    private void DeliverFinished() { if (!_notifyFinished || _building || IsDisposed) return; _notifyFinished = false; Finished?.Invoke(); }
    private void PublishLayout(int paragraphs = -1, bool complete = true)
    {
        if (_publishing) return; _publishing = true; try
        {
            _textRect = _fullTextRect; _drawParagraphs.Clear(); _lines.Clear(); _totalGlyphs = 0; CountGlyphOffsets(_renderRoot, paragraphs); CollectLayout(_renderRoot, Vector2.Zero, limit: paragraphs); _content = _renderRoot.Size; if (!complete) { var widest = 0f; var height = 0f; for (var i = 0; i < paragraphs; i++) { var p = _renderRoot.Paragraphs[i]; widest = Math.Max(widest, p.Width); height = Math.Max(height, p.Y + p.Height); } _content = new(widest, height); }
            var width = _scroll.GetBoundMinimumSize().X; _scroll.Position = new(Size.X - width - _textRect.Position.X, _textRect.Position.Y); _scroll.Size = new(width, _textRect.Size.Y); _scroll.MinValue = 0; _scroll.MaxValue = Math.Max(_content.Y, _textRect.Size.Y); _scroll.Page = _textRect.Size.Y; _scroll.Step = 1; _scroll.Visible = _scrollActive && _content.Y > _textRect.Size.Y; if (_scroll.Visible) _textRect = new(_textRect.Position, new(Math.Max(1, _textRect.Size.X - width), _textRect.Size.Y));
            if (_scrollFollowing) _scroll.Value = Math.Max(0, _content.Y - _textRect.Size.Y); if (_scrollFollowingVisible && _visibleCharacters >= 0) { var line = FindCharacterLine(_visibleCharacters); if (line >= 0) _scroll.Value = Math.Max(0, _lines[line].Y - _textRect.Size.Y + LoadedLineHeight(_lines[line])); }
            _finished = complete; QueueRedraw(); _notifyFinished = complete; if (_fitContent && !_building) UpdateMinimumSize();
        }
        finally { _publishing = false; }
    }
    private void CountGlyphOffsets(Frame frame, int limit = -1) { for (var i = 0; i < (limit < 0 ? frame.Paragraphs.Count : Math.Min(limit, frame.Paragraphs.Count)); i++) { var paragraph = frame.Paragraphs[i]; foreach (var part in paragraph.Parts) if (part.Table is { } table) foreach (var cell in table.Cells) CountGlyphOffsets(cell.Frame); paragraph.GlyphStart = _totalGlyphs; _totalGlyphs += paragraph.Layout.GlyphCount; } }
    private void CollectLayout(Frame frame, Vector2 origin, bool root = true, int limit = -1)
    { frame.Position = origin; for (var i = 0; i < (limit < 0 ? frame.Paragraphs.Count : Math.Min(limit, frame.Paragraphs.Count)); i++) { var paragraph = frame.Paragraphs[i]; paragraph.Origin = origin; _drawParagraphs.Add(paragraph); if (root) { if (paragraph.Layout.LineCount == 0) _lines.Add((paragraph, -1, origin.Y + paragraph.Y)); for (var line = 0; line < paragraph.Layout.LineCount; line++) _lines.Add((paragraph, line, origin.Y + paragraph.Y + paragraph.Layout.Lines[line].CrossOffset)); } foreach (var part in paragraph.Parts) if (part.Table is { } table) foreach (var cell in table.Cells) CollectLayout(cell.Frame, origin + new Vector2(part.Rect.Position.X + cell.Rect.Position.X + cell.Padding.Position.X, paragraph.Y + part.Rect.Position.Y + cell.Rect.Position.Y + cell.Padding.Position.Y), false); } }
    private Vector2 ShapeFrame(Frame frame, float width, ThemeValues theme, int depth, bool publish = false)
    {
        if (depth > 256) throw new InvalidOperationException("Rich-text table nesting exceeds its layout budget."); var y = 0f; var widest = 0f; var loaded = 0;
        foreach (var paragraph in frame.Paragraphs)
        {
            paragraph.Y = y; paragraph.Objects.Clear(); paragraph.Inline.Clear(); var format = paragraph.Parts.Count > 0 ? paragraph.Parts[0].Format : paragraph.Format; var indent = format.Indent * _tabSize * theme.Fonts[0].GetCharSize(' ', theme.Sizes[0]).X; var available = width <= 0 ? 0 : Math.Max(1, width - indent); var capWidth = 0f; var capHeight = 0f;
            foreach (var part in paragraph.Parts)
            {
                var size = PartSize(part.Format, theme); Vector2 objectSize = default; if (part.Kind == PartKind.Image) { if (part.Texture is not { IsDisposed: false }) continue; var natural = part.Region.Size.X > 0 && part.Region.Size.Y > 0 ? part.Region.Size : part.Texture.GetSize(); float Unit(float n, ImageUnit unit) => unit switch { ImageUnit.Percent => n * available / 100, ImageUnit.EM => n * size, _ => n }; var w = Unit(part.Width, part.WidthUnit); var h = Unit(part.Height, part.HeightUnit); if (w == 0 && h == 0) { w = natural.X; h = natural.Y; } else if (w == 0) w = h * natural.X / Math.Max(1, natural.Y); else if (h == 0) h = w * natural.Y / Math.Max(1, natural.X); objectSize = new(w, h); } else if (part.Kind == PartKind.Rule) objectSize = new(part.WidthPercent ? available * part.Width / 100 : part.Width, part.HeightPercent ? available * part.Height / 100 : part.Height); else if (part.Table is { } table) objectSize = ShapeTable(table, available, theme, depth + 1); else if (part.Kind == PartKind.Dropcap) { capWidth = Math.Max(capWidth, part.DropLayout.Size.X + part.DropMargins.Position.X + part.DropMargins.Size.X); capHeight = Math.Max(capHeight, part.DropLayout.Size.Y + part.DropMargins.Position.Y + part.DropMargins.Size.Y); part.Rect = new(new Vector2(_effectiveDirection == TextDirection.RTL ? Math.Max(0, available - capWidth) + part.DropMargins.Position.X : part.DropMargins.Position.X, part.DropMargins.Position.Y), part.DropLayout.Size); continue; } else continue;
                var index = paragraph.Inline.Count; paragraph.Inline[index] = part; paragraph.Objects.Add(new(part.Local, objectSize, part.Alignment, index, part.Table?.Baseline ?? -1)); part.Rect = new(Vector2.Zero, objectSize);
            }
            var breaks = TextLineBreakFlags.Mandatory | _trim | (_autowrap switch { TextAutowrapMode.Word => TextLineBreakFlags.WordBound, TextAutowrapMode.Arbitrary => TextLineBreakFlags.GraphemeBound, TextAutowrapMode.WordSmart => TextLineBreakFlags.WordBound | TextLineBreakFlags.Adaptive, _ => 0 }); var alignment = paragraph.Alignment ?? _horizontal; var direction = paragraph.Direction ?? _direction; if (direction == TextDirection.Inherited) direction = _effectiveDirection;
            var stops = paragraph.Tabs.Length > 0 ? paragraph.Tabs : _tabs; if (stops.Length == 0) { paragraph.DefaultTabs[0] = theme.Fonts[0].GetCharSize(' ', theme.Sizes[0]).X * _tabSize; stops = paragraph.DefaultTabs; }
            var reveal = _visibleBehavior == TextVisibleCharactersBehavior.CharsBeforeShaping && _visibleCharacters >= 0 ? LocalBudget(paragraph, _visibleCharacters) : -1;
            paragraph.Layout.Build(theme.Fonts[0], new(paragraph.Text, theme.Sizes[0], available, alignment, -1, breaks, paragraph.Alignment.HasValue ? paragraph.Justification : _justification, direction, TextOrientation.Horizontal, true), new(LineSpacing: theme.LineSpacing, Language: paragraph.Language.Length > 0 ? paragraph.Language : _language, TabStops: stops, VisibleCharacters: reveal, VisibleBehavior: 0, BIDIOverride: paragraph.Contexts, Styles: paragraph.Spans, Objects: paragraph.Objects, DropcapWidth: capWidth, DropcapHeight: capHeight));
            paragraph.Width = paragraph.Layout.Size.X + indent; paragraph.Height = Math.Max(paragraph.Layout.Size.Y, capHeight); if (paragraph.Layout.LineCount == 0) paragraph.Height = Math.Max(paragraph.Height, theme.Fonts[0].GetHeight(theme.Sizes[0]));
            foreach (var inline in paragraph.Inline) { var part = inline.Value; var bounds = paragraph.Layout.GetCharacterBounds(part.Local); var ascent = part.Rect.Size.Y; var metrics = PartFont(part.Format, theme); var fontSize = PartSize(part.Format, theme); var anchor = ((int)part.Alignment & 12) switch { 0 => -metrics.GetAscent(fontSize), 4 => (metrics.GetDescent(fontSize) - metrics.GetAscent(fontSize)) / 2, 8 => 0, _ => metrics.GetDescent(fontSize) }; var own = ((int)part.Alignment & 3) switch { 0 => 0, 1 => ascent / 2, _ => part.Table?.Baseline ?? ascent }; var lineIndex = 0; for (var n = 0; n < paragraph.Layout.Lines.Count; n++) if (part.Local >= paragraph.Layout.Lines[n].Start && part.Local < paragraph.Layout.Lines[n].End) { lineIndex = n; break; } var baseline = paragraph.Layout.Lines.Count > 0 ? paragraph.Layout.Lines[lineIndex].CrossOffset + paragraph.Layout.Lines[lineIndex].Ascent : 0; part.Rect = new(new(bounds.Position.X + indent, baseline + anchor - own), part.Rect.Size); }
            widest = Math.Max(widest, paragraph.Width); y += paragraph.Height + theme.ParagraphSpacing; if (publish) Volatile.Write(ref _workerReadyParagraphs, ++loaded);
        }
        frame.Size = new(widest, Math.Max(0, y - (frame.Paragraphs.Count > 0 ? theme.ParagraphSpacing : 0))); return frame.Size;
    }
    private Vector2 ShapeTable(Table table, float width, ThemeValues theme, int depth)
    {
        table.Baseline = 0; var columns = table.Columns; foreach (var column in columns) { column.Width = 0; column.Minimum = 0; }
        for (var i = 0; i < table.Cells.Count; i++) { var cell = table.Cells[i]; var natural = ShapeFrame(cell.Frame, 0, theme, depth); var minimum = Math.Max(cell.Minimum.X, natural.X + cell.Padding.Position.X + cell.Padding.Size.X); if (cell.Maximum.X > 0) minimum = Math.Min(minimum, cell.Maximum.X); columns[i % columns.Length].Width = Math.Max(columns[i % columns.Length].Width, minimum); columns[i % columns.Length].Minimum = Math.Max(columns[i % columns.Length].Minimum, cell.Minimum.X + cell.Padding.Position.X + cell.Padding.Size.X); }
        var gaps = Math.Max(0, columns.Length - 1) * theme.HSeparation; var used = (float)gaps; foreach (var column in columns) used += column.Width; var extra = width - used;
        if (width > 0 && extra > 0) { var weight = 0d; foreach (var column in columns) if (column.Expand) weight += column.Ratio; if (weight > 0) foreach (var column in columns) if (column.Expand) column.Width += (float)(extra * column.Ratio / weight); }
        else if (width > 0 && extra < 0) { var shrink = 0; foreach (var column in columns) if (column.Shrink) shrink++; if (shrink > 0) foreach (var column in columns) if (column.Shrink) column.Width = Math.Max(Math.Max(1, column.Minimum), column.Width + extra / shrink); }
        var y = 0f; for (var row = 0; row * columns.Length < table.Cells.Count; row++) { var height = 0f; var rowBaseline = 0f; var x = 0f; for (var column = 0; column < columns.Length; column++) { var index = row * columns.Length + column; if (index >= table.Cells.Count) break; var cell = table.Cells[index]; var available = Math.Max(1, columns[column].Width - cell.Padding.Position.X - cell.Padding.Size.X); var content = ShapeFrame(cell.Frame, available, theme, depth); var h = Math.Max(cell.Minimum.Y, content.Y + cell.Padding.Position.Y + cell.Padding.Size.Y); if (cell.Maximum.Y > 0) h = Math.Min(h, cell.Maximum.Y); height = Math.Max(height, h); var last = cell.Frame.Paragraphs[^1]; var lines = last.Layout.Lines; rowBaseline = Math.Max(rowBaseline, cell.Padding.Position.Y + last.Y + (lines.Count > 0 ? lines[^1].CrossOffset + lines[^1].Ascent : 0)); cell.Rect = new(x, y, columns[column].Width, h); x += columns[column].Width + theme.HSeparation; } for (var column = 0; column < columns.Length; column++) { var index = row * columns.Length + column; if (index < table.Cells.Count) { var cell = table.Cells[index]; cell.Rect = new(cell.Rect.Position, new(cell.Rect.Size.X, height)); } } if (table.AlignRow < 0 || table.AlignRow == row) table.Baseline = y + rowBaseline; y += height + theme.VSeparation; }
        var tableWidth = (float)gaps; foreach (var column in columns) tableWidth += column.Width; table.Size = new(tableWidth, Math.Max(0, y - (table.Cells.Count > 0 ? theme.VSeparation : 0))); return table.Size;
    }
    private static int LocalBudget(Paragraph paragraph, int global) { var at = 0; while (at < paragraph.Styles.Length && paragraph.Offsets[at + 1] <= global) at++; return at; }
    private static int CountFrame(Frame frame) { var count = Math.Max(0, frame.Paragraphs.Count - 1); foreach (var paragraph in frame.Paragraphs) foreach (var part in paragraph.Parts) { if (part.Kind == PartKind.Text) count += CountScalars(part.Text.AsSpan()); else if (part.Kind == PartKind.Image) count++; else if (part.Table is { } table) foreach (var cell in table.Cells) count += CountFrame(cell.Frame); } return count; }
    private int FindCharacterLine(int character)
    {
        var previous = -1; for (var i = 0; i < _lines.Count; i++) { var entry = _lines[i]; var paragraph = entry.Paragraph; if (character < paragraph.Start || character >= paragraph.Start + paragraph.Count) continue; if (entry.Line < 0) return i; var line = paragraph.Layout.Lines[entry.Line]; var start = paragraph.Offsets[Math.Min(line.Start, paragraph.Styles.Length)]; var end = paragraph.Offsets[Math.Min(line.End, paragraph.Styles.Length)]; if (character < start) return previous >= 0 ? previous : i; if (character < end) return i; previous = i; }
        return previous;
    }
}
