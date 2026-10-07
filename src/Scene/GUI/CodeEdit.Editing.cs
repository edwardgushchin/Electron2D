using System.Text;
namespace Electron2D;

public partial class CodeEdit
{
    private string IndentText(int column) => _useSpaces ? new(' ', GetTabSize() - column % GetTabSize()) : "\t";
    private void EditGroup(Action edit, bool requireEditable = true) { MutableCode(); if (requireEditable && !Editable) return; BeginComplexOperation(); BeginMulticaretEdit(); try { edit(); } finally { EndMulticaretEdit(); EndComplexOperation(); } }
    private int[] CaretLines() => GetLineRangesFromCarets().SelectMany(r => Enumerable.Range(r.X, r.Y - r.X + 1)).Distinct().Order().ToArray();
    /// <summary>Indents selections, or inserts indentation at each caret.</summary>
    public void DoIndent() { if (HasSelection()) { IndentLines(); return; } EditGroup(() => { foreach (var caret in GetSortedCarets().Reverse()) if (!MulticaretEditIgnoreCaret(caret)) InsertTextAtCaret(IndentText(GetCaretColumn(caret)), caret); }); }
    /// <summary>Indents each selected/caret line once, omitting empty lines.</summary>
    public void IndentLines() => EditGroup(() => { foreach (var line in CaretLines()) if (GetLine(line).Length > 0) InsertText(IndentText(GetFirstNonWhitespaceColumn(line)), line, 0, false); });
    /// <summary>Removes one indentation stop from each selected/caret line.</summary>
    public void UnindentLines() => EditGroup(() => { foreach (var line in CaretLines()) { var text = GetLine(line); if (text.StartsWith('\t')) RemoveText(line, 0, line, 1); else { var spaces = 0; while (spaces < text.Length && text[spaces] == ' ') spaces++; if (spaces > 0) { var remove = spaces % GetTabSize(); if (remove == 0) remove = GetTabSize(); RemoveText(line, 0, line, Math.Min(spaces, remove)); } } } });
    /// <summary>Converts leading indentation of a logical-line interval to the configured tab/space policy.</summary><param name="fromLine">Existing start or -1 for document start.</param><param name="toLine">Existing inclusive end or -1 for document end.</param>
    public void ConvertIndent(int fromLine = -1, int toLine = -1)
    { MutableCode(); if (fromLine == -1) fromLine = 0; if (toLine == -1) toLine = GetLineCount() - 1; RequireLine(fromLine); RequireLine(toLine); if (toLine < fromLine) throw new ArgumentException("The interval is reversed."); EditGroup(() => { for (var line = fromLine; line <= toLine; line++) { if (IsInString(line) != -1) continue; var count = GetFirstNonWhitespaceColumn(line); if (count == 0) continue; var width = GetIndentLevel(line); var indent = _useSpaces ? new string(' ', width) : new string('\t', width / GetTabSize()) + new string(' ', width % GetTabSize()); RemoveText(line, 0, line, count); InsertText(indent, line, 0, false); } }); }
    /// <summary>Adds a unique symbol-key brace pair.</summary><param name="startKey">Nonempty symbols.</param><param name="endKey">Nonempty symbols.</param>
    public void AddAutoBraceCompletionPair(string startKey, string endKey) { MutableCode(); RequireSymbols(startKey, false); RequireSymbols(endKey, false); if (!_bracePairs.TryAdd(startKey, endKey)) throw new ArgumentException("An opening key already exists.", nameof(startKey)); DirtyCode(); }
    /// <summary>Reports an opening brace key.</summary><param name="openKey">Exact key.</param><returns>Whether present.</returns>
    public bool HasAutoBraceCompletionOpenKey(string openKey) { CheckCode(); ArgumentNullException.ThrowIfNull(openKey); return _bracePairs.ContainsKey(openKey); }
    /// <summary>Reports a closing brace key.</summary><param name="closeKey">Exact key.</param><returns>Whether present.</returns>
    public bool HasAutoBraceCompletionCloseKey(string closeKey) { CheckCode(); ArgumentNullException.ThrowIfNull(closeKey); return _bracePairs.ContainsValue(closeKey); }
    /// <summary>Returns a matching closing key or an empty string.</summary><param name="openKey">Exact key.</param><returns>Closing key.</returns>
    public string GetAutoBraceCompletionCloseKey(string openKey) { CheckCode(); ArgumentNullException.ThrowIfNull(openKey); return _bracePairs.GetValueOrDefault(openKey, ""); }
    private void NewCodeLine(bool split = true, bool above = false)
    {
        EditGroup(() =>
        {
            foreach (var caret in GetSortedCarets().Reverse())
            {
                if (MulticaretEditIgnoreCaret(caret)) continue;
                UnhideCaret(caret); var line = GetCaretLine(caret); var column = split ? GetCaretColumn(caret) : LengthOf(line); var prefix = Slice(GetLine(line), 0, column); var units = 0; var spaces = 0;
                foreach (var ch in prefix) { if (ch == '\t') { units++; spaces = 0; } else if (ch == ' ') { if (++spaces == GetTabSize()) { units++; spaces = 0; } } else break; }
                var indent = _useSpaces ? new string(' ', units * GetTabSize()) : new string('\t', units); var originalIndent = indent; var effective = prefix;
                EnsureRegions(); foreach (var region in _regions) if (_delimiters[region.Index].Comment && region.Start.Y == line && region.Start.X <= column) { effective = Slice(GetLine(line), 0, region.Start.X - 1); break; }
                var trim = effective.TrimEnd(); var expand = _autoIndent && !above && trim.Length > 0 && _indentPrefixes.Any(p => trim.EndsWith(p, StringComparison.Ordinal)); if (expand) indent += _useSpaces ? new string(' ', GetTabSize()) : "\t";
                var extra = ""; if (split && expand && column < LengthOf(line)) foreach (var pair in _bracePairs) if (trim.EndsWith(pair.Key, StringComparison.Ordinal) && GetLine(line).AsSpan(ScalarIndex(GetLine(line), column)).StartsWith(pair.Value, StringComparison.Ordinal)) { extra = "\n" + originalIndent; break; }
                if (split) { InsertTextAtCaret("\n" + indent + extra, caret); if (extra.Length > 0) { SetCaretLine(line + 1, false, true, 0, caret); SetCaretColumn(CountScalars(indent.AsSpan()), false, caret); } }
                else { if (above) InsertLineAt(line, indent); else InsertText("\n" + indent, line, LengthOf(line), false); Deselect(caret); SetCaretLine(above ? line : line + 1, false, true, 0, caret); SetCaretColumn(CountScalars(indent.AsSpan()), false, caret); }
            }
        });
    }
    private void UnhideCaret(int caret) { var line = GetCaretLine(caret); while (IsCodeLineHidden(line)) { var before = _folded.Count; UnfoldLine(line); if (_folded.Count == before) break; } if (_folded.Contains(line)) UnfoldLine(line); }
    /// <inheritdoc />
    protected override void OnHandleUnicodeInput(int unicodeChar, int caretIndex)
    {
        if (!Rune.IsValid(unicodeChar)) throw new ArgumentOutOfRangeException(nameof(unicodeChar)); if (!Editable) return; var typed = new Rune(unicodeChar).ToString();
        EditGroup(() =>
        {
            var order = caretIndex >= 0 ? new[] { caretIndex } : GetSortedCarets().Reverse().ToArray(); foreach (var caret in order)
            {
                if (MulticaretEditIgnoreCaret(caret)) continue; UnhideCaret(caret); var line = GetCaretLine(caret); var column = GetCaretColumn(caret); var text = GetLine(line);
                if (_autoPairs && IsInComment(line, column) == -1 && !HasSelection(caret)) { var skip = _bracePairs.Values.Where(close => close.StartsWith(typed, StringComparison.Ordinal) && text.AsSpan(ScalarIndex(text, column)).StartsWith(close, StringComparison.Ordinal)).OrderByDescending(close => close.Length).FirstOrDefault(); if (skip != null) { SetCaretColumn(column + CountScalars(skip.AsSpan()), false, caret); continue; } }
                string? open = null; string? close = null; if (_autoPairs && IsInComment(line, column) == -1) { foreach (var pair in _bracePairs.OrderByDescending(p => CountScalars(p.Key.AsSpan()))) if ((Slice(text, 0, column) + typed).EndsWith(pair.Key, StringComparison.Ordinal)) { open = pair.Key; close = pair.Value; break; } }
                if (close == null || IsInString(line, column) != -1 && HasStringDelimiter(typed)) { InsertTextAtCaret(typed, caret); continue; }
                if (HasSelection(caret)) { var fromLine = GetSelectionFromLine(caret); var fromCol = GetSelectionFromColumn(caret); var toLine = GetSelectionToLine(caret); var toCol = GetSelectionToColumn(caret); var selection = GetSelectedText(caret); InsertTextAtCaret(typed + selection + close, caret); Select(fromLine, fromCol + 1, toLine, toCol + (fromLine == toLine ? 1 : 0), caret); }
                else { InsertTextAtCaret(typed + close, caret); SetCaretColumn(GetCaretColumn(caret) - CountScalars(close.AsSpan()), false, caret); }
            }
        });
    }
    /// <inheritdoc />
    protected override void OnTextInput(string text) { if (!Editable || !HasFocus()) return; CancelIME(); BeginComplexOperation(); try { foreach (var rune in text.EnumerateRunes()) { if (rune.Value == '\n') NewCodeLine(); else OnHandleUnicodeInput(rune.Value, -1); } } finally { EndComplexOperation(); } if (_completionEnabled) RequestCodeCompletion(); }
    /// <inheritdoc />
    protected override void OnBackspace(int caretIndex)
    {
        if (!Editable) return; EditGroup(() =>
        {
            var order = caretIndex >= 0 ? new[] { caretIndex } : GetSortedCarets().Reverse().ToArray(); foreach (var caret in order)
            {
                UnhideCaret(caret); if (HasSelection(caret)) { base.OnBackspace(caret); continue; }
                var line = GetCaretLine(caret); var column = GetCaretColumn(caret); var text = GetLine(line); var pairFound = false;
                if (_autoPairs) foreach (var pair in _bracePairs.OrderByDescending(pair => pair.Key.Length)) if (Slice(text, 0, column).EndsWith(pair.Key, StringComparison.Ordinal) && Slice(text, column, LengthOf(line)).StartsWith(pair.Value, StringComparison.Ordinal)) { RemoveText(line, column - CountScalars(pair.Key.AsSpan()), line, column + CountScalars(pair.Value.AsSpan())); pairFound = true; break; }
                if (pairFound) continue; if (column > 0 && column <= GetFirstNonWhitespaceColumn(line) && text[ScalarIndex(text, column - 1)] == ' ') { var count = column % GetTabSize(); if (count == 0) count = GetTabSize(); RemoveText(line, Math.Max(0, column - count), line, column); } else base.OnBackspace(caret);
            }
        });
    }
    /// <inheritdoc />
    protected override void OnCut(int caretIndex) { if (caretIndex >= 0) UnhideCaret(caretIndex); else foreach (var caret in GetSortedCarets()) UnhideCaret(caret); base.OnCut(caretIndex); }
    /// <inheritdoc />
    protected override void OnPaste(int caretIndex) { if (caretIndex >= 0) UnhideCaret(caretIndex); else foreach (var caret in GetSortedCarets()) UnhideCaret(caret); base.OnPaste(caretIndex); }
    /// <summary>Deletes each selected/caret logical line once.</summary>
    public void DeleteLines() => EditGroup(() => { foreach (var line in CaretLines().Reverse()) RemoveLineAt(line); }, false);
    /// <summary>Duplicates complete selected/caret lines and advances their caret identities.</summary>
    public void DuplicateLines() => EditGroup(() => { foreach (var range in GetLineRangesFromCarets().Reverse()) { var text = string.Join('\n', Enumerable.Range(range.X, range.Y - range.X + 1).Select(GetLine)); InsertText(text + "\n", range.X, 0); } }, false);
    /// <summary>Duplicates selected text, or complete lines for unselected carets.</summary>
    public void DuplicateSelection() => EditGroup(() => { foreach (var caret in GetSortedCarets().Reverse()) { UnhideCaret(caret); if (HasSelection(caret)) InsertText(GetSelectedText(caret), GetSelectionFromLine(caret), GetSelectionFromColumn(caret)); else InsertText(GetLine(GetCaretLine(caret)) + "\n", GetCaretLine(caret), 0); } }, false);
    /// <summary>Moves selected/caret line ranges upward with their gutters and backgrounds.</summary>
    public void MoveLinesUp() => EditGroup(() => { foreach (var range in GetLineRangesFromCarets()) { if (range.X == 0) continue; for (var line = range.X; line <= range.Y; line++) SwapLines(line, line - 1); } }, false);
    /// <summary>Moves selected/caret line ranges downward with their gutters and backgrounds.</summary>
    public void MoveLinesDown() => EditGroup(() => { foreach (var range in GetLineRangesFromCarets().Reverse()) { if (range.Y + 1 >= GetLineCount()) continue; for (var line = range.Y; line >= range.X; line--) SwapLines(line, line + 1); } }, false);
    /// <summary>Joins selected/caret lines with following lines, trimming connecting whitespace.</summary><param name="lineEnding">Nonnull inserted separator.</param>
    public void JoinLines(string lineEnding = " ") { ArgumentNullException.ThrowIfNull(lineEnding); if (lineEnding.Contains('\n')) throw new ArgumentException("A join separator cannot contain LF.", nameof(lineEnding)); EditGroup(() => { foreach (var range in GetLineRangesFromCarets().Reverse()) { var end = Math.Min(GetLineCount() - 1, range.Y + 1); if (end <= range.X) continue; var first = GetLine(range.X).TrimEnd(); var rest = Enumerable.Range(range.X + 1, end - range.X).Select(i => GetLine(i).Trim()); var joined = first; foreach (var part in rest) if (part.Length > 0) joined += lineEnding + part; RemoveText(range.X, 0, end, LengthOf(end)); InsertText(joined, range.X, 0, false); } }, false); }
    /// <summary>Wraps selected lines in named comment region tags, folds them and selects the new region name.</summary>
    public void CreateCodeRegion()
    { MutableCode(); if (!HasSelection()) return; var delimiter = _delimiters.FirstOrDefault(d => d.Comment && d.LineOnly); if (delimiter == null) return; var name = Atr("New Code Region"); var first = GetLineRangesFromCarets(true, false)[0].X; var start = delimiter.Start + _regionStart; EditGroup(() => { foreach (var range in GetLineRangesFromCarets(true, false).Reverse()) { InsertText("\n" + delimiter.Start + _regionEnd, range.Y, LengthOf(range.Y), false); InsertLineAt(range.X, start + " " + name); FoldLine(range.X); } RemoveSecondaryCarets(); Select(first, CountScalars(start.AsSpan()) + 1, first, CountScalars((start + " " + name).AsSpan())); }, false); }
    /// <summary>Returns the complete text with U+FFFF at a logical/scalar coordinate.</summary><param name="line">Existing line.</param><param name="column">Scalar column.</param><returns>Cursor-marked text.</returns>
    public string GetTextWithCursorChar(int line, int column) { RequirePosition(line, column); var lines = Enumerable.Range(0, GetLineCount()).Select(GetLine).ToArray(); var at = ScalarIndex(lines[line], column); lines[line] = lines[line].Insert(at, "\uffff"); return string.Join('\n', lines); }
    /// <summary>Returns cursor-marked text at the primary caret.</summary><returns>Source with U+FFFF.</returns>
    public string GetTextForCodeCompletion() => GetTextWithCursorChar(GetCaretLine(), GetCaretColumn());
    /// <summary>Returns cursor-marked text at the current symbol-lookup position.</summary><returns>Source with U+FFFF.</returns>
    public string GetTextForSymbolLookup() => GetTextWithCursorChar(Math.Clamp(_symbolPosition.Y, 0, GetLineCount() - 1), Math.Clamp(_symbolPosition.X, 0, LengthOf(Math.Clamp(_symbolPosition.Y, 0, GetLineCount() - 1))));
}
