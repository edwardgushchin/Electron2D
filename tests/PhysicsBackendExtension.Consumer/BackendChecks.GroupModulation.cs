using Electron2D;

public static partial class BackendChecks
{
    private sealed class ModulationProbe : CanvasModulate
    {
        internal bool Veto;
        protected override void ValidateMutation() { base.ValidateMutation(); if (Veto) throw new InvalidOperationException("Expected tint veto"); }
    }
    public static void RunGroupModulation(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false, Node? extraProvider = null)
    {
        var oldPool = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("GroupModulation", () => new Solver());
        try
        {
            if (poolOwner) { var job = WorkerThreadPool.AddTask(Run); WorkerThreadPool.WaitForTaskCompletion(job); }
            else Run();
        }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldPool); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldThread); }
        void Run()
        {
            using var world = backend is { } selected ? new World(selected) : new World("GroupModulation");
            using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 2 };
            var a = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
            var b = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
            var first = new ModulationProbe(); var second = new CanvasModulate(); var layer = new CanvasLayer(); var lazy = new CanvasLayer(); var layerTint = new CanvasModulate();
            var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(6, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            body.AddChild(new CollisionShape { Shape = shape }); a.AddChild(body); a.AddChild(first); b.AddChild(second); layer.AddChild(layerTint); a.AddChild(layer);
            root.AddChild(a); root.AddChild(b); root.AddChild(lazy);
            Node[] providers = [new Node(), new Electron2D.Timer(), new Entity(), new Control(), new ShaderGlobalsOverride(), new RemoteTransform(), new PathFollow(), new AnimatedSprite(), new CollisionPolygon(), new CollisionShape(), new MultiMeshInstance(), new Bone(), new PhysicalBone { AutoConfigureJoint = false }, new PinJoint(), new MeshInstance(), new Parallax(), new TouchScreenButton { Action = "group-warning-missing" }, new CPUParticles { Emitting = false }, new GPUParticles { Emitting = false }, new HSlider { MinValue = -1, ExpEdit = true }, new TextureRect(), new SubViewportContainer(), new Button(), new ScrollContainer(), new VirtualJoystick(), new MenuButton(), new MultiplayerSynchronizer(), new MultiplayerSpawner()];
            if (extraProvider is not null) providers = [.. providers, extraProvider];
            foreach (var node in providers) a.AddChild(node);
            var viewportContainer = providers.OfType<SubViewportContainer>().Single();
            var scrollContainer = providers.OfType<ScrollContainer>().Single();
            var label = new Label { Name = "LandmarkLabel", Text = "Scrolling landmark" };
            b.Name = "OtherGroup"; b.AddChild(label);
            scrollContainer.AccessibilityLabeledByNodes = ["../../OtherGroup/LandmarkLabel"];
            const string landmarkWarning = "Provide an accessible name for this action or landmark region.";
            using var tree = new SceneTree(root); tree.EditedSceneRoot = root;
            using var together = new Barrier(2); var cold = true; var phase = 0; var notices = 0; var landmarkNotices = 0; var observing = false; RID left = default, right = default;
            var failure = new ApplicationException("Expected grouped warning failure"); var fail = false;
            tree.NodeConfigurationWarningChanged += (_, node) =>
            {
                if (!observing)
                {
                    if (ReferenceEquals(node, viewportContainer))
                    {
                        landmarkNotices++;
                        Check(viewportContainer.GetConfigurationWarnings().Contains(landmarkWarning) == (viewportContainer.AccessibilityRegion && viewportContainer.AccessibilityName.Length == 0),
                            "Owner warning observers see the committed landmark flag and name");
                    }
                    return;
                }
                Check(Environment.CurrentManagedThreadId == a.ThreadID || Environment.CurrentManagedThreadId == b.ThreadID, "Warning refresh is synchronous on the calling group");
                Interlocked.Increment(ref notices); if (fail && ReferenceEquals(node, first)) { fail = false; throw failure; }
            };
            Check(!viewportContainer.AccessibilityRegion && !viewportContainer.GetConfigurationWarnings().Contains(landmarkWarning), "Default container has no landmark warning");
            viewportContainer.AccessibilityRegion = true;
            scrollContainer.AccessibilityRegion = true;
            Check(viewportContainer.AccessibilityRegion && viewportContainer.GetConfigurationWarnings().Contains(landmarkWarning), "Owner sees the unnamed landmark warning");
            a.Write = () => Write(a, first, second, true); b.Write = () => Write(b, second, first, false);
            Step(); cold = false;
            Check(a.ThreadID != b.ThreadID && left == right && left.IsValid() && left != first.GetCanvas() && first.GetCanvas() == world.Canvas, "Concurrent cold layer reads publish one identity independently of the world canvas");
            viewportContainer.AccessibilityName = "Viewport landmark";
            Check(!viewportContainer.GetConfigurationWarnings().Contains(landmarkWarning), "Naming the landmark clears its warning");
            viewportContainer.AccessibilityRegion = false;
            viewportContainer.AccessibilityRegion = false;
            Check(!viewportContainer.AccessibilityRegion && landmarkNotices == 3, "Flag and name changes refresh warnings once per committed value");
            using var done = new ManualResetEventSlim(); var job = WorkerThreadPool.AddTask(() =>
            {
                try
                {
                    Reject<InvalidOperationException>(() => first.GetCanvas()); Reject<InvalidOperationException>(() => lazy.GetCanvas()); Reject<InvalidOperationException>(() => _ = first.Color); Reject<InvalidOperationException>(() => first.Color = Colors.White); Reject<InvalidOperationException>(() => first.GetConfigurationWarnings()); Reject<InvalidOperationException>(() => first.UpdateConfigurationWarnings());
                    Reject<InvalidOperationException>(() => _ = viewportContainer.AccessibilityRegion);
                    Reject<InvalidOperationException>(() => viewportContainer.GetConfigurationWarnings());
                    Reject<InvalidOperationException>(() => viewportContainer.AccessibilityRegion = true);
                }
                finally { done.Set(); }
            }, highPriority: true);
            Check(done.Wait(TimeSpan.FromSeconds(5)), "Ordinary worker guards finish before owner assistance"); WorkerThreadPool.WaitForTaskCompletion(job);
            foreach (var node in providers)
            {
                a.RemoveChild(node); node.Dispose(); Reject<ObjectDisposedException>(() => node.GetConfigurationWarnings());
                if (node is Container container)
                {
                    Reject<ObjectDisposedException>(() => _ = container.AccessibilityRegion);
                    Reject<ObjectDisposedException>(() => container.AccessibilityRegion = true);
                }
            }
            first.Veto = true;
            var veto = CaptureCopy(Step); Check(veto is AggregateException, "Derived mutation veto propagates from its current group"); first.Veto = false;
            fail = true; var before = notices; var tick = PhysicsServer.SpaceGetTick(world.Space);
            var error = CaptureCopy(Step);
            Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(failure) && notices == before + 2 && PhysicsServer.SpaceGetTick(world.Space) == tick + 1, "Warning failure preserves committed tint, later groups and numerical continuation");
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            for (var i = 0; i < 128; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame(); own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Grouped modulation/warning full physics/query frames allocated {own}/{all} B");
            using var fallback = new CanvasModulate(); using var fallbackTree = new SceneTree(fallback);
            Check(fallback.GetCanvas() == fallback.GetWorld()!.Canvas, "Viewport-free canvas identity uses the same lazy logical world");
            using var packed = new PackedScene(); var capture = new ModulationCapture(); root.AddChild(capture); packed.Pack(capture);
            var regionCapture = new LandmarkCapture { AccessibilityRegion = true }; root.AddChild(regionCapture); packed.Pack(regionCapture);
            first.Dispose(); Reject<ObjectDisposedException>(() => first.GetCanvas()); Reject<ObjectDisposedException>(() => _ = first.Color);
            Console.WriteLine($"Group modulation {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}/{(poolOwner ? "pool" : "ordinary")}: concurrent canvas/tint, {providers.Length} warning providers, synchronous failures, guards and 128 full physics/query frames at {own}/{all} B passed.");
            void Write(TransformWriter writer, CanvasModulate tint, CanvasModulate foreign, bool primary)
            {
                if (cold && !together.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Modulation groups did not overlap");
                var rid = lazy.GetCanvas(); if (primary) left = rid; else right = rid;
                Check(first.GetCanvas() == world.Canvas && second.GetCanvas() == world.Canvas && layer.GetCanvas() != world.Canvas, "Active groups read coordinated default/layer identities");
                var color = (phase & 1) == 0 ? Colors.Red : Colors.Green;
                tint.Color = color; Check(tint.Color == color, "Own tint commits immediately");
                tint.PropagateNotification(CanvasItem.NotificationWorldChanged); tint.PropagateNotification(CanvasItem.NotificationVisibilityChanged);
                if (primary) layerTint.Color = Colors.Blue;
                if (cold) CheckCold(tint, foreign, primary);
                tint.UpdateConfigurationWarnings();
            }
            void CheckCold(CanvasModulate tint, CanvasModulate foreign, bool primary)
            {
                Reject<InvalidOperationException>(() => foreign.Color = Colors.White); Reject<InvalidOperationException>(() => foreign.GetConfigurationWarnings());
                Reject<ArgumentException>(() => tint.Color = new(float.NaN, 1, 1));
                Check(tint.GetConfigurationWarnings().Length > 0, "Duplicate warnings observe the synchronized canvas membership");
                if (primary) foreach (var provider in providers) { _ = provider.GetConfigurationWarnings(); provider.UpdateConfigurationWarnings(); }
                else foreach (var provider in providers) Reject<InvalidOperationException>(() => provider.GetConfigurationWarnings());
                if (primary)
                {
                    Check(viewportContainer.AccessibilityRegion && viewportContainer.GetConfigurationWarnings() is [landmarkWarning, "This container requires a direct SubViewport child to display content."],
                        "Own group sees both the unnamed landmark and the viewport-content warning");
                    Check(scrollContainer.AccessibilityRegion && scrollContainer.GetConfigurationWarnings() is [var content] && content.StartsWith("ScrollContainer is intended", StringComparison.Ordinal),
                        "A label in another group resolves the landmark warning and preserves the scroll-content warning");
                    Reject<InvalidOperationException>(() => label.GetWindow());
                }
                else
                {
                    Reject<InvalidOperationException>(() => _ = viewportContainer.AccessibilityRegion);
                    Reject<InvalidOperationException>(viewportContainer.UpdateConfigurationWarnings);
                }
                Reject<InvalidOperationException>(() => viewportContainer.AccessibilityRegion = false);
                Reject<InvalidOperationException>(viewportContainer.QueueSort);
            }
            void Step() { observing = true; try { tree.PhysicsFrame(.01); } finally { observing = false; } }
            void Frame()
            {
                phase++; var x = body.Position.X; Step(); point.Position = body.Position;
                Check(MathF.Abs(body.Position.X - x - .06f) < .002f && world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == body.GetRID(), "Tint and warning updates preserve selected physics motion/query identity");
            }
        }
    }
    private sealed class ModulationCapture : CanvasModulate
    {
        private static Node Create() => new ModulationCapture();
        protected override Func<Node> CreateSceneInstanceFactory()
        {
            _ = GetCanvas(); _ = GetConfigurationWarnings(); Reject<InvalidOperationException>(() => Color = Colors.Red); Reject<InvalidOperationException>(UpdateConfigurationWarnings);
            return Create;
        }
    }
    private sealed class LandmarkCapture : PanelContainer
    {
        private static Node Create() => new LandmarkCapture();
        protected override Func<Node> CreateSceneInstanceFactory()
        {
            Check(AccessibilityRegion && GetConfigurationWarnings().Length == 1, "Capture permits the landmark query");
            Reject<InvalidOperationException>(() => AccessibilityRegion = false);
            Reject<InvalidOperationException>(UpdateConfigurationWarnings);
            Reject<InvalidOperationException>(QueueSort);
            return Create;
        }
    }
}
