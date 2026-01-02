using System;
using System.Collections.Generic;

namespace Electron2D;

#region Signal (без аргументов)

/// <summary>
/// Простейший сигнал (событие) без аргументов с поддержкой:
/// - подписок с авто-отпиской (once);
/// - безопасного удаления во время рассылки (ленивое удаление + компактизация).
/// </summary>
/// <remarks>
/// Важный инвариант: во время <see cref="Emit"/> список слотов не модифицируется структурно
/// (не происходит Remove/Insert), только помечается "мертвым" через <c>Callback = null</c>.
/// Это предотвращает сдвиги элементов и сохраняет стабильный hot-path.
/// </remarks>
public sealed class Signal
{
    #region Constants

    private const int InitialSlotCapacity = 4;

    #endregion

    #region Instance fields

    private List<Slot>? _slots;
    private int _nextSubscriptionId = 1;
    private int _emittingDepth;
    private int _aliveCount;

    #endregion

    #region Properties

    /// <summary>Есть ли активные подписчики.</summary>
    public bool HasSubscribers => _aliveCount != 0;

    #endregion

    #region Nested types

    private struct Slot
    {
        public int Id;
        public Action? Callback;
        public bool Once;
    }

    /// <summary>
    /// Токен подписки, возвращаемый <see cref="Connect"/> и используемый в <see cref="Disconnect"/>.
    /// </summary>
    public readonly struct Subscription
    {
        internal Subscription(Signal owner, int id)
        {
            Owner = owner;
            Id = id;
        }

        /// <summary>Владелец подписки. Нужен для валидации Disconnect.</summary>
        internal Signal? Owner { get; }

        /// <summary>Идентификатор подписки внутри владельца.</summary>
        internal int Id { get; }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Подписаться на сигнал.
    /// </summary>
    /// <param name="callback">Обработчик.</param>
    /// <param name="once">Если true — обработчик будет автоматически отключён после первого вызова.</param>
    /// <returns>Токен подписки для последующего отключения.</returns>
    public Subscription Connect(Action callback, bool once = false)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _slots ??= new List<Slot>(InitialSlotCapacity);

        var id = _nextSubscriptionId++;
        _slots.Add(new Slot { Id = id, Callback = callback, Once = once });
        _aliveCount++;

        return new Subscription(this, id);
    }

    /// <summary>
    /// Отключить ранее добавленную подписку.
    /// </summary>
    /// <param name="subscription">Токен подписки.</param>
    /// <returns>True, если подписка была найдена и отключена; иначе false.</returns>
    public bool Disconnect(Subscription subscription)
    {
        if (subscription.Id <= 0 || !ReferenceEquals(subscription.Owner, this))
            return false;

        var slots = _slots;
        if (slots is null)
            return false;

        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.Id != subscription.Id || slot.Callback is null)
                continue;

            // Ленивое удаление: не делаем Remove во время/вокруг рассылки, чтобы избежать сдвигов в hot-path.
            slot.Callback = null;
            slots[i] = slot;
            _aliveCount--;

            if (_emittingDepth == 0)
                Compact(slots);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Вызвать всех подписчиков.
    /// </summary>
    public void Emit()
    {
        var slots = _slots;
        if (slots is null || slots.Count == 0)
            return;

        _emittingDepth++;
        try
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var callback = slot.Callback;
                if (callback is null)
                    continue;

                callback();

                if (!slot.Once)
                    continue;

                slot.Callback = null;
                slots[i] = slot;
                _aliveCount--;
            }
        }
        finally
        {
            _emittingDepth--;
            if (_emittingDepth == 0)
                Compact(slots);
        }
    }

    #endregion

    #region Private helpers

    private static void Compact(List<Slot> slots)
    {
        var writeIndex = 0;

        for (var readIndex = 0; readIndex < slots.Count; readIndex++)
        {
            var slot = slots[readIndex];
            if (slot.Callback is null)
                continue;

            if (writeIndex != readIndex)
                slots[writeIndex] = slot;

            writeIndex++;
        }

        if (writeIndex < slots.Count)
            slots.RemoveRange(writeIndex, slots.Count - writeIndex);
    }

    #endregion
}

