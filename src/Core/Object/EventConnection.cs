using System.Runtime.ExceptionServices;

namespace Electron2D;

/// <summary>Owns a typed event subscription that can be disconnected, delivered once, or scheduled for later delivery.</summary>
/// <remarks>
/// This type wraps ordinary C# event accessors without introducing string-addressed signals or untyped arguments.
/// Disposing the connection removes its wrapper and cancels deferred callbacks that have not started. A callback that
/// has already started can finish concurrently with disposal.
/// Prepared synchronous delivery and delivery through the exact SceneTree.Defer scheduler use typed invokers
/// without per-emission argument capture. Scene-owned slots retain independent values until completion/discard;
/// their first capacity growth, subscription and supplied handler/accessor allocations remain separate paths.
/// Arbitrary custom schedulers retain their independent captured-action behavior. Reusable zero-argument
/// custom delivery reuses an immutable callback and still submits once per emission.
/// Capturing a reference preserves identity, not ownership or lifetime. Borrowed callback values must remain
/// valid under their owning API; deferred delivery does not extend a borrowed input event lifetime.
/// </remarks>
public sealed class EventConnection : IDisposable
{
    private Action? Finished;
    internal void OnFinished(Action callback)
    {
        lock (_gate)
        {
            if (_state is not (ConnectionState.Completed or ConnectionState.Disposed)) { Finished = callback; return; }
        }
        callback();
    }
    internal bool OneShot => _oneShot;
    internal bool Deferred { get; }
    private readonly object _gate = new();
    private readonly bool _oneShot;
    private readonly Action? _deferredZeroCallback;
    private Action<Action>? _defer;
    private Delegate? _handler;
    private Delegate? _apply;
    private IDeferredScheduler? _typedScheduler;
    private Delegate? _typedCallback;
    private Action? _releaseTyped;
    private Action? _unsubscribe;
    private ConnectionState _state = ConnectionState.Active;

    private EventConnection(Delegate handler, bool oneShot, Action<Action>? defer)
    {
        _handler = handler;
        _oneShot = oneShot;
        _defer = defer;
        Deferred = defer is not null;
        if (!oneShot && defer is not null && handler is Action)
            _deferredZeroCallback = InvokeDeferred;
    }

    /// <summary>Gets whether this connection still accepts event emissions.</summary>
    /// <value>
    /// <see langword="true"/> until disposal or, for a one-shot connection, until the first emission is accepted.
    /// A deferred one-shot callback can still be pending when this property is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// This reports the token's logical state. It cannot detect a publisher that independently clears or replaces its
    /// event invocation list.
    /// </remarks>
    public bool IsConnected
    {
        get
        {
            lock (_gate)
                return _state == ConnectionState.Active;
        }
    }

    /// <summary>Creates a managed subscription to an event with no arguments.</summary>
    /// <param name="subscribe">Adds the supplied wrapper to the event.</param>
    /// <param name="unsubscribe">Removes the same wrapper from the event.</param>
    /// <param name="handler">Receives accepted event emissions.</param>
    /// <param name="oneShot">Whether only the first accepted emission can invoke the handler.</param>
    /// <param name="defer">
    /// An optional scheduler that accepts work for later execution. Pass <see cref="SceneTree.Defer"/> for scene-tree
    /// deferred delivery; pass <see langword="null"/> for synchronous delivery.
    /// </param>
    /// <returns>An idempotently disposable connection token.</returns>
    /// <remarks>
    /// For one-shot delivery, the wrapper is disconnected before the handler runs, which prevents re-entrant duplicate
    /// delivery. Disposing a deferred connection cancels callbacks that have not started. Handler exceptions propagate
    /// on the invoking thread for synchronous delivery or through the selected scheduler for deferred delivery.
    /// </remarks>
    /// <exception cref="ArgumentNullException">A required delegate is <see langword="null"/>.</exception>
    /// <exception cref="Exception">
    /// A supplied event accessor or scheduler throws. If subscription fails after partially attaching the wrapper,
    /// rollback is attempted; failures from both operations are reported as an <see cref="AggregateException"/>.
    /// </exception>
    public static EventConnection Subscribe(
        Action<Action> subscribe,
        Action<Action> unsubscribe,
        Action handler,
        bool oneShot = false,
        Action<Action>? defer = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Create(
            subscribe,
            unsubscribe,
            handler,
            connection =>
            {
                connection.PrepareInvocation<Action, ValueTuple>(static (handler, _) => handler());
                return connection.Dispatch;
            },
            oneShot,
            defer);
    }

