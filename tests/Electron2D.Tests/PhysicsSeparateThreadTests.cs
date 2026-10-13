using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Worlds;

internal static class PhysicsSeparateThreadTests
{
    internal static void Run()
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        try
        {
            Check(ProjectSettings.HasSetting(ProjectSettings.Physics2DRunOnSeparateThread) && !ProjectSettings.Physics2DRunOnSeparateThread.DefaultValue,
                "The separate numerical thread has an executable false-default project key.");
            if (OperatingSystem.IsBrowser())
            {
                ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, true);
                try { using var rejected = new World(PhysicsServer.Backend.CPU); throw new Exception("Unsupported world threading was accepted."); }
                catch (PlatformNotSupportedException) { }
                try { PhysicsServer.SpaceCreate(PhysicsServer.Backend.CPU); throw new Exception("Unsupported space threading was accepted."); }
                catch (PlatformNotSupportedException) { }
                return;
            }
            VerifyCaptureAndPublication();
            VerifyFeatureCapture();
            VerifyFailedInterval(separate: false); VerifyFailedInterval(separate: true);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, true);
            PhysicsCCDTests.Run(); PhysicsJointPolicyTests.Run(); WorldBoundaryTests.Run();
            PhysicsBodyTests.Run(); RigidBodyForceTests.Run(); RigidBodyContactTests.Run(); ShapePairEventTests.Run();
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Console.WriteLine("Separate CPU numerical thread passed: captured selection, genuine solver lane, owner callbacks, guards, retirement and 0/0 warmed full frames.");
    }

    private static void VerifyCaptureAndPublication()
    {
        var owner = Environment.CurrentManagedThreadId;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, true);
        using var world = new World(PhysicsServer.Backend.CPU);
        using var alias = (World)world.Duplicate();
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, false);
        using var circle = new CircleShape { Radius = 1 };
        using var root = new SubViewport { World = alias };
        var body = new ProbeBody(owner)
        {
            CanSleep = false,
            GravityScale = 0,
            LinearVelocity = new(30, 0),
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace
        };
        body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
        using var tree = new SceneTree(root);
        var space = PhysicsServer.Service.GetSceneSpace(world.Space);
        var native = b2GetWorldFromId(space.WorldID);
        Thread? numericalThread = null;
        var publicQueriesRejected = 0;
        native.finalizeBodyStates = _ =>
        {
            numericalThread = Thread.CurrentThread;
            Check(Environment.CurrentManagedThreadId != owner, "The whole numerical interval executes off-owner.");
            try { PhysicsServer.SpaceGetDirectState(world.Space); throw new Exception("A solver-lane public query was accepted."); }
            catch (InvalidOperationException) { publicQueriesRejected++; }
        };
        tree.PhysicsFrame(1d / 60);
        Check(body.Callbacks == 1 && publicQueriesRejected > 0 && Math.Abs(body.Position.X - .5) < .001,
            "The owner joins actual CPU motion before the game callback.");
        Check(alias.Space == world.Space && PhysicsServer.SpaceGetTick(world.Space) == 1,
            "Aliases retain the pre-start captured numerical thread and one world tick.");
        native.finalizeBodyStates = null;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread(); var allBytes = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() == ownerBytes && GC.GetTotalAllocatedBytes(true) == allBytes,
            "Warmed threaded complete frames allocate zero owner/all-thread managed bytes.");
        Check(body.Callbacks == 257 && PhysicsServer.SpaceGetTick(world.Space) == 257,
            "Each joined threaded interval publishes exactly one game callback and tick.");
        tree.Dispose(); root.Dispose(); alias.Dispose(); world.Dispose();
        Check(numericalThread is not null && !numericalThread.IsAlive, "Final world release joins and retires its retained numerical thread.");

        var serial = PhysicsServer.SpaceCreate(PhysicsServer.Backend.CPU);
        var serialSpace = PhysicsServer.Service.GetSceneSpace(serial);
        PhysicsServer.SpaceSetActive(serial, true);
        var serialThread = 0;
        b2GetWorldFromId(serialSpace.WorldID).finalizeBodyStates = _ => serialThread = Environment.CurrentManagedThreadId;
        var moving = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetMode(moving, PhysicsServer.BodyMode.Rigid);
            PhysicsServer.BodyAddShape(moving, circle.GetRID()); PhysicsServer.BodySetSpace(moving, serial);
            PhysicsServer.SpaceStep(serial, 1d / 60);
            Check(serialThread == owner, "Later false-policy worlds retain owner-thread solving.");
        }
        finally { PhysicsServer.FreeRID(moving); PhysicsServer.FreeRID(serial); }
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

    private static void VerifyFailedInterval(bool separate)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        var space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.CPU);
        var body = PhysicsServer.BodyCreate(); using var shape = new CircleShape { Radius = 1 };
        Thread? worker = null; var attempts = 0;
        try
        {
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
            PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetSpace(body, space);
            PhysicsServer.SpaceSetActive(space, true);
            var data = PhysicsServer.Service.GetSceneSpace(space);
            b2GetWorldFromId(data.WorldID).finalizeBodyStates = _ =>
            { worker = Thread.CurrentThread; attempts++; throw new ArithmeticException("numerical failure probe"); };
            try { PhysicsServer.SpaceStep(space, 1d / 60); throw new Exception("Numerical failure was ignored."); }
            catch (AggregateException error) when (error.InnerExceptions.Any(value => value is ArithmeticException)) { }
            Check(attempts == 1 && data.HasBackendFailure && data.Tick == 0,
                "A started worker interval fails once without publishing a completed tick or replaying.");
            try { PhysicsServer.SpaceStep(space, 1d / 60); throw new Exception("Failed world stepped again."); }
            catch (InvalidOperationException) { }
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
        Check(worker is not null && (separate ? !worker.IsAlive : worker.ManagedThreadId == Environment.CurrentManagedThreadId) && attempts == 1,
            "Failed world teardown preserves its numerical lane and has no second attempt.");
    }

    private static void VerifyFeatureCapture()
    {
        const string feature = "physics-thread-tests";
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, false);
        ProjectSettings.SetFeatureOverride(ProjectSettings.Physics2DRunOnSeparateThread, feature, true);
        ProjectSettings.AddCustomFeature(feature);
        var space = default(RID); var body = default(RID); Thread? worker = null;
        using var shape = new CircleShape { Radius = 1 };
        try
        {
            space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.CPU);
            ProjectSettings.RemoveCustomFeature(feature);
            ProjectSettings.ClearFeatureOverride(ProjectSettings.Physics2DRunOnSeparateThread, feature);
            body = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
            PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetSpace(body, space);
            b2GetWorldFromId(PhysicsServer.Service.GetSceneSpace(space).WorldID).finalizeBodyStates = _ => worker = Thread.CurrentThread;
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(worker is not null && worker.ManagedThreadId != Environment.CurrentManagedThreadId,
                "A raw space captures the active feature value independently of later feature removal.");
        }
        finally
        {
            ProjectSettings.RemoveCustomFeature(feature);
            ProjectSettings.ClearFeatureOverride(ProjectSettings.Physics2DRunOnSeparateThread, feature);
            if (body.IsValid()) PhysicsServer.FreeRID(body);
            if (space.IsValid()) PhysicsServer.FreeRID(space);
        }
        Check(worker is not null && !worker.IsAlive, "Feature-captured raw space retires its worker.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
