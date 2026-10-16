using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunViewportOwnership(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("ViewportOwnership", static () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("ViewportOwnership");
            using var host = new SubViewport { World = world, GUIEmbedSubwindows = true };
            var window = new Window { World = world, GUIEmbedSubwindows = true, Visible = false }; host.AddChild(window);
            var left = new SubViewport { World = world, GUIEmbedSubwindows = true }; window.AddChild(left);
            var inner = new SubViewport { World = world, GUIEmbedSubwindows = true }; left.AddChild(inner);
            var other = new Window { World = world, GUIEmbedSubwindows = true, Visible = false }; inner.AddChild(other);
            var right = new SubViewport { World = world }; other.AddChild(right);
            var branch = new Node { Name = "PhysicalRoles" }; inner.AddChild(branch);
            using var shape = new CircleShape { Radius = 2 };
            var moving = new RigidBody
            {
                Name = "Mover",
                GravityScale = 0,
                CanSleep = false,
                CollisionLayer = 0,
                LinearVelocity = new(6, 0),
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            moving.AddChild(new CollisionShape { Shape = shape }); branch.AddChild(moving);
            Node[] roles = [moving, moving.GetChild(0), new Node(), new Entity(), new StaticBody { CollisionLayer = 0 },
                new AnimatableBody { CollisionLayer = 0 }, new CharacterBody { CollisionLayer = 0 }, new PhysicalBone(), new Area { CollisionLayer = 0 }];
            foreach (var role in roles[2..]) branch.AddChild(role);
            foreach (var node in new Node[] { host, window, left, inner, other, right, branch }.Concat(roles)) Detached(node);
            Viewport enterViewport = inner, exitViewport = inner; Window enterWindow = window, exitWindow = window;
            var enters = 0; var exits = 0; var retired = 0;
            foreach (var role in roles)
            {
                role.TreeEntered += node => { Associated(node, enterViewport, enterWindow); enters++; };
                role.TreeExiting += node => { Associated(node, exitViewport, exitWindow); exits++; };
                role.TreeExited += node => { Detached(node); retired++; };
            }
            branch.TreeEntered += _ => { foreach (var role in roles) Detached(role); };
            using var tree = new SceneTree(host);
            Associated(host, host, null); Associated(window, window, window); Associated(left, left, window);
            Associated(inner, inner, window); Associated(other, other, other); Associated(right, right, other);
            Check(enters == 9 && world.PhysicsBackendName == (backend?.ToString() ?? "ViewportOwnership"), "All physical roles enter the selected backend with active viewport ownership");
            roles[2].Notify(Node.NotificationExitTree); roles[2].Notify(Node.NotificationEnterTree);
            Associated(roles[2], inner, window);
            enterViewport = right; enterWindow = other; branch.Reparent(right);
            Check(enters == 18 && exits == 9 && retired == 9, "Reparenting clears old ownership before publishing the new viewport/window");
            exitViewport = right; exitWindow = other;
            foreach (var role in roles) Associated(role, right, other);
            Task.Run(() =>
            {
                foreach (var role in roles)
                {
                    Check(role.GetViewport() == right, "A cached borrowed viewport read does not traverse the live hierarchy or require its processing group");
                    Reject<InvalidOperationException>(() => role.GetWindow());
                }
            }).GetAwaiter().GetResult();
            var worker = new ViewportOwnershipWorker { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
            right.AddChild(worker); var calls = 0; var workerThread = 0;
            worker.Call = node =>
            {
                Associated(node, right, other); Check(moving.GetViewport() == right, "A worker can read another group's cached viewport reference");
                Reject<InvalidOperationException>(() => moving.GetWindow()); calls++; workerThread = Environment.CurrentManagedThreadId;
            };
            tree.PhysicsFrame(1d / 60); right.RemoveChild(worker); worker.Dispose();
            Check(calls == 1 && workerThread != Environment.CurrentManagedThreadId, "Window lookup executes in its real subgroup without entering the window's group");
            Action<Node> enterFailure = _ => throw new InvalidOperationException("viewport-enter");
            Action<Node> exitFailure = _ => throw new InvalidOperationException("viewport-exit");
            Action<Node> retiredFailure = _ => throw new InvalidOperationException("viewport-retired");
            moving.TreeExiting += exitFailure; moving.TreeExited += retiredFailure;
            Reject<AggregateException>(() => right.RemoveChild(branch));
            Check(branch.Parent is null && roles.All(node => node.GetViewport() is null && node.GetWindow() is null), "Exit failures cannot retain an active viewport");
            moving.TreeExiting -= exitFailure; moving.TreeExited -= retiredFailure; moving.TreeEntered += enterFailure;
            Reject<AggregateException>(() => right.AddChild(branch)); moving.TreeEntered -= enterFailure;
            foreach (var role in roles) Associated(role, right, other);
            Check(branch.Parent == right && enters == 27 && exits == 18 && retired == 18, "Entry errors preserve committed associations for every sibling");
            using var survivor = new Node(); var weak = RetiredViewport(host, survivor, world); tree.FlushDeferred(); Collect();
            Check(!weak.TryGetTarget(out _) && survivor.GetViewport() is null, "A surviving detached node does not retain its retired viewport");
            using (var noViewport = new Node())
            using (var otherTree = new SceneTree(noViewport))
            {
                right.AddChild(survivor); Associated(survivor, right, other); right.RemoveChild(survivor);
                noViewport.AddChild(survivor); Detached(survivor);
                Check(survivor.Tree == otherTree, "Transfer to another active tree without a viewport cannot retain the old association");
                noViewport.RemoveChild(survivor);
            }
            var rid = moving.GetRID(); var callbacks = 0; PhysicsServer.BodySetForceIntegrationCallback(rid, _ => callbacks++);
            for (var i = 0; i < 128; i++) Frame();
            var start = moving.Position.X; var before = callbacks; Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && callbacks == before + 128 && Math.Abs(moving.Position.X - start - 12.8f) < .01f,
                $"128 viewport queries/full frames: {owner}/{all} B, motion={moving.Position.X - start}, callbacks={callbacks - before}");
            right.RemoveChild(branch); Detached(branch); foreach (var role in roles) Detached(role);
            branch.Dispose(); Reject<ObjectDisposedException>(() => moving.GetViewport()); Reject<ObjectDisposedException>(() => moving.GetWindow());
            ViewportOwnershipArchive(backend, threaded);
            Console.WriteLine($"Viewport ownership {backend?.ToString() ?? "registered"}, threaded={threaded}: active cache, nested windows, groups, lifecycle, fresh scene; 128 full frames {owner}/{all} B.");
            void Frame()
            {
                foreach (var role in roles) Associated(role, right, other);
                Check(moving.GetRID() == rid && moving.GetWorld() == world, "Viewport reads preserve selected physical ownership"); tree.PhysicsFrame(1d / 60);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
        static void Detached(Node node) => Associated(node, null, null);
        static void Associated(Node node, Viewport? viewport, Window? window) =>
            Check(node.GetViewport() == viewport && node.GetWindow() == window, "Viewport/window association matches the active lifecycle phase");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Viewport> RetiredViewport(Node root, Node survivor, World world)
    {
        var viewport = new SubViewport { World = world }; root.AddChild(viewport); viewport.AddChild(survivor);
        viewport.RemoveChild(survivor); viewport.Dispose(); return new(viewport);
    }

    private sealed class ViewportOwnershipWorker : Node
    {
        internal Action<Node>? Call;
        protected override void OnPhysicsProcess(double delta) => Call?.Invoke(this);
    }

    private static void ViewportOwnershipArchive(PhysicsServer.Backend? backend, bool threaded)
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-viewport-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            using var root = new SubViewport { Name = "Root", GUIEmbedSubwindows = true };
            var window = new Window { Name = "Window", Visible = false, GUIEmbedSubwindows = true }; root.AddChild(window);
            var nested = new SubViewport { Name = "Nested" }; window.AddChild(nested);
            var body = new RigidBody
            {
                Name = "Mover",
                GravityScale = 0,
                CanSleep = false,
                CollisionLayer = 0,
                LinearVelocity = new(6, 0),
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            using var shape = new CircleShape { Radius = 2 }; var collider = new CollisionShape { Name = "Shape", Shape = shape };
            nested.AddChild(body); body.AddChild(collider); window.Owner = nested.Owner = body.Owner = collider.Owner = root;
            using var packed = new PackedScene(); packed.Pack(root); var path = System.IO.Path.Combine(directory, "viewport.e2dscene"); ResourceSaver.Save(packed, path);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(System.IO.Path.Combine(AppContext.BaseDirectory, Assembly.GetEntryAssembly()!.GetName().Name + ".dll"));
            start.ArgumentList.Add("--viewport-ownership-child"); start.ArgumentList.Add(path);
            if (backend is { } choice) start.ArgumentList.Add(choice == PhysicsServer.Backend.CPU ? "--cpu" : "--gpu");
            if (threaded) start.ArgumentList.Add("--threaded");
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Viewport ownership fresh load"); }
            Check(child.ExitCode == 0 && output.Result.Contains("Fresh viewport ownership passed"), "Fresh viewport load: " + output.Result + errors.Result);
            Console.Write(output.Result);
        }
        finally { Directory.Delete(directory, true); }
    }

    public static void ReadViewportOwnership(string path, PhysicsServer.Backend? backend, bool threaded)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("ViewportOwnership", static () => new Solver());
        using var world = backend is { } choice ? new World(choice) : new World("ViewportOwnership");
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var root = (SubViewport)packed.Instantiate();
        var window = (Window)root.GetNode("Window"); var nested = (SubViewport)window.GetNode("Nested"); var body = (RigidBody)nested.GetNode("Mover");
        Check(root.GetViewport() is null && window.GetWindow() is null && body.GetViewport() is null, "Loaded authored hierarchy has no active associations");
        root.World = window.World = nested.World = world;
        using var tree = new SceneTree(root);
        Check(root.GetViewport() == root && root.GetWindow() is null && window.GetViewport() == window && window.GetWindow() == window &&
            nested.GetViewport() == nested && nested.GetWindow() == window && body.GetViewport() == nested && body.GetWindow() == window,
            "Fresh activation reconstructs viewport boundaries and containing windows");
        Check(body.GetWorld() == world && world.PhysicsBackendName == (backend?.ToString() ?? "ViewportOwnership"), "Fresh physical scene selects the requested real backend");
        tree.PhysicsFrame(1d / 60); Check(Math.Abs(body.Position.X - .1f) < .001f, "Fresh body executes its fixed physical step");
        nested.RemoveChild(body); Check(body.GetViewport() is null && body.GetWindow() is null, "Fresh detachment clears retained associations"); body.Dispose();
        Console.WriteLine("Fresh viewport ownership passed");
    }
}
