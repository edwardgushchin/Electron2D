using System.Buffers;

namespace Electron2D;

#region RenderQueue

/// <summary>
/// Очередь команд рендера на кадр. Append-only.
/// После прогрева не аллоцирует (использует <see cref="ArrayPool{T}"/>).
/// </summary>
internal sealed class RenderQueue : IDisposable
{
    #region Instance fields

    private SpriteCommand[] _buffer;
    private int _count;
    private bool _needsSort;
    private uint _sequence;

    #endregion

    #region Constructors

    public RenderQueue(int initialCapacity = 1024)
    {
        if (initialCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        _buffer = initialCapacity == 0
            ? Array.Empty<SpriteCommand>()
            : ArrayPool<SpriteCommand>.Shared.Rent(initialCapacity);
    }

    #endregion

    #region Properties

    public int Count => _count;

    public bool NeedsSort => _needsSort;

    public ReadOnlySpan<SpriteCommand> Commands => _buffer.AsSpan(0, _count);

    internal Span<SpriteCommand> CommandsMutable => _buffer.AsSpan(0, _count);

    #endregion

    #region Public API

    /// <summary>Очистить очередь команд (буфер сохраняется).</summary>
    public void Clear()
    {
        _count = 0;
        _needsSort = false;
        _sequence = 0;
    }

    /// <summary>
    /// Гарантировать емкость буфера не менее <paramref name="capacity"/>.
    /// </summary>
    public void Preallocate(int capacity)
    {
        if (capacity <= 0)
            return;

        EnsureCapacity(capacity);
    }

    /// <summary>
    /// Добавить команду в очередь.
    /// </summary>
    /// <param name="command">Команда (копируется в буфер).</param>
    /// <returns>Всегда true (буфер расширяется при необходимости).</returns>
    public bool TryPush(in SpriteCommand command)
    {
        EnsureCapacity(_count + 1);

        // Стабильность сортировки: присваиваем sequence на вставке.
        var cmd = command;
        cmd.SetSequence(_sequence++);

        if (_count > 0 && cmd.SortKey < _buffer[_count - 1].SortKey)
            _needsSort = true;

        _buffer[_count++] = cmd;
        return true;
    }

    public void Dispose()
    {
        ReturnBuffer();

        _buffer = Array.Empty<SpriteCommand>();
        _count = 0;
        _needsSort = false;
        _sequence = 0;
    }

    #endregion

    #region Internal helpers

    internal void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _buffer.Length)
            return;

        var newSize = _buffer.Length == 0 ? 256 : _buffer.Length * 2;
        if (newSize < requiredCapacity)
            newSize = requiredCapacity;

        var newBuffer = ArrayPool<SpriteCommand>.Shared.Rent(newSize);

        if (_count != 0)
            Array.Copy(_buffer, 0, newBuffer, 0, _count);

        ReturnBuffer();
        _buffer = newBuffer;
    }

    #endregion

    #region Private helpers

    private void ReturnBuffer()
    {
        var buffer = _buffer;
        if (buffer.Length != 0)
            ArrayPool<SpriteCommand>.Shared.Return(buffer, clearArray: false);
    }

    #endregion
}

#endregion
