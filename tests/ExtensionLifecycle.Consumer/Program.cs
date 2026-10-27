using Electron2D;

var backend = args.Contains("--gpu") ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
Engine.IsEditorHint = args.Contains("--editor");
ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, args.Contains("--threaded"));
var module = new Module(backend);
var manager = Engine.GetSingleton(nameof(ExtensionManager));
Check(ReferenceEquals(manager, Engine.GetSingleton(nameof(ExtensionManager))), "Manager has one retained service identity.");
Reject<InvalidOperationException>(manager.Dispose);
Reject<InvalidOperationException>(() => Engine.UnregisterSingleton(nameof(ExtensionManager)));
const string address = "function:physical-module";
var loaded = 0; var unloading = 0;
Action<Extension> onLoaded = extension =>
{
    Check(ReferenceEquals(extension, module.Resource) && extension.IsLibraryOpen(), "Loaded event receives the open module.");
    Check(!ExtensionManager.IsExtensionLoaded(address) && extension.ResourcePath.Length == 0,
        "Loaded event precedes address and path publication.");
    loaded++;
};
Action<Extension> onUnloading = extension =>
{
    Check(ReferenceEquals(ExtensionManager.GetExtension(address), extension) && module.Space.IsValid(),
        "Unloading event precedes numerical-resource retirement and address removal.");
    unloading++;
};
ExtensionManager.ExtensionLoaded += onLoaded; ExtensionManager.ExtensionUnloading += onUnloading;
var directory = Directory.CreateTempSubdirectory("e2d-extension-");
try
{
    Check(ExtensionManager.LoadExtensionFromFunction(address, module.Open) == ExtensionManager.LoadStatus.Ok, "Load succeeded.");
    var extension = ExtensionManager.GetExtension(address)!;
    Check(ReferenceEquals(extension, module.Resource) && loaded == 1 && extension.ResourcePath == address, "Stable published resource identity.");
    Check(extension.GetMinimumLibraryInitializationLevel() == ExtensionInitializationLevel.Scene, "Minimum replacement level retained.");
    Check(ExtensionManager.LoadExtensionFromFunction(address, _ => throw new Exception("Duplicate entry must not execute")) == ExtensionManager.LoadStatus.AlreadyLoaded,
        "Duplicate address preserves the original entry.");
    Check(ExtensionManager.GetLoadedExtensions().Contains(address), "Address snapshot contains the registered module.");
    Reject<InvalidOperationException>(extension.Dispose);
    using (var copy = (Extension)extension.Duplicate()) Check(!copy.IsLibraryOpen(), "Duplication never clones executable state.");
    var path = System.IO.Path.Combine(directory.FullName, "module.e2dres"); ResourceSaver.Save(extension, path);
    using (var restored = ResourceLoader.Load<Extension>(path, ResourceLoader.CacheMode.Ignore))
        Check(!restored.IsLibraryOpen(), "Archive reconstruction restores metadata without executing a module.");
    extension.ResourcePath = path;
    Check(ReferenceEquals(ExtensionManager.GetExtension(address), extension), "Resource path edits do not retarget the registry address.");
    module.VerifyMotion();
    Check(ExtensionManager.UnloadExtension(address) == ExtensionManager.LoadStatus.Ok && unloading == 1, "Unload succeeded once.");
    Check(!module.Space.IsValid() && ExtensionManager.GetExtension(address) is null, "Module numerical resources and registry entry retired.");
    Check(extension.IsLibraryOpen() && extension.GetMinimumLibraryInitializationLevel() == ExtensionInitializationLevel.Scene,
        "A retained unloaded resource still owns its open entry.");
    var levels = Engine.IsEditorHint ? new[] { 0, 1, 2, 3 } : new[] { 0, 1, 2 };
    Check(module.Trace.SequenceEqual(levels.Select(i => "+" + i).Concat(levels.Reverse().Select(i => "-" + i))), "Complete ascending/reverse lifecycle, including levels below the replacement minimum.");
    extension.Dispose(); Reject<ObjectDisposedException>(() => extension.IsLibraryOpen());
    Check(ExtensionManager.UnloadExtension(address) == ExtensionManager.LoadStatus.NotLoaded, "Repeated unload is observational.");
}
finally
{
    ExtensionManager.ExtensionLoaded -= onLoaded; ExtensionManager.ExtensionUnloading -= onUnloading;
    if (ExtensionManager.IsExtensionLoaded(address)) ExtensionManager.UnloadExtension(address);
    module.Resource?.Dispose(); directory.Delete(true);
}
Failures(backend);
Console.WriteLine($"{{\"ok\":true,\"backend\":\"{backend}\",\"editor\":{Engine.IsEditorHint.ToString().ToLowerInvariant()},\"steps\":128,\"ownerBytes\":0,\"allBytes\":0}}");

