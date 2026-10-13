using System.Buffers.Binary;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunPortableJoints(bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            PhysicsServerManager.RegisterServer("PortableJoints", static () => new Solver());
            for (var kind = 0; kind < 3; kind++)
                for (var scene = 0; scene < 2; scene++)
                {
                    using var cpu = new PortableJointRig(new World(PhysicsServer.Backend.CPU), kind, scene != 0, false);
                    using var registered = new PortableJointRig(new World("PortableJoints"), kind, scene != 0, true);
                    var sent = new PhysicsSnapshot(65536); var received = new PhysicsSnapshot(65536); var packet = new byte[65536];
                    cpu.Reset(); cpu.Tree.PhysicsFrame(.01); registered.Body.Position += new Vector2(100, 30);
                    Transfer(cpu, registered); Frames(cpu, registered);
                    foreach (var target in new[] { cpu, registered })
                    {
                        var cap = PhysicsServer.JointGetMaxForce(target.Joint); PhysicsServer.JointSetMaxForce(target.Joint, cap + 1);
                        var before = new PhysicsSnapshot(65536); target.Map.Capture(before); var bytes = new byte[before.GetEncodedSize()]; before.WriteTo(bytes); var events = target.Events;
                        Reject<System.IO.InvalidDataException>(() => target.Map.Apply(received));
                        var after = new PhysicsSnapshot(65536); target.Map.Capture(after); var actual = new byte[after.GetEncodedSize()]; after.WriteTo(actual);
                        Check(bytes.AsSpan().SequenceEqual(actual) && target.Events == events, "Incompatible joint policy rejects before complete state or event mutation");
                        PhysicsServer.JointSetMaxForce(target.Joint, cap); target.Map.Apply(received);
                        using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(target.World.Space);
                        target.Body.ApplyCentralImpulse(new(1, 2)); for (var step = 0; step < 12; step++) target.Tree.PhysicsFrame(.01);
                        var pose = target.Body.GlobalTransform; var velocity = target.Body.LinearVelocity;
                        checkpoint.Restore(); target.Body.ApplyCentralImpulse(new(1, 2)); for (var step = 0; step < 12; step++) target.Tree.PhysicsFrame(.01);
                        Check(target.Body.GlobalPosition.DistanceTo(pose.Origin) < .005f && MathF.Abs(target.Body.GlobalRotation - pose.Rotation) < .002f &&
                            target.Body.LinearVelocity.DistanceTo(velocity) < .005f,
                            "A local checkpoint replays the same real joint response after portable correction");
                    }
                    Collect(); for (var i = 0; i < 128; i++) Cycle();
                    var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                    for (var i = 0; i < 128; i++) Cycle();
                    var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
                    Check(ownerBytes == 0 && allBytes == 0, $"Portable joint kind={kind}, scene={scene} cycles allocate {ownerBytes}/{allBytes} B");

                    void Cycle()
                    {
                        cpu.Reset(); cpu.Tree.PhysicsFrame(.01); Transfer(cpu, registered); Frames(cpu, registered);
                        cpu.Tree.PhysicsFrame(.01); registered.Tree.PhysicsFrame(.01); registered.Verify();
                        Transfer(registered, cpu); Frames(registered, cpu); cpu.Tree.PhysicsFrame(.01); cpu.Verify();
                    }
                    void Transfer(PortableJointRig source, PortableJointRig target)
                    {
                        source.Map.Capture(sent); var count = sent.WriteTo(packet); received.ReadFrom(packet.AsSpan(0, count)); var events = target.Events;
                        target.Map.Apply(received);
                        Check(target.Events == events && PhysicsServer.SpaceGetTick(target.World.Space) == received.Tick &&
                            target.Body.GlobalPosition.DistanceTo(source.Body.GlobalPosition) < .001f && target.Body.LinearVelocity.DistanceTo(source.Body.LinearVelocity) < .001f,
                            "Joint correction preserves imported pose/motion/tick and local identity without transform events");
                    }
                    void Frames(PortableJointRig source, PortableJointRig target)
                    {
                        source.Map.Capture(sent); var count = sent.WriteTo(packet); source.ReadFrames(packet.AsSpan(0, count));
                        target.Map.Capture(sent); count = sent.WriteTo(packet); target.ReadFrames(packet.AsSpan(0, count));
                        Check(source.First.Origin.DistanceTo(target.First.Origin) < .0001f && source.Second.Origin.DistanceTo(target.Second.Origin) < .0001f &&
                            source.First.X.DistanceTo(target.First.X) < .0001f && source.Second.X.DistanceTo(target.Second.X) < .0001f,
                            "Portable joint frames retain both authored local anchors and axes in a different numerical owner");
                    }
                }
            Console.WriteLine($"Portable joints CPU/registered, threaded={threaded}: scene/raw pin/groove/spring frames, atomic policy guards, private replay and 0/0 warm full-frame transfers passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private sealed class PortableJointRig : IDisposable
    {
        internal readonly World World;
        internal readonly SceneTree Tree;
        internal readonly PhysicsSnapshotMap Map;
        internal readonly StaticBody Anchor;
        internal readonly RigidBody Body;
        internal readonly RID Joint;
        internal int Events;
        internal Transform First, Second;
        private readonly int _kind;
        private readonly Joint? _sceneJoint;
        private readonly CircleShape _circle = new() { Radius = 2 };
        private readonly byte[] _prefix = new byte[32];
        internal PortableJointRig(World world, int kind, bool scene, bool reverse)
        {
            World = world; _kind = kind;
            var root = new SubViewport { World = world }; Tree = new(root);
            if (reverse) { Body = Ball(); Anchor = Fixed(); root.AddChild(Body); root.AddChild(Anchor); }
            else { Anchor = Fixed(); Body = Ball(); root.AddChild(Anchor); root.AddChild(Body); }
            Anchor.Rotation = reverse ? -.6f : .3f; Body.Rotation = reverse ? .7f : -.4f;
            if (scene)
            {
                _sceneJoint = kind switch { 0 => new PinJoint(), 1 => new GrooveJoint(), _ => new DampedSpringJoint() };
                root.AddChild(_sceneJoint); Joint = _sceneJoint.GetRID();
            }
            else Joint = PhysicsServer.JointCreate();
            switch (kind)
            {
                case 0: PhysicsServer.JointMakePin(Joint, new(0, 2), Anchor.GetRID(), Body.GetRID()); PhysicsServer.PinJointSetSoftness(Joint, .2f); break;
                case 1: PhysicsServer.JointMakeGroove(Joint, new(-10, 2), new(10, 2), new(0, 2), Anchor.GetRID(), Body.GetRID()); break;
                case 2:
                    PhysicsServer.JointMakeDampedSpring(Joint, new(-10, 2), new(10, 2), Anchor.GetRID(), Body.GetRID());
                    PhysicsServer.DampedSpringJointSetRestLength(Joint, 10); PhysicsServer.DampedSpringJointSetStiffness(Joint, 2); PhysicsServer.DampedSpringJointSetDamping(Joint, 2); break;
            }
            PhysicsServer.JointSetBias(Joint, .2f); PhysicsServer.JointSetMaxBias(Joint, 10); PhysicsServer.JointSetMaxForce(Joint, 1000);
            Body.NotifyTransformChanges = true; Body.TransformChanged += _ => Events++;
            Map = new(World.Space); Map.Bind(10, 1, Anchor.GetRID()); Map.Bind(20, 1, Body.GetRID()); Map.Bind(30, 1, Joint);
            BinaryPrimitives.WriteUInt64LittleEndian(_prefix, 30); BinaryPrimitives.WriteUInt32LittleEndian(_prefix.AsSpan(8), 1);
            BinaryPrimitives.WriteUInt32LittleEndian(_prefix.AsSpan(12), (uint)kind);
            BinaryPrimitives.WriteUInt64LittleEndian(_prefix.AsSpan(16), 10); BinaryPrimitives.WriteUInt64LittleEndian(_prefix.AsSpan(24), 20);

            StaticBody Fixed() { var body = new StaticBody { Position = new(-10, 0), CollisionLayer = 0, CollisionMask = 0 }; body.AddChild(new CollisionShape { Shape = _circle }); return body; }
            RigidBody Ball()
            {
                var body = new RigidBody
                {
                    Position = new(10, 0),
                    Mass = 2,
                    Inertia = 2,
                    GravityScale = 0,
                    CanSleep = false,
                    CollisionLayer = 0,
                    CollisionMask = 0,
                    LinearDamp = 0,
                    AngularDamp = 0,
                    LinearDampMode = RigidBody.DampMode.Replace,
                    AngularDampMode = RigidBody.DampMode.Replace
                };
                body.AddChild(new CollisionShape { Shape = _circle }); return body;
            }
        }
        internal void Reset() { Anchor.Position = new(-10, 0); Anchor.Rotation = .3f; Body.Position = new(10, 0); Body.Rotation = -.4f; Body.LinearVelocity = new(0, 2); Body.AngularVelocity = .5f; }
        internal void ReadFrames(ReadOnlySpan<byte> packet)
        {
            var offset = packet.IndexOf(_prefix); Check(offset >= 0, "The complete bound joint is present in the packet");
            First = Pose(packet.Slice(offset + 32, 24)); Second = Pose(packet.Slice(offset + 56, 24));
            static Transform Pose(ReadOnlySpan<byte> bytes) => new(new Vector2(F(bytes, 0), F(bytes, 4)), new Vector2(F(bytes, 8), F(bytes, 12)), new Vector2(F(bytes, 16), F(bytes, 20)));
            static float F(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadSingleLittleEndian(bytes[offset..]);
        }
        internal void Verify()
        {
            var first = Anchor.GlobalTransform * First; var second = Body.GlobalTransform * Second;
            Check(Body.GlobalPosition.IsFinite() && Body.LinearVelocity.IsFinite() && float.IsFinite(Body.AngularVelocity) && Body.GlobalPosition.Length() < 100,
                "A corrected joint remains finite and spatially bounded under real simulation");
            if (_kind == 0) Check(first.Origin.DistanceTo(second.Origin) < .3f, "The imported pin retains its sampled anchor constraint");
            if (_kind == 1)
            {
                var delta = second.Origin - first.Origin; var axial = delta.Dot(first.X);
                Check(MathF.Abs(delta.Dot(first.Y)) < .3f && axial > -.3f && axial < 20.3f, "The imported groove retains its sampled guide and finite limits");
            }
        }
        public void Dispose() { Map.Dispose(); if (_sceneJoint is null) PhysicsServer.FreeRID(Joint); Tree.Dispose(); World.Dispose(); _circle.Dispose(); }
    }
}
