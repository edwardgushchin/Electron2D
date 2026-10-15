using Electron2D;

public static partial class BackendChecks
{
    public static void RunNodeSearch(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("NodeSearch", static () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("NodeSearch");
            using var shape = new CircleShape { Radius = 2 };
            using var root = new SubViewport { Name = "Root", World = world };
            var runtime = new Node { Name = "Runtime" }; root.AddChild(runtime);
            var hidden = new RigidBody { Name = "TargetHidden", CollisionLayer = 0 }; runtime.AddChild(hidden); hidden.Owner = root;
            var branch = new Entity { Name = "Branch" }; root.AddChild(branch); branch.Owner = root;
            var moving = new SearchBody
            {
                Name = "TargetMoving",
                GravityScale = 0,
                CanSleep = false,
                LinearVelocity = new(6, 0),
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace,
                CollisionLayer = 0
            };
            moving.AddChild(new CollisionShape { Shape = shape }); branch.AddChild(moving); moving.Owner = branch;
            var last = new Node { Name = "TargetLast" }; root.AddChild(last); last.Owner = root;
            var front = new Node { Name = "TargetFront" }; root.AddChild(front, internalMode: Node.InternalMode.Front); front.Owner = root;
            var back = new Node { Name = "TargetBack" }; root.AddChild(back, internalMode: Node.InternalMode.Back); back.Owner = root;
            var spatial = new Entity { Name = "Spatial" }; root.AddChild(spatial); spatial.Owner = root;
            CollisionObject[] siblings = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new PhysicalBone(), new Area()];
            for (var i = 0; i < siblings.Length; i++)
            {
                siblings[i].Name = "Sibling" + i; siblings[i].CollisionLayer = 0;
                root.AddChild(siblings[i]); siblings[i].Owner = root;
            }
            Check(root.FindChild("*") == front, "Default search includes owned internal-front children");
            Check(root.FindChild("TargetHidden") is null && root.FindChild<RigidBody>("TargetHidden") is null,
                "Default ownership filtering prunes the whole unowned runtime branch");
            Check(root.FindChild("TargetHidden", owned: false) == hidden && root.FindChild<RigidBody>("TargetHidden", owned: false) == hidden,
                "Explicit unowned search reaches an owned body below an unowned branch");
            Check(root.FindChild("TargetMoving") == moving && root.FindChild("TargetMoving", recursive: false) is null,
                "Recursive search walks through a nonmatching spatial parent");
            Check(root.FindChildren("Target*").SequenceEqual(new Node[] { front, moving, last, back }) &&
                root.FindChildren("Target*", recursive: false).SequenceEqual(new Node[] { front, last, back }),
                "All-results snapshots preserve internal and depth-first scene order");
            Check(root.FindChildren<RigidBody>("").SequenceEqual(new RigidBody[] { moving, (PhysicalBone)siblings[3] }) &&
                root.FindChildren<CollisionObject>("").SequenceEqual(new CollisionObject[] { moving }.Concat(siblings)),
                "Typed empty-pattern search includes inherited body/Area roles and compiled subclasses");
            Check(moving.FindParent("B*") == branch && moving.FindParent("Root") == root && moving.FindParent("b*") is null &&
                moving.FindParent("") is null && root.FindParent("*") is null, "Ancestor search starts at the parent and is case-sensitive");
            Check(root.FindChild(" ") is null && root.FindChildren(" ").Count == 0, "Whitespace is a valid unmatched pattern");
            Reject<ArgumentNullException>(() => root.FindChild(null!)); Reject<ArgumentException>(() => root.FindChild(""));
            Reject<ArgumentNullException>(() => root.FindChildren(null!)); Reject<ArgumentException>(() => root.FindChildren(""));
            Reject<ArgumentNullException>(() => root.FindChildren<Node>(null!)); Reject<ArgumentNullException>(() => root.FindParent(null!));
            var saved = root.FindChildren("Target*"); last.Name = "Renamed"; root.RemoveChild(back); back.Dispose();
            Check(saved.Count == 4 && ReferenceEquals(saved[2], last) && ReferenceEquals(saved[3], back) &&
                root.FindChildren("Target*").Count == 2, "A caller-owned result snapshot preserves borrowed identities across edits");
            branch.Owner = null; Check(root.FindChild("TargetMoving") is null, "Owner removal changes branch eligibility immediately");
            branch.Owner = root; Check(root.FindChild("TargetMoving") == moving, "Owner reassignment restores traversal");
            SearchPatterns();
            var group = new SearchGroup { Name = "SearchGroup", ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
            root.AddChild(group); group.Owner = root;
            var groupedChild = new Node { Name = "GroupedChild" }; group.AddChild(groupedChild); groupedChild.Owner = group;
            var foreign = new Node { Name = "Foreign", ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread };
            group.AddChild(foreign); foreign.Owner = group;
            var foreignChild = new Node { Name = "ForeignLeaf" }; foreign.AddChild(foreignChild); foreignChild.Owner = foreign;
            using var tree = new SceneTree(root); _ = root.FindWorld();
            foreach (var action in new Action[] { () => root.FindChild("*"), () => root.FindChildren("*"), () => moving.FindParent("*") })
                Task.Run(() => Reject<InvalidOperationException>(action)).GetAwaiter().GetResult();
            for (var i = 0; i < 128; i++) Frame();
            var start = moving.Position.X; var callbacks = moving.Searches;
            Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && moving.Searches == callbacks + 128 && group.Searches == callbacks + 128 && moving.Position.X > start + 10,
                "128 full physics/search frames execute actual motion and callbacks with zero owner/all-thread allocation");
            var disposed = new Node(); disposed.Dispose();
            Reject<ObjectDisposedException>(() => disposed.FindChild("*")); Reject<ObjectDisposedException>(() => disposed.FindChildren("*"));
            Reject<ObjectDisposedException>(() => disposed.FindParent("*"));
            Console.WriteLine($"Node search {backend?.ToString() ?? "registered"}, threaded={threaded}: owner pruning, internal order, typed roles, Unicode patterns, current/foreign processing groups, errors, live callbacks and 128 full frames {owner}/{all} B.");
            void Frame()
            {
                Check(root.FindChild<SearchBody>("TargetMoving") == moving && moving.FindParent("R*") == root,
                    "Repeated reads preserve borrowed identities");
                tree.PhysicsFrame(1d / 60);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }
    private static void SearchPatterns()
    {
        using var root = new Node(); var item = new Node(); root.AddChild(item);
        (string Name, string Pattern, bool Match)[] cases =
        [
            ("A.Z", "A?Z", false), ("A.Z", "A*Z", true), ("A.Z", "A.Z", true),
            ("A😀Z", "A?Z", true), ("A😀Z", "A??Z", false), ("A😀Z", "A*?Z", true),
            ("A😀Z", "A?*Z", true), ("A😀Z", "*😀?", true), ("A😀Z", "*?😀Z", true),
            ("AbcdZ", "A*d?Z", false), ("AbcdZ", "A*c?Z", true), ("AbcdZ", "**A***Z**", true),
            ("AbcdZ", "*X", false), ("AbcdZ", "a*", false), ("Éclair", "é*", false),
            ("AéZ", "A?Z", true), ("A..Z", "A??Z", false), ("A..bZ", "A*?Z", true),
            ("AZ", "A*Z", true), ("AZ", "A?Z", false)
        ];
        foreach (var (name, pattern, match) in cases)
        {
            item.Name = name;
            Check((root.FindChild(pattern, owned: false) == item) == match &&
                (root.FindChildren<Node>(pattern, owned: false).Count == 1) == match,
                $"Wildcard name={name}, pattern={pattern}, expected={match}");
        }
    }
    private sealed class SearchGroup : Node
    {
        internal int Searches;
        protected override void OnPhysicsProcess(double delta)
        {
            Check(FindChild("GroupedChild") == GetChild(0) && FindParent("Root") == Tree!.Root,
                "A processing worker can search its own hierarchy and ancestors");
            if (Searches == 0)
            {
                Check(FindChildren<Node>("", recursive: false).Count == 2, "Typed direct results execute within the current processing group");
                Reject<InvalidOperationException>(() => FindChild("Absent"));
                Reject<InvalidOperationException>(() => FindChildren("*"));
                Reject<InvalidOperationException>(() => Tree!.Root.FindChild("*"));
                Reject<InvalidOperationException>(() => Tree!.Root.FindChildren("*"));
                Reject<InvalidOperationException>(() => Tree!.Root.FindParent("*"));
            }
            Searches++;
        }
    }
    private sealed class SearchBody : RigidBody
    {
        internal int Searches;
        protected override void OnPhysicsProcess(double delta)
        {
            Check(Tree!.Root.FindChild<SearchBody>("TargetMoving") == this && FindParent("Branch") == Parent,
                "Inherited search executes inside a real physical callback"); Searches++;
        }
    }
}