    /// <summary>Creates a managed subscription to an event with one typed argument.</summary>
    /// <typeparam name="T">The event argument type.</typeparam>
    /// <param name="subscribe">Adds the supplied wrapper to the event.</param>
    /// <param name="unsubscribe">Removes the same wrapper from the event.</param>
    /// <param name="handler">Receives accepted event emissions.</param>
    /// <param name="oneShot">Whether only the first accepted emission can invoke the handler.</param>
    /// <param name="defer">
    /// An optional scheduler that accepts work for later execution. Pass <see cref="SceneTree.Defer"/> for scene-tree
    /// deferred delivery; pass <see langword="null"/> for synchronous delivery.
    /// </param>
    /// <returns>An idempotently disposable connection token.</returns>
    /// <remarks>
    /// Event arguments are captured at emission time. For one-shot delivery, the wrapper is disconnected before the
    /// handler runs or is scheduled. Disposing a deferred connection cancels callbacks that have not started.
    /// </remarks>
    /// <exception cref="ArgumentNullException">A required delegate is <see langword="null"/>.</exception>
    /// <exception cref="Exception">
    /// A supplied event accessor or scheduler throws. If subscription fails after partially attaching the wrapper,
    /// rollback is attempted; failures from both operations are reported as an <see cref="AggregateException"/>.
    /// </exception>
    public static EventConnection Subscribe<T>(
        Action<Action<T>> subscribe,
        Action<Action<T>> unsubscribe,
        Action<T> handler,
        bool oneShot = false,
        Action<Action>? defer = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Create(
            subscribe,
            unsubscribe,
            handler,
            connection =>
            {
                connection.PrepareInvocation<Action<T>, T>(static (handler, value) => handler(value));
                return connection.Dispatch;
            },
            oneShot,
            defer);
    }

    /// <summary>Creates a managed subscription to an event with two typed arguments.</summary>
    /// <typeparam name="T1">The first event argument type.</typeparam>
    /// <typeparam name="T2">The second event argument type.</typeparam>
    /// <param name="subscribe">Adds the supplied wrapper to the event.</param>
    /// <param name="unsubscribe">Removes the same wrapper from the event.</param>
    /// <param name="handler">Receives accepted event emissions.</param>
    /// <param name="oneShot">Whether only the first accepted emission can invoke the handler.</param>
    /// <param name="defer">
    /// An optional scheduler that accepts work for later execution. Pass <see cref="SceneTree.Defer"/> for scene-tree
    /// deferred delivery; pass <see langword="null"/> for synchronous delivery.
    /// </param>
    /// <returns>An idempotently disposable connection token.</returns>
    /// <remarks>
    /// Event arguments are captured at emission time. For one-shot delivery, the wrapper is disconnected before the
    /// handler runs or is scheduled. Disposing a deferred connection cancels callbacks that have not started.
    /// </remarks>
    /// <exception cref="ArgumentNullException">A required delegate is <see langword="null"/>.</exception>
    /// <exception cref="Exception">
    /// A supplied event accessor or scheduler throws. If subscription fails after partially attaching the wrapper,
    /// rollback is attempted; failures from both operations are reported as an <see cref="AggregateException"/>.
    /// </exception>
    public static EventConnection Subscribe<T1, T2>(
        Action<Action<T1, T2>> subscribe,
        Action<Action<T1, T2>> unsubscribe,
        Action<T1, T2> handler,
        bool oneShot = false,
        Action<Action>? defer = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Create(
            subscribe,
            unsubscribe,
            handler,
            connection =>
            {
                connection.PrepareInvocation<Action<T1, T2>, ValueTuple<T1, T2>>(static (handler, value) => handler(value.Item1, value.Item2));
                return connection.Dispatch;
            },
            oneShot,
            defer);
    }

