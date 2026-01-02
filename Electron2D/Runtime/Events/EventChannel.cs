using System.Runtime.CompilerServices;

namespace Electron2D;

#region EventChannel<TEvent>

/// <summary>
/// Двухбуферный канал событий фиксированной емкости:
/// публикация идет в write-буфер, чтение — из read-буфера после <see cref="Swap"/>.
/// </summary>
/// <typeparam name="TEvent">Тип события.</typeparam>
/// <remarks>
/// Модель использования:
/// 1) В течение кадра вызывается <see cref="TryPublish"/> (запись в write-буфер).
/// 2) В начале следующей фазы обработки вызывается <see cref="Swap"/> и читается <see cref="Read"/>.
/// Это позволяет избежать аллокаций и блокировок при типичном "producer -> consumer" в одном потоке.
/// </remarks>
internal sealed class EventChannel<TEvent>
{
    #region Instance fields

    private TEvent[] _readBuffer;
    private TEvent[] _writeBuffer;

    private int _readCount;
    private int _writeCount;

    #endregion

    #region Constructors

    public EventChannel(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _readBuffer = new TEvent[capacity];
        _writeBuffer = new TEvent[capacity];
    }

    #endregion

    #region Properties

    /// <summary>Емкость буферов канала.</summary>
    public int Capacity => _readBuffer.Length;

    /// <summary>Количество событий, доступных для чтения в <see cref="Read"/>.</summary>
    public int ReadCount => _readCount;

    /// <summary>Срез событий, доступных для чтения (read-буфер).</summary>
    public ReadOnlySpan<TEvent> Read => _readBuffer.AsSpan(0, _readCount);

    #endregion

    #region Public API

    /// <summary>
    /// Попытаться опубликовать событие в write-буфер.
    /// </summary>
    /// <param name="eventValue">Событие.</param>
    /// <returns>True, если событие добавлено; иначе false (write-буфер заполнен).</returns>
    public bool TryPublish(in TEvent eventValue)
    {
        if ((uint)_writeCount >= (uint)_writeBuffer.Length)
            return false;

        _writeBuffer[_writeCount++] = eventValue;
        return true;
    }

    /// <summary>
    /// Поменять местами read/write буферы.
    /// После вызова <see cref="Read"/> будет содержать опубликованные ранее события.
    /// </summary>
    public void Swap()
    {
        var oldReadCount = _readCount;

        (_readBuffer, _writeBuffer) = (_writeBuffer, _readBuffer);
        _readCount = _writeCount;
        _writeCount = 0;

        // Если тип содержит ссылки, нужно занулить те элементы в новом write-буфере,
        // которые раньше были прочитаны (иначе удерживаем объекты до следующего Clear/Swap).
        if (oldReadCount != 0 && RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>())
            Array.Clear(_writeBuffer, 0, oldReadCount);
    }

    /// <summary>
    /// Полностью очистить оба буфера и сбросить счетчики.
    /// </summary>
    public void Clear()
    {
        // Очищаем массивы только если есть ссылки (иначе это лишний O(capacity)).
        if (RuntimeHelpers.IsReferenceOrContainsReferences<TEvent>())
        {
            Array.Clear(_readBuffer, 0, _readBuffer.Length);
            Array.Clear(_writeBuffer, 0, _writeBuffer.Length);
        }

        _readCount = 0;
        _writeCount = 0;
    }

    #endregion
}

#endregion
