using Electron2D;

public static partial class BackendChecks
{
    private static void CustomDataGuards()
    {
        static bool Miss(ShapeGeometry a, Transform pa, Vector2 ma, ShapeGeometry b, Transform pb, Vector2 mb, Span<Vector2> output, out int count)
        { count = 0; return false; }
        byte[] bytes = [1, 2, 3]; Vector2[] lines = [Vector2.Zero, Vector2.Right];
        var data = ShapeGeometry.Custom("guard/v1", bytes, new(0, 0, 1, 1), new(1, 1, Vector2.Zero), lines, Miss);
        bytes[0] = 99; lines[0] = Vector2.Down;
        Check(data.GetCustomData()[0] == 1 && data.GetCustomDrawingSegments()[0] == Vector2.Zero, "Custom canonical and diagnostic data are copied");
        Reject<ArgumentException>(() => ShapeGeometry.Custom("", [], default, new(1, 1, Vector2.Zero), [], Miss));
        Reject<ArgumentException>(() => ShapeGeometry.Custom("v1", [], new(0, 0, -1, 1), new(1, 1, Vector2.Zero), [], Miss));
        Reject<ArgumentException>(() => ShapeGeometry.Custom("v1", [], default, new(1, 1, Vector2.Zero), [Vector2.Zero], Miss));
        Reject<ArgumentOutOfRangeException>(() => ShapeGeometry.Custom("v1", [], default, new(0, 1, Vector2.Zero), [], Miss));
        Reject<ArgumentException>(() => new CustomShape(ShapeGeometry.Circle(1)));
        using var circle = new CircleShape { Radius = 1 };
        var nested = 0;
        using var source = new CustomShape(ShapeGeometry.Custom("nested/v1", [], default, new(1, 1, Vector2.Zero), [], Nested));
        bool Nested(ShapeGeometry a, Transform pa, Vector2 ma, ShapeGeometry b, Transform pb, Vector2 mb, Span<Vector2> output, out int count)
        {
            nested++; using var left = new CircleShape { Radius = 1 }; using var right = new CircleShape { Radius = 1 };
            Check(left.Collide(Transform.Identity, right, Transform.Identity), "Different nested geometry pair remains queryable");
            count = output.Length == 0 ? 0 : 1; if (count > 0) { output[0] = Vector2.Right; output[1] = Vector2.Down; }
            return true;
        }
        var forward = source.CollideAndGetContacts(Transform.Identity, circle, Transform.Identity);
        var reverse = circle.CollideAndGetContacts(Transform.Identity, source, Transform.Identity);
        Check(nested == 2 && forward[0] == reverse[1] && forward[1] == reverse[0], "Custom argument reversal preserves contact ownership through nested queries");
        CustomShape? recursive = null;
        bool Recurse(ShapeGeometry a, Transform pa, Vector2 ma, ShapeGeometry b, Transform pb, Vector2 mb, Span<Vector2> output, out int count)
        { count = 0; recursive!.Collide(pa, circle, pb); return false; }
        using (recursive = new CustomShape(ShapeGeometry.Custom("recursive/v1", [], default, new(1, 1, Vector2.Zero), [], Recurse)))
            Reject<InvalidOperationException>(() => recursive.Collide(Transform.Identity, circle, Transform.Identity));
        using var bad = new CustomShape(ShapeGeometry.Custom("bad/v1", [], default, new(1, 1, Vector2.Zero), [], Invalid));
        static bool Invalid(ShapeGeometry a, Transform pa, Vector2 ma, ShapeGeometry b, Transform pb, Vector2 mb, Span<Vector2> output, out int count)
        { count = output.Length / 2 + 1; return true; }
        Reject<InvalidOperationException>(() => bad.CollideAndGetContacts(Transform.Identity, circle, Transform.Identity));
        using var recovered = new CustomShape(data);
        Check(!recovered.Collide(Transform.Identity, circle, Transform.Identity), "Failed callback pair releases reusable query state");
    }
}
