using Electron2D;

public static partial class BackendChecks
{
    public static void RunLogicalQueries(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { LogicalQueriesCore(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void LogicalQueriesCore(PhysicsServer.Backend? backend, bool threaded)
    {
        var owner = Thread.CurrentThread; Solver? selectedSolver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("LogicalQueries", () => selectedSolver = new Solver
        { StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Queries accompany the requested actual Solve lane") });
        using var world = backend is { } selected ? new World(selected) : new World("LogicalQueries");
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        using var ball = new CircleShape { Radius = 2 }; using var small = new CircleShape { Radius = 1 };
        using var large = new CircleShape { Radius = 10 };
        using var edges = new ConcavePolygonShape { Segments = [new(0, -3), new(0, 3), new(8, -3), new(8, 3)] };
        using var sourceEdges = new ConcavePolygonShape { Segments = [new(0, -3), new(0, 3), new(-8, -3), new(-8, 3)] };
        var bodies = new List<RID>();
        try
        {
            var low = Make(ball, Vector2.Zero, 1, false);
            PhysicsServer.BodySetShapeDisabled(low, 0, true); PhysicsServer.BodyAddShape(low, ball.GetRID()); PhysicsServer.BodyAddShape(low, ball.GetRID());
            PhysicsServer.BodySetSpace(low, world.Space); var high = Make(ball, Vector2.Zero, 1);
            var contactLow = Make(ball, new(3, 20), 2, false); var contactHigh = Make(ball, new(-3, 20), 2, false);
            PhysicsServer.BodySetSpace(contactHigh, world.Space); PhysicsServer.BodySetSpace(contactLow, world.Space);
            PhysicsServer.BodySetLinearVelocity(contactLow, new(2, 3));
            var initial = Make(ball, new(0, 40), 4); PhysicsServer.BodyAddShape(initial, ball.GetRID(), new(0, new(8, 0)));
            var later = Make(small, new(16, 40), 4);
            var logical = Make(edges, new(0, 60), 8); PhysicsServer.BodyAddShape(logical, ball.GetRID(), new(0, new(14, 0)));
            _ = Make(ball, new(0, 80), 16);
            var moving = Make(small, new(50, 0), 32);
            PhysicsServer.BodySetMode(moving, PhysicsServer.BodyMode.RigidLinear); PhysicsServer.BodySetGravityScale(moving, 0);
            PhysicsServer.BodySetCanSleep(moving, false); PhysicsServer.BodySetLinearDampMode(moving, RigidBody.DampMode.Replace);
            PhysicsServer.BodySetLinearDamp(moving, 0); PhysicsServer.BodySetLinearVelocity(moving, Vector2.Right);
            var view = world.DirectSpaceState;
            Check(ReferenceEquals(view, PhysicsServer.SpaceGetDirectState(world.Space)) && ReferenceEquals(view, root.FindWorld()!.DirectSpaceState),
                "World, viewport and server acquire one selected cached space view");
            using var ray = new PhysicsRayQueryParameters { From = new(-5, 0), To = new(5, 0), CollisionMask = 1 };
            using var point = new PhysicsPointQueryParameters { CollisionMask = 1 };
            using var overlap = new PhysicsShapeQueryParameters { Shape = small, CollisionMask = 1 };
            using var contacts = new PhysicsShapeQueryParameters { Shape = large, Transform = new(0, new(0, 20)), CollisionMask = 2 };
            using var rest = new PhysicsShapeQueryParameters { Shape = large, Transform = new(0, new(1, 20)), CollisionMask = 2 };
            using var cast = new PhysicsShapeQueryParameters { Shape = small, Transform = new(0, new(0, 40)), Motion = new(20, 0), CollisionMask = 4 };
            using var whole = new PhysicsShapeQueryParameters { Shape = small, Transform = new(0, new(0, 60)), Motion = new(20, 0), CollisionMask = 8 };
            using var reverse = new PhysicsShapeQueryParameters { Shape = sourceEdges, Transform = new(0, new(0, 80)), Motion = new(20, 0), CollisionMask = 16 };
            var points = new PhysicsPointResult[4]; var shapes = new PhysicsShapeResult[4]; var pairs = new Vector2[8];
            var failures = new List<string>();
            var hit = view.IntersectRay(ray); Probe(hit?.ColliderRID == low && hit?.ShapeIndex == 1, $"Ray tie: expected {low}/1, got {hit?.ColliderRID}/{hit?.ShapeIndex}");
            var fraction = view.CastMotion(cast); Probe(Near(fraction, .25f), $"Initial circle overlap: expected .25, got {fraction}");
            fraction = view.CastMotion(whole); Probe(Near(fraction, .55f), $"Whole logical target: expected .55, got {fraction}");
            fraction = view.CastMotion(reverse); Probe(fraction == (1, 1), $"Whole logical query: expected no new collision, got {fraction}");
            var count = view.CollideShape(contacts, pairs.AsSpan(0, 2));
            Probe(count == 1 && pairs[0].X > 0 && pairs[1].X > 0, $"Contact cap after identity ordering: expected positive X, got {count}/{pairs[0]}/{pairs[1]}");
            Check(failures.Count == 0, "Logical query contract failures: " + string.Join("; ", failures));
            var array = view.IntersectPoint(point); Check(array.Length == 3 && array[0].ColliderRID == low && array[0].ShapeIndex == 1 && array[1].ShapeIndex == 2 && array[2].ColliderRID == high,
                "Distinct logical slots are ordered once each while disabled gaps remain absent");
            Check(view.IntersectShape(overlap).Length == 3 && view.CollideShape(contacts, 1).Length == 2,
                "Array and span projections share shape identities and contact-pair caps");
            PhysicsServer.BodySetShapeDisabled(initial, 1, true);
            Check(Near(view.CastMotion(cast), .7f), "Ignoring the initial slot retains a later different body's collision");
            PhysicsServer.BodySetShapeDisabled(initial, 1, false);
            cast.Exclude = [initial]; Check(Near(view.CastMotion(cast), .7f), "Explicit exclusion retains other eligible bodies");
            cast.Exclude = [initial, later]; Check(view.CastMotion(cast) == (1, 1), "Excluding both bodies returns the full interval"); cast.Exclude = [];
            ray.From = Vector2.Zero; ray.To = new(5, 0); Check(view.IntersectRay(ray) is null, "Inside-origin rays skip all initially containing logical slots");
            ray.HitFromInside = true; hit = view.IntersectRay(ray);
            Check(hit?.ColliderRID == low && hit?.ShapeIndex == 1 && hit?.Position == Vector2.Zero && hit?.Normal == Vector2.Zero,
                "Enabled inside hits retain zero normal and the same identity tie policy");
            ray.HitFromInside = false; ray.From = new(-5, 0); ray.To = new(5, 0);
            Reject<ArgumentException>(() => PhysicsServer.SpaceGetDirectState(default));
            Reject<ArgumentException>(() => PhysicsServer.SpaceGetDirectState(low));
            Task.Run(() => Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetDirectState(world.Space))).GetAwaiter().GetResult();
            PhysicsServer.SpaceSetActive(world.Space, false);
            Check(ReferenceEquals(view, PhysicsServer.SpaceGetDirectState(world.Space)) && view.IntersectPoint(point, points) == 3,
                "Inactive acquisition retains the cached live query view"); PhysicsServer.SpaceSetActive(world.Space, true);
            if (selectedSolver is { } implementation)
            {
                var ordinary = implementation.StepHook;
                implementation.StepHook = () => { ordinary!(); Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetDirectState(world.Space)); };
                try { tree.PhysicsFrame(.01); } finally { implementation.StepHook = ordinary; }
            }
            var retiredView = view; view.Dispose(); view = PhysicsServer.SpaceGetDirectState(world.Space);
            Check(!ReferenceEquals(view, retiredView) && ReferenceEquals(view, world.DirectSpaceState), "Disposed cached views are replaced once with the same live context");
            Reject<ObjectDisposedException>(() => retiredView.IntersectPoint(point, points));
            var expiredSpace = backend is { } chosen ? PhysicsServer.SpaceCreate(chosen) : PhysicsServer.SpaceCreate("LogicalQueries");
            var expiredView = PhysicsServer.SpaceGetDirectState(expiredSpace); PhysicsServer.FreeRID(expiredSpace);
            Reject<ArgumentException>(() => PhysicsServer.SpaceGetDirectState(expiredSpace));
            Reject<ArgumentException>(() => expiredView.IntersectPoint(point, points)); expiredView.Dispose();
            for (var i = 0; i < 128; i++) Frame(); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Logical queries allocated {own}/{all} B");
            Console.WriteLine($"Logical queries {backend?.ToString() ?? "registered"}, separate={threaded}: six query families, logical origins, identity/caps and 128 moving full frames {own}/{all} B.");
            void Probe(bool accepted, string message) { if (!accepted) { failures.Add(message); Console.WriteLine(message); } }
            void Frame()
            {
                var before = PhysicsServer.BodyGetTransform(moving).Origin; tree.PhysicsFrame(.01);
                Check(MathF.Abs(PhysicsServer.BodyGetTransform(moving).Origin.X - before.X - .01f) < .0001f, "A complete selected interval advances the moving control");
                var current = view.IntersectRay(ray);
                Check(current?.ColliderRID == low && current?.ShapeIndex == 1, "Warm ray retains the lowest physical identity and logical slot");
                Check(view.IntersectPoint(point, points) == 3 && points[0].ShapeIndex == 1 && points[2].ColliderRID == high,
                    "Warm point projection retains ordered logical identities");
                Check(view.IntersectShape(overlap, shapes) == 3 && shapes[0].ColliderRID == low && shapes[0].ShapeIndex == 1,
                    "Warm shape projection retains ordered logical identities");
                Check(view.CollideShape(contacts, pairs.AsSpan(0, 2)) == 1 && pairs[0].X > 0 && pairs[1].X > 0,
                    "Warm contact cap follows identity ordering");
                var info = view.GetRestInfo(rest);
                Check(info?.ColliderRID == contactLow && info?.LinearVelocity.DistanceTo(new(2, 3)) < .001f,
                    "Deepest rest contact retains actual static surface velocity");
                Check(Near(view.CastMotion(cast), .25f) && Near(view.CastMotion(whole), .55f) && view.CastMotion(reverse) == (1, 1),
                    "Warm casts ignore whole initially intersecting logical slots");
            }
        }
        finally { foreach (var body in bodies) PhysicsServer.FreeRID(body); }

        RID Make(Shape shape, Vector2 position, uint layer, bool attach = true)
        {
            var body = PhysicsServer.BodyCreate(); bodies.Add(body); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetTransform(body, new(0, position));
            PhysicsServer.BodySetCollisionLayer(body, layer); PhysicsServer.BodySetCollisionMask(body, 0);
            if (attach) PhysicsServer.BodySetSpace(body, world.Space); return body;
        }
        static bool Near((float SafeFraction, float UnsafeFraction) value, float expected) =>
            value.SafeFraction <= expected + .0001f && value.UnsafeFraction >= expected - .0001f && value.UnsafeFraction - value.SafeFraction < .01f;
    }
}
