namespace Electron2D;

/// <summary>Retains ordinary multicast delivery while exposing current owned-wrapper membership.</summary>
internal struct EventPublisher<T> where T : Delegate
{
    private Store? _store;

    internal T? Handlers
    {
        get => Volatile.Read(ref _store)?.Handlers;
        set
        {
            if (value is null) Volatile.Read(ref _store)?.Set(null);
            else GetStore().Set(value);
        }
    }

    internal void Add(T? value) { if (value is not null) GetStore().Add(value); }
    internal void Remove(T? value) { if (value is not null) Volatile.Read(ref _store)?.Remove(value); }

    private Store GetStore()
    {
        var store = Volatile.Read(ref _store);
        return store ?? Interlocked.CompareExchange(ref _store, new Store(), null) ?? _store!;
    }

    private sealed class Store : EventConnection.IPublisher
    {
        private readonly object _gate = new();
        private T? _handlers;
        internal T? Handlers => Volatile.Read(ref _handlers);

        private void Observe(T? value)
        {
            // Queries take the token lock before this publisher lock.
            foreach (var callback in Delegate.EnumerateInvocationList(value))
                if (callback.Target is EventConnection connection) connection.ObservePublisher(this, callback);
        }

        internal void Add(T value)
        {
            Observe(value);
            lock (_gate) Volatile.Write(ref _handlers, (T?)Delegate.Combine(_handlers, value));
        }

        internal void Remove(T value)
        {
            lock (_gate) Volatile.Write(ref _handlers, (T?)Delegate.Remove(_handlers, value));
        }

        internal void Set(T? value)
        {
            Observe(value);
            lock (_gate) Volatile.Write(ref _handlers, value);
        }

        bool EventConnection.IPublisher.Contains(Delegate wrapper)
        {
            lock (_gate)
                foreach (var callback in Delegate.EnumerateInvocationList(_handlers))
                    if (callback.Equals(wrapper)) return true;
            return false;
        }
    }
}
