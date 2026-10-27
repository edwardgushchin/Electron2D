namespace Electron2D;

public partial class SceneTree
{
    private readonly DeferredScheduler _typedDeferredScheduler;
    internal long DeferredStorageBytes;

    internal sealed class DeferredQueue(SceneTree tree)
    {
        private readonly Queue<Action> _actions = new();
        private long _storageBytes;
        internal int Count => _actions.Count;
        internal bool TryDequeue(out Action action) => _actions.TryDequeue(out action!);
        internal void Enqueue(Action action)
        {
            if (_actions.Count < _actions.EnsureCapacity(0)) { _actions.Enqueue(action); return; }
            // Only the replacement backing array is allocated inside this growth boundary.
            var before = GC.GetAllocatedBytesForCurrentThread();
            _actions.Enqueue(action);
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            ObjectCensus.ChangeDeferredStorage(tree, bytes - _storageBytes);
            _storageBytes = bytes;
        }
        internal void ReleaseStorage()
        {
            _actions.TrimExcess();
            ObjectCensus.ChangeDeferredStorage(tree, -_storageBytes);
            _storageBytes = 0;
        }
    }

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
            lock (_tree._workGate) Enqueue(callback, value, _tree._deferred);
        }

        internal void Enqueue<T>(Action<T> callback, T value, DeferredQueue queue)
        {
            lock (_tree._workGate)
            {
                _tree.EnsureAcceptingWorkUnderLock();
                var slot = Pool<T>().Rent(callback, value);
                try { queue.Enqueue(slot.Callback); }
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
        private DeferredInvocation<T> Create()
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var slot = new DeferredInvocation<T>(this);
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            ObjectCensus.ChangeDeferredStorage(tree, bytes);
            _created++;
            return slot;
        }
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

    private static void DiscardDeferred(DeferredQueue queue)
    {
        while (queue.TryDequeue(out var action))
            if (action.Target is IDeferredInvocation invocation) invocation.Discard();
    }

    internal int PreparedDeferredSlots<T>() => _typedDeferredScheduler.Count<T>();
}
