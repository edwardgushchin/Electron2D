namespace Electron2D;

public partial class ElectronObject
{
    private int _blockSignals;

    /// <summary>Controls whether this object may begin ordinary event emissions.</summary>
    /// <param name="enable">True to suppress new emissions; false to allow them.</param>
    /// <remarks>Blocking does not remove subscriptions, consume one-shot connections, cancel accepted deferred
    /// callbacks, pause processing, or suppress notifications and deterministic disposal. The initial value is false.
    /// The existing derived mutation validator applies before every assignment, including an unchanged value.</remarks>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="InvalidOperationException">A derived mutation policy rejects the change.</exception>
    public void SetBlockSignals(bool enable)
    {
        ValidateMutation();
        Volatile.Write(ref _blockSignals, enable ? 1 : 0);
    }

    /// <summary>Returns whether this object suppresses new ordinary event emissions.</summary>
    /// <returns>True when blocked; false by default.</returns>
    /// <remarks>This reads only the object's atomic delivery policy, without observing scene or backend state.</remarks>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    public bool IsBlockingSignals()
    {
        ThrowIfDisposed();
        return Volatile.Read(ref _blockSignals) != 0;
    }

    /// <summary>Selects the typed handler snapshot for one ordinary event emission.</summary>
    /// <typeparam name="TDelegate">The event's concrete delegate type.</typeparam>
    /// <param name="handlers">The publisher's captured event delegate, or null when no listeners exist.</param>
    /// <returns>The same delegate when emissions are enabled; otherwise null.</returns>
    /// <remarks>Derived publishers immediately invoke the returned delegate with their typed arguments.
    /// Selection happens once: changing the policy inside a handler affects later emissions, while the selected
    /// multicast snapshot retains its order and exception behavior. The operation allocates nothing and preserves
    /// custom delegate signatures, including borrowed spans. The publishing member retains responsibility for
    /// lifetime and thread validation. Direct CLR delegate calls remain ordinary language operations.</remarks>
    protected TDelegate? BeginEventEmission<TDelegate>(TDelegate? handlers) where TDelegate : Delegate =>
        Volatile.Read(ref _blockSignals) == 0 ? handlers : null;

    internal EventHandlers<TDelegate>.Snapshot BeginBufferedEvent<TDelegate>(EventHandlers<TDelegate>? handlers) where TDelegate : Delegate =>
        Volatile.Read(ref _blockSignals) == 0 ? handlers?.Acquire() ?? default : default;
}
