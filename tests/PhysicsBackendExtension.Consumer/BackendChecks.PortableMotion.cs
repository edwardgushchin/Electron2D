using System.Buffers.Binary;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunPortableMotion(bool threaded = false, PhysicsServer.Backend? backend = null)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (backend is null) PhysicsServerManager.RegisterServer("PortableMotion", () => new Solver());
            using var cpu = new PortableContactRig(new World(PhysicsServer.Backend.CPU));
            using var registered = new PortableContactRig(backend is { } selected ? new World(selected) : new World("PortableMotion"));
            Check(cpu.World.PhysicsBackend == PhysicsServer.Backend.CPU && registered.World.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) &&
                cpu.World.PhysicsFallbackReason is null && registered.World.PhysicsFallbackReason is null,
                "Portable motion uses selected numerical owners without startup fallback");
            foreach (var rig in new[] { cpu, registered })
            {
                rig.Character.PlatformWallLayers = uint.MaxValue;
                rig.Peer.ConstantLinearVelocity = Vector2.Up * 4;
            }
            cpu.Contact(); registered.Separate();
            Check(cpu.Character.GetSlideCollisionCount() == 1 && cpu.Character.GetPlatformVelocity().Y < -3.9f,
                "Motion validation starts with a real wall slide and nonzero surface platform velocity");
            var snapshot = new PhysicsSnapshot(65536); cpu.Map.Capture(snapshot); var packet = Encode(snapshot);
            var prefix = new byte[24]; BinaryPrimitives.WriteUInt64LittleEndian(prefix, 40); BinaryPrimitives.WriteUInt64LittleEndian(prefix.AsSpan(8), 20);
            BinaryPrimitives.WriteInt32LittleEndian(prefix.AsSpan(16), 1); BinaryPrimitives.WriteInt32LittleEndian(prefix.AsSpan(20), 2);
            var slide = packet.AsSpan().IndexOf(prefix);
            Check(slide >= 16 && BinaryPrimitives.ReadUInt64LittleEndian(packet.AsSpan(slide - 12)) == 20,
                "The real platform reference and complete slide record are present in the encoding");
            var received = new PhysicsSnapshot(65536); received.ReadFrom(packet);
            for (var edit = 0; edit < 8; edit++)
            {
                var bad = (byte[])packet.Clone();
                switch (edit)
                {
                    case 0: F32(68, .75f); F32(72, .25f); break;
                    case 1: F32(40, -1); break;
                    case 2: BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(slide + 76), 0); break;
                    case 3: BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(slide + 16), -1); break;
                    case 4: BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(slide + 20), -1); break;
                    case 5: BinaryPrimitives.WriteUInt64LittleEndian(bad.AsSpan(slide), 20); break;
                    case 6: BinaryPrimitives.WriteUInt64LittleEndian(bad.AsSpan(slide + 8), 0); break;
                    case 7: BinaryPrimitives.WriteUInt64LittleEndian(bad.AsSpan(slide), 0); break;
                }
                Reject<System.IO.InvalidDataException>(() => received.ReadFrom(bad));
                Check(packet.AsSpan().SequenceEqual(Encode(received)), "Invalid slide geometry, fractions or hit identity preserves the last complete packet");
                void F32(int offset, float value) => BinaryPrimitives.WriteSingleLittleEndian(bad.AsSpan(slide + offset), value);
            }
            foreach (var platform in new ulong[] { 40, 30, 999 })
                foreach (var target in new[] { cpu, registered })
                {
                    var before = new PhysicsSnapshot(65536); target.Map.Capture(before); var bytes = Encode(before); var events = target.Events;
                    var bad = (byte[])packet.Clone(); BinaryPrimitives.WriteUInt64LittleEndian(bad.AsSpan(slide - 12), platform); received.ReadFrom(bad);
                    Reject<System.IO.InvalidDataException>(() => target.Map.Apply(received));
                    var after = new PhysicsSnapshot(65536); target.Map.Capture(after);
                    Check(bytes.AsSpan().SequenceEqual(Encode(after)) && target.Events == events && bad.AsSpan().SequenceEqual(Encode(received)),
                        "Self, Area and unbound platform references reject before complete state or event mutation");
                }
            received.ReadFrom(packet); var platformVelocity = cpu.Character.GetPlatformVelocity();
            foreach (var target in new[] { cpu, registered })
            {
                var events = target.Events; target.Map.Apply(received);
                Check(target.Events == events && target.Character.GetPlatformVelocity() == platformVelocity,
                    "A real body platform transfers silently into the destination's own world");
                target.VerifyCharacterQuery(); var before = target.Character.GlobalPosition;
                target.Character.Velocity = Vector2.Zero; target.Tree.PhysicsFrame(.01); target.Tree.ProcessFrame(.01); target.Character.MoveAndSlide();
                var carried = before.Y - target.Character.GlobalPosition.Y;
                Check(carried > .02f && carried < .1f, "The next real movement follows the remapped platform's nonzero surface velocity");
                target.Map.Capture(snapshot);
            }
            Console.WriteLine($"Portable motion CPU/{backend?.ToString() ?? "registered"}, threaded={threaded}: eight atomic wire guards, six platform binding guards and real platform continuation passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }

        static byte[] Encode(PhysicsSnapshot snapshot)
        { var bytes = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(bytes); return bytes; }
    }
}
