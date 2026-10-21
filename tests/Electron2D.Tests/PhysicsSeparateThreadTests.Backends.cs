using System.Runtime.CompilerServices;
using Electron2D;

internal static partial class PhysicsSeparateThreadTests
{
    // Observe only scheduling/lifetime; numerical acceptance uses the public world and body contract.
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_solverThread")]
    private static extern ref PhysicsSolverThread? SolverThread(PhysicsSpace space);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_thread")]
    private static extern ref Thread WorkerThread(PhysicsSolverThread solver);

    internal static void RunGPU()
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        try
        {
            foreach (var threaded in new[] { false, true })
            {
                VerifyGPUCapture(threaded);
                VerifyGPURawFeature(threaded);
                VerifyGPUFailedInterval(threaded);
            }
            GPUPhysicsPipelineTests.Run();
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Console.WriteLine("GPU numerical thread contract: captured scene/raw/feature policy, aliases, inactive/zero intervals, owner callbacks, failure/no-replay, retirement and 0/0 warmed bytes passed.");
    }

    private static Thread? RequireLane(PhysicsSpace space, bool threaded)
    {
        var solver = SolverThread(space);
        Check((solver is not null) == threaded, "The captured policy selects the retained numerical lane without serial fallback.");
        if (solver is null) return null;
        var worker = WorkerThread(solver);
        Check(worker.IsAlive && worker.ManagedThreadId != Environment.CurrentManagedThreadId,
            "The numerical worker is live and distinct from its synchronous owner.");
        return worker;
    }

    private static bool NoCPUWorld(PhysicsSpace space)
    {
        try { _ = space.WorldID; return false; }
        catch (InvalidOperationException) { return true; }
    }