    /// <summary>Creates a managed subscription to an event with three typed arguments.</summary>
    /// <typeparam name="T1">The first event argument type.</typeparam>
    /// <typeparam name="T2">The second event argument type.</typeparam>
    /// <typeparam name="T3">The third event argument type.</typeparam>
    /// <param name="subscribe">Adds the supplied wrapper to the event.</param>
    /// <param name="unsubscribe">Removes the same wrapper from the event.</param>
    /// <param name="handler">Receives accepted event emissions.</param>
    /// <param name="oneShot">Whether only the first accepted emission can invoke the handler.</param>
    /// <param name="defer">
    /// An optional scheduler that accepts work for later execution. Pass <see cref="SceneTree.Defer"/> for scene-tree
    /// deferred delivery; pass <see langword="null"/> for synchronous delivery.
    /// </param>
    /// <returns>An idempotently disposable connection token.</returns>
    /// <remarks>
    /// Event arguments are captured at emission time. For one-shot delivery, the wrapper is disconnected before the
    /// handler runs or is scheduled. Disposing a deferred connection cancels callbacks that have not started.
    /// </remarks>
    /// <exception cref="ArgumentNullException">A required delegate is <see langword="null"/>.</exception>
    /// <exception cref="Exception">
    /// A supplied event accessor or scheduler throws. If subscription fails after partially attaching the wrapper,
    /// rollback is attempted; failures from both operations are reported as an <see cref="AggregateException"/>.
    /// </exception>
    public static EventConnection Subscribe<T1, T2, T3>(
        Action<Action<T1, T2, T3>> subscribe,
        Action<Action<T1, T2, T3>> unsubscribe,
        Action<T1, T2, T3> handler,
        bool oneShot = false,
        Action<Action>? defer = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Create(
            subscribe,
            unsubscribe,
            handler,
            connection =>
            {
                connection.PrepareInvocation<Action<T1, T2, T3>, ValueTuple<T1, T2, T3>>(static (handler, value) => handler(value.Item1, value.Item2, value.Item3));
                return connection.Dispatch;
            },
            oneShot,
            defer);
    }

    /// <summary>Creates a managed subscription to an event with four typed arguments.</summary>
    /// <typeparam name="T1">The first event argument type.</typeparam>
    /// <typeparam name="T2">The second event argument type.</typeparam>
    /// <typeparam name="T3">The third event argument type.</typeparam>
    /// <typeparam name="T4">The fourth event argument type.</typeparam>
    /// <param name="subscribe">Adds the supplied wrapper to the event.</param>
    /// <param name="unsubscribe">Removes the same wrapper from the event.</param>
    /// <param name="handler">Receives accepted event emissions.</param>
    /// <param name="oneShot">Whether only the first accepted emission can invoke the handler.</param>
    /// <param name="defer">
    /// An optional scheduler that accepts work for later execution. Pass <see cref="SceneTree.Defer"/> for scene-tree
    /// deferred delivery; pass <see langword="null"/> for synchronous delivery.
    /// </param>
    /// <returns>An idempotently disposable connection token.</returns>
    /// <remarks>
    /// Event arguments are captured at emission time. For one-shot delivery, the wrapper is disconnected before the
    /// handler runs or is scheduled. Disposing a deferred connection cancels callbacks that have not started.
    /// </remarks>
    /// <exception cref="ArgumentNullException">A required delegate is <see langword="null"/>.</exception>
    /// <exception cref="Exception">
    /// A supplied event accessor or scheduler throws. If subscription fails after partially attaching the wrapper,
    /// rollback is attempted; failures from both operations are reported as an <see cref="AggregateException"/>.
    /// </exception>
    public static EventConnection Subscribe<T1, T2, T3, T4>(
        Action<Action<T1, T2, T3, T4>> subscribe,
        Action<Action<T1, T2, T3, T4>> unsubscribe,
        Action<T1, T2, T3, T4> handler,
        bool oneShot = false,
        Action<Action>? defer = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Create(
            subscribe,
            unsubscribe,
            handler,
            connection =>
            {
                connection.PrepareInvocation<Action<T1, T2, T3, T4>, ValueTuple<T1, T2, T3, T4>>(static (handler, value) => handler(value.Item1, value.Item2, value.Item3, value.Item4));
                return connection.Dispatch;
            },
            oneShot,
            defer);
    }

