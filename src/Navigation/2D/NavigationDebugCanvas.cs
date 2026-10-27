namespace Electron2D;

/// <summary>Retains one scene navigation producer's immutable world-space diagnostic commands.</summary>
internal sealed class NavigationDebugCanvas : Entity
{
    private readonly Node _source;
    private readonly RID _rid;
    private NavigationDebugData _data;
    private Vector2[] _scratch = [];
    internal NavigationDebugCanvas(Node source, RID rid)
    {
        _source = source; _rid = rid;
        ZIndex = MaximumZIndex - 2; ZAsRelative = false;
    }
    internal override bool CanvasUsesWorldCoordinates => true;
    internal void Refresh()
    {
        var data = NavigationServer.Service.ReadDebugGeometry(_source, _rid);
        if (_data == data) return;
        _data = data; InvalidateCanvas();
    }
    /// <inheritdoc />
    protected override void OnDraw()
    {
        if (!_data.Visible) return;
        var settings = NavigationServer.Service.GeometryDebugSettings;
        if (_source is NavigationRegion) Region(settings);
        else if (_source is NavigationLink) Link(settings);
        else if (_source is NavigationObstacle) Obstacle(settings);
        else if (_source is NavigationAgent && settings.AgentsRadius && _data.Enabled && _data.Radius > 0)
            DrawCircle(_data.Start, _data.Radius, settings.AgentsRadiusColor);
    }
    private void Region(NavigationGeometryDebugSettings settings)
    {
        if (_data.Geometry is not { } geometry) return;
        var face = _data.Enabled ? settings.GeometryFaceColor : settings.GeometryFaceDisabledColor;
        var edge = _data.Enabled ? settings.GeometryEdgeColor : settings.GeometryEdgeDisabledColor;
        for (var polygonIndex = 0; polygonIndex < geometry.Polygons.Length; polygonIndex++)
        {
            var polygon = geometry.Polygons[polygonIndex];
            var color = settings.GeometryFaceRandomColor ? FaceColor(face, polygonIndex) : face;
            var a = _data.Transform * geometry.Vertices[polygon[0]];
            for (var i = 1; i + 1 < polygon.Length; i++)
            {
                var b = _data.Transform * geometry.Vertices[polygon[i]];
                var c = _data.Transform * geometry.Vertices[polygon[i + 1]];
                if (a.IsFinite() && b.IsFinite() && c.IsFinite()) DrawColoredPolygon([a, b, c], color);
            }
            if (settings.EdgeLines)
                for (var i = 0; i < polygon.Length; i++)
                {
                    var start = _data.Transform * geometry.Vertices[polygon[i]];
                    var end = _data.Transform * geometry.Vertices[polygon[(i + 1) % polygon.Length]];
                    if (start.IsFinite() && end.IsFinite()) DrawLine(start, end, edge);
                }
        }
        if (settings.EdgeConnections && _data.Connections is { } connections)
            foreach (var (start, end) in connections)
            {
                DrawLine(start, end, settings.EdgeConnectionColor);
                var radius = _data.EdgeMargin * .5f;
                if (radius > 0)
                {
                    var angle = (end - start).Angle();
                    DrawArc(start, radius, angle + MathF.PI * .5f, angle + MathF.PI * 1.5f, 10, settings.EdgeConnectionColor);
                    DrawArc(end, radius, angle - MathF.PI * .5f, angle + MathF.PI * .5f, 10, settings.EdgeConnectionColor);
                }
            }
        if (_data.BakingRect.HasArea())
        {
            var rect = _data.BakingRect; rect.Position += _data.BakingOffset;
            Span<Vector2> corners = [rect.Position, rect.Position + new Vector2(rect.Size.X, 0), rect.End, rect.Position + new Vector2(0, rect.Size.Y)];
            for (var i = 0; i < corners.Length; i++) corners[i] = _data.Transform * corners[i];
            DrawColoredPolygon(corners, new(.8f, .5f, .7f, .1f));
            for (var i = 0; i < corners.Length; i++) DrawLine(corners[i], corners[(i + 1) % corners.Length], new(.8f, .5f, .7f));
        }
    }
    private static Color FaceColor(Color color, int polygon)
    {
        // A stable per-polygon shade avoids flicker and leaves the global random stream untouched.
        var hash = unchecked((uint)(polygon + 1) * 2654435761u);
        var hue = ((hash & 65535) / 65535f * 2 - 1) * .1f;
        var value = ((hash >> 16) / 65535f * 2 - 1) * .2f;
        return Color.FromHSV(color.H + hue, color.S, color.V + value, color.A);
    }
    private void Link(NavigationGeometryDebugSettings settings)
    {
        if (!settings.LinkConnections) return;
        var color = _data.Enabled ? settings.LinkConnectionColor : settings.LinkConnectionDisabledColor;
        DrawLine(_data.Start, _data.End, color);
        if (_data.Radius > 0)
        {
            DrawArc(_data.Start, _data.Radius, 0, Mathf.Tau, 10, color);
            DrawArc(_data.End, _data.Radius, 0, Mathf.Tau, 10, color);
        }
        Arrow(_data.Start.Lerp(_data.End, .75f), _data.Start, _data.End, color, 5);
        if (_data.Bidirectional) Arrow(_data.Start.Lerp(_data.End, .25f), _data.End, _data.Start, color, 5);
    }
    private void Arrow(Vector2 anchor, Vector2 start, Vector2 end, Color color, float size)
    {
        var dx = (double)end.X - start.X; var dy = (double)end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length == 0) return;
        var direction = new Vector2((float)(dx / length), (float)(dy / length));
        var side = new Vector2(-direction.Y, direction.X);
        DrawLine(anchor, anchor + (side - direction) * size, color);
        DrawLine(anchor, anchor + (-side - direction) * size, color);
    }
    private void Obstacle(NavigationGeometryDebugSettings settings)
    {
        if (settings.ObstaclesRadius && _data.Radius > 0) DrawCircle(_data.Start, _data.Radius, settings.ObstaclesRadiusColor);
        if (!settings.ObstaclesStatic || _data.Contour is not { Length: >= 2 } points) return;
        var pushOut = points.Length < 3 || !Geometry.IsPolygonClockwise(points);
        var face = pushOut ? settings.StaticFacePushOutColor : settings.StaticFacePushInColor;
        var edge = pushOut ? settings.StaticEdgePushOutColor : settings.StaticEdgePushInColor;
        if (_scratch.Length < points.Length) _scratch = new Vector2[points.Length];
        var contour = _scratch.AsSpan(0, points.Length);
        for (var i = 0; i < points.Length; i++) contour[i] = _data.Transform * points[i];
        if (contour.Length >= 3 && face.A != 0) DrawColoredPolygon(contour, face);
        var edgeCount = contour.Length == 2 ? 1 : contour.Length;
        for (var i = 0; i < edgeCount; i++)
        {
            var start = contour[i]; var end = contour[(i + 1) % contour.Length];
            DrawLine(start, end, edge);
            var localStart = points[i]; var localEnd = points[(i + 1) % points.Length];
            var dx = (double)localEnd.X - localStart.X; var dy = (double)localEnd.Y - localStart.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length > 0)
            {
                var middle = localStart * .5f + localEnd * .5f;
                var tip = middle + new Vector2((float)(dy / length), (float)(-dx / length)) * 10;
                DrawLine(_data.Transform * middle, _data.Transform * tip, edge);
            }
        }
    }
}
