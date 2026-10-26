namespace Electron2D;

public abstract partial class ElectronObject
{
    [ThreadStatic] private static ElectronObject? _notificationOwner;
    [ThreadStatic] private static bool _notificationReversed;

    /// <summary>Gets whether this object's current notification uses derived-first delivery.</summary>
    /// <value>False outside this object's active notification on the current thread.</value>
    /// <remarks>
    /// Overrides retain ordinary C# base calls. To follow the requested direction, run local handling before the base
    /// call when true and after it when false. Nested calls restore the previous direction on return or failure.
    /// The context does not flow to another thread or across asynchronous continuations.
    /// </remarks>
    protected bool IsNotificationReversed => ReferenceEquals(_notificationOwner, this) && _notificationReversed;

    /// <summary>Synchronously delivers a numeric notification with the requested inheritance direction.</summary>
    /// <param name="what">The notification identifier.</param>
    /// <param name="reversed">True for derived-first handling; false for the ordinary C# notification path.</param>
    /// <remarks>
    /// Built-in handlers finish their local handling before forwarding to their base when reversed. False preserves
    /// their ordinary base-call points. Custom overrides use <see cref="IsNotificationReversed"/> to order their
    /// explicit base call. An unhandled exception stops the remaining chain and propagates unchanged; existing
    /// handler-local error aggregation still applies. Direction is isolated per object, call and thread.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception"><see cref="OnNotification"/> throws.</exception>
    public void Notify(int what, bool reversed)
    {
        ThrowIfDisposed();
        DispatchNotification(what, reversed);
    }

    internal void DispatchNotification(int what, bool reversed = false)
    {
        var previousOwner = _notificationOwner;
        if (!reversed && previousOwner is null)
        {
            OnNotification(what);
            return;
        }

        var previousDirection = _notificationReversed;
        _notificationOwner = this;
        _notificationReversed = reversed;
        try { OnNotification(what); }
        finally
        {
            _notificationOwner = previousOwner;
            _notificationReversed = previousDirection;
        }
    }
}
