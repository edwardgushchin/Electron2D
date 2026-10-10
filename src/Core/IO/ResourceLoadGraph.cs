namespace Electron2D;

internal sealed class ResourceLoadGraph(ResourceFormatLoader[] formats, bool useSubThreads, CancellationToken cancellation)
{
    internal sealed class Entry(string path, ResourceLoader.CacheMode mode)
    {
        internal readonly string Path = path;
        internal readonly ResourceLoader.CacheMode Mode = mode;
        internal readonly object Gate = new();
        internal readonly List<Entry> Dependencies = [];
        internal Resource? Prepared, Final;
        internal ResourceArchiveRead? Archive;
        internal ResourceFileOwnership? Ownership;
        internal bool Borrowed, Planned, Publish, Released, Transferred;
        internal long UID = ResourceUID.InvalidID;
    }
    [ThreadStatic] private static ResourceLoadGraph? _current;
    [ThreadStatic] private static Entry? _entry;
    [ThreadStatic] private static int _depth;
    private readonly object _gate = new();
    private readonly Dictionary<(string Path, ResourceLoader.CacheMode Mode), Entry> _shared = new();
    private readonly Dictionary<string, List<string>> _edges = new(StringComparer.Ordinal);
    private readonly List<Entry> _entries = [];
    private readonly Dictionary<Resource, Resource> _remap = new(ReferenceEqualityComparer.Instance);
    private int _completed;
    private Entry? _root;
    internal static ResourceLoadGraph? Current => _current;
    internal ResourceFormatLoader[] Formats { get; } = formats;
    internal bool UseSubThreads { get; } = useSubThreads;
    internal object EntryGate => _entry!.Gate;
    internal float Progress { get { lock (_gate) return _entries.Count == 0 ? 0 : .9f * _completed / _entries.Count; } }
    private readonly struct Scope : IDisposable
    {
        private readonly ResourceLoadGraph? _previousGraph;
        private readonly Entry? _previousEntry;
        private readonly int _previousDepth;
        internal Scope(ResourceLoadGraph graph, Entry? entry, int depth)
        {
            _previousGraph = _current; _previousEntry = _entry; _previousDepth = _depth;
            _current = graph; _entry = entry; _depth = depth;
        }
        public void Dispose() { _current = _previousGraph; _entry = _previousEntry; _depth = _previousDepth; }
    }
    internal void Prepare(string path, ResourceLoader.CacheMode mode, Type type)
    {
        using var scope = new Scope(this, null, 0);
        _ = Load(path, mode, type);
    }
    internal Resource Load(string path, ResourceLoader.CacheMode mode, Type type)
    {
        cancellation.ThrowIfCancellationRequested(); ArgumentException.ThrowIfNullOrEmpty(path);
        if ((uint)mode > 4) throw new ArgumentOutOfRangeException(nameof(mode));
        path = ResourceUID.EnsurePath(path); var parent = _entry; var depth = _depth + 1;
        Entry entry;
        lock (_gate)
        {
            if (mode is ResourceLoader.CacheMode.Reuse or ResourceLoader.CacheMode.Replace or ResourceLoader.CacheMode.ReplaceDeep && _shared.TryGetValue((path, mode), out var existing)) entry = existing;
            else
            {
                if (_entries.Count >= 1024) throw new InvalidDataException("A staged resource graph exceeds 1024 files.");
                entry = new(path, mode); _entries.Add(entry);
                if (mode is ResourceLoader.CacheMode.Reuse or ResourceLoader.CacheMode.Replace or ResourceLoader.CacheMode.ReplaceDeep) _shared.Add((path, mode), entry);
                _root ??= entry;
            }
            if (parent is not null && !parent.Dependencies.Contains(entry)) parent.Dependencies.Add(entry);
        }
        if (entry.Prepared is null && !(mode == ResourceLoader.CacheMode.Reuse && Resource.GetRegisteredPath(path) is not null)) AddEdge(parent?.Path, path);
        lock (entry.Gate)
        {
            if (entry.Prepared is null)
            {
                if (depth > 64) throw new InvalidDataException("Resource file dependency depth exceeds 64.");
                using var scope = new Scope(this, entry, depth);
                var cached = mode == ResourceLoader.CacheMode.Reuse ? Resource.GetRegisteredPath(path) : null;
                if (cached is not null) { entry.Prepared = cached; entry.Borrowed = true; }
                else
                {
                    var loader = Formats.FirstOrDefault(item => !item.IsDisposed && item.RecognizePath(path, type));
                    if (loader is ResourceArchiveLoader)
                    {
                        entry.Archive = ResourceArchive.Decode(path, mode); entry.Prepared = entry.Archive.Root;
                        entry.UID = ResourceArchive.ReadUID(path);
                    }
                    else entry.Prepared = ResourceLoader.LoadImmediate(path, ResourceLoader.CacheMode.IgnoreDeep, type, isolated: true, hookMode: mode);
                    if (entry.Prepared.FilePathRegistered) { entry.Borrowed = true; throw new InvalidDataException("A format loader must prepare independent resource state without publishing cache identities."); }
                }
                if (entry.Prepared.IsDisposed || !type.IsInstanceOfType(entry.Prepared)) throw new InvalidDataException("Prepared resource does not match its requested type.");
                lock (_gate) _completed++;
            }
            if (entry.Prepared.IsDisposed || !type.IsInstanceOfType(entry.Prepared)) throw new InvalidDataException("Prepared shared resource does not match its requested type.");
            cancellation.ThrowIfCancellationRequested(); return entry.Prepared;
        }
    }
    private void AddEdge(string? parent, string target)
    {
        if (parent is null) return;
        parent = ResourceArchive.Absolute(parent); target = ResourceArchive.Absolute(target);
        lock (_gate)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            bool Reaches(string current)
            {
                if (current == parent) return true;
                if (!seen.Add(current) || !_edges.TryGetValue(current, out var children)) return false;
                return children.Any(Reaches);
            }
            if (Reaches(target)) throw new InvalidDataException("External resource file cycle requires bundled resources.");
            if (!_edges.TryGetValue(parent, out var edges)) _edges.Add(parent, edges = []);
            if (!edges.Contains(target)) edges.Add(target);
        }
    }
    internal void LoadDependencies(int count, Action<int> load)
    {
        if (!UseSubThreads || count < 2) { for (var i = 0; i < count; i++) { cancellation.ThrowIfCancellationRequested(); load(i); } return; }
        var parent = _entry; var depth = _depth;
        var group = WorkerThreadPool.AddGroupTask(index => { using var scope = new Scope(this, parent, depth); cancellation.ThrowIfCancellationRequested(); load(index); }, count, highPriority: true, description: "resource dependencies");
        WorkerThreadPool.WaitForDependencyGroup(group);
    }
    internal Resource Commit()
    {
        cancellation.ThrowIfCancellationRequested();
        Plan(_root!);
        Publish(_root!);
        var result = _root!.Final!; _root.Transferred = true;
        try { DisposeUnused(); } catch { _root.Transferred = false; throw; }
        return result;
    }
    private void Plan(Entry entry)
    {
        if (entry.Planned) return;
        entry.Planned = true;
        var source = entry.Prepared ?? throw new InvalidDataException("The prepared graph is incomplete.");
        ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        var cached = Resource.GetRegisteredPath(entry.Path);
        if (entry.Borrowed) entry.Final = cached?.GetType() == source.GetType() ? cached : source;
        else if (entry.Mode == ResourceLoader.CacheMode.Reuse && cached is not null) entry.Final = cached;
        else
        {
            entry.Final = entry.Mode is ResourceLoader.CacheMode.Replace or ResourceLoader.CacheMode.ReplaceDeep && cached?.GetType() == source.GetType() ? cached : source;
            entry.Publish = true;
        }
        _remap[source] = entry.Final;
        if (entry.Publish) foreach (var child in entry.Dependencies) Plan(child);
    }
    private Resource Redirect(Resource resource) => _remap.TryGetValue(resource, out var final) ? final : resource;
    private void Publish(Entry entry)
    {
        if (!entry.Publish || entry.Released) return;
        foreach (var child in entry.Dependencies) Publish(child);
        var source = entry.Prepared!;
        if (entry.Archive is { } archive) archive.RedirectResources(Redirect); else source.RemapPreparedReferences(Redirect);
        var ownedChildren = new List<ResourceFileOwnership>();
        foreach (var child in entry.Dependencies) if (child.Publish && ReferenceEquals(child.Final, child.Prepared))
            {
                child.Ownership ??= new ResourceFileOwnership([child.Final!]);
                child.Transferred = true; ownedChildren.Add(child.Ownership.RetainOwner());
            }
        if (entry.Archive is { } context)
        {
            source = context.ReleaseRoot(); entry.Archive = null;
        }
        source.AppendFileOwnerships(ownedChildren.ToArray());
        if (!ReferenceEquals(entry.Final, source))
        {
            try { entry.Final!.CopyFromResource(source); }
            finally { entry.Released = true; source.Dispose(); }
        }
        else
        {
            if (entry.Mode is ResourceLoader.CacheMode.Ignore or ResourceLoader.CacheMode.IgnoreDeep) source.SetPathCache(entry.Path);
            else source.TakeOverPath(entry.Path);
            entry.Released = true;
        }
        if (entry.UID != ResourceUID.InvalidID) ResourceUID.SetID(entry.UID, entry.Path);
    }
    internal void DisposeUnused()
    {
        List<Exception>? errors = null;
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            try
            {
                var ownership = entry.Ownership; entry.Ownership = null; ownership?.ReleaseOwner();
                if (entry.Archive is { } context) { entry.Archive = null; context.Dispose(); }
                else if (!entry.Borrowed && !entry.Transferred && entry.Prepared is { } resource && (!entry.Released || ReferenceEquals(entry.Final, resource))) resource.Dispose();
            }
            catch (Exception error) { (errors ??= []).Add(error); }
        }
        if (errors is not null) throw new AggregateException(errors);
    }
    internal bool IsCreatingPath(string path) => _entry is { } entry && entry.Path == path && entry.Prepared is null;
}
