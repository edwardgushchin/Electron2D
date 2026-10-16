using Electron2D;

public static partial class BackendChecks
{
    private interface ILifecycleProbe { Action<Node, int>? Lifecycle { get; set; } }
    private sealed class LifecycleNode : Node, ILifecycleProbe
    {
        public Action<Node, int>? Lifecycle { get; set; }
        protected override void OnNotification(int what) { Lifecycle?.Invoke(this, what); base.OnNotification(what); Lifecycle?.Invoke(this, what + 1000); }
        protected override void OnEnterTree() { base.OnEnterTree(); Lifecycle?.Invoke(this, -10); }
        protected override void OnExitTree() { Lifecycle?.Invoke(this, -11); base.OnExitTree(); }
        protected override void OnReady() { base.OnReady(); Lifecycle?.Invoke(this, -13); }
        protected override Func<Node> CreateSceneInstanceFactory() => static () => new LifecycleNode();
    }
    private sealed class LifecycleEntity : Entity, ILifecycleProbe
    {
        public Action<Node, int>? Lifecycle { get; set; }
        protected override void OnNotification(int what) { Lifecycle?.Invoke(this, what); base.OnNotification(what); Lifecycle?.Invoke(this, what + 1000); }
        protected override void OnEnterTree() { base.OnEnterTree(); Lifecycle?.Invoke(this, -10); }
        protected override void OnExitTree() { Lifecycle?.Invoke(this, -11); base.OnExitTree(); }
        protected override void OnReady() { base.OnReady(); Lifecycle?.Invoke(this, -13); }
        protected override Func<Node> CreateSceneInstanceFactory() => static () => new LifecycleEntity();
    }
    private sealed class LifecycleBody : RigidBody, ILifecycleProbe
    {
        public Action<Node, int>? Lifecycle { get; set; }
        protected override void OnNotification(int what) { Lifecycle?.Invoke(this, what); base.OnNotification(what); Lifecycle?.Invoke(this, what + 1000); }
        protected override void OnEnterTree() { base.OnEnterTree(); Lifecycle?.Invoke(this, -10); }
        protected override void OnExitTree() { Lifecycle?.Invoke(this, -11); base.OnExitTree(); }
        protected override void OnReady() { base.OnReady(); Lifecycle?.Invoke(this, -13); }
        protected override Func<Node> CreateSceneInstanceFactory() => static () => new LifecycleBody();
    }

    private sealed class LifecycleWorkerBody : RigidBody
    {
        internal Action? Tick;
        protected override void OnPhysicsProcess(double delta) => Tick?.Invoke();
    }

