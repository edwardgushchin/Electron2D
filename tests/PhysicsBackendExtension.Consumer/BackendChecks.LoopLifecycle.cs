using Electron2D;

public static partial class BackendChecks
{
    public static void RunLoopLifecycle(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previousThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var cadence = Engine.PhysicsTicksPerSecond; var cap = Engine.MaxPhysicsStepsPerFrame;
        var scale = Engine.TimeScale; var jitter = Engine.PhysicsJitterFix;
        var owner = Thread.CurrentThread;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("LoopLifecycle", () => new Solver
        {
            StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Loop physics uses the selected numerical lane")
        });
        try
        {
            Engine.PhysicsTicksPerSecond = 120; Engine.MaxPhysicsStepsPerFrame = 4;
            Engine.TimeScale = .5; Engine.PhysicsJitterFix = 0;
            Direct(); Hosted(); Failures();
            Console.WriteLine($"Loop lifecycle {backend?.ToString() ?? "registered"}, threaded={threaded}: direct/hosted callbacks, physical deltas, stop, failures and 128 measured moving frames at 0/0 B passed.");
        }
        finally
        {
            if (Engine.MainLoop is not null) Engine.Stop();
            Engine.PhysicsTicksPerSecond = cadence; Engine.MaxPhysicsStepsPerFrame = cap;
            Engine.TimeScale = scale; Engine.PhysicsJitterFix = jitter;
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previousThread);
        }

        void Direct()
        {
            using var loop = new PhysicalLoop(backend);
            Reject<InvalidOperationException>(() => loop.Process(0));
            Reject<InvalidOperationException>(() => loop.PhysicsProcess(0));
            loop.Initialize();
            Check(loop.Initializations == 1 && loop.Physics == 0 && loop.Idles == 0, "Initialization precedes all frames");
            Reject<InvalidOperationException>(loop.Initialize);
            var tick = PhysicsServer.SpaceGetTick(loop.World!.Space);
            Check(!loop.PhysicsProcess(.01) && !loop.Process(.02) && loop.LastPhysics == .01 && loop.LastIdle == .02 &&
                PhysicsServer.SpaceGetTick(loop.World.Space) == tick + 1, "Direct callback deltas are seconds without Engine rescaling");
            foreach (var delta in new[] { -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                Reject<ArgumentOutOfRangeException>(() => loop.Process(delta));
                Reject<ArgumentOutOfRangeException>(() => loop.PhysicsProcess(delta));
            }
            Check(loop.Physics == 1 && loop.Idles == 1, "Invalid deltas never reach a callback");
            Task.Run(() =>
            {
                Reject<InvalidOperationException>(() => loop.PhysicsProcess(.01));
                Reject<InvalidOperationException>(() => loop.Process(.01));
                Reject<InvalidOperationException>(loop.FinalizeLoop);
                Reject<InvalidOperationException>(loop.Dispose);
            }).GetAwaiter().GetResult();
            loop.StopPhysics = true; loop.StopIdle = true;
            Check(loop.PhysicsProcess(0) && loop.Process(0) && loop.Finalizations == 0, "Both stop requests return unchanged without self-finalization");
            loop.FinalizeLoop();
            Check(loop.Finalizations == 1 && loop.World.IsDisposed && loop.Body!.IsDisposed && !loop.IsDisposed,
                "Explicit finalization retires owned physical state before base disposal");
            Reject<InvalidOperationException>(loop.FinalizeLoop);
            Reject<InvalidOperationException>(() => loop.PhysicsProcess(0));
        }

        void Hosted()
        {
            using var loop = new PhysicalLoop(backend); Engine.Start(loop);
            Check(loop.Initializations == 1 && ReferenceEquals(Engine.MainLoop, loop), "Engine initializes and publishes the custom loop once");
            Check(!Engine.AdvanceFrame(1d / 240) && loop.Physics == 0 && loop.Idles == 1,
                "A half cadence interval has only the idle callback");
            Check(!Engine.AdvanceFrame(1d / 240) && loop.Physics == 1 && loop.LastPhysics == 1d / 240 && loop.Idles == 2 && loop.LastOrder == 12,
                "Scaled fixed work precedes idle work on the owner");
            Engine.TimeScale = 0;
            Engine.AdvanceFrame(1d / 120);
            Check(loop.LastPhysics == 0 && loop.LastIdle == 0, "A paused time scale reaches both callbacks as zero seconds");
            Engine.TimeScale = .5;
            var count = loop.Physics; Engine.AdvanceFrame(10);
            Check(loop.Physics - count == 4 && loop.LastIdle <= 4d / 240 + 1e-12,
                "Catch-up bounds both fixed calls and idle delta");
            for (var i = 0; i < 128; i++) Frame();
            Collect(); var before = loop.Body!.Position; var tick = PhysicsServer.SpaceGetTick(loop.World!.Space);
            var physical = loop.Physics; var idle = loop.Idles;
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && loop.Physics == physical + 128 && loop.Idles == idle + 128 &&
                PhysicsServer.SpaceGetTick(loop.World.Space) == tick + 128 && MathF.Abs(loop.Body.Position.X - before.X - 1.6f) < .003f,
                $"Hosted callbacks retain actual scaled motion and zero warm allocations: {own}/{all} B");
            loop.StopPhysics = true; count = loop.Physics; idle = loop.Idles;
            Check(Engine.AdvanceFrame(4d / 120) && loop.Physics == count + 1 && loop.Idles == idle + 1 && loop.Finalizations == 0,
                "A physics stop skips remaining catch-up but preserves the current idle callback");
            loop.StopPhysics = false; loop.StopIdle = true;
            Check(Engine.AdvanceFrame(0), "An idle-only stop reaches the host unchanged");
            Engine.Stop();
            Check(Engine.MainLoop is null && loop.Finalizations == 1 && loop.World.IsDisposed && !loop.IsDisposed,
                "Stop detaches and finalizes without taking loop disposal ownership");
            Reject<InvalidOperationException>(() => Engine.Start(loop));

            void Frame()
            {
                Check(!Engine.AdvanceFrame(1d / 120), "Ordinary physical frames do not request termination");
                Check(loop.LastPhysics == 1d / 240 && loop.LastIdle >= 0 && loop.LastOrder == 12,
                    "Prepared fixed/idle callbacks retain delta and order");
            }
        }

