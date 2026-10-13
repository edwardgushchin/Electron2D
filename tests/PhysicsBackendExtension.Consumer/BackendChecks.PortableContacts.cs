using System.Buffers.Binary;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunPortableContacts(bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            PhysicsServerManager.RegisterServer("PortableContacts", () => new Solver());
            using var cpu = new PortableContactRig(new World(PhysicsServer.Backend.CPU));
            using var registered = new PortableContactRig(new World("PortableContacts"));
            var sent = new PhysicsSnapshot(65536); var received = new PhysicsSnapshot(65536); var packet = new byte[65536];
            cpu.Contact(); cpu.Map.Capture(sent); var size = sent.WriteTo(packet);
            using (var checkpoint = PhysicsServer.SpaceCreateCheckpoint(cpu.World.Space))
            {
                var expected = cpu.Character.Position; cpu.Character.Position = new(90, 0); checkpoint.Restore();
                Check(cpu.Character.Position == expected, "A local checkpoint restores character movement after its query pose was prepared");
                cpu.VerifyCharacterQuery();
            }
            Check(cpu.State.GetContactCount() == 1 && cpu.State.GetContactLocalShape(0) == 1 && cpu.State.GetContactColliderShape(0) == 1,
                "A real CPU manifold uses the enabled logical slot after a disabled slot");
            Check(cpu.State.GetContactImpulse(0).DistanceTo((cpu.Mover.LinearVelocity - new Vector2(20, 5)) * 2) < .1f &&
                MathF.Abs(cpu.State.GetContactImpulse(0).X) > 20 && MathF.Abs(cpu.State.GetContactImpulse(0).Y) > 1,
                "The source's complete normal/tangent impulse agrees with actual momentum change");
            Check(cpu.Character.GetSlideCollisionCount() > 0, "A real character sweep supplies portable slide history");
            var contactPrefix = new byte[64]; BinaryPrimitives.WriteUInt64LittleEndian(contactPrefix, 20);
            BinaryPrimitives.WriteInt32LittleEndian(contactPrefix.AsSpan(8), 1); BinaryPrimitives.WriteInt32LittleEndian(contactPrefix.AsSpan(12), 1);
            Vector2[] values = [cpu.State.GetContactLocalPosition(0), cpu.State.GetContactColliderPosition(0), cpu.State.GetContactLocalNormal(0),
                cpu.State.GetContactLocalVelocityAtPosition(0), cpu.State.GetContactColliderVelocityAtPosition(0), cpu.State.GetContactImpulse(0)];
            for (var i = 0; i < values.Length; i++)
            {
                BinaryPrimitives.WriteSingleLittleEndian(contactPrefix.AsSpan(16 + i * 8), values[i].X);
                BinaryPrimitives.WriteSingleLittleEndian(contactPrefix.AsSpan(20 + i * 8), values[i].Y);
            }
            var contact = Find(contactPrefix); var pairs = Find(PairPrefix(20)); var areaPairs = Find(PairPrefix(10));
            Check(pairs < contact, "The body-monitor record is distinct from its direct contact record");
            var slidePrefix = new byte[24]; BinaryPrimitives.WriteUInt64LittleEndian(slidePrefix, 40);
            BinaryPrimitives.WriteUInt64LittleEndian(slidePrefix.AsSpan(8), 20);
            BinaryPrimitives.WriteInt32LittleEndian(slidePrefix.AsSpan(16), 1); BinaryPrimitives.WriteInt32LittleEndian(slidePrefix.AsSpan(20), 2);
            var slide = Find(slidePrefix);
            registered.Separate();
            var before = new PhysicsSnapshot(65536); var after = new PhysicsSnapshot(65536); var beforeBytes = new byte[65536]; var afterBytes = new byte[65536];
            Check(cpu.Character.GetSlideCollisionCount() == 1 && BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(slide - 16)) == 1,
                "The current format explicitly carries the real character's prepared query-pose flag");
            received.ReadFrom(packet.AsSpan(0, size)); received.WriteTo(beforeBytes);
            BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(slide - 16), 2);
            Reject<System.IO.InvalidDataException>(() => received.ReadFrom(packet.AsSpan(0, size)));
            received.WriteTo(afterBytes);
            Check(beforeBytes.AsSpan(0, size).SequenceEqual(afterBytes.AsSpan(0, size)), "A malformed query-pose boolean preserves the previous complete packet");
            BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(slide - 16), 1);
            foreach (var target in new[] { cpu, registered })
                foreach (var offset in new[] { contact + 8, contact + 12, pairs + 12, pairs + 16, areaPairs + 12, areaPairs + 16, slide + 16, slide + 20 })
                {
                    target.Map.Capture(before); var oldSize = before.WriteTo(beforeBytes); var events = target.Events;
                    var original = BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(offset)); BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(offset), 999);
                    received.ReadFrom(packet.AsSpan(0, size));
                    Reject<System.IO.InvalidDataException>(() => target.Map.Apply(received));
                    target.Map.Capture(after); var newSize = after.WriteTo(afterBytes);
                    Check(oldSize == newSize && beforeBytes.AsSpan(0, oldSize).SequenceEqual(afterBytes.AsSpan(0, newSize)) && target.Events == events,
                        "Out-of-range contact, monitor and slide indices reject without changing complete state or delivering events");
                    BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(offset), original);
                }
            Transfer(cpu, registered); Imported(cpu, registered);
            var enters = registered.MoverEnters; var areaEnters = registered.AreaEnters;
            registered.Tree.PhysicsFrame(.01);
            Check(registered.MoverEnters == enters && registered.AreaEnters == areaEnters,
                "Restored contact/overlap histories do not repeat enters on the next real tick");
            registered.Separate(); Check(registered.MoverExits > 0 && registered.AreaExits > 0,
                "A restored contact and raw Area overlap emit ordinary exits after separation");
            cpu.Contact();
            PhysicsServer.BodySetShapeDisabled(cpu.Mover.GetRID(), 1, true); PhysicsServer.BodySetShapeDisabled(registered.Mover.GetRID(), 1, true);
            Transfer(cpu, registered);
            Check(registered.State.GetContactCount() == cpu.State.GetContactCount(),
                "A completed history can retain a currently disabled logical slot until the next interval");
            PhysicsServer.BodySetShapeDisabled(cpu.Mover.GetRID(), 1, false); PhysicsServer.BodySetShapeDisabled(registered.Mover.GetRID(), 1, false);
            cpu.Separate(); registered.Separate();
            Collect(); for (var i = 0; i < 128; i++) Cycle();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Cycle();
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownerBytes == 0 && allBytes == 0, $"Portable contact cycles allocate zero owner/all-thread bytes: {ownerBytes}/{allBytes}");
            Console.WriteLine($"Portable contacts CPU/registered, threaded={threaded}: real impulse, remapped contacts/slides/Area history, sixteen atomic index guards and 0/0 warm cycles passed.");

            void Cycle()
            {
                cpu.Contact(); registered.Separate(); Transfer(cpu, registered); Imported(cpu, registered);
                var bodyEnters = registered.MoverEnters; var overlaps = registered.AreaEnters;
                registered.Tree.PhysicsFrame(.01);
                Check(registered.MoverEnters == bodyEnters && registered.AreaEnters == overlaps, "A warm restored interval preserves observer history");
                Transfer(registered, cpu); Imported(registered, cpu); cpu.Separate(); registered.Separate();
            }
            void Transfer(PortableContactRig source, PortableContactRig target)
            {
                source.Map.Capture(sent); var bytes = sent.WriteTo(packet); received.ReadFrom(packet.AsSpan(0, bytes)); var events = target.Events;
                target.Map.Apply(received); Check(target.Events == events, "Portable application is silent for contact/Area/transform observers");
            }
            static void Imported(PortableContactRig source, PortableContactRig target)
            {
                Check(target.State.GetContactCount() == source.State.GetContactCount() && target.State.GetContactCount() == 1 &&
                    target.State.GetContactCollider(0) == target.Peer.GetRID() && target.State.GetContactColliderID(0) == target.Peer.InstanceID &&
                    ReferenceEquals(target.State.GetContactColliderObject(0), target.Peer), "Imported contacts resolve destination-local peer identities");
                Check(target.State.GetContactLocalShape(0) == 1 && target.State.GetContactColliderShape(0) == 1 &&
                    target.State.GetContactImpulse(0) == source.State.GetContactImpulse(0) &&
                    target.State.GetContactLocalPosition(0) == source.State.GetContactLocalPosition(0) &&
                    target.State.GetContactColliderPosition(0) == source.State.GetContactColliderPosition(0),
                    "The completed contact values and enabled logical indices survive immediate portable application");
                Check(target.Character.GetSlideCollisionCount() == source.Character.GetSlideCollisionCount() && target.Character.GetSlideCollisionCount() > 0,
                    "Character slide history survives correction before another move");
                target.Character.GetSlideCollision(0, target.Collision);
                Check(target.Collision.GetColliderRID() == target.Peer.GetRID() && target.Collision.GetColliderShapeIndex() == 2,
                    "Imported character slides resolve the destination peer and its actual distant slot");
                target.VerifyCharacterQuery();
            }
            int Find(byte[] prefix)
            {
                var index = packet.AsSpan(0, size).IndexOf(prefix); Check(index >= 0, "The real encoded fixture contains its expected contact/history prefix"); return index;
            }
            static byte[] PairPrefix(ulong id)
            {
                var bytes = new byte[20]; BinaryPrimitives.WriteUInt64LittleEndian(bytes, id);
                BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), 1); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16), 1); return bytes;
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private sealed class PortableContactRig : IDisposable
    {
        internal readonly World World;
        internal readonly SceneTree Tree;
        internal readonly PhysicsSnapshotMap Map;
        internal readonly RigidBody Mover;
        internal readonly StaticBody Peer;
        internal readonly CharacterBody Character;
        internal readonly PhysicsDirectBodyState State;
        internal readonly KinematicCollision Collision = new();
        private readonly PhysicsDirectSpaceState _direct;
        private readonly PhysicsPointQueryParameters _point = new();
        private readonly PhysicsPointResult[] _hits = new PhysicsPointResult[2];
        internal int MoverEnters, MoverExits, AreaEnters, AreaExits, Transforms;
        internal (int, int, int, int, int) Events => (MoverEnters, MoverExits, AreaEnters, AreaExits, Transforms);
        private readonly CircleShape _circle = new() { Radius = 1 }, _region = new() { Radius = 5 };
        private readonly PhysicsMaterial _material = new() { Friction = .6f, Bounce = 0 };
        private readonly RID _area;
        internal PortableContactRig(World world)
        {
            World = world; var root = new SubViewport { World = world }; Tree = new(root);
            Mover = new()
            {
                Name = "Mover",
                Mass = 2,
                Inertia = 2,
                LockRotation = true,
                GravityScale = 0,
                CanSleep = false,
                MaxContactsReported = 4,
                ContactMonitor = true,
                PhysicsMaterialOverride = _material,
                Position = new(50, 0),
                LinearDamp = 0,
                AngularDamp = 0,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            Peer = new() { Name = "Peer", Position = new(2, 0), PhysicsMaterialOverride = _material };
            Character = new() { Name = "Character", Position = new(100, 0), MotionMode = CharacterMotionMode.Floating, PhysicsProcessEnabled = true };
            foreach (var body in new PhysicsBody[] { Mover, Peer, Character })
            {
                body.AddChild(new CollisionShape { Name = "Disabled", Shape = _circle, Disabled = true, Position = new(20, 0) });
                body.AddChild(new CollisionShape { Name = "Active", Shape = _circle }); root.AddChild(body);
            }
            Peer.AddChild(new CollisionShape { Name = "Distant", Shape = _circle, Position = new(102, 0) });
            _area = PhysicsServer.AreaCreate(); PhysicsServer.AreaAddShape(_area, _region.GetRID()); PhysicsServer.AreaSetShapeDisabled(_area, 0, true);
            PhysicsServer.AreaAddShape(_area, _region.GetRID()); PhysicsServer.AreaSetGravitySpaceOverride(_area, Area.SpaceOverride.Disabled);
            PhysicsServer.AreaSetMonitorCallback(_area, (status, _, _, _, _) => { if (status == PhysicsServer.AreaBodyStatus.Added) AreaEnters++; else AreaExits++; });
            PhysicsServer.AreaSetSpace(_area, World.Space);
            Mover.BodyEntered += _ => MoverEnters++; Mover.BodyExited += _ => MoverExits++;
            Mover.NotifyTransformChanges = true; Mover.TransformChanged += _ => Transforms++;
            State = PhysicsServer.BodyGetDirectState(Mover.GetRID())!; Map = new(World.Space);
            Map.Bind(10, 1, Mover.GetRID()); Map.Bind(20, 1, Peer.GetRID()); Map.Bind(30, 1, _area); Map.Bind(40, 1, Character.GetRID());
            _direct = world.DirectSpaceState;
        }
        internal void Contact()
        {
            Mover.Position = Vector2.Zero; Mover.LinearVelocity = new(20, 5); Mover.AngularVelocity = 0; Character.Position = new(100, 0);
            Tree.PhysicsFrame(.01); Character.Velocity = new(1000, 0); Character.MoveAndSlide();
        }
        internal void Separate() { Mover.Position = new(50, 0); Mover.LinearVelocity = Vector2.Zero; Tree.PhysicsFrame(.01); }
        internal void RemoveAreaShape() => PhysicsServer.AreaRemoveShape(_area, 1);
        internal void VerifyCharacterQuery()
        {
            _point.Position = Character.GlobalPosition;
            Check(_direct.IntersectPoint(_point, _hits) == 1 && _hits[0].ColliderRID == Character.GetRID() && _hits[0].ShapeIndex == 1,
                "The corrected character remains queryable at its displayed pose before the next simulation interval");
        }
        public void Dispose()
        {
            Map.Dispose(); State.Dispose(); Collision.Dispose(); _direct.Dispose(); _point.Dispose(); PhysicsServer.FreeRID(_area);
            Tree.Dispose(); World.Dispose(); _circle.Dispose(); _region.Dispose(); _material.Dispose();
        }
    }
}
