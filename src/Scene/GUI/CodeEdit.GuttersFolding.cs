namespace Electron2D;

public partial class CodeEdit
{
    private Marker MarkersAt(int line) { RequireLine(line); return _mainGutter >= 0 && TryGetCodeGutterMetadata<Marker>(line, _mainGutter, out var flags) ? flags : 0; }
    private void SetMarker(int line, Marker marker, bool set)
    { MutableCode(); RequireLine(line); if (_mainGutter < 0) return; var old = MarkersAt(line); var value = set ? old | marker : old & ~marker; if (old == value) return; SetLineGutterMetadata(line, _mainGutter, value); SetLineGutterClickable(line, _mainGutter, _drawBreakpoints); if (marker == Marker.Breakpoint) { if (set) _breakpoints.Add(line); else _breakpoints.Remove(line); BreakpointToggled?.Invoke(line); } QueueRedraw(); }
    private int[] MarkedLines(Marker marker) { CheckCode(); var result = new List<int>(); for (var line = 0; line < GetLineCount(); line++) if ((MarkersAt(line) & marker) != 0) result.Add(line); return result.ToArray(); }
    private void ClearMarkers(Marker marker) { MutableCode(); List<Exception>? errors = null; foreach (var line in MarkedLines(marker)) try { SetMarker(line, marker, false); } catch (Exception error) { CollectException(ref errors, error); } ThrowCollected("Marker callbacks failed.", errors); }
    private void RefreshBreakpoints()
    { var current = new HashSet<int>(MarkedLines(Marker.Breakpoint)); var changes = new HashSet<int>(_breakpoints); changes.SymmetricExceptWith(current); _breakpoints.Clear(); foreach (var line in current) _breakpoints.Add(line); List<Exception>? errors = null; foreach (var line in changes.Order()) try { BreakpointToggled?.Invoke(line); } catch (Exception error) { CollectException(ref errors, error); } ThrowCollected("Breakpoint callbacks failed.", errors); }
    /// <summary>Sets a runtime breakpoint marker.</summary><param name="line">Existing line.</param><param name="breakpointed">Desired state.</param>
    public void SetLineAsBreakpoint(int line, bool breakpointed) => SetMarker(line, Marker.Breakpoint, breakpointed);
    /// <summary>Reports a breakpoint marker.</summary><param name="line">Existing line.</param><returns>Marker state.</returns>
    public bool IsLineBreakpointed(int line) => (MarkersAt(line) & Marker.Breakpoint) != 0;
    /// <summary>Returns an independent ascending breakpoint line array.</summary><returns>Logical indices.</returns>
    public int[] GetBreakpointedLines() => MarkedLines(Marker.Breakpoint);
    /// <summary>Clears all breakpoints, notifying each changed line.</summary>
    public void ClearBreakpointedLines() => ClearMarkers(Marker.Breakpoint);
    /// <summary>Sets a runtime bookmark.</summary><param name="line">Existing line.</param><param name="bookmarked">Desired state.</param>
    public void SetLineAsBookmarked(int line, bool bookmarked) => SetMarker(line, Marker.Bookmark, bookmarked);
    /// <summary>Reports a bookmark.</summary><param name="line">Existing line.</param><returns>Marker state.</returns>
    public bool IsLineBookmarked(int line) => (MarkersAt(line) & Marker.Bookmark) != 0;
    /// <summary>Returns an ascending bookmark array.</summary><returns>Logical indices.</returns>
    public int[] GetBookmarkedLines() => MarkedLines(Marker.Bookmark);
    /// <summary>Clears bookmarks.</summary>
    public void ClearBookmarkedLines() => ClearMarkers(Marker.Bookmark);
    /// <summary>Sets an executing-line marker.</summary><param name="line">Existing line.</param><param name="executing">Desired state.</param>
    public void SetLineAsExecuting(int line, bool executing) => SetMarker(line, Marker.Executing, executing);
    /// <summary>Reports an executing-line marker.</summary><param name="line">Existing line.</param><returns>Marker state.</returns>
    public bool IsLineExecuting(int line) => (MarkersAt(line) & Marker.Executing) != 0;
    /// <summary>Returns an ascending executing-line array.</summary><returns>Logical indices.</returns>
    public int[] GetExecutingLines() => MarkedLines(Marker.Executing);
    /// <summary>Clears executing-line markers.</summary>
    public void ClearExecutingLines() => ClearMarkers(Marker.Executing);
    private int FoldEnd(int line)
    {
        RequireLine(line); if (!_lineFolding || line + 1 >= GetLineCount() || string.IsNullOrWhiteSpace(GetLine(line)) || IsLineCodeRegionEnd(line)) return line;
        if (IsLineCodeRegionStart(line)) { var depth = 1; for (var at = line + 1; at < GetLineCount(); at++) { if (IsLineCodeRegionStart(at)) depth++; else if (IsLineCodeRegionEnd(at) && --depth == 0) return at; } return line; }
        var region = RegionAt(line, -1); if (region != null) { if (region.Start.Y != line) return line; if (region.End.Y < 0) return GetLineCount() - 1; if (region.End.Y > line) return RegionAt(region.End.Y, -1) != null ? region.End.Y : line; var comment = _delimiters[region.Index].Comment; if (line > 0) { var previous = RegionAt(line - 1, -1); if (previous != null && _delimiters[previous.Index].Comment == comment && !IsLineCodeRegionStart(line - 1) && !IsLineCodeRegionEnd(line - 1)) return line; } var end = line; for (var at = line + 1; at < GetLineCount(); at++) { var next = RegionAt(at, -1); if (next == null || _delimiters[next.Index].Comment != comment || IsLineCodeRegionStart(at) || IsLineCodeRegionEnd(at)) break; end = at; } return end; }
        var indent = GetIndentLevel(line); var last = line; var canFold = false;
        for (var at = line + 1; at < GetLineCount(); at++) { if (string.IsNullOrWhiteSpace(GetLine(at))) continue; var delimiter = RegionAt(at, -1); if (!canFold && delimiter != null) continue; if (GetIndentLevel(at) > indent) { last = at; canFold = true; continue; } if (delimiter == null) break; }
        return canFold ? last : line;
    }
    /// <summary>Reports whether enabled folding can hide a region, multiline delimiter or indented block.</summary><param name="line">Existing line.</param><returns>Whether children can fold.</returns>
    public bool CanFoldLine(int line) { CheckCode(); return !_folded.Contains(line) && !IsCodeLineHidden(line) && FoldEnd(line) > line; }
    private void RebuildHidden()
    { for (var line = 0; line < GetLineCount(); line++) SetCodeLineHidden(line, false); foreach (var start in _folded.ToArray()) { if (start >= GetLineCount()) { _folded.Remove(start); continue; } var end = FoldEnd(start); if (end == start) { _folded.Remove(start); SetMarker(start, Marker.Folded, false); continue; } for (var line = start + 1; line <= end; line++) SetCodeLineHidden(line, true); } DirtyCode(); }
    /// <summary>Folds a supported block and moves carets from hidden descendants onto its header.</summary><param name="line">Existing line.</param>
    public void FoldLine(int line) { MutableCode(); var end = FoldEnd(line); if (end == line) return; _folded.Add(line); SetMarker(line, Marker.Folded, true); for (var caret = 0; caret < GetCaretCount(); caret++) if (GetCaretLine(caret) > line && GetCaretLine(caret) <= end) { SetCaretLine(line, false, true, 0, caret); SetCaretColumn(LengthOf(line), false, caret); Deselect(caret); } RebuildHidden(); }
    /// <summary>Unfolds the containing folded header while preserving nested folded blocks.</summary><param name="line">Existing header or hidden line.</param>
    public void UnfoldLine(int line) { MutableCode(); RequireLine(line); if (_folded.Remove(line)) { SetMarker(line, Marker.Folded, false); } else { var parent = -1; foreach (var start in _folded) if (start < line && FoldEnd(start) >= line && start > parent) parent = start; if (parent >= 0) { _folded.Remove(parent); SetMarker(parent, Marker.Folded, false); } } RebuildHidden(); }
    /// <summary>Folds every supported block.</summary>
    public void FoldAllLines() { MutableCode(); for (var line = 0; line < GetLineCount(); line++) if (CanFoldLine(line)) FoldLine(line); }
    /// <summary>Clears all folds and restores every logical line.</summary>
    public void UnfoldAllLines() { MutableCode(); _folded.Clear(); for (var line = 0; line < GetLineCount(); line++) { SetMarker(line, Marker.Folded, false); SetCodeLineHidden(line, false); } DirtyCode(); }
    /// <summary>Reports an explicit folded header.</summary><param name="line">Existing line.</param><returns>Fold state.</returns>
    public bool IsLineFolded(int line) { RequireLine(line); return _folded.Contains(line); }
    /// <summary>Returns an independent ascending folded-header array.</summary><returns>Logical indices.</returns>
    public int[] GetFoldedLines() { CheckCode(); return _folded.Order().ToArray(); }
    /// <summary>Toggles a supported folded header.</summary><param name="line">Existing line.</param>
    public void ToggleFoldableLine(int line) { if (IsLineFolded(line)) UnfoldLine(line); else FoldLine(line); }
    /// <summary>Toggles each distinct caret header.</summary>
    public void ToggleFoldableLinesAtCarets() { MutableCode(); var lines = Enumerable.Range(0, GetCaretCount()).Select(i => GetCaretLine(i)).Distinct().OrderDescending().ToArray(); foreach (var line in lines) ToggleFoldableLine(line); }
    private void GutterClick(int line, int gutter) { if (gutter == _mainGutter && _drawBreakpoints) SetLineAsBreakpoint(line, !IsLineBreakpointed(line)); else if (gutter == _foldGutter) ToggleFoldableLine(line); }
}