#endregion

#region Signal<T>

/// <summary>
/// Простейший сигнал (событие) с одним аргументом <typeparamref name="T"/>.
/// См. <see cref="Signal"/> для общих принципов реализации.
/// </summary>
/// <typeparam name="T">Тип аргумента рассылки.</typeparam>
public sealed class Signal<T>
{
    #region Constants

    private const int InitialSlotCapacity = 4;

    #endregion

    #region Instance fields

    private List<Slot>? _slots;
    private int _nextSubscriptionId = 1;
    private int _emittingDepth;
    private int _aliveCount;

    #endregion

    #region Properties

    /// <summary>Есть ли активные подписчики.</summary>
    public bool HasSubscribers => _aliveCount != 0;

    #endregion

    #region Nested types

    private struct Slot
    {
        public int Id;
        public Action<T>? Callback;
        public bool Once;
    }

    /// <summary>
    /// Токен подписки, возвращаемый <see cref="Connect"/> и используемый в <see cref="Disconnect"/>.
    /// </summary>
    public readonly struct Subscription
    {
        internal Subscription(Signal<T> owner, int id)
        {
            Owner = owner;
            Id = id;
        }

        /// <summary>Владелец подписки. Нужен для валидации Disconnect.</summary>
        internal Signal<T>? Owner { get; }

        /// <summary>Идентификатор подписки внутри владельца.</summary>
        internal int Id { get; }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Подписаться на сигнал.
    /// </summary>
    /// <param name="callback">Обработчик.</param>
    /// <param name="once">Если true — обработчик будет автоматически отключён после первого вызова.</param>
    /// <returns>Токен подписки для последующего отключения.</returns>
    public Subscription Connect(Action<T> callback, bool once = false)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _slots ??= new List<Slot>(InitialSlotCapacity);

        var id = _nextSubscriptionId++;
        _slots.Add(new Slot { Id = id, Callback = callback, Once = once });
        _aliveCount++;

        return new Subscription(this, id);
    }

    /// <summary>
    /// Отключить ранее добавленную подписку.
    /// </summary>
    /// <param name="subscription">Токен подписки.</param>
    /// <returns>True, если подписка была найдена и отключена; иначе false.</returns>
    public bool Disconnect(Subscription subscription)
    {
        if (subscription.Id <= 0 || !ReferenceEquals(subscription.Owner, this))
            return false;

        var slots = _slots;
        if (slots is null)
            return false;

        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.Id != subscription.Id || slot.Callback is null)
                continue;

            // Ленивое удаление: не делаем Remove во время/вокруг рассылки, чтобы избежать сдвигов.
            slot.Callback = null;
            slots[i] = slot;
            _aliveCount--;

            if (_emittingDepth == 0)
                Compact(slots);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Вызвать всех подписчиков.
    /// </summary>
    /// <param name="arg">Аргумент рассылки.</param>
    public void Emit(T arg)
    {
        var slots = _slots;
        if (slots is null || slots.Count == 0)
            return;

        _emittingDepth++;
        try
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var callback = slot.Callback;
                if (callback is null)
                    continue;

                callback(arg);

                if (!slot.Once)
                    continue;

                slot.Callback = null;
                slots[i] = slot;
                _aliveCount--;
            }
        }
        finally
        {
            _emittingDepth--;
            if (_emittingDepth == 0)
                Compact(slots);
        }
    }

    #endregion

    #region Private helpers

    private static void Compact(List<Slot> slots)
    {
        var writeIndex = 0;

        for (var readIndex = 0; readIndex < slots.Count; readIndex++)
        {
            var slot = slots[readIndex];
            if (slot.Callback is null)
                continue;

            if (writeIndex != readIndex)
                slots[writeIndex] = slot;

            writeIndex++;
        }

        if (writeIndex < slots.Count)
            slots.RemoveRange(writeIndex, slots.Count - writeIndex);
    }

    #endregion
}

#endregion