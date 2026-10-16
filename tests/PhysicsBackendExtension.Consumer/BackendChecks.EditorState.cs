using System.Diagnostics;
using Electron2D;
using IOPath = System.IO.Path;

public static partial class BackendChecks
{
    public static void RunEditorState(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? provider = null;
        if (backend is null) PhysicsServerManager.RegisterServer("EditorState", () => provider = new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("EditorState");
            Check(world.PhysicsBackendName == (backend?.ToString() ?? "EditorState") && world.PhysicsFallbackReason is null, "Exact authoring backend without fallback");
            EditorStateContract(world); EditorStateConnections(world); EditorStateArchive(backend, threaded); EditorStateFrames(world, provider);
            Console.WriteLine($"Editor state {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}: events, failures, ownership, fresh scene, motion and 128 full frames 0/0 B passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private sealed class EditorProbe : Node
    {
        internal Action? Validate;
        internal bool Veto;
        protected override void ValidateMutation() { base.ValidateMutation(); Validate?.Invoke(); }
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationPreDelete && Veto) CancelDisposal(); }
    }
    private sealed class EditorCollision : Node
    {
        private static Node Create() => new EditorCollision();
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static readonly PropertyDescriptor<EditorCollision, string> Collision = new("__SceneInstanceOrigin", _ => "bad", (_, _) => { }, _ => "", stored: true);
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(Collision);
    }
    private sealed class EditorCapture : Node
    {
        private static Node Create() => new EditorCapture();
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        internal Action? Read;
        private static readonly PropertyDescriptor<EditorCapture, int> Guard = new("Guard", n => { n.Read?.Invoke(); return 0; }, (_, _) => { }, _ => 0, stored: true);
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(Guard);
    }
    private static void EditorStateContract(World world)
    {
        using var detached = new EditorProbe(); var descriptions = 0;
        detached.EditorDescriptionChanged += node => { Check(node == detached, "Description sender"); descriptions++; };
        Check(detached.EditorDescription == "" && !detached.DisplayFolded && !detached.IsPartOfEditedScene, "Authoring defaults");
        detached.EditorDescription = "текст"; detached.EditorDescription = new string("текст".ToCharArray());
        Check(descriptions == 1, "Ordinal equal descriptions are silent even while detached");
        Reject<ArgumentNullException>(() => detached.EditorDescription = null!);
        detached.SetBlockSignals(true); detached.EditorDescription = "blocked"; detached.SetBlockSignals(false);
        Check(descriptions == 1 && detached.EditorDescription == "blocked", "Blocked description commits without event");
        var failure = new ApplicationException("description callback"); Action<Node> fail = _ => throw failure;
        detached.EditorDescriptionChanged += fail; Check(ReferenceEquals(CaptureCopy(() => detached.EditorDescription = "committed"), failure) && detached.EditorDescription == "committed", "Description failure follows commitment");
        detached.EditorDescriptionChanged -= fail;
        Action<Node> reenter = node => { if (node.EditorDescription == "outer") node.EditorDescription = "inner"; };
        detached.EditorDescriptionChanged += reenter; detached.EditorDescription = "outer"; detached.EditorDescriptionChanged -= reenter;
        Check(detached.EditorDescription == "inner" && descriptions == 4, "Description reentrancy sees committed text");
        var disposing = new EditorProbe(); var target = new EditorProbe(); disposing.AddChild(target); target.Validate = disposing.Dispose;
        Reject<ObjectDisposedException>(() => disposing.SetEditableInstance(target, false)); Check(disposing.IsDisposed && target.IsDisposed, "A target validation hook cannot write through a disposed receiver");
        var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        var edited = new EditorProbe { Name = "Edited" }; var instance = new EditorProbe { Name = "Instance" }; var leaf = new Node(); var outside = new Node();
        root.AddChild(edited); edited.AddChild(instance); instance.AddChild(leaf); root.AddChild(outside);
        tree.EditedSceneRoot = edited;
        Check(!root.IsPartOfEditedScene && edited.IsPartOfEditedScene && instance.IsPartOfEditedScene && leaf.IsPartOfEditedScene && !outside.IsPartOfEditedScene, "Explicit selected subtree");
        var changes = 0; instance.EditorStateChanged += node => { Check(node == instance, "State sender"); changes++; };
        edited.SetEditableInstance(instance, true); root.SetEditableInstance(instance, true);
        Check(changes == 2 && edited.IsEditableInstance(instance) && root.IsEditableInstance(instance), "Editable flag lives on target and repeats emit");
        edited.DisplayFolded = instance.DisplayFolded = true; edited.SetEditableInstance(instance, false);
        Check(!edited.DisplayFolded && instance.DisplayFolded && changes == 3, "Disabling unfolds receiver only");
        Check(!edited.IsEditableInstance(null), "Null query is false");
        Reject<ArgumentNullException>(() => edited.SetEditableInstance(null!, true));
        Reject<ArgumentException>(() => edited.SetEditableInstance(edited, true)); Reject<ArgumentException>(() => edited.IsEditableInstance(outside));
        edited.DisplayFolded = true; instance.Validate = () => throw failure;
        Check(ReferenceEquals(CaptureCopy(() => edited.SetEditableInstance(instance, false)), failure) && edited.DisplayFolded && changes == 3, "Both mutation hooks precede flag and fold writes"); instance.Validate = null;
        edited.SetEditableInstance(instance, true); instance.Reparent(outside); Check(root.IsEditableInstance(instance) && !instance.IsPartOfEditedScene, "Flag follows reparent beyond selected scene"); instance.Reparent(edited);
        var before = changes; instance.ProcessMode = ProcessMode.Always; instance.ProcessMode = ProcessMode.Always;
        instance.Owner = edited; instance.Owner = edited; instance.UniqueNameInOwner = true; instance.UniqueNameInOwner = true;
        instance.AddToGroup("temporary"); instance.RemoveFromGroup("temporary"); instance.AddToGroup("stored", true); instance.AddToGroup("stored", true); instance.RemoveFromGroup("stored"); instance.RemoveFromGroup("stored");
        Check(changes == before + 5, "Process, owner, unique and successful persistent group changes emit exactly once");
        instance.SetBlockSignals(true); instance.AddToGroup("blocked", true); instance.SetBlockSignals(false); Check(changes == before + 5 && instance.IsInGroup("blocked"), "State emission respects blocking");
        tree.EditedSceneRoot = null; instance.AddToGroup("unselected", true); Check(changes == before + 5 && !instance.IsPartOfEditedScene, "Unselected authoring state emits nothing");
        tree.EditedSceneRoot = root; Check(root.IsPartOfEditedScene && outside.IsPartOfEditedScene, "Tree root itself can be selected");
        Exception? foreign = null; var thread = new Thread(() => foreign = CaptureCopy(() => _ = instance.IsPartOfEditedScene)); thread.Start(); thread.Join(); Check(foreign is InvalidOperationException, "Foreign thread cannot read authoring state");
        using var guarded = new EditorCapture(); var child = new Node(); guarded.AddChild(child); child.Owner = guarded; guarded.DisplayFolded = true;
        guarded.Read = () => { Reject<InvalidOperationException>(() => guarded.EditorDescription = "bad"); Reject<InvalidOperationException>(() => guarded.DisplayFolded = false); Reject<InvalidOperationException>(() => guarded.SetEditableInstance(child, true)); };
        using var packed = new PackedScene(); packed.Pack(guarded); Check(guarded.DisplayFolded && !guarded.IsEditableInstance(child), "Capture rejects all authoring writes before commitment");
        using var collision = new EditorCollision(); Reject<InvalidOperationException>(() => packed.Pack(collision)); Check(!packed.CanInstantiate(), "Reserved metadata rejects conflicting consumer schemas and preserves failed-pack empty state");
        var veto = new EditorProbe { Veto = true }; edited.AddChild(veto); var retained = 0; veto.EditorDescriptionChanged += _ => retained++; veto.Dispose(); veto.EditorDescription = "alive";
        Check(!veto.IsDisposed && retained == 1, "Disposal cancellation retains editor subscriptions"); veto.Veto = false; veto.Dispose();
        Reject<ObjectDisposedException>(() => veto.EditorDescription = "dead"); Reject<ObjectDisposedException>(() => _ = veto.IsPartOfEditedScene);
    }

    private static EventConnection EditorRename(Node source, Node target, bool failRemoval) => EventConnection.Subscribe<Node>(
        h => source.Renamed += h, h => { source.Renamed -= h; if (failRemoval) throw new ArithmeticException("removal"); }, _ => target.EditorDescription = source.Name);
    private static EventConnection EditorRenameOnce(Node source, Node target, bool _) => EventConnection.Subscribe<Node>(
        h => source.Renamed += h, h => source.Renamed -= h, _ => target.EditorDescription = source.Name, oneShot: true);
    private static EventConnection EditorRenameDeferred(Node source, Node target, bool _) => EventConnection.Subscribe<Node>(
        h => source.Renamed += h, h => source.Renamed -= h, _ => target.EditorDescription = source.Name, oneShot: true, defer: source.Tree!.Defer);
    private static void EditorStateConnections(World world)
    {
        var root = new SubViewport { World = world }; var publisher = new Node(); var receiver = new Node(); root.AddChild(publisher); root.AddChild(receiver);
        using var tree = new SceneTree(root); tree.EditedSceneRoot = root;
        var binding = ResourceFileTypes.RegisterSceneConnection<Node, Node, bool>("consumer.editor-state." + world.PhysicsBackendName, "Renamed", "Describe", EditorRename);
        var single = ResourceFileTypes.RegisterSceneConnection<Node, Node, bool>("consumer.editor-once." + world.PhysicsBackendName, "Renamed", "Describe", EditorRenameOnce, oneShot: true);
        var changes = 0; publisher.EditorStateChanged += _ => changes++;
        var token = binding.Subscribe(publisher, receiver, false); Check(changes == 1, "Authored connection addition emits"); token.Dispose(); token.Dispose(); Check(changes == 2, "Manual disconnection emits once");
        using var once = single.Subscribe(publisher, receiver, false); publisher.Name = "Once"; Check(changes == 3 && receiver.EditorDescription == "Once" && !once.IsConnected, "Automatic one-shot completion does not emit a manual disconnect");
        var failure = new ApplicationException("connection event"); Action<Node> fail = _ => throw failure; publisher.EditorStateChanged += fail;
        Check(ReferenceEquals(CaptureCopy(() => binding.Subscribe(publisher, receiver, false)), failure) && changes == 4, "Failed add rolls back without second authoring event");
        var errors = CaptureCopy(() => binding.Subscribe(publisher, receiver, true)) as AggregateException;
        Check(errors is not null && errors.Flatten().InnerExceptions.Contains(failure) && errors.Flatten().InnerExceptions.Any(e => e is ArithmeticException) && changes == 5, "Failed add preserves callback and rollback errors");
        publisher.EditorStateChanged -= fail; token = binding.Subscribe(publisher, receiver, true); publisher.EditorStateChanged += fail;
        errors = CaptureCopy(token.Dispose) as AggregateException;
        Check(errors is not null && errors.Flatten().InnerExceptions.Contains(failure) && errors.Flatten().InnerExceptions.Any(e => e is ArithmeticException) && !token.IsConnected && changes == 7, "Failed removal preserves both failures and terminal state"); publisher.EditorStateChanged -= fail;
        token = binding.Subscribe(publisher, receiver, false); receiver.Dispose(); Check(!token.IsConnected && changes == 8, "Endpoint disposal is not an authoring disconnect");
        var deferred = ResourceFileTypes.RegisterSceneConnection<Node, Node, bool>("consumer.editor-deferred." + world.PhysicsBackendName, "Renamed", "Describe", EditorRenameDeferred, oneShot: true, deferred: true);
        var replacement = new Node(); root.AddChild(replacement); token = deferred.Subscribe(publisher, replacement, false); publisher.Name = "Pending"; token.Dispose(); tree.FlushDeferred();
        Check(changes == 9 && replacement.EditorDescription == "", "Canceling an accepted deferred one-shot does not report a manual disconnect or deliver its payload");
    }

    private static Node EditorStateSource()
    {
        using var disk = new CircleShape { Radius = 1, ResourceLocalToScene = true };
        using var inner = new Node { Name = "Inner" }; var body = new RigidBody { Name = "Body", GravityScale = 0, CanSleep = false, LinearVelocity = new(3, 0), LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace };
        body.AddChild(new CollisionShape { Name = "Shape", Shape = disk }); inner.AddChild(body); body.Owner = inner; body.GetChild(0).Owner = inner;
        using var template = new PackedScene(); template.Pack(inner); template.ResourcePath = "res://editable-inner.e2dscene";
        var instance = template.Instantiate(); instance.Name = "Instance"; instance.EditorDescription = "instance description"; instance.DisplayFolded = true;
        var outer = new Node { Name = "Outer", EditorDescription = "outer description", DisplayFolded = true }; outer.AddChild(instance); instance.Owner = outer;
        using var excluded = new PackedScene(); excluded.Pack(outer); Check(excluded.GetState().GetNodeCount() == 2, "Disabled instance-owned branch remains excluded from flattened capture");
        outer.SetEditableInstance(instance, true);
        return outer;
    }
    private static void EditorStateArchive(PhysicsServer.Backend? backend, bool threaded)
    {
        var directory = IOPath.Combine(IOPath.GetTempPath(), "e2d-editor-state-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            using var outer = EditorStateSource(); using var scene = new PackedScene(); scene.Pack(outer);
            Check(scene.GetState().GetNodeCount() == 4 && scene.GetState().GetNodeOwnerPath(2) == "Instance", "Editable owner selection and stored ownership");
            using (var copy = (PackedScene)scene.Duplicate(true)) using (var restored = copy.Instantiate()) EditorStateRestored(restored);
            var file = IOPath.Combine(directory, "editor.e2dscene"); ResourceSaver.Save(scene, file);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (IOPath.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(IOPath.Combine(AppContext.BaseDirectory, System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name + ".dll"));
            start.ArgumentList.Add("--editor-state-child"); start.ArgumentList.Add(file); if (threaded) start.ArgumentList.Add("--threaded"); if (backend is { } value) start.ArgumentList.Add(value == PhysicsServer.Backend.GPU ? "--gpu" : "--cpu");
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); process.WaitForExit(); throw new TimeoutException("Editor state fresh consumer"); }
            Task.WaitAll(output, errors); Check(process.ExitCode == 0 && output.Result.Contains("Editor archive passed"), $"Fresh editor scene: {output.Result} {errors.Result}");
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void EditorStateRestored(Node restored)
    {
        var instance = restored.GetNode("Instance"); var body = restored.GetNode<RigidBody>("Instance/Body");
        Check(restored.EditorDescription == "outer description" && instance.EditorDescription == "instance description" && !restored.DisplayFolded && !instance.DisplayFolded, "Descriptions stored; folding transient");
        Check(instance.SceneFilePath == "res://editable-inner.e2dscene" && restored.IsEditableInstance(instance) && body.Owner == instance && body.GetChild(0).Owner == instance && !restored.IsPartOfEditedScene, $"Instance origin/flag/ownership: path={instance.SceneFilePath}, editable={restored.IsEditableInstance(instance)}, bodyOwner={body.Owner?.Name}, shapeOwner={body.GetChild(0).Owner?.Name}, edited={restored.IsPartOfEditedScene}");
    }
    public static void ReadEditorState(string path, PhysicsServer.Backend? backend, bool threaded)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded); if (backend is null) PhysicsServerManager.RegisterServer("EditorState", static () => new Solver());
        using var scene = ResourceLoader.Load<PackedScene>(path); var restored = scene.Instantiate(); EditorStateRestored(restored);
        using var world = backend is { } selected ? new World(selected) : new World("EditorState"); var root = new SubViewport { World = world }; root.AddChild(restored); using var tree = new SceneTree(root); tree.EditedSceneRoot = restored;
        Check(world.PhysicsBackendName == (backend?.ToString() ?? "EditorState") && world.PhysicsFallbackReason is null, "Fresh process retains exact selected backend without fallback");
        var body = restored.GetNode<RigidBody>("Instance/Body"); var before = body.Position.X; tree.PhysicsFrame(.1);
        using var point = new PhysicsPointQueryParameters { Position = body.GlobalPosition }; var hits = new PhysicsPointResult[2];
        Check(Math.Abs(body.Position.X - before - .3f) < .001f && world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == body.GetRID() && body.IsPartOfEditedScene, "Fresh physical scene moves and queries on selected backend");
        using var recaptured = new PackedScene(); recaptured.Pack(restored); using var second = recaptured.Instantiate(); EditorStateRestored(second); Console.WriteLine("Editor archive passed");
    }

