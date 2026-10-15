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
        var header = new byte[8]; var headerWriter = new WireWriter(header); headerWriter.Header(3);
        var headerReader = new WireReader(header); Check(headerReader.Header() == 3, "Current application wire header round trip"); headerReader.End();
        BinaryPrimitives.WriteUInt32LittleEndian(header, 0x31504E45);
        var oldHeaderRejected = false;
        try { var legacy = new WireReader(header); _ = legacy.Header(); }
        catch (InvalidDataException) { oldHeaderRejected = true; }
        Check(oldHeaderRejected, "Previous recipe/command layout rejects before decoding application state");
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
        CheckSupportControls();
    }
    private static void CheckJointContinuations()
    {
        foreach (var jointID in new ulong[] { 100, 101, 102 })
        {
            using var world = new NetworkWorld(PhysicsServer.Backend.CPU);
            world.Reconcile(NetworkWorld.InitialObjects);
            world.Step(SimulationPhase.Authority);
            PhysicsServer.JointSetMaxForce(world.Find(jointID)!.RID, 0);
            if (jointID != 102) ((RigidBody)world.Find(jointID == 100 ? 20u : 41u)!.Body!).ApplyCentralImpulse(new(500, 0));
            var rejected = false;
            try { for (var i = 0; i < 50; i++) world.Step(SimulationPhase.Prediction); }
            catch (InvalidOperationException error) when (error.Message.StartsWith("Network joint continuation failed")) { rejected = true; }
            Check(rejected, "A disabled constraint fails the real trajectory check for each joint family");
        }
    }
    private static void CheckSupportControls()
    {
        for (var edit = 0; edit < 2; edit++)
        {
            using var world = new NetworkWorld(PhysicsServer.Backend.CPU);
            world.Reconcile(NetworkWorld.InitialObjects);
            if (edit == 0) { world.Find(73)!.Body!.CollisionLayer = 0; world.Find(73)!.Body!.CollisionMask = 0; }
            else ((DampedSpringJoint)world.Find(112)!.Joint!).Stiffness = 0;
            var rejected = false;
            try
            {
                for (var i = 0; i < 300; i++) world.Step(SimulationPhase.Authority);
                if (world.SupportContacts == 0 || world.SupportSleeping == 0)
                    throw new InvalidOperationException("Network coupled negative control has no supported sleeping state.");
            }
            catch (InvalidOperationException error) when (error.Message.StartsWith("Network coupled", StringComparison.Ordinal)) { rejected = true; }
            Check(rejected, "A coupled network world requires both its floor and actual spring load");
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
