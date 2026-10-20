using Electron2D;
using System.Runtime.CompilerServices;
using Path = System.IO.Path;

public static class FactoryConstructionChecks
{
    private static Func<Asset>? _resourceFactory;
    private static Func<Actor>? _nodeFactory;
    private static bool _registered;
    private static Asset CreateAsset() => _resourceFactory?.Invoke() ?? new();
    private static Actor CreateActor() => _nodeFactory?.Invoke() ?? new();

    public static void Run()
    {
        if (!_registered)
        {
            ResourceFileTypes.RegisterResource("FactoryConstruction.Asset", CreateAsset);
            ResourceFileTypes.RegisterNode("FactoryConstruction.Actor", CreateActor);
            _registered = true;
        }
        var folder = Path.Combine(Path.GetTempPath(), "e2d-factory-construction-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            using var source = new Asset { ResourceName = "source" };
            var path = Path.Combine(folder, "asset.e2dres");
            ResourceSaver.Save(source, path);
            RejectIndependent(() => new Asset { ResourceName = "borrowed", ResourceSceneUniqueID = "owner_id" }, factory =>
            {
                source.Factory = factory;
                try { using var copy = source.Duplicate(); }
                finally { source.Factory = null; }
            }, value => Check(value.ResourceName == "borrowed" && value.ResourceSceneUniqueID == "owner_id", "Borrowed stored state survives rejection."));
            var borrowedPath = Path.Combine(folder, "borrowed.e2dres");
            RejectIndependent(() => new Asset { ResourcePath = borrowedPath }, factory =>
            {
                _resourceFactory = factory;
                try { using var loaded = ResourceLoader.Load<Asset>(path, ResourceLoader.CacheMode.Ignore); }
                finally { _resourceFactory = null; }
            }, value => Check(ResourceLoader.HasCached(borrowedPath) && ReferenceEquals(ResourceLoader.Load<Asset>(borrowedPath), value), "Borrowed path/cache ownership survives file-factory rejection."));

            using var actor = new Actor { Name = "source" };
            using var scene = new PackedScene(); scene.Pack(actor);
            RejectIndependent(static () => new Actor { Name = "borrowed" }, factory => WithNodeFactory(factory, () => { using var copy = actor.Duplicate(); }));
            RejectIndependent(static () => new Actor { Name = "borrowed" }, factory => WithNodeFactory(factory, () => { using var copy = scene.Instantiate(); }));
            var scenePath = Path.Combine(folder, "actor.e2dscene"); ResourceSaver.Save(scene, scenePath);
            using var loadedScene = ResourceLoader.Load<PackedScene>(scenePath, ResourceLoader.CacheMode.Ignore);
            RejectIndependent(static () => new Actor { Name = "borrowed" }, factory => WithNodeFactory(factory, () => { using var copy = loadedScene.Instantiate(); }));

            foreach (var create in new Func<Asset>[] { static () => new(), static () => Task.Run(static () => new Asset()).GetAwaiter().GetResult(), CreateOnWorker })
            {
                source.Factory = create;
                using var copy = source.Duplicate();
                Check(copy.ResourceName == "source", "Fresh synchronous, Task and engine-worker results are accepted.");
            }
            source.Factory = null;
            using var nested = new Asset { Factory = () => (Asset)source.Duplicate() };
            using var nestedCopy = nested.Duplicate();
            WithNodeFactory(static () => Task.Run(static () => new Actor()).GetAwaiter().GetResult(), () => { using var copy = scene.Instantiate(); });
            WithNodeFactory(CreateNodeOnWorker, () => { using var copy = scene.Instantiate(); });
            CrossedFactories();
            ClosedContext();
            Console.WriteLine("Factory construction: independent concurrent results rejected across resources, files, node copies and packed scenes; Task/worker/nested creation, scene barrier, context retirement and cross-scope isolation passed.");
        }
        finally { _resourceFactory = null; _nodeFactory = null; Directory.Delete(folder, true); }
    }

