using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    private readonly List<Checkpoint> _checkpoints = [];
    private bool _checkpointRebuildHistory, _checkpointCopyActive;

    /// <summary>Retains device-local simulation history for this store and its current authored configuration.</summary>
    internal Checkpoint CreateCheckpoint()
    {
        EnsureAccess();
        var checkpoint = new Checkpoint(this);
        try { checkpoint.Capture(); _checkpoints.Add(checkpoint); return checkpoint; }
        catch { checkpoint.Dispose(); throw; }
    }

    /// <summary>Reusable motion/force/sleep/contact/joint-history checkpoint. Not a portable world or network snapshot.</summary>
    internal sealed class Checkpoint : IDisposable
    {
        private GPUPhysicsBodyStore? _store;
        private GPUPhysicsBodyStore Store => _store ?? throw new ObjectDisposedException(nameof(Checkpoint));
        private readonly Block[] _blocks = new Block[19];
        private Slot[] _slots = [];
        private ShapeSlot[] _shapes = [];
        private GeometryState[] _geometry = [];
        private JointSlot[] _joints = [];
        private ExceptionSlot[] _exceptions = [];
        private State _state;
        private bool _valid, _disposed;
        internal Action? BeforeSubmit;
        private struct Block { internal RenderHandle? Buffer; internal int Capacity, Count, Stride; }
        private readonly record struct GeometryState(Shape? Source, ulong Revision, bool Disposed, float Bias);
        private struct State
        {
            internal int Bodies, Shapes, Joints, Exceptions, PreviousPoints, SleepBodies, SleepEdges, Active, PublishedActive, Islands;
            internal int OneWay, OneWayTable, Reports, ReportBodies, Contacts, Pairs, Warm;
            internal long BodyVersion, ShapeVersion, FieldVersion, SleepBodyVersion, SleepShapeVersion, SleepGeometryEpoch, SteppedFields;
            internal PhysicsSleepSettings SleepSettings;
            internal PhysicsContactSettings ContactSettings;
            internal int Iterations;
            internal float Bias, SolveDelta, TickDuration, JointBias;
            internal bool WakeAll, Corrections, CaptureReports, ReportReady, ReportInitialize, FieldDirty;
            internal (int, float, float, float, float, float, float)? SleepPolicy;
            internal FieldParameters? Defaults;
        }
        internal Checkpoint(GPUPhysicsBodyStore store) => _store = store;
        internal long DeviceCapacityBytes { get { Check(); long size = 0; foreach (var block in _blocks) size += block.Capacity; return size; } }
        internal long ManagedCapacityBytes { get { Check(); return (long)_slots.Length * sizeof(Slot) + (long)_shapes.Length * Unsafe.SizeOf<ShapeSlot>() + (long)_geometry.Length * Unsafe.SizeOf<GeometryState>() + (long)_joints.Length * sizeof(JointSlot) + (long)_exceptions.Length * sizeof(ExceptionSlot); } }

        internal void Capture()
        {
            Check(); Store.EnsureAccess(); _valid = false;
            var s = Store;
            s.Step(0, default); s.FlushJoints();
            if (s._dirtyShapes.Count != 0 || s._dirtyGeometry.Count != 0 || s._spatialEpoch != Shape.GeometryEpoch) s.FindPairs();
            if (s._reportOpen) throw new InvalidOperationException("Capture requires a completed simulation interval.");
            Reserve(ref _slots, s._highWater); Reserve(ref _shapes, s._shapeHighWater); Reserve(ref _geometry, s._shapeHighWater);
            Reserve(ref _joints, s._jointHighWater); Reserve(ref _exceptions, s._exceptionHighWater);
            s._slots.AsSpan(0, s._highWater).CopyTo(_slots);
            s._shapeSlots.AsSpan(0, s._shapeHighWater).CopyTo(_shapes);
            s._jointSlots.AsSpan(0, s._jointHighWater).CopyTo(_joints);
            s._exceptionSlots.AsSpan(0, s._exceptionHighWater).CopyTo(_exceptions);
            for (var i = 0; i < s._shapeHighWater; i++)
            {
                var source = s._shapeSlots[i].Geometry?.Source;
                _geometry[i] = new(source, source?.GeometryRevision ?? 0, source?.IsDisposed ?? false, source is { IsDisposed: false } ? source.CustomSolverBias : 0);
            }
            _state = new()
            {
                Bodies = s._highWater,
                Shapes = s._shapeHighWater,
                Joints = s._jointHighWater,
                Exceptions = s._exceptionHighWater,
                PreviousPoints = s._previousPointCount,
                SleepBodies = s._sleepGraphBodies,
                SleepEdges = s._sleepEdgeCount,
                Active = s.ActiveSimulationBodyCount,
                PublishedActive = s.PublishedActiveBodyCount,
                Islands = s.PublishedIslandCount,
                OneWay = s._oneWayHistoryCount,
                OneWayTable = s.OneWayHistoryTableSize,
                Reports = s._reportCount,
                ReportBodies = s._publishedReportBodies,
                Contacts = s.ContactPointCount,
                Pairs = s.PairCount,
                Warm = s.WarmStartedPointCount,
                BodyVersion = s._bodyVersion,
                ShapeVersion = s._shapeVersion,
                FieldVersion = s._fieldVersion,
                SleepBodyVersion = s._sleepBodyVersion,
                SleepShapeVersion = s._sleepShapeVersion,
                SleepGeometryEpoch = s._sleepGeometryEpoch,
                SteppedFields = s._steppedFieldVersion,
                SleepSettings = s._sleepSettings,
                ContactSettings = s._contactSettings,
                Iterations = s._solverIterations,
                Bias = s._constraintDefaultBias,
                SolveDelta = s._previousSolveDelta,
                TickDuration = s._contactTickDuration,
                JointBias = s._jointTickBias,
                WakeAll = s._wakeAllSleep,
                Corrections = s._hasPositionCorrections,
                CaptureReports = s._captureReports,
                ReportReady = s._reportReady,
                ReportInitialize = s._reportInitialize,
                FieldDirty = s._fieldEditsDirty,
                SleepPolicy = s._sleepSolverPolicy,
                Defaults = s._steppedDefaults
            };
            for (var i = 0; i < _blocks.Length; i++)
            {
                var (buffer, count, stride) = s.CheckpointBuffer(i);
                ref var block = ref _blocks[i]; block.Count = buffer is null ? 0 : count; block.Stride = stride;
                var bytes = checked(block.Count * stride);
                if (block.Capacity < bytes)
                {
                    var replacement = s.Buffer(checked((uint)bytes));
                    block.Buffer?.Dispose(); block.Buffer = replacement; block.Capacity = bytes;
                }
            }
            Copy(restore: false); _valid = true;
        }

        internal void ValidateRestore()
        {
            Check(); Store.EnsureAccess();
            if (!_valid) throw new InvalidOperationException("The checkpoint has no completed capture.");
            ValidateConfiguration();
        }

        internal void Restore()
        {
            ValidateRestore();
            var s = Store;
            try
            {
                // Capacity changes may replace live buffers; a failure after this point cannot resume the old world.
                s._failed = true;
                for (var i = 0; i < _blocks.Length; i++) s.PrepareCheckpointBuffer(i, _blocks[i].Count);
                Copy(restore: true);
                _slots.AsSpan(0, _state.Bodies).CopyTo(s._slots); s._pendingCount = 0;
                s._bodyVersion++;
                s._sleepBodyVersion = _state.SleepBodyVersion == _state.BodyVersion ? s._bodyVersion : -1;
                s._sleepShapeVersion = _state.SleepShapeVersion == _state.ShapeVersion ? s._shapeVersion : -1;
                s._sleepGeometryEpoch = _state.SleepGeometryEpoch; s._sleepSolverPolicy = _state.SleepPolicy;
                s._wakeAllSleep = _state.WakeAll; s._sleepGraphBodies = _state.SleepBodies; s._sleepEdgeCount = _state.SleepEdges;
                s.ActiveSimulationBodyCount = _state.Active; s.PublishedActiveBodyCount = _state.PublishedActive; s.PublishedIslandCount = _state.Islands;
                s._previousPointCount = _state.PreviousPoints; s._previousSolveDelta = _state.SolveDelta;
                s._contactTickDuration = _state.TickDuration; s._jointTickBias = _state.JointBias; s._hasPositionCorrections = _state.Corrections;
                s._checkpointRebuildHistory = true;
                s._oneWayHistoryCount = _state.OneWay; s._oneWayReadCapacity = _state.OneWayTable;
                s._reportCount = _state.Reports; s._publishedReportBodies = _state.ReportBodies; s._captureReports = _state.CaptureReports;
                s._reportReady = _state.ReportReady; s._reportInitialize = _state.ReportInitialize; s._reportOpen = false;
                s._fieldEditsDirty = _state.FieldDirty; s._steppedDefaults = _state.Defaults; s._steppedFieldVersion = _state.SteppedFields;
                s.ContactPointCount = _state.Contacts; s.PairCount = _state.Pairs; s.WarmStartedPointCount = _state.Warm;
                s._pairBodyVersion = s._pairShapeVersion = s._boundsBodyVersion = s._boundsShapeVersion = s._contactPairVersion = -1;
                s._treeTopologyDirty = true; s._refitsSinceSort = 0;
                // A consumer may still hold poses from the discarded future.
                s._publishedSlots = 0; s._debugContactReady = false; s._debugContactPreviousCount = 0;
                s._failed = false;
            }
            catch { s._failed = true; throw; }
        }

        private void ValidateConfiguration()
        {
            var s = Store;
            if (s._dirtyJoints.Count != 0 || s._dirtyExceptions.Count != 0 || _state.Bodies != s._highWater || _state.Shapes != s._shapeHighWater || _state.Joints != s._jointHighWater ||
                _state.Exceptions != s._exceptionHighWater || _state.FieldVersion != s._fieldVersion || _state.SleepSettings != s._sleepSettings ||
                _state.ContactSettings != s._contactSettings || _state.Iterations != s._solverIterations || _state.Bias != s._constraintDefaultBias) Reject();
            for (var i = 0; i < _state.Bodies; i++)
            {
                ref readonly var a = ref _slots[i]; ref readonly var b = ref s._slots[i];
                if (a.Generation != b.Generation || a.Alive != b.Alive || a.NextFree != b.NextFree || a.FirstShape != b.FirstShape ||
                    a.FirstJoint != b.FirstJoint || a.FirstException != b.FirstException || a.Mode != b.Mode || a.CCDMode != b.CCDMode ||
                    a.Integration != b.Integration || a.MassProfile != b.MassProfile || a.MassProperties != b.MassProperties || a.Surface.W != b.Surface.W) Reject();
            }
            for (var i = 0; i < _state.Shapes; i++)
            {
                ref readonly var a = ref _shapes[i]; ref readonly var b = ref s._shapeSlots[i];
                if (a.Generation != b.Generation || a.Alive != b.Alive || a.Body != b.Body || a.Revision != b.Revision || a.Pose != b.Pose ||
                    a.Layer != b.Layer || a.Mask != b.Mask || a.Sensor != b.Sensor || a.Friction != b.Friction || a.Bounce != b.Bounce ||
                    a.SolverBias != b.SolverBias || a.OneWay != b.OneWay || !ReferenceEquals(a.Geometry, b.Geometry)) Reject();
                var source = b.Geometry?.Source; var previous = _geometry[i];
                if (!ReferenceEquals(source, previous.Source) || (source?.GeometryRevision ?? 0) != previous.Revision ||
                    (source?.IsDisposed ?? false) != previous.Disposed || source is { IsDisposed: false } && source.CustomSolverBias != previous.Bias) Reject();
            }
            for (var i = 0; i < _state.Joints; i++)
                if (_joints[i].Alive != s._jointSlots[i].Alive || _joints[i].Generation != s._jointSlots[i].Generation || _joints[i].Definition != s._jointSlots[i].Definition) Reject();
            for (var i = 0; i < _state.Exceptions; i++)
            {
                ref readonly var a = ref _exceptions[i]; ref readonly var b = ref s._exceptionSlots[i];
                if (a.Alive != b.Alive || a.Pair.A != b.Pair.A || a.Pair.B != b.Pair.B || a.Pair.GenerationA != b.Pair.GenerationA || a.Pair.GenerationB != b.Pair.GenerationB) Reject();
            }
        }
        private static void Reject() => throw new InvalidOperationException("Checkpoint restore requires the captured body, shape, joint, field and world configuration.");
        private static void Reserve<T>(ref T[] values, int count) { if (values.Length < count) Array.Resize(ref values, Capacity(count)); }
        private void Check()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Checkpoint));
            if (Store._owner != Environment.CurrentManagedThreadId) throw new InvalidOperationException("A GPU checkpoint requires its owner thread.");
        }
        private void Copy(bool restore)
        {
            var s = Store; long total = 0; foreach (var block in _blocks) total += (long)block.Count * block.Stride;
            if (total == 0) return;
            var command = SDL.AcquireGPUCommandBuffer(s.Device);
            if (command == 0) throw GPUPhysicsDevice.Failure("acquire checkpoint copy");
            s._checkpointCopyActive = true;
            try
            {
                var copy = SDL.BeginGPUCopyPass(command);
                if (copy == 0) throw GPUPhysicsDevice.Failure("begin checkpoint copy");
                for (var i = 0; i < _blocks.Length; i++)
                {
                    ref readonly var block = ref _blocks[i]; if (block.Count == 0) continue;
                    var live = s.CheckpointBuffer(i).Buffer!;
                    SDL.CopyGPUBufferToBuffer(copy, new() { Buffer = (restore ? block.Buffer! : live).DangerousGetHandle() },
                        new() { Buffer = (restore ? live : block.Buffer!).DangerousGetHandle() }, checked((uint)(block.Count * block.Stride)), false);
                }
                SDL.EndGPUCopyPass(copy); s._failed = true; BeforeSubmit?.Invoke(); s.Finish(ref command); s.DeviceCopyBytes += total;
                if (!restore) s._failed = false;
            }
            finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); s._checkpointCopyActive = false; }
        }
        public void Dispose()
        {
            if (_disposed) return;
            Check();
            if (Store._checkpointCopyActive) throw new InvalidOperationException("A checkpoint copy cannot be disposed while executing.");
            _disposed = true; _valid = false; BeforeSubmit = null;
            foreach (var block in _blocks) block.Buffer?.Dispose();
            Store._checkpoints.Remove(this); _store = null;
            _slots = []; _shapes = []; _geometry = []; _joints = []; _exceptions = [];
        }
    }

    private (RenderHandle? Buffer, int Count, int Stride) CheckpointBuffer(int index) => index switch
    {
        0 => (_bodies, _highWater, sizeof(Body)),
        1 => (_centers, _highWater, sizeof(Vector2)),
        2 => (_transientForces, _highWater, 16),
        3 => (_targets, _highWater, 16),
        4 => (_resolvedFields, _highWater, 16),
        5 => (_jointStatesGPU, _jointHighWater, 64),
        6 => (_solverHistoryGPU, _previousPointCount, 96),
        7 => (_sleepGraphGPU, _sleepGraphBodies, 16),
        8 => (_sleepEdgesGPU, _sleepEdgeCount, 16),
        9 => (_positionCorrectionsGPU, _highWater, 16),
        10 => (_oneWayHistoryGPU, _oneWayHistoryCount, 48),
        11 => (_oneWayHistoryTableGPU, _oneWayHistoryCount == 0 ? 0 : OneWayHistoryTableSize, 4),
        12 => (_reportRecords, _reportCount, 128),
        13 => (_reportBodyHeads, _publishedReportBodies, 4),
        14 => (_contactsGPU, _contactCapacity, 64),
        15 => (_constraintsGPU, _constraintCapacity, 64),
        16 => (_constraintImpulsesGPU, _constraintImpulseCapacity, 32),
        17 => (_contactHeadsGPU, _contactHeadCapacity, 8),
        18 => (_pairsGPU, _pairCapacity, 16),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
    private void PrepareCheckpointBuffer(int index, int count)
    {
        if (count == 0) return;
        switch (index)
        {
            case <= 4: break; // Body membership is validated before any copy; these buffers cannot have shrunk.
            case 5: Grow(ref _jointStatesGPU, ref _jointStateCapacity, count, 64, false); break;
            case 6: Grow(ref _solverHistoryGPU, ref _solverHistoryCapacity, count, 96, false); break;
            case 7: Grow(ref _sleepGraphGPU, ref _sleepGraphCapacity, count, 16, false); break;
            case 8: Grow(ref _sleepEdgesGPU, ref _sleepEdgeCapacity, count, 16, false); break;
            case 9: Grow(ref _positionCorrectionsGPU, ref _positionCorrectionCapacity, count, 16, false); break;
            case 10: Grow(ref _oneWayHistoryGPU, ref _oneWayHistoryCapacity, count, 48, false); break;
            case 11: Grow(ref _oneWayHistoryTableGPU, ref _oneWayHistoryTableCapacity, count, 4, false); break;
            case 12: Grow(ref _reportRecords, ref _reportRecordCapacity, count, 128, false); break;
            case 13: Grow(ref _reportBodyHeads, ref _reportBodyHeadCapacity, count, 4, false); break;
            case 14: Grow(ref _contactsGPU, ref _contactCapacity, count, 64, false); break;
            case 15: Grow(ref _constraintsGPU, ref _constraintCapacity, count, 64, false); break;
            case 16: Grow(ref _constraintImpulsesGPU, ref _constraintImpulseCapacity, count, 32, false); break;
            case 17: Grow(ref _contactHeadsGPU, ref _contactHeadCapacity, count, 8, false); break;
            case 18: Grow(ref _pairsGPU, ref _pairCapacity, count, 16, false); break;
        }
    }
}
