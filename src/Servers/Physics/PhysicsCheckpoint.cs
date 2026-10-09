namespace Electron2D;

/// <summary>Retains a reusable local physics-world rewind point and its captured simulation tick.</summary>
/// <remarks>Create through <see cref="PhysicsServer.SpaceCreateCheckpoint"/>. This point belongs to one live
/// world and backend; it is not a serialized or portable network snapshot. Body, shape, joint and authored
/// configuration identities must still match when restoring. Storage grows during preparation and is reused.
/// The world also releases its checkpoints on disposal. Operations and first disposal require its owner thread.</remarks>
public sealed class PhysicsCheckpoint : IDisposable
{
    private readonly PhysicsSpace.Checkpoint _state;
    internal PhysicsCheckpoint(PhysicsSpace world) => _state = world.CreateCheckpoint();

    /// <summary>Gets whether this point or its source world has released the retained state.</summary>
    /// <value>True after explicit or source-world disposal.</value>
    public bool IsDisposed => _state.IsDisposed;

    /// <summary>Gets the world-local simulation tick retained by the last completed capture.</summary>
    /// <value>Zero before the world's first positive active interval; independent of packet and scene-frame counters.</value>
    /// <exception cref="ObjectDisposedException">The point or source world has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner, the world failed or is stepping, or capture did not complete.</exception>
    public ulong Tick => _state.CapturedTick;

    /// <summary>Replaces this point with the source world's current simulation, observer state and tick.</summary>
    /// <remarks>Requires a completed scene physics frame and event dispatch. Does not advance time or consume
    /// pending forces/targets. A failed copy invalidates this point; a later successful capture can replace it.</remarks>
    /// <exception cref="ObjectDisposedException">The point or source world has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The world is failed, access is off-owner, or an interval/callback/scene capture is active.</exception>
    public void Capture() => _state.Capture();

    /// <summary>Restores the captured world state and tick without invoking gameplay callbacks.</summary>
    /// <remarks>Preserves live object/direct-view identity and restores contact/overlap history. Physics pose
    /// interpolation is reset. This does not rewind scene timers, scripts or other worlds. Replayed future
    /// intervals emit their ordinary events again; applications must reconcile predicted and confirmed effects.
    /// Validation rejects before mutation. A failure after restoration starts fails the world until disposal.</remarks>
    /// <exception cref="ObjectDisposedException">The point or source world has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Capture is incomplete, identity/configuration changed, the world failed,
    /// access is off-owner, or an interval/callback/scene capture is active.</exception>
    public void Restore() => _state.Restore();

    /// <summary>Releases retained storage without disposing the source world or its objects.</summary>
    /// <remarks>Repeated disposal, including after source-world disposal, does nothing.</remarks>
    /// <exception cref="InvalidOperationException">A live point is first disposed off its owner thread.</exception>
    public void Dispose() => _state.Dispose();
}