    private static void VerifyGPUCapture(bool threaded)
    {
        var owner = Environment.CurrentManagedThreadId;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        using var world = new World(PhysicsServer.Backend.GPU);
        using var alias = (World)world.Duplicate();
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, !threaded);
        using var shape = new CircleShape { Radius = 1 };
        using var root = new SubViewport { World = alias };
        var body = new ProbeBody(owner)
        {
            CanSleep = false,
            GravityScale = 0,
            LinearVelocity = new(30, 0),
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace
        };
        body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        using var tree = new SceneTree(root);
        var space = PhysicsServer.Service.GetSceneSpace(world.Space);
        Check(world.PhysicsBackend == PhysicsServer.Backend.GPU && alias.Space == world.Space && NoCPUWorld(space),
            "Aliases keep the independent GPU space without a CPU numerical world.");
        tree.PhysicsFrame(0);
        PhysicsServer.SpaceSetActive(world.Space, false); tree.PhysicsFrame(1d / 60);
        Check(SolverThread(space) is null && PhysicsServer.SpaceGetTick(world.Space) == 0 && body.Callbacks == 0,
            "Zero and inactive intervals do not start a worker, publish callbacks or advance physical time.");
        PhysicsServer.SpaceSetActive(world.Space, true); tree.PhysicsFrame(1d / 60);
        var worker = RequireLane(space, threaded);
        Check(body.Callbacks == 1 && Math.Abs(body.Position.X - .5) < .001 && PhysicsServer.SpaceGetTick(world.Space) == 1,
            "GPU solving completes before exactly one owner callback and public pose publication.");
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false); GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
        for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
        var current = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() == current && GC.GetTotalAllocatedBytes(true) == total &&
            body.Callbacks == 257 && PhysicsServer.SpaceGetTick(world.Space) == 257,
            "Complete active GPU frames retain one callback/tick and zero owner/all-thread managed bytes.");
        Check(ReferenceEquals(worker, RequireLane(space, threaded)), "The selected lane is reused across complete intervals.");
        tree.Dispose(); root.Dispose(); alias.Dispose(); world.Dispose();
        Check(worker is null || !worker.IsAlive, "Final alias/world retirement joins the GPU numerical worker.");
    }

    private static void VerifyGPURawFeature(bool threaded)
    {
        const string feature = "gpu-physics-thread-contract";
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, !threaded);
        ProjectSettings.SetFeatureOverride(ProjectSettings.Physics2DRunOnSeparateThread, feature, threaded);
        ProjectSettings.AddCustomFeature(feature);
        var space = default(RID); var body = default(RID); Thread? worker = null;
        using var shape = new CircleShape { Radius = 1 };
        try
        {
            space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU);
            ProjectSettings.RemoveCustomFeature(feature);
            ProjectSettings.ClearFeatureOverride(ProjectSettings.Physics2DRunOnSeparateThread, feature);
            body = PhysicsServer.BodyCreate(); PhysicsServer.BodyAddShape(body, shape.GetRID());
            PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetLinearVelocity(body, new(30, 0));
            PhysicsServer.BodySetSpace(body, space); PhysicsServer.SpaceSetActive(space, true);
            PhysicsServer.SpaceStep(space, 1d / 60); worker = RequireLane(PhysicsServer.Service.GetSceneSpace(space), threaded);
            Check(PhysicsServer.SpaceGetBackend(space) == PhysicsServer.Backend.GPU &&
                Math.Abs(PhysicsServer.BodyGetTransform(body).Origin.X - .5) < .002,
                "Raw GPU space captures the feature policy and executes physical motion after feature removal.");
        }
        finally
        {
            ProjectSettings.RemoveCustomFeature(feature);
            ProjectSettings.ClearFeatureOverride(ProjectSettings.Physics2DRunOnSeparateThread, feature);
            if (body.IsValid()) PhysicsServer.FreeRID(body);
            if (space.IsValid()) PhysicsServer.FreeRID(space);
        }
        Check(worker is null || !worker.IsAlive, "Raw GPU retirement joins its captured worker.");
    }

    private static void VerifyGPUFailedInterval(bool threaded)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        var space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU, allowCPUFallback: true);
        var body = PhysicsServer.BodyCreate(); Thread? worker = null;
        try
        {
            PhysicsServer.BodySetSpace(body, space); PhysicsServer.SpaceSetActive(space, true);
            var data = PhysicsServer.Service.GetSceneSpace(space);
            var rejected = false;
            try { PhysicsServer.SpaceStep(space, float.MaxValue); }
            catch (AggregateException) { rejected = true; }
            worker = RequireLane(data, threaded);
            Check(rejected && data.HasBackendFailure && data.Tick == 0 && data.GPUStore is { HasFailed: true } && NoCPUWorld(data),
                "An actual GPU numerical failure is terminal, publishes no tick and creates no CPU replay world.");
            try { PhysicsServer.SpaceStep(space, 1d / 60); throw new Exception("A failed GPU interval retried."); }
            catch (InvalidOperationException) { }
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
        Check(worker is null || !worker.IsAlive, "Failed GPU-world retirement joins its numerical worker.");
    }

    internal static void RunUnavailableGPU()
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        try
        {
            foreach (var threaded in new[] { false, true })
            {
                ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
                var rejected = false;
                try { var invalid = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU); PhysicsServer.FreeRID(invalid); }
                catch (InvalidOperationException) { rejected = true; }
                Check(rejected, "GPU startup is genuinely unavailable in the CPU fallback process.");
                var space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU, allowCPUFallback: true);
                var body = PhysicsServer.BodyCreate(); Thread? worker = null;
                using var shape = new CircleShape { Radius = 1 };
                try
                {
                    ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, !threaded);
                    PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetGravityScale(body, 0);
                    PhysicsServer.BodySetLinearVelocity(body, new(30, 0)); PhysicsServer.BodySetSpace(body, space);
                    PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, 1d / 60);
                    worker = RequireLane(PhysicsServer.Service.GetSceneSpace(space), threaded);
                    Check(PhysicsServer.SpaceGetRequestedBackend(space) == PhysicsServer.Backend.GPU &&
                        PhysicsServer.SpaceGetBackend(space) == PhysicsServer.Backend.CPU && PhysicsServer.SpaceGetBackendFallbackReason(space) is not null &&
                        PhysicsServer.BodyGetTransform(body).Origin.X > .4f && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable,
                        "Observable startup fallback retains the captured numerical policy and runs without graphics.");
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false); GC.WaitForPendingFinalizers();
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
                    for (var i = 0; i < 128; i++) PhysicsServer.SpaceStep(space, 1d / 60);
                    var current = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
                    for (var i = 0; i < 128; i++) PhysicsServer.SpaceStep(space, 1d / 60);
                    Check(GC.GetAllocatedBytesForCurrentThread() == current && GC.GetTotalAllocatedBytes(true) == total && PhysicsServer.SpaceGetTick(space) == 257,
                        "Fallback CPU intervals retain the captured policy and zero owner/all-thread managed bytes.");
                }
                finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
                Check(worker is null || !worker.IsAlive, "Fallback CPU retirement joins the selected numerical lane.");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Console.WriteLine("No-GPU numerical thread contract: strict denial, observable CPU fallback, captured owner/separate policy and joined retirement passed.");
    }

    private sealed class ProbeBody(int owner) : RigidBody
    {
        internal int Callbacks;
        protected override void IntegrateForces(PhysicsDirectBodyState state)
        {
            Check(Environment.CurrentManagedThreadId == owner && state.Transform.Origin == Position,
                "Game integration uses completed publication on its original owner.");
            Callbacks++;
        }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
