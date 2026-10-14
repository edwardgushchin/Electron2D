namespace Electron2D;

/// <summary>Stores ordered typed handlers with leased snapshots and prepared mutation capacity.</summary>
internal sealed class EventHandlers<T> where T : Delegate
{
    private readonly object _gate = new();
    private readonly List<Buffer> _buffers = new();
    private Buffer _current;

    internal EventHandlers()
    {
        _current = new Buffer(4);
        _buffers.Add(_current);
        _buffers.Add(new Buffer(4));
    }

    internal static void Add(ref EventHandlers<T>? store, T? value)
    {
        if (value is null) return;
        var handlers = Volatile.Read(ref store);
        if (handlers is null)
        {
            var created = new EventHandlers<T>();
            handlers = Interlocked.CompareExchange(ref store, created, null) ?? created;
        }
        handlers.Add(value);
    }

    private void Add(T value)
    {
        lock (_gate)
        {
            var added = 0;
            foreach (var callback in Delegate.EnumerateInvocationList(value)) added++;
            var needed = checked(_current.Count + added);
            var target = Writable(needed);
            foreach (var callback in Delegate.EnumerateInvocationList(value)) target.Items[target.Count++] = callback;
            // Reserve the next mutation while this buffer is leased by delivery.
            foreach (var buffer in _buffers)
                if (!ReferenceEquals(buffer, target) && buffer.Readers == 0 && buffer.Items.Length >= needed) return;
            _buffers.Add(new Buffer(Capacity(needed)));
        }
    }

    internal void Remove(T? value)
    {
        if (value is null) return;
        lock (_gate)
        {
            var length = 1;
            if (!value.HasSingleTarget)
            {
                length = 0;
                foreach (var callback in Delegate.EnumerateInvocationList(value)) length++;
            }
            for (var start = _current.Count - length; start >= 0; start--)
            {
                var match = true;
                if (value.HasSingleTarget) match = value.Equals(_current.Items[start]);
                else
                {
                    var index = start;
                    foreach (var callback in Delegate.EnumerateInvocationList(value))
                        if (!callback.Equals(_current.Items[index++])) { match = false; break; }
                }
                if (!match) continue;
                var target = Writable(_current.Count);
                Array.Copy(target.Items, start + length, target.Items, start, target.Count - start - length);
                target.Count -= length;
                Array.Clear(target.Items, target.Count, length);
                return;
            }
        }
    }

    internal Snapshot Acquire()
    {
        lock (_gate)
        {
            _current.Readers++;
            return new Snapshot(this, _current);
        }
    }

    internal void Clear()
    {
        lock (_gate)
        {
            if (_current.Count == 0) return;
            var target = Writable(_current.Count);
            Array.Clear(target.Items, 0, target.Count);
            target.Count = 0;
        }
    }

    private Buffer Writable(int needed)
    {
        if (_current.Readers == 0 && _current.Items.Length >= needed) return _current;
        Buffer? target = null;
        foreach (var buffer in _buffers)
            if (!ReferenceEquals(buffer, _current) && buffer.Readers == 0 && buffer.Items.Length >= needed) { target = buffer; break; }
        if (target is null) { target = new Buffer(Capacity(needed)); _buffers.Add(target); }
        Array.Copy(_current.Items, target.Items, _current.Count);
        target.Count = _current.Count;
        if (_current.Readers == 0) { Array.Clear(_current.Items, 0, _current.Count); _current.Count = 0; }
        _current = target;
        return target;
    }

    private static int Capacity(int needed) => checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(4, needed)));

    private void Release(Buffer buffer)
    {
        lock (_gate)
        {
            buffer.Readers--;
            if (buffer.Readers == 0 && !ReferenceEquals(buffer, _current))
            {
                Array.Clear(buffer.Items, 0, buffer.Count);
                buffer.Count = 0;
            }
        }
    }

    internal sealed class Buffer(int capacity)
    {
        internal readonly T[] Items = new T[capacity];
        internal int Count;
        internal int Readers;
    }

    internal readonly struct Snapshot : IDisposable
    {
        private readonly EventHandlers<T>? _owner;
        private readonly Buffer? _buffer;
        internal Snapshot(EventHandlers<T> owner, Buffer buffer) { _owner = owner; _buffer = buffer; Count = buffer.Count; }
        internal int Count { get; }
        internal T this[int index] => _buffer!.Items[index];
        public void Dispose() { if (_buffer is not null) _owner!.Release(_buffer); }
    }
}
