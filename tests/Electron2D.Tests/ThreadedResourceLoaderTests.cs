using Electron2D;

internal static class ThreadedResourceLoaderTests
{
    internal static void Run()
    {
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2);
        ResourceFileTypes.RegisterResource("tests.threaded.bundle", CreateBundle);
        ResourceFileTypes.RegisterResource("tests.threaded.leaf", CreateLeaf);
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-threaded-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = System.IO.Path.Combine(directory, "image.e2dres"); using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.Red); ResourceSaver.Save(image, path);
            Check(ResourceLoader.LoadThreadedGetStatus(path, out var absent) == ResourceLoader.ThreadLoadStatus.InvalidResource && absent == 0, "Absent request status.");
            ResourceLoader.LoadThreadedRequest<Image>(path); ResourceLoader.LoadThreadedRequest<Image>(path);
            using var first = ResourceLoader.LoadThreadedGet<Image>(path); Check(first.GetPixel(0, 0) == Colors.Red, "Blocking get publishes a prepared image.");
            Check(ResourceLoader.LoadThreadedGetStatus(path, out var progress) == ResourceLoader.ThreadLoadStatus.Loaded && progress == 1, "Duplicate consumer retains terminal status.");
            Check(ReferenceEquals(first, ResourceLoader.LoadThreadedGet<Image>(path)), "Duplicate request shares final identity."); Check(ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.InvalidResource, "Final get retires request.");
            image.Fill(Colors.Blue); ResourceSaver.Save(image, path); var owner = Environment.CurrentManagedThreadId; var deliveries = 0; first.Changed += _ => { Check(Environment.CurrentManagedThreadId == owner, "Replacement callbacks run on publication caller."); deliveries++; };
            ResourceLoader.LoadThreadedRequest<Image>(path, cacheMode: ResourceLoader.CacheMode.Replace); var replaced = ResourceLoader.LoadThreadedGet<Image>(path); Check(ReferenceEquals(replaced, first) && first.GetPixel(0, 0) == Colors.Blue && deliveries != 0, "Compatible replacement identity and data.");
            ResourceLoader.LoadThreadedRequest<Image>(path, cacheMode: ResourceLoader.CacheMode.IgnoreDeep); using var independent = ResourceLoader.LoadThreadedGet<Image>(path); Check(!ReferenceEquals(independent, first) && ReferenceEquals(ResourceLoader.GetCachedRef<Image>(path), first), "Deep ignore preserves live cache.");
            Action<Resource> collectDuringChange = _ => Reject<InvalidOperationException>(() => ResourceLoader.LoadThreadedGet<Image>(path)); first.Changed += collectDuringChange;
            try
            {
                ResourceLoader.LoadThreadedRequest<Image>(path, cacheMode: ResourceLoader.CacheMode.Replace); ResourceLoader.LoadThreadedRequest<Image>(path, cacheMode: ResourceLoader.CacheMode.Replace);
                Check(ReferenceEquals(ResourceLoader.LoadThreadedGet<Image>(path), first) && ReferenceEquals(ResourceLoader.LoadThreadedGet<Image>(path), first), "Publication callbacks reject recursive collection without consuming either duplicate.");
            }
            finally { first.Changed -= collectDuringChange; }
            var missing = System.IO.Path.Combine(directory, "missing.e2dres"); ResourceLoader.LoadThreadedRequest<Resource>(missing);
            Reject<FileNotFoundException>(() => ResourceLoader.LoadThreadedGet(missing)); Check(ResourceLoader.LoadThreadedGetStatus(missing) == ResourceLoader.ThreadLoadStatus.InvalidResource, "Failed get releases retained failure.");
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel(); Reject<OperationCanceledException>(() => ResourceLoader.LoadThreadedRequest<Image>(path, cancellationToken: cancellation.Token));
            ParallelDependencies(directory);
            ArchivePolicies(directory); DiamondOwnership(directory); ShaderReferences(directory); CancelDependencies(directory); SceneOwner(directory); EdgeCases(directory); RequestCapacity(directory, path);
        }
        finally { Directory.Delete(directory, true); ProjectSettings.Reset(ProjectSettings.WorkerPoolMaxThreads); }
        Console.WriteLine("Threaded resources: blocking get, duplicate ownership, status, replacement callbacks/cache identity, deep ignore, failures and pre-cancellation passed.");
    }
    private sealed class Bundle : Resource
    {
        internal Resource?[] Items = [];
        protected override Resource CreateDuplicateInstance() => new Bundle();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> copy, Func<Resource?, Resource?> force) => ((Bundle)target).Items = Items.Select(copy).ToArray();
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
        {
            foreach (var descriptor in base.GetPropertyDescriptors()) yield return descriptor;
            yield return new PropertyDescriptor<Bundle, Resource?[]>(nameof(Items), r => r.Items, (r, value) => r.Items = value, _ => [], stored: true);
        }
    }
    private sealed class Leaf : Resource
    {
        internal int Value;
        protected override Resource CreateDuplicateInstance() => new Leaf();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> copy, Func<Resource?, Resource?> force) => ((Leaf)target).Value = Value;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
        {
            foreach (var descriptor in base.GetPropertyDescriptors()) yield return descriptor;
            yield return new PropertyDescriptor<Leaf, int>(nameof(Value), r => r.Value, (r, value) => r.Value = value, _ => 0, stored: true);
        }
    }
    private static Bundle CreateBundle() => new();
    private static Leaf CreateLeaf() => new();
    private sealed class LeafLoader : ResourceFormatLoader
    {
        internal readonly CountdownEvent Entered = new(2);
        internal readonly ManualResetEventSlim Release = new();
        internal readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> Threads = new();
        internal readonly List<Leaf> Produced = [];
        public override string[] GetRecognizedExtensions() => ["leaf"];
        public override bool HandlesType(Type type) => type.IsAssignableFrom(typeof(Leaf));
        public override Type? GetResourceType(string path) => typeof(Leaf);
        public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode)
        {
            Check(useSubThreads, "Subthread option reaches format hooks."); Threads[Environment.CurrentManagedThreadId] = true; Entered.Signal(); Check(Release.Wait(TimeSpan.FromSeconds(10)), "Parallel leaf release timed out.");
            var result = new Leaf { Value = int.Parse(File.ReadAllText(path)) }; lock (Produced) Produced.Add(result); return result;
        }
        protected override void Dispose(bool disposing) { Entered.Dispose(); Release.Dispose(); base.Dispose(disposing); }
    }
    private static void ParallelDependencies(string directory)
    {
        var aPath = System.IO.Path.Combine(directory, "a.leaf"); var bPath = System.IO.Path.Combine(directory, "b.leaf"); File.WriteAllText(aPath, "17"); File.WriteAllText(bPath, "23");
        var path = System.IO.Path.Combine(directory, "bundle.e2dres");
        using (var a = new Leaf { ResourcePath = aPath }) using (var b = new Leaf { ResourcePath = bPath }) using (var source = new Bundle { Items = [a, b, a] }) ResourceSaver.Save(source, path);
        using var loader = new LeafLoader(); ResourceLoader.AddResourceFormatLoader(loader, true);
        try
        {
            ResourceLoader.LoadThreadedRequest<Bundle>(path, useSubThreads: true);
            Check(loader.Entered.Wait(TimeSpan.FromSeconds(10)) && loader.Threads.Count == 2, "Both dependencies genuinely load in parallel.");
            Check(!ResourceLoader.HasCached(aPath) && !ResourceLoader.HasCached(bPath) && ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.InProgress, "Preparation does not publish partial cache.");
            loader.Release.Set(); var result = ResourceLoader.LoadThreadedGet<Bundle>(path);
            Check(result.Items.Length == 3 && ReferenceEquals(result.Items[0], result.Items[2]) && ((Leaf)result.Items[0]!).Value == 17 && ((Leaf)result.Items[1]!).Value == 23, "Parallel graph data and aliases.");
            Check(ReferenceEquals(ResourceLoader.GetCachedRef<Leaf>(aPath), result.Items[0]), "Dependency cache publication.");
            result.Dispose(); Check(loader.Produced.Count == 2 && loader.Produced.All(r => r.IsDisposed), "Parent owns each created dependency exactly once.");
        }
        finally { loader.Release.Set(); ResourceLoader.RemoveResourceFormatLoader(loader); }
    }
    private static void ArchivePolicies(string directory)
    {
        var childPath = System.IO.Path.Combine(directory, "child.e2dres"); var path = System.IO.Path.Combine(directory, "policies.e2dres");
        using var leaf = new Leaf { Value = 1 }; ResourceSaver.Save(leaf, childPath); leaf.ResourcePath = childPath;
        using var original = new Bundle { Items = [leaf, leaf] }; ResourceSaver.Save(original, path); original.Items = [original, leaf, leaf]; ResourceSaver.Save(original, path);
        using var cached = ResourceLoader.Load<Bundle>(path); Check(ReferenceEquals(cached.Items[0], cached), "Stored root cycle.");
        leaf.Value = 9; ResourceSaver.Save(leaf, childPath);
        ResourceLoader.LoadThreadedRequest<Bundle>(path, useSubThreads: true, cacheMode: ResourceLoader.CacheMode.Replace);
        Check(ReferenceEquals(ResourceLoader.LoadThreadedGet<Bundle>(path), cached) && ReferenceEquals(cached.Items[0], cached) && ((Leaf)cached.Items[1]!).Value == 9, "Ordinary replacement keeps current external cache identity.");
        leaf.Value = 17; ResourceSaver.Save(leaf, childPath); leaf.Value = 9;
        ResourceLoader.LoadThreadedRequest<Bundle>(path, useSubThreads: true, cacheMode: ResourceLoader.CacheMode.ReplaceDeep);
        var refreshed = ResourceLoader.LoadThreadedGet<Bundle>(path); Check(ReferenceEquals(refreshed, cached) && ReferenceEquals(refreshed.Items[0], cached) && ReferenceEquals(refreshed.Items[1], leaf) && leaf.Value == 17 && ReferenceEquals(refreshed.Items[1], refreshed.Items[2]), "Deep replacement remaps cycles, aliases and compatible external identities.");
        ResourceLoader.LoadThreadedRequest<Bundle>(path, useSubThreads: true, cacheMode: ResourceLoader.CacheMode.IgnoreDeep); using var isolated = ResourceLoader.LoadThreadedGet<Bundle>(path);
        Check(!ReferenceEquals(isolated, cached) && ReferenceEquals(isolated.Items[0], isolated) && !ReferenceEquals(isolated.Items[1], leaf), "Deep ignore owns independent graph and retains its root cycle.");
        Check(!leaf.IsDisposed, "Borrowed external resource remains live.");
    }
    private static void DiamondOwnership(string directory)
    {
        var leafPath = System.IO.Path.Combine(directory, "diamond-leaf.e2dres");
        var aPath = System.IO.Path.Combine(directory, "diamond-a.e2dres"); var bPath = System.IO.Path.Combine(directory, "diamond-b.e2dres"); var rootPath = System.IO.Path.Combine(directory, "diamond.e2dres");
        using (var leaf = new Leaf { Value = 41 }) using (var a = new Bundle { Items = [leaf] }) using (var b = new Bundle { Items = [leaf] }) using (var source = new Bundle { Items = [a, b] })
        {
            ResourceSaver.Save(leaf, leafPath); leaf.ResourcePath = leafPath;
            ResourceSaver.Save(a, aPath); a.ResourcePath = aPath; ResourceSaver.Save(b, bPath); b.ResourcePath = bPath; ResourceSaver.Save(source, rootPath);
        }
        ResourceLoader.LoadThreadedRequest<Bundle>(rootPath, useSubThreads: true);
        var root = ResourceLoader.LoadThreadedGet<Bundle>(rootPath); var aResult = (Bundle)root.Items[0]!; var bResult = (Bundle)root.Items[1]!; var shared = (Leaf)aResult.Items[0]!;
        Check(ReferenceEquals(shared, bResult.Items[0]) && shared.Value == 41, "Parallel diamond shares its new external identity.");
        using var retained = (Bundle)bResult.Duplicate(); root.Dispose();
        Check(!shared.IsDisposed && ReferenceEquals(shared, retained.Items[0]), "Either parent copy retains a shared external root after graph disposal.");
        retained.Dispose(); Check(shared.IsDisposed, "Shared external root releases after its final parent lease.");
    }
    private sealed class ShaderLoader(string texturePath) : ResourceFormatLoader
    {
        public override string[] GetRecognizedExtensions() => ["shader-test"];
        public override bool HandlesType(Type type) => type.IsAssignableFrom(typeof(Shader));
        public override Type? GetResourceType(string path) => typeof(Shader);
        public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode)
        {
            using var input = typeof(ThreadedResourceLoaderTests).Assembly.GetManifestResourceStream("TestShaders.TextureGlsl.spv")!; using var memory = new MemoryStream(); input.CopyTo(memory);
            var shader = Shader.CreateFromSPIRV(memory.ToArray());
            try { shader.SetDefaultTextureParameter("colorMap", ResourceLoader.Load<ImageTexture>(texturePath, cacheMode)); return shader; }
            catch { shader.Dispose(); throw; }
        }
    }
    private static void ShaderReferences(string directory)
    {
        var texturePath = System.IO.Path.Combine(directory, "shader.png"); var path = System.IO.Path.Combine(directory, "shader.shader-test");
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.Red); image.SavePNG(texturePath); using var cached = ResourceLoader.Load<ImageTexture>(texturePath);
        image.Fill(Colors.Blue); image.SavePNG(texturePath); using var loader = new ShaderLoader(texturePath); ResourceLoader.AddResourceFormatLoader(loader, true);
        try
        {
            ResourceLoader.LoadThreadedRequest<Shader>(path, useSubThreads: true, cacheMode: ResourceLoader.CacheMode.ReplaceDeep); using var result = ResourceLoader.LoadThreadedGet<Shader>(path);
            using var pixels = cached.GetImage()!; Check(ReferenceEquals(result.GetDefaultTextureParameter("colorMap"), cached) && pixels.GetPixel(0, 0) == Colors.Blue, "Opaque shader defaults remap to refreshed cached dependency identities.");
            result.Dispose(); Check(!cached.IsDisposed, "Shader publication keeps pre-existing cached dependency borrowed.");
        }
        finally { ResourceLoader.RemoveResourceFormatLoader(loader); }
    }
    private static void CancelDependencies(string directory)
    {
        var aPath = System.IO.Path.Combine(directory, "cancel-a.leaf"); var bPath = System.IO.Path.Combine(directory, "cancel-b.leaf"); var path = System.IO.Path.Combine(directory, "cancel.e2dres"); File.WriteAllText(aPath, "1"); File.WriteAllText(bPath, "2");
        using (var a = new Leaf { ResourcePath = aPath }) using (var b = new Leaf { ResourcePath = bPath }) using (var root = new Bundle { Items = [a, b] }) ResourceSaver.Save(root, path);
        using var loader = new LeafLoader(); using var cancellation = new CancellationTokenSource(); ResourceLoader.AddResourceFormatLoader(loader, true);
        try
        {
            ResourceLoader.LoadThreadedRequest<Bundle>(path, true, cancellationToken: cancellation.Token); Check(loader.Entered.Wait(TimeSpan.FromSeconds(10)), "Cancellation load starts both leaves."); cancellation.Cancel(); loader.Release.Set();
            Reject<OperationCanceledException>(() => ResourceLoader.LoadThreadedGet(path)); Check(loader.Produced.All(resource => resource.IsDisposed) && !ResourceLoader.HasCached(aPath) && !ResourceLoader.HasCached(bPath), "Cancellation drains workers and disposes all new dependencies without cache publication.");
        }
        finally { loader.Release.Set(); ResourceLoader.RemoveResourceFormatLoader(loader); }
    }
    private static void SceneOwner(string directory)
    {
        var path = System.IO.Path.Combine(directory, "owner.e2dres"); using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.Green); ResourceSaver.Save(image, path);
        using var root = new Node(); using var tree = new SceneTree(root);
        ResourceLoader.LoadThreadedRequest<Image>(path, publicationTree: tree);
        Task.Run(() => Reject<InvalidOperationException>(() => ResourceLoader.LoadThreadedGet<Image>(path))).GetAwaiter().GetResult();
        Check(SpinWait.SpinUntil(() => ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.Loaded, TimeSpan.FromSeconds(10)), "Owner status poll publishes without blocking.");
        using var result = ResourceLoader.LoadThreadedGet<Image>(path); Check(result.GetPixel(0, 0) == Colors.Green, "Manual scene owner consumes published data.");
    }
    private static void EdgeCases(string directory)
    {
        var path = System.IO.Path.Combine(directory, "edge.e2dres"); using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.Red); ResourceSaver.Save(image, path);
        ResourceLoader.LoadThreadedRequest<Image>(path); ResourceLoader.LoadThreadedRequest<Image>(path);
        var one = Task.Run(() => ResourceLoader.LoadThreadedGet<Image>(path)); var two = Task.Run(() => ResourceLoader.LoadThreadedGet<Image>(path)); Check(Task.WaitAll([one, two], TimeSpan.FromSeconds(10)) && ReferenceEquals(one.Result, two.Result), "Concurrent duplicate collectors safely share publication and retire once.");
        using var result = one.Result;
        ResourceLoader.LoadThreadedRequest<Image>(path); Reject<InvalidOperationException>(() => ResourceLoader.LoadThreadedRequest<Image>(path, cacheMode: ResourceLoader.CacheMode.Ignore));
        Reject<ArgumentException>(() => ResourceLoader.LoadThreadedGet<Leaf>(path)); Check(ReferenceEquals(ResourceLoader.LoadThreadedGet<Image>(path), result), "Wrong result type does not consume request.");
        ResourceLoader.LoadThreadedRequest<Image>(path); Check(ResourceLoader.LoadThreadedGetStatus(path) is ResourceLoader.ThreadLoadStatus.InProgress or ResourceLoader.ThreadLoadStatus.Loaded, "Reusable request lifecycle.");
        Check(SpinWait.SpinUntil(() => ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.Loaded, TimeSpan.FromSeconds(10)), "Terminal poll reached.");
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) { Check(ResourceLoader.LoadThreadedGetStatus(path, out var ratio) == ResourceLoader.ThreadLoadStatus.Loaded && ratio == 1, "Stable polling."); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Terminal status/progress polling allocates zero managed bytes."); _ = ResourceLoader.LoadThreadedGet<Image>(path);
        var aPath = System.IO.Path.Combine(directory, "cycle-a.e2dres"); var bPath = System.IO.Path.Combine(directory, "cycle-b.e2dres");
        using (var a = new Bundle()) using (var b = new Bundle()) { a.SetPathCache(aPath); b.SetPathCache(bPath); a.Items = [b]; b.Items = [a]; ResourceSaver.Save(a, aPath); ResourceSaver.Save(b, bPath); }
        ResourceLoader.LoadThreadedRequest<Bundle>(aPath, useSubThreads: true); Reject<InvalidDataException>(() => ResourceLoader.LoadThreadedGet(aPath)); Check(!ResourceLoader.HasCached(aPath) && !ResourceLoader.HasCached(bPath), "External cycle rejects and cleans without publishing cache.");
        for (var i = 0; i <= 1024; i++)
        {
            var tree = new SceneTree(new Node()); ResourceLoader.LoadThreadedRequest<Image>(path, publicationTree: tree); tree.Dispose(); Reject<ObjectDisposedException>(() => ResourceLoader.LoadThreadedGet<Image>(path));
        }
        Check(ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.InvalidResource, "Closed-owner failures retire requests and more than the default pool ticket capacity.");
        ResourceLoader.LoadThreadedRequest<Image>(path); var getter = WorkerThreadPool.AddTask(() => { Check(ReferenceEquals(ResourceLoader.LoadThreadedGet<Image>(path), result), "Worker consumes an older resource request safely."); }, highPriority: true); WorkerThreadPool.WaitForTaskCompletion(getter);
    }
    private static void RequestCapacity(string directory, string source)
    {
        var paths = Enumerable.Range(0, 128).Select(index => System.IO.Path.Combine(directory, "capacity-" + index + ".e2dres")).ToArray();
        foreach (var path in paths)
        {
            File.Copy(source, path); ResourceLoader.LoadThreadedRequest<Image>(path);
            Check(SpinWait.SpinUntil(() => ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.Loaded, TimeSpan.FromSeconds(10)), "Retained request completes within capacity.");
        }
        Reject<InvalidOperationException>(() => ResourceLoader.LoadThreadedRequest<Image>(System.IO.Path.Combine(directory, "overflow.e2dres")));
        foreach (var path in paths) ResourceLoader.LoadThreadedGet<Image>(path).Dispose();
        ResourceLoader.LoadThreadedRequest<Image>(paths[0]); using var result = ResourceLoader.LoadThreadedGet<Image>(paths[0]); Check(!result.IsDisposed, "Collection releases service capacity for another request.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
