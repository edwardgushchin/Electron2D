namespace Electron2D;

internal sealed partial class PhysicsReplayEntry
{
    private Transform _portableSolverPose;
    private Vector2 _portableLinear;
    private float _portableAngular, _portableSleepTime;
    private bool _portableSleeping, _portableCanSleep;
    internal uint PortableKind => (uint)((_body is null ? 0 : 1) | (_rigid is null ? 0 : 2) |
        (_surface is null ? 0 : 4) | (_animatable is null ? 0 : 8) | (_character is null ? 0 : 16) |
        (_area is null ? 0 : 32) | (Server is null ? 0 : 64) | (_bodyRuntime is null ? 0 : 128) |
        (_areaRuntime is null ? 0 : 256) | (Server is { IsArea: true } ? 512 : 0));

    internal void CapturePortable(float sleepTime)
    {
        if (_runtime is not null && Backend.Space!.GPUStore is not null) Backend.PublishGPUFields(_runtime);
        Capture();
        if (Scene is null) _pose = Server!.GetTransform();
        _portableSolverPose = Backend.PortablePose;
        var motion = Backend.GetSolverMotion(); _portableLinear = motion.LinearVelocity; _portableAngular = motion.AngularVelocity;
        _portableSleeping = motion.Sleeping; _portableCanSleep = _runtime?.GetCanSleep() ?? false; _portableSleepTime = sleepTime;
    }
    internal void WritePortable(ref PhysicsSnapshotWriter writer, PhysicsSnapshotMap map, ulong id, uint generation)
    {
        writer.U64(id); writer.U32(generation); writer.U32(PortableKind); writer.I32(_shapeCount);
        writer.Pose(_pose); writer.Pose(_portableSolverPose); writer.Vector(_portableLinear); writer.F32(_portableAngular);
        writer.F32(_portableSleepTime); writer.Bool(_portableSleeping); writer.Bool(_portableCanSleep);
        if (_body is { } bodyState)
        {
            writer.Vector(bodyState._lastPosition);
            writer.F32(bodyState._lastRotation);
            writer.Pose(bodyState._validatedTransform);
            writer.F32(bodyState._validatedRotation);
            writer.Pose(bodyState._preparedTransform);
        }
        if (_rigid is { } rigidState)
        {
            writer.Vector(rigidState._linearVelocity);
            writer.F32(rigidState._angularVelocity);
            writer.Bool(rigidState._canSleep);
            writer.Bool(rigidState._sleeping);
            writer.Vector(rigidState._constantForce);
            writer.F32(rigidState._constantTorque);
            writer.Pose(rigidState._frozenSolverPose);
            writer.I32(rigidState._contactCount);
            writer.Bool(rigidState._sleepChangePending);
            WritePairs(ref writer, map, rigidState.Pairs);
        }
        if (_surface is { } surfaceState)
        {
            writer.Vector(surfaceState._constantLinearVelocity);
            writer.F32(surfaceState._constantAngularVelocity);
        }
        if (_animatable is { } animatableState)
        {
            writer.Bool(animatableState._hasTarget);
            writer.Pose(animatableState._lastValidTransform);
            writer.Pose(animatableState._targetTransform);
        }
        if (_character is { } characterState)
        {
            writer.Vector(characterState._velocity);
            writer.Vector(characterState._floorNormal);
            writer.Vector(characterState._wallNormal);
            writer.Vector(characterState._platformVelocity);
            writer.Vector(characterState._lastMotion);
            writer.Vector(characterState._previousPosition);
            writer.Vector(characterState._realVelocity);
            writer.Vector(characterState._resolvedGravity);
            writer.U32(characterState._platformLayer);
            writer.Bool(characterState._onFloor);
            writer.Bool(characterState._onWall);
            writer.Bool(characterState._onCeiling);
            writer.Pose(characterState._solverPose);
            writer.U64(map.NetworkID(characterState._platformRID));
            WriteSlides(ref writer, map, characterState.Slides);
        }
        if (_area is { } areaState)
        {
            writer.Vector(areaState._lastPosition);
            writer.F32(areaState._lastRotation);
            WritePairs(ref writer, map, areaState.Pairs);
        }
        if (_bodyRuntime is { } runtimeState)
        {
            writer.Vector(runtimeState.ConstantForce);
            writer.Vector(runtimeState.PendingForce);
            writer.Vector(runtimeState.Gravity);
            writer.Vector(runtimeState.Surface);
            writer.F32(runtimeState.ConstantTorque);
            writer.F32(runtimeState.PendingTorque);
            writer.F32(runtimeState.LinearDamp);
            writer.F32(runtimeState.AngularDamp);
            writer.F32(runtimeState.SurfaceAngular);
            writer.Bool(runtimeState.CanSleep);
            writer.Bool(runtimeState.FieldsInitialized);
            writer.Bool(runtimeState.Active);
            WriteContacts(ref writer, map, runtimeState.Contacts.AsSpan(0, runtimeState.ContactCount));
        }
        if (Server is not null)
        {
            writer.Pose(_server.Transform);
            writer.Vector(_server.Linear);
            writer.F32(_server.Angular);
            writer.Bool(_server.Sleeping);
            writer.Bool(_server.CanSleep);
            writer.Bool(_server.FirstTarget);
            writer.Bool(_server.HasTarget);
            writer.Pose(_server.Target);
        }
        if (_areaRuntime is not null) WritePairs(ref writer, map, _areaPairs);
    }
    internal void ReadPortable(ref PhysicsSnapshotReader reader, PhysicsSnapshotMap map, ulong id, uint generation)
    {
        PhysicsSnapshotReader.Require(reader.U64() == id && reader.U32() == generation && reader.U32() == PortableKind && reader.I32() == ShapeCount);
        _pose = reader.Pose(); _portableSolverPose = reader.Pose(); _portableLinear = reader.Vector(); _portableAngular = reader.F32();
        _portableSleepTime = reader.F32(); _portableSleeping = reader.Bool(); _portableCanSleep = reader.Bool();
        if (_body is { } bodyState)
        {
            bodyState._lastPosition = reader.Vector();
            bodyState._lastRotation = reader.F32();
            bodyState._validatedTransform = reader.Pose();
            bodyState._validatedRotation = reader.F32();
            bodyState._preparedTransform = reader.Pose();
        }
        if (_rigid is { } rigidState)
        {
            rigidState._linearVelocity = reader.Vector();
            rigidState._angularVelocity = reader.F32();
            rigidState._canSleep = reader.Bool();
            rigidState._sleeping = reader.Bool();
            rigidState._constantForce = reader.Vector();
            rigidState._constantTorque = reader.F32();
            rigidState._frozenSolverPose = reader.Pose();
            rigidState._contactCount = reader.I32();
            PhysicsSnapshotReader.Require(rigidState._contactCount >= 0 && rigidState._contactCount <= rigidState.Settings._maxContactsReported);
            rigidState._sleepChangePending = reader.Bool();
            ReadPairs(ref reader, map, rigidState.Pairs);
        }
        if (_surface is { } surfaceState)
        {
            surfaceState._constantLinearVelocity = reader.Vector();
            surfaceState._constantAngularVelocity = reader.F32();
        }
        if (_animatable is { } animatableState)
        {
            animatableState._hasTarget = reader.Bool();
            animatableState._lastValidTransform = reader.Pose();
            animatableState._targetTransform = reader.Pose();
        }
        if (_character is { } characterState)
        {
            characterState._velocity = reader.Vector();
            characterState._floorNormal = reader.Vector();
            characterState._wallNormal = reader.Vector();
            characterState._platformVelocity = reader.Vector();
            characterState._lastMotion = reader.Vector();
            characterState._previousPosition = reader.Vector();
            characterState._realVelocity = reader.Vector();
            characterState._resolvedGravity = reader.Vector();
            characterState._platformLayer = reader.U32();
            characterState._onFloor = reader.Bool();
            characterState._onWall = reader.Bool();
            characterState._onCeiling = reader.Bool();
            characterState._solverPose = reader.Pose();
            characterState._platformRID = map.LocalRID(reader.U64());
            ReadSlides(ref reader, map, characterState.Slides);
        }
        if (_area is { } areaState)
        {
            areaState._lastPosition = reader.Vector();
            areaState._lastRotation = reader.F32();
            ReadPairs(ref reader, map, areaState.Pairs);
        }
        if (_bodyRuntime is { } runtimeState)
        {
            runtimeState.ConstantForce = reader.Vector();
            runtimeState.PendingForce = reader.Vector();
            runtimeState.Gravity = reader.Vector();
            runtimeState.Surface = reader.Vector();
            runtimeState.ConstantTorque = reader.F32();
            runtimeState.PendingTorque = reader.F32();
            runtimeState.LinearDamp = reader.F32();
            runtimeState.AngularDamp = reader.F32();
            runtimeState.SurfaceAngular = reader.F32();
            runtimeState.CanSleep = reader.Bool();
            runtimeState.FieldsInitialized = reader.Bool();
            runtimeState.Active = reader.Bool();
            ReadContacts(ref reader, map, runtimeState);
        }
        if (Server is not null)
        {
            _server = new(reader.Pose(), reader.Vector(), reader.F32(), reader.Bool(), reader.Bool(), reader.Bool(), reader.Bool(), reader.Pose());
        }
        if (_areaRuntime is not null) ReadPairs(ref reader, map, _areaPairs);
    }
}
