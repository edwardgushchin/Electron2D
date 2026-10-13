using Electron2D;

internal static class PhysicsClockTests
{
    internal static void Run()
    {
        var frequency = ProjectSettings.Get(ProjectSettings.PhysicsTicksPerSecond);
        var maximum = ProjectSettings.Get(ProjectSettings.MaxPhysicsStepsPerFrame);
        var jitter = ProjectSettings.Get(ProjectSettings.PhysicsJitterFix);
        var scale = Engine.TimeScale;
        try
        {
            VerifyWorldClock();
            VerifyJitterBoundary();
        }
        finally
        {
            if (Engine.MainLoop is not null) Engine.Stop();
            ProjectSettings.Set(ProjectSettings.PhysicsTicksPerSecond, frequency);
            ProjectSettings.Set(ProjectSettings.MaxPhysicsStepsPerFrame, maximum);
            ProjectSettings.Set(ProjectSettings.PhysicsJitterFix, jitter);
            Engine.TimeScale = scale;
            ProjectSettings.FlushChanges();
        }
        Console.WriteLine("Public physics clock passed: actual CPU ticks/motion, frame order, scaling, catch-up, feature capture and 0/0 warmed managed bytes.");
    }

    private static void VerifyWorldClock()
    {
        Engine.PhysicsTicksPerSecond = 100;
        Engine.MaxPhysicsStepsPerFrame = 3;
        Engine.PhysicsJitterFix = 0;
        Engine.TimeScale = .5;
        using var world = new World(PhysicsServer.Backend.CPU);
        using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 1 };
        var body = new ClockBody
        {
            PhysicsProcessEnabled = true,
            GravityScale = 0,
            CanSleep = false,
            LinearVelocity = new(100, 0),
            LinearDamp = 0,
            AngularDamp = 0,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace
        };
        body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        using var tree = new SceneTree(root);
        var starts = 0;
        tree.PhysicsFrameStarted += current =>
        {
            Check(ReferenceEquals(current, tree) && Engine.IsInPhysicsFrame, "Frame event has its tree and fixed lane.");
            if (starts == 0) Check(Task.Run(static () => Engine.IsInPhysicsFrame).GetAwaiter().GetResult(),
                "The fixed phase is a process-wide snapshot, including an off-owner observer.");
            Check(++starts == body.FixedCalls + 1, "Frame event precedes every node fixed callback.");
            Check(PhysicsServer.SpaceGetTick(world.Space) == (ulong)body.Integrations, "Frame event precedes the numerical interval.");
        };
        Engine.Start(tree);
        var engineFrames = Engine.PhysicsFrames;
        Engine.AdvanceFrame(.005);
        Check(starts == 0 && PhysicsServer.SpaceGetTick(world.Space) == 0 && body.Position == Vector2.Zero,
            "An incomplete interval publishes no physical tick or motion.");
        Near(Engine.PhysicsInterpolationFraction, .5, 1e-10, "The half interval remains available for presentation.");
        Engine.AdvanceFrame(.015);
        Check(starts == 2 && body.FixedCalls == 2 && body.Integrations == 2 && Engine.PhysicsFrames == engineFrames + 2,
            "Unscaled cadence creates two fixed callbacks and physical intervals.");
        Near(body.LastDelta, .005, 1e-10, "Time scale changes the fixed delta.");
        Near(body.Position.X, 1, .001, "Two half-speed ticks integrate 100 units/s over .01 simulation seconds.");

        var tick = PhysicsServer.SpaceGetTick(world.Space); var position = body.Position;
        Engine.TimeScale = 0;
        Engine.AdvanceFrame(.02);
        Check(starts == 4 && body.LastDelta == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick && body.Position == position,
            "Zero scale preserves callback cadence while positive-duration physics and motion stay frozen.");
        Engine.TimeScale = 1;
        Engine.AdvanceFrame(.005);
        Engine.PhysicsTicksPerSecond = 200;
        Engine.AdvanceFrame(.0025);
        Check(starts == 4 && PhysicsServer.SpaceGetTick(world.Space) == tick, "A frequency change discards the old fractional interval.");
        Near(Engine.PhysicsInterpolationFraction, .5, 1e-10, "The new frequency has its own half interval.");
        Engine.AdvanceFrame(.0025);
        Check(starts == 5 && PhysicsServer.SpaceGetTick(world.Space) == ++tick, "The next new-frequency half completes one tick.");
        Near(body.Position.X, position.X + .5, .001, "New-frequency motion uses the new fixed duration.");

        var before = body.Position.X;
        Engine.AdvanceFrame(1000.0025);
        Check(starts == 8 && PhysicsServer.SpaceGetTick(world.Space) == tick + 3, "A long stall runs exactly the configured catch-up cap.");
        Near(body.Position.X, before + 1.5, .002, "Discarded catch-up time does not create hidden physical travel.");
        Near(Engine.PhysicsInterpolationFraction, .5, 1e-7, "Catch-up retains the fractional remainder.");