    /// <summary>Disconnects the wrapper and cancels deferred callbacks that have not started.</summary>
    /// <remarks>
    /// The operation is idempotent and safe to race with event delivery. It does not wait for a handler that has already
    /// started. The connection remains terminal even if the supplied removal accessor throws.
    /// </remarks>
    /// <exception cref="Exception">The supplied event removal accessor throws.</exception>
    public void Dispose()
    {
        Action? unsubscribe;

        lock (_gate)
        {
            if (_state is ConnectionState.Completed or ConnectionState.Disposed)
                return;

            _state = ConnectionState.Disposed;
            unsubscribe = _unsubscribe;
            _unsubscribe = null;
            _defer = null;
            _handler = null; _typedScheduler = null; _typedCallback = null;
        }

        try { ReleaseTyped(); unsubscribe?.Invoke(); } finally { Interlocked.Exchange(ref Finished, null)?.Invoke(); }
    }

    private static EventConnection Create<TDelegate>(
        Action<TDelegate> subscribe,
        Action<TDelegate> unsubscribe,
        TDelegate handler,
        Func<EventConnection, TDelegate> createWrapper,
        bool oneShot,
        Action<Action>? defer)
        where TDelegate : Delegate
    {
        ArgumentNullException.ThrowIfNull(subscribe);
        ArgumentNullException.ThrowIfNull(unsubscribe);

        var connection = new EventConnection(handler, oneShot, defer);
        var wrapper = createWrapper(connection);
        connection._unsubscribe = () => unsubscribe(wrapper);

        try
        {
            subscribe(wrapper);
        }
        catch (Exception subscriptionError)
        {
            try
            {
                connection.Dispose();
            }
            catch (Exception rollbackError)
            {
                throw new AggregateException("Event subscription and rollback both failed.", subscriptionError, rollbackError);
            }

            ExceptionDispatchInfo.Capture(subscriptionError).Throw();
        }

        return connection;
    }

    private void Dispatch()
    {
        if (_oneShot)
        {
            DispatchOneShot<Action, ValueTuple>(default);
            return;
        }

        if (!TryGetScheduler(out var defer, out var typedScheduler, out var typedCallback))
            return;

        if (defer is null)
        {
            if (TryGetHandler<Action>(out var handler))
                handler();
            return;
        }

        defer(_deferredZeroCallback!);
    }

    private void Dispatch<T>(T value)
    {
        if (_oneShot)
        {
            DispatchOneShot<Action<T>, T>(value);
            return;
        }

        if (!TryGetScheduler(out var defer, out var typedScheduler, out var typedCallback))
            return;

        if (defer is not null)
        {
            if (typedScheduler is null) DispatchDeferred(defer, value);
            else QueueTyped(typedScheduler, typedCallback!, value);
            return;
        }

        if (TryGetHandler<Action<T>>(out var handler))
            handler(value);
    }

    private void Dispatch<T1, T2>(T1 first, T2 second)
    {
        if (_oneShot)
        {
            DispatchOneShot<Action<T1, T2>, ValueTuple<T1, T2>>((first, second));
            return;
        }

        if (!TryGetScheduler(out var defer, out var typedScheduler, out var typedCallback))
            return;

        if (defer is not null)
        {
            if (typedScheduler is null) DispatchDeferred(defer, first, second);
            else QueueTyped(typedScheduler, typedCallback!, (first, second));
            return;
        }

        if (TryGetHandler<Action<T1, T2>>(out var handler))
            handler(first, second);
    }

