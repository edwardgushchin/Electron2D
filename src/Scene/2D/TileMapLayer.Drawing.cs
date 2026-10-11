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
        if (!_enabled || _tileSet is null) { ReleaseDrawing(); ReleaseDrawingDiagnostics(); return; }
        PrepareTileDrawing();
        var target = DrawingDiagnosticsTarget();
        if (target is null) return;
        using var scope = target.StartServerDrawing();
        if (_collisionVisibility == DebugVisibilityMode.ForceShow || _collisionVisibility == DebugVisibilityMode.Default && Tree?.DebugCollisionsHint == true)
        {
            var color = Tree?.DebugCollisionsColor ?? new Color(.2f, .6f, 1f, .4f);
            foreach (var bodies in _quadrants.Values) foreach (var body in bodies)
                {
                    target.DrawSetTransformMatrix(body.DrawTransform);
                    foreach (var shape in body.Shapes) shape.DrawToCanvas(target, color);
                }
        }
        DrawNavigation(target);
        target.DrawSetTransformMatrix(Transform.Identity);
    }
}
