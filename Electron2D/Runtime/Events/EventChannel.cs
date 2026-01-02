using System.Runtime.CompilerServices;

namespace Electron2D;

internal sealed class EventChannel<TEvent>
{
    private TEvent[] _read;
    private TEvent[] _write;

    private int _readCount;
    private int _writeCount;

    public EventChannel(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _read = new TEvent[capacity];
        _write = new TEvent[capacity];
    }

    public int Capacity => _read.Length;
    public int ReadCount => _readCount;

    public ReadOnlySpan<TEvent> Read => _read.AsSpan(0, _readCount);

    public bool TryPublish(in TEvent ev)
    {
        if ((uint)_writeCount >= (uint)_write.Length) return false;
        _write[_writeCount++] = ev;
        return true;
    }

    public void Swap()
    {
        var oldReadCount = _readCount;

        (_read, _write) = (_write, _read);
        _readCount = _writeCount;
        _writeCount = 0;

        if (oldReadCount != 0 && RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>())
            Array.Clear(_write, 0, oldReadCount);
    }

    public void Clear()
    {
        // Очищаем только если есть ссылки (иначе это лишний O(capacity))
        if (RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>())
        {
            Array.Clear(_read, 0, _read.Length);
            Array.Clear(_write, 0, _write.Length);
        }

        _readCount = 0;
        _writeCount = 0;
    }
}