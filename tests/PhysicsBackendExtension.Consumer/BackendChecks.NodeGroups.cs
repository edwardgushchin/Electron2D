using System.Diagnostics;
using Electron2D;

public static partial class BackendChecks
{
    private sealed class GroupMutationBody : RigidBody
    {
        protected override void ValidateMutation() { }
    }
    public static void RunNodeGroups(PhysicsServer.Backend? backend = null, bool threaded = false, Func<long>? samplePool = null)
    {
        using (var source = new Node())
        using (var packed = new PackedScene())
        {
            source.AddToGroup("runtime"); source.AddToGroup("runtime", persistent: true);
            source.AddToGroup("persistent", persistent: true); source.AddToGroup("persistent");
            packed.Pack(source); Check(packed.GetState().GetNodeGroups(0).SequenceEqual(new[] { "persistent" }),
                "Repeated group addition must retain the first persistence choice");
            Check(source.RemoveFromGroup("runtime"), "Existing runtime group removes"); source.AddToGroup("runtime", true);
            packed.Pack(source); Check(packed.GetState().GetNodeGroups(0).Count == 2, "Remove/readd can select new persistence");
            var snapshot = source.GetGroups(); source.AddToGroup("later"); Check(!snapshot.Contains("later"), "Group results are independent snapshots");
            source.AddToGroup(" \t ", true); source.AddToGroup("_reserved", true); source.AddToGroup("Группа", true); source.AddToGroup("GROUP"); source.AddToGroup("group");
            Check(source.IsInGroup(" \t ") && source.IsInGroup("GROUP") && source.IsInGroup("group") && !source.IsInGroup("Group"), "Identifiers preserve whitespace, Unicode and case");
            Check(!source.IsInGroup("") && !source.RemoveFromGroup(""), "Empty lookup/removal finds no membership");
            Reject<ArgumentException>(() => source.AddToGroup("")); Reject<ArgumentNullException>(() => source.AddToGroup(null!));
            Reject<ArgumentNullException>(() => source.IsInGroup(null!)); Reject<ArgumentNullException>(() => source.RemoveFromGroup(null!));
        }
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread); var workers = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded); ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2);
        var providers = new List<Solver>();
        if (backend is null) PhysicsServerManager.RegisterServer("NodeGroups", () => { var solver = new Solver { CountSolveAllocations = true }; providers.Add(solver); return solver; });
        try
        {
            using var first = Selected(); using var second = Selected(); using var disk = new CircleShape { Radius = 1 };
            var root = new SubViewport { World = first }; var otherRoot = new SubViewport { World = second };
            var mover = new RigidBody
            {
                Name = "Mover",
                LinearVelocity = new(6, 0),
                GravityScale = 0,
                CanSleep = false,
                CollisionLayer = 0,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            var geometry = new CollisionShape { Name = "Shape", Shape = disk }; mover.AddChild(geometry); root.AddChild(mover);
            var sensor = new Area { Name = "Sensor", Position = new(0, 100) }; root.AddChild(sensor);
            var internalNode = new Node { Name = "Internal" }; root.AddChild(internalNode, internalMode: Node.InternalMode.Front);
            foreach (var node in new Node[] { mover, geometry, sensor, internalNode }) node.AddToGroup("actors");
            using var tree = new SceneTree(root); using var other = new SceneTree(otherRoot);
            var original = tree.GetNodesInGroup("actors");
            Check(original.SequenceEqual(new Node[] { internalNode, mover, geometry, sensor }) && tree.GetFirstNodeInGroup("actors") == internalNode && tree.GetNodeCountInGroup("actors") == 4,
                "Tree group selection includes internal nodes and exact parent-first order");
            Check(tree.GetNodesInGroup("").Count == 0 && !tree.HasGroup("missing"), "Absent/empty tree group queries are empty");
            Exception? foreignError = null;
            var foreign = new Thread(() =>
            {
                try
                {
                    Reject<InvalidOperationException>(() => mover.IsInGroup("actors")); Reject<InvalidOperationException>(() => mover.GetGroups());
                    Reject<InvalidOperationException>(() => mover.AddToGroup("foreign")); Reject<InvalidOperationException>(() => mover.RemoveFromGroup("actors"));
                }
                catch (Exception error) { foreignError = error; }
            });
            foreign.Start(); foreign.Join(); if (foreignError is not null) throw foreignError;
            var calls = new List<Node>();
            tree.CallGroup("actors", node => { calls.Add(node); if (node == internalNode) mover.RemoveFromGroup("actors"); });
            Check(calls.SequenceEqual(new Node[] { internalNode, geometry, sensor }), "A group call revalidates removed membership before each target");
            mover.AddToGroup("actors"); calls.Clear(); tree.CallGroup("actors", calls.Add, GroupCallFlags.Reverse);
            Check(calls.SequenceEqual(new Node[] { sensor, geometry, mover, internalNode }), "Reverse group calls traverse the selected hierarchy in reverse");
            tree.CallGroup("actors", calls.Add, GroupCallFlags.Deferred); mover.Reparent(otherRoot);
            Check(tree.GetNodeCountInGroup("actors") == 2 && other.GetNodeCountInGroup("actors") == 2 && original.Count == 4 && original[1] == mover,
                "Cross-tree transfer moves live membership while preserving old snapshots and node identity");
            calls.Clear(); tree.FlushDeferred(); Check(calls.SequenceEqual(new Node[] { internalNode, sensor }), "Deferred group operations resolve current tree membership");
            var owner = Environment.CurrentManagedThreadId; var workerRan = false;
            var worker = new GroupProbe { Name = "Worker", ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread }; var leaf = new Node { Name = "Leaf" };
            worker.AddToGroup("worker"); leaf.AddToGroup("worker"); worker.AddChild(leaf);
            var workerBody = new GroupMutationBody { Position = new(-100, -100), GravityScale = 0 }; workerBody.AddChild(new CollisionShape { Shape = disk }); worker.AddChild(workerBody);
            root.AddChild(worker);
            worker.Signal = (_, physics) =>
            {
                Check(physics && worker.IsInGroup("worker") && leaf.IsInGroup("worker"), "Current processing group can read its own memberships");
                worker.AddToGroup("temporary"); Check(worker.RemoveFromGroup("temporary"), "Worker can edit its own nonphysical membership");
                workerBody.AddToGroup("temporary"); Check(workerBody.IsInGroup("temporary") && workerBody.RemoveFromGroup("temporary"), "Worker can edit group metadata of its own physical nodes");
                if (workerRan) return;
                Check(Environment.CurrentManagedThreadId != owner && worker.GetGroups().Contains("worker"), "Worker uses actual group-owned access");
                Reject<InvalidOperationException>(() => mover.IsInGroup("actors")); Reject<InvalidOperationException>(() => mover.GetGroups());
                Reject<InvalidOperationException>(() => mover.AddToGroup("bad"));
                workerBody.Position = workerBody.Position; workerRan = true;
            };
            long workerBytes = 0;
            var injectAllocation = false; byte[]? allocationSink = null;
            Action allocate = () => allocationSink = new byte[8];
            var callback = worker.Signal;
            worker.Signal = (node, physics) =>
            {
                var allocated = GC.GetAllocatedBytesForCurrentThread();
                try { callback!(node, physics); if (injectAllocation) allocate(); }
                finally { Interlocked.Add(ref workerBytes, GC.GetAllocatedBytesForCurrentThread() - allocated); }
            };
            worker.PhysicsProcessEnabled = true; tree.PhysicsFrame(1d / 60); Check(workerRan, "Worker frame executes");
            if (providers.Count != 0)
            {
                var originalPose = workerBody.Position;
                foreach (var provider in providers) provider.StatusHook = () => Reject<InvalidOperationException>(() => workerBody.Position = originalPose + new Vector2(1, 0));
                using var parameters = new PhysicsPointQueryParameters { Position = originalPose };
                try { Check(first.DirectSpaceState.IntersectPoint(parameters, new PhysicsPointResult[2]) == 1, "Guarded provider query retains actual original geometry"); }
                finally { foreach (var provider in providers) provider.StatusHook = null; }
                Check(workerBody.Position == originalPose, "A user validation override cannot bypass the nonvirtual physical authoring guard");
            }
            var forceCount = 0; PhysicsServer.BodySetForceIntegrationCallback(mover.GetRID(), _ => forceCount++);
            for (var i = 0; i < 128; i++) Frame();
            workerBody.Sleeping = true; Frame();
            var start = mover.Position.X; var initial = forceCount; var rid = mover.GetRID();
            workerBytes = 0; foreach (var provider in providers) provider.SolveAllocatedBytes = 0;
            Collect();
            var poolStart = samplePool?.Invoke() ?? 0;
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            var poolBytes = samplePool is null ? 0 : samplePool() - poolStart;
            var callbackBytes = workerBytes;
            var solver0 = providers.Count > 0 ? providers[0].SolveAllocatedBytes : 0;
            var solver1 = providers.Count > 1 ? providers[1].SolveAllocatedBytes : 0;
            var unattributed = all - own - (samplePool is null ? callbackBytes : poolBytes) - (threaded ? solver0 + solver1 : 0);
            Check(workerBody.Sleeping, "Group metadata changes do not wake a sleeping physical body");
            Check(own == 0 && all == 0 && callbackBytes == 0 && solver0 == 0 && solver1 == 0 && Math.Abs(mover.Position.X - start - 12.8f) < .01f && mover.GetRID() == rid && forceCount == initial + 128,
                $"Full group/worker/two-world frames retain motion, RID and zero allocation: {own}/{all} B; callback={callbackBytes}, solver0={solver0}, solver1={solver1}, pool={poolBytes} (available={samplePool is not null}), unattributed={unattributed} B (serial solver samples overlap owner)");
            workerBytes = 0; foreach (var provider in providers) { provider.SolveAllocatedBytes = 0; provider.StepHook = allocate; }
            var calibrationOwner = GC.GetAllocatedBytesForCurrentThread(); allocate();
            calibrationOwner = GC.GetAllocatedBytesForCurrentThread() - calibrationOwner;
            injectAllocation = true; Frame(); injectAllocation = false;
            Check(calibrationOwner == 32 && workerBytes == 32 && providers.All(provider => provider.SolveAllocatedBytes == 32),
                "Owner, actual group callback and each registered numerical lane observe an injected 32-byte array exactly");
            foreach (var provider in providers) provider.StepHook = null;
            GC.KeepAlive(allocationSink);
            Console.WriteLine($"Allocation calibration owner/callback/registered-solvers: 32/32/{providers.Count}x32 B; warm attribution callback={callbackBytes}, solver0={solver0}, solver1={solver1}, pool={poolBytes} (available={samplePool is not null}), unattributed={unattributed} B; built-in solver allocation is not sampled separately.");
            ArchiveGroups(backend, threaded);
            otherRoot.RemoveChild(mover); Check(mover.IsInGroup("actors") && other.GetNodeCountInGroup("actors") == 0, "Detached nodes retain local groups and leave tree queries");
            var detached = new Thread(() => { mover.AddToGroup("detached"); Check(mover.GetGroups().Contains("detached"), "Detached caller coordination permits group access"); }); detached.Start(); detached.Join();
            mover.Dispose(); Reject<ObjectDisposedException>(() => mover.GetGroups()); Reject<ObjectDisposedException>(() => mover.IsInGroup("actors"));
            Console.WriteLine($"Node groups {backend}, threaded={threaded}: persistence, snapshots, identifiers, tree transfer, worker access, fresh file; 128 groups of two real steps {own}/{all} B.");
            void Frame()
            {
                mover.AddToGroup("actors", true); Check(mover.IsInGroup("actors"), "Repeated live membership remains present");
                mover.AddToGroup("temporary"); Check(mover.RemoveFromGroup("temporary") && !mover.IsInGroup("temporary"), "Prepared membership churn is exact");
                tree.PhysicsFrame(1d / 60); other.PhysicsFrame(1d / 60);
            }
            World Selected()
            {
                var world = backend is { } selected ? new World(selected) : new World("NodeGroups");
                Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension), "Explicit actual backend is retained"); return world;
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, workers); }
    }

    private static void ArchiveGroups(PhysicsServer.Backend? backend, bool threaded)
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-node-groups-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            using var root = new Node { Name = "Saved" }; using var disk = new CircleShape { Radius = 1 }; using var packed = new PackedScene();
            var body = new RigidBody { Name = "Body", GravityScale = 0, LinearVelocity = new(6, 0), CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, AngularDampMode = RigidBody.DampMode.Replace };
            body.AddChild(new CollisionShape { Name = "Shape", Shape = disk }); root.AddChild(body); body.Owner = root; body.GetChild(0).Owner = root;
            foreach (var node in new Node[] { root, body, body.GetChild(0) })
            { node.AddToGroup("runtime"); node.AddToGroup("runtime", true); node.AddToGroup(" \t ", true); node.AddToGroup("Группа", true); node.AddToGroup("_reserved", true); }
            packed.Pack(root); Check(packed.CanInstantiate(), "Physical hierarchy packs"); var file = System.IO.Path.Combine(directory, "groups.e2dscene"); ResourceSaver.Save(packed, file);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(System.IO.Path.Combine(AppContext.BaseDirectory, System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name + ".dll"));
            start.ArgumentList.Add("--node-groups-child"); start.ArgumentList.Add(file); if (threaded) start.ArgumentList.Add("--threaded"); if (backend is { } selected) start.ArgumentList.Add(selected == PhysicsServer.Backend.GPU ? "--gpu" : "--cpu");
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(entireProcessTree: true); process.WaitForExit(); throw new InvalidOperationException("Fresh group consumer timed out."); }
            Task.WaitAll(output, errors);
            Check(process.ExitCode == 0 && output.Result.Contains("group archive passed"), $"Fresh group archive failed: {output.Result} {errors.Result}");
        }
        finally { Directory.Delete(directory, true); }
    }

    public static void ReadNodeGroups(string path, PhysicsServer.Backend? backend, bool threaded)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("NodeGroups", () => new Solver());
        using var packed = ResourceLoader.Load<PackedScene>(path); using var loaded = packed.Instantiate();
        var expected = new[] { " \t ", "_reserved", "Группа" };
        foreach (var node in new[] { loaded, loaded.GetNode("Body"), loaded.GetNode("Body/Shape") }) Check(node.GetGroups().SequenceEqual(expected) && !node.IsInGroup("runtime"), "Only first-persistent identifiers round-trip exactly");
        using var world = backend is { } selected ? new World(selected) : new World("NodeGroups"); var root = new SubViewport { World = world }; root.AddChild(loaded); using var tree = new SceneTree(root);
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && tree.GetNodeCountInGroup(" \t ") == 3, "Fresh physical instance retains exact groups and backend");
        var body = loaded.GetNode<RigidBody>("Body"); var before = body.Position.X; var rid = body.GetRID(); for (var i = 0; i < 16; i++) tree.PhysicsFrame(1d / 60);
        Check(body.GetRID() == rid && Math.Abs(body.Position.X - before - 1.6f) < .01f, "Fresh physical instance really simulates");
        Console.WriteLine("group archive passed");
    }
}