    private static void EditorStateFrames(World world, Solver? provider)
    {
        using var disk = new CircleShape { Radius = 1 }; var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(3, 0), LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace };
        body.AddChild(new CollisionShape { Shape = disk }); var root = new SubViewport { World = world }; root.AddChild(body); using var tree = new SceneTree(root); tree.EditedSceneRoot = root;
        var events = 0; body.EditorStateChanged += _ => events++; var descriptions = 0; body.EditorDescriptionChanged += _ => descriptions++;
        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[2]; var rid = body.GetRID(); var identity = body.InstanceID;
        if (provider is not null)
        {
            root.DisplayFolded = true;
            provider.StatusHook = () =>
            {
                Reject<InvalidOperationException>(() => body.EditorDescription = "borrowed");
                Reject<InvalidOperationException>(() => body.DisplayFolded = true);
                Reject<InvalidOperationException>(() => root.SetEditableInstance(body, false));
            };
            try { query.Position = body.GlobalPosition; Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && root.DisplayFolded, "Numerical borrowing rejects both-node authoring before unfolding receiver"); }
            finally { provider.StatusHook = null; }
        }
        var workers = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2);
        try
        {
            var ownerThread = Environment.CurrentManagedThreadId; var workerThread = 0; var workerEvents = 0;
            var worker = new GroupProbe { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread }; root.AddChild(worker);
            worker.EditorStateChanged += node => { Check(node == worker && Environment.CurrentManagedThreadId == workerThread, "Editor state uses actual mutating group thread"); workerEvents++; };
            worker.Signal = (node, physics) =>
            {
                workerThread = Environment.CurrentManagedThreadId;
                Check(node.IsPartOfEditedScene, "Selected context is observable in current processing group");
                Reject<InvalidOperationException>(() => _ = root.IsPartOfEditedScene);
                Reject<InvalidOperationException>(() => node.EditorDescription = "foreign authoring");
                node.AddToGroup("worker-authored", true); node.RemoveFromGroup("worker-authored");
            };
            worker.ProcessEnabled = true; tree.ProcessFrame(.01);
            Check(workerThread != ownerThread && workerEvents == 2, "Persistent worker groups emit authoring events outside owner thread"); worker.Dispose();
        }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, workers); }
        for (var i = 0; i < 128; i++) Frame(i);
        var x = body.Position.X; var prior = events; var priorDescriptions = descriptions; Collect();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(i);
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0 && Math.Abs(body.Position.X - x - 38.4f) < .01f && events == prior + 384 && descriptions == priorDescriptions + 128 && body.GetRID() == rid && body.InstanceID == identity,
            $"Warm editor/physical frames: {owner}/{all} B, motion={body.Position.X - x}, state={events - prior}, description={descriptions - priorDescriptions}");
        void Frame(int index)
        {
            body.EditorDescription = (index & 1) == 0 ? "first" : "second"; body.DisplayFolded = (index & 1) == 0;
            root.SetEditableInstance(body, (index & 1) == 0); body.AddToGroup("authored", true); body.RemoveFromGroup("authored");
            tree.PhysicsFrame(.1); query.Position = body.GlobalPosition;
            Check(body.IsPartOfEditedScene && world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == rid, "Folded physics remains active");
        }
    }
}
