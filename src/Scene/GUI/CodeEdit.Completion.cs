using System.Text;
namespace Electron2D;

public partial class CodeEdit
{
    private readonly List<CodeCompletionOption> _submitted = [], _sources = [], _options = [];
    private readonly List<TextLayout> _optionLayouts = [];
    private readonly TextLayout _hintLayout = new();
    private bool _completionActive, _completionForced, _filtering, _refilter, _filterCanceled, _hintBelow = true;
    private int _selectedOption, _optionOffset;
    private string _completionBase = "", _hint = "";
    private Rect2 _completionRect, _hintRect;
    private float _optionHeight, _completionWidth;
    /// <summary>Submits a typed completion candidate without a default value.</summary><param name="type">Candidate kind.</param><param name="displayText">Menu text.</param><param name="insertText">Inserted text.</param><param name="textColor">Finite color or white.</param><param name="icon">Borrowed texture.</param><param name="location">Relative scope location.</param>
    public void AddCodeCompletionOption(CodeCompletionKind type, string displayText, string insertText, Color? textColor = null, Texture? icon = null, int location = (int)CodeCompletionLocation.Other)
    { MutableCode(); _submitted.Add(new(type, displayText, insertText, textColor, icon, location)); }
    /// <summary>Submits a completion candidate with a borrowed exact generic default value.</summary><typeparam name="T">Payload type.</typeparam><param name="type">Candidate kind.</param><param name="displayText">Menu text.</param><param name="insertText">Inserted text.</param><param name="textColor">Finite color or white.</param><param name="icon">Borrowed texture.</param><param name="value">Borrowed typed payload.</param><param name="location">Relative scope location.</param>
    public void AddCodeCompletionOption<T>(CodeCompletionKind type, string displayText, string insertText, Color? textColor = null, Texture? icon = null, T value = default!, int location = (int)CodeCompletionLocation.Other)
    { MutableCode(); _submitted.Add(new CodeCompletionOption(type, displayText, insertText, textColor, icon, location).WithDefaultValue(value)); }
    /// <summary>Replaces the current candidate sources with the submitted queue and filters them.</summary><param name="force">Allows presentation with an empty prefix.</param>
    public void UpdateCodeCompletionOptions(bool force) { MutableCode(); _sources.Clear(); _sources.AddRange(_submitted); _submitted.Clear(); _completionForced = force; FilterCompletion(); }
    /// <summary>Gets a stable borrowed immutable visible candidate.</summary><param name="index">Existing visible index.</param><returns>Candidate.</returns>
    public CodeCompletionOption? GetCodeCompletionOption(int index) { CheckCode(); if (!_completionActive) return null; if ((uint)index >= _options.Count) throw new ArgumentOutOfRangeException(nameof(index)); return _options[index]; }
    /// <summary>Gets an independent array of immutable visible candidates.</summary><returns>Borrowed candidate elements.</returns>
    public CodeCompletionOption[] GetCodeCompletionOptions() { CheckCode(); return _completionActive ? _options.ToArray() : []; }
    /// <summary>Gets the selected visible candidate index.</summary><returns>Index, or -1 when inactive.</returns>
    public int GetCodeCompletionSelectedIndex() { CheckCode(); return _completionActive ? _selectedOption : -1; }
    /// <summary>Changes selection while completion is active.</summary><param name="index">Existing visible candidate index.</param>
    public void SetCodeCompletionSelectedIndex(int index) { MutableCode(); if (!_completionActive) return; if ((uint)index >= _options.Count) throw new ArgumentOutOfRangeException(nameof(index)); _selectedOption = index; FitCompletionSelection(); QueueRedraw(); }
    /// <summary>Cancels the menu and clears visible candidates.</summary>
    public void CancelCodeCompletion() { MutableCode(); if (_filtering) _filterCanceled = true; _completionActive = false; _completionScrollPressed = false; _completionForced = false; _options.Clear(); _completionBase = ""; _optionOffset = 0; QueueRedraw(); }
    private static bool Identifier(Rune rune) => Rune.IsLetterOrDigit(rune) || rune.Value == '_';
    private string CompletionPrefix()
    {
        var line = GetCaretLine(); var column = GetCaretColumn(); var text = GetLine(line); var start = column;
        var region = RegionAt(line, column); if (region != null && !_delimiters[region.Index].Comment && region.Start.Y == line) start = Math.Max(0, region.Start.X - CountScalars(_delimiters[region.Index].Start.AsSpan()));
        else while (start > 0) { var scalar = Rune.GetRuneAt(text, ScalarIndex(text, start - 1)); if (!Identifier(scalar) && scalar.Value != '/') break; start--; }
        return Slice(text, start, column);
    }
    private sealed record CompletionMatch(CodeCompletionOption Option, int[] Positions, int Segments, int CaseErrors);
    private static int CompareMatch(CompletionMatch left, CompletionMatch right)
    {
        var order = left.Segments.CompareTo(right.Segments); if (order != 0) return order;
        if (left.Positions.Length > 0) { order = (left.Positions[0] == 0 ? 0 : 1).CompareTo(right.Positions[0] == 0 ? 0 : 1); if (order != 0) return order; order = left.CaseErrors.CompareTo(right.CaseErrors); if (order != 0) return order; }
        order = left.Option.Location.CompareTo(right.Option.Location); if (order != 0) return order;
        for (var i = 0; i < left.Positions.Length; i++) { order = left.Positions[i].CompareTo(right.Positions[i]); if (order != 0) return order; }
        return NaturalCompletionCompare(left.Option.DisplayText, right.Option.DisplayText);
    }
    private static int NaturalCompletionCompare(string left, string right)
    { var a = 0; var b = 0; while (a < left.Length && b < right.Length) { if (char.IsAsciiDigit(left[a]) && char.IsAsciiDigit(right[b])) { var startA = a; var startB = b; while (a < left.Length && char.IsAsciiDigit(left[a])) a++; while (b < right.Length && char.IsAsciiDigit(right[b])) b++; var significantA = startA; var significantB = startB; while (significantA < a && left[significantA] == '0') significantA++; while (significantB < b && right[significantB] == '0') significantB++; var order = (a - significantA).CompareTo(b - significantB); if (order == 0) order = left.AsSpan(significantA, a - significantA).SequenceCompareTo(right.AsSpan(significantB, b - significantB)); if (order != 0) return order; continue; } var comparison = char.ToUpperInvariant(left[a]).CompareTo(char.ToUpperInvariant(right[b])); if (comparison != 0) return comparison; a++; b++; } return (left.Length - a).CompareTo(right.Length - b); }
    private static CompletionMatch? MatchCompletion(CodeCompletionOption option, Rune[] prefix)
    {
        if (prefix.Length == 0) return new(option, [], 0, 0); var runes = option.DisplayText.EnumerateRunes().ToArray(); var previous = new Dictionary<int, CompletionMatch>();
        // ponytail: cold O(prefix * display squared) matching; a prefix-minimum table can replace the inner scan for very long candidate lists.
        for (var part = 0; part < prefix.Length; part++) { var next = new Dictionary<int, CompletionMatch>(); for (var at = part; at < runes.Length; at++) { if (Rune.ToUpperInvariant(runes[at]) != Rune.ToUpperInvariant(prefix[part])) continue; var mismatch = runes[at] == prefix[part] ? 0 : 1; if (part == 0) { next[at] = new(option, [at], 1, mismatch); continue; } foreach (var entry in previous) { if (entry.Key >= at) continue; var positions = new int[part + 1]; entry.Value.Positions.CopyTo(positions, 0); positions[part] = at; var match = new CompletionMatch(option, positions, entry.Value.Segments + (entry.Key + 1 == at ? 0 : 1), entry.Value.CaseErrors + mismatch); if (!next.TryGetValue(at, out var best) || CompareMatch(match, best) < 0) next[at] = match; } } previous = next; if (previous.Count == 0) return null; }
        return previous.Values.MinBy(m => m, Comparer<CompletionMatch>.Create(CompareMatch));
    }
    /// <summary>Filters application candidates into their visible order. An override owns the complete filter and insertion prefix is empty.</summary><param name="candidates">Independent array of borrowed immutable candidates.</param><returns>Non-null candidate array without null elements.</returns>
    protected virtual CodeCompletionOption[] OnFilterCodeCompletionCandidates(CodeCompletionOption[] candidates)
    {
        var prefix = _completionBase.EnumerateRunes().ToArray(); var region = RegionAt(GetCaretLine(), GetCaretColumn()); var quote = region != null && !_delimiters[region.Index].Comment ? (_delimiters[region.Index].Start == "'" ? "'" : "\"") : null;
        var matches = new List<CompletionMatch>(); foreach (var original in candidates) { var option = original; if (quote != null) { string Normalize(string value) { var literal = value.Length > 2 && value[1] is '\"' or '\'' ? value[..1] : ""; var text = value[literal.Length..]; if (text.Length >= 2 && text[0] is '\"' or '\'' && text[^1] == text[0]) text = text[1..^1]; return literal + quote + text + quote; } option = original.WithText(Normalize(original.DisplayText), Normalize(original.InsertText)); } if (option.DisplayText.Length == 0) continue; var match = MatchCompletion(option, prefix); if (match != null) matches.Add(match); }
        matches.Sort(CompareMatch); return matches.Select(m => m.Option).ToArray();
    }
    private void FilterCompletion()
    {
        if (_filtering) { _refilter = true; return; }
        _filtering = true; try
        {
            for (var pass = 0; pass < 64; pass++)
            {
                _refilter = _filterCanceled = false;
                var selected = _completionActive ? _selectedOption : -1; _completionBase = _customCompletionFilter ? "" : CompletionPrefix();
                if (!_customCompletionFilter) { var text = GetLine(GetCaretLine()); var column = GetCaretColumn(); var inString = IsInString(GetCaretLine(), column); var context = _completionBase.Length > 0 || column > 0 && _completionPrefixes.Contains(Slice(text, column - 1, column)) || column > 1 && Slice(text, column - 1, column) == " " && _completionPrefixes.Contains(Slice(text, column - 2, column - 1)) || column > 1 && Slice(text, column - 1, column) == " " && Identifier(Rune.GetRuneAt(text, ScalarIndex(text, column - 2))); if (!context || double.TryParse(_completionBase, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _) || !_completionForced && column > 0 && Slice(text, column - 1, column) == "(" || inString == -1 && column > 0 && IsInString(GetCaretLine(), column - 1) != -1) { CancelCodeCompletion(); return; } }
                var filtered = OnFilterCodeCompletionCandidates(_sources.ToArray()) ?? throw new InvalidOperationException("The completion filter returned null."); if (IsDisposed) return; if (_refilter) continue; if (_filterCanceled) return; if (filtered.Any(o => o == null)) throw new InvalidOperationException("The completion filter returned a null candidate.");
                var preserve = selected >= 0 && selected < filtered.Length && selected < _options.Count; for (var i = 0; preserve && i <= selected; i++) preserve = _options[i].DisplayText == filtered[i].DisplayText;
                _options.Clear(); _options.AddRange(filtered); _completionActive = _options.Count > 0; if (!_customCompletionFilter && _options.Count == 1 && _options[0].DisplayText == _completionBase) _completionActive = false; _selectedOption = preserve ? selected : 0; _optionOffset = 0; _codeDirty = true; QueueRedraw(); return;
            }
            throw new InvalidOperationException("The completion filter did not settle after 64 passes.");
        }
        finally { _filtering = false; }
    }
    /// <summary>Requests application completion using the typed virtual request hook.</summary><param name="force">Bypasses ordinary prefix/context checks.</param>
    public void RequestCodeCompletion(bool force = false) { MutableCode(); OnRequestCodeCompletion(force); }
    /// <summary>Requests candidate production after validating ordinary query context.</summary><param name="force">Bypasses word/prefix checks.</param>
    protected virtual void OnRequestCodeCompletion(bool force)
    { if (_completionActive && _options.Count > 0 && _options.All(o => o.Kind == _options[0].Kind) && _options[0].Kind is CodeCompletionKind.FilePath or CodeCompletionKind.NodePath or CodeCompletionKind.Signal) return; var column = GetCaretColumn(); var line = GetCaretLine(); var prefix = CompletionPrefix(); if (force || prefix.Length > 0 || column > 0 && IsInString(line, column) != -1 || column > 0 && _completionPrefixes.Contains(Slice(GetLine(line), column - 1, column)) || column > 1 && Slice(GetLine(line), column - 1, column) == " " && _completionPrefixes.Contains(Slice(GetLine(line), column - 2, column - 1))) CodeCompletionRequested?.Invoke(); }
    /// <summary>Confirms the selected candidate through the virtual insertion hook.</summary><param name="replace">Replaces the following identifier instead of merging matching suffix text.</param>
    public void ConfirmCodeCompletion(bool replace = false) { MutableCode(); if (!_completionActive || !Editable) return; OnConfirmCodeCompletion(replace); }
    /// <summary>Inserts the active candidate as one undo group and cancels completion.</summary><param name="replace">Replaces the following identifier.</param>
    protected virtual void OnConfirmCodeCompletion(bool replace)
    {
        if (!_completionActive || _options.Count == 0) return; var option = _options[_selectedOption]; var prefix = _completionBase; CancelCodeCompletion(); EditGroup(() =>
        {
            foreach (var caret in GetSortedCarets().Reverse())
            {
                if (MulticaretEditIgnoreCaret(caret)) continue; var line = GetCaretLine(caret); var column = GetCaretColumn(caret); var from = Math.Max(0, column - CountScalars(prefix.AsSpan())); var end = column; var endLine = line; var text = GetLine(line);
                if (replace) { var region = RegionAt(line, column); if (region != null && !_delimiters[region.Index].Comment && region.End.Y >= 0) { endLine = region.End.Y; end = Math.Max(0, region.End.X - 1); } else while (end < LengthOf(line) && Identifier(Rune.GetRuneAt(text, ScalarIndex(text, end)))) end++; }
                else { var suffix = option.InsertText[ScalarIndex(option.InsertText, Math.Min(CountScalars(option.InsertText.AsSpan()), CountScalars(prefix.AsSpan())))..]; var remaining = text.AsSpan(ScalarIndex(text, column)); var match = 0; while (match < suffix.Length && match < remaining.Length && suffix[match] == remaining[match]) match++; if (match > 0 && char.IsHighSurrogate(suffix[match - 1])) match--; end += CountScalars(remaining[..match]); }
                RemoveText(line, from, endLine, end); InsertTextAtCaret(option.InsertText, caret); line = GetCaretLine(caret); column = GetCaretColumn(caret); text = GetLine(line);
                if (option.InsertText.Length > 0)
                {
                    var last = Rune.GetRuneAt(option.InsertText, ScalarIndex(option.InsertText, CountScalars(option.InsertText.AsSpan()) - 1)).ToString(); var next = column < LengthOf(line) ? Slice(text, column, column + 1) : "";
                    if (HasStringDelimiter(last) && last == next) RemoveText(line, column, line, column + 1);
                    else if (_autoPairs) { foreach (var pair in _bracePairs) if (text.AsSpan(0, ScalarIndex(text, column)).EndsWith(pair.Key, StringComparison.Ordinal)) { if (!text.AsSpan(ScalarIndex(text, column)).StartsWith(pair.Value, StringComparison.Ordinal)) InsertTextAtCaret(pair.Value, caret); SetCaretColumn(column, false, caret); break; } }
                    if (last == ")" && column >= 2 && Slice(GetLine(line), column - 2, column) == "()") SetCaretColumn(column - 1, false, caret);
                }
            }
        }); if (option.InsertText.Length > 0 && _completionPrefixes.Contains(option.InsertText[^1..])) RequestCodeCompletion();
    }
    /// <summary>Sets source code-hint text; empty clears it. U+FFFF toggles highlighted argument spans.</summary><param name="codeHint">Nonnull text.</param>
    public void SetCodeHint(string codeHint) { MutableCode(); ArgumentNullException.ThrowIfNull(codeHint); _hint = codeHint; _hintX = float.NaN; DirtyCode(); }
    /// <summary>Chooses whether the code hint is below or above the primary caret.</summary><param name="drawBelow">True draws below.</param>
    public void SetCodeHintDrawBelow(bool drawBelow) { MutableCode(); _hintBelow = drawBelow; DirtyCode(); }
    /// <summary>Marks the most recently requested pointer symbol as eligible for lookup.</summary><param name="valid">Validation result.</param>
    public void SetSymbolLookupWordAsValid(bool valid) { MutableCode(); _symbolValid = valid; QueueRedraw(); }
    private string WordAt(Vector2i position)
    { if (position.Y < 0 || position.Y >= GetLineCount()) return ""; var text = GetLine(position.Y); var column = Math.Clamp(position.X, 0, LengthOf(position.Y)); var from = column; var to = column; bool Word(int at) { var rune = Slice(text, at, at + 1).EnumerateRunes().First(); return Rune.IsLetterOrDigit(rune) || rune.Value == '_'; } while (from > 0 && Word(from - 1)) from--; while (to < LengthOf(position.Y) && Word(to)) to++; return Slice(text, from, to); }
    private void FitCompletionSelection() { var visible = _completionVisibleLines; if (_selectedOption < _optionOffset) _optionOffset = _selectedOption; else if (_selectedOption >= _optionOffset + visible) _optionOffset = _selectedOption - visible + 1; }
}
