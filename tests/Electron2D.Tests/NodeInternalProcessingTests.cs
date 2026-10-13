using Electron2D;
using Timer = Electron2D.Timer;

internal static partial class NodeInternalProcessingTests
{
    private sealed class Probe : Node
    {
        internal int Idle, Physics, OrdinaryIdle, OrdinaryPhysics;
        internal Action<Probe, bool>? DuringInternal;
        internal readonly List<int> Order = [];
        protected override void OnNotification(int what)
        {
            if (what == NotificationInternalProcess) { Idle++; Order.Add(1); DuringInternal?.Invoke(this, false); }
            if (what == NotificationInternalPhysicsProcess) { Physics++; Order.Add(3); DuringInternal?.Invoke(this, true); }
            base.OnNotification(what);
        }
        protected override void OnProcess(double delta) { OrdinaryIdle++; Order.Add(2); }
        protected override void OnPhysicsProcess(double delta) { OrdinaryPhysics++; Order.Add(4); }
    }
    private sealed class Quiet : Node
    {
        internal int Idle, Physics;
        protected override void OnNotification(int what)
        { if (what == NotificationInternalProcess) Idle++; if (what == NotificationInternalPhysicsProcess) Physics++; }
    }
    internal static void Run()
    {
        using var root = new Node(); var probe = new Probe(); root.AddChild(probe);
        Check(!probe.InternalProcessEnabled && !probe.InternalPhysicsProcessEnabled, "Plain defaults disabled even with notification override.");
        probe.InternalProcessEnabled = true; probe.InternalPhysicsProcessEnabled = true;
        using var tree = new SceneTree(root);
        tree.ProcessFrame(.02); tree.PhysicsFrame(.01);
        Check(probe.Idle == 1 && probe.Physics == 1 && probe.OrdinaryIdle == 0 && probe.OrdinaryPhysics == 0, "Independent internal lanes run with ordinary callbacks disabled.");
        Check(probe.ProcessDeltaTime == .02 && probe.PhysicsProcessDeltaTime == .01, "Notification deltas use the selected frame lane.");
        probe.ProcessEnabled = probe.PhysicsProcessEnabled = true; probe.Order.Clear();
        tree.ProcessFrame(.02); tree.PhysicsFrame(.01);
        Check(probe.Order.SequenceEqual([1, 2, 3, 4]), "Internal notifications precede ordinary callbacks in each lane.");
        probe.InternalProcessEnabled = false; probe.Order.Clear(); tree.ProcessFrame(.02); tree.PhysicsFrame(.01);
        Check(probe.Order.SequenceEqual([2, 3, 4]) && probe.InternalPhysicsProcessEnabled, "Disabling one lane preserves the other and ordinary work.");
        probe.InternalProcessEnabled = true; probe.ProcessEnabled = probe.PhysicsProcessEnabled = false;
        probe.Order.Clear(); tree.Paused = true; tree.ProcessFrame(.02); tree.PhysicsFrame(.01);
        Check(probe.Order.Count == 0, "Resolved pausable mode gates internal callbacks.");
        probe.ProcessMode = ProcessMode.WhenPaused; tree.ProcessFrame(.02); tree.PhysicsFrame(.01);
        Check(probe.Order.SequenceEqual([1, 3]), "WhenPaused internal callbacks execute.");
        probe.ProcessMode = ProcessMode.Disabled; probe.Order.Clear(); tree.ProcessFrame(.02); tree.PhysicsFrame(.01);
        Check(probe.Order.Count == 0, "Disabled mode gates both lanes without clearing switches.");
        tree.Paused = false; probe.ProcessMode = ProcessMode.Always;
        probe.DuringInternal = (node, physics) => { if (physics) node.PhysicsProcessEnabled = true; else node.ProcessEnabled = true; };
        tree.ProcessFrame(.02); tree.PhysicsFrame(.01);
        Check(probe.Order.SequenceEqual([1, 2, 3, 4]), "Enabling ordinary processing inside internal callback takes effect in this frame.");
        probe.DuringInternal = (node, physics) => { if (physics) node.PhysicsProcessEnabled = false; else node.ProcessEnabled = false; };
        probe.Order.Clear(); tree.ProcessFrame(.02); tree.PhysicsFrame(.01);
        Check(probe.Order.SequenceEqual([1, 3]), "Disabling ordinary processing suppresses its current callback.");
        probe.DuringInternal = (node, physics) => { node.InternalProcessEnabled = false; node.InternalPhysicsProcessEnabled = false; };
        probe.Order.Clear(); tree.ProcessFrame(.02); tree.PhysicsFrame(.01);
        Check(probe.Order.SequenceEqual([1]), "Self-disable prevents subsequent frames and the other lane.");
        probe.DuringInternal = null; probe.InternalProcessEnabled = probe.InternalPhysicsProcessEnabled = true;
        root.RemoveChild(probe); tree.ProcessFrame(.02); Check(probe.Tree is null && probe.InternalProcessEnabled, "Detached node retains runtime flags without callbacks.");
        root.AddChild(probe); var idle = probe.Idle; tree.ProcessFrame(.02); Check(probe.Idle == idle + 1, "Reattachment restores scheduled internal work.");
        Task.Run(() => { Reject<InvalidOperationException>(() => probe.InternalProcessEnabled = false); Reject<InvalidOperationException>(() => probe.InternalPhysicsProcessEnabled = false); }).GetAwaiter().GetResult();
        Check(probe.InternalProcessEnabled && probe.InternalPhysicsProcessEnabled, "Rejected off-owner setters preserve both flags.");
        var properties = probe.GetPropertyList();
        Check(properties.Where(p => p.Name is nameof(Node.InternalProcessEnabled) or nameof(Node.InternalPhysicsProcessEnabled)).Count(p => !p.IsStored && !p.IsReadOnly) == 2, "Typed tooling exposes runtime switches without scene storage.");
        RuntimeSceneState(); FailureAndDetach(); TimerConsumer(); WarmAllocation(); SceneTreeTimerTests.Run();
        probe.Dispose(); Reject<ObjectDisposedException>(() => _ = probe.InternalProcessEnabled); Reject<ObjectDisposedException>(() => _ = probe.InternalPhysicsProcessEnabled);
        Reject<ObjectDisposedException>(() => probe.InternalProcessEnabled = true); Reject<ObjectDisposedException>(() => probe.InternalPhysicsProcessEnabled = true);
        Console.WriteLine("Node internal lanes, current callback state, pause modes, lifecycle, Timer consumers and zero-byte warm operations passed.");
    }
    private static void RuntimeSceneState()
    {
        using var source = new Node { InternalProcessEnabled = true, InternalPhysicsProcessEnabled = true };
        using var packed = new PackedScene(); packed.Pack(source);
        using var copy = packed.Instantiate(); Check(!copy.InternalProcessEnabled && !copy.InternalPhysicsProcessEnabled, "Ordinary scene capture omits runtime internal switches.");
    }
    private static void FailureAndDetach()
    {
        using var root = new Node(); var failing = new Probe { InternalProcessEnabled = true }; var sibling = new Quiet { InternalProcessEnabled = true }; root.AddChild(failing); root.AddChild(sibling);
        using var tree = new SceneTree(root);
        failing.DuringInternal = (node, _) => { node.ProcessEnabled = true; throw new InvalidDataException("internal fixture"); };
        Reject<AggregateException>(() => tree.ProcessFrame(.01));
        Check(failing.OrdinaryIdle == 1 && sibling.Idle == 1, "Internal failure still attempts ordinary and sibling callbacks.");
        failing.DuringInternal = (node, _) => root.RemoveChild(node);
        tree.ProcessFrame(.01); Check(failing.OrdinaryIdle == 1 && failing.Tree is null, "Internal detach suppresses ordinary callback on the escaped node.");
        failing.Dispose();
    }
    private static void TimerConsumer()
    {
        using var root = new Node(); var idle = new Timer { Name = "IdleTimer", OneShot = true }; var physics = new Timer { Name = "PhysicsTimer", OneShot = true, ProcessCallback = ProcessPhase.Physics }; root.AddChild(idle); root.AddChild(physics);
        using var tree = new SceneTree(root); var expired = 0; idle.Timeout += _ => expired++; physics.Timeout += _ => expired++;
        idle.Start(.05); physics.Start(.05);
        Check(idle.InternalProcessEnabled && !idle.InternalPhysicsProcessEnabled && physics.InternalPhysicsProcessEnabled && !physics.InternalProcessEnabled, "Existing Timer startup publishes exact lane flags.");
        idle.InternalProcessEnabled = false; physics.InternalPhysicsProcessEnabled = false;
        tree.ProcessFrame(.1); tree.PhysicsFrame(.1); Check(expired == 0 && idle.TimeLeft == .05 && physics.TimeLeft == .05, "Public switches suspend actual built-in countdowns.");
        idle.InternalProcessEnabled = true; physics.InternalPhysicsProcessEnabled = true;
        tree.ProcessFrame(.1); tree.PhysicsFrame(.1); Check(expired == 2 && !idle.InternalProcessEnabled && !physics.InternalPhysicsProcessEnabled, "Resumed one-shot timers expire and release their internal lanes.");
    }
    private static void WarmAllocation()
    {
        using var node = new Quiet(); using var tree = new SceneTree(node);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 64; i++) WarmOperation(node, tree);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++) WarmOperation(node, tree);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, "Warmed setters/getters and internal frame dispatch allocate zero owner bytes: " + allocated);
    }
    private static void WarmOperation(Quiet node, SceneTree tree)
    {
        node.InternalProcessEnabled = true; node.InternalPhysicsProcessEnabled = true;
        if (!node.InternalProcessEnabled || !node.InternalPhysicsProcessEnabled) throw new InvalidOperationException();
        tree.ProcessFrame(.01); tree.PhysicsFrame(.01);
        node.InternalProcessEnabled = false; node.InternalPhysicsProcessEnabled = false;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