    public static void RejectIndependent<T>(Func<T> create, Action<Func<T>> invoke, Action<T>? preserved = null) where T : ElectronObject
    {
        using var start = new ManualResetEventSlim(); using var ready = new ManualResetEventSlim();
        T? target = null; Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { start.Wait(); target = create(); }
            catch (Exception error) { failure = error; }
            finally { ready.Set(); }
        })
        { IsBackground = true };
        thread.Start();
        try
        {
            Exception? rejected = null;
            try { invoke(() => { start.Set(); Check(ready.Wait(TimeSpan.FromSeconds(10)), "Independent construction completed."); if (failure is not null) throw failure; return target!; }); }
            catch (Exception error) { rejected = error; }
            Check(rejected is InvalidOperationException && target is { IsDisposed: false }, "An unrelated concurrent result is rejected without disposal.");
            preserved?.Invoke(target!);
        }
        finally { start.Set(); Check(thread.Join(TimeSpan.FromSeconds(10)), "Independent constructor retired."); target?.Dispose(); }
    }

    private static Asset CreateOnWorker()
    {
        Asset? result = null;
        var task = WorkerThreadPool.AddTask(() =>
        {
            var nested = WorkerThreadPool.AddTask(() => result = new Asset());
            WorkerThreadPool.WaitForTaskCompletion(nested);
        });
        WorkerThreadPool.WaitForTaskCompletion(task);
        return result!;
    }

    private static Actor CreateNodeOnWorker()
    {
        Actor? result = null;
        var task = WorkerThreadPool.AddTask(() =>
        {
            var rejected = false;
            using var root = new Node();
            try { using var tree = new SceneTree(root); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected && !root.IsInsideTree, "Scene construction remains inactive on engine workers.");
            result = new Actor();
        });
        WorkerThreadPool.WaitForTaskCompletion(task);
        return result!;
    }

    private static void CrossedFactories()
    {
        using var barrier = new Barrier(2);
        var targets = new Asset[2];
        var tasks = Enumerable.Range(0, 2).Select(index => Task.Run(() =>
        {
            using var source = new Asset
            {
                Factory = () =>
            {
                targets[index] = new Asset();
                Check(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), "Both independent factories constructed their own result.");
                return targets[1 - index];
            }
            };
            var rejected = false;
            try { using var copy = source.Duplicate(); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Concurrent factories cannot exchange ownership of each other's result.");
        })).ToArray();
        try { Task.WaitAll(tasks); Check(targets.All(target => !target.IsDisposed), "Rejected cross-scope results remain caller-owned."); }
        finally { foreach (var target in targets) target?.Dispose(); }
    }

    private static void ClosedContext()
    {
        var weak = CaptureContext(out var context);
        for (var i = 0; i < 3 && weak.IsAlive; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Check(!weak.IsAlive, "A retained execution context does not retain completed factory objects.");
        ExecutionContext.Run(context, static _ => { using var tree = new SceneTree(new Node()); }, null);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CaptureContext(out ExecutionContext context)
    {
        ExecutionContext? captured = null;
        using var source = new Actor();
        Actor? created = null;
        WithNodeFactory(() => { captured = ExecutionContext.Capture(); return created = new Actor(); }, () => { using var copy = source.Duplicate(); });
        context = captured!;
        return new WeakReference(created!);
    }

    private static void WithNodeFactory(Func<Actor> factory, Action action)
    {
        _nodeFactory = factory;
        try { action(); }
        finally { _nodeFactory = null; }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Asset : Resource
    {
        internal Func<Asset>? Factory;
        protected override Resource CreateDuplicateInstance() => Factory?.Invoke() ?? new Asset();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force) { }
    }
    private sealed class Actor : Node
    {
        protected override Func<Node> CreateSceneInstanceFactory() => CreateActor;
    }
}
