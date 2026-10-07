using Box2D.NET;
using static Box2D.NET.B2ContactSolvers;

internal static class Box2DSIMDTests
{
    internal static void Run()
    {
        VerifyPolygonSeparation();
        var random = new Random(712);
        float[] edge = [0, -0f, 1, -1, float.Epsilon, -float.Epsilon, float.MaxValue, float.MinValue, float.PositiveInfinity, float.NegativeInfinity, float.NaN];
        for (var i = 0; i < 4096; i++)
        {
            var a = new B2FloatW(); var b = a; var c = a;
            for (var lane = 0; lane < Box2D.NET.B2Cores.B2_SIMD_WIDTH; lane++)
            {
                a[lane] = i < edge.Length * edge.Length ? edge[i % edge.Length] : (float)(random.NextDouble() * 200 - 100);
                b[lane] = i < edge.Length * edge.Length ? edge[i / edge.Length] : (float)(random.NextDouble() * 200 - 100);
                c[lane] = i < edge.Length * edge.Length ? edge[(i + lane) % edge.Length] : (float)(random.NextDouble() * 200 - 100);
            }
            var add = b2AddW(a, b); var sub = b2SubW(a, b); var mul = b2MulW(a, b);
            var madd = b2MulAddW(a, b, c); var msub = b2MulSubW(a, b, c);
            var min = b2MinW(a, b); var max = b2MaxW(a, b); var clamp = b2SymClampW(a, b);
            var or = b2OrW(a, b); var greater = b2GreaterThanW(a, b); var equal = b2EqualsW(a, b); var blend = b2BlendW(a, b, c);
            var zero = true;
            for (var lane = 0; lane < Box2D.NET.B2Cores.B2_SIMD_WIDTH; lane++)
            {
                Equal(add[lane], a[lane] + b[lane]); Equal(sub[lane], a[lane] - b[lane]); Equal(mul[lane], a[lane] * b[lane]);
                Equal(madd[lane], a[lane] + b[lane] * c[lane]); Equal(msub[lane], a[lane] - b[lane] * c[lane]);
                Equal(min[lane], a[lane] <= b[lane] ? a[lane] : b[lane], true);
                Equal(max[lane], a[lane] >= b[lane] ? a[lane] : b[lane], true);
                Equal(clamp[lane], a[lane] < -b[lane] ? -b[lane] : a[lane] > b[lane] ? b[lane] : a[lane], true);
                Equal(or[lane], a[lane] != 0 || b[lane] != 0 ? 1 : 0); Equal(greater[lane], a[lane] > b[lane] ? 1 : 0);
                Equal(equal[lane], a[lane] == b[lane] ? 1 : 0); Equal(blend[lane], c[lane] != 0 ? b[lane] : a[lane], true);
                zero &= a[lane] == 0;
            }
            if (b2AllZeroW(a) != zero) throw new InvalidOperationException("SIMD zero reduction differs from scalar arithmetic.");
        }
    }

    private static void VerifyPolygonSeparation()
    {
        var random = new Random(174);
        float[] edges = [0, -0f, float.Epsilon, -float.Epsilon, float.MaxValue, float.MinValue, float.PositiveInfinity, float.NegativeInfinity, float.NaN];
        for (var sample = 0; sample < 4096; sample++)
        {
            var first = new B2Polygon { count = 2 + sample % 7 };
            var second = new B2Polygon { count = sample % 8 == 0 ? 2 + sample % 7 : 4 };
            for (var i = 0; i < first.count; i++)
            {
                first.vertices[i] = new((float)random.NextDouble() * 20 - 10, (float)random.NextDouble() * 20 - 10);
                first.normals[i] = new((float)random.NextDouble() * 2 - 1, (float)random.NextDouble() * 2 - 1);
            }
            for (var i = 0; i < second.count; i++)
                second.vertices[i] = new((float)random.NextDouble() * 20 - 10, (float)random.NextDouble() * 20 - 10);
            if (sample < edges.Length * edges.Length)
            {
                first.normals[0] = new(edges[sample % edges.Length], edges[sample / edges.Length]);
                second.vertices[0] = new(edges[(sample + 3) % edges.Length], edges[(sample + 5) % edges.Length]);
            }
            var expectedEdge = 0; var expected = -float.MaxValue;
            for (var i = 0; i < first.count; i++)
            {
                var normal = first.normals[i]; var origin = first.vertices[i]; var minimum = float.MaxValue;
                for (var j = 0; j < second.count; j++)
                {
                    var point = second.vertices[j];
                    var value = normal.X * (point.X - origin.X) + normal.Y * (point.Y - origin.Y);
                    if (value < minimum) minimum = value;
                }
                if (minimum > expected) { expected = minimum; expectedEdge = i; }
            }
            var actualEdge = -1;
            var actual = B2Manifolds.b2FindMaxSeparation(ref actualEdge, ref first, ref second);
            Equal(actual, expected, true);
            if (actualEdge != expectedEdge) throw new InvalidOperationException("Polygon separation chose a different edge.");
        }
    }

    private static void Equal(float actual, float expected, bool preserveBits = false)
    {
        if (BitConverter.SingleToInt32Bits(actual) == BitConverter.SingleToInt32Bits(expected) ||
            !preserveBits && float.IsNaN(actual) && float.IsNaN(expected)) return;
        throw new InvalidOperationException($"SIMD differs from scalar arithmetic: {actual:R} versus {expected:R}.");
    }
}
