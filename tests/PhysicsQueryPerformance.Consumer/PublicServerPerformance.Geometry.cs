using System.Reflection;
using Electron2D;

internal static partial class PublicServerPerformance
{
    private static Func<int, int> GeometryCase(MethodInfo method)
    {
        Vector2[] triangle = [new(-1, -1), new(1, -1), new(0, 1)];
        Vector2[] segment = [new(-1, 0), new(1, 0)];
        var circle = ShapeGeometry.Circle(1); var capsule = ShapeGeometry.Capsule(1, 4);
        var line = ShapeGeometry.Segment(segment[0], segment[1]); var rectangle = ShapeGeometry.Rectangle(new(2, 2));
        var convex = ShapeGeometry.ConvexPolygon(triangle); var concave = ShapeGeometry.ConcavePolygon(segment);
        var ray = ShapeGeometry.SeparationRay(2); var boundary = ShapeGeometry.WorldBoundary(Vector2.Up, 0);
        ShapeCollisionAlgorithm collision = Empty;
        var custom = ShapeGeometry.Custom("benchmark.empty/v1", [], default, new(1, 1, Vector2.Zero), [], collision);
        return method.Name switch
        {
            "Circle" => _ => { ResultSink<ShapeGeometry>.Value = ShapeGeometry.Circle(1); return 1; }
            ,
            "Capsule" => _ => { ResultSink<ShapeGeometry>.Value = ShapeGeometry.Capsule(1, 4); return 1; }
            ,
            "Segment" => _ => { ResultSink<ShapeGeometry>.Value = ShapeGeometry.Segment(segment[0], segment[1]); return 1; }
            ,
            "Rectangle" => _ => { ResultSink<ShapeGeometry>.Value = ShapeGeometry.Rectangle(new(2, 2)); return 1; }
            ,
            "ConvexPolygon" => _ => { ResultSink<ShapeGeometry>.Value = ShapeGeometry.ConvexPolygon(triangle); return 1; }
            ,
            "ConcavePolygon" => _ => { ResultSink<ShapeGeometry>.Value = ShapeGeometry.ConcavePolygon(segment); return 1; }
            ,
            "SeparationRay" => _ => { ResultSink<ShapeGeometry>.Value = ShapeGeometry.SeparationRay(2); return 1; }
            ,
            "WorldBoundary" => _ => { ResultSink<ShapeGeometry>.Value = ShapeGeometry.WorldBoundary(Vector2.Up, 0); return 1; }
            ,
            "Custom" => _ => { ResultSink<ShapeGeometry>.Value = ShapeGeometry.Custom("benchmark.empty/v1", [], default, new(1, 1, Vector2.Zero), [], collision); return 1; }
            ,
            "GetCircleRadius" => _ => { ResultSink<float>.Value = circle.GetCircleRadius(); return 1; }
            ,
            "GetCapsuleDimensions" => _ => { ResultSink<(float, float)>.Value = capsule.GetCapsuleDimensions(); return 1; }
            ,
            "GetSegmentEndpoints" => _ => { ResultSink<(Vector2, Vector2)>.Value = line.GetSegmentEndpoints(); return 1; }
            ,
            "GetRectangleSize" => _ => { ResultSink<Vector2>.Value = rectangle.GetRectangleSize(); return 1; }
            ,
            "GetConvexPolygonPoints" => _ => { ResultSink<int>.Value = convex.GetConvexPolygonPoints().Length; return 1; }
            ,
            "GetConcavePolygonSegments" => _ => { ResultSink<int>.Value = concave.GetConcavePolygonSegments().Length; return 1; }
            ,
            "GetSeparationRayParameters" => _ => { ResultSink<(float, bool)>.Value = ray.GetSeparationRayParameters(); return 1; }
            ,
            "GetWorldBoundaryPlane" => _ => { ResultSink<(Vector2, float)>.Value = boundary.GetWorldBoundaryPlane(); return 1; }
            ,
            "GetCustomFormatID" => _ => { ResultSink<string>.Value = custom.GetCustomFormatID(); return 1; }
            ,
            "GetCustomData" => _ => { ResultSink<int>.Value = custom.GetCustomData().Length; return 1; }
            ,
            "GetCustomMassProperties" => _ => { ResultSink<PhysicsMassProperties>.Value = custom.GetCustomMassProperties(); return 1; }
            ,
            "GetCustomDrawingSegments" => _ => { ResultSink<int>.Value = custom.GetCustomDrawingSegments().Length; return 1; }
            ,
            _ => throw new InvalidOperationException("Missing public immutable geometry case: " + method)
        };

        static bool Empty(ShapeGeometry first, Transform firstPose, Vector2 firstMotion, ShapeGeometry second,
            Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count)
        { count = 0; return false; }
    }
}