    private void Dispatch<T1, T2, T3>(T1 first, T2 second, T3 third)
    {
        if (_oneShot)
        {
            DispatchOneShot<Action<T1, T2, T3>, ValueTuple<T1, T2, T3>>((first, second, third));
            return;
        }

        if (!TryGetScheduler(out var defer, out var typedScheduler, out var typedCallback))
            return;

        if (defer is not null)
        {
            if (typedScheduler is null) DispatchDeferred(defer, first, second, third);
            else QueueTyped(typedScheduler, typedCallback!, (first, second, third));
            return;
        }

        if (TryGetHandler<Action<T1, T2, T3>>(out var handler))
            handler(first, second, third);
    }

    private void Dispatch<T1, T2, T3, T4>(T1 first, T2 second, T3 third, T4 fourth)
    {
        if (_oneShot)
        {
            DispatchOneShot<Action<T1, T2, T3, T4>, ValueTuple<T1, T2, T3, T4>>((first, second, third, fourth));
            return;
        }

        if (!TryGetScheduler(out var defer, out var typedScheduler, out var typedCallback))
            return;

        if (defer is not null)
        {
            if (typedScheduler is null) DispatchDeferred(defer, first, second, third, fourth);
            else QueueTyped(typedScheduler, typedCallback!, (first, second, third, fourth));
            return;
        }

        if (TryGetHandler<Action<T1, T2, T3, T4>>(out var handler))
            handler(first, second, third, fourth);
    }

    private void DispatchDeferred<T>(Action<Action> defer, T value) =>
        defer(() => InvokeDeferred(value));

    private void DispatchDeferred<T1, T2>(Action<Action> defer, T1 first, T2 second) =>
        defer(() => InvokeDeferred(first, second));

    private void DispatchDeferred<T1, T2, T3>(Action<Action> defer, T1 first, T2 second, T3 third) =>
        defer(() => InvokeDeferred(first, second, third));

    private void DispatchDeferred<T1, T2, T3, T4>(Action<Action> defer, T1 first, T2 second, T3 third, T4 fourth) =>
        defer(() => InvokeDeferred(first, second, third, fourth));

    private void PrepareInvocation<TDelegate, TPayload>(Action<TDelegate, TPayload> apply) where TDelegate : Delegate
    {
        _apply = apply;
        var scheduler = DeferredSchedulerRegistry.Find(_defer);
        if (scheduler is null || !_oneShot && typeof(TDelegate) == typeof(Action)) return;
        scheduler.Prepare<TPayload>();
        _releaseTyped = scheduler.Release<TPayload>;
        _typedScheduler = scheduler;
        _typedCallback = new Action<TPayload>(InvokeTyped<TDelegate, TPayload>);
    }

    private static void QueueTyped<T>(IDeferredScheduler scheduler, Delegate callback, T value) => scheduler.Enqueue((Action<T>)callback, value);

    private void DispatchOneShot<TDelegate, TPayload>(TPayload value) where TDelegate : Delegate
    {
        Action? unsubscribe;
        Action<Action>? defer;
        IDeferredScheduler? typedScheduler;
        Delegate? typedCallback;
        TDelegate handler;
        lock (_gate)
        {
            if (_state != ConnectionState.Active) return;
            handler = (TDelegate)_handler!;
            defer = _defer; typedScheduler = _typedScheduler; typedCallback = _typedCallback;
            _defer = null;
            _state = defer is not null ? ConnectionState.Pending : ConnectionState.Completed;
            unsubscribe = _unsubscribe; _unsubscribe = null;
            if (defer is null) { _handler = null; _typedScheduler = null; _typedCallback = null; }
        }
        try
        {
            unsubscribe?.Invoke();
            if (defer is null) Interlocked.Exchange(ref Finished, null)?.Invoke();
        }
        catch { CompletePending(); throw; }
        if (defer is null) { ((Action<TDelegate, TPayload>)_apply!)(handler, value); return; }
        lock (_gate) { if (_state != ConnectionState.Pending) return; }
        try
        {
            if (typedScheduler is not null) QueueTyped(typedScheduler, typedCallback!, value);
            else ScheduleLegacyOneShot<TDelegate, TPayload>(defer, value);
        }
        catch { CompletePending(); throw; }
    }

