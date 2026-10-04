namespace Electron2D;

internal sealed class ResourceFileOwnership(Resource[] resources, ResourceFileOwnership[]? retainedOwners = null)
{
    private readonly object _gate = new();
    private int _leases = 1;
    internal bool Contains(Resource resource) => resources.Any(item => ReferenceEquals(item, resource)) || retainedOwners is not null && retainedOwners.Any(owner => owner.Contains(resource));
    internal ResourceFileOwnership RetainOwner()
    {
        lock (_gate) { if (_leases == 0) throw new ObjectDisposedException(nameof(ResourceFileOwnership)); _leases++; return this; }
    }
    internal IDisposable Retain() => new Lease(RetainOwner());
    internal void ReleaseOwner() => Release();
    private void Release()
    {
        lock (_gate)
        {
            if (--_leases != 0) return;
        }
        List<Exception>? errors = null;
        foreach (var resource in resources) try
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
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Release();
    }
}