        void Failures()
        {
            var error = new InvalidOperationException("Intentional loop callback failure");
            using (var loop = new PhysicalLoop(backend) { InitializeError = error })
            {
                SameError(() => Engine.Start(loop), error);
                Check(Engine.MainLoop is null && loop.Initializations == 1 && loop.Finalizations == 0 && loop.World!.IsDisposed,
                    "Failed initialization rolls back physical acquisition and detaches without finalization");
                Reject<InvalidOperationException>(loop.Initialize);
            }
            using (var loop = new PhysicalLoop(backend))
            {
                Engine.Start(loop); loop.PhysicsError = error;
                var tick = PhysicsServer.SpaceGetTick(loop.World!.Space);
                SameError(() => Engine.AdvanceFrame(1d / 120), error);
                Check(PhysicsServer.SpaceGetTick(loop.World.Space) == tick && ReferenceEquals(Engine.MainLoop, loop) && !Engine.IsInPhysicsFrame,
                    "A failed callback does not simulate or detach and restores the Engine phase");
                loop.PhysicsError = null; Engine.AdvanceFrame(1d / 120);
                Check(PhysicsServer.SpaceGetTick(loop.World.Space) == tick + 1, "The next valid frame executes without replaying the failed callback");
                loop.IdleError = error; SameError(() => Engine.AdvanceFrame(0), error);
                loop.IdleError = null; Engine.AdvanceFrame(0);
                loop.CheckReentry = true; Engine.AdvanceFrame(1d / 120); loop.CheckReentry = false;
                loop.FinalizeError = error;
                SameError(Engine.Stop, error);
                Check(Engine.MainLoop is null && loop.Finalizations == 1 && loop.World.IsDisposed && loop.Body!.IsDisposed,
                    "Failed finalization still retires physical ownership and detaches terminal state");
                Reject<InvalidOperationException>(loop.FinalizeLoop);
            }
            var automatic = new PhysicalLoop(backend); automatic.Initialize(); automatic.Dispose(); automatic.Dispose();
            Check(automatic.Finalizations == 1 && automatic.World!.IsDisposed && automatic.IsDisposed,
                "Disposal automatically finalizes a running loop exactly once");
            using var unused = new PhysicalLoop(backend); unused.Dispose();
            Check(unused.Initializations == 0 && unused.Finalizations == 0 && unused.World is null,
                "An uninitialized loop owns no simulation and has no finalization callback");
        }
        static void SameError(Action action, Exception expected)
        {
            try { action(); } catch (Exception error) when (ReferenceEquals(error, expected)) { return; }
            throw new InvalidOperationException("The original callback exception must survive unchanged");
        }
    }

    private sealed class PhysicalLoop(PhysicsServer.Backend? backend) : MainLoop
    {
        private readonly Thread _owner = Thread.CurrentThread;
        private CircleShape? _shape;
        private SceneTree? _tree;
        private readonly PhysicsPointQueryParameters _query = new();
        private readonly PhysicsPointResult[] _hits = new PhysicsPointResult[1];
        internal World? World;
        internal RigidBody? Body;
        internal int Initializations, Finalizations, Physics, Idles, LastOrder;
        internal double LastPhysics, LastIdle;
        internal bool StopPhysics, StopIdle, CheckReentry;
        internal Exception? InitializeError, PhysicsError, IdleError, FinalizeError;
        protected override void OnInitialize()
        {
            Check(Thread.CurrentThread == _owner && Initializations++ == 0, "Initialization runs once on its owner");
            World = backend is { } selected ? new World(selected) : new World("LoopLifecycle");
            try
            {
                Check(World.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && World.PhysicsFallbackReason is null,
                    "A loop retains the explicitly selected physical owner");
                _shape = new CircleShape { Radius = 1 };
                var root = new SubViewport { World = World };
                Body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(3, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
                Body.AddChild(new CollisionShape { Shape = _shape }); root.AddChild(Body); _tree = new SceneTree(root);
                if (InitializeError is { } error) throw error;
            }
            catch { Release(); throw; }
        }
        protected override bool OnPhysicsProcess(double delta)
        {
            Check(Thread.CurrentThread == _owner && (Engine.MainLoop is null || Engine.IsInPhysicsFrame), "Fixed callback runs in its owner phase");
            if (PhysicsError is { } error) throw error;
            if (CheckReentry)
            {
                Reject<InvalidOperationException>(() => PhysicsProcess(0)); Reject<InvalidOperationException>(() => Process(0));
                Reject<InvalidOperationException>(FinalizeLoop); Reject<InvalidOperationException>(Dispose); Reject<InvalidOperationException>(Engine.Stop);
            }
            Physics++; LastPhysics = delta; LastOrder = 1;
            _tree!.PhysicsProcess(delta);
            _query.Position = Body!.Position;
            Check(World!.DirectSpaceState.IntersectPoint(_query, _hits) == 1 && _hits[0].ColliderRID == Body.GetRID(),
                "Fixed callbacks advance a real selected body and query its published pose");
            return StopPhysics;
        }
        protected override bool OnProcess(double delta)
        {
            Check(Thread.CurrentThread == _owner && !Engine.IsInPhysicsFrame, "Idle callback runs in its owner phase");
            if (IdleError is { } error) throw error;
            Idles++; LastIdle = delta; LastOrder = LastOrder * 10 + 2;
            _tree!.Process(delta);
            return StopIdle;
        }
        protected override void OnFinalize()
        {
            Check(Thread.CurrentThread == _owner && Finalizations++ == 0, "Finalization runs once on its owner");
            Release(); if (FinalizeError is { } error) throw error;
        }
        private void Release() { _tree?.Dispose(); World?.Dispose(); _shape?.Dispose(); _query.Dispose(); }
    }
}
