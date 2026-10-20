namespace Electron2D;

internal sealed class ResourceFileOwnership(Resource[] resources, ResourceFileOwnership[]? retainedOwners = null)
{
    private readonly object _gate = new();
    private readonly List<Resource> _resources = [.. resources];
    private int _leases = 1;
    internal bool Contains(Resource resource)
    {
        lock (_gate) return _resources.Any(item => ReferenceEquals(item, resource)) || retainedOwners is not null && retainedOwners.Any(owner => owner.Contains(resource));
    }
    internal ResourceFileOwnership RetainOwner()
    {
        lock (_gate) { if (_leases == 0) throw new ObjectDisposedException(nameof(ResourceFileOwnership)); _leases++; return this; }
    }
    internal IDisposable Retain() => new Lease(RetainOwner());
    internal IDisposable TransferToLease() => new Lease(this);
    internal static IDisposable RetainLease(IDisposable lease) => lease is Lease retained
        ? retained.Retain() : throw new InvalidOperationException("Unknown scene file lease.");
    internal void ReleaseOwner() => Release();
    internal void AppendResources(IReadOnlyList<Resource> added)
    {
        lock (_gate) { if (_leases == 0) throw new ObjectDisposedException(nameof(ResourceFileOwnership)); _resources.AddRange(added); }
    }
    internal void TransferResources()
    {
        lock (_gate)
        {
            if (_leases != 1 || retainedOwners is { Length: > 0 }) throw new InvalidOperationException("Shared scene resources cannot transfer their ownership.");
            _resources.Clear();
        }
    }
    private void Release()
    {
        lock (_gate)
        {
            if (--_leases != 0) return;
        }
        List<Exception>? errors = null;
        foreach (var resource in _resources) try
            {
                resource.Dispose();
            }
            catch (Exception error)
            {
                (errors ??= []).Add(error);
            }
        if (retainedOwners is not null) foreach (var owner in retainedOwners) try { owner.ReleaseOwner(); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException(errors);
    }
    private sealed class Lease(ResourceFileOwnership owner) : IDisposable
    {
        private ResourceFileOwnership? _owner = owner;
        internal IDisposable Retain() => (_owner ?? throw new ObjectDisposedException(nameof(Lease))).Retain();
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Release();
    }
}
