namespace Electron2D;

/// <summary>Owns selected solver history and its implementation-local observation caches.</summary>
internal abstract class PhysicsBackendCheckpoint : IDisposable
{
    private int _claimed;
    internal void Claim() { if (Interlocked.CompareExchange(ref _claimed, 1, 0) != 0) throw new InvalidOperationException("A solver history object can belong to only one checkpoint."); }
    internal abstract long DeviceCapacityBytes { get; }
    internal abstract void Capture();
    internal abstract void ValidateRestore();
    internal abstract void Restore();
    public abstract void Dispose();
}
