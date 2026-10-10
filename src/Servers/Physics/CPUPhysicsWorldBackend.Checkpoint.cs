namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    internal override PhysicsBackendCheckpoint CreateCheckpoint() => new SolverCheckpoint(this);
    private sealed class SolverCheckpoint : PhysicsBackendCheckpoint
    {
        private readonly CPUPhysicsWorldBackend _owner;
        private readonly CPUPhysicsCheckpoint _kernel;
        private readonly Dictionary<(ulong, ulong), OneWayPair> _oneWay = [];
        private BodyMotion[] _motions = [];
        private readonly List<FrameContact> _contacts = [];
        private readonly Dictionary<(int, int, ushort), int> _indices = [];
        private int[] _heads = [], _tails = [];
        private readonly List<(int A, int B)> _reportPairs = [];
        private int _reportActive;
        private bool _aggregate, _disposed;
        private long _step;
        internal SolverCheckpoint(CPUPhysicsWorldBackend owner)
        {
            _owner = owner; _kernel = new(owner.WorldID);
            try { CaptureObservers(); } catch { _kernel.Dispose(); throw; }
        }
        internal override long DeviceCapacityBytes => 0;
        internal override void Capture() { RequireAlive(); _kernel.Capture(); CaptureObservers(); }
        private void CaptureObservers()
        {
            var s = _owner;
            PhysicsReplayCopy.Map(s._oneWayPairs, _oneWay);
            PhysicsReplayCopy.Buffer<BodyMotion>(s._bodyMotions, ref _motions);
            PhysicsReplayCopy.List(s._frameContacts, _contacts); PhysicsReplayCopy.Map(s._frameContactIndices, _indices);
            PhysicsReplayCopy.Buffer<int>(s._frameContactHeads, ref _heads); PhysicsReplayCopy.Buffer<int>(s._frameContactTails, ref _tails);
            _reportPairs.Clear(); _reportPairs.EnsureCapacity(s._reportOnlyPairs.Count);
            foreach (var pair in s._reportOnlyPairs) _reportPairs.Add(pair);
            _reportActive = s._reportOnlyActive;
            _step = s._contactStep; _aggregate = s._aggregateContactImpulses;
        }
        private void RequireAlive() { if (_disposed) throw new ObjectDisposedException(nameof(SolverCheckpoint)); _owner.EnsureAccess(); }
        internal override void ValidateRestore() { RequireAlive(); _kernel.ValidateRestore(); }
        internal override void Restore()
        {
            ValidateRestore(); _kernel.Restore(); var s = _owner;
            PhysicsReplayCopy.Map(_oneWay, s._oneWayPairs);
            PhysicsReplayCopy.Buffer<BodyMotion>(_motions, ref s._bodyMotions);
            PhysicsReplayCopy.List(_contacts, s._frameContacts); PhysicsReplayCopy.Map(_indices, s._frameContactIndices);
            PhysicsReplayCopy.Buffer<int>(_heads, ref s._frameContactHeads); PhysicsReplayCopy.Buffer<int>(_tails, ref s._frameContactTails);
            s._reportOnlyPairs.Clear(); s._reportOnlyPairs.EnsureCapacity(_reportPairs.Count);
            foreach (var pair in _reportPairs) s._reportOnlyPairs.Add(pair);
            s._reportOnlyActive = _reportActive;
            s._contactStep = _step; s._aggregateContactImpulses = _aggregate;
        }
        public override void Dispose()
        {
            if (_disposed) return; _kernel.Dispose(); _disposed = true;
            _reportPairs.Clear(); _oneWay.Clear(); _contacts.Clear(); _indices.Clear(); _motions = []; _heads = []; _tails = [];
        }
    }
}
