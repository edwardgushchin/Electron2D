namespace Electron2D;

/// <summary>Owns selected solver history and its implementation-local observation caches.</summary>
internal abstract class PhysicsBackendCheckpoint : IDisposable
{
    internal abstract long DeviceCapacityBytes { get; }
    internal abstract void Capture();
    internal abstract void ValidateRestore();
    internal abstract void Restore();
    public abstract void Dispose();
}
