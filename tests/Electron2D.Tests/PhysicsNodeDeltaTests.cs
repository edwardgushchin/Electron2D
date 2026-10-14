using Electron2D;

internal static class PhysicsNodeDeltaTests
{
    private sealed class Driver(CharacterBody body) : Node
    {
        internal int Calls;
        protected override void OnPhysicsProcess(double delta) { Calls++; body.MoveAndSlide(); }
        protected override void OnProcess(double delta) { Calls++; body.MoveAndSlide(); }
    }
    internal static void Run(PhysicsServer.Backend backend, bool threaded)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { CharacterMotion(); NodeObservations(); ScaledTime(); NodeInternalProcessingTests.Run(); SceneHierarchyTests.Run(); CharacterBodyTests.Run(backend); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Console.WriteLine($"Node frame deltas: {backend}, threaded={threaded}, public character motion and lifecycle passed.");

        void CharacterMotion()
        {
            using var world = new World(backend); using var root = new SubViewport { World = world };
            using var shape = new CircleShape { Radius = 2 };
            var body = new CharacterBody { Name = "Actor", Velocity = new(100, 0) }; body.AddChild(new CollisionShape { Shape = shape });
            var driver = new Driver(body) { Name = "Driver", PhysicsProcessEnabled = true, ProcessEnabled = true };
            root.AddChild(driver); root.AddChild(body); using var tree = new SceneTree(root);
            Check(!body.PhysicsProcessEnabled && !body.ProcessEnabled, "Character stays outside its own callback lanes");
            body.MoveAndSlide(); Near(body.Position.X, 0, "No tree lane time means no guessed motion");
            tree.PhysicsFrame(0); Near(body.Position.X, 0, "First zero physical duration is preserved");
            tree.PhysicsFrame(.03); Near(body.Position.X, 3, "Controller uses current physical duration on its first frame");
            tree.PhysicsFrame(.07); Near(body.Position.X, 10, "Controller sees changed physical duration");
            tree.ProcessFrame(.02); Near(body.Position.X, 12, "Idle controller uses its own duration");
            tree.PhysicsFrame(0); Near(body.Position.X, 12, "Zero physical frame never borrows idle duration");
            tree.ProcessFrame(0); Near(body.Position.X, 12, "Zero idle frame never borrows physical duration");
            tree.PhysicsFrame(.01); tree.ProcessFrame(0); body.MoveAndSlide(); Near(body.Position.X, 13, "Outside idle lane has zero retained duration");
            Check(world.PhysicsBackend == backend, "Requested backend executes character motion");
            Warm(() => { body.Position = Vector2.Zero; tree.PhysicsFrame(.01); Near(body.Position.X, 1, "Warm controller motion"); tree.ProcessFrame(.02); Near(body.Position.X, 3, "Warm idle motion"); });
        }
        void ScaledTime()
        {
            using var root = new Node(); var idle = new Node { ProcessMode = ProcessMode.Disabled }; root.AddChild(idle);
            using var tree = new SceneTree(root); var scale = Engine.TimeScale; var rate = Engine.PhysicsTicksPerSecond; var jitter = Engine.PhysicsJitterFix;
            try
            {
                Engine.TimeScale = .5; Engine.PhysicsTicksPerSecond = 50; Engine.PhysicsJitterFix = 0; Engine.Start(tree); Engine.AdvanceFrame(.04);
                Check(Math.Abs(idle.PhysicsProcessDeltaTime - .01) < 1e-10 && Math.Abs(idle.ProcessDeltaTime - .02) < 1e-10, "Shared observations use scaled Engine lane time");
                Engine.TimeScale = 0; Engine.AdvanceFrame(.04);
                Check(idle.PhysicsProcessDeltaTime == 0 && idle.ProcessDeltaTime == 0, "Zero Engine time scale commits zero in both lanes");
            }
            finally { Engine.Stop(); Engine.TimeScale = scale; Engine.PhysicsTicksPerSecond = rate; Engine.PhysicsJitterFix = jitter; }
        }
        void NodeObservations()
        {
            using var root = new Node(); var idle = new Node { Name = "Idle", ProcessMode = ProcessMode.Disabled }; root.AddChild(idle);
            using var tree = new SceneTree(root); var startCount = 0;
            tree.PhysicsFrameStarted += _ => { Check(idle.PhysicsProcessDeltaTime == .04, "Frame start sees current delta before any node callback"); startCount++; };
            Check(idle.ProcessDeltaTime == 0 && idle.PhysicsProcessDeltaTime == 0, "Construction starts with zero tree time");
            tree.PhysicsFrame(.04); tree.ProcessFrame(.09);
            Check(idle.PhysicsProcessDeltaTime == .04 && idle.ProcessDeltaTime == .09 && startCount == 1, "Disabled nodes read separate tree lane times");
            root.RemoveChild(idle); Check(idle.ProcessDeltaTime == 0 && idle.PhysicsProcessDeltaTime == 0, "Detachment drops prior tree time");
            using var secondRoot = new Node(); using var secondTree = new SceneTree(secondRoot); secondTree.PhysicsFrame(.08); secondTree.ProcessFrame(.11); secondRoot.AddChild(idle);
            Check(idle.PhysicsProcessDeltaTime == .08 && idle.ProcessDeltaTime == .11, "Reattachment adopts the receiving tree's time");
            secondTree.Paused = true; secondTree.PhysicsFrame(.06); Check(idle.PhysicsProcessDeltaTime == .06, "Paused disabled nodes still observe attempted frame time");
            Action<SceneTree> failed = _ => throw new InvalidOperationException("expected frame-start failure"); secondTree.ProcessFrameStarted += failed;
            Reject<AggregateException>(() => secondTree.ProcessFrame(.13)); Check(idle.ProcessDeltaTime == .13, "Callback error does not roll committed frame time back"); secondTree.ProcessFrameStarted -= failed;
            Reject<ArgumentOutOfRangeException>(() => secondTree.ProcessFrame(double.NaN)); Check(idle.ProcessDeltaTime == .13, "Rejected frame preserves prior time");
            Reject<InvalidOperationException>(() => Task.Run(() => _ = idle.PhysicsProcessDeltaTime).GetAwaiter().GetResult());
            secondTree.ProcessFrame(0); secondTree.PhysicsFrame(0); Check(idle.ProcessDeltaTime == 0 && idle.PhysicsProcessDeltaTime == 0, "Zero attempts replace prior time");
            Warm(() => { secondTree.ProcessFrame(.01); secondTree.PhysicsFrame(.02); Check(idle.ProcessDeltaTime == .01 && idle.PhysicsProcessDeltaTime == .02, "Warm disabled observation"); });
        }
    }
    private static void Warm(Action frame)
    {
        for (var i = 0; i < 128; i++) frame(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); for (var i = 0; i < 128; i++) frame();
        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(own == 0 && all == 0, "Warm full frames and time queries allocate zero managed bytes"); Console.WriteLine($"Node delta full frames: 128 warm/128 measured, {own}/{all} B.");
    }
    private static void Near(float actual, float expected, string message) { if (MathF.Abs(actual - expected) > .002f) throw new InvalidOperationException($"{message}: {actual}, expected {expected}"); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
}
