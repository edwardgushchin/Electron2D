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
        foreach (var oldVersion in new uint[] { 0x31504E45, 0x32504E45, 0x33504E45, 0x34504E45, 0x35504E45 })
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
        CheckInitialPreparation();
        CheckJointContinuations();
        CheckLatticeControls();
        CheckSupportControls();
        CheckGeometryControls();
        CheckCrossedControls();
        CheckWakeCorrections();
        CheckPacketOrder();
        NetworkSession.CheckInputCommands();
        NetworkSession.CheckHandoffInputs();
        NetworkSession.CheckPresentationOffsetTiming();
    }
    private static void CheckInitialPreparation()
    {
        var separate = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DRunOnSeparateThread);
        try
        {
            foreach (var threaded in new[] { false, true })
            {
                ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
                using var prepared = new NetworkWorld(PhysicsServer.Backend.CPU);
                using var control = new NetworkWorld(PhysicsServer.Backend.CPU);
                var preparedEvents = new List<GameEvent>(); var controlEvents = new List<GameEvent>();
                prepared.Observed = preparedEvents.Add; control.Observed = controlEvents.Add;
                prepared.PrepareInitialState(); control.Reconcile(NetworkWorld.InitialObjects);
                Check(prepared.Tick == 0 && prepared.EventSequence == 0 && preparedEvents.Count == 0 &&
                    prepared.PreparationFrames == 1 && prepared.JointSteps == 0 && prepared.SupportSteps == 0 && prepared.GeometrySteps == 0 && prepared.LatticeSteps == 0 &&
                    prepared.CrossedContacts == 0 && prepared.Removed == 0 && prepared.OwnershipChanges == 0,
                    "Preparation restores physical time without emitting effects or claiming gameplay steps");
                var first = new PhysicsSnapshot(24000); var second = new PhysicsSnapshot(24000);
                var a = new byte[24000]; var b = new byte[24000];
                for (var tick = 0; tick <= 16; tick++)
                {
                    if (tick > 0) { prepared.Step(SimulationPhase.Authority); control.Step(SimulationPhase.Authority); }
                    prepared.Map.Capture(first); control.Map.Capture(second);
                    var length = first.WriteTo(a); var otherLength = second.WriteTo(b);
                    Check(length == otherLength && a.AsSpan(0, length).SequenceEqual(b.AsSpan(0, otherLength)),
                        "Prepared CPU continuation preserves contacts, sleep, forces and joints against an untouched control");
                }
                Check(preparedEvents.SequenceEqual(controlEvents), "Preparation preserves subsequent exactly-once gameplay events");
                var rejected = false;
                try { prepared.PrepareInitialState(); } catch (InvalidOperationException) { rejected = true; }
                Check(rejected && prepared.Tick == 16, "Preparation rejects a live world without rewinding it");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate); }
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
    private static void CheckLatticeControls()
    {
        for (var edit = 0; edit < 2; edit++)
        {
            using var world = new NetworkWorld(PhysicsServer.Backend.CPU); world.Reconcile(NetworkWorld.InitialObjects);
            if (edit == 0) ((RigidBody)world.Find(304)!.Body!).CollisionMask = 0;
            else for (ulong id = 330; id <= 333; id++) PhysicsServer.JointSetMaxForce(world.Find(id)!.RID, 0);
            var rejected = false;
            try { for (var i = 0; i < 32; i++) world.Step(SimulationPhase.Authority); }
            catch (InvalidOperationException error) when (error.Message.StartsWith("Network lattice", StringComparison.Ordinal)) { rejected = true; }
            Check(rejected, "The lattice requires the actual witness contact and closed constraints");
        }
        using var source = new NetworkWorld(PhysicsServer.Backend.CPU); source.Reconcile(NetworkWorld.InitialObjects);
        using var target = new NetworkWorld(PhysicsServer.Backend.CPU); target.Reconcile(NetworkWorld.InitialObjects);
        source.Step(SimulationPhase.Authority); var snapshot = new PhysicsSnapshot(24000); source.Map.Capture(snapshot);
        target.CheckLatticeCorrection(); Check(target.LatticeContactCorrections == 0, "An omitted lattice snapshot supplies no imported contact evidence");
        target.Map.Apply(snapshot); target.CheckLatticeCorrection(true);
        Check(target.LatticeContactCorrections == 1 && target.EventSequence == 0, "The closed-loop impact snapshot imports complete contacts silently into distinct local objects");
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
    private static void CheckCrossedControls()
    {
        for (var edit = 0; edit < 3; edit++)
        {
            using var world = new NetworkWorld(PhysicsServer.Backend.CPU); world.Reconcile(NetworkWorld.InitialObjects);
            var body = (RigidBody)world.Find(edit == 0 ? 220u : 221u)!.Body!;
            if (edit < 2) ((CollisionShape)body.GetChild(1)).Disabled = true;
            else body.LockRotation = true;
            var rejected = false;
            try { world.Step(SimulationPhase.Authority); }
            catch (InvalidOperationException error) when (error.Message.StartsWith("Network crossed capsules", StringComparison.Ordinal)) { rejected = true; }
            Check(rejected, "Crossed continuation requires both enabled capsules and actual free angular response");
        }
        using var source = new NetworkWorld(PhysicsServer.Backend.CPU); source.Reconcile(NetworkWorld.InitialObjects);
        using var target = new NetworkWorld(PhysicsServer.Backend.CPU); target.Reconcile(NetworkWorld.InitialObjects);
        source.Step(SimulationPhase.Authority); var snapshot = new PhysicsSnapshot(24000); source.Map.Capture(snapshot);
        target.CheckCrossedCorrection(); Check(target.CrossedContactCorrections == 0, "An omitted crossed snapshot supplies no imported contact evidence");
        target.Map.Apply(snapshot); target.CheckCrossedCorrection();
        Check(target.CrossedContactCorrections == 1 && target.EventSequence == 0,
            "A real crossed contact snapshot imports complete paired response silently into distinct local objects");
    }
    private static void CheckWakeCorrections()
    {
        using var source = new NetworkWorld(PhysicsServer.Backend.CPU); source.Reconcile(NetworkWorld.InitialObjects);
        using var target = new NetworkWorld(PhysicsServer.Backend.CPU); target.Reconcile(NetworkWorld.InitialObjects);
        var snapshot = new PhysicsSnapshot(24000);
        for (var i = 0; i < 200; i++) source.Step(SimulationPhase.Authority);
        source.Map.Capture(snapshot); target.Map.Apply(snapshot);
        target.CheckSupportCorrection(); target.CheckGeometryCorrection();
        Check(target.SupportSleepingCorrections > 0 && target.GeometrySleepingCorrections == NetworkWorld.GeometryFamilyMask &&
            target.SupportWakeCorrections == 0 && target.GeometryWakeCorrections == 0, "Sleeping correction alone cannot count as a restored wake");
        source.QueueFinalImpulses(); source.Step(SimulationPhase.Authority); source.Map.Capture(snapshot);
        target.CheckSupportCorrection(); target.CheckGeometryCorrection();
        Check(target.SupportWakeCorrections == 0 && target.GeometryWakeCorrections == 0, "Omitting the awake snapshot leaves the client asleep");
        target.Map.Apply(snapshot); target.CheckSupportCorrection(); target.CheckGeometryCorrection();
        Check(target.SupportWakeCorrections == 1 && target.GeometryWakeCorrections == NetworkWorld.GeometryFamilyMask,
            "Applying actual awake state restores the coupled island and all geometry wake observations");
    }

    private static void CheckPacketOrder()
    {
        using var peer = new ENetMultiplayerPeer(); peer.SetBindIP("127.0.0.1"); peer.CreateServer(0, 4, 1);
        using var multiplayer = new SceneMultiplayer { MultiplayerPeer = peer };
        var port = peer.Host!.GetLocalPort();
        using var first = new ENetMultiplayerPeer(); first.CreateClient("127.0.0.1", port, 1);
        using var firstAPI = new SceneMultiplayer { MultiplayerPeer = first };
        using var second = new ENetMultiplayerPeer(); second.CreateClient("127.0.0.1", port, 1);
        using var secondAPI = new SceneMultiplayer { MultiplayerPeer = second };
        using var third = new ENetMultiplayerPeer(); third.CreateClient("127.0.0.1", port, 1);
        using var thirdAPI = new SceneMultiplayer { MultiplayerPeer = third };
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        while (multiplayer.GetPeers().Length != 3)
        {
            multiplayer.Poll(); firstAPI.Poll(); secondAPI.Poll(); thirdAPI.Poll();
            if (deadline.Elapsed.TotalSeconds > 5) throw new TimeoutException("Packet-order control peers did not connect.");
            Thread.Sleep(1);
        }
        var a = firstAPI.GetUniqueID(); var b = secondAPI.GetUniqueID(); var c = thirdAPI.GetUniqueID();
        var separate = new ImpairedLink(multiplayer, true);
        separate.Send(a, [1], 0); separate.Send(b, [1], 0); separate.Send(c, [1], 0); separate.Flush(1);
        Check(separate.Sent == 3 && separate.Reordered == 0, "Different recipients cannot count as packet reordering");
        var shared = new ImpairedLink(multiplayer, true);
        shared.Send(a, [1], 0); shared.Send(a, [1], 0); shared.Send(a, [1], 0); shared.Flush(1);
        Check(shared.Sent == 3 && shared.Reordered > 0, "Later same-peer packets can overtake earlier queued packets");
        var reused = new ImpairedLink(multiplayer, true);
        for (var i = 0; i < 5; i++) reused.Send(a, [1], 0);
        reused.Flush(.1); var before = reused.Reordered;
        Check(reused.Sent == 4, "A long-tail packet remains queued after the short delays expire");
        reused.Send(a, [1], .1); reused.Flush(.3);
        Check(reused.Sent == 6 && reused.Reordered == before + 1, "Slot reuse preserves packet age when measuring same-peer reordering");
        Check(reused.PendingPackets == 0 && reused.PendingBytes == 0 && reused.PeakPendingPackets == 5 && reused.PeakPendingBytes == 5,
            "Draining reused slots preserves queue high water while removing current payload bytes");
        reused.Send(a, [1, 2, 3], 1);
        Check(reused.Dropped == 1 && reused.PendingPackets == 0 && reused.PendingBytes == 0, "Dropped input never occupies a queue slot");
        var forgotten = new ImpairedLink(multiplayer, false);
        forgotten.Send(a, [1, 2], 0); forgotten.Send(b, [1, 2, 3], 0); forgotten.Forget(a); forgotten.Forget(a);
        Check(forgotten.PendingPackets == 1 && forgotten.PendingBytes == 3 && forgotten.PeakPendingPackets == 2 && forgotten.PeakPendingBytes == 5,
            "Forgetting a peer removes only its live payload once");
        forgotten.Flush(1);
        Check(forgotten.Sent == 1 && forgotten.PendingPackets == 0 && forgotten.PendingBytes == 0, "The retained peer's payload still sends and drains");
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
    internal static void CheckHandoffInputs()
    {
        using var session = new NetworkSession(true, PhysicsServer.Backend.CPU, 0, Guid.NewGuid(), 360, false, true);
        session._peers[0] = new(2, 0) { Ready = true, CheckPhases = 1 }; session._peers[1] = new(3, 0) { Ready = true, CheckPhases = 1 };
        session._firstPeer = 2; session._secondPeer = 3;
        session.SetOwner(10, 2); session.SetOwner(11, 3);
        while (session.Simulation.Tick < 239) session.AuthorityStep();
        Feed(2, 10, 240); Feed(3, 11, 240);
        Check(!session.CheckProgressAllowsStep() && session.CheckHandoffWaitPolls == 1,
            "Current handoff-tick input cannot substitute for the missing old future commands");
        Feed(2, 10, 241);
        Check(!session.CheckProgressAllowsStep() && session.CheckHandoffWaitPolls == 2,
            "One player's future command cannot release the other player's handoff");
        Feed(3, 11, 241);
        Check(session.CheckProgressAllowsStep(), "Both exact old-controller future commands release the handoff");
        session.AuthorityStep();
        Check(session.Simulation.Tick == 240 && session.CheckHandoffPrepared == 2 && session.CheckHandoffApplied == 0 &&
            !session.CheckProgressAllowsStep(), "Retired epochs cannot satisfy the next tick after the actual owner change");
        Feed(3, 10, 241); Check(!session.CheckProgressAllowsStep(), "Both new-controller replacements are required independently");
        Feed(2, 11, 241); Check(session.CheckProgressAllowsStep(), "Both new epochs release the actual input gate");
        session.AuthorityStep();
        Check(session.CheckHandoffApplied == 2 && session.RetiredCommandReplacements == 2 &&
            session.Simulation.Find(10)!.LastInputTick == 241 && session.Simulation.Find(11)!.LastInputTick == 241,
            "Both replacements actually reach the controlled bodies at the prepared tick");
        void Feed(int peer, ulong id, ulong tick)
        {
            var actor = session.Simulation.Find(id)!;
            var writer = new WireWriter(session._send); writer.U64(0); writer.U64(0); writer.I32(1);
            writer.Input(new(tick, id, actor.Spec.Generation, actor.Spec.ControlEpoch, new(1, false)));
            var reader = new WireReader(session._send.AsSpan(0, writer.Count)); session.ReceiveInputs(peer, ref reader);
        }
        static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
    internal static void CheckPresentationOffsetTiming()
    {
        using var session = new NetworkSession(true, PhysicsServer.Backend.CPU, 0, Guid.NewGuid(), 360, false);
        var snapshot = new PhysicsSnapshot(24000); session.Simulation.Map.Capture(snapshot);
        var before = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(before);
        session._offsets[0] = new(10, 0); session._offsetTimes[0] = 1;
        session._offsets[1] = new(7, 0); session._offsetTimes[1] = 1;
        session.AddPresentationOffset(0, new(35, -15), 1.2);
        var fresh = new Vector2(36.3533528f, -15);
        Check(session._offsets[0].DistanceTo(fresh) < .0001f, "Only the prior offset ages before a newly received correction");
        session.DecayPresentationOffset(0, 1.2);
        Check(session._offsets[0].DistanceTo(fresh) < .0001f, "The first display retains the fresh correction after a long poll gap");
        session.DecayPresentationOffset(0, 1.3); session.DecayPresentationOffset(1, 1.3);
        Check(session._offsets[0].DistanceTo(fresh * .36787945f) < .0001f && session._offsets[1].DistanceTo(new(.3485095f, 0)) < .0001f,
            "Each actor retains its own exact exponential decay interval");
        session.Simulation.Map.Capture(snapshot); var after = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(after);
        Check(before.AsSpan().SequenceEqual(after), "Correction presentation timing never changes portable physical state");
        static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }

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
