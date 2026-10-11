namespace Electron2D;

internal sealed partial class RegisteredPhysicsWorldBackend
{
    private sealed class History(RegisteredPhysicsWorldBackend world, PhysicsBackendCheckpoint history) : PhysicsBackendCheckpoint
    {
        private bool _disposed;
        private void Enter() { if (_disposed) throw new ObjectDisposedException(nameof(History)); world.Enter(); }
        internal override long DeviceCapacityBytes { get { Enter(); try { return history.DeviceCapacityBytes; } finally { world.Exit(); } } }
        internal override void Capture() { Enter(); try { history.Capture(); } finally { world.Exit(); } }
        internal override void ValidateRestore() { Enter(); try { history.ValidateRestore(); } finally { world.Exit(); } }
        internal override void Restore() { Enter(); try { history.Restore(); } finally { world.Exit(); } }
        public override void Dispose()
        {
            if (_disposed) return;
            Enter(); try { history.Dispose(); } finally { _disposed = true; world.Exit(); }
        }
    }
}
