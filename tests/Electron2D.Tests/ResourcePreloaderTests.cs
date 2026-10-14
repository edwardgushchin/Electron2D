using Electron2D;

internal static class ResourcePreloaderTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }

    internal static void Run()
    {
        using var first = new Gradient();
        using var second = new Gradient();
        using var holder = new ResourcePreloader();
        holder.AddResource("asset", first); holder.AddResource("asset 2", second); holder.AddResource("asset", first);
        Check(holder.GetResourceList().SequenceEqual(["asset", "asset 2", "asset 3"]), "First free suffix and insertion order.");
        holder.RemoveResource("asset 2"); holder.AddResource("asset", second);
        Check(holder.GetResourceList().SequenceEqual(["asset", "asset 3", "asset 2"]), "Reused suffix goes to the end.");
        holder.RenameResource("asset 2", "asset");
        Check(ReferenceEquals(holder.GetResource("asset 2"), second), "Rename considers source name free.");
        holder.RenameResource("asset", "asset");
        Check(holder.GetResourceList().SequenceEqual(["asset 3", "asset 2", "asset"]), "Rename to self moves entry to end.");
        holder.RenameResource("asset 2", "asset 3");
        Check(ReferenceEquals(holder.GetResource("asset 3 2"), second), "Rename collision suffix.");
        holder.AddResource("ASSET", second); Check(holder.HasResource("ASSET") && holder.HasResource("asset") && !holder.HasResource("Asset"), "Ordinal case-sensitive membership.");
        var copy = holder.GetResourceList(); copy[0] = "edited"; Check(!holder.HasResource("edited"), "List is caller-owned.");
        Reject<KeyNotFoundException>(() => holder.GetResource("missing"));
        Reject<KeyNotFoundException>(() => holder.RemoveResource("missing"));
        Reject<KeyNotFoundException>(() => holder.RenameResource("missing", "new"));
        Reject<ArgumentNullException>(() => holder.AddResource(null!, first));
        Reject<ArgumentNullException>(() => holder.AddResource("null", null!));
        Reject<ArgumentNullException>(() => holder.GetResource(null!));
        Reject<ArgumentNullException>(() => holder.HasResource(null!));
        Reject<ArgumentException>(() => holder.AddResource("\uD800", first));
        var old = holder.GetResourceList(); Reject<ArgumentException>(() => holder.RenameResource("asset", "\uDC00"));
        Check(old.SequenceEqual(holder.GetResourceList()), "Rejected rename preserves state.");
        using (var disposed = new Gradient()) { disposed.Dispose(); Reject<ObjectDisposedException>(() => holder.AddResource("dead", disposed)); }
        using (var borrowed = new Gradient())
        {
            holder.AddResource("borrowed", borrowed); borrowed.Dispose();
            Check(holder.HasResource("borrowed") && ReferenceEquals(holder.GetResource("borrowed"), borrowed), "Borrowed logical disposal does not erase membership or identity.");
            holder.RemoveResource("borrowed");
        }
        for (var i = 0; i < 128; i++) { _ = holder.GetResource("asset"); _ = holder.HasResource("asset"); }
        var start = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) { if (!ReferenceEquals(holder.GetResource("asset"), first) || !holder.HasResource("asset")) throw new InvalidOperationException(); }
        Check(GC.GetAllocatedBytesForCurrentThread() - start == 0, "128 prepared lookup/Has cycles allocate zero owner bytes.");
        using var local = new Gradient { ResourceLocalToScene = true }; local.SetColor(0, new Color(.25f, .5f, .75f));
        using var shared = new Gradient();
        using var source = new ResourcePreloader { Name = "Assets" };
        source.AddResource("local", local); source.AddResource("alias", local); source.AddResource("shared", shared);
        foreach (var name in new[] { "", "a/b", "nul\0key", "🌏", "\U00010000", "\uE000" }) source.AddResource(name, shared);
        using var packed = new PackedScene(); packed.Pack(source);
        using var one = (ResourcePreloader)packed.Instantiate(); using var two = (ResourcePreloader)packed.Instantiate();
        Check(!ReferenceEquals(one.GetResource("local"), local) && ReferenceEquals(one.GetResource("local"), one.GetResource("alias")) && !ReferenceEquals(one.GetResource("local"), two.GetResource("local")), "Local resources duplicate once per scene and retain aliases.");
        Check(ReferenceEquals(one.GetResource("shared"), shared) && ReferenceEquals(one.GetResource("shared"), one.GetResource("a/b")), "Nonlocal resources remain shared.");
        Check(ReferenceEquals(one.GetResource("local").GetLocalScene(), one), "Local resource scene association.");
        Check(one.HasResource("") && one.HasResource("nul\0key") && one.HasResource("🌏"), "Canonical encoded labels reconstruct empty/NUL/Unicode names.");
        var list = one.GetResourceList(); Check(Array.IndexOf(list, "\uE000") < Array.IndexOf(list, "\U00010000"), "Storage uses Unicode scalar order.");
        using var duplicatedScene = (PackedScene)packed.Duplicate(true);
        using var duplicate = (ResourcePreloader)duplicatedScene.Instantiate();
        Check(ReferenceEquals(duplicate.GetResource("local"), duplicate.GetResource("alias")), "Packed resource duplication preserves local aliasing.");
        Check(ResourcePreloader.StoredResource("PreloadedResource/GG", typeof(Resource)) is null && ResourcePreloader.StoredResource("PreloadedResource/ef", typeof(Resource)) is null && ResourcePreloader.StoredResource("PreloadedResource/EDA080", typeof(Resource)) is null, "Malformed or noncanonical labels rejected.");
        Check(ResourcePreloader.StoredResource("PreloadedResource/61", typeof(Gradient)) is null, "Only the exact Resource schema is recovered.");
        var descriptor = source.GetPropertyList().Single(p => p.Name == "PreloadedResource/6C6F63616C");
        var resourceDescriptor = (PropertyDescriptor<ResourcePreloader, Resource?>)descriptor;
        resourceDescriptor.SetValue(source, null); Check(!source.HasResource("local"), "Typed document null removes the slot."); source.AddResource("local", local);
        using (var root = new Node())
        {
            var target = new ResourcePreloader(); var attacker = new CaptureMutationNode { Target = target, Asset = first };
            root.AddChild(target); target.Owner = root; root.AddChild(attacker); attacker.Owner = root;
            using var failed = new PackedScene(); Reject<InvalidOperationException>(() => failed.Pack(root)); Check(!target.HasResource("during"), "Capture guard rejects named-table mutation.");
        }
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-preloader-" + Guid.NewGuid().ToString("N")); System.IO.Directory.CreateDirectory(folder);
        try
        {
            var path = System.IO.Path.Combine(folder, "assets.e2dscene");
            ResourceSaver.Save(packed, path); RunArchive(path);
            ResourceSaver.Save(packed, path, SaverFlags.Compress | SaverFlags.SaveBigEndian); RunArchive(path);
            using var empty = new ResourcePreloader(); using var emptyScene = new PackedScene(); emptyScene.Pack(empty); ResourceSaver.Save(emptyScene, path); using var loadedEmpty = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var emptyInstance = (ResourcePreloader)loadedEmpty.Instantiate(); Check(emptyInstance.GetResourceList().Length == 0, "Empty table file roundtrip.");
        }
        finally { System.IO.Directory.Delete(folder, true); }
        var ownedLocal = one.GetResource("alias"); one.Dispose(); Check(ownedLocal.IsDisposed && !local.IsDisposed && !shared.IsDisposed, "Instance owns only its local copies.");
        using var retire = new ResourcePreloader(); retire.AddResource("borrowed", first); retire.Dispose(); Check(!first.IsDisposed, "Holder disposal never disposes borrowed assets.");
        Reject<ObjectDisposedException>(() => retire.GetResourceList()); Reject<ObjectDisposedException>(() => retire.HasResource("borrowed")); Reject<ObjectDisposedException>(() => retire.AddResource("other", second));
        Console.WriteLine("ResourcePreloader: suffix/order/Unicode/snapshots, borrowed/local aliases, typed storage and file graph/empty/compressed/endian ownership, capture/lifetime guards and 128 warm lookup/Has cycles=0 owner B passed.");
    }

    internal static void RunArchive(string path)
    {
        var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
        var one = (ResourcePreloader)scene.Instantiate(); var two = (ResourcePreloader)scene.Instantiate(); scene.Dispose();
        try
        {
            var local = (Gradient)one.GetResource("local"); var shared = one.GetResource("shared");
            Check(local.GetColor(0) == new Color(.25f, .5f, .75f) && ReferenceEquals(local, one.GetResource("alias")), "File local payload and aliases.");
            Check(!ReferenceEquals(local, two.GetResource("local")) && ReferenceEquals(shared, two.GetResource("shared")) && ReferenceEquals(shared, one.GetResource("nul\0key")), "Loaded graph lease survives PackedScene disposal and aliases across instances.");
            one.Dispose(); Check(!shared.IsDisposed && local.IsDisposed, "Other instance retains the decoded shared graph."); two.Dispose(); Check(shared.IsDisposed, "Last instance releases owned decoded graph.");
        }
        finally { one.Dispose(); two.Dispose(); }
    }

    private sealed class CaptureMutationNode : Node
    {
        internal ResourcePreloader? Target; internal Resource? Asset;
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new CaptureMutationNode();
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
        {
            foreach (var p in base.GetPropertyDescriptors()) yield return p;
            yield return new PropertyDescriptor<CaptureMutationNode, int>("Probe", n => { n.Target!.AddResource("during", n.Asset!); return 0; }, (_, _) => { }, _ => 0, stored: true);
        }
    }
}
