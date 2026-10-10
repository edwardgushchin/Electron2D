namespace Electron2D;

public partial class TileMapLayer
{
    /// <inheritdoc />
    protected override void OnDraw()
    {
        if (!_enabled || _tileSet is null) return;
        foreach (var pair in _cells)
        {
            var cell = pair.Value;
            if (Resolve(cell) is not { Texture: { IsDisposed: false } texture } source || EffectiveData(pair.Key, cell) is not { IsDisposed: false } data) continue;
            var region = source.GetTileTextureRegion(cell.Atlas);
            var flags = (data.FlipH ? TileSetAtlasSource.TransformFlipH : 0) | (data.FlipV ? TileSetAtlasSource.TransformFlipV : 0) | (data.Transpose ? TileSetAtlasSource.TransformTranspose : 0);
            var x = CellPoint(CellPoint(Vector2.Right, flags), cell.Alternative);
            var y = CellPoint(CellPoint(Vector2.Down, flags), cell.Alternative);
            DrawSetTransformMatrix(new Transform(x, y, MapToLocal(pair.Key) - CellPoint(data.TextureOrigin, cell.Alternative)));
            var size = (Vector2)region.Size;
            DrawTextureRectRegion(texture, new Rect2(-size * .5f, size), new Rect2(region.Position, region.Size), data.Modulate);
        }
        if (_collisionVisibility == DebugVisibilityMode.ForceShow || _collisionVisibility == DebugVisibilityMode.Default && Tree?.DebugCollisionsHint == true)
        {
            var color = Tree?.DebugCollisionsColor ?? new Color(.2f, .6f, 1f, .4f);
            foreach (var bodies in _quadrants.Values) foreach (var body in bodies)
                {
                    DrawSetTransformMatrix(body.DrawTransform);
                    foreach (var shape in body.Shapes) shape.DrawToCanvas(this, color);
                }
        }
        DrawSetTransformMatrix(Transform.Identity);
    }
}
