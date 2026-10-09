using System.Diagnostics;
using Electron2D;

internal static class PhysicsSleepPolicyTests
{
    internal static void Run(bool stages = false)
    {
        VerifyProjectDefaults(false); VerifyPolicy(false, stages); VerifyPublicLifetime(stages); VerifyScene(stages); Measure(false, stages); Measure(false, stages, false);
        Console.WriteLine($"World sleep policy passed on {(stages ? "CPU host/GPU stages" : "CPU")}: independent thresholds, duration, changes, ownership and zero allocation.");
    }
    internal static void RunResident()
    {
        VerifyProjectDefaults(true); VerifyPolicy(true, false); Measure(true, false); Measure(true, false, false);
        Console.WriteLine("Resident world sleep policy passed: shared project defaults, thresholds, duration, changes and zero allocation.");
    }
    private sealed class Rig : IDisposable
    {
        internal readonly GPUPhysicsBodyStore? Store;
        internal readonly RID Space;
        private readonly List<RID> _bodies = [];
        private readonly List<GPUPhysicsBodyStore.BodyHandle> _residentBodies = [];
        private readonly List<Shape> _shapes = [];
        internal Rig(bool resident, bool stages = false)
        {
            if (resident) Store = new();
            else
            {
                Space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(Space, true);
                PhysicsServer.AreaSetGravity(Space, 0); PhysicsServer.AreaSetLinearDamp(Space, 0); PhysicsServer.AreaSetAngularDamp(Space, 0);
                if (stages) PhysicsServer.Service.GetSceneSpace(Space).EnableGPUSolver();
            }
        }
        internal PhysicsSleepSettings Policy => Store?.GetSleepSettings() ?? new(
            PhysicsServer.SpaceGetBodyLinearVelocitySleepThreshold(Space), PhysicsServer.SpaceGetBodyAngularVelocitySleepThreshold(Space), PhysicsServer.SpaceGetBodyTimeToSleep(Space));
        internal void Set(PhysicsSleepSettings value)
        {
            if (Store is not null) Store.SetSleepSettings(value);
            else
            {
                PhysicsServer.SpaceSetBodyLinearVelocitySleepThreshold(Space, value.LinearThreshold);
                PhysicsServer.SpaceSetBodyAngularVelocitySleepThreshold(Space, value.AngularThreshold);
                PhysicsServer.SpaceSetBodyTimeToSleep(Space, value.TimeToSleep);
            }
        }
        internal int Add(Vector2 velocity = default, float angular = 0, float radius = 2, bool canSleep = true)
        {
            var shape = new CircleShape { Radius = radius }; _shapes.Add(shape);
            var index = _shapes.Count - 1; var at = new Vector2(index * 1000, 0);
            if (Store is not null)
            {
                var b = Store.Add(new(PhysicsServer.BodyMode.Rigid, at, 0, velocity, angular, CanSleep: canSleep));
                Store.AddShape(b, shape); _residentBodies.Add(b);
            }
            else
            {
                var b = PhysicsServer.BodyCreate(); _bodies.Add(b);
                PhysicsServer.BodyAddShape(b, shape.GetRID()); PhysicsServer.BodySetCanSleep(b, canSleep);
                PhysicsServer.BodySetTransform(b, new(0, at)); PhysicsServer.BodySetLinearVelocity(b, velocity); PhysicsServer.BodySetAngularVelocity(b, angular);
                PhysicsServer.BodySetSpace(b, Space);
            }
            return index;
        }
        internal RID Body(int index) => _bodies[index];
        internal bool Sleeping(int index)
        {
            if (Store is null) return PhysicsServer.BodyGetSleeping(_bodies[index]);
            Span<GPUPhysicsBodyStore.Snapshot> state = stackalloc GPUPhysicsBodyStore.Snapshot[1];
            Store.Read([_residentBodies[index]], state); return state[0].Sleeping;
        }
        internal void Sleep(int index)
        {
            if (Store is not null) Store.SetSleeping(_residentBodies[index], true);
            else PhysicsServer.BodySetSleeping(_bodies[index], true);
        }
        internal void Step(double dt = .03125)
        {
            if (Store is not null) Store.Simulate((float)dt, Vector2.Zero);
            else PhysicsServer.SpaceStep(Space, dt);
        }
        public void Dispose()
        {
            Store?.Dispose(); foreach (var body in _bodies) PhysicsServer.FreeRID(body);
            if (Space.IsValid()) PhysicsServer.FreeRID(Space);
            foreach (var shape in _shapes) shape.Dispose();
        }
    }
    private static void VerifyProjectDefaults(bool resident)
    {
        var old = PhysicsSleepSettings.FromProject();
        Check(old == new PhysicsSleepSettings(2, .13962634f, .5f), "Default project sleep profile.");
        try
        {
            ProjectSettings.Set(ProjectSettings.Physics2DSleepThresholdLinear, 7f);
            ProjectSettings.Set(ProjectSettings.Physics2DSleepThresholdAngular, .25f);
            ProjectSettings.Set(ProjectSettings.Physics2DTimeBeforeSleep, .125f);
            using var r = new Rig(resident); Check(r.Policy == new PhysicsSleepSettings(7, .25f, .125f), "World captures all project settings.");
            ProjectSettings.Set(ProjectSettings.Physics2DSleepThresholdLinear, 8f);
            Check(r.Policy.LinearThreshold == 7, "Existing world retains its captured setting.");
            using var next = new Rig(resident); Check(next.Policy.LinearThreshold == 8, "New world observes changed project default.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.Physics2DSleepThresholdLinear, old.LinearThreshold);
            ProjectSettings.Set(ProjectSettings.Physics2DSleepThresholdAngular, old.AngularThreshold);
            ProjectSettings.Set(ProjectSettings.Physics2DTimeBeforeSleep, old.TimeToSleep);
        }
    }
    private static void VerifyPolicy(bool resident, bool stages)
    {
        using var r = new Rig(resident, stages); r.Set(new(2, .25f, .125f));
        var small = r.Add(angular: .125f, radius: 1); var large = r.Add(angular: .125f, radius: 100);
        var linearEdge = r.Add(new(2, 0)); var angularEdge = r.Add(angular: -.25f); var forbidden = r.Add(canSleep: false);
        // Binary-exact duration/steps distinguish strict threshold and delay semantics without rounding tolerance.
        for (var i = 0; i < 4; i++) r.Step();
        Check(!r.Sleeping(small) && !r.Sleeping(large), "Duration equality does not yet sleep.");
        r.Step(); Check(r.Sleeping(small) && r.Sleeping(large), "Quiet angular motion sleeps independent of shape radius.");
        Check(!r.Sleeping(linearEdge) && !r.Sleeping(angularEdge) && !r.Sleeping(forbidden), "Threshold equality and CanSleep=false stay active.");
        var current = r.Policy; r.Set(current); r.Step(0); Check(r.Sleeping(small), "Equal settings and zero time preserve sleep.");
        r.Set(current with { TimeToSleep = .25f }); r.Step(0); Check(!r.Sleeping(small), "Changed policy wakes sleeping bodies without a simulation tick.");
        for (var i = 0; i < 8; i++) r.Step(); Check(!r.Sleeping(small), "Changing settings restarts quiet time.");
        r.Step(); Check(r.Sleeping(small), "Body sleeps after the new quiet duration.");
        foreach (var invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
        {
            Reject<ArgumentOutOfRangeException>(() => r.Set(r.Policy with { LinearThreshold = invalid }));
            Reject<ArgumentOutOfRangeException>(() => r.Set(r.Policy with { AngularThreshold = invalid }));
            Reject<ArgumentOutOfRangeException>(() => r.Set(r.Policy with { TimeToSleep = invalid }));
        }
        Check(r.Policy == current with { TimeToSleep = .25f } && r.Sleeping(small), "Rejected settings leave policy and sleeping body untouched.");
        r.Set(new(0, .25f, 0)); r.Step(); Check(!r.Sleeping(small), "Zero linear threshold disables automatic sleep at rest.");
        r.Set(new(2, 0, 0)); r.Step(); Check(!r.Sleeping(small), "Zero angular threshold disables automatic sleep at rest.");
        r.Set(new(2, .25f, 0)); r.Step(0); Check(!r.Sleeping(small), "Zero-duration policy still requires positive elapsed time.");
        r.Step(); Check(r.Sleeping(small), "Zero duration sleeps a quiet body on its first positive tick.");
        r.Set(new(0, 0, 0)); r.Sleep(small); r.Step(); Check(r.Sleeping(small), "Explicit sleep remains available with zero automatic thresholds.");
        Reject<InvalidOperationException>(() => Task.Run(() => r.Set(new(2, .25f, .5f))).GetAwaiter().GetResult());
    }
    private static void VerifyPublicLifetime(bool stages)
    {
        using var r = new Rig(false, stages); r.Set(new(2, .25f, .125f)); var body = r.Add();
        using var view = PhysicsServer.BodyGetDirectState(r.Body(body))!;
        r.Step(); PhysicsServer.SpaceSetActive(r.Space, false);
        for (var i = 0; i < 8; i++) r.Step();
        PhysicsServer.SpaceSetActive(r.Space, true); r.Step();
        Check(!r.Sleeping(body), "Inactive steps do not age quiet time.");
        PhysicsServer.BodySetStateSyncCallback(r.Body(body), _ =>
        {
            PhysicsServer.SpaceSetBodyTimeToSleep(r.Space, 0);
            throw new ApplicationException("Expected callback failure after policy change.");
        });
        Reject<AggregateException>(() => r.Step());
        PhysicsServer.BodySetStateSyncCallback(r.Body(body), null);
        Check(PhysicsServer.SpaceGetBodyTimeToSleep(r.Space) == 0 && !view.Sleeping, "Post-solver callback change commits and preserves the live view after callback failure.");
        r.Step(); Check(r.Sleeping(body), "Next interval uses the callback's new policy.");
        Reject<ArgumentException>(() => PhysicsServer.SpaceGetBodyTimeToSleep(r.Body(body)));
        var dead = PhysicsServer.SpaceCreate(); PhysicsServer.FreeRID(dead);
        Reject<ArgumentException>(() => PhysicsServer.SpaceSetBodyTimeToSleep(dead, 0));
    }
    private static void VerifyScene(bool stages)
    {
        using var shape = new CircleShape { Radius = 100 };
        var root = new Node(); var body = new RigidBody
        {
            Name = "Body",
            GravityScale = 0,
            AngularVelocity = .125f,
            AngularDampMode = RigidBody.DampMode.Replace,
            AngularDamp = 0
        };
        body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        using var tree = new SceneTree(root); var space = body.GetWorld()!.Space;
        if (stages) body.Space!.EnableGPUSolver();
        PhysicsServer.SpaceSetBodyLinearVelocitySleepThreshold(space, 2); PhysicsServer.SpaceSetBodyAngularVelocitySleepThreshold(space, .25f);
        PhysicsServer.SpaceSetBodyTimeToSleep(space, .125f);
        var transitions = 0; var guards = 0; body.SleepingStateChanged += _ => transitions++;
        body.NotifyLocalTransformChanges = true;
        body.LocalTransformChanged += _ =>
        {
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetBodyTimeToSleep(space, 1));
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetBodyTimeToSleep(space)); guards++;
        };
        for (var i = 0; i < 5; i++) tree.PhysicsFrame(.03125);
        Check(body.Sleeping && transitions == 1 && guards > 0, "Scene sleep transition and in-solver policy guards.");
        PhysicsServer.SpaceSetBodyTimeToSleep(space, .25f);
        Check(!body.Sleeping, "Scene property immediately observes policy wake.");
        tree.PhysicsFrame(.03125); Check(transitions == 2, "Policy wake publishes one scene sleep transition.");
        PhysicsServer.SpaceSetBodyTimeToSleep(space, .25f);
        tree.PhysicsFrame(.03125); Check(transitions == 2, "Equal policy does not add scene transitions.");
    }
    private static void Measure(bool resident, bool stages, bool moving = true)
    {
        using var r = new Rig(resident, stages); var duration = moving ? .5f : 0; r.Set(new(2, .25f, duration));
        for (var i = 0; i < 64; i++) r.Add(moving ? new(4, 0) : Vector2.Zero);
        for (var i = 0; i < 256; i++) { r.Set(new(1 + (i & 1), .25f, duration)); r.Step(1d / 60); }
        Check(r.Sleeping(0) == !moving && r.Sleeping(63) == !moving, "Measured population exercises active or wake/sleep cycles.");
        var samples = new long[128]; var before = GC.GetTotalAllocatedBytes(true);
        var upload = r.Store?.UploadBytes ?? 0; var uniforms = r.Store?.UniformBytes ?? 0; var readback = r.Store?.ReadbackBytes ?? 0; var wait = r.Store?.WaitMS ?? 0;
        for (var i = 0; i < samples.Length; i++)
        {
            var start = Stopwatch.GetTimestamp(); r.Set(new(1 + (i & 1), .25f, duration)); r.Step(1d / 60); samples[i] = Stopwatch.GetTimestamp() - start;
        }
        var bytes = GC.GetTotalAllocatedBytes(true) - before; Array.Sort(samples); var ms = 1000d / Stopwatch.Frequency;
        Console.WriteLine($"Sleep policy {(resident ? "resident GPU" : stages ? "CPU host/GPU stages" : "CPU")}: 64 {(moving ? "awake" : "wake/sleep cycling")} isolated circles, alternating linear threshold + whole 1/60 s step, 256 warmup/128 samples; p50/p95/p99={samples[64] * ms:F4}/{samples[121] * ms:F4}/{samples[126] * ms:F4} ms; {bytes} all-thread managed B.");
        if (r.Store is { } s) Console.WriteLine($"Resident sleep traffic/frame: upload={(s.UploadBytes - upload) / 128}, uniforms={(s.UniformBytes - uniforms) / 128}, readback={(s.ReadbackBytes - readback) / 128} B; wait={(s.WaitMS - wait) / 128:F4} ms; {s.Driver}, {s.DeviceName}.");
        Check(bytes == 0, "Warmed policy edits and steps allocate no managed memory.");
    }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
    private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}
