using Electron2D;

static class Failures
{
    internal static void Run()
    {
        const string address = "failure-module";
        var plan = new Plan();
        Check(ExtensionManager.ReloadExtension("absent") == ExtensionManager.LoadStatus.NotLoaded, "Absent reload is observational.");
        Reject<ArgumentNullException>(() => ExtensionManager.ReloadExtension(null!));
        ExtensionManager.LoadExtensionFromFunction(address, plan.Open);
        var module = ExtensionManager.GetExtension(address)!;
        Probe? body = null;
        try
        {
            Plan.ConstructorHook = () =>
            {
                Reject<InvalidOperationException>(() => ExtensionManager.UnloadExtension(address));
                Reject<InvalidOperationException>(() => ExtensionManager.ReloadExtension(address));
            };
            body = module.New<Probe>("probe"); Plan.ConstructorHook = null; body.Score = 99;
            Check(ExtensionManager.IsExtensionLoaded(address), "Retirement rejection inside a factory preserves registration.");
            Reject<InvalidOperationException>(() => module.New<Resource>("probe"));
            Reject<InvalidOperationException>(() => module.RegisterClass<Probe>("late", Plan.Create, _ => { }, _ => { }));
            var opened = plan.Opens; Probe.FailCapture = true;
            Check(ReferenceEquals(Reject<InvalidOperationException>(() => ExtensionManager.ReloadExtension(address)), Probe.Expected), "Original capture error.");
            Probe.FailCapture = false;
            Check(module.IsLibraryOpen() && body.Generation == 1 && plan.Opens == opened, "Capture failure preserves the running module before entry invocation.");
            plan.Version = 2; plan.Mode = "bind";
            Check(ReferenceEquals(Reject<InvalidOperationException>(() => ExtensionManager.ReloadExtension(address)), plan.Expected), "Original rebind failure.");
            Check(!module.IsLibraryOpen() && body.Generation == 0 && body.Notifications == 0, "Failed replacement closes the module and clears partial binding without notification.");
            Reject<InvalidOperationException>(() => module.New<Probe>("probe"));
            plan.Version = 3; plan.Mode = "";
            Check(ExtensionManager.ReloadExtension(address) == ExtensionManager.LoadStatus.Ok && body.Score == 99 && body.Generation == 3 && body.Notifications == 1,
                "Explicit retry restores the original pre-failure state.");
            plan.Mode = "decline";
            Check(ExtensionManager.ReloadExtension(address) == ExtensionManager.LoadStatus.Failed && !module.IsLibraryOpen(), "Declined replacement remains closed and retryable.");
            plan.Mode = ""; plan.Version = 4;
            Check(ExtensionManager.ReloadExtension(address) == ExtensionManager.LoadStatus.Ok && body.Score == 99, "Declined-entry state survives retry.");
            plan.Mode = "abi";
            Reject<InvalidOperationException>(() => ExtensionManager.ReloadExtension(address));
            Check(!module.IsLibraryOpen(), "A different CLR ABI is never silently assigned to an existing owner.");
            plan.Mode = ""; plan.Version = 5; ExtensionManager.ReloadExtension(address);
            Check(body.Score == 99 && body.Generation == 5, "ABI failure retains explicit retry state.");
            plan.Mode = "restore";
            Check(ReferenceEquals(Reject<InvalidOperationException>(() => ExtensionManager.ReloadExtension(address)), Probe.Expected), "Original restoration error.");
            Probe.FailRestore = false; plan.Mode = ""; plan.Version = 6; ExtensionManager.ReloadExtension(address);
            Check(body.Score == 99 && body.Generation == 6, "Restoration failure can retry the retained original state.");
            plan.Mode = "deinitialize";
            Check(ReferenceEquals(Reject<InvalidOperationException>(() => ExtensionManager.ReloadExtension(address)), plan.Expected), "Cleanup error is not duplicated in an aggregate.");
            plan.Mode = ""; ExtensionManager.ReloadExtension(address); Check(body.Score == 99, "Cleanup failure preserves retry state.");
            var signals = 0; Action<Extension> observed = _ => signals++;
            ExtensionManager.ExtensionLoaded += observed;
            body.Reloaded = () => throw plan.Expected;
            try
            {
                Check(ReferenceEquals(Reject<InvalidOperationException>(() => ExtensionManager.ReloadExtension(address)), plan.Expected), "Original notification error.");
                Check(module.IsLibraryOpen() && body.Score == 99 && signals == 1, "Notification failure leaves committed state and still emits the manager signal.");
            }
            finally { body.Reloaded = null; ExtensionManager.ExtensionLoaded -= observed; }
            using var disposed = module.New<Probe>("probe"); disposed.Dispose();
            plan.Spawn = true; ExtensionManager.ReloadExtension(address);
            Check(plan.Fresh is { Notifications: 0 } && disposed.Notifications == 0, "New initialization instances and disposed old owners are not reload recipients.");
            var manager = Engine.GetSingleton(nameof(ExtensionManager));
            ExtensionManager.ExtensionLoaded += observed;
            try
            {
                manager.SetBlockSignals(true); ExtensionManager.ReloadExtension(address);
                Check(signals == 1 && body.Notifications > 1, "Signal blocking does not suppress instance notifications.");
            }
            finally { manager.SetBlockSignals(false); ExtensionManager.ExtensionLoaded -= observed; }
            Exception? foreign = null; var worker = new Thread(() => { try { ExtensionManager.ReloadExtension(address); } catch (Exception error) { foreign = error; } }); worker.Start(); worker.Join();
            Check(foreign is InvalidOperationException && module.IsLibraryOpen(), "Foreign replacement rejects without changing the module.");
            var batches = 0;
            Action batch = () => { batches++; Reject<InvalidOperationException>(() => ExtensionManager.ReloadExtensions([])); };
            ExtensionManager.ExtensionsReloaded += batch;
            try
            {
                var counts = ExtensionManager.ReloadExtensions(new[] { address, address, "absent" });
                Check(counts.Count == 2 && counts[address] == ExtensionManager.LoadStatus.Ok && counts["absent"] == ExtensionManager.LoadStatus.NotLoaded && batches == 1, "Changed addresses deduplicate and complete once after individual reloads.");
                ExtensionManager.ReloadExtensions([]); ExtensionManager.ReloadExtensions(["absent"]);
                Check(batches == 1, "Empty and unknown-only batches do not claim a change.");
                plan.Mode = "bind";
                Check(ReferenceEquals(Reject<InvalidOperationException>(() => ExtensionManager.ReloadExtensions([address])), plan.Expected) && batches == 2,
                    "A failed changed module still completes its batch before the original error propagates.");
                plan.Mode = ""; ExtensionManager.ReloadExtension(address);
            }
            finally { ExtensionManager.ExtensionsReloaded -= batch; }
            plan.Mode = "remove"; var count = body.Notifications; ExtensionManager.ReloadExtension(address);
            Check(body.Generation == 0 && body.Notifications == count, "Removed class leaves a cleared owner without a reload notification.");
        }
        finally
        {
            Probe.FailCapture = Probe.FailRestore = false; Plan.ConstructorHook = null;
            body?.Dispose(); ExtensionManager.UnloadExtension(address); module.Dispose();
            plan.Fresh?.Dispose();
        }
        plan = new Plan { Minimum = ExtensionInitializationLevel.Core };
        ExtensionManager.LoadExtensionFromFunction(address, plan.Open); module = ExtensionManager.GetExtension(address)!;
        using (var owner = module.New<Probe>("probe"))
        {
            owner.Score = 21; var before = plan.Opens;
            Check(ExtensionManager.ReloadExtension(address) == ExtensionManager.LoadStatus.NeedsRestart && module.IsLibraryOpen() && plan.Opens == before + 1 && owner.Score == 21 && owner.Generation == 0,
                "A reopened Core minimum requires restart after retiring the old generation, without binding or notification.");
        }
        ExtensionManager.UnloadExtension(address); module.Dispose();
        if (Engine.IsEditorHint)
        {
            plan = new Plan(); ExtensionManager.LoadExtensionFromFunction(address, plan.Open); module = ExtensionManager.GetExtension(address)!;
            using var owner = module.New<Probe>("probe"); owner.Score = 13;
            plan.Minimum = ExtensionInitializationLevel.Editor; plan.Trace.Clear();
            ExtensionManager.ReloadExtension(address); Check(owner.Score == 13, "A higher minimum initializes its actual level.");
            plan.Trace.Clear(); ExtensionManager.UnloadExtension(address);
            Check(plan.Trace.SequenceEqual(new[] { "-3" }), "Uninitialized holes are not deinitialized."); module.Dispose();
        }
        Check(ExtensionManager.GetLoadedExtensions().Length == 0, "Failure cases retire every module.");
        plan = new Plan(); ExtensionManager.LoadExtensionFromFunction(address, plan.Open); module = ExtensionManager.GetExtension(address)!;
        using (var owner = module.New<Probe>("probe"))
        {
            owner.Score = 41; var notified = owner.Notifications; plan.Mode = "move";
            ExtensionManager.ReloadExtension(address);
            Check(owner.Generation == 0 && owner.Notifications == notified, "A different module reusing the class ID does not adopt old instances.");
            var moved = ExtensionManager.GetExtension("moved-module")!;
            using (var fresh = moved.New<Probe>("probe")) Check(fresh.Generation == 77, "The new module owns only its newly admitted instances.");
            ExtensionManager.UnloadExtension("moved-module"); moved.Dispose();
        }
        ExtensionManager.UnloadExtension(address); module.Dispose();
        var retired = Retired(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Check(!retired.Module.TryGetTarget(out _) && !retired.Plan.TryGetTarget(out _), "Retirement releases module/factory/callback captures from process-wide registries.");
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static (WeakReference<Extension> Module, WeakReference<Plan> Plan) Retired()
    {
        var plan = new Plan(); ExtensionManager.LoadExtensionFromFunction("collect", plan.Open);
        var module = ExtensionManager.GetExtension("collect")!;
        using (var instance = module.New<Probe>("probe")) { }
        ExtensionManager.UnloadExtension("collect"); module.Dispose();
        return (new(module), new(plan));
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static T Reject<T>(Action action) where T : Exception { try { action(); } catch (T error) { return error; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}

sealed class Plan
{
    internal static Action? ConstructorHook;
    internal int Version = 1, Opens;
    internal string Mode = "";
    internal bool Spawn;
    internal Probe? Fresh;
    internal ExtensionInitializationLevel Minimum = ExtensionInitializationLevel.Scene;
    internal readonly List<string> Trace = [];
    internal readonly InvalidOperationException Expected = new("Expected module callback error");
    internal ExtensionInitialization? Open(Extension module)
    {
        Opens++;
        if (Mode == "decline") return null;
        var version = Version;
        return new(Minimum, level =>
        {
            Trace.Add("+" + (int)level);
            if (level != (Minimum < ExtensionInitializationLevel.Scene ? ExtensionInitializationLevel.Scene : Minimum) || Mode == "remove") return;
            if (Mode == "move") { ExtensionManager.LoadExtensionFromFunction("moved-module", new Plan { Version = 77 }.Open); return; }
            if (Mode == "abi") { module.RegisterClass<OtherProbe>("probe", CreateOther, _ => { }, _ => { }); return; }
            module.RegisterClass<Probe>("probe", Create,
                owner => { owner.Generation = version; owner.Score = 0; Probe.FailRestore = Mode == "restore"; if (Mode == "bind") throw Expected; },
                owner => owner.Generation = 0);
            if (Spawn) { Spawn = false; Fresh = module.New<Probe>("probe"); }
        }, level => { Trace.Add("-" + (int)level); if (Mode == "deinitialize" && level == ExtensionInitializationLevel.Scene) throw Expected; });
    }
    internal static Probe Create() => new();
    private static OtherProbe CreateOther() => new();
}
class Probe : Node
{
    internal static bool FailCapture, FailRestore;
    internal static readonly InvalidOperationException Expected = new("Expected stored-state error");
    internal int Score, Generation, Notifications;
    internal Action? Reloaded;
    internal Probe() { Plan.ConstructorHook?.Invoke(); }
    private static readonly PropertyDescriptor<Probe, int> Field = new("Score", owner => FailCapture ? throw Expected : owner.Score,
        (owner, value) => { if (FailRestore) throw Expected; owner.Score = value; }, stored: true);
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() { foreach (var p in base.GetPropertyDescriptors()) yield return p; yield return Field; }
    protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationExtensionReloaded) { Notifications++; Reloaded?.Invoke(); } }
}
sealed class OtherProbe : Node { }
