namespace Electron2D;

public partial class SceneTree
{
    private readonly DeferredScheduler _typedDeferredScheduler;

    private sealed class DeferredScheduler : IDeferredScheduler
    {
        private readonly SceneTree _tree;
        private readonly Dictionary<Type, IDeferredPool> _pools = [];
        internal DeferredScheduler(SceneTree tree) { _tree = tree; Callback = tree.Defer; }
        public Action<Action> Callback { get; }

        public void Prepare<T>()
        {
            lock (_tree._workGate)
            {
                Pool<T>().Prepare();
            }
        }

        public void Enqueue<T>(Action<T> callback, T value)
        {
            lock (_tree._workGate)
            {
                _tree.EnsureAcceptingWorkUnderLock();
                var slot = Pool<T>().Rent(callback, value);
                try { _tree._deferred.Enqueue(slot.Callback); }
                catch { slot.Discard(); throw; }
            }
        }

        public void Release<T>() { lock (_tree._workGate) Pool<T>().Release(); }
        internal int Count<T>() { lock (_tree._workGate) return _pools.TryGetValue(typeof(T), out var pool) ? ((DeferredPool<T>)pool).CreatedCount : 0; }

        private DeferredPool<T> Pool<T>()
        {
            if (_pools.TryGetValue(typeof(T), out var pool)) return (DeferredPool<T>)pool;
            var created = new DeferredPool<T>(_tree); _pools.Add(typeof(T), created); return created;
        }
    }

    private interface IDeferredPool { }

    private sealed class DeferredPool<T>(SceneTree tree) : IDeferredPool
    {
        private readonly Stack<DeferredInvocation<T>> _free = [];
        private int _created, _reservations;
        internal int CreatedCount => _created;
        internal void Prepare() { _reservations++; if (_created < _reservations) _free.Push(Create()); }
        internal void Release() => _reservations--;
        private DeferredInvocation<T> Create() { _created++; return new(this); }
        internal DeferredInvocation<T> Rent(Action<T> callback, T value)
        {
            var slot = _free.TryPop(out var ready) ? ready : Create();
            slot.Set(callback, value); return slot;
        }
        internal void Return(DeferredInvocation<T> slot) { lock (tree._workGate) _free.Push(slot); }
    }

    private sealed class DeferredInvocation<T> : IDeferredInvocation
    {
        private readonly DeferredPool<T> _pool;
        private Action<T>? _invoke;
        private T _value = default!;
        internal DeferredInvocation(DeferredPool<T> pool) { _pool = pool; Callback = Execute; }
        internal Action Callback { get; }
        internal void Set(Action<T> invoke, T value) { _invoke = invoke; _value = value; }
        private void Execute()
        {
            try { _invoke!(_value); }
            finally { Discard(); }
        }
        public void Discard()
        {
            if (_invoke is null) return;
            _invoke = null; _value = default!; _pool.Return(this);
        }
    }

    private static void DiscardDeferred(Queue<Action> queue)
    {
        while (queue.TryDequeue(out var action))
            if (action.Target is IDeferredInvocation invocation) invocation.Discard();
    }

    internal int PreparedDeferredSlots<T>() => _typedDeferredScheduler.Count<T>();
}
