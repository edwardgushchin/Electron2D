namespace Electron2D;

internal sealed partial class PhysicsReplayEntry
{
    private static void WritePairs(ref PhysicsSnapshotWriter writer, PhysicsSnapshotMap map, PhysicsShapePairTracker pairs)
    {
        writer.I32(pairs.Current.Count);
        foreach (var pair in pairs.Current)
        {
            writer.U64(map.NetworkID(pair.RID)); writer.Bool(pair.IsArea);
            writer.I32(pair.OtherShape); writer.I32(pair.LocalShape); writer.Bool(pair.InTree);
        }
    }
    private void ReadPairs(ref PhysicsSnapshotReader reader, PhysicsSnapshotMap map, PhysicsShapePairTracker pairs)
    {
        var count = reader.Count(24); pairs.Clear(); pairs.Prepare(count);
        for (var i = 0; i < count; i++)
        {
            var other = map.Collider(reader.U64()); var area = reader.Bool(); var remote = reader.I32(); var local = reader.I32(); var inTree = reader.Bool();
            PhysicsSnapshotReader.Require(other != this && other.IsArea == area);
            PhysicsSnapshotReader.Require(local >= 0 && remote >= 0);
            var pair = new PhysicsShapePair(other.Backend.RID, other.Backend.ObjectIdentity, area, remote, local) { InTree = inTree };
            PhysicsSnapshotReader.Require(pairs.Current.Add(pair));
        }
    }
    private static void WriteContacts(ref PhysicsSnapshotWriter writer, PhysicsSnapshotMap map, ReadOnlySpan<PhysicsDirectBodyState.Contact> contacts)
    {
        writer.I32(contacts.Length);
        foreach (ref readonly var contact in contacts)
        {
            writer.U64(map.NetworkID(contact.Collider)); writer.I32(contact.LocalShape); writer.I32(contact.ColliderShape);
            writer.Vector(contact.LocalPoint); writer.Vector(contact.ColliderPoint); writer.Vector(contact.Normal);
            writer.Vector(contact.LocalVelocity); writer.Vector(contact.ColliderVelocity); writer.Vector(contact.Impulse); writer.F32(contact.Depth);
        }
    }
    private void ReadContacts(ref PhysicsSnapshotReader reader, PhysicsSnapshotMap map, PhysicsBodyRuntime.ReplayState state)
    {
        var count = reader.Count(68); PhysicsSnapshotReader.Require(count <= _runtime!.ContactLimit);
        if (state.Contacts.Length < count) Array.Resize(ref state.Contacts, count);
        state.ContactCount = count;
        for (var i = 0; i < count; i++)
        {
            var other = map.Collider(reader.U64()); var local = reader.I32(); var remote = reader.I32();
            var point = reader.Vector(); var colliderPoint = reader.Vector(); var normal = reader.Vector();
            var velocity = reader.Vector(); var colliderVelocity = reader.Vector(); var impulse = reader.Vector(); var depth = reader.F32();
            PhysicsSnapshotReader.Require(other != this && !other.IsArea);
            PhysicsSnapshotReader.Require(local >= 0 && remote >= 0);
            var identity = other.Backend.ObjectIdentity;
            state.Contacts[i] = new(other.Backend.RID, identity.ID, local, remote, point, colliderPoint, normal,
                velocity, colliderVelocity, impulse, depth, other.Backend.SceneOwnerReference, identity);
        }
    }
    private static void WriteSlides(ref PhysicsSnapshotWriter writer, PhysicsSnapshotMap map, List<MotionResultData> slides)
    {
        writer.I32(slides.Count);
        foreach (var slide in slides)
        {
            writer.U64(map.NetworkID(slide.OwnerRID)); writer.U64(map.NetworkID(slide.ColliderRID));
            writer.I32(slide.LocalShape); writer.I32(slide.ColliderShape);
            writer.Vector(slide.Point); writer.Vector(slide.Normal); writer.F32(slide.Depth); writer.Vector(slide.ColliderVelocity);
            writer.Vector(slide.Travel); writer.Vector(slide.Remainder); writer.F32(slide.SafeFraction); writer.F32(slide.UnsafeFraction); writer.Bool(slide.Collided);
        }
    }
    private void ReadSlides(ref PhysicsSnapshotReader reader, PhysicsSnapshotMap map, List<MotionResultData> slides)
    {
        var count = reader.Count(80); slides.Clear(); slides.EnsureCapacity(count);
        for (var i = 0; i < count; i++)
        {
            var owner = map.LocalRID(reader.U64()); var colliderID = reader.U64(); var other = colliderID == 0 ? null : map.Collider(colliderID);
            var local = reader.I32(); var remote = reader.I32(); var point = reader.Vector(); var normal = reader.Vector(); var depth = reader.F32();
            var velocity = reader.Vector(); var travel = reader.Vector(); var remainder = reader.Vector();
            var safe = reader.F32(); var unsafeFraction = reader.F32(); var collided = reader.Bool();
            PhysicsSnapshotReader.Require(owner == Backend.RID && (!collided || other is { IsArea: false } && other != this));
            if (collided) PhysicsSnapshotReader.Require(local >= 0 && remote >= 0);
            var identity = other?.Backend.ObjectIdentity ?? default;
            slides.Add(new(owner, other?.Backend.RID ?? default, identity.ID, local, remote, point, normal, depth, velocity,
                travel, remainder, safe, unsafeFraction, collided, identity));
        }
    }
    internal bool IsArea => _areaRuntime is not null;
    internal void ApplyPortableMotion()
    {
        var surface = _surface?._constantLinearVelocity ?? _bodyRuntime?.Surface ?? default;
        var surfaceAngular = _surface?._constantAngularVelocity ?? _bodyRuntime?.SurfaceAngular ?? 0;
        if (Server is { IsArea: false } && _configuration.Mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic)
        { surface = _server.Linear; surfaceAngular = _server.Angular; }
        Backend.ApplyPortableMotion(_portableSolverPose, _portableLinear, _portableAngular, _portableSleepTime, _portableCanSleep, _portableSleeping,
            surface, surfaceAngular, _rigid?._constantForce ?? _bodyRuntime?.ConstantForce ?? default, _rigid?._constantTorque ?? _bodyRuntime?.ConstantTorque ?? 0,
            _bodyRuntime?.Gravity ?? default, _bodyRuntime?.LinearDamp ?? 0, _bodyRuntime?.AngularDamp ?? 0, _bodyRuntime?.FieldsInitialized ?? false);
        // Keep destination-owned cache identities while replacing the physical/backing values.
        _backend = Backend.CaptureReplay();
        if (_bodyRuntime is { ContactCount: > 0 }) _runtime!.GetView(Backend.Space!);
        Restore();
    }
    internal void ApplyPortableSleep() => Backend.ApplyPortableSleep(_portableSleepTime, _portableCanSleep, _portableSleeping);
}
