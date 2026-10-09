using System.Diagnostics;
using System.Runtime.InteropServices;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class PhysicsSolverIterationTests
{
    internal static void Run(bool stages = false)
    {
        VerifyDefaults(false); VerifyConvergence(false, stages); VerifyScene(stages); Measure(false, stages);
        if (!stages) VerifyLongParallelSchedule();
        Console.WriteLine($"World solver iterations passed on {(stages ? "CPU host/GPU stages" : "CPU")}.");
    }
    internal static void RunResident()
    {
        VerifyDefaults(true); VerifyConvergence(true, false); Measure(true, false);
        Console.WriteLine("World solver iterations passed on resident GPU.");
    }
    private sealed class Rig : IDisposable
    {
        internal readonly Store? GPU;
        internal readonly RID Space;
        internal readonly List<RID> Bodies = [], Joints = [];
        internal readonly List<Store.BodyHandle> Handles = [];
        private readonly CircleShape _shape = new() { Radius = 5 };
        internal Rig(bool resident, bool stages = false)
        {
            if (resident) { GPU = new(); GPU.SetContactSettings(new(0, .3f)); }
            else
            {
                Space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(Space, true);
                PhysicsServer.SpaceSetContactDefaultBias(Space, 0);
                if (stages) PhysicsServer.Service.GetSceneSpace(Space).EnableGPUSolver();
            }
        }
        internal int Iterations
        {
            get => GPU?.GetSolverIterations() ?? PhysicsServer.SpaceGetSolverIterations(Space);
            set { if (GPU is { } s) s.SetSolverIterations(value); else PhysicsServer.SpaceSetSolverIterations(Space, value); }
        }
        internal void Add(Vector2 position, Vector2 velocity = default, bool shape = true)
        {
            if (GPU is { } s)
            {
                var b = s.Add(new(Mode.RigidLinear, position, 0, velocity, 0, 1, CanSleep: false)); Handles.Add(b);
                if (shape) s.AddShape(b, _shape, friction: 0);
            }
            else
            {
                var b = PhysicsServer.BodyCreate(); Bodies.Add(b); PhysicsServer.BodySetMode(b, Mode.RigidLinear);
                PhysicsServer.BodySetTransform(b, new(0, position)); PhysicsServer.BodySetLinearVelocity(b, velocity);
                PhysicsServer.BodySetCanSleep(b, false); PhysicsServer.BodySetGravityScale(b, 0); PhysicsServer.BodySetMass(b, 1);
                PhysicsServer.BodySetLinearDampMode(b, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(b, 0);
                if (shape) PhysicsServer.BodyAddShape(b, _shape.GetRID()); PhysicsServer.BodySetFriction(b, 0);
                PhysicsServer.BodySetSpace(b, Space);
            }
        }
        internal void Pin(int a, int b)
        {
            var middle = new Vector2((a + b) * 5, 0);
            if (GPU is { } s) s.AddJoint(new(PhysicsServer.JointType.Pin, Handles[a], Handles[b],
                new(0, middle - new Vector2(a * 10, 0)), new(0, middle - new Vector2(b * 10, 0)))
            { MaxBias = 0 });
            else { var j = PhysicsServer.JointCreate(); Joints.Add(j); PhysicsServer.JointMakePin(j, middle, Bodies[a], Bodies[b]); PhysicsServer.JointSetMaxBias(j, 0); }
        }
        internal void Step(float dt = 1f / 60)
        { if (GPU is { } s) s.Simulate(dt, Vector2.Zero); else PhysicsServer.SpaceStep(Space, dt); }
        internal void Read(Span<Vector2> velocity)
        {
            if (GPU is { } s)
            {
                Span<Store.Snapshot> snapshots = stackalloc Store.Snapshot[velocity.Length]; s.Read(CollectionsMarshal.AsSpan(Handles), snapshots);
                for (var i = 0; i < snapshots.Length; i++) velocity[i] = new(snapshots[i].Velocity.X, snapshots[i].Velocity.Y);
            }
            else for (var i = 0; i < Bodies.Count; i++) velocity[i] = PhysicsServer.BodyGetLinearVelocity(Bodies[i]);
        }
        public void Dispose()
        {
            GPU?.Dispose(); foreach (var j in Joints) PhysicsServer.FreeRID(j); foreach (var b in Bodies) PhysicsServer.FreeRID(b);
            if (Space.IsValid()) PhysicsServer.FreeRID(Space); _shape.Dispose();
        }
    }
    private static void VerifyDefaults(bool resident)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DSolverIterations); Check(old == 16, "Project default is sixteen.");
        try
        {
            ProjectSettings.Set(ProjectSettings.Physics2DSolverIterations, 3); using var r = new Rig(resident); Check(r.Iterations == 3, "World captures the project value.");
            ProjectSettings.Set(ProjectSettings.Physics2DSolverIterations, 7); Check(r.Iterations == 3, "An existing world retains its count.");
            foreach (var value in new[] { 0, -1 })
            {
                Reject<ArgumentOutOfRangeException>(() => r.Iterations = value);
                Reject<ArgumentException>(() => ProjectSettings.Set(ProjectSettings.Physics2DSolverIterations, value));
            }
            Check(r.Iterations == 3, "Invalid edits are atomic."); r.Iterations = int.MaxValue; Check(r.Iterations == int.MaxValue, "The authored count is not silently clamped."); r.Iterations = 3;
            Task.Run(() => Reject<InvalidOperationException>(() => r.Iterations = 4)).GetAwaiter().GetResult();
            r.Add(Vector2.Zero, shape: false); Sleep(true); r.Iterations = 3; Check(Sleeping(), "Equal world counts preserve sleep on both backends.");
            Reject<ArgumentOutOfRangeException>(() => r.Iterations = 0); Check(Sleeping(), "Invalid world counts do not wake bodies.");
            r.Iterations = 4; Check(!Sleeping(), "Changed world counts wake dynamics on both backends.");
            r.Iterations = 5; Sleep(true); r.Step(); Check(Sleeping(), "Explicit sleep after iteration edits wins on both backends.");
            void Sleep(bool value) { if (r.GPU is { } gpu) gpu.SetSleeping(r.Handles[0], value); else PhysicsServer.BodySetSleeping(r.Bodies[0], value); }
            bool Sleeping()
            {
                if (r.GPU is not { } gpu) return PhysicsServer.BodyGetSleeping(r.Bodies[0]);
                Span<Store.Snapshot> state = stackalloc Store.Snapshot[1]; gpu.Read(CollectionsMarshal.AsSpan(r.Handles), state); return state[0].Sleeping;
            }
            if (!resident)
            {
                PhysicsServer.SpaceSetActive(r.Space, false); r.Iterations = 4; r.Step(); Check(r.Iterations == 4, "Inactive worlds remain configurable.");
                Reject<ArgumentException>(() => PhysicsServer.SpaceGetSolverIterations(default));
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DSolverIterations, old); }
    }
    private static void VerifyConvergence(bool resident, bool stages)
    {
        Span<Store.Snapshot> timeState = stackalloc Store.Snapshot[1];
        foreach (var iterations in new[] { 1, 16, 128, int.MaxValue })
        {
            using var r = new Rig(resident, stages); r.Iterations = iterations; r.Add(Vector2.Zero, new(40, 0), shape: false); r.Step(.02f);
            Vector2 position;
            if (r.GPU is { } gpu) { gpu.Read(CollectionsMarshal.AsSpan(r.Handles), timeState); position = timeState[0].Position; }
            else position = PhysicsServer.BodyGetTransform(r.Bodies[0]).Origin;
            Check(position.DistanceTo(new(.8f, 0)) < .00003f, "Sweeps do not multiply the 0.02 s integration interval.");
        }
        foreach (var joints in new[] { false, true })
        {
            var low = Error(1); var high = Error(128);
            Console.WriteLine($"Iteration convergence {(resident ? "resident" : stages ? "stages" : "CPU")}, {(joints ? "pins" : "contacts")}: rms(1)={low:F6}, rms(128)={high:F6} u/s.");
            Check(high < low * .4f && high < .5f, "Additional sweeps converge the eight-body common velocity, preserving time and momentum.");
            float Error(int iterations)
            {
                using var r = new Rig(resident, stages); r.Iterations = iterations;
                for (var i = 0; i < 8; i++) r.Add(new(i * 10, 0), i == 0 ? new(60, 0) : default, !joints);
                if (joints) for (var i = 1; i < 8; i++) r.Pin(i - 1, i);
                r.Step(); Span<Vector2> v = stackalloc Vector2[8]; r.Read(v);
                var sum = Vector2.Zero; var error = 0f; foreach (var item in v) { sum += item; error += (item - new Vector2(7.5f, 0)).LengthSquared(); }
                Check(sum.DistanceTo(new(60, 0)) < .03f, "Internal contact/joint impulses conserve the initial 60 kg*u/s momentum.");
                return MathF.Sqrt(error / 8);
            }
        }
    }
    private static void VerifyScene(bool stages)
    {
        using var root = new Node(); var body = new RigidBody { GravityScale = 0 }; root.AddChild(body);
        using var tree = new SceneTree(root); var world = body.GetWorld()!.Space;
        if (stages) body.Space!.EnableGPUSolver();
        PhysicsServer.SpaceSetSolverIterations(world, 4); body.Sleeping = true;
        PhysicsServer.SpaceSetSolverIterations(world, 4); Check(body.Sleeping, "Equal counts preserve explicit sleep.");
        PhysicsServer.SpaceSetSolverIterations(world, 8); Check(!body.Sleeping, "Changed counts wake the scene body.");
        body.Sleeping = true; PhysicsServer.SpaceSetSolverIterations(world, 16); body.Sleeping = true;
        tree.PhysicsFrame(.01); Check(body.Sleeping, "An explicit sleep after a settings edit wins.");
        var calls = 0; body.NotifyLocalTransformChanges = true;
        body.LocalTransformChanged += _ => { Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetSolverIterations(world, 2)); calls++; };
        body.Sleeping = false; body.LinearVelocity = new(1, 0); tree.PhysicsFrame(.01); Check(calls > 0, "Solver guard was exercised."); Check(PhysicsServer.SpaceGetSolverIterations(world) == 16, "Solver-phase edits reject before publication.");
    }
    private static void Measure(bool resident, bool stages)
    {
        using var r = new Rig(resident, stages);
        for (var i = 0; i < 256; i++) r.Add(new(i % 8 * 9.8f, i / 8 * 30), new(4, 0));
        foreach (var sweeps in new[] { 1, 16, 32 })
        {
            r.Iterations = sweeps; for (var i = 0; i < 128; i++) r.Step();
            var samples = new long[128]; var upload = r.GPU?.UploadBytes ?? 0; var read = r.GPU?.ReadbackBytes ?? 0; var uniform = r.GPU?.UniformBytes ?? 0; var wait = r.GPU?.WaitMS ?? 0;
            var before = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < samples.Length; i++) { var t = Stopwatch.GetTimestamp(); r.Step(); samples[i] = Stopwatch.GetTimestamp() - t; }
            var bytes = GC.GetTotalAllocatedBytes(true) - before; Array.Sort(samples); var ms = 1000d / Stopwatch.Frequency;
            Console.WriteLine($"Solver iterations {(resident ? "resident GPU" : stages ? "stage GPU" : "CPU")}: 256 active bodies, {sweeps} sweeps, 4 substeps, 128 warmup/128 samples, p50/p95/p99={samples[64] * ms:F4}/{samples[121] * ms:F4}/{samples[126] * ms:F4} ms; {bytes} all-thread managed B.");
            if (r.GPU is { } gpu) Console.WriteLine($"Iteration traffic: {(gpu.UploadBytes - upload) / 128} upload, {(gpu.ReadbackBytes - read) / 128} readback, {(gpu.UniformBytes - uniform) / 128} uniform B/tick; wait={(gpu.WaitMS - wait) / 128:F4} ms; {gpu.Driver}/{gpu.DeviceName}.");
            Check(bytes == 0, "Warmed changing iteration budgets retain zero managed allocation.");
        }
    }
    private static void VerifyLongParallelSchedule()
    {
        using var r = new Rig(false); for (var i = 0; i < 256; i++) r.Add(new(i % 2 * 9.8f, i / 2 * 30));
        r.Iterations = 1; for (var i = 0; i < 64; i++) r.Step();
        r.Iterations = 8193; var start = Stopwatch.GetTimestamp(); var before = GC.GetTotalAllocatedBytes(true); r.Step(.001f);
        var bytes = GC.GetTotalAllocatedBytes(true) - before;
        var native = Box2D.NET.B2Worlds.b2GetWorldFromId(PhysicsServer.Service.GetSceneSpace(r.Space).WorldID);
        var largestColor = 0; foreach (var color in native.constraintGraph.colors.AsSpan()) largestColor = Math.Max(largestColor, color.contactSims.count);
        Check(largestColor > 4 * Box2D.NET.B2Cores.B2_SIMD_WIDTH, "The graph contains multiple parallel work blocks.");
        Check(native.workerCount > 1 && bytes == 0, $"Crossing 65535 graph phases uses the parallel scheduler without growing stage storage: workers={native.workerCount}, bytes={bytes}.");
        Console.WriteLine($"Parallel 8193-sweep boundary: {Stopwatch.GetElapsedTime(start).TotalMilliseconds:F3} ms, {bytes} managed B.");
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
