using System.Diagnostics;
using Electron2D;
using IOPath = System.IO.Path;

internal static class PhysicsContactPolicyTests
{
    internal static void Run(bool stages = false)
    {
        VerifyResources(); VerifyDefaults(false); VerifyCorrection(false, stages); VerifyLiveEdits(false, stages); VerifyScene(stages); Measure(false, stages);
        Console.WriteLine($"Contact policy passed on {(stages ? "CPU host/GPU stages" : "CPU")}: bias/slack, shape mixing, duration, resource state and live edits.");
    }
    internal static void RunResident()
    {
        VerifyDefaults(true); VerifyCorrection(true, false); VerifyLiveEdits(true, false); Measure(true, false);
        Console.WriteLine("Resident contact policy passed: shared world defaults, shape mixing, duration, live metadata and allocation.");
    }
    private sealed class Rig : IDisposable
    {
        internal readonly GPUPhysicsBodyStore? Store;
        internal readonly Shape Shape;
        internal readonly WorldBoundaryShape FloorShape = new();
        internal readonly RID Space, Body;
        private readonly PhysicsDirectBodyState? _view;
        private readonly List<RID> _floors = [];
        private CircleShape? _bulletShape;
        internal readonly GPUPhysicsBodyStore.BodyHandle ResidentBody;
        internal Rig(bool resident, bool stages = false, Shape? shape = null, int floors = 1)
        {
            Shape = shape ?? new CircleShape { Radius = 5 };
            if (resident)
            {
                Store = new() { CaptureContactReports = true };
                for (var i = 0; i < floors; i++)
                {
                    var floor = Store.Add(new(PhysicsServer.BodyMode.Static, new(0, 5), 0, Vector2.Zero, 0)); Store.AddShape(floor, FloorShape);
                }
                ResidentBody = Store.Add(new(PhysicsServer.BodyMode.RigidLinear, new(0, 1), 0, Vector2.Zero, 0, Mass: 2, CanSleep: false));
                Store.AddShape(ResidentBody, Shape);
            }
            else
            {
                Space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(Space, true);
                PhysicsServer.AreaSetGravity(Space, 0); PhysicsServer.AreaSetLinearDamp(Space, 0); PhysicsServer.AreaSetAngularDamp(Space, 0);
                if (stages) PhysicsServer.Service.GetSceneSpace(Space).EnableGPUSolver();
                for (var i = 0; i < floors; i++)
                {
                    var floor = PhysicsServer.BodyCreate(); _floors.Add(floor);
                    PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(floor, FloorShape.GetRID());
                    PhysicsServer.BodySetTransform(floor, new(0, new(0, 5))); PhysicsServer.BodySetSpace(floor, Space);
                }
                Body = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(Body, PhysicsServer.BodyMode.RigidLinear); PhysicsServer.BodySetMass(Body, 2);
                PhysicsServer.BodySetCanSleep(Body, false); PhysicsServer.BodySetMaxContactsReported(Body, 32);
                PhysicsServer.BodyAddShape(Body, Shape.GetRID()); PhysicsServer.BodySetTransform(Body, new(0, new(0, 1))); PhysicsServer.BodySetSpace(Body, Space);
                _view = PhysicsServer.BodyGetDirectState(Body)!;
            }
        }
        internal PhysicsContactSettings Settings => Store?.GetContactSettings() ?? new(
            PhysicsServer.SpaceGetContactDefaultBias(Space), PhysicsServer.SpaceGetContactMaxAllowedPenetration(Space));
        internal void Set(PhysicsContactSettings value)
        {
            if (Store is { } s) s.SetContactSettings(value);
            else { PhysicsServer.SpaceSetContactDefaultBias(Space, value.Bias); PhysicsServer.SpaceSetContactMaxAllowedPenetration(Space, value.AllowedPenetration); }
        }
        internal void Reset(float y = 1, Vector2 velocity = default)
        {
            if (Store is { } s) { s.SetPose(ResidentBody, new(0, y), 0); s.SetVelocity(ResidentBody, velocity, 0); }
            else { PhysicsServer.BodySetTransform(Body, new(0, new(0, y))); PhysicsServer.BodySetLinearVelocity(Body, velocity); }
        }
        internal void AddBullet()
        {
            _bulletShape = new CircleShape { Radius = 1 };
            if (Store is { } s)
            {
                var bullet = s.Add(new(PhysicsServer.BodyMode.RigidLinear, new(200, -95), 0, new(0, 6000), 0, CanSleep: false, ContinuousMode: CCDMode.CastShape));
                s.AddShape(bullet, _bulletShape);
            }
            else
            {
                var bullet = PhysicsServer.BodyCreate(); _floors.Add(bullet);
                PhysicsServer.BodySetMode(bullet, PhysicsServer.BodyMode.RigidLinear); PhysicsServer.BodyAddShape(bullet, _bulletShape.GetRID());
                PhysicsServer.BodySetTransform(bullet, new(0, new(200, -95))); PhysicsServer.BodySetLinearVelocity(bullet, new(0, 6000));
                PhysicsServer.BodySetContinuousCollisionDetectionMode(bullet, CCDMode.CastShape); PhysicsServer.BodySetCanSleep(bullet, false); PhysicsServer.BodySetSpace(bullet, Space);
            }
        }
        internal void Step(float delta = .01f, int substeps = 4)
        {
            if (Store is { } s) s.Simulate(delta, Vector2.Zero, substeps: substeps, iterations: 32);
            else PhysicsServer.SpaceStep(Space, delta);
        }
        internal (float Y, Vector2 Velocity, Vector2 Impulse, int Contacts) Read()
        {
            var impulse = Vector2.Zero;
            if (Store is { } s)
            {
                Span<GPUPhysicsBodyStore.Snapshot> state = stackalloc GPUPhysicsBodyStore.Snapshot[1]; s.Read([ResidentBody], state);
                Span<GPUPhysicsBodyStore.ContactReport> reports = stackalloc GPUPhysicsBodyStore.ContactReport[32]; Span<int> count = stackalloc int[1];
                s.ReadContactReports([ResidentBody], [32], count, reports); foreach (var r in reports[..count[0]]) impulse += r.Impulse;
                return (state[0].Position.Y, new(state[0].Velocity.X, state[0].Velocity.Y), impulse, count[0]);
            }
            var contacts = _view!.GetContactCount(); for (var i = 0; i < contacts; i++) impulse += _view.GetContactImpulse(i);
            return (_view.Transform.Origin.Y, _view.LinearVelocity, impulse, contacts);
        }
        public void Dispose()
        {
            Store?.Dispose(); _view?.Dispose();
            if (Space.IsValid()) { PhysicsServer.FreeRID(Body); foreach (var floor in _floors) PhysicsServer.FreeRID(floor); PhysicsServer.FreeRID(Space); }
            Shape.Dispose(); FloorShape.Dispose(); _bulletShape?.Dispose();
        }
    }
    private static void VerifyDefaults(bool resident)
    {
        var old = PhysicsContactSettings.FromProject(); Check(old == new PhysicsContactSettings(.8f, .3f), "World contact defaults.");
        try
        {
            ProjectSettings.Set(ProjectSettings.Physics2DDefaultContactBias, .4f); ProjectSettings.Set(ProjectSettings.Physics2DContactMaxAllowedPenetration, .2f);
            using var r = new Rig(resident); Check(r.Settings == new PhysicsContactSettings(.4f, .2f), "World captures both project values.");
            ProjectSettings.Set(ProjectSettings.Physics2DDefaultContactBias, .6f); Check(r.Settings.Bias == .4f, "Existing world retains captured settings.");
            foreach (var invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
            {
                Reject<ArgumentOutOfRangeException>(() => r.Set(r.Settings with { Bias = invalid }));
                Reject<ArgumentOutOfRangeException>(() => r.Set(r.Settings with { AllowedPenetration = invalid }));
            }
            Reject<ArgumentOutOfRangeException>(() => r.Set(r.Settings with { Bias = 1.01f }));
            Check(r.Settings == new PhysicsContactSettings(.4f, .2f), "Invalid world values preserve settings.");
            Reject<InvalidOperationException>(() => Task.Run(() => r.Set(new(.5f, .2f))).GetAwaiter().GetResult());
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DDefaultContactBias, old.Bias); ProjectSettings.Set(ProjectSettings.Physics2DContactMaxAllowedPenetration, old.AllowedPenetration); }
    }
    private static Shape[] Family() => [new CircleShape { Radius = 5 }, new RectangleShape { Size = new(10, 10) }, new CapsuleShape { Radius = 2, Height = 10 },
        new SegmentShape { A = new(-5, 5), B = new(5, 5) }, new ConvexPolygonShape { Points = [new(-5, -5), new(5, -5), new(5, 5), new(-5, 5)] },
        new ConcavePolygonShape { Segments = [new(-5, 5), new(5, 5)] }, new SeparationRayShape { Length = 5 }];
    private static void VerifyCorrection(bool resident, bool stages)
    {
        foreach (var shape in Family())
        {
            using var r = new Rig(resident, stages, shape);
            foreach (var (own, floor, world, effective) in new[] { (0f, 0f, .8f, .8f), (.25f, 0f, .8f, .25f), (0f, .5f, .8f, .5f), (.25f, .75f, .8f, .5f), (0f, 0f, 0f, 0f) })
            {
                r.Shape.CustomSolverBias = own; r.FloorShape.CustomSolverBias = floor; r.Set(new(world, .3f)); r.Reset(); r.Step();
                var state = r.Read();
                // One unit initial penetration, .3 unit slack: ideal residual is 1 - bias * .7, independent of the four internal intervals.
                Near(state.Y, 1 - effective * .7f, .003f, $"{shape.GetType().Name} mixed correction");
                Near(state.Velocity, Vector2.Zero, .003f, "Correction does not create persistent kinetic velocity");
                Near(state.Impulse, state.Velocity * 2, .01f, "Contact reports account for actual momentum only");
                Check(state.Contacts > 0, "Zero bias retains collision reporting.");
            }
        }
        using (var r = new Rig(resident, stages, floors: 12))
        {
            r.Set(new(.5f, .3f)); r.Step(); Near(r.Read().Y, .65f, .01f, "Dense incident contacts include CPU scalar overflow and preserve correction");
        }
        using (var r = new Rig(resident, stages))
        {
            foreach (var dt in new[] { .01f, .04f }) foreach (var substeps in resident ? new[] { 1, 4, 8 } : new[] { 4 })
                {
                    r.Set(new(.8f, .3f)); r.Reset(); r.Step(dt, substeps); Near(r.Read().Y, .44f, .003f, "Outer-tick correction is independent of duration and substeps");
                }
            r.AddBullet(); r.Reset(); r.Step(.04f);
            // CCD clips a prepared correction velocity at impact, then resolves the remainder. One nominal-substep
            // linearization contributes at most .004 u here; it must not reapply a whole tick's .8 fraction per hit.
            Near(r.Read().Y, .44f, .004f, "CCD continuation retains the nominal contact correction budget");
            Check(r.Store is { } gpu ? gpu.CCDIntervalCount > 0 : PhysicsServer.Service.GetSceneSpace(r.Space).ContinuousQueryCount > 0, "CCD control actually executes.");
            r.Set(new(.8f, 1.2f)); r.Reset(); r.Step(); var within = r.Read(); Near(within.Y, 1, .002f, "Penetration within slack has no positional correction"); Check(within.Contacts > 0, "Slack does not filter contacts.");
            r.Set(new(.8f, .3f)); var velocity = new Vector2(30, 80); r.Reset(1, velocity); r.Step(); var impact = r.Read();
            Near(impact.Impulse, (impact.Velocity - velocity) * 2, .03f, "Friction and normal reports preserve full momentum with correction enabled");
            Check(impact.Impulse.Y < -1 && impact.Impulse.X < -1, "Both material impulse channels remain active.");
        }
    }
    private static void VerifyLiveEdits(bool resident, bool stages)
    {
        using var r = new Rig(resident, stages); r.Set(new(.8f, .3f)); r.Step();
        var revision = r.Shape.GeometryRevision; var bounds = r.Shape.GetRect();
        r.Shape.CustomSolverBias = .5f; Check(r.Shape.GeometryRevision == revision && r.Shape.GetRect() == bounds, "Bias edits preserve geometry.");
        r.Reset(); r.Step(); Near(r.Read().Y, .65f, .003f, "Live bias reaches existing body.");
        Action<Resource> fail = _ => throw new ApplicationException("Expected resource observer failure."); r.Shape.Changed += fail;
        Reject<ApplicationException>(() => r.Shape.CustomSolverBias = .25f); r.Shape.Changed -= fail;
        r.Reset(); r.Step(); Near(r.Read().Y, .825f, .003f, "Committed policy survives Changed subscriber failure.");
        if (!resident)
        {
            var rid = r.Shape.GetRID(); using var copy = PhysicsServer.ShapeGetData(rid);
            Check(copy.CustomSolverBias == .25f && rid == r.Shape.GetRID(), "Server data copying preserves bias and borrowed identity.");
            PhysicsServer.BodySetMode(r.Body, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetMode(r.Body, PhysicsServer.BodyMode.RigidLinear);
            r.Reset(); r.Step(); Near(r.Read().Y, .825f, .003f, "Mode changes restore then mask solver inertia.");
        }
        else
        {
            var uploads = r.Store!.GeometryUploadBytes; r.Shape.CustomSolverBias = .4f; r.Reset(); r.Step();
            Check(r.Store.GeometryUploadBytes == uploads, "Bias edits do not upload vertex geometry.");
        }
    }
    private static void VerifyScene(bool stages)
    {
        using var shape = new CircleShape { Radius = 5 }; using var floorShape = new WorldBoundaryShape();
        var root = new Node(); var floor = new StaticBody { Name = "Floor", Position = new(0, 5) };
        floor.AddChild(new CollisionShape { Shape = floorShape }); root.AddChild(floor);
        var body = new RigidBody
        {
            Name = "Body",
            Position = new(0, 1),
            GravityScale = 0,
            LockRotation = true,
            Inertia = 10,
            ContactMonitor = true,
            MaxContactsReported = 8,
            LinearDampMode = RigidBody.DampMode.Replace,
            LinearDamp = 0
        };
        body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        using var tree = new SceneTree(root); var space = body.GetWorld()!.Space;
        if (stages) body.Space!.EnableGPUSolver();
        PhysicsServer.SpaceSetContactDefaultBias(space, 0); PhysicsServer.SpaceSetContactMaxAllowedPenetration(space, 1.2f);
        var entered = 0; var exited = 0; body.BodyEntered += _ => entered++; body.BodyExited += _ => exited++;
        for (var i = 0; i < 90; i++) tree.PhysicsFrame(1d / 60);
        Check(body.Sleeping && entered == 1 && exited == 0, "Supported overlap sleeps and reports one body entry.");
        var inertia = PhysicsServer.BodyGetInertia(body.GetRID()); shape.CustomSolverBias = .5f;
        tree.PhysicsFrame(1d / 60); Check(!body.Sleeping && entered == 1 && exited == 0, "Shape policy wakes without replacing contact membership.");
        Near(PhysicsServer.BodyGetInertia(body.GetRID()), inertia, 0, "Bias preserves mass profile");
        for (var i = 0; i < 90; i++) tree.PhysicsFrame(1d / 60);
        Check(body.Sleeping, "Body settles after policy change.");
        floorShape.CustomSolverBias = .25f; tree.PhysicsFrame(1d / 60);
        Check(!body.Sleeping && entered == 1 && exited == 0, "Static-shape policy wakes touching dynamic bodies.");
        body.LockRotation = false; body.ApplyTorqueImpulse(1); Near(PhysicsServer.BodyGetAngularVelocity(body.GetRID()), .1f, .0001f, "Unlock restores inverse inertia");
        body.LockRotation = true; Near(PhysicsServer.BodyGetAngularVelocity(body.GetRID()), 0, 0, "Lock clears angular motion");
        var guards = 0; body.NotifyLocalTransformChanges = true;
        body.LocalTransformChanged += _ => { Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetContactDefaultBias(space, .4f)); guards++; };
        body.LinearVelocity = new(1, 0); tree.PhysicsFrame(.01); Check(guards > 0, "World contact policy rejects in-solver scene callbacks.");
    }
    private static void VerifyResources()
    {
        var folder = IOPath.Combine(IOPath.GetTempPath(), "electron2d-contact-policy-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var shapes = Family().Append<Shape>(new WorldBoundaryShape { Normal = new(0, -2), Distance = 6 }).ToArray();
        try
        {
            foreach (var shape in shapes)
            {
                shape.CustomSolverBias = .6f; using var copy = (Shape)shape.Duplicate();
                Check(copy.CustomSolverBias == .6f && copy.GetRect() == shape.GetRect(), "Every shape duplicates its inherited policy and geometry.");
                shape.CustomSolverBias = .2f; Check(copy.CustomSolverBias == .6f, "Duplicate is independent.");
                copy.CopyFromResource(shape); Check(copy.CustomSolverBias == .2f, "CopyFromResource includes inherited policy.");
                var path = IOPath.Combine(folder, shape.GetType().Name + ".e2dres"); ResourceSaver.Save(shape, path);
                using var loaded = ResourceLoader.Load<Shape>(path, ResourceLoader.CacheMode.Ignore)!;
                Check(loaded.GetType() == shape.GetType() && loaded.CustomSolverBias == .2f && loaded.GetRect() == shape.GetRect(), $"Stored shape roundtrip: {shape.GetType().Name}.");
                Reject<ArgumentOutOfRangeException>(() => shape.CustomSolverBias = float.NaN);
                Reject<ArgumentOutOfRangeException>(() => shape.CustomSolverBias = 1.01f);
            }
        }
        finally { foreach (var shape in shapes) shape.Dispose(); Directory.Delete(folder, true); }
    }
    private static void Measure(bool resident, bool stages)
    {
        using var r = new Rig(resident, stages); r.Set(new(0, 1.2f));
        for (var i = 0; i < 256; i++) { r.Shape.CustomSolverBias = (i & 1) == 0 ? .25f : .5f; r.Step(1f / 60); }
        var upload = r.Store?.UploadBytes ?? 0; var uniforms = r.Store?.UniformBytes ?? 0; var readback = r.Store?.ReadbackBytes ?? 0; var wait = r.Store?.WaitMS ?? 0;
        var samples = new long[128]; var before = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < samples.Length; i++)
        {
            var start = Stopwatch.GetTimestamp(); r.Shape.CustomSolverBias = (i & 1) == 0 ? .25f : .5f; r.Step(1f / 60); samples[i] = Stopwatch.GetTimestamp() - start;
        }
        var bytes = GC.GetTotalAllocatedBytes(true) - before; Array.Sort(samples); var ms = 1000d / Stopwatch.Frequency;
        Console.WriteLine($"Contact policy {(resident ? "resident GPU" : stages ? "CPU host/GPU stages" : "CPU")}: one active contact, changing shape bias, 256 warmup/128 whole 1/60 s steps; p50/p95/p99={samples[64] * ms:F4}/{samples[121] * ms:F4}/{samples[126] * ms:F4} ms; {bytes} all-thread managed B.");
        if (r.Store is { } gpu) Console.WriteLine($"Resident contact policy traffic/frame: upload={(gpu.UploadBytes - upload) / 128}, uniforms={(gpu.UniformBytes - uniforms) / 128}, readback={(gpu.ReadbackBytes - readback) / 128} B; wait={(gpu.WaitMS - wait) / 128:F4} ms; {gpu.Driver}, {gpu.DeviceName}.");
        Check(bytes == 0, "Warmed shape policy edits and active steps allocate zero managed memory.");
    }
    private static void Near(float actual, float expected, float tolerance, string text) => Check(MathF.Abs(actual - expected) <= tolerance, $"{text}: {actual} vs {expected} (tolerance {tolerance}).");
    private static void Near(Vector2 actual, Vector2 expected, float tolerance, string text) => Check((actual - expected).Length() <= tolerance, $"{text}: {actual} vs {expected} (tolerance {tolerance}).");
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
    private static void Check(bool ok, string text) { if (!ok) throw new InvalidOperationException(text); }
}