    private void ScheduleLegacyOneShot<TDelegate, TPayload>(Action<Action> defer, TPayload value) where TDelegate : Delegate =>
        defer(() => InvokePending<TDelegate, TPayload>(value));

    private void InvokeTyped<TDelegate, TPayload>(TPayload value) where TDelegate : Delegate
    {
        if (_oneShot) InvokePending<TDelegate, TPayload>(value);
        else if (TryGetHandler<TDelegate>(out var handler)) ((Action<TDelegate, TPayload>)_apply!)(handler, value);
    }

    private bool TryGetScheduler(out Action<Action>? defer, out IDeferredScheduler? typedScheduler, out Delegate? typedCallback)
    {
        lock (_gate)
        {
            if (_state != ConnectionState.Active)
            {
                defer = null; typedScheduler = null; typedCallback = null;
                return false;
            }

            defer = _defer; typedScheduler = _typedScheduler; typedCallback = _typedCallback;
            return true;
        }
    }

    private bool TryGetHandler<TDelegate>(out TDelegate handler)
        where TDelegate : Delegate
    {
        lock (_gate)
        {
            if (_state == ConnectionState.Active && _handler is TDelegate typedHandler)
            {
                handler = typedHandler;
                return true;
            }

            handler = null!;
            return false;
        }
    }

    private void InvokeDeferred()
    {
        if (TryGetHandler<Action>(out var handler))
            handler();
    }

    private void InvokeDeferred<T>(T value)
    {
        if (TryGetHandler<Action<T>>(out var handler))
            handler(value);
    }

    private void InvokeDeferred<T1, T2>(T1 first, T2 second)
    {
        if (TryGetHandler<Action<T1, T2>>(out var handler))
            handler(first, second);
    }

    private void InvokeDeferred<T1, T2, T3>(T1 first, T2 second, T3 third)
    {
        if (TryGetHandler<Action<T1, T2, T3>>(out var handler))
            handler(first, second, third);
    }

    private void InvokeDeferred<T1, T2, T3, T4>(T1 first, T2 second, T3 third, T4 fourth)
    {
        if (TryGetHandler<Action<T1, T2, T3, T4>>(out var handler))
            handler(first, second, third, fourth);
    }

    private void InvokePending<TDelegate, TPayload>(TPayload value) where TDelegate : Delegate
    {
        TDelegate handler;
        lock (_gate)
        {
            if (_state != ConnectionState.Pending || _handler is not TDelegate typedHandler) return;
            _state = ConnectionState.Completed;
            _handler = null; _typedScheduler = null; _typedCallback = null;
            handler = typedHandler;
        }
        try { ReleaseTyped(); ((Action<TDelegate, TPayload>)_apply!)(handler, value); }
        finally { Interlocked.Exchange(ref Finished, null)?.Invoke(); }
    }

    private void CompletePending()
    {
        lock (_gate)
        {
            if (_state == ConnectionState.Pending)
            {
                _state = ConnectionState.Completed;
                _handler = null; _typedScheduler = null; _typedCallback = null;
            }
        }
        ReleaseTyped();
        Interlocked.Exchange(ref Finished, null)?.Invoke();
    }

    private void ReleaseTyped() => Interlocked.Exchange(ref _releaseTyped, null)?.Invoke();

    private enum ConnectionState
    {
        Active,
        Pending,
        Completed,
        Disposed
    }
}
