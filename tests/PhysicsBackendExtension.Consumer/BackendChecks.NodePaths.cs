using Electron2D;

public static partial class BackendChecks
{
    public static void RunNodePaths(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("NodePaths", static () => new Solver());
        try
        {
            using (var failed = new PathObserver { Name = "Failed", FailEnter = true })
            {
                var child = new PathObserver { Name = "Child" }; failed.AddChild(child);
                Reject<AggregateException>(() => new SceneTree(failed));
                Check(failed.EnteredPath == "/Failed" && failed.GetPath() == "" && child.GetPath() == "",
                    "Failed activation clears cached paths while retaining the caller-owned hierarchy");
                failed.FailEnter = false;
                using var recovered = new SceneTree(failed);
                Check(child.GetPath() == "/Failed/Child", "A failed activation can be retried with fresh paths");
            }
            using var world = backend is { } selected ? new World(selected) : new World("NodePaths");
            using var shape = new CircleShape { Radius = 2 };
            using var root = new SubViewport { Name = "Root", World = world };
            var branch = new Entity { Name = "Branch" }; root.AddChild(branch);
            var moving = new PathBody
            {
                Name = "Moving",
                GravityScale = 0,
                CanSleep = false,
                CollisionLayer = 0,
                LinearVelocity = new(6, 0),
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            moving.AddChild(new CollisionShape { Name = "Shape", Shape = shape }); branch.AddChild(moving);
            moving.Owner = root; moving.UniqueNameInOwner = true;
            var observer = new PathObserver { Name = "Observer" }; branch.AddChild(observer);
            var internalNode = new Node { Name = "Internal" }; branch.AddChild(internalNode, internalMode: Node.InternalMode.Back);
            Check(moving.GetPath() == "" && moving.GetNodeOrNull("/Root/Branch/Moving") is null,
                "A detached hierarchy has relative paths but no absolute path");
            PathGrammar(root, moving);
            Check(root.GetNode("Branch/Internal") == internalNode && root.GetNode("%Moving") == moving,
                "Exact paths include internal and unowned branches");
            Check(moving.GetPathTo(moving) == "." && moving.GetNode(moving.GetPathTo(internalNode)) == internalNode &&
                observer.GetNode(observer.GetPathTo(moving, useUniquePath: true)) == moving,
                "Detached relative and owner-unique paths round-trip");
            using var unrelated = new Node();
            Reject<InvalidOperationException>(() => moving.GetPathTo(unrelated));
            Reject<ArgumentNullException>(() => moving.GetPathTo(null!));
            CollisionObject[] siblings = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new PhysicalBone(), new Area()];
            for (var i = 0; i < siblings.Length; i++)
            {
                siblings[i].Name = "Sibling" + i; siblings[i].CollisionLayer = 0; branch.AddChild(siblings[i]);
                Check(branch.GetNode<CollisionObject>(siblings[i].Name) == siblings[i], "Inherited paths return the original physical role");
            }
            var group = new PathGroup { Name = "Group", ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
            root.AddChild(group); group.AddChild(new Node { Name = "Leaf" });
            var foreign = new Node { Name = "Foreign", ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread };
            group.AddChild(foreign); foreign.AddChild(new Node { Name = "Leaf" });
            using var tree = new SceneTree(root); _ = root.FindWorld();
            Check(((SceneMultiplayer)tree.GetMultiplayer()).RootPath == "/Root",
                "Default multiplayer binds its absolute root before scene entry");
            Check(observer.EnteredPath == "/Root/Branch/Observer", "Entry callbacks see the committed active path");
            var saved = moving.GetPath();
            Check(saved == "/Root/Branch/Moving" && ReferenceEquals(saved, moving.GetPath()) && root.GetNode(saved) == moving,
                "An active path is a cached immutable string and round-trips from the root");
            PathGrammar(root, moving);
            branch.Name = "Renamed";
            Check(moving.GetPath() == "/Root/Renamed/Moving" && saved == "/Root/Branch/Moving" &&
                observer.RenamedPath == "/Root/Renamed/Observer", "Ancestor rename invalidates descendants before notifications");
            root.Name = "Scene";
            Check(moving.GetPath() == "/Scene/Renamed/Moving", "Root rename invalidates every cached absolute path");
            root.Name = "Root"; branch.Name = "Branch";
            moving.Renamed += FailRename;
            Reject<AggregateException>(() => moving.Name = "Changed");
            Check(moving.GetPath() == "/Root/Branch/Changed" && root.GetNode("%Changed") == moving && !root.HasNode("%Moving"),
                "A failed observer cannot retain an old path or unique name");
            moving.Renamed -= FailRename; moving.Name = "Moving";
            var other = new Node { Name = "Other" }; root.AddChild(other);
            branch.Reparent(other);
            Check(moving.GetPath() == "/Root/Other/Branch/Moving", "Reparent changes descendant paths");
            branch.Reparent(root);
            var reordered = moving.GetPath(); root.MoveChild(branch, 0);
            Check(ReferenceEquals(reordered, moving.GetPath()), "Sibling ordering does not change a path");
            root.RemoveChild(branch);
            Check(moving.GetPath() == "" && observer.ExitingPath == "/Root/Branch/Observer" &&
                observer.ExitedPath == "" && moving.GetNode("../Internal") == internalNode,
                "Exit callbacks retain active paths until membership ends; detached paths then clear");
            root.AddChild(branch); moving.Owner = root;
            Check(moving.GetPath() == "/Root/Branch/Moving" && root.GetNode("%Moving") == moving,
                "Reattachment restores current paths and unique ownership");
            observer.FailExit = true;
            Reject<AggregateException>(() => root.RemoveChild(branch));
            Check(branch.Parent is null && moving.GetPath() == "" && observer.GetPath() == "",
                "Failed exit callbacks still clear membership and cached paths");
            observer.FailExit = false; root.AddChild(branch); moving.Owner = root;
            foreach (var action in new Action[] { () => moving.GetPath(), () => moving.GetPathTo(root),
                () => moving.GetNode("."), () => moving.GetNodeOrNull("."), () => moving.HasNode(".") })
                Task.Run(() => Reject<InvalidOperationException>(action)).GetAwaiter().GetResult();
            for (var i = 0; i < 128; i++) Frame();
            var start = moving.Position.X; var callbacks = moving.Reads;
            Collect(); var ownerBytes = GC.GetAllocatedBytesForCurrentThread(); var allBytes = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes; allBytes = GC.GetTotalAllocatedBytes(true) - allBytes;
            Check(ownerBytes == 0 && allBytes == 0 && moving.Reads == callbacks + 128 && group.Reads == callbacks + 128 && moving.Position.X > start + 10,
                "Cached paths and exact lookups in 128 full moving physics frames allocate zero owner/all-thread bytes");
            var disposed = new Node(); disposed.Dispose();
            Reject<ObjectDisposedException>(() => disposed.GetPath()); Reject<ObjectDisposedException>(() => moving.GetPathTo(disposed));
            Reject<ObjectDisposedException>(() => disposed.GetPathTo(moving)); Reject<ObjectDisposedException>(() => disposed.GetNodeOrNull("."));
            Reject<ObjectDisposedException>(() => disposed.HasNode("."));
            Console.WriteLine($"Node paths {backend?.ToString() ?? "registered"}, threaded={threaded}: grammar, typed roles, cached lifecycle, failed callbacks, owner/group reads and 128 full physics frames {ownerBytes}/{allBytes} B.");
            void Frame()
            {
                Check(root.GetNode<PathBody>("Branch/Moving:Name") == moving && root.GetNode("%Moving") == moving &&
                    root.GetNode("/Root//Branch/Moving/") == moving && !root.HasNode("Branch/Missing") &&
                    root.GetNodeOrNull("") is null && moving.GetPath() == "/Root/Branch/Moving",
                    "Repeated exact, unique, absolute and failed lookups retain identities");
                tree.PhysicsFrame(1d / 60);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
        static void FailRename(Node _) => throw new ApplicationException("rename observer");
    }
    private static void PathGrammar(Node root, Node moving)
    {
        Check(moving.GetNode(".") == moving && moving.GetNode("./../Moving//Shape/") == moving.GetChild(0) &&
            moving.GetNode(":Name:unused:") == moving && moving.GetNode(".:") == moving,
            "Relative traversal ignores valid property subnames and accepts a final colon");
        foreach (var path in new[] { "", ":", " ", "/", "//", "missing", "../../..", "%", "/Wrong/Branch/Moving" })
        {
            Check(moving.GetNodeOrNull(path) is null && !moving.HasNode(path), "Unresolved node path: " + path);
            Reject<KeyNotFoundException>(() => moving.GetNode(path));
        }
        Reject<ArgumentNullException>(() => moving.GetNodeOrNull(null!));
        Reject<ArgumentException>(() => moving.GetNodeOrNull(".::Name"));
        Reject<InvalidCastException>(() => root.GetNode<Area>("Branch/Moving"));
    }
    private sealed class PathBody : RigidBody
    {
        internal int Reads;
        protected override void OnPhysicsProcess(double delta)
        {
            Check(GetPath() == "/Root/Branch/Moving" && GetNode("../../%Moving") == this && HasNode("Shape") &&
                GetNode<RigidBody>(".") == this && GetNodeOrNull("../Missing:ignored") is null,
                "Inherited path operations execute in a physical callback"); Reads++;
        }
    }
    private sealed class PathGroup : Node
    {
        internal int Reads;
        protected override void OnPhysicsProcess(double delta)
        {
            Check(GetPath() == "/Root/Group" && GetNode("Leaf") == GetChild(0) && GetNode("Foreign/Leaf") is not null &&
                GetNode("/Root/Branch/Moving") is RigidBody && HasNode(":Name"),
                "Current processing-group lookup can traverse immutable names across group boundaries");
            if (Reads == 0)
            {
                Check(GetPathTo(GetChild(0)) == "Leaf", "Relative path construction works in the current group");
                Reject<InvalidOperationException>(() => Tree!.Root.GetPath());
                Reject<InvalidOperationException>(() => Tree!.Root.GetNodeOrNull("Group"));
            }
            Reads++;
        }
    }
    private sealed class PathObserver : Node
    {
        internal string? EnteredPath, ExitingPath, ExitedPath, RenamedPath;
        internal bool FailExit, FailEnter;
        public PathObserver() => TreeExited += _ => ExitedPath = GetPath();
        protected override void OnEnterTree()
        {
            EnteredPath = GetPath();
            if (FailEnter) throw new ApplicationException("entry observer");
        }
        protected override void OnExitTree()
        {
            ExitingPath = GetPath();
            if (FailExit) throw new ApplicationException("exit observer");
        }
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationPathRenamed) RenamedPath = GetPath();
        }
    }
}