    public static void RunNodeLifecycle(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("Lifecycle", () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("Lifecycle");
            using var shape = new CircleShape { Radius = 1 };
            var viewport = new SubViewport { World = world }; using var tree = new SceneTree(viewport);
            var log = new List<(string Name, int Phase)>(128);
            var parent = new LifecycleNode { Name = "Parent" };
            var front = new LifecycleEntity { Name = "Front" };
            var body = new LifecycleBody { Name = "Body", GravityScale = 0, CanSleep = false, LinearDamp = 0, AngularDamp = 0, LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace };
            body.AddChild(new CollisionShape { Shape = shape });
            var back = new LifecycleNode { Name = "Back" };
            parent.AddChild(front, internalMode: Node.InternalMode.Front); parent.AddChild(body); parent.AddChild(back, internalMode: Node.InternalMode.Back);
            Node[] nodes = [parent, front, body, back]; var watched = new HashSet<Node>(nodes);
            var recording = true;
            foreach (var node in nodes)
            {
                ((ILifecycleProbe)node).Lifecycle = (current, phase) =>
                {
                    if (phase is 10 or 11 or 13 or 27 or 1010 or 1011 or 1013 or 1027 or -10 or -11 or -13) Record(current, phase);
                };
                node.TreeEntered += current => Record(current, 100);
                node.TreeExiting += current => Record(current, 104);
                node.TreeExited += current => Record(current, 107);
                node.Ready += current => Record(current, 103);
                node.ChildEnteredTree += (_, child) => { if (watched.Contains(child)) Record(child, 102); };
                node.ChildExitingTree += (_, child) => { if (watched.Contains(child)) Record(child, 106); };
            }
            viewport.ChildEnteredTree += (_, child) => { if (watched.Contains(child)) Record(child, 102); };
            viewport.ChildExitingTree += (_, child) => { if (watched.Contains(child)) Record(child, 106); };
            tree.NodeAdded += (_, node) => { if (watched.Contains(node)) Record(node, 101); };
            tree.NodeRemoved += (_, node) => { if (watched.Contains(node)) Record(node, 105); };
            viewport.AddChild(parent);
            var expected = new List<(string, int)>();
            foreach (var node in nodes) foreach (var phase in new[] { 10, 1010, -10, 100, 101, 102 }) expected.Add((node.Name, phase));
            foreach (var node in nodes.Skip(1).Append(parent)) foreach (var phase in new[] { 27, 1027, 13, -13, 1013, 103 }) expected.Add((node.Name, phase));
            Check(log.SequenceEqual(expected), "Complete mixed hierarchy entry/ready order: " + string.Join(',', log));
            Check(nodes.All(node => node.IsNodeReady), "Ready is consumed on every node");
            log.Clear(); viewport.RemoveChild(parent);
            expected.Clear();
            foreach (var node in nodes.Skip(1).Reverse().Append(parent)) foreach (var phase in new[] { -11, 104, 11, 1011, 105, 106 }) expected.Add((node.Name, phase));
            foreach (var node in nodes.Skip(1).Reverse().Append(parent)) expected.Add((node.Name, 107));
            Check(log.SequenceEqual(expected), "Complete reverse-sibling exit, removal and after-exit order: " + string.Join(',', log));
            Check(parent.Parent is null && nodes.All(node => node.Tree is null && node.IsNodeReady), "Exit preserves consumed readiness and detaches the whole hierarchy before after-exit signals");
            log.Clear(); viewport.AddChild(parent);
            Check(log.Count(item => item.Phase == 27) == 4 && !log.Any(item => item.Phase is 13 or -13 or 103), "Every reentry gets post-enter; consumed ready remains one-shot");
            parent.RequestReady(); Check(!parent.IsNodeReady && body.IsNodeReady, "RequestReady only rearms its receiver");
            log.Clear(); viewport.RemoveChild(parent); viewport.AddChild(parent);
            Check(log.Count(item => item.Phase == 103) == 1 && log.Contains(("Parent", 103)), "Requested ready repeats only on the requested node's next attachment");
            recording = false;
            var manual = new LifecycleNode { Name = "Manual" }; var manualCalls = new List<int>(); manual.Lifecycle = (_, phase) => manualCalls.Add(phase);
            manual.Notify(Node.NotificationEnterTree); manual.Notify(Node.NotificationExitTree);
            Check(!manualCalls.Contains(-10) && !manualCalls.Contains(-11) && !manual.IsInsideTree, "Manual tree notifications do not perform managed enter/exit callbacks");
            manual.Notify(Node.NotificationReady); Check(manualCalls.Contains(-13) && !manual.IsNodeReady, "Manual ready runs the hook without consuming scene-managed readiness"); manual.Dispose();
            CheckReadyInsertion();
            LifecycleFailures(viewport, world, backend, threaded);
            ArchiveLifecycle(backend, threaded);
            LifecycleExitGuards(viewport, tree, world, shape);
            var worker = new LifecycleWorkerBody { Name = "Worker", ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, GravityScale = 0, Sleeping = true };
            viewport.AddChild(worker); var workerCalls = 0;
            worker.Tick = () =>
            {
                if (workerCalls++ == 0)
                {
                    Check(worker.IsNodeReady, "A worker sees initially consumed ready state");
                    Reject<InvalidOperationException>(parent.RequestReady);
                    Reject<InvalidOperationException>(() => _ = parent.IsNodeReady);
                }
                worker.RequestReady(); Check(!worker.IsNodeReady, "Own-group ready metadata mutation executes on an actual physical worker");
            };
            Exception? foreignReady = null, foreignRequest = null;
            var foreign = new Thread(() => { foreignReady = CaptureCopy(() => _ = worker.IsNodeReady); foreignRequest = CaptureCopy(worker.RequestReady); }); foreign.Start(); foreign.Join();
            Check(foreignReady is InvalidOperationException && foreignRequest is InvalidOperationException, "Unrelated worker ready access rejects");
            body.LinearVelocity = Vector2.Right;
            var steps = 0; PhysicsServer.BodySetForceIntegrationCallback(body.GetRID(), _ => steps++);
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
            var start = body.Position.X; var initialSteps = steps; var rid = body.GetRID();
            Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && Math.Abs(body.Position.X - start - 1.28f) < .002f && steps == initialSteps + 128 && body.GetRID() == rid,
                $"Lifecycle consumers retain real body motion, callbacks, RID and strict warm allocation: {own}/{all} B; motion={body.Position.X - start}, steps={steps - initialSteps}, same RID={body.GetRID() == rid}");
            Check(workerCalls == 256 && worker.Sleeping && !worker.IsNodeReady, "Ready metadata does not wake the sleeping body or disable its independent process callback");
            worker.Tick = null;
            Console.WriteLine($"Node lifecycle {backend}, threaded={threaded}: mixed entry/ready/reverse-exit, reentry, requests, insertion, manual delivery and 128 moving full frames; 0/0 B.");

            void Record(Node node, int phase)
            {
                if (!recording) return;
                log.Add((node.Name, phase));
                if (phase is -10 or 100 or 101 or 102 or -11 or 104 or 11 or 105 or 106)
                    Check(ReferenceEquals(node.Tree, tree), "Lifecycle phase retains tree association: " + phase);
                if (ReferenceEquals(node, body) && phase == -10)
                    Check(ReferenceEquals(body.GetWorld(), world) && PhysicsServer.BodyGetSpace(body.GetRID()) == world.Space, "Entering body observes its selected world and completed base attachment");
                if (ReferenceEquals(node, body) && phase is -11 or 104)
                    Check(PhysicsServer.BodyGetSpace(body.GetRID()) == world.Space, "Body remains bound during exit hook and TreeExiting");
                if (phase == 107) Check(nodes.All(item => item.Tree is null) && parent.Parent is null, "After-exit observers see the entire pruned hierarchy detached");
            }
            void CheckReadyInsertion()
            {
                var order = new List<string>();
                var branch = new LifecycleNode { Name = "Insertion" }; var existing = new LifecycleNode { Name = "Existing" };
                var added = new LifecycleNode { Name = "Added" }; branch.AddChild(existing);
                branch.Lifecycle = (_, phase) => { if (phase == -10) branch.AddChild(added); };
                existing.TreeEntered += _ => order.Add("existing-enter"); added.Ready += _ => order.Add("added-ready");
                viewport.AddChild(branch);
                Check(order.SequenceEqual(new[] { "existing-enter", "added-ready" }), "Enter-added children wait for the parent's ready phase");
                var duringReady = new LifecycleNode { Name = "DuringReady" }; var readyCount = 0;
                duringReady.Ready += _ => readyCount++;
                branch.Lifecycle = (_, phase) => { if (phase == -13) branch.AddChild(duringReady); };
                branch.RequestReady(); viewport.RemoveChild(branch); viewport.AddChild(branch);
                Check(readyCount == 1 && duringReady.IsNodeReady, "Ready-added children complete ready exactly once");
                branch.Lifecycle = null; branch.Dispose();
                var snapshotRoot = new LifecycleNode(); var first = new LifecycleNode(); var second = new LifecycleNode();
                snapshotRoot.AddChild(first); snapshotRoot.AddChild(second); var posts = 0;
                second.Lifecycle = (_, phase) => { if (phase == 27) posts++; };
                first.Lifecycle = (_, phase) => { if (phase == -13) { snapshotRoot.RemoveChild(second); snapshotRoot.AddChild(second); } };
                viewport.AddChild(snapshotRoot);
                Check(posts == 1 && second.IsNodeReady, "Reentered siblings cannot revive an older ready snapshot");
                snapshotRoot.Dispose();
                var requested = new LifecycleNode(); var requests = 0;
                requested.Lifecycle = (node, phase) => { if (phase == -13 && requests++ == 0) node.RequestReady(); };
                viewport.AddChild(requested); Check(!requested.IsNodeReady, "A ready callback can rearm itself without immediate recursion");
                viewport.RemoveChild(requested); viewport.AddChild(requested);
                Check(requests == 2 && requested.IsNodeReady, "A request made in ready is consumed only on the next attachment"); requested.Dispose();
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }
    private static void LifecycleFailures(SubViewport viewport, World world, PhysicsServer.Backend? backend, bool threaded)
    {
        foreach (var phase in new[] { 10, 1010, -10, 100, 101, 102, 27, 1027, 13, -13, 103 })
        {
            using var isolated = backend is { } selected ? new World(selected) : new World("Lifecycle");
            var root = new SubViewport { World = isolated }; var failed = new LifecycleBody(); root.AddChild(failed);
            var failure = new ApplicationException("Expected lifecycle activation failure");
            failed.Lifecycle = (_, what) => { if (what == phase) throw failure; };
            if (phase == 100) failed.TreeEntered += _ => throw failure;
            if (phase == 101) failed.TreeEntered += _ => failed.Tree!.NodeAdded += (_, node) => { if (ReferenceEquals(node, failed)) throw failure; };
            if (phase == 102) root.ChildEnteredTree += (_, _) => throw failure;
            if (phase == 103) failed.Ready += _ => throw failure;
            var exited = 0; failed.TreeExited += _ => exited++;
            var error = CaptureCopy(() => _ = new SceneTree(root));
            Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(failure) && exited == 1 &&
                root.Tree is null && failed.Tree is null && !failed.IsNodeReady && PhysicsServer.BodyGetSpace(failed.GetRID()) == default,
                "Failed activation releases every native binding and consumed ready state with original error identity");
            root.Dispose();
        }
        var branch = new Node(); var body = new LifecycleBody(); branch.AddChild(body); viewport.AddChild(branch);
        var expected = new[] { new ApplicationException("Exit hook"), new ApplicationException("Exiting event"), new ApplicationException("Exited event"), new ApplicationException("Exit notification"), new ApplicationException("Removed event"), new ApplicationException("Child exiting event") };
        var finished = false; body.Lifecycle = (_, phase) => { if (phase == -11) throw expected[0]; if (phase == 11) throw expected[3]; };
        Action<SceneTree, Node> removed = (_, node) => { if (ReferenceEquals(node, body)) throw expected[4]; };
        viewport.Tree!.NodeRemoved += removed;
        branch.ChildExitingTree += (_, _) => throw expected[5];
        body.TreeExiting += _ => { Check(PhysicsServer.BodyGetSpace(body.GetRID()) == world.Space, "Failing exit preserves the live binding for TreeExiting"); throw expected[1]; };
        body.TreeExited += _ => { Check(body.Tree is null && PhysicsServer.BodyGetSpace(body.GetRID()) == default, "After-exit observes completed physical release"); throw expected[2]; };
        branch.TreeExited += _ => finished = true;
        var removal = CaptureCopy(() => viewport.RemoveChild(branch));
        Check(removal is AggregateException errors && expected.All(error => errors.Flatten().InnerExceptions.Contains(error)) && finished &&
            branch.Parent is null && body.Tree is null && !body.IsDisposed, "Every exit stage and native cleanup runs after multiple callback failures");
        viewport.Tree!.NodeRemoved -= removed;
        body.Lifecycle = null; branch.Dispose();
    }

