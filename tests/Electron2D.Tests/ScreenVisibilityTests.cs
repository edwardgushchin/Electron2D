using Electron2D;

internal static class ScreenVisibilityTests
{
    internal static void Run()
    {
        var root = new Node(); var target = new Node { Name = "Target" }; root.AddChild(target);
        var notifier = new VisibleOnScreenNotifier(); root.AddChild(notifier);
        var enabler = new VisibleOnScreenEnabler { Name = "Enabler", EnableNodePath = "../Target" }; root.AddChild(enabler);
        using var tree = new SceneTree(root);
        Check(notifier.Rect == new Rect2(-10, -10, 20, 20) && !notifier.IsOnScreen() &&
            enabler.EnableMode == ScreenEnableMode.Inherit && target.ProcessMode == ProcessMode.Disabled, "Defaults and entry disable target before render.");
        Check((int)ScreenEnableMode.Inherit == 0 && (int)ScreenEnableMode.Always == 1 && (int)ScreenEnableMode.WhenPaused == 2, "Numeric enabled policies.");
        Reject<ArgumentException>(() => notifier.Rect = new(float.NaN, 0, 1, 1));
        Reject<ArgumentOutOfRangeException>(() => enabler.EnableMode = (ScreenEnableMode)3);
        Reject<ArgumentNullException>(() => enabler.EnableNodePath = null!);
        var calls = 0; notifier.ScreenEntered += () => { calls++; Check(notifier.IsOnScreen(), "Commit precedes callback."); };
        notifier.ScreenCandidate = true; Check(notifier.CommitScreenState(), "New visible transition."); notifier.RaiseScreenChange();
        Check(calls == 1 && !notifier.CommitScreenState(), "Unchanged sample does not replay.");
        enabler.ScreenCandidate = true; enabler.CommitScreenState(); enabler.RaiseScreenChange();
        Check(target.ProcessMode == ProcessMode.Inherit, "Screen entry enables inherited processing.");
        enabler.EnableMode = ScreenEnableMode.Always; Check(target.ProcessMode == ProcessMode.Always, "Immediate enabled mode change.");
        enabler.EnableMode = ScreenEnableMode.WhenPaused; Check(target.ProcessMode == ProcessMode.WhenPaused, "Pause policy projection.");
        enabler.EnableNodePath = ""; enabler.ScreenCandidate = false; enabler.CommitScreenState(); enabler.RaiseScreenChange();
        Check(target.ProcessMode == ProcessMode.WhenPaused, "Clearing target does not restore or edit old target.");
        Reject<InvalidOperationException>(() => enabler.EnableNodePath = "../Missing");
        Check(enabler.EnableNodePath == "../Missing", "Invalid changed path commits and leaves empty target cache.");
        enabler.EnableNodePath = "../Target"; Check(target.ProcessMode == ProcessMode.Disabled, "Changed valid path uses current off-screen state.");
        root.RemoveChild(target); var replacement = new Node { Name = "Target", ProcessMode = ProcessMode.Always }; root.AddChild(replacement);
        enabler.ScreenCandidate = true; enabler.CommitScreenState(); enabler.RaiseScreenChange();
        Check(target.ProcessMode == ProcessMode.WhenPaused && replacement.ProcessMode == ProcessMode.Always, "Path target is cached by original object identity.");
        target.Dispose(); enabler.EnableMode = ScreenEnableMode.Inherit;
        root.RemoveChild(enabler); root.AddChild(enabler); Check(replacement.ProcessMode == ProcessMode.Disabled && !enabler.IsOnScreen(), "Reentry resolves replacement and resets screen state.");
        var enteredBefore = calls; root.RemoveChild(notifier); Check(!notifier.IsOnScreen() && calls == enteredBefore, "Departure resets silently."); root.AddChild(notifier);
        Check(Task.Run(() => Capture(() => notifier.IsOnScreen())).Result is InvalidOperationException &&
            Task.Run(() => Capture(() => notifier.Rect = new(0, 0, 2, 2))).Result is InvalidOperationException, "Attached read/write owner guards.");
        foreach (var child in root.Children) child.Owner = root;
        using var scene = new PackedScene(); scene.Pack(root); using var copy = scene.Instantiate()!;
        var copied = copy.GetNodeOrNull("Enabler") as VisibleOnScreenEnabler;
        Check(copied is not null && copied.EnableMode == enabler.EnableMode && copied.EnableNodePath == enabler.EnableNodePath && !copied.IsOnScreen(), "Packing retains exact type and descriptors but no sampled runtime state.");
        var failure = new VisibleOnScreenEnabler { EnableNodePath = "../Target" }; root.AddChild(failure);
        failure.ScreenEntered += () => throw new ApplicationException("expected"); failure.ScreenCandidate = true; failure.CommitScreenState();
        Reject<AggregateException>(failure.RaiseScreenChange);
        Check(replacement.ProcessMode == ProcessMode.Inherit && failure.IsOnScreen() && !failure.CommitScreenState(), "Failed event does not replay and still applies target policy.");
        VerifyReentrantProcessSnapshots();
        Console.WriteLine("Screen visibility types, cached target policies, lifecycle, errors and scene packing passed.");
    }
    private static void VerifyReentrantProcessSnapshots()
    {
        var root = new Node(); var probe = new ReentrantModeNode { Target = root }; root.AddChild(probe);
        using var tree = new SceneTree(root);
        for (var pass = 0; pass < 64; pass++) root.ProcessMode = ProcessMode.Disabled;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 64; pass++) root.ProcessMode = ProcessMode.Disabled;
        Check(GC.GetAllocatedBytesForCurrentThread() == before && root.ProcessMode == ProcessMode.Always && probe.Disabled == 128 && probe.Enabled == 128,
            "Nested notification edits keep separate snapshots and allocate zero managed bytes after warmup.");
    }
    private sealed class ReentrantModeNode : Node
    {
        internal required Node Target;
        internal int Disabled, Enabled;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationDisabled) { Disabled++; Target.ProcessMode = ProcessMode.Always; }
            if (what == NotificationEnabled) Enabled++;
        }
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
