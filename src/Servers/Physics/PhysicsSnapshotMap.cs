namespace Electron2D;

/// <summary>Binds portable network identities to one live world's collision objects and joints.</summary>
/// <remarks>The caller assigns shared nonzero IDs and incarnation generations. Bind every live collision
/// object and joint before capture/apply. Worlds must have compatible authored configuration and scene/server
/// roles; local creation order and physics backend can differ. This class borrows objects and owns reusable
/// transfer scratch. It does not authenticate peers, choose stale-packet policy or replicate lifecycle.</remarks>
public sealed partial class PhysicsSnapshotMap : IDisposable
{
    internal sealed class Binding(ulong id, uint generation, RID rid, PhysicsReplayEntry? entry, PhysicsJointRuntime? joint)
    {
        internal readonly ulong ID = id;
        internal readonly uint Generation = generation;
        internal readonly RID RID = rid;
        internal readonly PhysicsReplayEntry? Entry = entry;
        internal readonly PhysicsJointRuntime? Joint = joint;
        internal Transform FrameA, FrameB;
        internal readonly long Attachment = entry?.Backend.AttachmentVersion ?? 0;
    }
    private PhysicsSpace? _space;
    private readonly Dictionary<ulong, Binding> _identities = [];
    private readonly Dictionary<RID, ulong> _local = [];
    private readonly List<Binding> _objects = [], _joints = [];
    private readonly List<PhysicsReplayEntry> _poses = [];
    private readonly List<PhysicsPortableOneWay> _oneWays = [];
    private GPUPhysicsBodyStore.BodyHandle[] _gpuHandles = [];
    private GPUPhysicsBodyStore.Snapshot[] _gpuStates = [];
    private bool _busy;
    private readonly int _owner = Environment.CurrentManagedThreadId;

