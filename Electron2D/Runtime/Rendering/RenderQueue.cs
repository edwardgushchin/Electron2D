using System.Buffers;

namespace Electron2D;

/// <summary>
/// Очередь команд рендера на кадр. Append-only. После прогрева не аллоцирует.
/// </summary>
internal sealed class RenderQueue : IDisposable
{
    private SpriteCommand[] _buffer;
    private int _count;
    private bool _needsSort;
    private uint _sequence;

    public RenderQueue(int initialCapacity = 1024)
    {
        if (initialCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        _buffer = initialCapacity == 0
            ? Array.Empty<SpriteCommand>()
            : ArrayPool<SpriteCommand>.Shared.Rent(initialCapacity);
    }

    public int Count => _count;
    public bool NeedsSort => _needsSort;

    public ReadOnlySpan<SpriteCommand> Commands => _buffer.AsSpan(0, _count);
    internal Span<SpriteCommand> CommandsMutable => _buffer.AsSpan(0, _count);

    public void Clear()
    {
        _count = 0;
        _needsSort = false;
        _sequence = 0;
    }
    
    public void Preallocate(int capacity)
    {
        if (capacity <= 0) return;
        EnsureCapacity(capacity);
    }

    public bool TryPush(in SpriteCommand command)
    {
        EnsureCapacity(_count + 1);

        // Стабильность сортировки: присваиваем sequence на вставке.
        var cmd = command;
        cmd.SetSequence(_sequence++);

        if (_count > 0)
        {
            // Если ключи перестали быть неубывающими — понадобится сортировка.
            if (cmd.SortKey < _buffer[_count - 1].SortKey)
                _needsSort = true;
        }

        _buffer[_count++] = cmd;
        return true;
    }

    private void EnsureCapacity(int required)
    {
        if (required <= _buffer.Length)
            return;

        var newSize = _buffer.Length == 0 ? 256 : _buffer.Length * 2;
        if (newSize < required) newSize = required;

        var newArr = ArrayPool<SpriteCommand>.Shared.Rent(newSize);
        if (_count > 0)
            Array.Copy(_buffer, 0, newArr, 0, _count);

        ReturnBuffer();
        _buffer = newArr;
    }

    private void ReturnBuffer()
    {
        var arr = _buffer;
        if (arr.Length != 0)
            ArrayPool<SpriteCommand>.Shared.Return(arr, clearArray: false);
    }

    public void Dispose()
    {
        ReturnBuffer();
        _buffer = Array.Empty<SpriteCommand>();
        _count = 0;
        _needsSort = false;
        _sequence = 0;
    }
}
