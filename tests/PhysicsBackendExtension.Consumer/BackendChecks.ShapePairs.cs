using Electron2D;

public static partial class BackendChecks
{
    public static void RunShapePairs(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { ShapePairChecks(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void ShapePairChecks(PhysicsServer.Backend? backend, bool threaded)
    {
        Solver? factory = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ShapePairs", () => factory = new());
        using var world = backend is { } selected ? new World(selected) : new World("ShapePairs");
        Check(world.PhysicsBackendName == (backend?.ToString() ?? "ShapePairs"), "Shape pairs use the selected implementation");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 }; using var small = new CircleShape { Radius = 1 };
        using var scene = new StaticBody { Position = new(20, 10), Rotation = .5f, CollisionLayer = 0, CollisionMask = 0 };
        var disabledShape = new CollisionShape { Name = "Disabled", Shape = small, Disabled = true };
        scene.AddChild(disabledShape);
        scene.AddChild(new CollisionShape { Name = "Offset", Shape = circle, Position = new(10, 0), Rotation = .25f });
        var scenePose = scene.GlobalTransform * new Transform(.25f, new(10, 0));
        Check(PhysicsServer.BodyCollideShape(scene.GetRID(), 1, circle.GetRID(), scenePose, Vector2.Zero, [], out var count) && count == 0,
            "Detached scene logical pairs execute with zero output capacity");
        root.AddChild(scene); using var tree = new SceneTree(root);
        var moving = new RigidBody
        {
            Position = new(0, 1000),
            LinearVelocity = Vector2.Right,
            GravityScale = 0,
            CanSleep = false,
            CollisionLayer = 0,
            CollisionMask = 0,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace
        };
        moving.AddChild(new CollisionShape { Shape = small }); root.AddChild(moving);
        var space = world.Space; var solver = factory;
        var raw = PhysicsServer.BodyCreate(); var detached = PhysicsServer.BodyCreate(); var nestedSpace = default(RID);
        var output = new Vector2[8]; var nestedOutput = new Vector2[4]; var otherOutput = new Vector2[4];
        var sentinel = new Vector2(701, 709); var near = new Transform(0, new Vector2(5, 0));
        var rawPose = new Transform(.2f, new Vector2(100, 10)); var rawSlot = new Transform(.3f, new Vector2(10, 0));
        var rawPairPose = rawPose * rawSlot; var detachedPose = new Transform(0, new Vector2(60, 10));
        try
        {
            foreach (var body in new[] { raw, detached })
            {
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(body, 0); PhysicsServer.BodySetCollisionMask(body, 0);
                PhysicsServer.BodyAddShape(body, small.GetRID(), disabled: true); PhysicsServer.BodyAddShape(body, circle.GetRID(), rawSlot);
            }
            PhysicsServer.BodySetTransform(raw, rawPose); PhysicsServer.BodySetSpace(raw, space);
            PhysicsServer.BodySetTransform(detached, detachedPose); tree.PhysicsFrame(1d / 120);
            Fill(); Check(Pair(near, output, out count) && count > 0, "Public selected pair reaches the chosen owner");
            for (var i = count * 2; i < output.Length; i++) Check(output[i] == sentinel, "Unused contact tail stays untouched");
            Check(output[0].DistanceTo(Vector2.Zero) is >= 2.9999f and <= 3.0001f &&
                output[1].DistanceTo(near.Origin) is >= 2.9999f and <= 3.0001f, "Contact order and circle boundaries use scene units");
            Fill(); Check(!Pair(new(0, new Vector2(1000, 0)), output, out count) && count == 0, "Disjoint pair reports no contacts"); Unchanged();
            Check(Pair(near, [], out count) && count == 0, "Zero capacity retains collision truth");
            Check(Pair(near, output.AsSpan(0, 2), out count) && count == 1, "Caller pair capacity is respected");
            Check(PhysicsServer.BodyCollideShape(scene.GetRID(), 0, small.GetRID(), scene.GlobalTransform, Vector2.Zero, [], out count),
                "Explicit logical pairs include disabled slots and ignore zero filters");
            foreach (var invalidSlot in new[] { new Transform(0, new Vector2(2, 1), 0, Vector2.Zero), new Transform(0, Vector2.One, .2f, Vector2.Zero) })
            {
                disabledShape.Transform = invalidSlot; Fill(); var published = 997; var pairQueries = solver?.PairQueries ?? 0;
                Exception? error = null; bool? hit = null;
                try { hit = PhysicsServer.BodyCollideShape(scene.GetRID(), 0, small.GetRID(), scene.GlobalTransform, Vector2.Zero, output, out published); }
                catch (Exception failure) { error = failure; }
                finally { disabledShape.Transform = Transform.Identity; }
                Check(error is ArgumentException && published == 997 && (solver?.PairQueries ?? 0) == pairQueries,
                    $"Unsupported composed body-pair pose: error={error?.Message ?? "none"}, hit={hit}, count={published}"); Unchanged();
            }
            Reject<ArgumentException>(() => Pair(near, new Vector2[3], out _));
            Reject<ArgumentException>(() => PhysicsServer.ShapeCollide(raw, circle.GetRID(), Transform.Identity, Vector2.Zero, circle.GetRID(), near, Vector2.Zero, output, out _));
            Reject<ArgumentException>(() => PhysicsServer.ShapeCollide(space, raw, Transform.Identity, Vector2.Zero, circle.GetRID(), near, Vector2.Zero, output, out _));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyCollideShape(raw, 2, circle.GetRID(), near, Vector2.Zero, output, out _));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.ShapeCollide(space, circle.GetRID(), Transform.Identity, new(float.NaN, 0), circle.GetRID(), near, Vector2.Zero, output, out _));
            if (solver is not null)
            {
                var committedPose = new Transform(.2f, new Vector2(200, 10));
                var poseError = new IOException("pair-pose-prepare"); var poseAttempts = 0;
                solver.PoseHook = id =>
                {
                    if (id != raw) return;
                    poseAttempts++; solver.Bodies[id].Position = new(-500, -500); throw poseError;
                };
                try
                {
                    var errors = ReleaseErrors(() => PhysicsServer.BodySetTransform(raw, committedPose));
                    Check(errors.Length == 1 && ReferenceEquals(errors[0], poseError), "Raw pair setup retains the original pose publication error");
                    var tick = PhysicsServer.SpaceGetTick(space);
                    Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyCollideShape(raw, -1, circle.GetRID(), committedPose, Vector2.Zero, output, out _));
                    Reject<ArgumentException>(() => PhysicsServer.BodyCollideShape(raw, 1, raw, committedPose, Vector2.Zero, output, out _));
                    Check(Pair(near, output, out _) && poseAttempts == 1, "Resource-only pairs and invalid body inputs do not repair unrelated numerical debt");
                    Fill(); var published = 997;
                    Exception? queryError = null; bool? queryHit = null;
                    try { queryHit = PhysicsServer.BodyCollideShape(raw, 1, circle.GetRID(), committedPose * rawSlot, Vector2.Zero, output, out published); }
                    catch (Exception error) { queryError = error; }
                    Check(ReferenceEquals(queryError, poseError) && poseAttempts == 2 && published == 997,
                        $"Shape-pair pose preparation: attempts={poseAttempts}, error={queryError?.Message ?? "none"}, hit={queryHit}, count={published}");
                    Unchanged(); solver.PoseHook = null;
                    Check(PhysicsServer.BodyCollideShape(raw, 1, circle.GetRID(), committedPose * rawSlot, Vector2.Zero, output, out published) && published > 0 &&
                        MathF.Abs(output[0].DistanceTo((committedPose * rawSlot).Origin) - 3) < .001f && PhysicsServer.SpaceGetTick(space) == tick,
                        "A successful body-pair retry uses the committed pose without an extra physical tick");
                }
                finally { solver.PoseHook = null; PhysicsServer.BodySetTransform(raw, rawPose); }
                using var unbound = new Solver(); Reject<InvalidOperationException>(() => unbound.ReadGeometry(circle.GetRID()));
                Check(ReferenceEquals(solver.ReadGeometry(circle.GetRID()), circle.GetGeometryData()), "Authored geometry is borrowed without a resource clone");
                Reject<ArgumentException>(() => solver.ReadGeometry(raw));
                using (var temporary = new CircleShape { Radius = 2 })
                {
                    var id = temporary.GetRID(); var retained = solver.ReadGeometry(id); temporary.Radius = 4;
                    Check(retained.GetCircleRadius() == 2 && solver.ReadGeometry(id).GetCircleRadius() == 4, "Geometry revisions retain old immutable data");
                    temporary.Dispose(); Reject<ArgumentException>(() => solver.ReadGeometry(id)); Check(retained.GetCircleRadius() == 2, "Retired resources do not invalidate retained geometry");
                }
                using (var supplier = new PairSupplier())
                {
                    var id = supplier.GetRID(); var invoked = 0;
                    supplier.Read = () =>
                    {
                        invoked++; Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(space));
                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetCollisionLayer(raw, 1));
                        throw new InvalidOperationException("Supplier failure");
                    };
                    supplier.Invalidate(); Reject<InvalidOperationException>(() => solver.ReadGeometry(id)); Check(invoked == 1, "Procedural supplier executes under world borrowing");
                    supplier.Read = null; Check(solver.ReadGeometry(id).GetCircleRadius() == 2, "Supplier failure restores world borrow scope");
                }
                using var ring = new CustomShape(Annulus.Create(5, 10));
                Check(!PhysicsServer.ShapeCollide(space, ring.GetRID(), Transform.Identity, Vector2.Zero, small.GetRID(), Transform.Identity, Vector2.Zero, output, out count), "Selected custom annulus keeps a real empty hole");
                Check(PhysicsServer.ShapeCollide(space, ring.GetRID(), Transform.Identity, Vector2.Zero, small.GetRID(), near, Vector2.Zero, output, out count) && count == 1 && output[0] == new Vector2(5, 0) && output[1] == new Vector2(6, 0), "Selected custom inner wall supplies exact contact ownership");
                Check(PhysicsServer.ShapeCollide(space, small.GetRID(), near, Vector2.Zero, ring.GetRID(), Transform.Identity, Vector2.Zero, output, out count) && output[0] == new Vector2(6, 0) && output[1] == new Vector2(5, 0), "Reversed custom pair reverses contact ownership");
                for (var fault = 1; fault <= 5; fault++)
                {
                    solver.PairFault = fault; Fill(); Reject<InvalidOperationException>(() => Pair(near, output, out _)); Unchanged();
                }
                solver.PairFault = 0;
                nestedSpace = PhysicsServer.SpaceCreate("ShapePairs");
                var nesting = false; var guarded = false;
                solver.PairHook = () =>
                {
                    if (nesting) return;
                    nesting = true;
                    try
                    {
                        Check(Pair(near, nestedOutput, out var n) && n > 0, "Nested same-world pair uses independent staging");
                        Check(PhysicsServer.ShapeCollide(nestedSpace, circle.GetRID(), Transform.Identity, Vector2.Zero, circle.GetRID(), near, Vector2.Zero, otherOutput, out n) && n > 0, "Other-world pair has independent owner and staging");
                        if (!guarded)
                        {
                            guarded = true; Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(raw));
                            Reject<InvalidOperationException>(() => PhysicsServer.BodySetShapeTransform(raw, 1, Transform.Identity));
                            var thread = new Thread(() => Reject<InvalidOperationException>(() => solver.ReadGeometry(circle.GetRID()))); thread.Start(); thread.Join();
                        }
                    }
                    finally { nesting = false; }
                };
                Check(Pair(near, output, out count) && count > 0 && output[0].DistanceTo(Vector2.Zero) < 3.0001f, "Nested calls preserve the outer complete result");
                var ownerThread = Environment.CurrentManagedThreadId; var numericalHooks = 0;
                solver.StepHook = () =>
                {
                    Check((Environment.CurrentManagedThreadId != ownerThread) == threaded, "Pair input reads execute on the captured numerical lane"); numericalHooks++;
                    Check(solver.ReadGeometry(circle.GetRID()).GetCircleRadius() == 3, "Numerical hooks can read authored geometry without solver reentry");
                    Reject<InvalidOperationException>(() => Pair(near, output, out _));
                };
                tree.PhysicsFrame(1d / 120); solver.StepHook = null;
                Check(numericalHooks == 1, "The selected numerical hook actually exercised pair input reads");
            }
            BuiltInFamilies();
            ShapePairCapacity(world, tree);
            var foreign = new Thread(() => Reject<InvalidOperationException>(() => Pair(near, output, out _))); foreign.Start(); foreign.Join();
            for (var i = 0; i < 128; i++) Cycle();
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            var start = moving.Position.X; var queries = solver?.PairQueries ?? 0; var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Cycle();
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownerBytes == 0 && allBytes == 0 && MathF.Abs(moving.Position.X - start - 64f / 120) < .001f,
                "Warmed public shape/body pairs, nested queries and complete moving frames allocate zero managed bytes");
            Console.WriteLine($"Public shape pairs backend={world.PhysicsBackend} frames=64 outerQueries=320 selectedHooks={(solver?.PairQueries ?? 0) - queries} bytes={ownerBytes}/{allBytes}.");
        }
        finally
        {
            if (solver is not null) { solver.PairHook = null; solver.PairFault = 0; solver.StepHook = null; }
            if (nestedSpace.IsValid()) PhysicsServer.FreeRID(nestedSpace);
            PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(detached);
        }
        if (solver is not null)
        {
            tree.Dispose(); world.Dispose(); Reject<ObjectDisposedException>(() => solver.ReadGeometry(circle.GetRID()));
            Reject<ArgumentException>(() => Pair(near, output, out _));
            var failedSpace = PhysicsServer.SpaceCreate("ShapePairs"); var failed = factory!;
            PhysicsServer.SpaceSetActive(failedSpace, true);
            using var failedView = PhysicsServer.SpaceGetDirectState(failedSpace); var creations = failed.SpaceViewCreations; failed.FailSolve = true;
            try
            {
                Reject<AggregateException>(() => PhysicsServer.SpaceStep(failedSpace, 1d / 120));
                Reject<InvalidOperationException>(() => failed.ReadGeometry(circle.GetRID()));
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetDirectState(failedSpace));
                failedView.Dispose(); Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetDirectState(failedSpace));
                Check(failed.SpaceViewCreations == creations, "A terminal world neither returns its cached query view nor invokes another factory");
                Reject<InvalidOperationException>(() => PhysicsServer.ShapeCollide(failedSpace, circle.GetRID(), Transform.Identity, Vector2.Zero, circle.GetRID(), near, Vector2.Zero, output, out _));
            }
            finally { PhysicsServer.FreeRID(failedSpace); }
        }
        bool Pair(Transform pose, Span<Vector2> contacts, out int pairs) => PhysicsServer.ShapeCollide(space, circle.GetRID(), Transform.Identity, Vector2.Zero, circle.GetRID(), pose, Vector2.Zero, contacts, out pairs);
        void Fill() => Array.Fill(output, sentinel);
        void Unchanged() { foreach (var point in output) Check(point == sentinel, "Failed/miss result preserves all caller output"); }
        void Cycle()
        {
            Check(Pair(near, output, out var pairs) && pairs > 0, "Warm resource pair intersects");
            Check(PhysicsServer.ShapeCollide(space, small.GetRID(), new(0, new Vector2(-10, 0)), new(20, 0), small.GetRID(), new(0, new Vector2(0, -10)), new(0, 10), [], out pairs) && pairs == 0, "Independent swept regions intersect even when synchronized trajectories do not");
            Check(PhysicsServer.BodyCollideShape(scene.GetRID(), 1, circle.GetRID(), scenePose, Vector2.Zero, output, out pairs) && pairs > 0, "Warm scene slot pair intersects");
            Check(PhysicsServer.BodyCollideShape(raw, 1, circle.GetRID(), rawPairPose, Vector2.Zero, output, out pairs) && pairs > 0, "Warm raw slot pair composes transforms");
            Check(PhysicsServer.BodyCollideShape(detached, 1, circle.GetRID(), detachedPose * rawSlot, Vector2.Zero, output, out pairs) && pairs > 0, "Warm detached slot pair intersects");
            tree.PhysicsFrame(1d / 120);
        }
        void BuiltInFamilies()
        {
            using var box = new RectangleShape { Size = new(6, 6) }; using var capsule = new CapsuleShape { Radius = 3, Height = 8 };
            using var segment = new SegmentShape { A = new(-10, 0), B = new(10, 0) };
            using var convex = new ConvexPolygonShape { Points = [new(-3, -3), new(3, -3), new(3, 3), new(-3, 3)] };
            using var concave = new ConcavePolygonShape { Segments = [new(-10, 0), new(10, 0)] };
            using var ray = new SeparationRayShape { Length = 10 }; using var boundary = new WorldBoundaryShape();
            foreach (var shape in new Shape[] { box, capsule, segment, convex, concave, boundary })
            {
                if (solver is not null) Check(ReferenceEquals(solver.ReadGeometry(shape.GetRID()), shape.GetGeometryData()), "The implementation can borrow each exact authored geometry family");
                Check(PhysicsServer.ShapeCollide(space, circle.GetRID(), Transform.Identity, Vector2.Zero, shape.GetRID(), Transform.Identity, Vector2.Zero, [], out _), "Public selected pair handles each ordinary/terrain/boundary family");
            }
            if (solver is not null) Check(solver.ReadGeometry(ray.GetRID()).GetSeparationRayParameters().Length == 10, "Exact separation-ray authoring is available without a resource clone");
            Check(PhysicsServer.ShapeCollide(space, ray.GetRID(), Transform.Identity, Vector2.Zero, circle.GetRID(), new(0, new Vector2(0, 5)), Vector2.Zero, [], out _), "Public selected separation-ray pair executes");
        }
    }

    private sealed class PairSupplier : Shape
    {
        internal Action? Read;
        internal void Invalidate() { EnsureGeometryChangeAccess(); EmitGeometryChanged(); }
        protected override ShapeGeometry GetGeometryCore() { Read?.Invoke(); return ShapeGeometry.Circle(2); }
    }
}
