namespace Electron2D;

internal static class PhysicsSnapshotFormat
{
    internal const int HeaderSize = 48;
    internal const uint Magic = 0x31535045, Version = 1;
    internal readonly record struct Metadata(int Objects, int Joints, ulong Tick, float Step, ulong SchemaA, ulong SchemaB, int OneWays);
    internal static Metadata Header(ref PhysicsSnapshotReader reader)
    {
        PhysicsSnapshotReader.Require(reader.U32() == Magic && reader.U32() == Version);
        var objects = reader.I32(); var joints = reader.I32(); var tick = reader.U64(); var step = reader.F32();
        var first = reader.U64(); var second = reader.U64(); var oneWays = reader.I32();
        PhysicsSnapshotReader.Require(objects >= 0 && joints >= 0 && oneWays >= 0 && step >= 0 &&
            (long)objects * 92 + (long)joints * 88 + (long)oneWays * 36 <= reader.Remaining);
        return new(objects, joints, tick, step, first, second, oneWays);
    }
    internal static void Validate(ReadOnlySpan<byte> bytes)
    {
        var reader = new PhysicsSnapshotReader(bytes); var header = Header(ref reader); ulong previous = 0;
        for (var i = 0; i < header.Objects; i++) { var id = reader.U64(); PhysicsSnapshotReader.Require(id > previous && reader.U32() != 0); previous = id; Object(ref reader); }
        previous = 0;
        for (var i = 0; i < header.Joints; i++)
        {
            var id = reader.U64(); PhysicsSnapshotReader.Require(id > previous && reader.U32() != 0); previous = id;
            var kind = reader.U32(); PhysicsSnapshotReader.Require(kind <= 2);
            PhysicsSnapshotReader.Require(reader.U64() != 0); _ = reader.U64();
            JointFrame(reader.Pose()); JointFrame(reader.Pose()); _ = reader.F32(); _ = reader.F32();
        }
        for (var i = 0; i < header.OneWays; i++)
        {
            PhysicsSnapshotReader.Require(reader.U64() != 0 && reader.I32() >= 0 && reader.I32() >= 0);
            PhysicsSnapshotReader.Require(reader.U64() != 0 && reader.I32() >= 0 && reader.I32() >= 0); _ = reader.Bool();
        }
        reader.End();
    }
    private static void Object(ref PhysicsSnapshotReader reader)
    {
        var kind = reader.U32(); PhysicsSnapshotReader.Require(kind is 131 or 133 or 141 or 145 or 288 or 192 or 832);
        PhysicsSnapshotReader.Require(reader.I32() >= 0);
        RigidPose(reader.Pose()); RigidPose(reader.Pose()); _ = reader.Vector(); _ = reader.F32();
        PhysicsSnapshotReader.Require(reader.F32() >= 0); _ = reader.Bool(); _ = reader.Bool();
        if ((kind & 1) != 0)
        {
            _ = reader.Vector();
            _ = reader.F32();
            _ = reader.Pose();
            _ = reader.F32();
            _ = reader.Pose();
        }
        if ((kind & 2) != 0)
        {
            _ = reader.Vector();
            _ = reader.F32();
            _ = reader.Bool();
            _ = reader.Bool();
            _ = reader.Vector();
            _ = reader.F32();
            _ = reader.Pose();
            var contacts = reader.I32(); PhysicsSnapshotReader.Require(contacts is >= 0 and <= PhysicsBodyRuntime.MaxContactLimit);
            _ = reader.Bool();
            Pairs(ref reader);
        }
        if ((kind & 4) != 0)
        {
            _ = reader.Vector();
            _ = reader.F32();
        }
        if ((kind & 8) != 0)
        {
            _ = reader.Bool();
            _ = reader.Pose();
            _ = reader.Pose();
        }
        if ((kind & 16) != 0)
        {
            _ = reader.Vector();
            _ = reader.Vector();
            _ = reader.Vector();
            _ = reader.Vector();
            _ = reader.Vector();
            _ = reader.Vector();
            _ = reader.Vector();
            _ = reader.Vector();
            _ = reader.U32();
            _ = reader.Bool();
            _ = reader.Bool();
            _ = reader.Bool();
            _ = reader.Pose();
            _ = reader.U64();
            Slides(ref reader);
        }
        if ((kind & 32) != 0)
        {
            _ = reader.Vector();
            _ = reader.F32();
            Pairs(ref reader);
        }
        if ((kind & 128) != 0)
        {
            _ = reader.Vector();
            _ = reader.Vector();
            _ = reader.Vector();
            _ = reader.Vector();
            _ = reader.F32();
            _ = reader.F32();
            _ = reader.F32();
            _ = reader.F32();
            _ = reader.F32();
            _ = reader.Bool();
            _ = reader.Bool();
            _ = reader.Bool();
            Contacts(ref reader);
        }
        if ((kind & 64) != 0)
        {
            _ = reader.Pose();
            _ = reader.Vector();
            _ = reader.F32();
            _ = reader.Bool();
            _ = reader.Bool();
            _ = reader.Bool();
            _ = reader.Bool();
            _ = reader.Pose();
        }
        if ((kind & 256) != 0) Pairs(ref reader);
    }
    internal static void JointFrame(Transform pose)
    {
        RigidPose(pose);
        PhysicsSnapshotReader.Require((double)pose.Origin.X * pose.Origin.X + (double)pose.Origin.Y * pose.Origin.Y <=
            (double)PhysicsJointRuntime.MaxExtentSceneUnits * PhysicsJointRuntime.MaxExtentSceneUnits);
    }
    private static void RigidPose(Transform pose) => PhysicsSnapshotReader.Require(pose.Scale.IsEqualApprox(Vector2.One) && Mathf.IsZeroApprox(pose.Skew));
    private static void Pairs(ref PhysicsSnapshotReader reader)
    {
        var count = reader.Count(24);
        for (var i = 0; i < count; i++)
        {
            PhysicsSnapshotReader.Require(reader.U64() != 0); _ = reader.Bool();
            PhysicsSnapshotReader.Require(reader.I32() >= 0 && reader.I32() >= 0); _ = reader.Bool();
        }
    }
    private static void Contacts(ref PhysicsSnapshotReader reader)
    {
        var count = reader.Count(68); PhysicsSnapshotReader.Require(count <= PhysicsBodyRuntime.MaxContactLimit);
        for (var i = 0; i < count; i++)
        {
            PhysicsSnapshotReader.Require(reader.U64() != 0 && reader.I32() >= 0 && reader.I32() >= 0);
            for (var f = 0; f < 13; f++) _ = reader.F32();
        }
    }
    private static void Slides(ref PhysicsSnapshotReader reader)
    {
        var count = reader.Count(80);
        for (var i = 0; i < count; i++)
        {
            PhysicsSnapshotReader.Require(reader.U64() != 0); var other = reader.U64();
            _ = reader.I32(); _ = reader.I32();
            for (var f = 0; f < 11; f++) _ = reader.F32();
            var safe = reader.F32(); var unsafeFraction = reader.F32(); var collided = reader.Bool();
            PhysicsSnapshotReader.Require(safe is >= 0 and <= 1 && unsafeFraction is >= 0 and <= 1 && (!collided || other != 0));
        }
    }
}
