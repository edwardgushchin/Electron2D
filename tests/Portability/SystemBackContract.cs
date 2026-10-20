using Electron2D;

internal static class SystemBackContract
{
    internal static async Task RunAsync(string mode, Action<string> report, Action? request = null,
        PhysicsServer.Backend backend = PhysicsServer.Backend.CPU, string renderer = "compatibility")
    {
        Check(mode is "default" or "cancel" or "blocked" or "explicit" or "failure", "Known native scenario.");
        var priorRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var priorFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback); var priorFPS = Engine.MaxFPS;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
            using var world = new World(backend); using var shape = new CircleShape { Radius = 1 };
            var window = new BackWindow(mode, world, shape, report, request, renderer); var exit = -1;
            try { exit = OperatingSystem.IsAndroid() ? await Engine.RunAsync(window) : Engine.Run(window); Check(mode != "failure", "Failure scenario must fault the host."); }
            catch (AggregateException error) when (mode == "failure")
            { Check(error.Flatten().InnerExceptions.Contains(window.ExpectedFailure), "Owner host preserves the original notification failure."); }
            Check(window.IsDisposed, "Native host retires its window after completion or failure.");
            var expectedRequests = mode is "cancel" or "blocked" ? 2 : 1;
            var expectedSignals = mode == "blocked" ? 1 : expectedRequests;
            Check(window.Requests == expectedRequests && window.Signals == expectedSignals, "Exact native callback counts without replay.");
            Check(exit == (mode == "explicit" ? 37 : mode == "failure" ? -1 : 0), "Actual host result preserves default/explicit quit or failure.");
            report($"RESULT {mode} PASS exit={exit} requests={window.Requests} signals={window.Signals}");
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, priorRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, priorFallback); Engine.MaxFPS = priorFPS; }
    }

    private sealed class BackWindow : Window
    {
        private readonly string _mode;
        private readonly World _world;
        private readonly Action<string> _report;
        private readonly Action? _request;
        private readonly string _renderer;
        private bool _firstPosted;
        private readonly List<int> _trace = new(16);
        private readonly Body _body;
        private int _owner;
        private int _frames;
        private long _ownerStart;
        private long _allStart;
        private float _positionStart;
        private bool _secondReady;
        internal readonly Exception ExpectedFailure = new InvalidOperationException("Expected native back notification failure");
        internal int Requests { get; private set; }
        internal int Signals { get; private set; }

        internal BackWindow(string mode, World world, Shape shape, Action<string> report, Action? request, string renderer)
        {
            _mode = mode; _world = world; _report = report; _request = request; _renderer = renderer; World = world; Size = new(128, 128); GUIEmbedSubwindows = true;
            Disposed += _ => { world.Dispose(); shape.Dispose(); };
            _body = new Body(_trace) { Name = "PhysicalReceiver", GravityScale = 0, CanSleep = false, CollisionLayer = 0, CollisionMask = 0, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0, LinearVelocity = new(60, 0) };
            _body.AddChild(new CollisionShape { Shape = shape }); AddChild(_body);
            var nested = new Window { Name = "Nested", Visible = false }; nested.AddChild(new BoundaryProbe()); AddChild(nested);
            GoBackRequested += () =>
            {
                Check(Environment.CurrentManagedThreadId == _owner && _trace.Count == 2 && _trace[0] == 1 && _trace[1] == 2, "Owner notifications precede the window signal and stop at nested windows.");
                _trace.Add(3); Signals++;
                if (_mode == "cancel" && Requests == 1) Tree!.QuitOnGoBack = true;
                if (_mode == "explicit") Tree!.Quit(37);
            };
        }

        protected override void OnReady()
        {
            base.OnReady();
            _owner = Environment.CurrentManagedThreadId;
            Check(_world.PhysicsBackend == _world.RequestedPhysicsBackend && _world.PhysicsFallbackReason is null, "Actual explicitly requested physical backend.");
            Check(RenderingServer.GetCurrentRenderingMethod() == _renderer, "Actual explicitly selected renderer without fallback.");
            Check(Tree!.QuitOnGoBack && Tree!.AutoAcceptQuit, "Independent default quit policies.");
            Tree!.AutoAcceptQuit = false;
            if (_mode == "blocked") SetBlockSignals(true);
            RenderingServer.SetDefaultClearColor(Colors.Blue);
            var before = GC.GetTotalAllocatedBytes(true); var control = new byte[4096]; GC.KeepAlive(control);
            Check(GC.GetTotalAllocatedBytes(true) > before, "Managed allocation counter observes a positive control.");
        }

        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != NotificationWmGoBackRequest) return;
            Check(Environment.CurrentManagedThreadId == _owner && _frames >= 256, "Real system request arrives on the running scene owner.");
            _trace.Clear(); _trace.Add(1); Requests++;
            if (_mode == "cancel" && Requests == 1) Tree!.QuitOnGoBack = false;
            if (_mode == "failure") throw ExpectedFailure;
        }

        protected override void OnPhysicsProcess(double delta)
        {
            base.OnPhysicsProcess(delta);
            _frames++;
            if (_frames == 128)
            {
                GC.Collect(2, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
                _positionStart = _body.Position.X;
                _ownerStart = GC.GetAllocatedBytesForCurrentThread(); _allStart = GC.GetTotalAllocatedBytes(true);
            }
            if (_frames != 256) return;
            var owner = GC.GetAllocatedBytesForCurrentThread() - _ownerStart; var all = GC.GetTotalAllocatedBytes(true) - _allStart;
            Check(owner == 0 && all == 0, $"128 complete warmed native physical frames allocate {owner}/{all} bytes.");
            Check(MathF.Abs(_body.Position.X - _positionStart - 128) < .02f, "Native owner loop advances genuine fixed-step physics.");
            using var image = GetTexture().GetImage()!;
            Check(image.GetPixel(image.Width / 2, image.Height / 2).B > .9f, "Actual native scene output is rendered.");
            _report($"READY {_mode} first; warm={owner}/{all}");
        }

        protected override void OnProcess(double delta)
        {
            base.OnProcess(delta);
            if (_frames >= 256 && !_firstPosted && _request is not null) { _firstPosted = true; _request(); }
            if (_frames < 256 || Requests != 1 || _secondReady || _mode is not ("cancel" or "blocked")) return;
            _secondReady = true;
            if (_mode == "blocked") { Check(Signals == 0 && _trace.Count == 2, "Blocked signal preserves notifications and does not quit."); SetBlockSignals(false); }
            _report($"READY {_mode} second"); _request?.Invoke();
        }
    }

    private sealed class Body(List<int> trace) : RigidBody
    {
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationWmGoBackRequest) trace.Add(2); }
    }

    private sealed class BoundaryProbe : Node
    {
        protected override void OnNotification(int what) { base.OnNotification(what); Check(what != NotificationWmGoBackRequest, "Containing window notification cannot cross a nested window."); }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
