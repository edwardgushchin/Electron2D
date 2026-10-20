using Electron2D;

public static partial class BackendChecks
{
    private static void GeneralCasts(World world, SubViewport root)
    {
        using var rectangle = new RectangleShape { Size = new(2, 2) };
        using var capsule = new CapsuleShape { Radius = 1, Height = 6 };
        using var segment = new SegmentShape { A = new(0, -2), B = new(0, 2) };
        using var convex = new ConvexPolygonShape { Points = [new(-1, -1), new(1, -1), new(1, 1), new(-1, 1)] };
        using var concave = new ConcavePolygonShape { Segments = [new(0, -2), new(0, 2)] };
        using var probe = new CircleShape { Radius = 1 };
        using var sharpProbe = new SegmentShape { A = new(0, -1), B = new(0, 1) };
        var body = new StaticBody { Name = "GeneralCast", Position = new(180, 20) };
        var slot = new CollisionShape { Shape = rectangle }; body.AddChild(slot); root.AddChild(body);
        using var query = new PhysicsShapeQueryParameters { Shape = probe, Transform = new(0, new Vector2(170, 20)), Motion = new(20, 0), Margin = 0 };
        try
        {
            foreach (var queryShape in new Shape[] { probe, sharpProbe })
                foreach (var shape in new Shape[] { rectangle, capsule, segment, convex, concave })
                {
                    query.Shape = queryShape; slot.Shape = shape;
                    var extent = shape == segment || shape == concave ? 0 : 1;
                    var analytic = (10f - extent - (queryShape == probe ? 1 : 0)) / 20;
                    Verify(); for (var i = 0; i < 128; i++) Verify(); Collect();
                    var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                    for (var i = 0; i < 128; i++) Verify();
                    own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                    Check(own == 0 && all == 0, $"General geometric casts allocate {own}/{all} B");
                    void Verify()
                    {
                        var cast = world.DirectSpaceState.CastMotion(query);
                        if (!(cast.SafeFraction <= analytic + .00001f && cast.UnsafeFraction >= analytic - .00001f &&
                            cast.UnsafeFraction - cast.SafeFraction <= 3f / 512 + .00001f && MathF.Abs(cast.UnsafeFraction - analytic) <= 1f / 256 + .00001f))
                            throw new InvalidOperationException($"{queryShape.GetType().Name}/{shape.GetType().Name} cast first touch {analytic}: {cast.SafeFraction}/{cast.UnsafeFraction}");
                    }
                }
            Console.WriteLine($"General geometric casts {world.PhysicsBackendName}: ten rounded/sharp proxy combinations, eight-refinement first touch and 128 warm queries each at 0/0 B passed.");
        }
        finally { root.RemoveChild(body); body.Dispose(); }
    }
}
