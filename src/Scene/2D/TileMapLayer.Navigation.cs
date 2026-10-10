namespace Electron2D;

public partial class TileMapLayer
{
    internal Vector2[][] CaptureNavigationContours(uint collisionMask)
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread(); var result = new List<Vector2[]>();
        if (!_enabled || !_collisionEnabled || _tileSet is not { IsDisposed: false } set) return [];
        foreach (var (coords, cell) in _cells)
        {
            if (EffectiveData(coords, cell) is not { IsDisposed: false } data) continue;
            for (var layer = 0; layer < Math.Min(set.GetPhysicsLayersCount(), data.Layers.Count); layer++) if ((set.GetPhysicsLayerCollisionLayer(layer) & collisionMask) != 0)
                    for (var index = 0; index < data.GetCollisionPolygonsCount(layer); index++)
                    {
                        var points = data.GetCollisionPolygonPoints(layer, index); if (points.Length < 3 || Geometry.DecomposePolygonInConvex(points).Length == 0) continue; var origin = MapToLocal(coords);
                        for (var i = 0; i < points.Length; i++) points[i] = CellPoint(points[i], cell.Alternative) + origin;
                        result.Add(points);
                    }
        }
        return result.ToArray();
    }
}
