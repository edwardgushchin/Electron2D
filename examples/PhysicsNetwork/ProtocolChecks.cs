using System.Buffers.Binary;
using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal static class ProtocolChecks
{
    internal static void Run()
    {
        _ = ProjectSettings.Get(ProjectSettings.MSAA2D);
        _ = ProjectSettings.Get(ProjectSettings.LCDSubpixelLayout);
        using (var config = new ConfigFile())
        {
            var msaa = new ConfigKey<Viewport.MSAA>("project", "msaa");
            var lcd = new ConfigKey<FontLCDSubpixelLayout>("project", "lcd");
            config.SetValue(msaa, Viewport.MSAA.Samples4X); config.SetValue(lcd, FontLCDSubpixelLayout.VerticalBGR);
            config.Parse(config.EncodeToText());
            Check(config.GetValue(msaa) == Viewport.MSAA.Samples4X && config.GetValue(lcd) == FontLCDSubpixelLayout.VerticalBGR,
                "Built-in project enum metadata round trips without requiring JSON reflection");
        }
        var encoded = new byte[32]; var writer = new WireWriter(encoded);
        var expected = new InputCommand(17, 10, 2, 3, new(.5f, true)); writer.Input(expected);
        var reader = new WireReader(encoded); Check(reader.Input() == expected, "Input wire round trip"); reader.End();
        for (var i = 0; i < encoded.Length; i++)
        {
            var rejected = false;
            try { var truncated = new WireReader(encoded.AsSpan(0, i)); _ = truncated.Input(); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Every truncated input rejects");
        }
        foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, 1.1f, -1.1f })
        {
            BinaryPrimitives.WriteSingleLittleEndian(encoded.AsSpan(24), invalid); RejectInput(encoded);
        }
        BinaryPrimitives.WriteSingleLittleEndian(encoded.AsSpan(24), .5f);
        BinaryPrimitives.WriteUInt32LittleEndian(encoded.AsSpan(28), 2); RejectInput(encoded);
        BinaryPrimitives.WriteUInt32LittleEndian(encoded.AsSpan(28), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(encoded.AsSpan(16), 0); RejectInput(encoded);
        BinaryPrimitives.WriteUInt32LittleEndian(encoded.AsSpan(16), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(encoded.AsSpan(20), 0); RejectInput(encoded);
        CheckJointContinuations();
    }
    private static void CheckJointContinuations()
    {
        foreach (var jointID in new ulong[] { 100, 101, 102 })
        {
            using var world = new NetworkWorld(PhysicsServer.Backend.CPU);
            world.Reconcile([
                new(20, 1, ObjectKind.Ball, 0, 1, new(340, 160)),
                new(21, 1, ObjectKind.Ball, 0, 1, new(375, 180)),
                new(100, 1, ObjectKind.Pin, 0, 1, new(360, 170), 20, 21),
                new(40, 1, ObjectKind.Anchor, 0, 1, new(700, 140)),
                new(41, 1, ObjectKind.Ball, 0, 1, new(700, 180)),
                new(101, 1, ObjectKind.Groove, 0, 1, new(700, 140), 40, 41),
                new(50, 1, ObjectKind.Anchor, 0, 1, new(850, 140)),
                new(51, 1, ObjectKind.Ball, 0, 1, new(850, 190)),
                new(102, 1, ObjectKind.Spring, 0, 1, new(850, 140), 50, 51)
            ]);
            world.Step(SimulationPhase.Authority);
            PhysicsServer.JointSetMaxForce(world.Find(jointID)!.RID, 0);
            if (jointID != 102) ((RigidBody)world.Find(jointID == 100 ? 20u : 41u)!.Body!).ApplyCentralImpulse(new(500, 0));
            var rejected = false;
            try { for (var i = 0; i < 50; i++) world.Step(SimulationPhase.Prediction); }
            catch (InvalidOperationException error) when (error.Message.StartsWith("Network joint continuation failed")) { rejected = true; }
            Check(rejected, "A disabled constraint fails the real trajectory check for each joint family");
        }
    }
    private static void RejectInput(byte[] bytes)
    {
        try { var reader = new WireReader(bytes); _ = reader.Input(); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("Malformed game input was accepted.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