    private static void LifecycleExitGuards(SubViewport viewport, SceneTree tree, World world, Shape shape)
    {
        var area = new Area(); area.AddChild(new CollisionShape { Shape = shape }); viewport.AddChild(area);
        var exitingArea = false; var exitedArea = false;
        area.TreeExiting += _ => { Check(PhysicsServer.AreaGetSpace(area.GetRID()) == world.Space, "Area remains bound through TreeExiting"); exitingArea = true; };
        area.TreeExited += _ => { Check(PhysicsServer.AreaGetSpace(area.GetRID()) == default, "Area is released before TreeExited"); exitedArea = true; };
        viewport.RemoveChild(area); Check(exitingArea && exitedArea, "Area executes both exit stages"); area.Dispose();
        var first = new RigidBody { Name = "JointFirst" }; var second = new RigidBody { Name = "JointSecond", Position = new(2, 0) };
        viewport.AddChild(first); viewport.AddChild(second);
        var joint = new PinJoint { NodeA = "../JointFirst", NodeB = "../JointSecond" }; viewport.AddChild(joint);
        using (var map = new PhysicsSnapshotMap(world.Space))
        {
            var jointExiting = false; var jointExited = false;
            joint.TreeExiting += _ => { map.Bind(1, 1, joint.GetRID()); Check(map.Unbind(1), "Joint remains attached through TreeExiting"); jointExiting = true; };
            joint.TreeExited += _ => { Reject<ArgumentException>(() => map.Bind(1, 1, joint.GetRID())); jointExited = true; };
            viewport.RemoveChild(joint); Check(jointExiting && jointExited, "Joint releases its world binding before TreeExited");
        }
        joint.Dispose(); second.Dispose(); first.Dispose();
        var guarded = new LifecycleNode(); viewport.AddChild(guarded); var afterExit = 0;
        Action<Node, Node> removed = (_, child) =>
        {
            if (!ReferenceEquals(child, guarded)) return;
            Reject<InvalidOperationException>(() => tree.PhysicsFrame(0));
            Reject<InvalidOperationException>(() => viewport.AddChild(guarded));
            Check(guarded.Parent is null, "Rejected intermediate reentry preserves committed removal");
        };
        Action<Node> exited = _ =>
        {
            afterExit++;
            Reject<InvalidOperationException>(() => tree.PhysicsFrame(0));
            Reject<InvalidOperationException>(() => viewport.AddChild(guarded));
            Reject<InvalidOperationException>(guarded.Dispose);
        };
        viewport.ChildRemoved += removed; guarded.TreeExited += exited;
        viewport.RemoveChild(guarded);
        Check(afterExit == 1 && guarded.Parent is null && guarded.Tree is null && !guarded.IsDisposed, "Exit barriers survive structural and after-exit callbacks");
        viewport.ChildRemoved -= removed; guarded.TreeExited -= exited;
        viewport.AddChild(guarded); Check(guarded.IsNodeReady, "Completed exit releases the reentry barrier"); guarded.Dispose();
    }