        const string feature = "physics-clock-tests";
        ProjectSettings.SetFeatureOverride(ProjectSettings.PhysicsTicksPerSecond, feature, 400);
        ProjectSettings.SetFeatureOverride(ProjectSettings.MaxPhysicsStepsPerFrame, feature, 2);
        ProjectSettings.SetFeatureOverride(ProjectSettings.PhysicsJitterFix, feature, 0d);
        ProjectSettings.AddCustomFeature(feature);
        try
        {
            Check(Engine.PhysicsTicksPerSecond == 400 && Engine.MaxPhysicsStepsPerFrame == 2 && Engine.PhysicsJitterFix == 0,
                "All three clock keys resolve active feature values.");
            tick = PhysicsServer.SpaceGetTick(world.Space); before = body.Position.X;
            Engine.AdvanceFrame(.1);
            Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 2, "Feature frequency and cap affect actual world ticks.");
            Near(body.Position.X, before + .5, .002, "Feature frequency affects actual physical travel.");
        }
        finally
        {
            ProjectSettings.RemoveCustomFeature(feature);
            ProjectSettings.ClearFeatureOverride(ProjectSettings.PhysicsTicksPerSecond, feature);
            ProjectSettings.ClearFeatureOverride(ProjectSettings.MaxPhysicsStepsPerFrame, feature);
            ProjectSettings.ClearFeatureOverride(ProjectSettings.PhysicsJitterFix, feature);
        }

        Engine.TimeScale = 2;
        tree.PhysicsFrameStarted += ChangeClockOnce;
        void ChangeClockOnce(SceneTree _)
        {
            tree.PhysicsFrameStarted -= ChangeClockOnce;
            Engine.PhysicsTicksPerSecond = 400; Engine.MaxPhysicsStepsPerFrame = 1;
            Engine.PhysicsJitterFix = .5; Engine.TimeScale = .25;
        }
        tick = PhysicsServer.SpaceGetTick(world.Space); before = body.Position.X;
        Engine.AdvanceFrame(.01);
        Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 2 && body.LastDelta == .01,
            "All clock edits during the first fixed callback wait for the next host frame.");
        Near(body.Position.X, before + 2, .002, "All catch-up ticks retain the captured time scale and frequency.");
        tick = PhysicsServer.SpaceGetTick(world.Space); before = body.Position.X;
        Engine.AdvanceFrame(.1);
        Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1, "The next frame applies the edited catch-up limit.");
        Near(body.LastDelta, .000625, 1e-10, "The next frame applies the edited frequency and scale.");
        Near(body.Position.X, before + .0625, .002, "The edited clock reaches actual physics motion.");
        Engine.PhysicsTicksPerSecond = 200; Engine.MaxPhysicsStepsPerFrame = 3;
        Engine.PhysicsJitterFix = 0; Engine.TimeScale = 1;

        tree.PhysicsFrameStarted += FailOnce;
        void FailOnce(SceneTree _) { tree.PhysicsFrameStarted -= FailOnce; throw new ArithmeticException("clock probe"); }
        tick = PhysicsServer.SpaceGetTick(world.Space);
        try { Engine.AdvanceFrame(.005); throw new Exception("Frame-start failure was ignored."); }
        catch (AggregateException error) when (error.InnerExceptions.Any(value => value is ArithmeticException)) { }
        Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1 && !Engine.IsInPhysicsFrame,
            "A failed frame event does not replay or skip the numerical tick and the fixed flag clears.");

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
        GC.WaitForPendingFinalizers();
        for (var i = 0; i < 128; i++) Engine.AdvanceFrame(.005);
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Engine.AdvanceFrame(.005);
        Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all,
            "Warmed active Engine/SceneTree/CPU physics frames allocate 0/0 managed bytes.");
        Check(world.PhysicsBackend == PhysicsServer.Backend.CPU && tree.PhysicsFrameCount == (ulong)starts,
            "The checked world remains CPU and each fixed callback has one tree frame event.");
        Engine.Stop();
    }

    private static void VerifyJitterBoundary()
    {
        Engine.PhysicsTicksPerSecond = 100; Engine.PhysicsJitterFix = .5; Engine.TimeScale = 1;
        using var root = new Node(); using var tree = new SceneTree(root);
        Engine.Start(tree); Engine.AdvanceFrame(.005);
        Check(tree.PhysicsFrameCount == 1 && Engine.PhysicsInterpolationFraction == 0,
            "Configured jitter tolerance advances a fixed callback at the half-step boundary.");
        Engine.Stop();
    }

    private sealed class ClockBody : RigidBody
    {
        internal int FixedCalls, Integrations;
        internal double LastDelta;
        protected override void OnPhysicsProcess(double delta) { FixedCalls++; LastDelta = delta; }
        protected override void IntegrateForces(PhysicsDirectBodyState state)
        {
            Check(Engine.IsInPhysicsFrame && Integrations < FixedCalls, "Body integration follows its node fixed callback on the fixed lane.");
            Integrations++;
        }
    }

    private static void Near(double actual, double expected, double tolerance, string message) => Check(Math.Abs(actual - expected) <= tolerance, message);
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
