using System.Text;
namespace Electron2D;

public partial class CodeEdit
{
    private static void RequireSymbols(string key, bool empty)
    { ArgumentNullException.ThrowIfNull(key); if (!empty && key.Length == 0) throw new ArgumentException("A symbol key cannot be empty.", nameof(key)); foreach (var rune in key.EnumerateRunes()) if (Rune.IsLetterOrDigit(rune) || Rune.IsWhiteSpace(rune) || rune.Value == '_') throw new ArgumentException("Keys must contain symbols.", nameof(key)); }
    private void AddDelimiter(string start, string end, bool lineOnly, bool comment)
    { MutableCode(); RequireSymbols(start, false); RequireSymbols(end, true); if (_delimiters.Any(d => d.Start == start)) throw new ArgumentException("Delimiter start keys must be unique.", nameof(start)); var at = _delimiters.FindIndex(d => CountScalars(d.Start.AsSpan()) <= CountScalars(start.AsSpan())); _delimiters.Insert(at < 0 ? _delimiters.Count : at, new(start, end, lineOnly || end.Length == 0, comment)); DirtyCode(); }
    private string[] DelimiterDescriptions(bool comment) { CheckCode(); return _delimiters.Where(d => d.Comment == comment).Select(d => d.Start + (d.End.Length > 0 ? " " + d.End : "")).ToArray(); }
    private void SetDelimiterDescriptions(string[] descriptions, bool comment)
    {
        MutableCode(); var values = CopyKeys(descriptions, nameof(descriptions)); var replacement = new List<Delimiter>(); var keys = new HashSet<string>(_delimiters.Where(d => d.Comment != comment).Select(d => d.Start), StringComparer.Ordinal);
        foreach (var description in values) { var split = description.IndexOf(' '); var start = split < 0 ? description : description[..split]; var end = split < 0 ? "" : description[(split + 1)..]; RequireSymbols(start, false); RequireSymbols(end, true); if (!keys.Add(start)) throw new ArgumentException("Delimiter start keys must be unique.", nameof(descriptions)); replacement.Add(new(start, end, end.Length == 0, comment)); }
        _delimiters.RemoveAll(d => d.Comment == comment); foreach (var delimiter in replacement) { var at = _delimiters.FindIndex(d => CountScalars(d.Start.AsSpan()) <= CountScalars(delimiter.Start.AsSpan())); _delimiters.Insert(at < 0 ? _delimiters.Count : at, delimiter); }
        DirtyCode();
    }
    /// <summary>Adds a symbol-delimited string region, ordered longest start key first.</summary><param name="startKey">Unique nonempty symbol key.</param><param name="endKey">Symbol key or empty for line-only.</param><param name="lineOnly">Prevents continuation across LF.</param>
    public void AddStringDelimiter(string startKey, string endKey, bool lineOnly = false) => AddDelimiter(startKey, endKey, lineOnly, false);
    /// <summary>Adds a symbol-delimited comment region.</summary><param name="startKey">Unique nonempty symbol key.</param><param name="endKey">Symbol key or empty.</param><param name="lineOnly">Prevents continuation across LF.</param>
    public void AddCommentDelimiter(string startKey, string endKey, bool lineOnly = false) => AddDelimiter(startKey, endKey, lineOnly, true);
    /// <summary>Removes a string delimiter by start key.</summary><param name="startKey">Exact key.</param>
    public void RemoveStringDelimiter(string startKey) { MutableCode(); ArgumentNullException.ThrowIfNull(startKey); _delimiters.RemoveAll(d => !d.Comment && d.Start == startKey); DirtyCode(); }
    /// <summary>Removes a comment delimiter by start key.</summary><param name="startKey">Exact key.</param>
    public void RemoveCommentDelimiter(string startKey) { MutableCode(); ArgumentNullException.ThrowIfNull(startKey); _delimiters.RemoveAll(d => d.Comment && d.Start == startKey); DirtyCode(); }
    /// <summary>Clears string delimiters while retaining comments.</summary>
    public void ClearStringDelimiters() { MutableCode(); _delimiters.RemoveAll(d => !d.Comment); DirtyCode(); }
    /// <summary>Clears comment delimiters while retaining strings.</summary>
    public void ClearCommentDelimiters() { MutableCode(); _delimiters.RemoveAll(d => d.Comment); DirtyCode(); }
    /// <summary>Reports an exact string start key.</summary><param name="startKey">Exact key.</param><returns>Whether present.</returns>
    public bool HasStringDelimiter(string startKey) { CheckCode(); ArgumentNullException.ThrowIfNull(startKey); return _delimiters.Any(d => !d.Comment && d.Start == startKey); }
    /// <summary>Reports an exact comment start key.</summary><param name="startKey">Exact key.</param><returns>Whether present.</returns>
    public bool HasCommentDelimiter(string startKey) { CheckCode(); ArgumentNullException.ThrowIfNull(startKey); return _delimiters.Any(d => d.Comment && d.Start == startKey); }
    private Delimiter AtDelimiter(int index) { CheckCode(); if ((uint)index >= _delimiters.Count) throw new ArgumentOutOfRangeException(nameof(index)); return _delimiters[index]; }
    /// <summary>Returns the combined ordered delimiter start key.</summary><param name="delimiterIndex">Existing combined index.</param><returns>Source key.</returns>
    public string GetDelimiterStartKey(int delimiterIndex) => AtDelimiter(delimiterIndex).Start;
    /// <summary>Returns the combined ordered delimiter end key.</summary><param name="delimiterIndex">Existing combined index.</param><returns>Source key.</returns>
    public string GetDelimiterEndKey(int delimiterIndex) => AtDelimiter(delimiterIndex).End;
    private void EnsureRegions()
    {
        CheckCode(); var version = GetVersion(); if (_parsedVersion == version) return; _regions.Clear(); Region? active = null;
        for (var line = 0; line < GetLineCount(); line++)
        {
            var text = GetLine(line); var utf = 0; var column = 0; var segmentStart = active == null ? -1 : 0;
            while (utf < text.Length)
            {
                if (text[utf] == '\\') { utf++; column++; if (utf < text.Length) { utf += char.IsHighSurrogate(text[utf]) && utf + 1 < text.Length && char.IsLowSurrogate(text[utf + 1]) ? 2 : 1; column++; } continue; }
                if (active != null)
                {
                    var delimiter = _delimiters[active.Index];
                    if (delimiter.End.Length > 0 && text.AsSpan(utf).StartsWith(delimiter.End, StringComparison.Ordinal))
                    { var length = CountScalars(delimiter.End.AsSpan()); utf += delimiter.End.Length; column += length; active.End = new(column, line); active.Segments.Add((line, segmentStart, column)); active = null; segmentStart = -1; continue; }
                }
                else
                {
                    var found = -1; for (var index = 0; index < _delimiters.Count; index++) if (text.AsSpan(utf).StartsWith(_delimiters[index].Start, StringComparison.Ordinal)) { found = index; break; }
                    if (found >= 0) { active = new(found, new(column + 1, line)); _regions.Add(active); segmentStart = column; utf += _delimiters[found].Start.Length; column += CountScalars(_delimiters[found].Start.AsSpan()); continue; }
                }
                utf += char.IsHighSurrogate(text[utf]) && utf + 1 < text.Length && char.IsLowSurrogate(text[utf + 1]) ? 2 : 1; column++;
            }
            if (active != null) { active.Segments.Add((line, segmentStart, column + 1)); if (_delimiters[active.Index].LineOnly) { active.End = new(column + 1, line); active = null; } }
        }
        _parsedVersion = version;
    }
    private bool WholeRegionLine(int line, int from, int to) { var text = GetLine(line); return Slice(text, 0, Math.Min(from, LengthOf(line))).Trim().Length == 0 && Slice(text, Math.Min(to, LengthOf(line)), LengthOf(line)).Trim().Length == 0; }
    private Region? RegionAt(int line, int column)
    { RequireLine(line); if (column != -1 && ((uint)column > (uint)(LengthOf(line) + 1))) throw new ArgumentOutOfRangeException(nameof(column)); EnsureRegions(); foreach (var region in _regions) foreach (var span in region.Segments) if (span.Line == line && (column == -1 ? WholeRegionLine(line, span.From, span.To) : column > span.From && column < span.To || column == span.From && region.Start.Y < line)) return region; return null; }
    /// <summary>Returns a combined string delimiter index at a scalar position, or for a wholly covered line.</summary><param name="line">Existing line.</param><param name="column">Scalar position, or -1 for whole line.</param><returns>Combined index or -1.</returns>
    public int IsInString(int line, int column = -1) { var region = RegionAt(line, column); return region != null && !_delimiters[region.Index].Comment ? region.Index : -1; }
    /// <summary>Returns a combined comment delimiter index at a scalar position, or for a wholly covered line.</summary><param name="line">Existing line.</param><param name="column">Scalar position, or -1 for whole line.</param><returns>Combined index or -1.</returns>
    public int IsInComment(int line, int column = -1) { var region = RegionAt(line, column); return region != null && _delimiters[region.Index].Comment ? region.Index : -1; }
    /// <summary>Returns the delimiter start boundary one scalar after its opening key begins.</summary><param name="line">Existing line.</param><param name="column">Scalar position.</param><returns>Column/line, or (-1,-1).</returns>
    public Vector2 GetDelimiterStartPosition(int line, int column) => RegionAt(line, column)?.Start ?? new Vector2i(-1, -1);
    /// <summary>Returns the delimiter end boundary after its closing key, or line-length plus one for line-only.</summary><param name="line">Existing line.</param><param name="column">Scalar position.</param><returns>Column/line, or (-1,-1) if unavailable/unclosed.</returns>
    public Vector2 GetDelimiterEndPosition(int line, int column) => RegionAt(line, column)?.End ?? new Vector2i(-1, -1);
    /// <summary>Sets source region tags without a comment prefix.</summary><param name="start">Nonempty start tag.</param><param name="end">Nonempty end tag.</param>
    public void SetCodeRegionTags(string start = "region", string end = "endregion") { MutableCode(); ArgumentException.ThrowIfNullOrWhiteSpace(start); ArgumentException.ThrowIfNullOrWhiteSpace(end); _regionStart = start; _regionEnd = end; DirtyCode(); }
    /// <summary>Returns the source region start tag.</summary><returns>Region initially.</returns>
    public string GetCodeRegionStartTag() { CheckCode(); return _regionStart; }
    /// <summary>Returns the source region end tag.</summary><returns>Endregion initially.</returns>
    public string GetCodeRegionEndTag() { CheckCode(); return _regionEnd; }
    private bool RegionTag(int line, string tag) { var text = GetLine(line).TrimStart(); foreach (var delimiter in _delimiters) if (delimiter.Comment && delimiter.LineOnly && text.StartsWith(delimiter.Start, StringComparison.Ordinal) && text[delimiter.Start.Length..].TrimStart().StartsWith(tag, StringComparison.Ordinal)) return true; return false; }
    /// <summary>Reports a single-line-comment code-region start.</summary><param name="line">Existing line.</param><returns>Whether the configured tag begins this line.</returns>
    public bool IsLineCodeRegionStart(int line) { RequireLine(line); return RegionTag(line, _regionStart); }
    /// <summary>Reports a single-line-comment code-region end.</summary><param name="line">Existing line.</param><returns>Whether the configured tag begins this line.</returns>
    public bool IsLineCodeRegionEnd(int line) { RequireLine(line); return RegionTag(line, _regionEnd); }
}