    private static void ArchiveLifecycle(PhysicsServer.Backend? backend, bool threaded)
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-lifecycle-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            using var root = new Node { Name = "Saved" }; using var disk = new CircleShape { Radius = 1 }; using var packed = new PackedScene();
            var body = new RigidBody { Name = "Body", GravityScale = 0, LinearVelocity = new(6, 0), CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace };
            body.AddChild(new CollisionShape { Name = "Shape", Shape = disk }); root.AddChild(body); body.Owner = root; body.GetChild(0).Owner = root;
            packed.Pack(root); var file = System.IO.Path.Combine(directory, "lifecycle.e2dscene"); ResourceSaver.Save(packed, file);
            var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(System.IO.Path.Combine(AppContext.BaseDirectory, System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name + ".dll"));
            start.ArgumentList.Add("--node-lifecycle-child"); start.ArgumentList.Add(file); if (threaded) start.ArgumentList.Add("--threaded"); if (backend is { } selected) start.ArgumentList.Add(selected == PhysicsServer.Backend.GPU ? "--gpu" : "--cpu");
            using var process = System.Diagnostics.Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(entireProcessTree: true); process.WaitForExit(); throw new InvalidOperationException("Fresh lifecycle consumer timed out."); }
            Task.WaitAll(output, errors);
            Check(process.ExitCode == 0 && output.Result.Contains("lifecycle archive passed"), $"Fresh lifecycle archive failed: {output.Result} {errors.Result}");
        }
        finally { Directory.Delete(directory, true); }
    }

    public static void ReadNodeLifecycle(string path, PhysicsServer.Backend? backend, bool threaded)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("Lifecycle", () => new Solver());
        using var packed = ResourceLoader.Load<PackedScene>(path); using var loaded = packed.Instantiate();
        var body = loaded.GetNode<RigidBody>("Body"); var shape = body.GetChild(0);
        Check(!loaded.IsNodeReady && !body.IsNodeReady && !shape.IsNodeReady, "Stored scene starts with unconsumed ready state");
        var order = new List<Node>(); loaded.Ready += order.Add; body.Ready += order.Add; shape.Ready += order.Add;
        using var world = backend is { } selected ? new World(selected) : new World("Lifecycle"); var root = new SubViewport { World = world }; root.AddChild(loaded); using var tree = new SceneTree(root);
        Check(order.SequenceEqual(new[] { shape, body, loaded }) && world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension), "Fresh file reconstructs child-first ready on the selected backend");
        var before = body.Position.X; var rid = body.GetRID(); for (var i = 0; i < 16; i++) tree.PhysicsFrame(1d / 60);
        Check(body.GetRID() == rid && Math.Abs(body.Position.X - before - 1.6f) < .01f, "Fresh lifecycle scene really simulates");
        Console.WriteLine("lifecycle archive passed");
    }

}
