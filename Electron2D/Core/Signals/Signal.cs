namespace Electron2D;

public sealed class Signal
{
    private struct Slot
    {
        public int Id;
        public Action? Fn;
        public bool Once;
    }

    private List<Slot>? _slots;
    private int _nextId = 1;
    private int _emitting;
    private int _aliveCount;

    public bool HasSubscribers => _aliveCount != 0;

    public readonly struct Subscription
    {
        internal Subscription(int id) => Id = id;
        internal int Id { get; }
    }

    public Subscription Connect(Action fn, bool once = false)
    {
        if (fn is null) throw new ArgumentNullException(nameof(fn));

        _slots ??= new List<Slot>(4);

        var id = _nextId++;
        _slots.Add(new Slot { Id = id, Fn = fn, Once = once });
        _aliveCount++;
        return new Subscription(id);
    }

    public bool Disconnect(Subscription sub)
    {
        var slots = _slots;
        if (slots is null) return false;

        for (var i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s.Id != sub.Id || s.Fn is null) continue;

            s.Fn = null; // ленивое удаление
            slots[i] = s;
            _aliveCount--;

            if (_emitting == 0) Compact(slots);
            return true;
        }

        return false;
    }

    public void Emit()
    {
        var slots = _slots;
        if (slots is null || slots.Count == 0) return;

        _emitting++;
        try
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                var fn = s.Fn;
                if (fn is null) continue;

                fn();

                if (!s.Once) continue;
                s.Fn = null;
                slots[i] = s;
                _aliveCount--;
            }
        }
        finally
        {
            _emitting--;
            if (_emitting == 0) Compact(slots);
        }
    }

    private static void Compact(List<Slot> slots)
    {
        var write = 0;
        for (var read = 0; read < slots.Count; read++)
        {
            var s = slots[read];
            if (s.Fn is null) continue;
            if (write != read) slots[write] = s;
            write++;
        }

        if (write < slots.Count)
            slots.RemoveRange(write, slots.Count - write);
    }
}

public sealed class Signal<T>
{
    private struct Slot
    {
        public int Id;
        public Action<T>? Fn;
        public bool Once;
    }

    private List<Slot>? _slots;
    private int _nextId = 1;
    private int _emitting;
    private int _aliveCount;

    public bool HasSubscribers => _aliveCount != 0;

    public readonly struct Subscription
    {
        internal Subscription(int id) => Id = id;
        internal int Id { get; }
    }

    public Subscription Connect(Action<T> fn, bool once = false)
    {
        if (fn is null) throw new ArgumentNullException(nameof(fn));

        _slots ??= new List<Slot>(4);

        var id = _nextId++;
        _slots.Add(new Slot { Id = id, Fn = fn, Once = once });
        _aliveCount++;
        return new Subscription(id);
    }

    public bool Disconnect(Subscription sub)
    {
        var slots = _slots;
        if (slots is null) return false;

        for (var i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s.Id != sub.Id || s.Fn is null) continue;

            s.Fn = null;
            slots[i] = s;
            _aliveCount--;

            if (_emitting == 0) Compact(slots);
            return true;
        }

        return false;
    }

    public void Emit(T arg)
    {
        var slots = _slots;
        if (slots is null || slots.Count == 0) return;

        _emitting++;
        try
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                var fn = s.Fn;
                if (fn is null) continue;

                fn(arg);

                if (!s.Once) continue;
                s.Fn = null;
                slots[i] = s;
                _aliveCount--;
            }
        }
        finally
        {
            _emitting--;
            if (_emitting == 0) Compact(slots);
        }
    }

    private static void Compact(List<Slot> slots)
    {
        var write = 0;
        for (var read = 0; read < slots.Count; read++)
        {
            var s = slots[read];
            if (s.Fn is null) continue;
            if (write != read) slots[write] = s;
            write++;
        }

        if (write < slots.Count)
            slots.RemoveRange(write, slots.Count - write);
    }
}
