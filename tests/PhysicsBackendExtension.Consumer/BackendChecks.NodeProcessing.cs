using Electron2D;
using Path = System.IO.Path;

public static partial class BackendChecks
{
    private static readonly (string Name, Func<Node, int> Read, Action<Node> Write)[] ProcessingControls =
    [
        (nameof(Node.ProcessEnabled), n => n.ProcessEnabled ? 1 : 0, n => n.ProcessEnabled = false),
        (nameof(Node.PhysicsProcessEnabled), n => n.PhysicsProcessEnabled ? 1 : 0, n => n.PhysicsProcessEnabled = false),
        (nameof(Node.InternalProcessEnabled), n => n.InternalProcessEnabled ? 1 : 0, n => n.InternalProcessEnabled = false),
        (nameof(Node.InternalPhysicsProcessEnabled), n => n.InternalPhysicsProcessEnabled ? 1 : 0, n => n.InternalPhysicsProcessEnabled = false),
        (nameof(Node.InputEnabled), n => n.InputEnabled ? 1 : 0, n => n.InputEnabled = false),
        (nameof(Node.ShortcutInputEnabled), n => n.ShortcutInputEnabled ? 1 : 0, n => n.ShortcutInputEnabled = false),
        (nameof(Node.UnhandledKeyInputEnabled), n => n.UnhandledKeyInputEnabled ? 1 : 0, n => n.UnhandledKeyInputEnabled = false),
        (nameof(Node.UnhandledInputEnabled), n => n.UnhandledInputEnabled ? 1 : 0, n => n.UnhandledInputEnabled = false),
        (nameof(Node.ProcessPriority), n => n.ProcessPriority, n => n.ProcessPriority = 0),
        (nameof(Node.PhysicsProcessPriority), n => n.PhysicsProcessPriority, n => n.PhysicsProcessPriority = 0)
    ];

    private sealed class ProcessingProbeBody : RigidBody
    {
        internal Action<Node, bool>? Tick;
        protected override void OnProcess(double delta) { base.OnProcess(delta); Tick?.Invoke(this, false); }
        protected override void OnPhysicsProcess(double delta) { base.OnPhysicsProcess(delta); Tick?.Invoke(this, true); }
    }

