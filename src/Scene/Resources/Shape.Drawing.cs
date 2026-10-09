namespace Electron2D;

public abstract partial class Shape
{
    /// <summary>Appends this shape's local diagnostic geometry to a live canvas item.</summary>
    /// <param name="canvasItem">A borrowed scene canvas-item RID or a caller-owned rendering canvas-item RID.</param>
    /// <param name="color">The finite fill or line color.</param>
    /// <remarks>Commands copy current geometry and remain until the item is cleared or redrawn. Filled shapes
    /// use the project collision-outline setting; segments and rays remain lines and arrows. An infinite boundary
    /// uses a finite marker at its normalized plane. This operation neither creates nor queries a physics world.
    /// The rendering session and its owner thread must be active; drawing from an item's draw callback is supported.</remarks>
    /// <exception cref="ObjectDisposedException">This shape is disposed.</exception>
    /// <exception cref="ArgumentException">The color is nonfinite or the RID is not a live canvas item.</exception>
    /// <exception cref="InvalidOperationException">Rendering is unavailable, off-owner, or the item belongs to another session.</exception>
    public void Draw(RID canvasItem, Color color)
    {
        ThrowIfDisposed();
        if (!color.IsFinite()) throw new ArgumentException("Shape drawing color must be finite.", nameof(color));
        RenderingServer.DrawPhysicsShape(canvasItem, this, color);
    }

    internal void DrawToCanvas(CanvasItem item, Color color)
    {
        var geometry = GetGeometry();
        Span<Vector2> points = stackalloc Vector2[26];
        var count = 0;
        switch (geometry.Kind)
        {
            case PhysicsShapeGeometry.ShapeKind.Circle:
                for (var i = 0; i < 24; i++) points[count++] = new Vector2(MathF.Cos(i * Mathf.Tau / 24), MathF.Sin(i * Mathf.Tau / 24)) * geometry.Radius;
                break;
            case PhysicsShapeGeometry.ShapeKind.Capsule:
                if (geometry.Radius == 0) { item.DrawLine(geometry.A, geometry.B, color); return; }
                for (var i = 0; i < 24; i++)
                {
                    var offset = i > 6 && i <= 18 ? geometry.A : geometry.B;
                    var radial = new Vector2(MathF.Sin(i * Mathf.Tau / 24), MathF.Cos(i * Mathf.Tau / 24)) * geometry.Radius;
                    points[count++] = radial + offset;
                    if (i is 6 or 18 && geometry.A != geometry.B) points[count++] = radial - offset;
                }
                break;
            case PhysicsShapeGeometry.ShapeKind.Rectangle:
                points[0] = geometry.A; points[1] = new(geometry.B.X, geometry.A.Y); points[2] = geometry.B; points[3] = new(geometry.A.X, geometry.B.Y); count = 4;
                break;
            case PhysicsShapeGeometry.ShapeKind.ConvexPolygon:
                DrawContour(item, geometry.Points, color); return;
            case PhysicsShapeGeometry.ShapeKind.ConcavePolygon:
                if (geometry.Points.Length > 0) item.DrawMultiline(geometry.Points, color); return;
            case PhysicsShapeGeometry.ShapeKind.Segment:
                item.DrawLine(geometry.A, geometry.B, color); return;
            case PhysicsShapeGeometry.ShapeKind.SeparationRay:
                PhysicsDebugDrawing.Arrow(item, geometry.B, color); return;
            case PhysicsShapeGeometry.ShapeKind.WorldBoundary:
                var normal = geometry.A; var center = normal * geometry.Radius; var tangent = new Vector2(-normal.Y, normal.X);
                item.DrawPolylineColors([center - tangent * 100, center - tangent * 60, center + tangent * 60, center + tangent * 100],
                    [new(color.R, color.G, color.B, 0), color, color, new(color.R, color.G, color.B, 0)], 3);
                Span<Vector2> arrow = stackalloc Vector2[7] { new(1.5f, -2.5f), new(20, -2.5f), new(20, -10), new(40, 0), new(20, 10), new(20, 2.5f), new(1.5f, 2.5f) };
                for (var i = 0; i < arrow.Length; i++) arrow[i] = center + normal * arrow[i].X + tangent * arrow[i].Y;
                item.DrawPolyline(arrow, color.Inverted(), 1.5f); return;
        }
        DrawContour(item, points[..count], color);
    }

    private static void DrawContour(CanvasItem item, ReadOnlySpan<Vector2> points, Color color)
    {
        if (points.Length < 3) return;
        item.DrawColoredPolygon(points, color);
        if (!ProjectSettings.GetWithOverride(ProjectSettings.DebugCollisionDrawOutlines)) return;
        var outline = new Color(color.R, color.G, color.B, 1);
        item.DrawPolyline(points, outline); item.DrawLine(points[^1], points[0], outline);
    }
}
