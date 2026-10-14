using Electron2D;

public static partial class BackendChecks
{
    public static void RunPairLifetime(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { PairLifetime(backend); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void PairLifetime(PhysicsServer.Backend? backend)
    {
        using var circle = new CircleShape { Radius = 3 };
        using var replacement = new CircleShape { Radius = 5, ResourceName = "replacement" };
        using var dataSource = new CustomShape(Annulus.Create(5, 10));
        using var nested = new CustomShape(dataSource.Geometry);
        CustomShape? custom = null;
        Action? hook = null;
        var calls = 0;
        bool Collide(ShapeGeometry a, Transform pa, Vector2 ma, ShapeGeometry b, Transform pb, Vector2 mb, Span<Vector2> output, out int count)
        {
            calls++; hook?.Invoke(); count = output.IsEmpty ? 0 : 1;
            if (count > 0) { output[0] = new(3, 0); output[1] = new(2, 0); }
            return true;
        }
        using (custom = new CustomShape(ShapeGeometry.Custom("pair-lifetime/v1", [], new(new(-3, -3), new(6, 6)), new(1, 1, default), [], Collide)))
        {
            var original = custom.Geometry; var changes = 0; custom.Changed += _ => changes++; circle.Changed += _ => changes++;
            hook = () =>
            {
                Check(nested.Collide(Transform.Identity, circle, new(0, new Vector2(4, 0))), "Different nested custom pair shares a readonly operand");
                Reject<InvalidOperationException>(() => circle.Radius = 5);
                Reject<InvalidOperationException>(() => circle.Radius = 3);
                Reject<InvalidOperationException>(() => circle.CustomSolverBias = .3f);
                Reject<InvalidOperationException>(() => circle.CopyFromResource(replacement));
                Reject<InvalidOperationException>(circle.ResetState);
                Reject<InvalidOperationException>(circle.Dispose);
                Reject<InvalidOperationException>(() => custom.Geometry = dataSource.Geometry);
                Reject<InvalidOperationException>(() => custom.Geometry = original);
                Reject<InvalidOperationException>(() => custom.CopyFromResource(dataSource));
                Reject<InvalidOperationException>(custom.ResetState);
                Reject<InvalidOperationException>(custom.Dispose);
                Exception? workerError = null;
                var worker = new Thread(() =>
                {
                    try { Reject<InvalidOperationException>(() => circle.Radius = 5); Reject<InvalidOperationException>(custom.Dispose); }
                    catch (Exception error) { workerError = error; }
                });
                worker.Start(); worker.Join(); if (workerError is not null) throw workerError;
                Check(circle.Radius == 3 && circle.ResourceName.Length == 0 && !circle.IsDisposed && !custom.IsDisposed &&
                    ReferenceEquals(custom.Geometry, original) && changes == 0, "Borrowed shape pairs preserve geometry, metadata, notifications and lifetime");
            };
            Check(custom.Collide(Transform.Identity, circle, Transform.Identity), "Standalone custom collision keeps its operands live");
            Check(circle.Collide(Transform.Identity, custom, Transform.Identity), "Reversed custom pair guards the same resources");
            _ = custom.CollideAndGetContacts(Transform.Identity, circle, Transform.Identity);
            _ = custom.CollideWithMotionAndGetContacts(Transform.Identity, Vector2.Zero, circle, Transform.Identity, Vector2.Zero);
            Check(custom.CollideWithMotion(Transform.Identity, Vector2.Zero, circle, Transform.Identity, Vector2.Zero), "Motion-only queries retain operand lifetime");
            hook = () => throw new InvalidOperationException("Expected custom pair callback failure");
            Reject<InvalidOperationException>(() => custom.Collide(Transform.Identity, circle, Transform.Identity));
            hook = null;
            Check(circle.Collide(Transform.Identity, circle, Transform.Identity), "Same-resource pairs balance both operand borrows");
            circle.Radius = 4; custom.Geometry = dataSource.Geometry;
            Check(circle.Radius == 4 && changes == 2, "Completed collision releases operand authoring guards");
        }
        Check(calls == 6, "Every custom pair family and failure reached its callback once");
        SelectedPairs(backend);
        Console.WriteLine("Shape-pair lifetime: mutation, inherited copying/reset and disposal guards passed.");
    }

    private static void SelectedPairs(PhysicsServer.Backend? backend)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("PairLifetime", () => solver = new Solver());
        using var world = backend is { } selected ? new World(selected) : new World("PairLifetime");
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        _ = root.FindWorld();
        using var circle = new CircleShape { Radius = 3 }; using var replacement = new CircleShape { Radius = 5 };
        var owned = PhysicsServer.CircleShapeCreate(); var body = PhysicsServer.BodyCreate(); var moving = PhysicsServer.BodyCreate();
        var output = new Vector2[8]; var near = new Transform(0, new Vector2(4, 0));
        try
        {
            PhysicsServer.ShapeSetData(owned, circle); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(body, owned); PhysicsServer.BodySetSpace(body, world.Space);
            PhysicsServer.AreaSetGravity(world.Space, 0); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
            PhysicsServer.BodySetMode(moving, PhysicsServer.BodyMode.Rigid);
            PhysicsServer.BodySetCanSleep(moving, false); PhysicsServer.BodySetLinearVelocity(moving, new(60, 0)); PhysicsServer.BodySetSpace(moving, world.Space);
            if (solver is not null)
            {
                var nesting = false;
                solver.PairHook = () =>
                {
                    Reject<InvalidOperationException>(() => circle.Radius = 5);
                    Reject<InvalidOperationException>(circle.Dispose);
                    Reject<InvalidOperationException>(() => PhysicsServer.ShapeSetData(owned, replacement));
                    Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(owned));
                    if (!nesting)
                    {
                        nesting = true;
                        try { Check(PhysicsServer.ShapeCollide(world.Space, owned, Transform.Identity, Vector2.Zero, circle.GetRID(), near, Vector2.Zero, [], out _), "A nested selected pair borrows the same resources independently"); }
                        finally { nesting = false; }
                    }
                    Check(circle.Radius == 3 && !circle.IsDisposed, "Selected pair refuses resource changes before authoring or registry mutation");
                };
                Queries(); solver.PairHook = () => throw new InvalidOperationException("Expected selected pair failure");
                var sentinel = new Vector2(111, 222); Array.Fill(output, sentinel);
                Reject<InvalidOperationException>(() => PhysicsServer.ShapeCollide(world.Space, owned, Transform.Identity, Vector2.Zero, circle.GetRID(), near, Vector2.Zero, output, out _));
                Check(output.All(value => value == sentinel), "A selected failure leaves caller storage unchanged");
                solver.PairHook = null;
            }
            Queries(); PhysicsServer.ShapeSetData(owned, replacement); PhysicsServer.ShapeSetData(owned, circle);
            for (var i = 0; i < 128; i++) { tree.PhysicsFrame(1d / 60); Queries(); }
            Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) { tree.PhysicsFrame(1d / 60); Queries(); }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Shape-pair lifetime full frames/queries allocate {owner}/{all} B");
            var position = PhysicsServer.BodyGetTransform(moving).Origin; var velocity = PhysicsServer.BodyGetLinearVelocity(moving);
            Check(MathF.Abs(position.X - 256) < .02f && velocity.DistanceTo(new(60, 0)) < .001f,
                $"Measured pair guards retain real complete numerical steps and solved motion: {position}, {velocity}");
            Console.WriteLine($"Selected pair lifetime {backend?.ToString() ?? "registered"}, threaded={ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread)}: nested/error/owned RID guards and 128 full frames/queries {owner}/{all} B passed.");
        }
        finally { if (solver is not null) solver.PairHook = null; PhysicsServer.FreeRID(moving); PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(owned); }
        void Queries()
        {
            Check(PhysicsServer.ShapeCollide(world.Space, owned, Transform.Identity, Vector2.Zero, circle.GetRID(), near, Vector2.Zero, output, out var count) && count > 0,
                "Selected primitive shape pair retains actual contact output");
            Check(PhysicsServer.BodyCollideShape(body, 0, circle.GetRID(), near, Vector2.Zero, output, out count) && count > 0,
                "Attached body pair retains actual geometry and complete output");
        }
    }
}
