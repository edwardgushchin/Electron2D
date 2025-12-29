namespace Electron2D;

public sealed class Signal
{
    private struct Slot { public int Id; public Action? Fn; public bool Once; }
    private readonly List<Slot> _slots = [];
    private int _nextId = 1;
    private int _emitting;

    public readonly struct Subscription
    {
        internal Subscription(int id) { Id = id; }
        internal int Id { get; }
    }

    public Subscription Connect(Action fn, bool once = false)
    {
        if (fn is null) throw new ArgumentNullException(nameof(fn));
        var id = _nextId++;
        _slots.Add(new Slot { Id = id, Fn = fn, Once = once });
        return new Subscription(id);
    }

    public bool Disconnect(Subscription sub)
    {
        for (var i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].Id != sub.Id || _slots[i].Fn is null) continue;
            var s = _slots[i];
            s.Fn = null; // ленивое удаление (без сдвигов во время emit)
            _slots[i] = s;
            if (_emitting == 0) Compact();
            return true;
        }
        return false;
    }

    public void Emit()
    {
        _emitting++;
        try
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                var fn = s.Fn;
                if (fn is null) continue;

                fn();

                if (!s.Once) continue;
                s.Fn = null;
                _slots[i] = s;
            }
        }
        finally
        {
            _emitting--;
            if (_emitting == 0) Compact();
        }
    }

    private void Compact()
    {
        _slots.RemoveAll(s => s.Fn is null);
    }
}

public sealed class Signal<T>
{
    private struct Slot { public int Id; public Action<T>? Fn; public bool Once; }
    private readonly List<Slot> _slots = [];
    private int _nextId = 1;
    private int _emitting;

    public readonly struct Subscription
    {
        internal Subscription(int id) { Id = id; }
        internal int Id { get; }
    }

    public Subscription Connect(Action<T> fn, bool once = false)
    {
        if (fn is null) throw new ArgumentNullException(nameof(fn));
        var id = _nextId++;
        _slots.Add(new Slot { Id = id, Fn = fn, Once = once });
        return new Subscription(id);
    }

    public bool Disconnect(Subscription sub)
    {
        for (var i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].Id != sub.Id || _slots[i].Fn is null) continue;
            var s = _slots[i];
            s.Fn = null;
            _slots[i] = s;
            if (_emitting == 0) Compact();
            return true;
        }
        return false;
    }

    public void Emit(T arg)
    {
        _emitting++;
        try
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                var fn = s.Fn;
                if (fn is null) continue;

                fn(arg);

                if (!s.Once) continue;
                s.Fn = null;
                _slots[i] = s;
            }
        }
        finally
        {
            _emitting--;
            if (_emitting == 0) Compact();
        }
    }

    private void Compact()
    {
        _slots.RemoveAll(s => s.Fn is null);
    }
}