static void Failures(PhysicsServer.Backend backend)
{
    const string path = "function:failures";
    Extension? resource = null;
    Check(ExtensionManager.LoadExtensionFromFunction(path, value => { resource = value; return null; }) == ExtensionManager.LoadStatus.Failed && resource!.IsDisposed,
        "Declined entry retires its unpublished resource.");
    Reject<ArgumentException>(() => ExtensionManager.LoadExtensionFromFunction(path, value => { resource = value; return default(ExtensionInitialization); }));
    Check(resource!.IsDisposed && !ExtensionManager.IsExtensionLoaded(path), "Invalid descriptor publishes nothing.");
    Reject<ArgumentNullException>(() => ExtensionManager.LoadExtensionFromFunction(null!, _ => null));
    Reject<ArgumentNullException>(() => ExtensionManager.LoadExtensionFromFunction(path, null!));
    using (var closed = new Extension()) { Check(!closed.IsLibraryOpen(), "New module is closed."); Reject<InvalidOperationException>(() => closed.GetMinimumLibraryInitializationLevel()); }
    using (var missingFactory = new MissingFactory()) Reject<NotSupportedException>(() => missingFactory.Duplicate());
    using (var missingCopy = new MissingCopy()) Reject<NotSupportedException>(() => missingCopy.Duplicate());
    Reject<ArgumentOutOfRangeException>(() => new ExtensionInitialization((ExtensionInitializationLevel)999, _ => { }, _ => { }));
    Reject<ArgumentNullException>(() => new ExtensionInitialization(ExtensionInitializationLevel.Core, null!, _ => { }));
    var failure = new InvalidOperationException("Expected entry failure");
    Check(ReferenceEquals(Reject<InvalidOperationException>(() => ExtensionManager.LoadExtensionFromFunction(path, _ => throw failure)), failure), "Original factory error preserved.");
    var failedWorld = new Module(backend) { InitializationFailure = failure };
    Check(ReferenceEquals(Reject<InvalidOperationException>(() => ExtensionManager.LoadExtensionFromFunction(path, failedWorld.Open)), failure)
        && !failedWorld.Space.IsValid() && !failedWorld.Body.IsValid() && failedWorld.Resource!.IsDisposed,
        "Failure after real numerical-world creation unwinds the failed level and retires its module resource.");
    var cleanupFailure = new InvalidOperationException("Expected cleanup failure");
    var trace = new List<string>();
    var errors = Reject<AggregateException>(() => ExtensionManager.LoadExtensionFromFunction(path, value =>
    {
        resource = value;
        return new ExtensionInitialization(ExtensionInitializationLevel.Scene,
            level => { trace.Add("+" + (int)level); if (level == ExtensionInitializationLevel.Servers) throw failure; },
            level => { trace.Add("-" + (int)level); if (level == ExtensionInitializationLevel.Servers) throw cleanupFailure; });
    }));
    Check(errors.InnerExceptions.SequenceEqual(new[] { failure, cleanupFailure }) && trace.SequenceEqual(new[] { "+0", "+1", "-1", "-0" }) && resource!.IsDisposed,
        "Failed level and earlier levels unwind once; original and cleanup errors survive.");
    ExtensionInitialization Empty() => new(ExtensionInitializationLevel.Scene, _ => { }, _ => { });
    Action<Extension> loadError = _ => throw failure;
    ExtensionManager.ExtensionLoaded += loadError;
    try { Check(ReferenceEquals(Reject<InvalidOperationException>(() => ExtensionManager.LoadExtensionFromFunction(path, _ => Empty())), failure), "Original loaded-observer error preserved."); }
    finally { ExtensionManager.ExtensionLoaded -= loadError; }
    resource = ExtensionManager.GetExtension(path);
    Check(resource is not null && resource.IsLibraryOpen(), "Loaded-observer failure retains the published module.");
    ExtensionManager.UnloadExtension(path); resource!.Dispose();
    var coreReleased = false;
    ExtensionManager.LoadExtensionFromFunction(path, value =>
    {
        resource = value;
        return new ExtensionInitialization(ExtensionInitializationLevel.Core, _ => { }, level =>
        { if (level == ExtensionInitializationLevel.Scene) throw cleanupFailure; if (level == ExtensionInitializationLevel.Core) coreReleased = true; });
    });
    Action<Extension> unloadError = _ => throw failure;
    ExtensionManager.ExtensionUnloading += unloadError;
    try { errors = Reject<AggregateException>(() => ExtensionManager.UnloadExtension(path)); }
    finally { ExtensionManager.ExtensionUnloading -= unloadError; }
    Check(errors.InnerExceptions.SequenceEqual(new[] { failure, cleanupFailure }) && coreReleased && !ExtensionManager.IsExtensionLoaded(path),
        "Unload continues after observer/deinitialization errors and commits retirement.");
    resource!.Dispose();
    var manager = Engine.GetSingleton(nameof(ExtensionManager)); var emissions = 0;
    Action<Extension> observed = _ => emissions++;
    ExtensionManager.ExtensionLoaded += observed; ExtensionManager.ExtensionUnloading += observed;
    try
    {
        manager.SetBlockSignals(true);
        ExtensionManager.LoadExtensionFromFunction(path, _ => Empty());
        resource = ExtensionManager.GetExtension(path); ExtensionManager.UnloadExtension(path); resource!.Dispose();
        Check(emissions == 0, "Signal blocking suppresses both ordinary module emissions without suppressing lifecycle work.");
    }
    finally { manager.SetBlockSignals(false); ExtensionManager.ExtensionLoaded -= observed; ExtensionManager.ExtensionUnloading -= observed; }
    Action<Extension>? wrapper = null;
    using (var connection = EventConnection.Subscribe<Extension>(
        handler => { wrapper = handler; ExtensionManager.ExtensionLoaded += handler; },
        handler => ExtensionManager.ExtensionLoaded -= handler, _ => { }))
    {
        Check(connection.IsConnected, "Connection sees its registered module publisher.");
        ExtensionManager.ExtensionLoaded -= wrapper;
        Check(!connection.IsConnected, "Explicit wrapper removal retires the connection's publisher membership.");
    }
    ExtensionManager.LoadExtensionFromFunction(path, _ =>
    {
        Reject<InvalidOperationException>(() => ExtensionManager.LoadExtensionFromFunction(path, _ => Empty()));
        Check(ExtensionManager.LoadExtensionFromFunction(path + ":child", _ => Empty()) == ExtensionManager.LoadStatus.Ok, "Nested different-address loading is supported.");
        return Empty();
    });
    Exception? foreignLoad = null; Exception? foreignUnload = null;
    var worker = new Thread(() =>
    {
        try { ExtensionManager.LoadExtensionFromFunction(path + ":foreign", _ => Empty()); } catch (Exception error) { foreignLoad = error; }
        try { ExtensionManager.UnloadExtension(path); } catch (Exception error) { foreignUnload = error; }
        Check(ExtensionManager.GetLoadedExtensions().Contains(path), "Foreign registry snapshots remain readable.");
    });
    worker.Start(); worker.Join();
    Check(foreignLoad is InvalidOperationException && foreignUnload is InvalidOperationException, "Foreign mutation rejects before entry effects.");
    foreach (var key in new[] { path, path + ":child" }) { var retained = ExtensionManager.GetExtension(key)!; ExtensionManager.UnloadExtension(key); retained.Dispose(); }
    Check(ExtensionManager.GetLoadedExtensions().Length == 0, "Every test module is retired.");
}

