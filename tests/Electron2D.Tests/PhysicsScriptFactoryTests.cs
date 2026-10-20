using Electron2D;

internal static class PhysicsScriptFactoryTests
{
    internal static void Run(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        ScriptTests.Register();
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread); var owner = Thread.CurrentThread; var steps = 0;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        var name = "ScriptFactories" + threaded;
        if (backend is null) BackendChecks.RegisterScriptFactoryBackend(name, () =>
        { Check((Thread.CurrentThread != owner) == threaded, "Factory ownership keeps the captured numerical lane"); steps++; });
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World(name);
            using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
            using var script = ResourceLoader.Load<Script>(ScriptActor.SourcePath, ResourceLoader.CacheMode.Ignore);
            using var paint = ResourceLoader.Load<Script>(ScriptPaintEffect.SourcePath, ResourceLoader.CacheMode.Ignore);
            FactoryConstructionChecks.RejectIndependent(static () => new ScriptActor { Count = 29 }, factory =>
            {
                using var result = script.New(factory);
            }, value => Check(value.Count == 29, "Independent constructor result retains its stored state"));
            FactoryConstructionChecks.RejectIndependent(static () => new ScriptActor { Count = 31 }, factory =>
            {
                ScriptTests.ActorFactory = factory;
                try { using var result = script.New<ScriptActor>(); }
                finally { ScriptTests.ActorFactory = null; }
            }, value => Check(value.Count == 31, "Independent registered-factory result retains its stored state"));
            FactoryConstructionChecks.RejectIndependent(static () => new ScriptPaintEffect(), factory => { using var result = paint.New(factory); });
            using (var fresh = script.New(static () => Task.Run(static () => new ScriptActor(11)).GetAwaiter().GetResult()))
                Check(fresh.Count == 11, "Task construction retains the factory context");
            using (var fresh = script.New(() =>
            {
                ScriptActor? result = null;
                var task = WorkerThreadPool.AddTask(() => result = new ScriptActor(13));
                WorkerThreadPool.WaitForTaskCompletion(task);
                return result!;
            })) Check(fresh.Count == 13, "Engine worker construction retains only the internal factory context");
            using var shape = new CircleShape { Radius = 2 }; using var material = new PhysicsMaterial { Friction = .4f };
            using var actor = new ScriptActor { Name = "BorrowedActor", Count = 17 };
            using var moving = new RigidBody
            {
                GravityScale = 0,
                CanSleep = false,
                LinearVelocity = Vector2.Right,
                LinearDampMode = RigidBody.DampMode.Replace,
                LinearDamp = 0,
                PhysicsMaterialOverride = material
            };
            moving.AddChild(new CollisionShape { Shape = shape }); actor.AddChild(moving); root.AddChild(actor);
            var bodyRID = moving.GetRID(); var shapeRID = shape.GetRID(); var bodyID = moving.InstanceID; var notifications = 0;
            moving.Disposed += _ => notifications++;
            Reject<InvalidOperationException>(() => script.New<Node>(() => moving));
            Check(!moving.IsDisposed && notifications == 0 && moving.GetRID() == bodyRID && moving.InstanceID == bodyID && moving.IsInsideTree,
                "Rejecting a borrowed wrong-type script result preserves the active physical body");
            Reject<InvalidOperationException>(() => script.New(() => actor));
            ScriptTests.ActorFactoryTarget = actor;
            try { Reject<InvalidOperationException>(() => script.New<ScriptActor>()); }
            finally { ScriptTests.ActorFactoryTarget = null; }
            Check(!actor.IsDisposed && actor.Count == 17 && actor.Tree == tree && moving.Parent == actor,
                "Both constructor overloads preserve an existing exact script type and its physical subtree");
            foreach (var borrowed in new ElectronObject[] { shape, material, world, script })
                Reject<InvalidOperationException>(() => script.New(() => borrowed));
            Check(!shape.IsDisposed && shape.GetRID() == shapeRID && shape.Radius == 2 && !material.IsDisposed && material.Friction == .4f && !world.IsDisposed && !script.IsDisposed,
                "Wrong-type borrowed geometry, material, world and source resource stay untouched");
            using var effect = new ScriptPaintEffect(); using var rich = new RichTextLabel(); rich.InstallEffect(effect);
            Reject<InvalidOperationException>(() => paint.New(() => effect)); ScriptTests.PaintFactoryTarget = effect;
            try
            {
                Reject<InvalidOperationException>(() => paint.New<RichTextEffect>());
                Reject<InvalidOperationException>(() => rich.InstallEffect(paint));
                Check(rich.CustomEffects.Length == 1 && ReferenceEquals(rich.CustomEffects[0], effect) && !effect.IsDisposed,
                    "Rejected script effects cannot acquire an already borrowed label effect");
                VerifyFileFactory(effect);
            }
            finally { ScriptTests.PaintFactoryTarget = null; }
            rich.Dispose(); Check(!effect.IsDisposed, "Label retirement preserves its borrowed effect after rejected script installation");
            using (var fresh = script.New(static () => new ScriptActor(9))) Check(fresh.Count == 9, "Fresh typed constructor arguments remain executable");
            using (var fresh = script.New<ScriptActor>()) Check(fresh.Count == 3, "Fresh registered node construction remains executable");
            using (var fresh = paint.New<ScriptPaintEffect>()) Check(fresh.BBCode == "compiled_paint", "Fresh registered resource construction remains executable");
            ScriptActor? invalid = null; var schema = new ApplicationException("script-schema"); var cleanup = new ApplicationException("script-cleanup");
            var failure = Catch<ApplicationException>(() => script.New(() => invalid = new ScriptActor { PropertyFailure = schema }));
            Check(ReferenceEquals(failure, schema) && invalid!.IsDisposed, "Fresh schema failure preserves its error and retires only the new node");
            var both = Catch<AggregateException>(() => script.New(() => invalid = new ScriptActor { PropertyFailure = schema, CleanupFailure = cleanup }));
            Check(invalid!.IsDisposed && both.Flatten().InnerExceptions.Contains(schema) && both.Flatten().InnerExceptions.Contains(cleanup),
                "Fresh script validation and cleanup failures both remain observable");
            Reject<InvalidOperationException>(() => script.New<ScriptActor>(() => null!));
            var constructor = new ApplicationException("script-constructor");
            Check(ReferenceEquals(Catch<ApplicationException>(() => script.New<ScriptActor>(() => throw constructor)), constructor), "Constructor exceptions retain their identity");
            using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            for (var i = 0; i < 128; i++) Frame();
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            var beforeSteps = steps; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(backend is not null || steps - beforeSteps == 128, "Factory ownership brackets contain actual registered solves");
            Check(own == 0 && all == 0 && notifications == 0, $"Preserved script physics allocated {own}/{all} B or retired its borrowed body");
            Console.WriteLine($"Script factory ownership {world.PhysicsBackendName}/{threaded}: borrowed identities, registered/argument constructors, file/effect consumers, cleanup errors; 128 full frames / 128 queries {own}/{all} B.");
            void Frame()
            {
                var before = moving.Position.X; var frames = actor.Frames; tree.PhysicsFrame(.01); tree.ProcessFrame(.01);
                query.Position = moving.GlobalPosition;
                Check(MathF.Abs(moving.Position.X - before - .01f) < .0001f && actor.Frames == frames + 1 && actor.Count == 17 &&
                    world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == bodyRID,
                    "Rejected factories leave real physical movement, queries and compiled script callbacks active");
            }
        }
        finally { ScriptTests.ActorFactoryTarget = null; ScriptTests.PaintFactoryTarget = null; ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
    private static void VerifyFileFactory(ScriptPaintEffect borrowed)
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-script-factory-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            var path = System.IO.Path.Combine(folder, "effect.e2dres"); ResourceSaver.Save(borrowed, path);
            Reject<InvalidOperationException>(() => ResourceLoader.Load<ScriptPaintEffect>(path, ResourceLoader.CacheMode.Ignore));
            Check(!borrowed.IsDisposed && borrowed.BBCode == "compiled_paint", "File factories preserve pre-existing exact resource identities before decode");
            ScriptTests.PaintFactoryTarget = null;
            using var fresh = ResourceLoader.Load<ScriptPaintEffect>(path, ResourceLoader.CacheMode.Ignore);
            Check(!ReferenceEquals(fresh, borrowed) && fresh.BBCode == borrowed.BBCode, "A rejected file factory does not poison later fresh decoding");
        }
        finally { Directory.Delete(folder, true); }
    }
    private static T Catch<T>(Action action) where T : Exception
    { try { action(); } catch (T error) { return error; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Reject<T>(Action action) where T : Exception => _ = Catch<T>(action);
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
