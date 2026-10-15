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
        foreach (var oldVersion in new uint[] { 0x31504E45, 0x32504E45, 0x33504E45 })
        {
            BinaryPrimitives.WriteUInt32LittleEndian(header, oldVersion);
            var oldHeaderRejected = false;
            try { var legacy = new WireReader(header); _ = legacy.Header(); }
            catch (InvalidDataException) { oldHeaderRejected = true; }
            Check(oldHeaderRejected, "Previous recipe/command layout rejects before decoding application state");
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
        CheckSupportControls();
        CheckGeometryControls();
        NetworkSession.CheckInputCommands();
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
    private static void CheckGeometryControls()
    {
        for (var edit = 0; edit < 5; edit++)
        {
            using var world = new NetworkWorld(PhysicsServer.Backend.CPU); world.Reconcile(NetworkWorld.InitialObjects);
            if (edit < 2)
            {
                var floor = world.Find(edit == 0 ? 200u : 201u)!.Body!; floor.CollisionLayer = 0; floor.CollisionMask = 0;
            }
            else if (edit < 4) ((CollisionShape)world.Find(213)!.Body!.GetChild(edit - 2)).Disabled = true;
            else ((RigidBody)world.Find(214)!.Body!).LockRotation = true;
            var rejected = false;
            try
            {
                for (var i = 0; i < 360; i++) world.Step(SimulationPhase.Authority);
                if (world.GeometryCompoundContacts != 3 || world.GeometryCompoundImpulses != 3 || world.GeometryRayAngularSteps == 0)
                    throw new InvalidOperationException("Network geometry negative control lost required slot or angular evidence.");
            }
            catch (InvalidOperationException error) when (error.Message.StartsWith("Network geometry", StringComparison.Ordinal)) { rejected = true; }
            Check(rejected, "Geometry continuation requires its boundary/ledge, both compound slots and unlocked angular response");
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

internal sealed partial class NetworkSession
{
    internal static void CheckInputCommands()
    {
        using var session = new NetworkSession(true, PhysicsServer.Backend.CPU, 0, Guid.NewGuid(), 360, false);
        session._peers[0] = new(2, 0) { Ready = true }; session._peers[1] = new(3, 0) { Ready = true };
        session.SetOwner(10, 2);
        var first = new InputCommand(7, 10, 1, session.Simulation.Find(10)!.Spec.ControlEpoch, new(1, false));
        Feed(2, first); Feed(2, first); Feed(2, first with { Input = new(-1, false) });
        Check(session.AcceptedCommands == 1 && session.RejectedCommands == 1 && Stored() == first,
            "Exact duplicates are idempotent and conflicting same-epoch input cannot replace the first command");
        session.SetOwner(10, 3);
        var next = first with { Epoch = session.Simulation.Find(10)!.Spec.ControlEpoch, Input = new(-.5f, false) };
        Feed(2, first); Feed(3, first); Feed(3, next); Feed(3, next);
        Check(session.AcceptedCommands == 2 && session.RetiredCommandReplacements == 1 && Stored() == next,
            "A valid new controller replaces retired future input after old-controller and old-epoch packets reject");
        Feed(3, next with { Input = new(.8f, false) });
        Check(Stored() == next && session.RejectedCommands == 4, "New-epoch conflicting input also remains immutable");
        var count = session.Simulation.Describe(session._manifest);
        for (var i = 0; i < count; i++)
            if (session._manifest[i].ID == 10) session._manifest[i] = session._manifest[i] with { Generation = 2 };
        session.Simulation.Reconcile(session._manifest.AsSpan(0, count));
        Feed(3, next); var incarnation = next with { Generation = 2 }; Feed(3, incarnation);
        Check(session.AcceptedCommands == 3 && session.RejectedCommands == 5 && session.RetiredCommandReplacements == 2 && Stored() == incarnation,
            "A new object incarnation replaces retired future input while stale generations reject");

        InputCommand Stored() => session._commands[CommandSlot(7, 10)];
        void Feed(int sender, InputCommand command)
        {
            var writer = new WireWriter(session._send); writer.U64(0); writer.U64(0); writer.I32(1); writer.Input(command);
            var reader = new WireReader(session._send.AsSpan(0, writer.Count)); session.ReceiveInputs(sender, ref reader);
        }
        static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
