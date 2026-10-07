using System.Text;
namespace Electron2D;

public partial class RichTextLabel
{
    /// <summary>Gets raw parsed text, including drop caps, image spaces and indentation markers.</summary><returns>Markup-free document text.</returns>
    public string GetParsedText() { CheckRich(); var text = new StringBuilder(); AppendPlain(_main, text, true); return text.ToString(); }
    private static void AppendPlain(Frame frame, StringBuilder text, bool caps) { for (var p = 0; p < frame.Paragraphs.Count; p++) { foreach (var part in frame.Paragraphs[p].Parts) { if (part.Kind == PartKind.Text || caps && part.Kind == PartKind.Dropcap) text.Append(part.Text); else if (caps && part.Kind == PartKind.Marker) text.Append('\t'); else if (part.Kind == PartKind.Image) text.Append(' '); else if (part.Table is { } table) foreach (var cell in table.Cells) AppendPlain(cell.Frame, text, caps); } if (p + 1 < frame.Paragraphs.Count) text.Append('\n'); } }
    private string SelectionPlain() { var text = new StringBuilder(); AppendPlain(_main, text, false); return text.ToString(); }
    /// <summary>Gets scalar document count excluding drop caps and structural tags.</summary><returns>Text, newline and image count.</returns>
    public int GetTotalCharacterCount() { CheckRich(); return CountFrame(_main); }
    /// <summary>Gets root paragraph count.</summary><returns>At least one paragraph.</returns>
    public int GetParagraphCount() { CheckRich(); return _main.Paragraphs.Count; }
    /// <summary>Gets the number of shaped lines in root paragraphs.</summary><returns>Loaded root line count. Cell lines contribute to table height.</returns>
    public int GetLineCount() { EnsureRichLayout(); return _lines.Count; }
    private int LoadedParagraphCount => _threaded && !_finished ? _workerPublishedParagraphs : _renderRoot.Paragraphs.Count;
    private float LoadedLineHeight((Paragraph Paragraph, int Line, float Y) entry) => entry.Line < 0 ? entry.Paragraph.Height : entry.Paragraph.Layout.Lines[entry.Line].Height;
    private (Paragraph Paragraph, int Line, float Y) LineAt(int line) { EnsureRichLayout(); if ((uint)line >= _lines.Count) throw new ArgumentOutOfRangeException(nameof(line)); return _lines[line]; }
    /// <summary>Gets one loaded line's pixel height.</summary><param name="line">Existing line.</param><returns>Ceiling height.</returns>
    public int GetLineHeight(int line) { var entry = LineAt(line); return (int)Math.Ceiling(LoadedLineHeight(entry)); }
    /// <summary>Gets one loaded line's width.</summary><param name="line">Existing line.</param><returns>Ceiling width.</returns>
    public int GetLineWidth(int line) { var entry = LineAt(line); return (int)Math.Ceiling(entry.Line < 0 ? 0 : entry.Paragraph.Layout.Lines[entry.Line].Width); }
    /// <summary>Gets a loaded line's document Y offset.</summary><param name="line">Existing line.</param><returns>Pixel offset.</returns>
    public float GetLineOffset(int line) => LineAt(line).Y;
    /// <summary>Gets a loaded line's absolute scalar interval.</summary><param name="line">Existing line.</param><returns>Start and exclusive end.</returns>
    public Vector2i GetLineRange(int line) { var entry = LineAt(line); if (entry.Line < 0) return new(entry.Paragraph.Start, entry.Paragraph.Start); var range = entry.Paragraph.Layout.Lines[entry.Line]; return new(entry.Paragraph.Offsets[Math.Min(range.Start, entry.Paragraph.Styles.Length)], entry.Paragraph.Offsets[Math.Min(range.End, entry.Paragraph.Styles.Length)]); }
    /// <summary>Gets the loaded line containing a scalar position.</summary><param name="character">Nonnegative scalar offset.</param><returns>Line or -1 if not loaded.</returns>
    public int GetCharacterLine(int character) { EnsureRichLayout(); if (character < 0) throw new ArgumentOutOfRangeException(nameof(character)); return FindCharacterLine(character); }
    /// <summary>Gets the root paragraph containing a scalar position.</summary><param name="character">Nonnegative scalar offset.</param><returns>Paragraph or -1.</returns>
    public int GetCharacterParagraph(int character) { EnsureRichLayout(); if (character < 0) throw new ArgumentOutOfRangeException(nameof(character)); for (var p = 0; p < LoadedParagraphCount; p++) { var item = _renderRoot.Paragraphs[p]; if (character >= item.Start && character < item.Start + item.Count) return p; } return -1; }
    /// <summary>Gets one root paragraph's Y offset.</summary><param name="paragraph">Existing root paragraph.</param><returns>Pixel offset.</returns>
    public float GetParagraphOffset(int paragraph) { EnsureRichLayout(); if ((uint)paragraph >= _renderRoot.Paragraphs.Count) throw new ArgumentOutOfRangeException(nameof(paragraph)); return paragraph < LoadedParagraphCount ? _renderRoot.Paragraphs[paragraph].Y : 0; }
    /// <summary>Gets total loaded content height.</summary><returns>Ceiling pixels.</returns>
    public int GetContentHeight() { EnsureRichLayout(); return (int)Math.Ceiling(_content.Y); }
    /// <summary>Gets total loaded content width.</summary><returns>Ceiling pixels.</returns>
    public int GetContentWidth() { EnsureRichLayout(); return (int)Math.Ceiling(_content.X); }
    /// <summary>Gets the content viewport rectangle excluding padding and the visible scrollbar.</summary><returns>Local integer rectangle.</returns>
    public Rect2i GetVisibleContentRect() { EnsureRichLayout(); return new((Vector2i)_textRect.Position, (Vector2i)_textRect.Size); }
    /// <summary>Gets the number of lines intersecting the current visible viewport.</summary><returns>Zero for a locally hidden label.</returns>
    public int GetVisibleLineCount() { EnsureRichLayout(); if (!Visible) return 0; var top = (float)_scroll.Value; var count = 0; foreach (var line in _lines) if (line.Y + LoadedLineHeight(line) > top && line.Y < top + _textRect.Size.Y) count++; return count; }
    /// <summary>Gets the number of root paragraphs intersecting the viewport.</summary><returns>Zero for a locally hidden label.</returns>
    public int GetVisibleParagraphCount() { EnsureRichLayout(); if (!Visible) return 0; var top = (float)_scroll.Value; var count = 0; for (var p = 0; p < LoadedParagraphCount; p++) { var paragraph = _renderRoot.Paragraphs[p]; if (paragraph.Y + paragraph.Height > top && paragraph.Y < top + _textRect.Size.Y) count++; } return count; }
    /// <summary>Gets completion of the current asynchronous layout.</summary><returns>True when the current version is loaded.</returns>
    public bool IsFinished() { EnsureRichLayout(); return _finished; }
    /// <summary>Invalidates one root paragraph's cached layout.</summary><param name="paragraph">Root index.</param><returns>False for an invalid index.</returns>
    public bool InvalidateParagraph(int paragraph) { MutableRich(); if ((uint)paragraph >= _main.Paragraphs.Count) return false; Changed(); PruneOwnedState(); return true; }
    /// <summary>Removes one root paragraph and its children.</summary><param name="paragraph">Root index.</param><param name="noInvalidate">Defers shaping invalidation until an explicit invalidation.</param><returns>False for an invalid index.</returns>
    public bool RemoveParagraph(int paragraph, bool noInvalidate = false) { MutableRich(); if ((uint)paragraph >= _main.Paragraphs.Count) return false; _main.Paragraphs.RemoveAt(paragraph); if (_main.Paragraphs.Count == 0) _main.Paragraphs.Add(new()); if (!ContainsFrame(_main, _frame)) { _frame = _main; _table = null; _stack.Clear(); _format = new(); } if (!noInvalidate) { Changed(); PruneOwnedState(); } return true; }
    private static bool ContainsFrame(Frame root, Frame target) { if (ReferenceEquals(root, target)) return true; foreach (var paragraph in root.Paragraphs) foreach (var part in paragraph.Parts) if (part.Table is { } table) foreach (var cell in table.Cells) if (ContainsFrame(cell.Frame, target)) return true; return false; }
    /// <summary>Gets the borrowed required vertical scrollbar.</summary><returns>Owned child.</returns>
    public VScrollBar GetVScrollBar() { CheckRich(); return _scroll; }
    /// <summary>Gets the borrowed required context menu.</summary><returns>Owned child.</returns>
    public PopupMenu GetMenu() { CheckRich(); return _menu; }
    /// <summary>Gets whether the context menu is visible.</summary><returns>Popup visibility.</returns>
    public bool IsMenuVisible() { CheckRich(); return _menu.Visible; }
    /// <summary>Scrolls to a shaped line.</summary><param name="line">Existing line.</param>
    public void ScrollToLine(int line) { MutableRich(); EnsureRichLayout(); _scroll.Value = line <= 0 ? 0 : line >= _lines.Count ? _scroll.MaxValue : GetLineOffset(line); }
    /// <summary>Scrolls to a root paragraph.</summary><param name="paragraph">Existing paragraph.</param>
    public void ScrollToParagraph(int paragraph) { MutableRich(); EnsureRichLayout(); _scroll.Value = paragraph <= 0 ? 0 : paragraph >= _renderRoot.Paragraphs.Count ? _scroll.MaxValue : GetParagraphOffset(paragraph); }
    /// <summary>Scrolls to the first selected scalar.</summary>
    public void ScrollToSelection() { MutableRich(); if (_selectionFrom >= 0) { var line = GetCharacterLine(_selectionFrom); if (line >= 0) ScrollToLine(line); } }
    /// <summary>Clears selection.</summary>
    public void Deselect() { CheckRich(); _selectionFrom = _selectionTo = -1; _selecting = false; QueueRedraw(); }
    /// <summary>Selects the complete document when selection is enabled.</summary>
    public void SelectAll() { MutableRich(); if (!_selectionEnabled) return; _selectionFrom = 0; _selectionTo = GetTotalCharacterCount(); QueueRedraw(); }
    /// <summary>Gets the selected absolute scalar start.</summary><returns>-1 without selection.</returns>
    public int GetSelectionFrom() { CheckRich(); return _selectionFrom; }
    /// <summary>Gets the selected exclusive scalar end.</summary><returns>-1 without selection.</returns>
    public int GetSelectionTo() { CheckRich(); return _selectionTo; }
    /// <summary>Gets the selected plain text.</summary><returns>Empty without selection.</returns>
    public string GetSelectedText() { CheckRich(); if (_selectionFrom < 0 || _selectionTo <= _selectionFrom) return ""; var text = SelectionPlain(); return SliceScalars(text, _selectionFrom, _selectionTo); }
    /// <summary>Gets the selected line's Y offset.</summary><returns>Zero without selection.</returns>
    public float GetSelectionLineOffset() { EnsureRichLayout(); var line = _selectionFrom < 0 ? -1 : FindCharacterLine(_selectionFrom); return line < 0 ? 0 : _lines[line].Y; }
    private static string SliceScalars(string text, int from, int to) { var start = 0; var end = text.Length; var at = 0; var scalar = 0; foreach (var rune in text.EnumerateRunes()) { if (scalar == from) start = at; if (scalar == to) { end = at; break; } at += rune.Utf16SequenceLength; scalar++; } if (from >= scalar && at == text.Length) start = text.Length; return text[start..Math.Max(start, end)]; }
}
