namespace Electron2D;

/// <summary>Records physics diagnostics through the owning node's ordinary retained canvas.</summary>
internal static class PhysicsDebugDrawing
{
    internal static void Refresh(CanvasItem item)
    {
        if (item is CollisionShape collision) collision.RefreshDebugGeometry();
        else if (item is ShapeCast cast) cast.RefreshDebugGeometry();
    }
    internal static bool Enabled(CanvasItem item) => item.Tree is { DebugCollisionsHint: true };
    internal static Color Disabled(Color color, bool fade = false)
    {
        var value = MathF.Max(color.R, MathF.Max(color.G, color.B));
        return new(value, value, value, fade ? color.A * .5f : color.A);
    }
    internal static void Arrow(CanvasItem item, Vector2 target, Color color)
    {
        var length = Math.Sqrt((double)target.X * target.X + (double)target.Y * target.Y);
        if (length == 0) return;
        var direction = new Vector2((float)(target.X / length), (float)(target.Y / length));
        var size = (float)(length < 1.4 ? length : Math.Clamp(length * 2 / 3, 1.4, 6));
        var root = target - direction * size; var side = new Vector2(-direction.Y, direction.X) * size * .5f;
        if (length >= 1.4) item.DrawLine(Vector2.Zero, root, color, 1.4f);
        item.DrawColoredPolygon([target, root + side, root - side], color);
    }
    internal static void OneWay(CanvasItem item, Vector2 direction, Color color, float width)
    {
        if (direction == Vector2.Zero) return;
        var end = direction * 20; var side = new Vector2(-direction.Y, direction.X) * (MathF.Sqrt(.5f) * 8);
        item.DrawLine(Vector2.Zero, end, color, width);
        item.DrawColoredPolygon([end + direction * 8, end + side, end - side], color);
    }
    internal static void Joint(Joint joint)
    {
        if (!Enabled(joint)) return;
        var color = new Color(.7f, .6f, 0, .5f);
        joint.DrawLine(new(-10, 0), new(10, 0), color, 3);
        if (joint is PinJoint) { joint.DrawLine(new(0, -10), new(0, 10), color, 3); return; }
        var length = joint is GrooveJoint groove ? groove.Length : ((DampedSpringJoint)joint).Length;
        joint.DrawLine(new(-10, length), new(10, length), color, 3);
        joint.DrawLine(Vector2.Zero, new(0, length), color, 3);
        if (joint is GrooveJoint guide) joint.DrawLine(new(-10, guide.InitialOffset), new(10, guide.InitialOffset), new(.8f, .8f, .9f, .5f), 5);
    }
}
