using Electron2D;

internal static class PermissionResultContract
{
    internal static async Task RunAsync(string mode, Action<string[]> request, Action<string> report)
    {
        Check(mode is "grant" or "deny" or "batch" or "throw", "Known permission scenario");
        var priorRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var priorFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var priorThreaded = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var priorFPS = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, true);
        Engine.MaxFPS = 120;
        try
        {
            using var world = new World(PhysicsServer.Backend.CPU);
            using var shape = new CircleShape { Radius = 1 };
            var window = new PermissionWindow(mode, world, shape, request, report);
            try { Check(await Engine.RunAsync(window) == 0 && mode != "throw", "Native host completes normally"); }
            catch (Exception error) when (mode == "throw")
            { Check(ReferenceEquals(error, window.ExpectedFailure), "The host preserves the exact original queued permission error"); }
            Check(window.IsDisposed && window.Results == (mode == "batch" ? 2 : 1), "Exact native result count and terminal host lifetime");
            report($"RESULT {mode} PASS results={window.Results} owner={window.OwnerThreadID} frames={window.PhysicsFrames}");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, priorRenderer);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, priorFallback);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, priorThreaded);
            Engine.MaxFPS = priorFPS;
        }
    }

    private sealed class PermissionWindow : Window
    {
        private const string Audio = "android.permission.RECORD_AUDIO", Camera = "android.permission.CAMERA";
        private readonly string _mode;
        private readonly World _world;
        private readonly Action<string[]> _request;
        private readonly Action<string> _report;
        private readonly RigidBody _body;
        private RID _identity;
        private bool _requested;
        private int _resultsFrame;
        private long _ownerStart, _allStart;
        private float _startPosition;
        internal readonly Exception ExpectedFailure = new ApplicationException("native-permission-handler");
        internal int Results, OwnerThreadID, PhysicsFrames;

        internal PermissionWindow(string mode, World world, Shape shape, Action<string[]> request, Action<string> report)
        {
            _mode = mode; _world = world; _request = request; _report = report;
            World = world; Size = new(128, 128); GUIEmbedSubwindows = true;
            Disposed += _ => { world.Dispose(); shape.Dispose(); };
            _body = new RigidBody { Name = "PermissionReceiver", GravityScale = 0, CanSleep = false, CollisionLayer = 0, CollisionMask = 0,
                LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0, LinearVelocity = new(60, 0) };
            _body.AddChild(new CollisionShape { Shape = shape }); AddChild(_body);
        }

        protected override void OnReady()
        {
            base.OnReady();
            OwnerThreadID = Environment.CurrentManagedThreadId;
            _identity = _body.GetRID();
            Check(_world.PhysicsBackend == PhysicsServer.Backend.CPU && _world.PhysicsFallbackReason is null, "Explicit independent CPU physics");
            Check(RenderingServer.GetCurrentRenderingMethod() == "compatibility", "Explicit native renderer");
            Engine.MainLoop!.OnRequestPermissionsResult += Result;
            RenderingServer.SetDefaultClearColor(Colors.Blue);
            var owner = GC.GetAllocatedBytesForCurrentThread();
            var all = GC.GetTotalAllocatedBytes(true);
            var control = new byte[4096];
            GC.KeepAlive(control);
            Check(GC.GetAllocatedBytesForCurrentThread() >= owner + 4096 && GC.GetTotalAllocatedBytes(true) >= all + 4096,
                "Both managed allocation counters observe a positive native-host control");
        }

        private void Result(MainLoop sender, string permission, bool granted)
        {
            Check(ReferenceEquals(sender, Tree) && Environment.CurrentManagedThreadId == OwnerThreadID && _requested, "Real result reaches the current scene owner");
            Check(Results < (_mode == "batch" ? 2 : 1) && permission == (Results == 0 ? Audio : Camera) &&
                granted == (_mode != "deny" && Results == 0), "Exact platform permission name, decision and batch order");
            _body.LinearVelocity = new(granted ? 120 : 60, 0);
            Check(_body.GetRID() == _identity, "Native permission handler can edit the same live physical receiver");
            Results++;
            if (_mode == "throw") throw ExpectedFailure;
            if (Results == (_mode == "batch" ? 2 : 1)) _resultsFrame = PhysicsFrames;
        }

        protected override void OnPhysicsProcess(double delta)
        {
            base.OnPhysicsProcess(delta);
            PhysicsFrames++;
            if (PhysicsFrames == 128)
            {
                GC.Collect(2, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
                _ownerStart = GC.GetAllocatedBytesForCurrentThread(); _allStart = GC.GetTotalAllocatedBytes(true); _startPosition = _body.Position.X;
            }
            if (PhysicsFrames == 256)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _ownerStart;
                var all = GC.GetTotalAllocatedBytes(true) - _allStart;
                Check(owner == 0 && all == 0 && MathF.Abs(_body.Position.X - _startPosition - 128) < .02f,
                    $"128 warmed full native frames preserve motion and 0/0 allocation: {owner}/{all}");
                using var image = GetTexture().GetImage()!;
                Check(image.GetPixel(image.Width / 2, image.Height / 2).B > .9f, "The actual native window renders");
                _report($"READY {_mode} warm={owner}/{all}");
            }
            if (_resultsFrame == 0 || PhysicsFrames < _resultsFrame + 8) return;
            var expected = _mode == "batch" || _mode == "deny" ? 60 : 120;
            var velocity = _body.LinearVelocity.X;
            Check(_body.GetRID() == _identity && MathF.Abs(velocity - expected) < .001f,
                $"The decision survives scene/solver float conversion: velocity={velocity:R}, expected={expected}, identity={_body.GetRID() == _identity}");
            Tree!.Quit();
        }

        protected override void OnProcess(double delta)
        {
            base.OnProcess(delta);
            if (PhysicsFrames < 256 || _requested) return;
            _requested = true;
            _request(_mode == "batch" ? [Audio, Camera] : [Audio]);
        }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
