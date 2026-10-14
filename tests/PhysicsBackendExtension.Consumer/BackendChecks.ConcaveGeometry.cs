using Electron2D;

public static partial class BackendChecks
{
    private static int GeometryCount(ShapeGeometry geometry) => geometry.Type == PhysicsServer.ShapeType.ConcavePolygon
        ? geometry.GetConcavePolygonSegments().Length / 2 : 1;
    internal static bool CollideConcaveGeometry(ShapeGeometry first, Transform firstPose, Vector2 firstMotion,
        ShapeGeometry second, Transform secondPose, Vector2 secondMotion, Span<Vector2> output, out int count)
    {
        count = 0;
        var ca = first.Type == PhysicsServer.ShapeType.ConcavePolygon; var cb = second.Type == PhysicsServer.ShapeType.ConcavePolygon;
        if (ca && cb) return false;
        if (ca) firstMotion = Vector2.Zero; if (cb) secondMotion = Vector2.Zero;
        var hit = false; Span<Vector2> pair = stackalloc Vector2[2];
        for (var i = 0; i < GeometryCount(first); i++) for (var j = 0; j < GeometryCount(second); j++)
            {
                var a = GeometryPart(first, firstPose, 0, i); var b = GeometryPart(second, secondPose, 0, j);
                if (!CollidePartsRegions(a, firstMotion, b, secondMotion, pair, out var written)) continue;
                hit = true;
                if (written == 0 || output.Length < 2) continue;
                var depth = pair[0].DistanceSquaredTo(pair[1]); var slot = count;
                if (count == output.Length / 2)
                {
                    slot = 0;
                    for (var k = 1; k < count; k++)
                        if (output[2 * k].DistanceSquaredTo(output[2 * k + 1]) < output[2 * slot].DistanceSquaredTo(output[2 * slot + 1])) slot = k;
                    if (depth <= output[2 * slot].DistanceSquaredTo(output[2 * slot + 1])) continue;
                }
                else count++;
                output[2 * slot] = pair[0]; output[2 * slot + 1] = pair[1];
            }
        return hit;
    }
    private static bool PriorAreaPair(Body a, int ai, Body b, int bi)
    {
        for (var i = 0; i <= ai; i++)
        {
            if (a.Parts[i].Index != a.Parts[ai].Index) continue;
            var end = i == ai ? bi : b.Shapes;
            for (var j = 0; j < end; j++)
                if (b.Parts[j].Index == b.Parts[bi].Index && Solver.PartsOverlap(a, a.Parts[i], b, b.Parts[j])) return true;
        }
        return false;
    }
}
