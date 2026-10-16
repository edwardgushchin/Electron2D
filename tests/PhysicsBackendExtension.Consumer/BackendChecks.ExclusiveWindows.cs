using System.Runtime.CompilerServices;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunExclusiveWindows(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("ExclusiveWindows", static () => new Solver());
        try
        {
            using var empty = new Node();
            Check(empty.GetLastExclusiveWindow() is null, "A hierarchy without a window has no exclusive window");
            using var world = backend is { } selected ? new World(selected) : new World("ExclusiveWindows");
            using var shape = new CircleShape { Radius = 2 };
            using var host = new SubViewport { World = world, GUIEmbedSubwindows = true, Size = new(640, 480) };
            var root = new Window { Name = "Root", World = world, GUIEmbedSubwindows = true, Visible = false, Size = new(640, 480) }; host.AddChild(root);
            var moving = new RigidBody
            {
                GravityScale = 0,
                CanSleep = false,
                LinearVelocity = new(6, 0),
                CollisionLayer = 0,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            moving.AddChild(new CollisionShape { Shape = shape }); root.AddChild(moving);
            Node[] roles = [moving, moving.GetChild(0), new Node(), new Entity(), new StaticBody { CollisionLayer = 0 },
                new AnimatableBody { CollisionLayer = 0 }, new CharacterBody { CollisionLayer = 0 }, new PhysicalBone(), new Area { CollisionLayer = 0 }];
            foreach (var role in roles[2..]) root.AddChild(role);
            var first = Modal("First"); var second = Modal("Second"); var sibling = Modal("Sibling");
            var unrelated = Modal("Unrelated"); unrelated.Transient = false;
            root.AddChild(first); first.AddChild(second); first.AddChild(sibling); root.AddChild(unrelated);
            Check(second.GetLastExclusiveWindow() is null && moving.GetLastExclusiveWindow() is null, "Detached windows have no active containing window");
            using var tree = new SceneTree(host); root.Show(); _ = root.FindWorld();
            Check(world.PhysicsBackendName == (backend?.ToString() ?? "ExclusiveWindows"), "The selected public backend actually owns the simulated world");
            root.TranslationDomain = "exclusive-domain";
            foreach (var role in roles) Check(role.TranslationDomain == "exclusive-domain", "Physical roles inherit the assigned object translation domain");
            moving.TranslationDomain = "body-domain"; Check(moving.TranslationDomain == "body-domain", "The inherited public object setter accepts an explicit physical-node domain"); moving.SetTranslationDomainInherited();
            Check(moving.GetLastExclusiveWindow() == root, "Hidden exclusive windows are not selected");
            unrelated.Show(); Check(root.GetLastExclusiveWindow() == root, "A nontransient exclusive window does not extend its containing window's chain");
            first.Show(); second.Show(); unrelated.GrabFocus();
            foreach (var role in roles) Check(role.GetLastExclusiveWindow() == second, "Every physical node role reaches the last active exclusive child");
            Check(unrelated.GetLastExclusiveWindow() == unrelated && first.GetLastExclusiveWindow() == second, "Queries start at the caller's own containing window");
            Task.Run(() => Reject<InvalidOperationException>(() => moving.GetLastExclusiveWindow())).GetAwaiter().GetResult();
            var worker = new ExclusiveQueryNode { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, PhysicsProcessEnabled = true };
            root.AddChild(worker); var calls = 0; var workerThread = 0;
            worker.Call = node => { Check(node.GetLastExclusiveWindow() == second, "An own-group worker may read its active window chain"); Reject<InvalidOperationException>(() => moving.GetLastExclusiveWindow()); calls++; workerThread = Environment.CurrentManagedThreadId; };
            tree.PhysicsFrame(1d / 60); root.RemoveChild(worker); worker.Dispose();
            Check(calls == 1 && workerThread != Environment.CurrentManagedThreadId, "The query executes in the real subgroup and rejects a different group");
            Action<Window, Node>[] popups = [(w, n) => w.PopupExclusive(n), (w, n) => w.PopupExclusiveCentered(n),
                (w, n) => w.PopupExclusiveCenteredClamped(n), (w, n) => w.PopupExclusiveCenteredRatio(n),
                (w, n) => w.PopupExclusiveOnParent(n, new(new(10, 10), new(80, 40)))];
            foreach (var popup in popups)
            {
                using var dialog = new Window { World = world, Visible = false, Borderless = true, Size = new(80, 40) };
                Reject<ArgumentNullException>(() => popup(dialog, null!));
                Reject<InvalidOperationException>(() => popup(dialog, empty));
                using var disposedSource = new Node(); disposedSource.Dispose(); Reject<ObjectDisposedException>(() => popup(dialog, disposedSource));
                Task.Run(() => Reject<InvalidOperationException>(() => popup(dialog, moving))).GetAwaiter().GetResult();
                Check(dialog.Parent is null, "Rejected sources do not parent the dialog");
                popup(dialog, moving); Check(dialog.Parent == second && dialog.Transient && dialog.Visible, "Every exclusive popup entry point uses the active chain despite another frontmost window");
                dialog.Hide(); second.RemoveChild(dialog);
                using var failed = new Window { World = world, Visible = false, Transient = true, Exclusive = true, Borderless = true };
                failed.TreeEntered += _ => { failed.Show(); throw new InvalidOperationException("dialog-entry"); };
                failed.TreeExiting += _ => throw new InvalidOperationException("dialog-exit");
                AggregateException? errors = null;
                try { popup(failed, moving); } catch (AggregateException error) { errors = error; }
                Check(errors is not null && errors.Flatten().InnerExceptions.Any(error => error.Message == "dialog-entry") &&
                    errors.Flatten().InnerExceptions.Any(error => error.Message == "dialog-exit"), "Popup rollback preserves both attachment and cleanup failures");
                Check(failed.Parent is null && !failed.IsInsideTree && root.GetLastExclusiveWindow() == second, "Every failed popup detaches and clears its transient selection");
            }
            second.Exclusive = false; Check(root.GetLastExclusiveWindow() == first, "Disabling exclusivity clears only its own selection");
            second.Exclusive = true; second.Transient = false; Check(root.GetLastExclusiveWindow() == first, "Clearing transient ownership removes the exclusive child");
            second.Transient = true; Check(root.GetLastExclusiveWindow() == second, "Restoring transient ownership selects the visible exclusive child");
            using var logger = new ExclusiveSelectionLogger(); OS.AddLogger(logger);
            try
            {
                sibling.Show(); second.GrabFocus(); Check(root.GetLastExclusiveWindow() == sibling && logger.Conflicts == 1, "Latest activation wins, independently of subsequent focus/display order, and diagnoses conflicting siblings");
                second.Hide(); Check(root.GetLastExclusiveWindow() == sibling, "Retiring an older sibling preserves the active selection");
                second.Show(); Check(root.GetLastExclusiveWindow() == second && logger.Conflicts == 2, "Showing another exclusive sibling explicitly replaces the selection");
                second.Hide(); Check(sibling.Visible && root.GetLastExclusiveWindow() == first, "Retiring the active selection does not silently reactivate an older visible sibling");
                sibling.Hide(); second.Show();
            }
            finally { OS.RemoveLogger(logger); }
            Action badVisibility = () => { Check(root.GetLastExclusiveWindow() == (second.Visible ? second : first), "Visibility observers see the committed exclusive chain"); throw new InvalidOperationException("visibility"); };
            second.VisibilityChanged += badVisibility;
            Reject<AggregateException>(second.Hide); Reject<AggregateException>(second.Show); second.VisibilityChanged -= badVisibility;
            second.Hide(); Action badFocus = () => { Check(root.GetLastExclusiveWindow() == second, "Focus observers see the committed selection"); throw new InvalidOperationException("focus"); };
            second.FocusEntered += badFocus; Reject<AggregateException>(second.Show); second.FocusEntered -= badFocus;
            Check(root.GetLastExclusiveWindow() == second, "Callback errors preserve the selected child");
            first.Exclusive = false; Check(root.GetLastExclusiveWindow() == root && first.GetLastExclusiveWindow() == second, "A broken parent link stops traversal without erasing its own child chain"); first.Exclusive = true;
            first.RemoveChild(second); Check(root.GetLastExclusiveWindow() == first && second.GetLastExclusiveWindow() is null, "Detachment clears the old owner and active containing window");
            first.AddChild(second); Check(root.GetLastExclusiveWindow() == second, "Reentry restores the actual transient parent");
            var other = new Window { World = world, Visible = false, Borderless = true }; root.AddChild(other);
            second.Reparent(other); Check(root.GetLastExclusiveWindow() == first && other.GetLastExclusiveWindow() == second, "Reparenting moves the transient link to its new containing window");
            second.Reparent(first); other.Dispose(); first.Hide(); Check(root.GetLastExclusiveWindow() == root && !second.Visible, "Hiding the parent retires the complete embedded child chain"); first.Show(); second.Show();
            second.Exclusive = false;
            var retired = RetiredExclusiveWindow(first, world); tree.FlushDeferred(); Collect(); Check(!retired.TryGetTarget(out _), "Retired transient state does not keep an exclusive window alive");
            Check(root.GetLastExclusiveWindow() == first, "Retirement leaves no stale selection"); second.Exclusive = false; second.Exclusive = true;
            var rid = moving.GetRID(); var callbacks = 0; PhysicsServer.BodySetForceIntegrationCallback(rid, _ => callbacks++);
            for (var i = 0; i < 128; i++) Frame();
            var start = moving.Position.X; var before = callbacks; Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && callbacks == before + 128 && Math.Abs(moving.Position.X - start - 12.8f) < .01f,
                $"128 exclusive queries/full frames: {owner}/{all} B, motion={moving.Position.X - start}, callbacks={callbacks - before}");
            second.Dispose(); Check(root.GetLastExclusiveWindow() == first, "Disposal removes the selected child"); Reject<ObjectDisposedException>(() => second.GetLastExclusiveWindow());
            first.Dispose(); Check(root.GetLastExclusiveWindow() == root, "Disposal removes the parent selection");
            Console.WriteLine($"Exclusive windows {backend?.ToString() ?? "registered"}, threaded={threaded}: transient chains, popup siblings, callbacks, retirement; 128 full frames {owner}/{all} B.");
            Window Modal(string name) => new() { Name = name, World = world, Visible = false, Transient = true, Exclusive = true, Borderless = true, Size = new(160, 100) };
            void Frame()
            {
                second.Exclusive = false; Check(moving.GetLastExclusiveWindow() == first, "Warm disabled link is observed"); second.Exclusive = true;
                foreach (var role in roles) Check(role.GetLastExclusiveWindow() == second, "Warm physical roles share the same chain");
                Check(moving.GetRID() == rid, "Window queries preserve the actual physical owner"); tree.PhysicsFrame(1d / 60);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Window> RetiredExclusiveWindow(Window root, World world)
    {
        var window = new Window { World = world, Visible = false, Transient = true, Exclusive = true, Borderless = true };
        root.AddChild(window); window.Show(); var reference = new WeakReference<Window>(window); window.Dispose(); return reference;
    }

    private sealed class ExclusiveSelectionLogger : Logger
    {
        internal int Conflicts;
        protected override void OnLogError(string function, string file, int line, string code, string rationale,
            bool editorNotify, LoggerErrorType errorType, ReadOnlySpan<ScriptBacktrace> scriptBacktraces)
        { if (rationale == "A window already has another active exclusive child.") Conflicts++; }
    }

    private sealed class ExclusiveQueryNode : Node
    {
        internal Action<Node>? Call;
        protected override void OnPhysicsProcess(double delta) => Call?.Invoke(this);
    }
}
