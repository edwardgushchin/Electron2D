namespace Electron2D;

public sealed partial class PhysicsSnapshotMap
{
    private (ulong, ulong) Schema(PhysicsSpace space)
    {
        var schema = new PhysicsSnapshotSchema(); PhysicsReplayEntry.AddFields(ref schema, PhysicsSpace.ReplayFields(space.DefaultAreaFields));
        schema.Add(space.SleepSettings.LinearThreshold); schema.Add(space.SleepSettings.AngularThreshold); schema.Add(space.SleepSettings.TimeToSleep);
        schema.Add(space.ContactSettings.Bias); schema.Add(space.ContactSettings.AllowedPenetration); schema.Add(space.ContactSettings.RecycleRadius); schema.Add(space.ContactSettings.MaxSeparation);
        schema.Add(space.SolverIterations); schema.Add(space.ConstraintDefaultBias); schema.Add(_objects.Count); schema.Add(_joints.Count);
        foreach (var item in _objects) { schema.Add(item.ID); schema.Add(item.Generation); item.Entry!.AddPortableSchema(ref schema, this); }
        foreach (var item in _joints)
        {
            var j = item.Joint!.CaptureReplay(); schema.Add(item.ID); schema.Add(item.Generation); schema.Add((int)j.Type);
            schema.Add(NetworkID(j.BodyA)); schema.Add(NetworkID(j.BodyB)); schema.Add(j.DisableCollision); schema.Add(j.Bias); schema.Add(j.MaxBias); schema.Add(j.MaxForce);
            schema.Add(j.PinSoftness); schema.Add(j.PinLimitEnabled); schema.Add(j.PinLimitLower); schema.Add(j.PinLimitUpper);
            schema.Add(j.PinMotorEnabled); schema.Add(j.PinMotorVelocity); schema.Add(j.PinMotorMaxTorque);
            schema.Add(j.SpringRestLength); schema.Add(j.SpringStiffness); schema.Add(j.SpringDamping); schema.Add(j.SpringAutomaticRest); schema.Add(j.SpringAutomaticLength);
        }
        return schema.Value;
    }
    private void Write(ref PhysicsSnapshotWriter writer, PhysicsSpace space, (ulong First, ulong Second) schema)
    {
        writer.U32(PhysicsSnapshotFormat.Magic); writer.U32(PhysicsSnapshotFormat.Version); writer.I32(_objects.Count); writer.I32(_joints.Count);
        writer.U64(space.Tick); writer.F32(space.LastStep); writer.U64(schema.First); writer.U64(schema.Second); writer.I32(_oneWays.Count);
        foreach (var item in _objects) item.Entry!.WritePortable(ref writer, this, item.ID, item.Generation);
        foreach (var item in _joints)
        {
            var joint = item.Joint!.CaptureReplay(); writer.U64(item.ID); writer.U32(item.Generation); writer.U32((uint)joint.Type);
            writer.U64(NetworkID(joint.BodyA)); writer.U64(NetworkID(joint.BodyB)); writer.Pose(joint.FrameA); writer.Pose(joint.FrameB);
            writer.F32(joint.LowerTranslation); writer.F32(joint.UpperTranslation);
        }
        foreach (var pair in _oneWays) pair.Write(ref writer);
    }
    private void ReadJoints(ref PhysicsSnapshotReader reader)
    {
        foreach (var item in _joints)
        {
            var joint = item.Joint!.CaptureReplay();
            PhysicsSnapshotReader.Require(reader.U64() == item.ID && reader.U32() == item.Generation && reader.U32() == (uint)joint.Type &&
                reader.U64() == NetworkID(joint.BodyA) && reader.U64() == NetworkID(joint.BodyB));
            var first = reader.Pose(); var second = reader.Pose(); var lower = reader.F32(); var upper = reader.F32();
            PhysicsSnapshotFormat.JointFrame(first); PhysicsSnapshotFormat.JointFrame(second);
            PhysicsSnapshotReader.Require(lower == joint.LowerTranslation && upper == joint.UpperTranslation);
            item.FrameA = first; item.FrameB = second;
        }
    }
}

internal readonly record struct PhysicsPortableOneWay(ulong A, int ShapeA, int PieceA, ulong B, int ShapeB, int PieceB, bool Allowed)
{
    internal void Write(ref PhysicsSnapshotWriter writer)
    {
        writer.U64(A); writer.I32(ShapeA); writer.I32(PieceA); writer.U64(B); writer.I32(ShapeB); writer.I32(PieceB); writer.Bool(Allowed);
    }
    internal static PhysicsPortableOneWay Read(ref PhysicsSnapshotReader reader) => new(reader.U64(), reader.I32(), reader.I32(), reader.U64(), reader.I32(), reader.I32(), reader.Bool());
}
