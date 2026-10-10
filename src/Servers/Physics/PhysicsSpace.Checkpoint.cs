namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private readonly List<Checkpoint> _checkpoints = [];
    private Exception? _checkpointFailure;

    /// <summary>Captures local simulation and scene/server observer state between completed intervals.</summary>
    internal Checkpoint CreateCheckpoint()
    {
        var checkpoint = new Checkpoint(this);
        try { checkpoint.Capture(); _checkpoints.Add(checkpoint); return checkpoint; }
        catch { checkpoint.Dispose(); throw; }
    }

    internal void EnsureCheckpointAccess()
    {
        EnsureQueryAccess();
        foreach (var body in _bodies) body.EnsurePhysicsReplayAccess();
        foreach (var area in _areas) area.EnsurePhysicsReplayAccess();
        foreach (var joint in _joints) joint.EnsurePhysicsReplayAccess();
        if (DispatchingCallbacks ||
            _overlapEvents.Count != 0 || _contactEvents.Count != 0 || _sleepEvents.Count != 0 || _serverAreaEvents.Count != 0)
            throw new InvalidOperationException("Physics checkpoints require a completed interval with dispatched events.");
    }

    /// <summary>Reusable same-world rewind with fixed identities and authored configuration; no gameplay callbacks run during restore.</summary>
    internal sealed class Checkpoint : IDisposable
    {
        private PhysicsSpace? _space;
        private PhysicsSpace Space => _space ?? throw new ObjectDisposedException(nameof(Checkpoint));
        private PhysicsBackendCheckpoint? _solver;
        private bool _valid;
        private readonly List<PhysicsReplayEntry> _entries = [];
        private readonly List<PhysicsReplayEntry> _poses = [];
        private readonly List<(PhysicsJointRuntime Joint, PhysicsJointRuntime.ReplayConfiguration Configuration)> _joints = [];
        private readonly List<Joint> _sceneJoints = [];
        private GPUPhysicsBodyStore.ContactReport[] _reports = [];
        private readonly Dictionary<int, (int Offset, int Count)> _ranges = [];
        private Vector2[] _debug = [];
        private int _debugCount;
        private bool _wake, _coldStep;
        private long _epoch, _publication;
        private ulong _tick;
        private float _delta;
        private Vector2 _defaultGravity;
        private Statistics _statistics;
        private Configuration _configuration;
        private readonly record struct Configuration(GPUPhysicsBodyStore.FieldParameters Fields, PhysicsSleepSettings Sleep,
            PhysicsContactSettings Contacts, int Iterations, float Bias);
        private static Configuration Settings(PhysicsSpace s) => new(ReplayFields(s.DefaultAreaFields), s.SleepSettings, s.ContactSettings, s.SolverIterations, s.ConstraintDefaultBias);
        internal Checkpoint(PhysicsSpace space) => _space = space;
        internal bool IsDisposed => _space is null;
        internal ulong CapturedTick
        {
            get
            {
                Space.EnsureQueryAccess();
                if (!_valid) throw new InvalidOperationException("The checkpoint has no completed capture.");
                return _tick;
            }
        }
        internal long DeviceCapacityBytes { get { Space.EnsureCheckpointAccess(); return _solver?.DeviceCapacityBytes ?? 0; } }

        internal void Capture()
        {
            var s = Space; s.EnsureCheckpointAccess(); _valid = false;
            s.PrepareForQuery();
            foreach (var joint in s._joints) joint.PrepareBackend();
            // The kernel flushes pending device commands without downloading a full body-state mirror.
            if (s.GPUStore is { } store)
            {
                foreach (var body in s._bodies) s.GPUCollider(body.Backend).PrepareGPUParameters(body.Runtime);
                uint areaOrder = 0;
                foreach (var area in s._areas) store.SetAreaFields(s.GPUCollider(area.Backend).GPUHandle, GPUFields(area.Fields), areaOrder++);
                foreach (var body in s._serverColliders)
                    if (body.IsArea) store.SetAreaFields(s.GPUCollider(body.Backend).GPUHandle, GPUFields(body.AreaFields!), areaOrder++);
                    else s.GPUCollider(body.Backend).PrepareGPUParameters(body.Runtime);
                foreach (var joint in s._jointRuntimes) joint.ApplySolverPolicy();
                s.SyncGPUExceptions();
            }
            if (_solver is null) _solver = s._backend.CreateCheckpoint(); else _solver.Capture();
            var index = 0;
            foreach (var body in s._bodies) CaptureEntry(body.Backend, body, null, ref index);
            foreach (var area in s._areas) CaptureEntry(area.Backend, area, null, ref index);
            foreach (var server in s._serverColliders) CaptureEntry(server.Backend, null, server, ref index);
            if (_entries.Count > index) _entries.RemoveRange(index, _entries.Count - index);
            PhysicsReplayCopy.List(_entries, _poses);
            _poses.Sort(static (a, b) => a.Depth.CompareTo(b.Depth));
            _joints.Clear(); _joints.EnsureCapacity(s._jointRuntimes.Count);
            foreach (var joint in s._jointRuntimes) _joints.Add((joint, joint.CaptureReplay()));
            PhysicsReplayCopy.List(s._joints, _sceneJoints);
            PhysicsReplayCopy.Buffer<GPUPhysicsBodyStore.ContactReport>(s._gpuReports, ref _reports); PhysicsReplayCopy.Map(s._gpuReportRanges, _ranges);
            PhysicsReplayCopy.Buffer<Vector2>(s._debugContacts.AsSpan(0, s._debugContactCount), ref _debug); _debugCount = s._debugContactCount;
            _coldStep = s.PortableColdStep;
            _tick = s.Tick;
            _defaultGravity = s._defaultGravity;
            _configuration = Settings(s); _delta = s.LastStep; _statistics = s.PublishedStatistics;
            _epoch = s.GPUStateEpoch; _publication = s.GPUStatePublicationEpoch; _wake = s._gpuWakePending;
            _valid = true;
        }
        private void CaptureEntry(PhysicsColliderBackend backend, CollisionObject? scene, PhysicsServerCollider? server, ref int index)
        {
            if (index == _entries.Count) _entries.Add(new(backend, scene, server));
            else if (_entries[index].Backend != backend) _entries[index] = new(backend, scene, server);
            _entries[index++].Capture();
        }

        internal void Restore()
        {
            var s = Space; s.EnsureCheckpointAccess();
            if (!_valid) throw new InvalidOperationException("The checkpoint has no completed capture.");
            PhysicsReplayCopy.Require(Settings(s) == _configuration && _entries.Count == s._bodies.Count + s._areas.Count + s._serverColliders.Count &&
                _joints.Count == s._jointRuntimes.Count && _sceneJoints.Count == s._joints.Count);
            var index = 0;
            foreach (var body in s._bodies) PhysicsReplayCopy.Require(_entries[index++].Scene == body);
            foreach (var area in s._areas) PhysicsReplayCopy.Require(_entries[index++].Scene == area);
            foreach (var server in s._serverColliders) PhysicsReplayCopy.Require(_entries[index++].Server == server);
            foreach (var entry in _entries) entry.Validate(s);
            for (var i = 0; i < _joints.Count; i++)
                PhysicsReplayCopy.Require(_joints[i].Joint == s._jointRuntimes[i] && _joints[i].Configuration == s._jointRuntimes[i].CaptureReplay());
            for (var i = 0; i < _sceneJoints.Count; i++)
                PhysicsReplayCopy.Require(_sceneJoints[i] == s._joints[i] && !_sceneJoints[i].HasPendingReplayConfiguration);
            _solver!.ValidateRestore();
            try
            {
                _solver!.Restore();
                PhysicsReplayCopy.Buffer<GPUPhysicsBodyStore.ContactReport>(_reports, ref s._gpuReports); PhysicsReplayCopy.Map(_ranges, s._gpuReportRanges);
                s.PortableColdStep = _coldStep;
                s.Tick = _tick;
                s._defaultGravity = _defaultGravity;
                s.LastStep = _delta;
                s.GPUStateEpoch = _epoch; s.GPUStatePublicationEpoch = _publication; s._gpuWakePending = _wake;
                foreach (var entry in _entries) entry.Restore();
                foreach (var entry in _poses) entry.RestorePose();
                s._debugContactCount = Math.Min(_debugCount, s._debugContactLimit);
                _debug.AsSpan(0, s._debugContactCount).CopyTo(s._debugContacts); s.DebugContactRevision++;
                PhysicsServer.Service.PublishStatistics(s, _statistics);
            }
            catch (Exception error) { s._checkpointFailure = error; throw; }
        }

        public void Dispose()
        {
            if (_space is not { } s) return;
            if (Environment.CurrentManagedThreadId != s._ownerThreadID) throw new InvalidOperationException("A physics checkpoint requires the space owner thread.");
            _solver?.Dispose(); s._checkpoints.Remove(this); _space = null; _valid = false;
            _entries.Clear(); _poses.Clear(); _joints.Clear(); _sceneJoints.Clear(); _ranges.Clear();
            _reports = []; _debug = [];
        }
    }
}
