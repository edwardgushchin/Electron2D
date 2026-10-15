using System.Runtime.CompilerServices;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunNodeOrphans(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("NodeOrphans", static () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("NodeOrphans");
            using var shape = new CircleShape { Radius = 2 };
            using var root = new SubViewport { Name = "OrphanRoot", World = world };
            var moving = new OrphanBody
            {
                Name = "Moving",
                GravityScale = 0,
                CanSleep = false,
                LinearVelocity = new(6, 0),
                CollisionLayer = 0,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            moving.AddChild(new CollisionShape { Name = "Geometry", Shape = shape }); root.AddChild(moving);
            Node[] siblings = [new Node(), new Entity(), new StaticBody(), new AnimatableBody(), new CharacterBody(), new PhysicalBone(), new Area()];
            for (var i = 0; i < siblings.Length; i++)
            {
                var node = siblings[i]; node.Name = "Role" + i; root.AddChild(node, internalMode: i == 0 ? Node.InternalMode.Front : Node.InternalMode.Disabled);
                if (node is CollisionObject collider) collider.CollisionLayer = 0;
            }
            var ids = new[] { root.InstanceID, moving.InstanceID, moving.GetChild(0).InstanceID }.Concat(siblings.Select(n => n.InstanceID)).ToArray();
            Membership(ids, true);
            var snapshot = Node.GetOrphanNodeIDs();
            Check(snapshot.SequenceEqual(snapshot.Order()) && snapshot.Distinct().Count() == snapshot.Length, "Diagnostic IDs are unique and ordered");
            if (OrphanDiagnosticsEnabled)
            {
                snapshot[0] = 0;
                Check(!Node.GetOrphanNodeIDs().Contains(0UL), "Caller array writes cannot alter the registry");
            }
            PrintCheck(root, moving);
            using var tree = new SceneTree(root);
            Membership(ids, false);
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
            var start = moving.Position.X; var callbacks = moving.Callbacks;
            Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && moving.Callbacks == callbacks + 128 && moving.Position.X > start + 10,
                "Tracking adds no allocation to 128 warmed complete physical frames");
            root.RemoveChild(moving); Membership([moving.InstanceID, moving.GetChild(0).InstanceID], true);
            PhysicsServer.BodySetSpace(moving.GetRID(), world.Space);
            Membership([moving.InstanceID, moving.GetChild(0).InstanceID], true);
            Check(PhysicsServer.BodyGetSpace(moving.GetRID()) == world.Space, "A live detached physics attachment is still a node orphan");
            root.AddChild(moving); Membership(ids, false);
            var detached = new Node { Name = "Disposed" }; var detachedID = detached.InstanceID;
            Membership([detachedID], true); var before = Node.GetOrphanNodeIDs(); detached.Dispose();
            Membership([detachedID], false);
            if (OrphanDiagnosticsEnabled) Check(before.Contains(detachedID), "Previous ID snapshots survive disposal");
            var failed = new OrphanDeleteFailure(); var failedID = failed.InstanceID;
            Reject<InvalidOperationException>(failed.Dispose); Membership([failedID], false);
            var weak = AbandonedOrphan(); var constructorWeak = FailedOrphanConstructor();
            Collect();
            Check(!weak.Reference.TryGetTarget(out _) && !constructorWeak.Reference.TryGetTarget(out _), "Diagnostic keys do not keep abandoned or failed-construction nodes alive");
            Membership([weak.ID, constructorWeak.ID], false);
            ConcurrentOrphans();
            tree.Dispose(); Membership(ids, false);
            GC.KeepAlive(detached); GC.KeepAlive(failed);
            Console.WriteLine($"Node orphan diagnostics enabled={OrphanDiagnosticsEnabled}, {backend?.ToString() ?? "registered"}, threaded={threaded}: roles/internal hierarchy, print/reentry, attachment/lifetime/weak/concurrent checks; 128 full frames {owner}/{all} B.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }

    private static bool OrphanDiagnosticsEnabled =>
#if DEBUG
        true;
#else
        false;
#endif

    private static void Membership(ulong[] ids, bool orphan)
    {
        var found = Node.GetOrphanNodeIDs();
        if (!OrphanDiagnosticsEnabled) Check(found.Length == 0, "Release diagnostics return an empty array");
        else foreach (var id in ids) Check(found.Contains(id) == orphan, "Node orphan membership follows live tree/lifetime state: " + id);
    }

    private static void PrintCheck(Node root, Node moving)
    {
        var output = Console.Out; using var capture = new StringWriter();
        using var logger = new OrphanLogger(); OS.AddLogger(logger);
        try
        {
            Console.SetOut(capture); Node.PrintOrphanNodes();
            if (OrphanDiagnosticsEnabled)
            {
                var row = $"{moving.InstanceID} - Stray Node: OrphanRoot/Moving (Type: OrphanBody)";
                Check(logger.Rows.Contains(row) && capture.ToString().Contains(row, StringComparison.Ordinal), "Print uses ordinary output plus typed logger with exact ID, path and CLR subclass");
                var back = new Node { Name = "Back" }; root.AddChild(back, internalMode: Node.InternalMode.Back);
                logger.Rows.Clear(); logger.First = () => back.Dispose(); Node.PrintOrphanNodes();
                Check(logger.Rows.Contains($"{back.InstanceID} - Stray Node: OrphanRoot/Back (Type: Node)"), "Log callbacks cannot invalidate already captured rows");
            }
            else Check(logger.Rows.Count == 0 && capture.ToString().Length == 0, "Release print emits nothing to either sink");
        }
        finally { Console.SetOut(output); OS.RemoveLogger(logger); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (ulong ID, WeakReference<Node> Reference) AbandonedOrphan()
    {
        var node = new Node(); Membership([node.InstanceID], true);
        return (node.InstanceID, new(node));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (ulong ID, WeakReference<Node> Reference) FailedOrphanConstructor()
    {
        Reject<InvalidOperationException>(() => _ = new OrphanConstructorFailure());
        var result = OrphanConstructorFailure.Created; OrphanConstructorFailure.Created = default;
        return result;
    }

    private static void ConcurrentOrphans()
    {
        var maker = Task.Run(() => { for (var i = 0; i < 256; i++) { using var node = new Node(); _ = Node.GetOrphanNodeIDs(); } });
        for (var i = 0; i < 256; i++)
        {
            var ids = Node.GetOrphanNodeIDs();
            Check(ids.SequenceEqual(ids.Order()) && ids.Distinct().Count() == ids.Length && !ids.Contains(0UL), "Concurrent diagnostics retain valid unique ordered identities");
        }
        maker.GetAwaiter().GetResult();
    }

    private sealed class OrphanLogger : Logger
    {
        internal readonly List<string> Rows = [];
        internal Action? First;
        protected override void OnLogMessage(string message, bool error)
        {
            Check(!error, "Orphan diagnostics use ordinary messages"); Rows.Add(message);
            var callback = First; First = null; callback?.Invoke();
        }
    }
    private sealed class OrphanBody : RigidBody
    {
        internal int Callbacks;
        protected override void OnPhysicsProcess(double delta)
        {
            if (Callbacks++ == 0) Membership([InstanceID], false);
        }
    }
    private sealed class OrphanDeleteFailure : Node
    {
        protected override void OnNotification(int what)
        {
            if (what == NotificationPreDelete) throw new InvalidOperationException("Expected pre-delete failure");
            base.OnNotification(what);
        }
    }
    private sealed class OrphanConstructorFailure : Node
    {
        internal static (ulong ID, WeakReference<Node> Reference) Created;
        internal OrphanConstructorFailure()
        {
            Created = (InstanceID, new(this)); throw new InvalidOperationException("Expected constructor failure");
        }
    }
}