static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
static T Reject<T>(Action operation) where T : Exception
{
    try { operation(); } catch (T error) { return error; }
    throw new InvalidOperationException("Expected " + typeof(T).Name);
}

sealed class Module(PhysicsServer.Backend backend)
{
    internal Extension? Resource;
    internal RID Space, Body;
    internal readonly List<string> Trace = [];
    private CircleShape? _shape;
    internal Exception? InitializationFailure;
    internal ExtensionInitialization? Open(Extension resource)
    {
        Resource = resource;
        if (resource.IsLibraryOpen()) throw new InvalidOperationException("Entry must receive a closed resource.");
        return new(ExtensionInitializationLevel.Scene, Initialize, Deinitialize);
    }
    private void Initialize(ExtensionInitializationLevel level)
    {
        Trace.Add("+" + (int)level);
        if (level != ExtensionInitializationLevel.Scene) return;
        Space = PhysicsServer.SpaceCreate(backend);
        PhysicsServer.SpaceSetActive(Space, true);
        PhysicsServer.AreaSetGravity(Space, 0); PhysicsServer.AreaSetLinearDamp(Space, 0); PhysicsServer.AreaSetAngularDamp(Space, 0);
        _shape = new CircleShape { Radius = 4 }; Body = PhysicsServer.BodyCreate();
        PhysicsServer.BodyAddShape(Body, _shape.GetRID()); PhysicsServer.BodySetCanSleep(Body, false);
        PhysicsServer.BodySetLinearVelocity(Body, new(60, 0)); PhysicsServer.BodySetSpace(Body, Space);
        if (PhysicsServer.SpaceGetBackend(Space) != backend || PhysicsServer.SpaceGetBackendFallbackReason(Space) is not null)
            throw new InvalidOperationException("Module physics selection changed.");
        if (InitializationFailure is not null) throw InitializationFailure;
    }
    internal void VerifyMotion()
    {
        GC.Collect(); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 128; i++) Step();
        var x = PhysicsServer.BodyGetTransform(Body).Origin.X;
        var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) Step();
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        var travel = PhysicsServer.BodyGetTransform(Body).Origin.X - x;
        if (owner != 0 || all != 0 || MathF.Abs(travel - 128) > .01f)
            throw new InvalidOperationException($"Module motion/allocations failed: {travel} travel, {owner}/{all} B.");
        void Step() { PhysicsServer.SpaceStep(Space, 1d / 60); _ = PhysicsServer.BodyGetTransform(Body); }
    }
    private void Deinitialize(ExtensionInitializationLevel level)
    {
        Trace.Add("-" + (int)level);
        if (level != ExtensionInitializationLevel.Scene) return;
        if (Body.IsValid()) { PhysicsServer.FreeRID(Body); Body = default; }
        if (Space.IsValid()) { PhysicsServer.FreeRID(Space); Space = default; }
        _shape?.Dispose(); _shape = null;
    }
}

sealed class MissingFactory : Extension { }
sealed class MissingCopy : Extension
{
    protected override Resource CreateDuplicateInstance() => new MissingCopy();
}
