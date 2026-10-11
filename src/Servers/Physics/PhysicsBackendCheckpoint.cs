namespace Electron2D;

/// <summary>Owns selected solver history and its implementation-local observation caches.</summary>
public abstract class PhysicsBackendCheckpoint : IDisposable
{
    private int _claimed;
    internal void Claim() { if (Interlocked.CompareExchange(ref _claimed, 1, 0) != 0) throw new InvalidOperationException("A solver history object can belong to only one checkpoint."); }
    /// <summary>Supplies the selected implementation operation: device capacity bytes.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract long DeviceCapacityBytes { get; }
    /// <summary>Supplies the selected implementation operation: capture.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void Capture();
    /// <summary>Supplies the selected implementation operation: validate restore.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void ValidateRestore();
    /// <summary>Supplies the selected implementation operation: restore.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void Restore();
    /// <summary>Releases implementation-private history storage after the engine retires its checkpoint.</summary>
    public abstract void Dispose();
}
