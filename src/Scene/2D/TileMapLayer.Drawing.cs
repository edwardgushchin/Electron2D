namespace Electron2D;

public partial class TileMapLayer
{
    private double TileAnimationPhase(Vector2i coords)
    {
        var hash = unchecked(InstanceID ^ (ulong)(uint)coords.X * 0x9E3779B185EBCA87UL ^ (ulong)(uint)coords.Y * 0xC2B2AE3D27D4EB4FUL);
        hash ^= hash >> 30; hash = unchecked(hash * 0xBF58476D1CE4E5B9UL); hash ^= hash >> 27; hash = unchecked(hash * 0x94D049BB133111EBUL); hash ^= hash >> 31;
        return (hash >> 11) * (1d / 9007199254740992d);
    }
    /// <inheritdoc />
    protected override void OnDraw()
    {
        if (!_enabled || _tileSet is null) return;
        foreach (var pair in _cells)
        {
            var cell = pair.Value;
            if (Resolve(cell) is not { } source || source.GetRuntimeTexture() is not { IsDisposed: false } texture || EffectiveData(pair.Key, cell) is not { IsDisposed: false } data) continue;
            var region = source.GetRuntimeTileTextureRegion(cell.Atlas, 0);
            var flags = (data.FlipH ? TileSetAtlasSource.TransformFlipH : 0) | (data.FlipV ? TileSetAtlasSource.TransformFlipV : 0) | (data.Transpose ? TileSetAtlasSource.TransformTranspose : 0);
            var x = CellPoint(CellPoint(Vector2.Right, flags), cell.Alternative);
            var y = CellPoint(CellPoint(Vector2.Down, flags), cell.Alternative);
            DrawSetTransformMatrix(new Transform(x, y, MapToLocal(pair.Key) - CellPoint(data.TextureOrigin, cell.Alternative)));
            var size = (Vector2)region.Size;
            var count = source.GetTileAnimationFramesCount(cell.Atlas);
            if (count == 1) DrawTextureRectRegion(texture, new Rect2(-size * .5f, size), new Rect2(region.Position, region.Size), data.Modulate);
            else
            {
                var speed = source.GetTileAnimationSpeed(cell.Atlas); double total = 0;
                for (var frame = 0; frame < count; frame++) total += source.GetTileAnimationFrameDuration(cell.Atlas, frame);
                var length = total / speed; var offset = source.GetTileAnimationMode(cell.Atlas) == TileSetAtlasSource.TileAnimationMode.RandomStartTimes ? TileAnimationPhase(pair.Key) * length : 0; double elapsed = 0;
                for (var frame = 0; frame < count; frame++)
                {
                    var duration = source.GetTileAnimationFrameDuration(cell.Atlas, frame); DrawAnimationSlice(length, elapsed / speed, (elapsed + duration) / speed, offset);
                    region = source.GetRuntimeTileTextureRegion(cell.Atlas, frame); DrawTextureRectRegion(texture, new Rect2(-size * .5f, size), new Rect2(region.Position, region.Size), data.Modulate); elapsed += duration;
                }
                DrawEndAnimation();
            }
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
        DrawNavigation();
        DrawSetTransformMatrix(Transform.Identity);
    }
}
