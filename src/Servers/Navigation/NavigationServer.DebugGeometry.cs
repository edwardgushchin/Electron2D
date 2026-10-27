namespace Electron2D;

public sealed partial class NavigationServer
{
    internal NavigationDebugData ReadDebugGeometry(Node source, RID rid)
    {
        if (source.IsDisposed || !source.IsInsideTree) return default;
        if (source is CanvasItem { IsVisibleInTree: false } || source is NavigationAgent { Parent: Entity { IsVisibleInTree: false } }) return default;
        var requested = GetDebugEnabled() || source.Tree!.DebugNavigationHint || Engine.IsEditorHint;
        if (!requested) return default;
        lock (_gate)
        {
            if (source is NavigationRegion)
            {
                var region = Region(rid); var map = _maps.GetValueOrDefault(region.Map);
                var baking = region.Polygon is { IsDisposed: false } polygon ? polygon.SettingsSnapshot() : null;
                return new(Visible: true, Enabled: region.Enabled, Geometry: region.Geometry, Transform: region.Transform,
                    Connections: region.UseEdgeConnections && map?.UseEdgeConnections == true ? map.Iteration.ConnectionsSnapshot(rid) : null,
                    EdgeMargin: map?.EdgeMargin ?? 0, BakingRect: baking?.BakingRect ?? default, BakingOffset: baking?.BakingRectOffset ?? default);
            }
            if (source is NavigationLink)
            {
                var link = Link(rid);
                return new(Visible: true, Enabled: link.Enabled, Start: link.Start, End: link.End,
                    Radius: _maps.GetValueOrDefault(link.Map)?.LinkRadius ?? 0, Bidirectional: link.Bidirectional);
            }
            if (source is NavigationObstacle obstacle)
            {
                var transform = obstacle.GlobalTransform; var scale = obstacle.GlobalScale.Abs();
                return new(Visible: true, Enabled: obstacle.AvoidanceEnabled, Transform: transform,
                    Start: transform.Origin, Radius: Math.Max(.001f, Math.Max(scale.X, scale.Y)) * obstacle.Radius, Contour: obstacle.DebugVertices);
            }
            if (source is NavigationAgent agent && agent.Parent is Entity actor)
                return new(Visible: true, Enabled: agent.AvoidanceEnabled, Start: actor.GlobalPosition, Radius: agent.Radius);
            return default;
        }
    }
}

internal readonly record struct NavigationDebugData(
    bool Visible = false, bool Enabled = true, NavigationPolygonData? Geometry = null, Transform Transform = default,
    Vector2 Start = default, Vector2 End = default, float Radius = 0, bool Bidirectional = false,
    (Vector2 Start, Vector2 End)[]? Connections = null, float EdgeMargin = 0,
    Vector2[]? Contour = null, Rect2 BakingRect = default, Vector2 BakingOffset = default);
