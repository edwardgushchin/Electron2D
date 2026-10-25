using Electron2D;

public static partial class BackendChecks
{
    public static void RunGroupMotion(PhysicsServer.Backend? backend, bool threaded, Action<World, bool>? observe = null, bool worker = true)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("GroupMotion", () => solver = new Solver());
        try { GroupMotion(backend, () => solver, observe, worker); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void GroupMotion(PhysicsServer.Backend? backend, Func<Solver?> selectedSolver, Action<World, bool>? observe, bool worker)
    {
        using var world = backend is { } selected ? new World(selected) : new World("GroupMotion");
        using var root = new SubViewport { World = world };
        var group = new Node { ProcessThreadGroup = worker ? Node.ProcessThreadGroupMode.SubThread : Node.ProcessThreadGroupMode.MainThread };
        var driver = new MotionGroupProbe(); group.AddChild(driver); root.AddChild(group);
        using var shape = new CircleShape { Radius = 1 };
        var rigid = new RigidBody { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace };
        var direct = new RigidBody { Position = new(20, 0), GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace };
        var frozen = new RigidBody { Position = new(40, 0), Freeze = true };
        var staticProfile = new RigidBody { Position = new(140, 0), GravityScale = 0 };
        var kinematicProfile = new RigidBody { Position = new(160, 0), GravityScale = 0 };
        var linearProfile = new RigidBody { Position = new(180, 0), GravityScale = 0, LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace };
        var surface = new StaticBody { Position = new(60, 0) };
        var animatable = new AnimatableBody { Position = new(80, 0) };
        var character = new CharacterBody { Position = new(100, 0) };
        var collector = new MotionGroupProbe { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread }; root.AddChild(collector);
        foreach (var body in new PhysicsBody[] { rigid, direct, frozen, surface, animatable, character, staticProfile, kinematicProfile, linearProfile })
        { body.AddChild(new CollisionShape { Shape = shape }); group.AddChild(body); }
        using var tree = new SceneTree(root);
        using var trace = PhysicsAllocationTrace.Open(backend ?? PhysicsServer.Backend.CPU, 0x20_0000_0000UL);
        PhysicsServer.BodySetMode(direct.GetRID(), PhysicsServer.BodyMode.Rigid);
        PhysicsServer.BodySetMode(staticProfile.GetRID(), PhysicsServer.BodyMode.Static);
        PhysicsServer.BodySetMode(kinematicProfile.GetRID(), PhysicsServer.BodyMode.Kinematic);
        PhysicsServer.BodySetMode(linearProfile.GetRID(), PhysicsServer.BodyMode.RigidLinear);
        var owner = Environment.CurrentManagedThreadId; var frames = 0;
        RigidBody[] moving = [rigid, direct];
        driver.Signal = () =>
        {
            Check((Environment.CurrentManagedThreadId != owner) == worker, "Motion authoring executes on the selected owner or scene worker");
            foreach (var body in moving)
            {
                body.CanSleep = true; body.Sleeping = true;
                Check(body.Sleeping && body.LinearVelocity == Vector2.Zero && body.AngularVelocity == 0,
                    "Current-group sleep immediately clears logical motion");
                body.SetThreadSafe<RigidBody, Vector2>(static (node, value) => node.LinearVelocity = value, new(3, 0));
                body.CallThreadSafe<RigidBody>(static node => node.AngularVelocity = .25f);
                body.SetAxisVelocity(new(0, 2));
                Check(body.LinearVelocity == new Vector2(3, 2) && body.AngularVelocity == .25f && !body.Sleeping,
                    "Current-group velocity, angular and axis writes have immediate logical effects");
                body.CanSleep = false;
            }
            frozen.LinearVelocity = new(7, 0); frozen.AngularVelocity = .5f;
            Check(frozen.LinearVelocity == new Vector2(7, 0) && frozen.AngularVelocity == .5f && frozen.Sleeping,
                "A frozen role retains configured velocities independently of native inactivity");
            frozen.CanSleep = false; frozen.Sleeping = false;
            Check(frozen.Sleeping, "Static inactivity is preserved by current-group sleep-policy and wake requests");
            var parity = frames & 1;
            surface.ConstantLinearVelocity = new(4 + parity, 0); surface.ConstantAngularVelocity = .5f + .125f * parity;
            animatable.ConstantLinearVelocity = new(5 + parity, 0); animatable.ConstantAngularVelocity = .75f + .125f * parity;
            Check(surface.ConstantLinearVelocity == new Vector2(4 + parity, 0) && surface.ConstantAngularVelocity == .5f + .125f * parity &&
                animatable.ConstantLinearVelocity == new Vector2(5 + parity, 0) && animatable.ConstantAngularVelocity == .75f + .125f * parity,
                "Both surface roles expose current-group authored velocities");
            staticProfile.LinearVelocity = new(4, 1); staticProfile.AngularVelocity = .75f; staticProfile.Sleeping = false;
            kinematicProfile.LinearVelocity = new(6, 1); kinematicProfile.AngularVelocity = .5f;
            var kinematicSleeping = kinematicProfile.Sleeping; kinematicProfile.Sleeping = true;
            linearProfile.LinearVelocity = new(8, 1); linearProfile.AngularVelocity = .9f;
            Check(staticProfile.LinearVelocity == new Vector2(4, 1) && staticProfile.AngularVelocity == .75f && staticProfile.Sleeping,
                "A static direct profile retains virtual motion and inactive state");
            Check((kinematicProfile.LinearVelocity - new Vector2(6, 1)).Length() < .00001f && MathF.Abs(kinematicProfile.AngularVelocity - .5f) < .00001f && kinematicProfile.Sleeping == kinematicSleeping,
                "A kinematic direct profile combines authored surface motion and ignores dynamic sleep requests");
            Check(linearProfile.LinearVelocity == new Vector2(8, 1) && linearProfile.AngularVelocity == 0,
                "A linear-only direct profile preserves its angular lock during worker writes");
            character.Velocity = new(5, 0);
            Check(character.Velocity == new Vector2(5, 0), "Current-group character velocity is immediate authored state");
            frames++;
        };
        collector.Signal = () =>
        {
            Check(Environment.CurrentManagedThreadId == owner, "The main collector runs after worker motion publication");
            var parity = (frames - 1) & 1;
            var surfaceVelocity = (PhysicsServer.BodyGetDirectState(surface.GetRID()) ?? throw new InvalidOperationException("Missing surface state")).GetVelocityAtLocalPosition(new(1, 0));
            var animatedVelocity = (PhysicsServer.BodyGetDirectState(animatable.GetRID()) ?? throw new InvalidOperationException("Missing animatable state")).GetVelocityAtLocalPosition(new(1, 0));
            Check((surfaceVelocity - new Vector2(4 + parity, .5f + .125f * parity)).Length() < .00001f &&
                (animatedVelocity - new Vector2(5 + parity, .75f + .125f * parity)).Length() < .00001f,
                "Owner direct views observe changing linear and angular surface velocity before integration");
            character.MoveAndSlide();
        };
        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        for (var i = 0; i < 64; i++) Frame();
        Collect(); observe?.Invoke(world, true); trace?.Begin(trace.Tag(1)); long current, all;
        try
        {
            current = GC.GetAllocatedBytesForCurrentThread(); all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Frame();
            current = GC.GetAllocatedBytesForCurrentThread() - current; all = GC.GetTotalAllocatedBytes(true) - all;
        }
        finally { trace?.End(); }
        observe?.Invoke(world, false);
        Check(current == 0 && all == 0, $"Current-group motion and complete physical/query frames allocate {current}/{all} B");
        Check(frames == 128, "Each prepared frame executes exactly one worker authoring callback");
        driver.Signal = null; collector.Signal = null;
        Exception? foreign = null;
        var thread = new Thread(() => { try { rigid.LinearVelocity = default; } catch (Exception error) { foreign = error; } });
        thread.Start(); thread.Join(); Check(foreign is InvalidOperationException, "An unrelated worker receives no scene-motion permission");
        if (worker)
        {
            using var barrier = new Barrier(2);
            var intruder = new MotionGroupProbe { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
            driver.Signal = () => Check(barrier.SignalAndWait(TimeSpan.FromSeconds(5)), "Both worker groups reach the current-group isolation barrier");
            intruder.Signal = () =>
            {
                Check(barrier.SignalAndWait(TimeSpan.FromSeconds(5)), "The foreign group runs concurrently with the body group");
                Reject<InvalidOperationException>(() => rigid.LinearVelocity = default);
                Reject<InvalidOperationException>(() => _ = rigid.AngularVelocity);
                Reject<InvalidOperationException>(() => rigid.CanSleep = true);
                Reject<InvalidOperationException>(() => _ = rigid.CanSleep);
                Reject<InvalidOperationException>(() => surface.ConstantAngularVelocity = 0);
                Reject<InvalidOperationException>(() => character.Velocity = default);
            };
            root.AddChild(intruder); tree.PhysicsFrame(.01); root.RemoveChild(intruder); intruder.Dispose(); driver.Signal = null;
        }
        if (worker && selectedSolver() is { } provider)
        {
            var directRID = direct.GetRID(); var reads = new Vector2[2]; var readCount = 0;
            provider.BodyHook = body => { if (body == directRID) reads[readCount++] = direct.LinearVelocity; };
            driver.Signal = () => { direct.LinearVelocity = new(3, 0); direct.AngularVelocity = .5f; direct.LinearVelocity = new(4, 0); };
            tree.PhysicsFrame(.01); driver.Signal = null; provider.BodyHook = null;
            Check(readCount == 2 && reads[0] == new Vector2(3, 2) && reads[1] == new Vector2(3, 0),
                "Readonly selected hooks observe each native prefix without recursively publishing the queued tail");
            var rid = rigid.GetRID(); var linearWrites = 0; var authored = 0;
            var failure = new ArithmeticException("Expected queued angular publication failure");
            provider.BodyHook = body => { if (body == rid) linearWrites++; };
            provider.AngularHook = body => { if (body == rid) throw failure; };
            driver.Signal = () => { authored++; rigid.LinearVelocity = new(7, 0); rigid.AngularVelocity = .5f; rigid.LinearVelocity = new(9, 0); rigid.AngularVelocity = .75f; };
            var first = CaptureCopy(() => tree.PhysicsFrame(.01));
            Check(first is AggregateException errors && errors.Flatten().InnerExceptions.Contains(failure) && linearWrites == 1 && authored == 1,
                "A failed publication retains the failed operation and ordered tail without repeating its successful prefix");
            Check(CaptureCopy(() => tree.PhysicsFrame(.01)) is AggregateException && linearWrites == 1 && authored == 1,
                "A pending failure rejects another worker stage before accepting new authored commands");
            driver.Signal = null; provider.AngularHook = null;
            query.Position = rigid.Position; world.DirectSpaceState.IntersectPoint(query, hits);
            Check(linearWrites == 2 && PhysicsServer.BodyGetLinearVelocity(rid) == new Vector2(9, 0) && PhysicsServer.BodyGetAngularVelocity(rid) == .75f,
                "Owner query preparation retries the failed command and completes the original ordered tail");
            provider.AngularHook = body => { if (body == rid) throw failure; };
            driver.Signal = () => { rigid.LinearVelocity = new(11, 0); rigid.AngularVelocity = .1f; rigid.LinearVelocity = new(13, 0); rigid.AngularVelocity = .25f; };
            Check(CaptureCopy(() => tree.PhysicsFrame(.01)) is AggregateException, "A later attachment retains an independent failing command tail");
            driver.Signal = null; group.RemoveChild(rigid);
            Check(rigid.LinearVelocity == new Vector2(13, 0) && rigid.AngularVelocity == .25f,
                "Retirement preserves all accepted logical motion without repairing the discarded implementation");
            provider.AngularHook = null; provider.BodyHook = null;
            rigid.LinearVelocity = new(17, 0); rigid.AngularVelocity = .4f; group.AddChild(rigid);
            tree.PhysicsFrame(.01);
            Check(PhysicsServer.BodyGetLinearVelocity(rigid.GetRID()) == new Vector2(17, 0) && PhysicsServer.BodyGetAngularVelocity(rigid.GetRID()) == .4f,
                "A replacement attachment cannot replay discarded commands over newly authored state");
        }
        Console.WriteLine($"Group motion {backend?.ToString() ?? "registered"}, worker={worker}, separate={ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread)}: ordered worker motion, sleep, surfaces, real queries, 64 measured frames {current}/{all} B passed.");

        void Frame()
        {
            var from = rigid.Position; var characterFrom = character.Position; tree.PhysicsFrame(.01);
            Check(MathF.Abs(character.Position.X - characterFrom.X - .05f) < .00002f, "The owner consumes worker-authored character velocity through real motion");
            Check((rigid.Position - from - new Vector2(.03f, .02f)).Length() < .00002f &&
                (direct.LinearVelocity - new Vector2(3, 2)).Length() < .00001f,
                "Owner publication precedes the selected full step and preserves actual direct-mode motion");
            if ((surface.Position - new Vector2(60, 0)).Length() > .00002f || (animatable.Position - new Vector2(80, 0)).Length() > .00002f)
                throw new InvalidOperationException($"Surface poses: static={surface.Position}, animatable={animatable.Position}");
            query.Position = rigid.Position;
            Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == rigid.GetRID(), "Actual selected query retains the worker-authored body identity");
        }
    }

    private sealed class MotionGroupProbe : Node
    {
        internal Action? Signal;
        protected override void OnPhysicsProcess(double delta) => Signal?.Invoke();
    }
}
