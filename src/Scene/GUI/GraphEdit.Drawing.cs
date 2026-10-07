namespace Electron2D;

public partial class GraphEdit
{
    private readonly Vector2[] _previewLine = new Vector2[65];
    private readonly Color[] _lineColors = new Color[65];
    private Connection? _hoverConnection;
    private bool Endpoints(Connection c)
    {
        if (Element(c.Value.FromNode) is not GraphNode from || Element(c.Value.ToNode) is not GraphNode to || c.Value.FromPort >= from.GetOutputPortCount() || c.Value.ToPort >= to.GetInputPortCount()) { c.Valid = false; return false; }
        var p1 = from.Position + from.GetOutputPortPosition(c.Value.FromPort) * _zoom; var p2 = to.Position + to.GetInputPortPosition(c.Value.ToPort) * _zoom;
        c.FromColor = from.GetOutputPortColor(c.Value.FromPort); c.ToColor = to.GetInputPortColor(c.Value.ToPort);
        if (c.Dirty || !c.Valid || c.From != p1 || c.To != p2)
        {
            c.From = p1; c.To = p2;
            if (_customLine) { var points = OnGetConnectionLine(p1, p2); ValidateLine(points); c.CustomPoints = points; }
            else FillLine(c.Points, p1, p2);
            c.Dirty = false;
        }
        c.Valid = true; return true;
    }
    private static void ValidateLine(Vector2[] points) { ArgumentNullException.ThrowIfNull(points); if (points.Length < 2) throw new InvalidOperationException("A connection line requires at least two points."); foreach (var p in points) if (!p.IsFinite()) throw new InvalidOperationException("Connection line points must be finite."); }
    private void FillLine(Span<Vector2> points, Vector2 from, Vector2 to)
    {
        var offset = MathF.Abs(to.X - from.X) * _curvature;
        var a = from + new Vector2(offset, 0); var b = to - new Vector2(offset, 0);
        for (var i = 0; i < points.Length; i++) points[i] = from.BezierInterpolate(a, b, to, (float)i / (points.Length - 1));
    }
    /// <summary>Builds a customized local connection line.</summary><param name="fromPosition">Finite start.</param><param name="toPosition">Finite end.</param><returns>An owned point array with at least two points.</returns>
    protected virtual Vector2[] OnGetConnectionLine(Vector2 fromPosition, Vector2 toPosition) { var result = new Vector2[65]; FillLine(result, fromPosition, toPosition); return result; }
    /// <summary>Builds a caller-owned connection line using the virtual geometry hook.</summary><param name="fromNode">Finite start position.</param><param name="toNode">Finite end position.</param><returns>Copied local points.</returns>
    public Vector2[] GetConnectionLine(Vector2 fromNode, Vector2 toNode) { CheckEdit(); if (!fromNode.IsFinite() || !toNode.IsFinite()) throw new ArgumentOutOfRangeException(nameof(fromNode)); var points = OnGetConnectionLine(fromNode, toNode); ValidateLine(points); return _customLine ? (Vector2[])points.Clone() : points; }
    private static ReadOnlySpan<Vector2> Line(Connection c) => c.CustomPoints ?? c.Points;
    /// <summary>Finds a visible connection nearest a viewport-local point.</summary><param name="point">Finite local point.</param><param name="maxDistance">Finite nonnegative distance.</param><returns>A value snapshot or null.</returns>
    public GraphConnection? GetClosestConnectionAtPoint(Vector2 point, float maxDistance = 4) { CheckEdit(); if (!point.IsFinite() || !float.IsFinite(maxDistance) || maxDistance < 0) throw new ArgumentOutOfRangeException(nameof(point)); return Closest(point, maxDistance)?.Value; }
    private Connection? Closest(Vector2 point, float maxDistance)
    {
        Connection? best = null; var distance = maxDistance;
        foreach (var c in _connections) if (Endpoints(c)) { var points = Line(c); for (var i = 1; i < points.Length; i++) { var d = point.DistanceTo(Geometry.GetClosestPointToSegment(point, points[i - 1], points[i])); if (d <= distance) { best = c; distance = d; } } }
        return best;
    }
    /// <summary>Copies visible connections intersecting a viewport-local rectangle.</summary><param name="rect">Finite rectangle; negative sizes are normalized.</param><returns>Caller-owned record snapshots.</returns>
    public GraphConnection[] GetConnectionsIntersectingWithRect(Rect2 rect)
    {
        CheckEdit(); if (!rect.Position.IsFinite() || !rect.Size.IsFinite()) throw new ArgumentOutOfRangeException(nameof(rect)); rect = rect.Abs(); var result = new List<GraphConnection>();
        foreach (var c in _connections) if (Endpoints(c)) { var line = Line(c); for (var i = 1; i < line.Length; i++) if (SegmentHits(rect, line[i - 1], line[i])) { result.Add(c.Value); break; } }
        return result.ToArray();
    }
    private static bool SegmentHits(Rect2 rect, Vector2 a, Vector2 b) => rect.HasPoint(a) || rect.HasPoint(b) || Geometry.SegmentIntersectsSegment(a, b, rect.Position, new(rect.End.X, rect.Position.Y)) != null || Geometry.SegmentIntersectsSegment(a, b, new(rect.End.X, rect.Position.Y), rect.End) != null || Geometry.SegmentIntersectsSegment(a, b, rect.End, new(rect.Position.X, rect.End.Y)) != null || Geometry.SegmentIntersectsSegment(a, b, new(rect.Position.X, rect.End.Y), rect.Position) != null;
    private void DrawConnections(Control canvas)
    {
        for (var index = 0; index < _connections.Count; index++)
        {
            var c = _connections[index]; if (!Endpoints(c)) { if (!c.Value.KeepAlive && IsInsideTree) _connections.RemoveAt(index--); continue; }
            var line = Line(c); var color = c.FromColor.Lerp(c.ToColor, .5f).Lerp(GetThemeColor("activity"), c.Activity);
            var hover = ReferenceEquals(c, _hoverConnection); var width = _thickness + (hover ? GetThemeConstant("connection_hover_thickness") : 0);
            if (hover) color = color.Lerp(GetThemeColor("connection_hover_tint_color"), GetThemeColor("connection_hover_tint_color").A);
            var rim = GetThemeColor("connection_rim_color"); if (rim.A > 0) canvas.DrawPolyline(line, rim, width + 2, _antialiased);
            if (!_customLine)
            {
                for (var i = 0; i < _lineColors.Length; i++) { _lineColors[i] = c.FromColor.Lerp(c.ToColor, (float)i / (_lineColors.Length - 1)).Lerp(GetThemeColor("activity"), c.Activity); if (hover) _lineColors[i] = _lineColors[i].Lerp(GetThemeColor("connection_hover_tint_color"), GetThemeColor("connection_hover_tint_color").A); }
                canvas.DrawPolylineColors(line, _lineColors, width, _antialiased);
            }
            else canvas.DrawPolyline(line, color, width, _antialiased);
        }
    }
    private Rect2 MinimapRect => new Rect2(Size - _minimapSize - new Vector2(12, 12), _minimapSize);
    private Rect2 GraphBounds()
    {
        var result = new Rect2(_scroll / _zoom, Size / _zoom);
        foreach (var e in _elements) if (!e.IsDisposed) result = result.Merge(new(e.PositionOffset, e.Size));
        return result.Grow(20);
    }
    private void DrawOverlay(Control canvas)
    {
        if (_boxSelecting) { var rect = new Rect2(_pressPosition, _pointer - _pressPosition).Abs(); canvas.DrawRect(rect, GetThemeColor("selection_fill")); canvas.DrawRect(rect, GetThemeColor("selection_stroke"), false); }
        if (_connectionNode != null && !_connectionNode.IsDisposed)
        {
            var start = _connectionNode.Position + (_connectionOutput ? _connectionNode.GetOutputPortPosition(_connectionPort) : _connectionNode.GetInputPortPosition(_connectionPort)) * _zoom;
            var end = _pointer; var color = _connectionOutput ? _connectionNode.GetOutputPortColor(_connectionPort) : _connectionNode.GetInputPortColor(_connectionPort);
            if (ConnectionTarget(_pointer, out var target, out var port)) { end = target.Position + (_connectionOutput ? target.GetInputPortPosition(port) : target.GetOutputPortPosition(port)) * _zoom; color = color.Lerp(GetThemeColor("connection_valid_target_tint_color"), GetThemeColor("connection_valid_target_tint_color").A); }
            FillLine(_previewLine, _connectionOutput ? start : end, _connectionOutput ? end : start); canvas.DrawPolyline(_previewLine, color, _thickness, _antialiased);
        }
        if (!_minimap) return;
        var rectMap = MinimapRect; var bounds = GraphBounds(); var scale = Math.Min(rectMap.Size.X / bounds.Size.X, rectMap.Size.Y / bounds.Size.Y); var origin = rectMap.Position + (rectMap.Size - bounds.Size * scale) / 2;
        canvas.DrawRect(rectMap, new(.08f, .08f, .1f, _minimapOpacity));
        foreach (var e in _elements) if (!e.IsDisposed) { var color = e is GraphFrame ? new Color(.5f, .5f, .6f, _minimapOpacity) : e.Selected ? new Color(.8f, .85f, 1, _minimapOpacity) : new Color(.5f, .7f, .9f, _minimapOpacity); canvas.DrawRect(new(origin + (e.PositionOffset - bounds.Position) * scale, e.Size * scale), color, e is not GraphFrame); }
        canvas.DrawRect(new(origin + (_scroll / _zoom - bounds.Position) * scale, Size / _zoom * scale), new(1, 1, 1, _minimapOpacity), false);
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what is NotificationResized or NotificationEnterTree) { _lines.Size = _overlay.Size = Size; ChangedGraph(); }
        else if (what == NotificationThemeChanged) { RefreshMenu(); ChangedGraph(); }
        else if (what == NotificationFocusExit) CancelInteraction();
        else if (what == NotificationDraw)
        {
            if (GetThemeStyleBox("panel") is { } panel) DrawStyleBox(panel, new(Vector2.Zero, Size));
            if (HasFocus() && GetThemeStyleBox("panel_focus") is { } focus) DrawStyleBox(focus, new(Vector2.Zero, Size));
            if (_showGrid)
            {
                var spacing = _snapDistance * _zoom;
                if (spacing <= 0 || !float.IsFinite(spacing)) return;
                spacing *= MathF.Max(1, MathF.Ceiling(8 / spacing));
                var left = MathF.Floor(_scroll.X / spacing); var top = MathF.Floor(_scroll.Y / spacing);
                var startX = -_scroll.X + left * spacing; var startY = -_scroll.Y + top * spacing;
                var columns = Math.Min(2048, (int)Math.Ceiling(Size.X / spacing) + 1); var rows = Math.Min(2048, (int)Math.Ceiling(Size.Y / spacing) + 1);
                for (var x = 0; x < columns; x++) { var px = startX + x * spacing; var color = GetThemeColor(((int)left + x) % 10 == 0 ? "grid_major" : "grid_minor"); if (_gridPattern == GridPatternMode.Lines) DrawLine(new(px, 0), new(px, Size.Y), color); else for (var y = 0; y < rows; y++) DrawCircle(new(px, startY + y * spacing), 1, color); }
                if (_gridPattern == GridPatternMode.Lines) for (var y = 0; y < rows; y++) { var py = startY + y * spacing; DrawLine(new(0, py), new(Size.X, py), GetThemeColor(((int)top + y) % 10 == 0 ? "grid_major" : "grid_minor")); }
            }
        }
    }
}