    private interface IProcessingActor { Action<Node, int, InputEvent?>? Observe { get; set; } }
    private sealed class ProcessingActorNode : Node, IProcessingActor
    {
        public Action<Node, int, InputEvent?>? Observe { get; set; }
        protected override void OnProcess(double delta) { base.OnProcess(delta); Check(delta == ProcessDeltaTime, "Typed idle callback receives its lane delta"); Observe?.Invoke(this, 0, null); }
        protected override void OnPhysicsProcess(double delta) { base.OnPhysicsProcess(delta); Check(delta == PhysicsProcessDeltaTime, "Typed physics callback receives its lane delta"); Observe?.Invoke(this, 1, null); }
        protected override void OnInput(InputEvent e) { base.OnInput(e); Observe?.Invoke(this, 2, e); }
        protected override void OnShortcutInput(InputEvent e) { base.OnShortcutInput(e); Observe?.Invoke(this, 3, e); }
        protected override void OnUnhandledKeyInput(InputEventKey e) { base.OnUnhandledKeyInput(e); Observe?.Invoke(this, 4, e); }
        protected override void OnUnhandledInput(InputEvent e) { base.OnUnhandledInput(e); Observe?.Invoke(this, 5, e); }
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationInternalProcess) Observe?.Invoke(this, 6, null); if (what == NotificationInternalPhysicsProcess) Observe?.Invoke(this, 7, null); }
    }
    private sealed class ProcessingActorEntity : Entity, IProcessingActor
    {
        public Action<Node, int, InputEvent?>? Observe { get; set; }
        protected override void OnProcess(double delta) { base.OnProcess(delta); Check(delta == ProcessDeltaTime, "Typed idle callback receives its lane delta"); Observe?.Invoke(this, 0, null); }
        protected override void OnPhysicsProcess(double delta) { base.OnPhysicsProcess(delta); Check(delta == PhysicsProcessDeltaTime, "Typed physics callback receives its lane delta"); Observe?.Invoke(this, 1, null); }
        protected override void OnInput(InputEvent e) { base.OnInput(e); Observe?.Invoke(this, 2, e); }
        protected override void OnShortcutInput(InputEvent e) { base.OnShortcutInput(e); Observe?.Invoke(this, 3, e); }
        protected override void OnUnhandledKeyInput(InputEventKey e) { base.OnUnhandledKeyInput(e); Observe?.Invoke(this, 4, e); }
        protected override void OnUnhandledInput(InputEvent e) { base.OnUnhandledInput(e); Observe?.Invoke(this, 5, e); }
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationInternalProcess) Observe?.Invoke(this, 6, null); if (what == NotificationInternalPhysicsProcess) Observe?.Invoke(this, 7, null); }
    }
    private sealed class ProcessingActorBody : RigidBody, IProcessingActor
    {
        public Action<Node, int, InputEvent?>? Observe { get; set; }
        protected override void OnProcess(double delta) { base.OnProcess(delta); Check(delta == ProcessDeltaTime, "Typed idle callback receives its lane delta"); Observe?.Invoke(this, 0, null); }
        protected override void OnPhysicsProcess(double delta) { base.OnPhysicsProcess(delta); Check(delta == PhysicsProcessDeltaTime, "Typed physics callback receives its lane delta"); Observe?.Invoke(this, 1, null); }
        protected override void OnInput(InputEvent e) { base.OnInput(e); Observe?.Invoke(this, 2, e); }
        protected override void OnShortcutInput(InputEvent e) { base.OnShortcutInput(e); Observe?.Invoke(this, 3, e); }
        protected override void OnUnhandledKeyInput(InputEventKey e) { base.OnUnhandledKeyInput(e); Observe?.Invoke(this, 4, e); }
        protected override void OnUnhandledInput(InputEvent e) { base.OnUnhandledInput(e); Observe?.Invoke(this, 5, e); }
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationInternalProcess) Observe?.Invoke(this, 6, null); if (what == NotificationInternalPhysicsProcess) Observe?.Invoke(this, 7, null); }
    }

    public static void RunNodeProcessing(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var workers = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 1);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("Processing", static () => new Solver());
        try
        {
            List<string> defects = [];
            ProcessingStorage(defects); ProcessingLifetimeAndCapture();
            using var world = backend is { } selected ? new World(selected) : new World("Processing");
            var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
            var worker = new ProcessingProbeBody { GravityScale = 0, Sleeping = true, ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
            root.AddChild(worker);
            Node[] family = [new Node(), new Entity(), new Control(), new StaticBody(), new AnimatableBody(), new RigidBody(), new CharacterBody(), new PhysicalBone(), new Area(), new CollisionShape()];
            foreach (var node in family) { node.InternalProcessEnabled = node.InternalPhysicsProcessEnabled = false; worker.AddChild(node); }
            var owner = Environment.CurrentManagedThreadId;
            Task.Run(() =>
            {
                foreach (var node in family)
                    foreach (var control in ProcessingControls)
                    {
                        try { _ = control.Read(node); }
                        catch (Exception error) { defects.Add(node.GetType().Name + ": coordinated " + control.Name + " read rejected: " + error.GetType().Name); }
                        Reject<InvalidOperationException>(() => control.Write(node));
                    }
            }).GetAwaiter().GetResult();
            var workerCalls = 0;
            worker.Tick = (_, physics) =>
            {
                Check(physics && Environment.CurrentManagedThreadId != owner, "Processing metadata executes in an actual worker group"); workerCalls++;
                foreach (var node in family)
                    foreach (var control in ProcessingControls)
                        try { control.Write(node); }
                        catch (Exception error) { defects.Add(node.GetType().Name + ": current-group " + control.Name + " write rejected: " + error.GetType().Name); }
                Reject<InvalidOperationException>(() => worker.Position = Vector2.One);
                Reject<InvalidOperationException>(() => worker.InputPickable = true);
                Reject<InvalidOperationException>(() => root.ProcessEnabled = root.ProcessEnabled);
            };
            tree.PhysicsFrame(.01); worker.Tick = null;
            Check(workerCalls == 1, "The metadata fixture runs exactly once before selected physical integration");
            ProcessingDispatch(root, tree, world);
            ProcessingWarmFrames(root, tree, worker, world);
            ProcessingArchive(backend, threaded); ProcessingWorkerOwnedTree();
            foreach (var defect in defects) Console.Error.WriteLine("PROCESSING RED: " + defect);
            Check(defects.Count == 0, "Node processing regression failures: " + defects.Count);
            Console.WriteLine($"Node processing {backend?.ToString() ?? "registered"}, threaded={threaded}: metadata, callbacks, priorities, storage and fresh scene; 128 moving full frames 0/0 B.");
        }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, workers); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }

    private static void ProcessingStorage(List<string> defects)
    {
        using var source = new Node { ProcessPriority = int.MinValue, PhysicsProcessPriority = int.MaxValue, InternalProcessEnabled = true, InternalPhysicsProcessEnabled = true };
        using var packed = new PackedScene();
        var mismatch = 0;
        for (var mask = 0; mask < 64; mask++)
        {
            WriteProcessingMask(source, mask); packed.Pack(source); using var copy = packed.Instantiate();
            if (ReadProcessingMask(copy) != mask) mismatch++;
            Check(copy.ProcessPriority == int.MinValue && copy.PhysicsProcessPriority == int.MaxValue && !copy.InternalProcessEnabled && !copy.InternalPhysicsProcessEnabled,
                "Stored priorities retain all integer values while internal switches remain runtime state");
        }
        if (mismatch != 0) defects.Add("Packed six-callback configuration lost " + mismatch + "/64 flag combinations");
    }

    private static int ReadProcessingMask(Node node) => (node.ProcessEnabled ? 1 : 0) | (node.PhysicsProcessEnabled ? 2 : 0) |
        (node.InputEnabled ? 4 : 0) | (node.ShortcutInputEnabled ? 8 : 0) | (node.UnhandledKeyInputEnabled ? 16 : 0) | (node.UnhandledInputEnabled ? 32 : 0);
    private static void WriteProcessingMask(Node node, int mask)
    {
        node.ProcessEnabled = (mask & 1) != 0; node.PhysicsProcessEnabled = (mask & 2) != 0;
        node.InputEnabled = (mask & 4) != 0; node.ShortcutInputEnabled = (mask & 8) != 0;
        node.UnhandledKeyInputEnabled = (mask & 16) != 0; node.UnhandledInputEnabled = (mask & 32) != 0;
    }

    private sealed class ProcessingCaptureNode : Node
    {
        internal Action? Capturing;
        protected override Func<Node> CreateSceneInstanceFactory() { Capturing?.Invoke(); return Create; }
        private static Node Create() => new ProcessingCaptureNode();
    }
    private static void ProcessingLifetimeAndCapture()
    {
        using var source = new ProcessingCaptureNode(); using var packed = new PackedScene();
        source.Capturing = () =>
        {
            foreach (var control in ProcessingControls) Reject<InvalidOperationException>(() => control.Write(source));
        };
        packed.Pack(source); source.Capturing = null;
        using var copy = packed.Instantiate(); Check(ReadProcessingMask(copy) == 0, "Capture barriers release after every rejected metadata write");
        var dead = new Node(); dead.Dispose();
        foreach (var control in ProcessingControls)
        { Reject<ObjectDisposedException>(() => control.Read(dead)); Reject<ObjectDisposedException>(() => control.Write(dead)); }
    }

    private static void ProcessingDispatch(SubViewport root, SceneTree tree, World world)
    {
        var neutral = new ProcessingActorNode(); var spatial = new ProcessingActorEntity();
        var physical = new ProcessingActorBody { GravityScale = 0, CanSleep = false, LinearVelocity = Vector2.Right, LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace, Position = new(100, 100) };
        Node[] nodes = [neutral, spatial, physical]; List<(Node Node, int Lane)> trace = [];
        InputEvent? expectedEvent = null; var owner = Environment.CurrentManagedThreadId;
        Action<Node, int, InputEvent?> observe = (node, lane, e) =>
        {
            Check(Environment.CurrentManagedThreadId == owner, "Main processing and every input stage run on the owner");
            if (lane >= 2 && lane <= 5) Check(ReferenceEquals(e, expectedEvent), "Non-positional input stays the same borrowed event");
            trace.Add((node, lane));
        };
        foreach (var node in nodes)
        {
            Check(ReadProcessingMask(node) == 0, "Detached overrides start with disabled callback flags");
            ((IProcessingActor)node).Observe = observe; root.AddChild(node);
            Check(ReadProcessingMask(node) == 63, "Ready enables the complete actual typed callback family");
            node.InternalProcessEnabled = node.InternalPhysicsProcessEnabled = true;
        }
        neutral.ProcessPriority = int.MaxValue; spatial.ProcessPriority = int.MinValue; physical.ProcessPriority = 0;
        neutral.PhysicsProcessPriority = int.MinValue; spatial.PhysicsProcessPriority = int.MaxValue; physical.PhysicsProcessPriority = 0;
        tree.ProcessFrame(.02);
        Check(trace.SequenceEqual(new[] { (spatial as Node, 6), (spatial as Node, 0), (physical, 6), (physical, 0), (neutral, 6), (neutral, 0) }), "Idle priorities use the complete signed integer range with per-node internal/public phases"); trace.Clear();
        tree.PhysicsFrame(.01);
        Check(trace.SequenceEqual(new[] { (neutral as Node, 7), (neutral as Node, 1), (physical, 7), (physical, 1), (spatial, 7), (spatial, 1) }), "Physics priorities are independent of idle priorities"); trace.Clear();
        foreach (var node in nodes) node.ProcessPriority = node.PhysicsProcessPriority = 0;
        tree.ProcessFrame(.02);
        Check(trace.SequenceEqual(new[] { (neutral as Node, 6), (neutral as Node, 0), (spatial, 6), (spatial, 0), (physical, 6), (physical, 0) }), "Equal priorities retain captured tree order"); trace.Clear();
        using var key = new InputEventKey { Keycode = Key.F9, Pressed = true }; expectedEvent = key;
        root.PushInput(key, true);
        var expected = new List<(Node, int)>();
        foreach (var lane in new[] { 2, 3, 4, 5 }) foreach (var node in nodes.Reverse()) expected.Add((node, lane));
        Check(trace.SequenceEqual(expected), "All four input stages use reverse hierarchy order independently of callback priorities"); trace.Clear();
        ((IProcessingActor)physical).Observe = (node, lane, e) => { observe(node, lane, e); if (lane == 2) tree.SetInputAsHandled(); };
        root.PushInput(key, true); Check(trace.SequenceEqual(new[] { (physical as Node, 2) }), "Handling input stops every later node and stage"); trace.Clear();
        ((IProcessingActor)physical).Observe = observe;
        using var action = new InputEventAction { Action = "processing-probe", Pressed = true }; expectedEvent = action;
        root.PushInput(action, true); expected.RemoveAll(item => item.Item2 is 3 or 4);
        Check(trace.SequenceEqual(expected), "Non-keyboard action input skips shortcut and unhandled-key stages"); trace.Clear();
        var failure = new ApplicationException("Expected typed processing callback failure");
        ((IProcessingActor)spatial).Observe = (node, lane, e) => { observe(node, lane, e); if (lane == 1) throw failure; };
        var start = physical.GlobalPosition.X; var rid = physical.GetRID();
        var error = CaptureCopy(() => tree.PhysicsFrame(.01));
        Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(failure) && trace.Count == 6 && physical.GetRID() == rid && physical.GetWorld() == world && Math.Abs(physical.GlobalPosition.X - start - .01f) < .001f,
            "Typed callback errors preserve identity, later deliveries and the actual selected numerical step"); trace.Clear();
        ((IProcessingActor)spatial).Observe = (node, lane, e) => { observe(node, lane, e); if (lane == 2) throw failure; }; expectedEvent = key;
        error = CaptureCopy(() => root.PushInput(key, true));
        Check(error is AggregateException inputErrors && inputErrors.Flatten().InnerExceptions.Contains(failure) && trace.Count == 12 && !key.IsDisposed,
            "Input errors preserve the borrowed event and continue all eligible stages");
        foreach (var node in nodes) { ((IProcessingActor)node).Observe = null; node.Dispose(); }
    }

    private static void ProcessingWarmFrames(SubViewport root, SceneTree tree, ProcessingProbeBody worker, World world)
    {
        using var shape = new CircleShape { Radius = 1 };
        var body = new ProcessingActorBody { Name = "ProcessingMover", GravityScale = 0, CanSleep = false, LinearVelocity = Vector2.Right, LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace, Position = new(100, 100) };
        body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        var workerChild = new ProcessingActorBody { GravityScale = 0, Sleeping = true }; worker.AddChild(workerChild);
        var workerCalls = 0;
        worker.Tick = (_, _) =>
        {
            var mask = (++workerCalls & 1) == 0 ? 63 : 0; WriteProcessingMask(workerChild, mask);
            workerChild.InternalProcessEnabled = workerChild.InternalPhysicsProcessEnabled = mask == 0;
            workerChild.ProcessPriority = mask == 0 ? int.MinValue : int.MaxValue;
            workerChild.PhysicsProcessPriority = mask == 0 ? int.MaxValue : int.MinValue;
        };
        using var key = new InputEventKey { Keycode = Key.F9, Pressed = true }; var counts = new int[8];
        var inputOwner = Environment.CurrentManagedThreadId; var workerInputCalls = 0;
        workerChild.Observe = (_, lane, e) =>
        {
            if (lane >= 2 && lane <= 5) { Check(Environment.CurrentManagedThreadId == inputOwner && ReferenceEquals(e, key), "Worker input-flag writes affect actual owner-delivered callbacks"); workerInputCalls++; }
            else Check(Environment.CurrentManagedThreadId != inputOwner, "A physical child receives typed frame callbacks in its worker group");
        };
        body.Observe = (_, lane, e) => { counts[lane]++; if (lane >= 2 && lane <= 5) Check(ReferenceEquals(e, key), "Warmed input borrows its event"); };
        var callbacks = 0; PhysicsServer.BodySetForceIntegrationCallback(body.GetRID(), _ => callbacks++);
        var frame = 0;
        for (var i = 0; i < 128; i++) Frame();
        var before = counts.ToArray(); var forceBefore = callbacks; var workerBefore = workerCalls; var inputBefore = workerInputCalls;
        var start = body.GlobalPosition.X; var rid = body.GetRID(); Collect();
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame();
        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(own == 0 && all == 0 && counts.Select((value, lane) => value - before[lane]).All(value => value == 64) && callbacks - forceBefore == 128 && workerCalls - workerBefore == 256 && workerInputCalls - inputBefore == 512 && workerChild.Sleeping && worker.Sleeping &&
            body.GetRID() == rid && body.GetWorld() == world && Math.Abs(body.GlobalPosition.X - start - 1.28f) < .002f,
            $"Processing full idle/physics/input frames: {own}/{all} B, motion {body.GlobalPosition.X - start}, force callbacks {callbacks - forceBefore}, worker callbacks {workerCalls - workerBefore}");
        worker.Tick = null; body.Observe = null; workerChild.Observe = null; workerChild.Dispose(); body.Dispose();
        void Frame()
        {
            var mask = (++frame & 1) == 0 ? 63 : 0; WriteProcessingMask(body, mask);
            body.InternalProcessEnabled = body.InternalPhysicsProcessEnabled = mask == 0;
            body.ProcessPriority = mask == 0 ? int.MinValue : int.MaxValue;
            body.PhysicsProcessPriority = mask == 0 ? int.MaxValue : int.MinValue;
            tree.ProcessFrame(.02); tree.PhysicsFrame(.01); root.PushInput(key, true);
        }
    }

    private static void ProcessingWorkerOwnedTree()
    {
        var calls = 0;
        var task = WorkerThreadPool.AddTask(() =>
        {
            using var world = new World(PhysicsServer.Backend.CPU);
            var root = new SubViewport { World = world }; var child = new ProcessingProbeBody { GravityScale = 0, Sleeping = true, ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
            child.Tick = (_, physics) => { Check(physics, "Nested worker-owned tree runs only its fixed lane"); calls++; }; root.AddChild(child);
            using var tree = new SceneTree(root); Check(calls == 0, "Gate preparation never invokes game callbacks");
            tree.PhysicsFrame(.01); Check(calls == 1, "A one-worker pool can execute a worker-owned tree without preparation deadlock");
            child.Tick = null;
        });
        WorkerThreadPool.WaitForTaskCompletion(task); Check(calls == 1, "The nested tree retires its one actual callback");
    }

    private static void ProcessingArchive(PhysicsServer.Backend? backend, bool threaded)
    {
        var directory = Path.Combine(Path.GetTempPath(), "e2d-processing-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            using var scene = new Node { Name = "StoredProcessing", ProcessPriority = int.MinValue, PhysicsProcessPriority = int.MaxValue, InternalProcessEnabled = true, InternalPhysicsProcessEnabled = true };
            WriteProcessingMask(scene, 63);
            var body = new RigidBody { Name = "Body", GravityScale = 0, CanSleep = false, LinearVelocity = new(2, 0), LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace };
            using var shape = new CircleShape { Radius = 1 }; body.AddChild(new CollisionShape { Shape = shape }); scene.AddChild(body); body.Owner = body.GetChild(0).Owner = scene; WriteProcessingMask(body, 63);
            using var packed = new PackedScene(); packed.Pack(scene); var path = Path.Combine(directory, "processing.e2dscene"); ResourceSaver.Save(packed, path);
            var info = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) info.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name + ".dll"));
            info.ArgumentList.Add("--node-processing-child"); info.ArgumentList.Add(path); if (threaded) info.ArgumentList.Add("--threaded"); if (backend is { } selected) info.ArgumentList.Add(selected == PhysicsServer.Backend.GPU ? "--gpu" : "--cpu");
            using var child = System.Diagnostics.Process.Start(info)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); child.WaitForExit(); throw new TimeoutException("Fresh processing scene"); }
            Check(child.ExitCode == 0 && output.Result.Contains("Fresh node processing passed"), "Fresh processing scene: " + output.Result + error.Result);
        }
        finally { Directory.Delete(directory, true); }
    }
    public static void ReadNodeProcessing(string path, PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("Processing", static () => new Solver());
        using var world = backend is { } selected ? new World(selected) : new World("Processing");
        using var packed = ResourceLoader.Load<PackedScene>(path)!; var scene = packed.Instantiate(); var body = scene.GetNode<RigidBody>("Body");
        Check(ReadProcessingMask(scene) == 63 && ReadProcessingMask(body) == 63 && scene.ProcessPriority == int.MinValue && scene.PhysicsProcessPriority == int.MaxValue && !scene.InternalProcessEnabled && !scene.InternalPhysicsProcessEnabled && !scene.IsInsideTree,
            "Fresh file reconstructs all six callback switches and priorities while omitting runtime switches");
        var root = new SubViewport { World = world }; root.AddChild(scene); using var tree = new SceneTree(root);
        Check(ReadProcessingMask(scene) == 63 && ReadProcessingMask(body) == 63 && world.PhysicsBackendName == (backend?.ToString() ?? "Processing"), "Activation retains builtin configuration on the exact selected backend");
        var start = body.GlobalPosition.X; var rid = body.GetRID(); for (var i = 0; i < 16; i++) tree.PhysicsFrame(.01);
        Check(body.GetRID() == rid && Math.Abs(body.GlobalPosition.X - start - .32f) < .002f, "Fresh configured physical scene actually integrates");
        Console.WriteLine("Fresh node processing passed");
    }

}
