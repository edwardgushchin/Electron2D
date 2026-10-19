namespace Electron2D;

public sealed partial class TextParagraph
{
    private readonly Dictionary<int, TextLine> _serverLines = [];
    private readonly Dictionary<int, long> _serverVersions = [];
    private bool _serverMetricsDirty, _serverReadsRetired;
    private void ServerAttachSources()
    {
        _source.ServerGuard = Check; _dropcap.ServerGuard = Check;
        _source.ServerChanged = ContentChanged; _dropcap.ServerChanged = ContentChanged;
    }
    private void RetireServerLines()
    {
        var changed = _serverLines.Count != 0;
        foreach (var line in _serverLines.Values) line.Dispose();
        _serverLines.Clear(); _serverVersions.Clear(); _serverMetricsDirty = false;
        if (changed) { _serverReadsRetired = true; ContentChanged(); }
    }
    private void ServerLineChanged()
    {
        _serverMetricsDirty = true; _fonts.Clear(); _source.CollectFonts(_fonts); _dropcap.CollectFonts(_fonts);
        foreach (var line in _serverLines.Values) line.CollectFonts(_fonts);
        _fonts.Sort(static (a, b) => a.InstanceID.CompareTo(b.InstanceID));
    }
    private Vector2 ServerLineSize(int index) => _serverLines.TryGetValue(index, out var edited) ? edited.GetSize() : LineSize(_display.Lines[index]);
    private Vector2 ServerBaseline(int index) => _serverLines.TryGetValue(index, out var edited)
        ? edited.Orientation == TextOrientation.Horizontal ? new(0, edited.GetLineAscent()) : new(edited.GetLineAscent(), 0)
        : Axis(0, _display.Lines[index].Ascent);
    private TextLayoutLine ServerMetrics(int index, bool geometry = false)
    {
        if (!_serverLines.TryGetValue(index, out var line)) return (geometry ? _geometry : _display).Lines[index];
        var layout = line.ServerLayout; var m = layout.LineCount == 0 ? default : layout.Lines[0];
        return m with { Start = line.ServerOffset, End = line.ServerOffset + line.ScalarCount, RangeEnd = line.ServerOffset + line.ScalarCount };
    }
    private void RefreshServerLines()
    {
        if (_serverLines.Count == 0) return;
        foreach (var entry in _serverLines)
        {
            var revision = entry.Value.PreparedRevision;
            if (!_serverVersions.TryGetValue(entry.Key, out var previous) || previous != revision) { _serverVersions[entry.Key] = revision; _serverMetricsDirty = true; }
        }
        if (!_serverMetricsDirty) return;
        var advance = 0f; var cross = 0f;
        for (var i = 0; i < VisibleLines; i++)
        {
            var size = ServerLineSize(i); advance = Math.Max(advance, (Vertical ? size.Y : size.X) + (CapAdvance > 0 && i <= _display.DropcapLines ? CapAdvance : 0));
            cross += Vertical ? size.X : size.Y; if (i + 1 < VisibleLines) cross += _spacing;
        }
        if (CapAdvance > 0) cross = Math.Max(cross, Vertical ? _capSize.X : _capSize.Y);
        _size = Axis(advance, cross); if (!_size.IsFinite()) throw new OverflowException("Paragraph exceeds finite geometry.");
        cross = 0; var capLTR = _dropcap.GetInferredDirection() == TextDirection.LTR;
        for (var i = 0; i < _display.LineCount; i++) { _origins[i] = CalculateLineOrigin(i, cross, capLTR); if (!_origins[i].IsFinite()) throw new OverflowException("Paragraph line origin exceeds finite geometry."); var size = ServerLineSize(i); cross += (Vertical ? size.X : size.Y) + _spacing; }
        _serverMetricsDirty = false;
    }
    private void ServerCheckLine(int line, RID rid)
    {
        Ensure();
        if ((uint)line >= (uint)_lines.Count || _lines[line] != rid) throw new InvalidOperationException("The paragraph line was retired by reflow.");
    }
    internal TextServer.BufferView ServerAuthoringView(int line, RID rid)
    {
        Check(); if ((uint)line >= (uint)_lines.Count || _lines[line] != rid) throw new InvalidOperationException("The paragraph line was retired.");
        if (_serverLines.TryGetValue(line, out var edited)) return new(edited, edited.ServerUnpreparedLayout, 0, edited.ServerOffset, edited.ServerOffset + edited.ScalarCount, edited.ServerOffset, edited.ServerDisplayWidth);
        var value = _display.Lines[line]; return new(_source, _geometry, line, value.Start, value.RangeEnd < 0 ? value.End : value.RangeEnd, 0, value.Width);
    }
    internal TextServer.BufferView ServerView(int line, RID rid)
    {
        ServerCheckLine(line, rid);
        if (_serverLines.TryGetValue(line, out var edited))
            return new(edited, edited.ServerLayout, 0, edited.ServerOffset, edited.ServerOffset + edited.ScalarCount, edited.ServerOffset, edited.ServerDisplayWidth);
        var value = _display.Lines[line];
        return new(_source, _geometry, line, value.Start, value.RangeEnd < 0 ? value.End : value.RangeEnd, 0, value.Width);
    }
    internal TextLine ServerMutableLine(int line, RID rid, out bool pending)
    {
        ServerCheckLine(line, rid);
        if (_serverLines.TryGetValue(line, out var existing)) { pending = false; return existing; }
        var range = _display.Lines[line]; var end = range.RangeEnd < 0 ? range.End : range.RangeEnd;
        var copy = _source.ServerSlice(range.Start, end);
        copy.ServerGuard = Check; pending = true; return copy;
    }
    internal void ServerPublishLine(int line, TextLine copy)
    {
        _serverLines.Add(line, copy); copy.ServerChanged = ServerLineChanged; ServerLineChanged();
    }
    internal bool ServerIsReady(int line, RID rid)
    {
        Check(); if (_dirty || !_source.ServerReady || !_dropcap.ServerReady || _source.ServerCurrentRevision != _sourceVersion || _dropcap.ServerCurrentRevision != _dropcapVersion) return false;
        if ((uint)line >= (uint)_lines.Count || _lines[line] != rid) throw new InvalidOperationException("The paragraph line was retired.");
        return !_serverLines.TryGetValue(line, out var edited) || edited.ServerReady;
    }
    internal void ServerDraw(int line, RID rid, CanvasItem canvas, Vector2 baseline, float left, float right, int outline, Color color, float oversampling, bool outlinePass)
    {
        ServerCheckLine(line, rid); Font.ValidateDraw(canvas, baseline, color, oversampling);
        if (!float.IsFinite(left) || !float.IsFinite(right)) throw new ArgumentOutOfRangeException(nameof(left));
        _busy = true;
        try
        {
            BeginPreparedReads();
            if (_serverLines.TryGetValue(line, out var edited)) edited.ServerDraw(canvas, baseline, left, right, outline, color, oversampling, outlinePass);
            else _display.Draw(canvas, baseline, color, outline, oversampling, firstLine: line, maxLines: 1, outlinePass: outlinePass, ignoreLineLimit: true, advanceClipLeft: left, advanceClipRight: right);
        }
        finally { try { EndReads(); } finally { _busy = false; } }
    }
}
