namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    /// <summary>Retains resident solver history, slot-indexed contact observations and full/velocity cache qualification for one GPU world.</summary>
    internal sealed class GPUCheckpoint : PhysicsBackendCheckpoint
    {
        private readonly PhysicsSpace _space;
        private readonly GPUPhysicsBodyStore _store;
        private readonly GPUPhysicsBodyStore.Checkpoint _kernel;
        private readonly Dictionary<PhysicsColliderBackend, GPUPhysicsColliderImplementation.ReplayState> _colliders = [];
        private GPUPhysicsBodyStore.ContactReport[] _reports = [];
        private (int Offset, int Count)[] _ranges = [];
        private long _epoch, _publication, _velocityEpoch;
        private bool _wake, _disposed;

        internal GPUCheckpoint(PhysicsSpace space, GPUPhysicsBodyStore store)
        {
            _space = space; _store = store; _kernel = store.CreateCheckpoint();
            try { CaptureObservers(); }
            catch { _kernel.Dispose(); throw; }
        }
        protected internal override long DeviceCapacityBytes => _kernel.DeviceCapacityBytes;
        protected internal override void Capture() { RequireAlive(); _kernel.Capture(); CaptureObservers(); }
        private void CaptureObservers()
        {
            var s = _space; s.MaterializeGPUContactReports();
            PhysicsReplayCopy.Buffer<GPUPhysicsBodyStore.ContactReport>(s._gpuReports, ref _reports);
            PhysicsReplayCopy.Buffer<(int Offset, int Count)>(s._gpuReportRanges, ref _ranges);
            _ranges.AsSpan(s._gpuReportRanges.Length).Clear();
            _epoch = s.GPUStateEpoch; _publication = s.GPUStatePublicationEpoch; _velocityEpoch = s.GPUVelocityEpoch; _wake = s._gpuWakePending;
            _colliders.Clear(); _colliders.EnsureCapacity(s._bodies.Count + s._areas.Count + s._serverColliders.Count);
            foreach (var body in s._bodies) CaptureCollider(body.Backend);
            foreach (var area in s._areas) CaptureCollider(area.Backend);
            foreach (var body in s._serverColliders) CaptureCollider(body.Backend);
        }
        private void CaptureCollider(PhysicsColliderBackend collider) => _colliders.Add(collider, _store.NativeCollider(collider).CaptureReplay());
        private void RequireAlive() { if (_disposed) throw new ObjectDisposedException(nameof(GPUCheckpoint)); }
        protected internal override void ValidateRestore() { RequireAlive(); _kernel.ValidateRestore(); }
        protected internal override void Restore()
        {
            ValidateRestore(); _kernel.Restore(); var s = _space; s._gpuUseContactKeys = false;
            PhysicsReplayCopy.Buffer<GPUPhysicsBodyStore.ContactReport>(_reports, ref s._gpuReports);
            PhysicsReplayCopy.Buffer<(int Offset, int Count)>(_ranges, ref s._gpuReportRanges);
            s._gpuReportRanges.AsSpan(_ranges.Length).Clear();
            s.GPUStateEpoch = _epoch; s.GPUStatePublicationEpoch = _publication; s.GPUVelocityEpoch = _velocityEpoch; s._gpuWakePending = _wake;
            foreach (var pair in _colliders) _store.NativeCollider(pair.Key).RestoreReplay(pair.Value);
        }
        public override void Dispose()
        {
            if (_disposed) return;
            try { _kernel.Dispose(); }
            finally { _disposed = true; _colliders.Clear(); _ranges = []; _reports = []; }
        }
    }
}
