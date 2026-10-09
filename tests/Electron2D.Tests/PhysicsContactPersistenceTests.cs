using System.Diagnostics;
using Box2D.NET;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class PhysicsContactPersistenceTests
{
    internal static void Run(bool stages = false)
    {
        if (stages) GPUPhysicsTests.RunContactRecycling();
        VerifyRules(); VerifySettings(false); VerifyReuse(false, stages); VerifyScene(stages); Measure(false, stages);
        Console.WriteLine($"Contact recycling passed on {(stages ? "CPU host/GPU stages" : "CPU")}.");
    }
    internal static void RunResident()
    {
        VerifySettings(true); VerifyReuse(true, false); VerifyResidentOwnership(); Measure(true, false);
        Console.WriteLine("Contact recycling passed on resident GPU.");
    }
    private sealed class Rig : IDisposable
    {
        internal readonly Store? GPU;
        internal readonly RID Space, Body, Floor;
        internal readonly Store.BodyHandle ResidentBody;
        private readonly Shape _shape;
        private readonly Shape _plane;
        private readonly PhysicsDirectBodyState? _view;
        internal Rig(bool resident, bool stages = false, Shape? shape = null, float speed = 400, bool boundary = false)
        {
            _shape = shape ?? new CircleShape { Radius = 5 };
            _plane = boundary ? new WorldBoundaryShape() : new RectangleShape { Size = new(10000, 100) };
            var ground = boundary ? Vector2.Zero : new Vector2(0, 50);
            if (resident)
            {
                GPU = new() { CaptureContactReports = true };
                var floor = GPU.Add(new(Mode.Static, ground, 0, Vector2.Zero, 0)); GPU.AddShape(floor, _plane, friction: 0);
                ResidentBody = GPU.Add(new(Mode.RigidLinear, new(0, -4.9f), 0, new(speed, 0), 0, Mass: 1, CanSleep: false));
                GPU.AddShape(ResidentBody, _shape, friction: 0);
            }
            else
            {
                Space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(Space, true);
                var fields = PhysicsServer.Service.GetSceneSpace(Space).DefaultAreaFields; fields.Gravity = 980; fields.LinearDamp = fields.AngularDamp = 0;
                Floor = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(Floor, Mode.Static); PhysicsServer.BodyAddShape(Floor, _plane.GetRID());
                PhysicsServer.BodySetTransform(Floor, new(0, ground)); PhysicsServer.BodySetFriction(Floor, 0); PhysicsServer.BodySetSpace(Floor, Space);
                Body = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(Body, Mode.RigidLinear); PhysicsServer.BodyAddShape(Body, _shape.GetRID());
                PhysicsServer.BodySetMass(Body, 1); PhysicsServer.BodySetTransform(Body, new(0, new(0, -4.9f)));
                PhysicsServer.BodySetLinearVelocity(Body, new(speed, 0)); PhysicsServer.BodySetFriction(Body, 0); PhysicsServer.BodySetCanSleep(Body, false);
                PhysicsServer.BodySetMaxContactsReported(Body, 16); PhysicsServer.BodySetSpace(Body, Space);
                if (stages) PhysicsServer.Service.GetSceneSpace(Space).EnableGPUSolver();
                _view = PhysicsServer.BodyGetDirectState(Body)!;
            }
        }
        internal PhysicsContactSettings Settings => GPU?.GetContactSettings() ?? PhysicsServer.Service.GetSceneSpace(Space).ContactSettings;
        internal void Set(float radius, float separation)
        {
            if (GPU is { } gpu) gpu.SetContactSettings(Settings with { RecycleRadius = radius, MaxSeparation = separation });
            else { PhysicsServer.SpaceSetContactRecycleRadius(Space, radius); PhysicsServer.SpaceSetContactMaxSeparation(Space, separation); }
        }
        internal void Step() { if (GPU is { } gpu) gpu.Simulate(.01f, new(0, 980)); else PhysicsServer.SpaceStep(Space, .01); }
        internal int Reused()
        {
            if (GPU is { } gpu) return gpu.WarmStartedPointCount;
            var world = B2Worlds.b2GetWorldFromId(PhysicsServer.Service.GetSceneSpace(Space).WorldID); var count = 0;
            foreach (var color in world.constraintGraph.colors.AsSpan())
                for (var i = 0; i < color.contactSims.count; i++)
                { var m = color.contactSims.data[i].manifold; for (var j = 0; j < m.pointCount; j++) if (m.points[j].persisted) count++; }
            return count;
        }
        internal (Vector2 Velocity, Vector2 Impulse, int Contacts) Read()
        {
            var impulse = Vector2.Zero;
            if (GPU is { } gpu)
            {
                Span<Store.Snapshot> state = stackalloc Store.Snapshot[1]; gpu.Read([ResidentBody], state);
                Span<Store.ContactReport> reports = stackalloc Store.ContactReport[16]; Span<int> count = stackalloc int[1];
                gpu.ReadContactReports([ResidentBody], [16], count, reports); foreach (var c in reports[..count[0]]) impulse += c.Impulse;
                return (new(state[0].Velocity.X, state[0].Velocity.Y), impulse, count[0]);
            }
            var contacts = _view!.GetContactCount(); for (var i = 0; i < contacts; i++) impulse += _view.GetContactImpulse(i);
            return (_view.LinearVelocity, impulse, contacts);
        }
        public void Dispose()
        {
            _view?.Dispose(); GPU?.Dispose(); if (Space.IsValid()) { PhysicsServer.FreeRID(Body); PhysicsServer.FreeRID(Floor); PhysicsServer.FreeRID(Space); }
            _shape.Dispose(); _plane.Dispose();
        }
    }
    private static void VerifyRules()
    {
        var old = new B2ManifoldPoint { localAnchorA = new(0, 0), localAnchorB = new(0, -.125f) };
        var current = old; var a = B2MathFunction.b2Transform_identity; var b = a;
        float Distance(float radius, float separation) => B2Contacts.ContactHistoryDistance(radius, separation, old, current, new(0, 1), a, b);
        Check(float.IsPositiveInfinity(Distance(0, 10)), "Zero radius disables even exact-point reuse.");
        Check(Distance(1, 0) == 0, "Zero separation retains coincident tangent anchors with penetration.");
        current.localAnchorA.X = .5f; Check(Distance(1, 1) == .25f && float.IsPositiveInfinity(Distance(.5f, 1)), "Both local anchor distances use a strict radius boundary.");
        current = old; current.localAnchorB.X = .5f; Check(float.IsPositiveInfinity(Distance(.5f, 1)), "The second body's anchor is also bounded.");
        current = old; b.p.X = .5f; Check(Distance(1, .5f) == 0 && float.IsPositiveInfinity(Distance(1, .499f)), "Tangential drift uses an inclusive separation bound.");
        b.p = new(0, .625f); Check(Distance(1, .5f) == 0 && float.IsPositiveInfinity(Distance(1, .499f)), "Positive normal separation invalidates history independently of local anchor proximity.");
    }
    private static void VerifySettings(bool resident)
    {
        var old = PhysicsContactSettings.FromProject(); Check(old.RecycleRadius == 1 && old.MaxSeparation == 1.5f, "Captured project defaults.");
        try
        {
            ProjectSettings.Set(ProjectSettings.Physics2DContactRecycleRadius, 4.5f); ProjectSettings.Set(ProjectSettings.Physics2DContactMaxSeparation, 2.5f);
            using var r = new Rig(resident); Check(r.Settings.RecycleRadius == 4.5f && r.Settings.MaxSeparation == 2.5f, "New worlds capture both settings.");
            ProjectSettings.Set(ProjectSettings.Physics2DContactRecycleRadius, 1f); Check(r.Settings.RecycleRadius == 4.5f, "Existing worlds retain their policy.");
            foreach (var invalid in new[] { -1f, float.NaN, float.PositiveInfinity, float.MaxValue, float.Epsilon, 1e-30f })
            {
                Reject<ArgumentOutOfRangeException>(() => r.Set(invalid, r.Settings.MaxSeparation));
                Reject<ArgumentOutOfRangeException>(() => r.Set(r.Settings.RecycleRadius, invalid));
            }
            Check(r.Settings.RecycleRadius == 4.5f && r.Settings.MaxSeparation == 2.5f, "Invalid policy writes preserve both fields.");
            Task.Run(() => Reject<InvalidOperationException>(() => r.Set(1, 1))).GetAwaiter().GetResult();
            if (!resident)
            {
                Check(PhysicsServer.SpaceGetContactRecycleRadius(r.Space) == 4.5f && PhysicsServer.SpaceGetContactMaxSeparation(r.Space) == 2.5f, "Typed getters return the actual world policy.");
                Reject<ArgumentException>(() => PhysicsServer.SpaceGetContactRecycleRadius(default));
                PhysicsServer.SpaceSetActive(r.Space, false); r.Set(2, 3); r.Step(); Check(PhysicsServer.SpaceGetContactMaxSeparation(r.Space) == 3, "Inactive worlds remain configurable.");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DContactRecycleRadius, old.RecycleRadius); ProjectSettings.Set(ProjectSettings.Physics2DContactMaxSeparation, old.MaxSeparation); }
    }
    private static Shape[] Family() => [new CircleShape { Radius = 5 }, new RectangleShape { Size = new(10, 10) }, new CapsuleShape { Radius = 2, Height = 10 },
        new SegmentShape { A = new(-5, 5), B = new(5, 5) }, new ConvexPolygonShape { Points = [new(-5, -5), new(5, -5), new(5, 5), new(-5, 5)] },
        new ConcavePolygonShape { Segments = [new(-5, 5), new(5, 5)] }, new SeparationRayShape { Length = 5 }];
    private static void VerifyReuse(bool resident, bool stages)
    {
        foreach (var shape in Family())
        {
            using var r = new Rig(resident, stages, shape);
            foreach (var (radius, separation, reused) in new[] { (10f, 10f, true), (0f, 10f, false), (.5f, 10f, false), (10f, .5f, false), (10f, 10f, true) })
            {
                r.Set(radius, separation); for (var i = 0; i < 4; i++) r.Step();
                var state = r.Read(); var warm = r.Reused();
                Check((warm > 0) == reused, $"{shape.GetType().Name}, radius={radius}, separation={separation}: expected reuse={reused}, actual={warm}.");
                Check(state.Contacts > 0 && MathF.Abs(state.Velocity.X - 400) < .01f && MathF.Abs(state.Velocity.Y) < .02f, "History limits preserve actual contact support and tangent motion.");
                Check(state.Impulse.DistanceTo(new(0, -9.8f)) < .03f, $"Frame impulse balances gravity independently of reuse: {state.Impulse}.");
            }
        }
        using (var boundary = new Rig(resident, stages, boundary: true))
        {
            boundary.Set(10, 10); for (var i = 0; i < 4; i++) boundary.Step(); Check(boundary.Reused() > 0, "Infinite boundary contacts reuse history.");
            boundary.Set(0, 10); boundary.Step(); Check(boundary.Reused() == 0 && boundary.Read().Contacts > 0, "Infinite boundary history can be disabled without losing collisions.");
        }
        using var stationary = new Rig(resident, stages, speed: 0); stationary.Set(10, 0); for (var i = 0; i < 4; i++) stationary.Step();
        Check(stationary.Reused() > 0, "Zero maximum separation still accepts a stable penetrating contact.");
    }
    private static void VerifyResidentOwnership()
    {
        using var gpu = new Store { CaptureContactReports = true };
        using var box = new RectangleShape { Size = new(10, 10) }; using var ground = new RectangleShape { Size = new(100, 10) };
        var floor = gpu.Add(new(Mode.Static, new(0, 5), 0, Vector2.Zero, 0)); gpu.AddShape(floor, ground, friction: 0);
        var body = gpu.Add(new(Mode.Rigid, new(0, -5), .15f, Vector2.Zero, 0, CanSleep: false)); gpu.AddShape(body, box, friction: 0);
        gpu.SetContactSettings(new(0, .3f, 100, 100));
        Span<Store.ContactReport> reports = stackalloc Store.ContactReport[16]; Span<int> counts = stackalloc int[1];
        var previous = 0; var witnesses = 0;
        for (var tick = 0; tick < 24; tick++)
        {
            gpu.Simulate(.01f, new(0, 980), substeps: 1);
            Check(gpu.WarmStartedPointCount <= previous, "Parallel points cannot consume one previous impulse more than once.");
            if (previous > 0 && gpu.ContactPointCount > previous && gpu.WarmStartedPointCount > 0) witnesses++;
            gpu.ReadContactReports([body], [16], counts, reports); previous = 0;
            foreach (var report in reports[..counts[0]]) if (report.Impulse != Vector2.Zero) previous++;
        }
        Check(witnesses > 0, "The tilted-box fixture exercises more fresh points than reusable impulses.");
        Console.WriteLine($"Resident contact history ownership: {witnesses} competing-point intervals passed.");
    }
    private static void VerifyScene(bool stages)
    {
        using var shape = new CircleShape { Radius = 5 }; using var plane = new WorldBoundaryShape();
        using var root = new Node(); var floor = new StaticBody { Name = "Floor" }; floor.AddChild(new CollisionShape { Shape = plane }); root.AddChild(floor);
        var body = new RigidBody { Name = "Body", Position = new(0, -4.9f), GravityScale = 0, ContactMonitor = true, MaxContactsReported = 4 };
        body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body); using var tree = new SceneTree(root); var world = body.GetWorld()!.Space;
        if (stages) body.Space!.EnableGPUSolver();
        var entered = 0; var exited = 0; body.BodyEntered += _ => entered++; body.BodyExited += _ => exited++;
        tree.PhysicsFrame(.01); body.Sleeping = true;
        PhysicsServer.SpaceSetContactRecycleRadius(world, 1); Check(body.Sleeping, "Equal policy preserves sleep.");
        PhysicsServer.SpaceSetContactRecycleRadius(world, 0); Check(!body.Sleeping, "Changed policy wakes the body.");
        PhysicsServer.SpaceSetContactMaxSeparation(world, 0); body.Sleeping = true; tree.PhysicsFrame(.01);
        Check(body.Sleeping && entered == 1 && exited == 0, "Later explicit sleep wins and policy changes retain contact events.");
        var guards = 0; body.NotifyLocalTransformChanges = true;
        body.LocalTransformChanged += _ => { Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetContactMaxSeparation(world, 2)); guards++; };
        body.Sleeping = false; body.LinearVelocity = new(1, 0); tree.PhysicsFrame(.01); Check(guards > 0, "Solver-phase edits are guarded.");
    }
    private static void Measure(bool resident, bool stages)
    {
        using var r = new Rig(resident, stages); r.Set(10, 10);
        for (var i = 0; i < 256; i++) r.Step();
        var samples = new long[128]; var upload = r.GPU?.UploadBytes ?? 0; var read = r.GPU?.ReadbackBytes ?? 0; var uniforms = r.GPU?.UniformBytes ?? 0; var wait = r.GPU?.WaitMS ?? 0;
        var before = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < samples.Length; i++) { var t = Stopwatch.GetTimestamp(); r.Set((i & 1) == 0 ? 10 : 0, 10); r.Step(); samples[i] = Stopwatch.GetTimestamp() - t; }
        var bytes = GC.GetTotalAllocatedBytes(true) - before; Array.Sort(samples); var ms = 1000d / Stopwatch.Frequency;
        Console.WriteLine($"Contact history {(resident ? "resident GPU" : stages ? "stage GPU" : "CPU")}: 1 sliding body, 4 substeps/16 sweeps, 256 warmup/128 edited whole steps; p50/p95/p99={samples[64] * ms:F4}/{samples[121] * ms:F4}/{samples[126] * ms:F4} ms, {bytes} all-thread managed B.");
        if (r.GPU is { } gpu) Console.WriteLine($"History traffic/tick: upload={(gpu.UploadBytes - upload) / 128}, readback={(gpu.ReadbackBytes - read) / 128}, uniforms={(gpu.UniformBytes - uniforms) / 128} B, wait={(gpu.WaitMS - wait) / 128:F4} ms; {gpu.Driver}/{gpu.DeviceName}.");
        Check(bytes == 0, "Warmed policy edits and history matching allocate zero managed memory.");
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
