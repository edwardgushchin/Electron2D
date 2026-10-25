using System.Diagnostics;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunProcessGroups(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var workers = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("ProcessGroups", () => new Solver());
        try { ProcessGroupRoles(backend); ProcessGroups(backend); }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, workers); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }
    private static void ProcessGroups(PhysicsServer.Backend? backend)
    {
        using var world = backend is { } selected ? new World(selected) : new World("ProcessGroups");
        using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 2 };
        var body = new RigidBody
        {
            Name = "Body",
            GravityScale = 0,
            CanSleep = false,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace
        };
        body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        var a = new GroupProbe { Name = "A", ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        var b = new GroupProbe { Name = "B", ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        var gather = new GroupProbe { Name = "Gather", ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread, ProcessThreadGroupOrder = -1 };
        var inherited = new GroupProbe { Name = "Inherited" }; a.AddChild(inherited);
        root.AddChild(a); root.AddChild(b); root.AddChild(gather);
        using var tree = new SceneTree(root); var owner = Environment.CurrentManagedThreadId;
        using var startedA = new ManualResetEventSlim(); using var startedB = new ManualResetEventSlim();
        var outputA = 0; var outputB = 0; var threadA = 0; var threadB = 0; var childThread = 0; var delivered = 0; var gatherCount = 0;
        a.Signal = (node, physics) =>
        {
            threadA = Environment.CurrentManagedThreadId; startedA.Set();
            Check(startedB.Wait(TimeSpan.FromSeconds(5)), "Independent same-order subgroups execute concurrently"); outputA = 2;
            Check(node.PhysicsProcessDeltaTime == .01 && threadA != owner, "Group callbacks observe the actual lane delta on a worker");
            Reject<InvalidOperationException>(() => _ = gather.PhysicsProcessDeltaTime);
            Reject<InvalidOperationException>(() => gather.PhysicsProcessEnabled = false);
            Reject<InvalidOperationException>(() => node.ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread);
            node.ProcessPriority = 1; node.PhysicsProcessPriority = -1;
            node.ProcessEnabled = false; node.ProcessEnabled = true;
            node.PhysicsProcessEnabled = false; node.PhysicsProcessEnabled = true;
            node.InternalProcessEnabled = true; node.InternalProcessEnabled = false;
            node.InternalPhysicsProcessEnabled = true; node.InternalPhysicsProcessEnabled = false;
            Check(node.ProcessPriority == 1 && node.PhysicsProcessPriority == -1 && node.ProcessEnabled && node.PhysicsProcessEnabled &&
                !node.InternalProcessEnabled && !node.InternalPhysicsProcessEnabled, "All six processing controls retain current-group read/write access");
            node.CallThreadSafe<GroupProbe>(_ => delivered++);
            gather.CallDeferredThreadGroup<GroupProbe>(_ => delivered++);
        };
        b.Signal = (_, _) => { threadB = Environment.CurrentManagedThreadId; startedB.Set(); Check(startedA.Wait(TimeSpan.FromSeconds(5)), "The second group joins parallel work"); outputB = 4; };
        inherited.Signal = (_, _) => { childThread = Environment.CurrentManagedThreadId; Check(outputA == 2, "Inherited members follow their group owner's tree order"); };
        gather.Signal = (_, physics) =>
        {
            gatherCount++; Check(Environment.CurrentManagedThreadId == owner && outputA == 2 && outputB == 4 && delivered == 2,
                "Equal-order main groups execute only after all subgroups and pre-group messages complete");
            if (physics) body.LinearVelocity = new(outputA + outputB, 0);
        };
        tree.PhysicsFrame(.01);
        Check(threadA != threadB && childThread == threadA && gatherCount == 1 && MathF.Abs(body.Position.X - .06f) < .00001f,
            "Inherited subgroup execution and main collection precede one actual selected physical step");

        a.Signal = null; b.Signal = null; inherited.Signal = null; gather.Signal = null;
        a.ProcessEnabled = a.PhysicsProcessEnabled = b.ProcessEnabled = b.PhysicsProcessEnabled = false;
        inherited.ProcessEnabled = inherited.PhysicsProcessEnabled = gather.ProcessEnabled = gather.PhysicsProcessEnabled = false;
        a.ProcessThreadMessages = Node.ProcessThreadMessageFlags.Physics; var messages = 0; var messageThread = 0;
        a.CallDeferredThreadGroup<GroupProbe>(_ => { messages++; messageThread = Environment.CurrentManagedThreadId; });
        tree.ProcessFrame(.01); Check(messages == 0, "An empty explicit group does not drain in a masked-out lane");
        tree.PhysicsFrame(.01); Check(messages == 1 && messageThread != owner, "A physics message mask runs an otherwise inactive subgroup");
        a.ProcessThreadMessages = 0; a.ProcessEnabled = true;
        a.CallDeferredThreadGroup<GroupProbe>(_ => messages++); tree.ProcessFrame(.01);
        Check(messages == 2, "Active callbacks service messages independently of the empty-group mask");
        var errors = new InvalidOperationException("Expected subgroup failure"); a.Signal = (_, _) => throw errors;
        b.ProcessEnabled = true; var completed = 0; b.Signal = (_, _) => completed++;
        Check(CaptureCopy(() => tree.ProcessFrame(.01)) is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(errors) && completed == 1,
            "A failed subgroup still joins every peer before publishing frame errors");
        a.Signal = null; b.Signal = null;
        Reject<ArgumentOutOfRangeException>(() => a.ProcessThreadGroup = (Node.ProcessThreadGroupMode)3);
        Reject<ArgumentOutOfRangeException>(() => a.ProcessThreadMessages = (Node.ProcessThreadMessageFlags)4);
        Check(a.GetPropertyList().Where(p => p.Name is nameof(Node.ProcessThreadGroup) or nameof(Node.ProcessThreadGroupOrder) or nameof(Node.ProcessThreadMessages)).All(p => p.IsStored),
            "The complete group settings family is portable stored state");
        GroupSettingsArchive(backend);

        var inheritedCalls = 0; var inheritedOwner = 0;
        a.Signal = null; b.Signal = null; gather.Signal = null;
        inherited.ProcessEnabled = true;
        inherited.Signal = (_, _) => { inheritedCalls++; inheritedOwner = Environment.CurrentManagedThreadId; };
        a.ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread;
        tree.ProcessFrame(.01);
        Check(inheritedOwner == owner, "A configured boundary rebinds inherited descendants to its new execution owner");
        a.ProcessThreadGroup = Node.ProcessThreadGroupMode.Inherit;
        tree.ProcessFrame(.01); Check(inheritedOwner == owner, "Removing a boundary returns inherited descendants to the parent group");
        a.ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread;
        tree.ProcessFrame(.01); Check(inheritedOwner != owner && inheritedCalls == 3, "Restoring a subgroup creates one fresh execution identity");
        a.ProcessThreadMessages = Node.ProcessThreadMessageFlags.Process;
        var stale = 0; a.CallDeferredThreadGroup<GroupProbe>(_ => stale++);
        a.ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread;
        tree.ProcessFrame(.01); Check(stale == 0, "Retiring a group discards its pending queue without replaying it in the replacement");
        a.ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread;

        a.PhysicsProcessEnabled = b.PhysicsProcessEnabled = inherited.PhysicsProcessEnabled = gather.PhysicsProcessEnabled = true;
        a.Signal = (_, physics) => { outputA = 2; if (physics) gather.CallDeferredThreadGroup<GroupProbe>(static _ => { }); };
        b.Signal = (_, _) => outputB = 4; inherited.Signal = null;
        gather.Signal = (_, physics) => { if (physics) body.LinearVelocity = new(outputA + outputB, 0); };
        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        for (var i = 0; i < 128; i++) Frame();
        Collect(); var current = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame();
        current = GC.GetAllocatedBytesForCurrentThread() - current; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(current == 0 && all == 0, $"Actual subgroup/barrier/message/physics/query cycles allocate {current}/{all} B");
        Console.WriteLine($"Process groups {backend?.ToString() ?? "registered"}, solverThreaded={ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread)}: parallel workers, inherited order, main barrier, masks, failures, stored settings and 128 full frames/queries {current}/{all} B passed.");

        void Frame()
        {
            var x = body.Position.X; tree.PhysicsFrame(.01); query.Position = body.Position;
            Check(MathF.Abs(body.Position.X - x - .06f) < .0001f && world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == body.GetRID(),
                "Main collection of worker results preserves real selected physical motion and query identity");
        }
    }
    private sealed class GroupProbe : Node
    {
        internal Action<GroupProbe, bool>? Signal;
        protected override void OnProcess(double delta) => Signal?.Invoke(this, false);
        protected override void OnPhysicsProcess(double delta) => Signal?.Invoke(this, true);
    }

    private static void GroupSettingsArchive(PhysicsServer.Backend? backend)
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-process-groups-" + Guid.NewGuid());
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            using var root = new Node
            {
                Name = "StoredGroups",
                ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread,
                ProcessThreadGroupOrder = -7,
                ProcessThreadMessages = Node.ProcessThreadMessageFlags.Physics
            };
            var child = new Node
            {
                Name = "Main",
                ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread,
                ProcessThreadGroupOrder = 3,
                ProcessThreadMessages = Node.ProcessThreadMessageFlags.All
            };
            root.AddChild(child); child.Owner = root;
            using var packed = new PackedScene(); packed.Pack(root);
            using var copy = packed.Instantiate(); CheckGroupSettings(copy);
            var path = System.IO.Path.Combine(directory, "groups.e2dscene"); ResourceSaver.Save(packed, path);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase))
                start.ArgumentList.Add(System.IO.Path.Combine(AppContext.BaseDirectory, System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name + ".dll"));
            start.ArgumentList.Add(backend?.ToString().ToLowerInvariant() ?? "registered");
            start.ArgumentList.Add("--process-groups-child"); start.ArgumentList.Add(path);
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            process.WaitForExit(); Check(process.ExitCode == 0, "Fresh-process group settings load failed: " + output.Result + errors.Result);
        }
        finally { System.IO.Directory.Delete(directory, recursive: true); }
    }
    public static void ReadGroupSettings(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
        using var root = packed.Instantiate(); CheckGroupSettings(root);
        Console.WriteLine("Fresh process restored both group boundaries, signed order and masks.");
    }
    private static void CheckGroupSettings(Node root)
    {
        var child = root.GetNode("Main");
        Check(root.ProcessThreadGroup == Node.ProcessThreadGroupMode.SubThread && root.ProcessThreadGroupOrder == -7 &&
            root.ProcessThreadMessages == Node.ProcessThreadMessageFlags.Physics && child.ProcessThreadGroup == Node.ProcessThreadGroupMode.MainThread &&
            child.ProcessThreadGroupOrder == 3 && child.ProcessThreadMessages == Node.ProcessThreadMessageFlags.All,
            "Packed and persisted settings preserve the full group family and independent nested boundaries");
    }
}