    /// <summary>Creates a portable identity map borrowing a live scene-owned or server-owned space.</summary>
    /// <param name="space">A process-local space identity, never written to a packet.</param>
    /// <exception cref="ArgumentException">The identity is not a live space.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or the world is failed/busy.</exception>
    public PhysicsSnapshotMap(RID space)
    {
        var source = PhysicsServer.Service.GetSceneSpace(space); source.EnsureCheckpointAccess(); _space = source; source.RegisterSnapshotMap(this);
    }
    /// <summary>Gets whether this map or its source world released its bindings.</summary>
    public bool IsDisposed => _space is null;
    /// <summary>Associates one network incarnation with an attached collision object or joint.</summary>
    /// <param name="networkID">Shared nonzero logical identity, independent of local RIDs.</param>
    /// <param name="generation">Shared nonzero incarnation; change it or the ID when an authority reuses an identity.</param>
    /// <param name="objectRID">A live collision-object or joint identity belonging to this map's space.</param>
    /// <exception cref="ArgumentException">An identity is zero/duplicate or the object does not belong to the space.</exception>
    /// <exception cref="ObjectDisposedException">The map/world is disposed.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or an operation is active.</exception>
    public void Bind(ulong networkID, uint generation, RID objectRID)
    {
        var space = Check();
        if (networkID == 0 || generation == 0 || _identities.ContainsKey(networkID) || _local.ContainsKey(objectRID)) throw new ArgumentException("Snapshot bindings require unique nonzero IDs, generations and local objects.");
        var entry = space.FindSnapshotObject(objectRID); var joint = entry is null ? space.FindSnapshotJoint(objectRID) : null;
        if (entry is null && joint is null) throw new ArgumentException("The object does not belong to this physics space.", nameof(objectRID));
        var binding = new Binding(networkID, generation, objectRID, entry, joint);
        _identities.Add(networkID, binding); _local.Add(objectRID, networkID);
        var list = entry is null ? _joints : _objects; list.Add(binding); list.Sort(static (a, b) => a.ID.CompareTo(b.ID));
    }
    /// <summary>Removes a binding without removing its object from the world.</summary>
    /// <param name="networkID">The logical identity to remove.</param>
    /// <returns>True if a binding existed.</returns>
    /// <exception cref="ObjectDisposedException">The map/world is disposed.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or an operation is active.</exception>
    public bool Unbind(ulong networkID)
    {
        Check(); if (!_identities.Remove(networkID, out var binding)) return false;
        _local.Remove(binding.RID); (binding.Entry is null ? _joints : _objects).Remove(binding); return true;
    }
    /// <summary>Captures compatible local physical state and its tick into reusable portable storage.</summary>
    /// <param name="snapshot">Caller-owned destination storage.</param>
    /// <exception cref="ArgumentNullException">The destination is null.</exception>
    /// <exception cref="InvalidOperationException">The world/bindings are incomplete or busy, access is off-owner, or the budget is insufficient.</exception>
    /// <exception cref="ObjectDisposedException">The map/world is disposed.</exception>
    public void Capture(PhysicsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot); var space = Check(); _busy = true;
        try
        {
            Prepare(space, capture: true); space.CapturePortableOneWays(this, _oneWays);
            var schema = Schema(space);
            var measure = new PhysicsSnapshotWriter(default); Write(ref measure, space, schema);
            var writer = new PhysicsSnapshotWriter(snapshot.BeginCapture(measure.Position)); Write(ref writer, space, schema); snapshot.CompleteCapture(writer.Position);
        }
        finally { _busy = false; }
    }
    /// <summary>Validates then applies a compatible authoritative snapshot without gameplay callbacks.</summary>
    /// <param name="snapshot">A complete decoded or locally captured portable snapshot.</param>
    /// <remarks>Network sender authorization and tick ordering are the caller's responsibility. Incoming values are validated before being applied. An execution/device failure after mutation starts fails the world until disposal.
    /// Private solver caches are reconstructed; cross-backend bitwise continuation is not promised.</remarks>
    /// <exception cref="ArgumentNullException">The source is null.</exception>
    /// <exception cref="InvalidDataException">Wire data, bindings, generations or authoring are incompatible.</exception>
    /// <exception cref="InvalidOperationException">The world is failed/busy, access is off-owner or execution fails.</exception>
    /// <exception cref="ObjectDisposedException">The map/world is disposed.</exception>
    public void Apply(PhysicsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot); var space = Check(); var data = snapshot.Data; _busy = true;
        try
        {
            Prepare(space, capture: false); var reader = new PhysicsSnapshotReader(data); var header = PhysicsSnapshotFormat.Header(ref reader); var schema = Schema(space);
            PhysicsSnapshotReader.Require(header.Objects == _objects.Count && header.Joints == _joints.Count && schema == (header.SchemaA, header.SchemaB));
            foreach (var item in _objects) item.Entry!.ReadPortable(ref reader, this, item.ID, item.Generation);
            ReadJoints(ref reader);
            _oneWays.Clear(); _oneWays.EnsureCapacity(header.OneWays);
            for (var i = 0; i < header.OneWays; i++) _oneWays.Add(PhysicsPortableOneWay.Read(ref reader));
            reader.End(); space.ValidatePortableOneWays(this, _oneWays);
            foreach (var item in _objects) item.Entry!.Validate(space);
            try
            {
                space.BeginPortableRestore();
                foreach (var item in _joints) item.Joint!.ApplyPortableFrames(item.FrameA, item.FrameB);
                foreach (var item in _objects) item.Entry!.ApplyPortableMotion();
                foreach (var entry in _poses) entry.RestorePose();
                foreach (var item in _objects) item.Entry!.ApplyPortableSleep();
                space.CompletePortableRestore(this, _oneWays, header.Tick, header.Step);
            }
            catch (Exception error) { space.FailPortableRestore(error); throw; }
        }
        finally { _busy = false; }
    }
    private PhysicsSpace Check()
    {
        if (_space is not { } space) throw new ObjectDisposedException(nameof(PhysicsSnapshotMap));
        if (_owner != Environment.CurrentManagedThreadId || _busy) throw new InvalidOperationException("Snapshot maps require an idle owner thread.");
        space.EnsureCheckpointAccess(); return space;
    }
    private void Prepare(PhysicsSpace space, bool capture)
    {
        space.PreparePortableCapture();
        PhysicsReplayCopy.Require(space.Bodies.Count + space.Areas.Count + space.ServerColliders.Count == _objects.Count && space.SnapshotJoints.Count == _joints.Count);
        if (capture && space.GPUStore is not null && _gpuHandles.Length < _objects.Count) { Array.Resize(ref _gpuHandles, _objects.Count); Array.Resize(ref _gpuStates, _objects.Count); }
        for (var i = 0; i < _objects.Count; i++)
        {
            var entry = _objects[i].Entry!;
            PhysicsReplayCopy.Require(entry.Backend.Space == space && entry.Backend.AttachmentVersion == _objects[i].Attachment);
            if (capture && space.GPUStore is not null) _gpuHandles[i] = entry.Backend.GPUHandle;
        }
        if (capture && space.GPUStore is { } gpu)
        {
            gpu.Read(_gpuHandles.AsSpan(0, _objects.Count), _gpuStates);
            for (var i = 0; i < _objects.Count; i++) _objects[i].Entry!.Backend.AcceptGPUState(_gpuStates[i]);
        }
        if (!capture) space.GPUStore?.Step(0, default);
        _poses.Clear(); _poses.EnsureCapacity(_objects.Count);
        for (var i = 0; i < _objects.Count; i++)
        {
            var entry = _objects[i].Entry!;
            if (capture) entry.CapturePortable(space.GPUStore is null ? entry.Backend.PortableSleepTime : _gpuStates[i].SleepTime);
            else entry.Capture();
            _poses.Add(entry);
        }
        _poses.Sort(static (a, b) => a.Depth.CompareTo(b.Depth));
        foreach (var item in _joints) PhysicsReplayCopy.Require(item.Joint!.Space == space);
    }
    internal ulong NetworkID(RID rid)
    {
        if (!rid.IsValid()) return 0;
        return _local.TryGetValue(rid, out var id) ? id : throw new InvalidOperationException("Every referenced physics object must have a network binding.");
    }
    internal RID LocalRID(ulong id) => id == 0 ? default : Collider(id).Backend.RID;
    internal PhysicsReplayEntry Collider(ulong id) => _identities.TryGetValue(id, out var value) && value.Entry is { } entry ? entry : throw new InvalidDataException("Snapshot references an unbound collision object.");
    /// <summary>Releases reusable scratch and bindings without disposing the borrowed world or its objects.</summary>
    /// <exception cref="InvalidOperationException">First disposal is off-owner or an operation is active.</exception>
    public void Dispose()
    {
        if (_space is not { } space) return;
        if (_owner != Environment.CurrentManagedThreadId || _busy) throw new InvalidOperationException("Snapshot maps require an idle owner thread.");
        _identities.Clear(); _local.Clear(); _objects.Clear(); _joints.Clear(); _poses.Clear(); _oneWays.Clear(); _gpuHandles = []; _gpuStates = [];
        _space = null; space.UnregisterSnapshotMap(this);
    }
}
