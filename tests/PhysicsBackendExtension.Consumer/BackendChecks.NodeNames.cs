using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunNodeNames(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var oldSeparator = ProjectSettings.Get(ProjectSettings.NodeNameNumberSeparator);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("NodeNames", () => solver = new Solver());
        try
        {
            NamingCases();
            using var world = backend is { } selected ? new World(selected) : new World("NodeNames");
            using var shape = new CircleShape { Radius = 2 };
            using var root = new SubViewport { Name = "Root", World = world };
            var moving = new RigidBody
            {
                Name = "Mover",
                GravityScale = 0,
                CanSleep = false,
                LinearVelocity = new(6, 0),
                CollisionLayer = 0,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            moving.AddChild(new CollisionShape { Name = "Geometry", Shape = shape }); root.AddChild(moving);
            var same = new StaticBody { Name = "Mover", CollisionLayer = 0 }; root.AddChild(same, true);
            Check(same.Name == "Mover2", "Physical siblings receive readable names before insertion");
            Node[] roles = [new Area(), new AnimatableBody(), new CharacterBody(), new PhysicalBone(), new CollisionShape(), new CollisionPolygon()];
            foreach (var role in roles) { role.Name = "Mover"; root.AddChild(role, true); }
            var next = new Node { Name = "Rejected" };
            using var tree = new SceneTree(root); _ = root.FindWorld();
            var retired = RenameSnapshotChecks(root); Collect(); Check(!retired.TryGetTarget(out _), "Retained rename storage releases removed children");
            var id = moving.GetRID(); var version = moving.InstanceID; var callbacks = 0;
            PhysicsServer.BodySetForceIntegrationCallback(id, _ => callbacks++);
            foreach (var action in new Action[] { () => root.AddChild(next, true), () => moving.AddSibling(next, true), () => moving.Name = "Mover2" })
                Task.Run(() => Reject<InvalidOperationException>(action)).GetAwaiter().GetResult();
            Check(next.Parent is null && next.Name == "Rejected" && moving.Name == "Mover", "Rejected foreign operations preserve names and hierarchy");
            if (solver is not null)
            {
                solver.StepHook = () =>
                {
                    Reject<InvalidOperationException>(() => root.AddChild(next, true));
                    Reject<InvalidOperationException>(() => moving.Name = "Mover2");
                };
                tree.PhysicsFrame(1d / 60); solver.StepHook = null;
            }
            next.Dispose();
            var renamed = 0;
            moving.Renamed += _ => { renamed++; Check(root.GetNode(moving.Name) == moving, "Rename callbacks observe the committed unique name"); };
            moving.Name = "Mover2";
            Check(moving.Name == "Mover9" && renamed == 1 && moving.GetRID() == id && moving.InstanceID == version,
                "Collision rename advances through existing slots without changing object or physical identity");
            moving.Name = "Mover2";
            Check(moving.Name == "Mover9" && renamed == 2,
                "A different requested name still notifies when collision resolution returns the current name");
            moving.Name = "Mover";
            Action<Node> fail = _ => throw new InvalidOperationException("Expected rename observer failure"); moving.Renamed += fail;
            Reject<AggregateException>(() => moving.Name = "Mover2"); moving.Renamed -= fail;
            Check(moving.Name == "Mover9" && root.GetNode("Mover9") == moving, "Failed rename callbacks preserve committed paths");
            for (var i = 0; i < 128; i++) Frame();
            var start = moving.Position.X; var count = callbacks;
            Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && callbacks == count + 128 && Math.Abs(moving.Position.X - start - 12.8f) < .01f,
                $"128 rename/full frames: {owner}/{all} B, callbacks={callbacks - count}, motion={moving.Position.X - start} (expected 12.8 u within .01 u)");
            var before = moving.Name; var target = new Node { Name = "Target" }; root.AddChild(target); target.AddChild(new Node { Name = before });
            moving.Reparent(target); Check(moving.Name.StartsWith("@RigidBody@", StringComparison.Ordinal) && moving.GetRID() == id && root.GetNode("Target/" + moving.Name) == moving,
                "Reparenting resolves destination collisions with stable RID and actual paths");
            PersistNames();
            Console.WriteLine($"Node naming {backend?.ToString() ?? "registered"}, threaded={threaded}: suffix/fast/internal/rename/reparent/replacement/guards/file cases; 128 full frames {owner}/{all} B.");
            void Frame()
            {
                moving.Name = moving.Name == "Mover" ? "Moving" : "Mover";
                Check(root.GetNode(moving.Name) == moving && moving.GetRID() == id, "Warm names resolve the same physical object"); tree.PhysicsFrame(1d / 60);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.NodeNameNumberSeparator, oldSeparator); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Node> RenameSnapshotChecks(Node root)
    {
        var branch = new NameNotificationNode { Name = "Notifications" }; root.AddChild(branch);
        var first = new NameNotificationNode { Name = "First" }; var later = new NameNotificationNode { Name = "Later" };
        branch.AddChild(first); branch.AddChild(later);
        var nested = false; var firstCalls = 0; var laterCalls = 0;
        first.Hook = () => { firstCalls++; if (nested) return; nested = true; branch.RemoveChild(later); branch.Name = "Nested"; };
        later.Hook = () => laterCalls++;
        branch.Name = "Changed";
        Check(branch.Name == "Nested" && firstCalls == 2 && laterCalls == 1 && later.Parent is null,
            "Nested rename and callback removal preserve the outer captured traversal");
        first.Hook = () => throw new InvalidOperationException("Expected path notification failure");
        Reject<AggregateException>(() => branch.Name = "Failed"); first.Hook = null; later.Hook = null;
        branch.Name = "Recovered"; later.Dispose(); return new(later);
    }
    private sealed class NameNotificationNode : Node
    {
        internal Action? Hook;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what); if (what == NotificationPathRenamed) Hook?.Invoke();
        }
    }

    private static void NamingCases()
    {
        for (var separator = 0; separator < 4; separator++)
        {
            ProjectSettings.Set(ProjectSettings.NodeNameNumberSeparator, separator);
            var join = new[] { "", " ", "_", "-" }[separator];
            using var root = new Node(); var first = new Node { Name = "Item" }; root.AddChild(first);
            var second = new Node { Name = "Item" }; root.AddChild(second, true); Check(second.Name == "Item" + join + "2", "Readable separator drives collision naming");
            var third = new Node { Name = second.Name }; first.AddSibling(third, true);
            Check(third.Name == "Item" + join + "3" && third.GetIndex() == 1, "AddSibling preserves immediate order and increments an existing suffix");
            var padded = new Node { Name = "Padded" + join + "009" }; root.AddChild(padded);
            var padded2 = new Node { Name = padded.Name }; root.AddChild(padded2, true); Check(padded2.Name == "Padded" + join + "010", "Leading numeric width survives carry");
            var huge = new Node { Name = "Huge" + join + new string('9', 40) }; root.AddChild(huge);
            var huge2 = new Node { Name = huge.Name }; root.AddChild(huge2, true); Check(huge2.Name == "Huge" + join + "1" + new string('0', 40), "Suffix carry has no fixed integer width");
            var plain = new Node { Name = "Other9" }; root.AddChild(plain); var other = new Node { Name = plain.Name }; root.AddChild(other, true);
            Check(other.Name == (separator == 0 ? "Other10" : "Other9" + join + "2"), "Trailing digits count only after the selected separator");
            var fast = new Node { Name = "Item" }; root.AddChild(fast); Check(fast.Name.StartsWith("@Node@", StringComparison.Ordinal), "Default collision names use the compiled type");
            var hidden = new Node { Name = "Item" }; root.AddChild(hidden, true, Node.InternalMode.Front);
            var hiddenSibling = new Node { Name = "Item" }; hidden.AddSibling(hiddenSibling, true);
            Check(hidden.Name != hiddenSibling.Name && !root.Children.Contains(hiddenSibling) && root.GetChild(1, true) == hiddenSibling,
                "Internal partitions share the name namespace and sibling insertion mode");
            var existing = second.Name; first.Name = existing;
            Check(first.Name != existing && root.GetNode(first.Name) == first, "Name setter uses the same readable policy");
            var old = new Node { Name = "Old" }; root.AddChild(old); var moved = new Node { Name = "Child" }; old.AddChild(moved);
            var replacement = new Node { Name = second.Name }; replacement.AddChild(new Node { Name = "Child" });
            Reject<InvalidOperationException>(() => old.ReplaceBy(root));
            old.ReplaceBy(replacement); Check(replacement.Name.StartsWith("@Node@", StringComparison.Ordinal) && moved.Name.StartsWith("@Node@", StringComparison.Ordinal) && moved.Parent == replacement,
                "Replacement resolves both outer and transferred-child collisions while rejecting ancestor cycles"); old.Dispose();
        }
        ProjectSettings.Set(ProjectSettings.NodeNameNumberSeparator, 0);
        ProjectSettings.SetFeatureOverride(ProjectSettings.NodeNameNumberSeparator, "names-check", 2); ProjectSettings.AddCustomFeature("names-check");
        try { using var root = new Node(); root.AddChild(new Node { Name = "Feature" }); var child = new Node { Name = "Feature" }; root.AddChild(child, true); Check(child.Name == "Feature_2", "Live feature overrides drive the next collision"); }
        finally { ProjectSettings.RemoveCustomFeature("names-check"); ProjectSettings.ClearFeatureOverride(ProjectSettings.NodeNameNumberSeparator, "names-check"); }
        Reject<ArgumentOutOfRangeException>(() => ProjectSettings.Set(ProjectSettings.NodeNameNumberSeparator, 4));
    }

    private static void PersistNames()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-node-names-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); Directory.CreateDirectory(System.IO.Path.Combine(directory, "user"));
        try
        {
            using var root = new Node { Name = "Saved" };
            var first = new StaticBody { Name = "Actor" }; var second = new StaticBody { Name = "Actor" }; var third = new StaticBody { Name = "Actor" };
            root.AddChild(first); root.AddChild(second, true); root.AddChild(third); first.Owner = second.Owner = third.Owner = root;
            using var packed = new PackedScene(); packed.Pack(root); using var copy = packed.Instantiate(); CheckNamesArchive(copy, third.Name);
            File.WriteAllText(System.IO.Path.Combine(directory, "generated.txt"), third.Name);
            var path = System.IO.Path.Combine(directory, "names.e2dscene"); ResourceSaver.Save(packed, path);
            using var registry = new ProjectSettingsRegistry(directory, System.IO.Path.Combine(directory, "user")); registry.Set(ProjectSettings.NodeNameNumberSeparator, 3); registry.SaveCustom(System.IO.Path.Combine(directory, "names.cfg"));
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(System.IO.Path.Combine(AppContext.BaseDirectory, Assembly.GetEntryAssembly()!.GetName().Name + ".dll"));
            start.ArgumentList.Add("--node-names-child"); start.ArgumentList.Add(directory);
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Node names fresh load"); }
            Check(child.ExitCode == 0 && output.Result.Contains("Fresh node names passed"), "Fresh scene/config load: " + output.Result + errors.Result);
        }
        finally { Directory.Delete(directory, true); }
    }
    public static void ReadNodeNames(string directory)
    {
        using var registry = new ProjectSettingsRegistry(directory, System.IO.Path.Combine(directory, "user")); registry.LoadCustom(System.IO.Path.Combine(directory, "names.cfg")); Check(registry.Get(ProjectSettings.NodeNameNumberSeparator) == 3, "Typed separator survives independent project persistence");
        using var packed = ResourceLoader.Load<PackedScene>(System.IO.Path.Combine(directory, "names.e2dscene"), ResourceLoader.CacheMode.IgnoreDeep);
        using var root = packed.Instantiate(); CheckNamesArchive(root, File.ReadAllText(System.IO.Path.Combine(directory, "generated.txt"))); Console.WriteLine("Fresh node names passed");
    }
    private static void CheckNamesArchive(Node root, string generated) => Check(root.GetChild(0).Name == "Actor" && root.GetChild(1).Name == "Actor2" && root.GetChild(2).Name == generated, "Packed names retain exact readable and generated identities");
}
