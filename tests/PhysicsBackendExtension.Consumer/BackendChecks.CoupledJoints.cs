using Electron2D;

public static partial class BackendChecks
{
    public static void RunCoupledJoints(PhysicsServer.Backend backend = PhysicsServer.Backend.CPU, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            VerifyCoupledContributions(backend);
            foreach (var sceneJoints in new[] { false, true })
            {
                using var rig = new CoupledJointRig(backend, sceneJoints);
                for (var i = 0; i < 128; i++) rig.Step();
                Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) rig.Step();
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0, $"Coupled joints allocate {owner}/{all} B");
                rig.VerifyReplay();
                using var authority = new CoupledJointRig(PhysicsServer.Backend.CPU, sceneJoints);
                for (var i = 0; i < 64; i++) authority.Step();
                var sent = new PhysicsSnapshot(65536); var received = new PhysicsSnapshot(65536); var packet = new byte[65536];
                var identities = (RID[])rig.Bodies.Clone(); var transmissions = 0;
                PhysicsServer.BodySetTransform(rig.Bodies[0], new(.3f, new Vector2(200, 200)));
                PhysicsServer.BodySetLinearVelocity(rig.Bodies[0], new(-100, 70));
                Cycle();
                var maxForce = PhysicsServer.JointGetMaxForce(rig.Joints[0]); PhysicsServer.JointSetMaxForce(rig.Joints[0], 0);
                var before = new PhysicsSnapshot(65536); var after = new PhysicsSnapshot(65536); rig.Map.Capture(before);
                var beforeBytes = new byte[before.GetEncodedSize()]; before.WriteTo(beforeBytes); var rejectedEvents = rig.Events;
                Reject<System.IO.InvalidDataException>(() => rig.Map.Apply(received)); rig.Map.Capture(after);
                var afterBytes = new byte[after.GetEncodedSize()]; after.WriteTo(afterBytes);
                Check(beforeBytes.AsSpan().SequenceEqual(afterBytes) && rig.Events == rejectedEvents, "Incompatible coupled policy rejects before any world or event mutation");
                PhysicsServer.JointSetMaxForce(rig.Joints[0], maxForce);
                for (var i = 0; i < 128; i++) Cycle();
                Collect(); owner = GC.GetAllocatedBytesForCurrentThread(); all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Cycle();
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0, $"Coupled complete transport/frames allocate {owner}/{all} B");
                Console.WriteLine($"Coupled {backend}, sceneJoints={sceneJoints}, threaded={threaded}: pin={rig.MaxPinError:F6}, groove={rig.MaxGrooveError:F6}, momentum={rig.MaxMomentumError:F6}, angular={rig.MaxAngularError:F6}, energy={rig.MaxEnergy:F6}; replay and {transmissions} portable corrections; 128 full frames and 128 full transport cycles 0/0 B.");
                void Cycle()
                {
                    authority.Step(); authority.Map.Capture(sent); var count = sent.WriteTo(packet); received.ReadFrom(packet.AsSpan(0, count));
                    var events = rig.Events; rig.Map.Apply(received); rig.ImportTicks(authority);
                    Check(rig.Events == events, "Authoritative coupled correction is silent");
                    for (var body = 0; body < 4; body++)
                    {
                        Check(rig.Bodies[body] == identities[body] && rig.Bodies[body] != authority.Bodies[body], "Portable IDs preserve distinct local scene/raw identity");
                        var actual = PhysicsServer.BodyGetTransform(rig.Bodies[body]); var expected = PhysicsServer.BodyGetTransform(authority.Bodies[body]);
                        Check(actual.Origin.DistanceTo(expected.Origin) < .002f && MathF.Abs(Mathf.AngleDifference(actual.Rotation, expected.Rotation)) < .002f &&
                            PhysicsServer.BodyGetLinearVelocity(rig.Bodies[body]).DistanceTo(PhysicsServer.BodyGetLinearVelocity(authority.Bodies[body])) < .002f &&
                            MathF.Abs(PhysicsServer.BodyGetAngularVelocity(rig.Bodies[body]) - PhysicsServer.BodyGetAngularVelocity(authority.Bodies[body])) < .002f &&
                            PhysicsServer.BodyGetSleeping(rig.Bodies[body]) == PhysicsServer.BodyGetSleeping(authority.Bodies[body]) &&
                            PhysicsServer.BodyGetCanSleep(rig.Bodies[body]) == PhysicsServer.BodyGetCanSleep(authority.Bodies[body]) &&
                            PhysicsServer.BodyGetMode(rig.Bodies[body]) == PhysicsServer.BodyGetMode(authority.Bodies[body]),
                            "All coupled pose/motion/sleep/role values converge to authoritative state");
                    }
                    rig.Step(); transmissions++;
                }
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void VerifyCoupledContributions(PhysicsServer.Backend backend)
    {
        using var baseline = new CoupledJointRig(backend, false); baseline.Step();
        for (var family = 0; family < 3; family++)
        {
            using var disabled = new CoupledJointRig(backend, false);
            if (family == 2)
            {
                PhysicsServer.DampedSpringJointSetStiffness(disabled.Joints[2], 0); PhysicsServer.DampedSpringJointSetDamping(disabled.Joints[2], 0);
            }
            else PhysicsServer.JointSetMaxForce(disabled.Joints[family], 0);
            disabled.Tree.PhysicsFrame(1d / 60); var difference = 0f;
            for (var body = 0; body < 4; body++) difference +=
                PhysicsServer.BodyGetLinearVelocity(disabled.Bodies[body]).DistanceSquaredTo(PhysicsServer.BodyGetLinearVelocity(baseline.Bodies[body]));
            Check(difference > .00001f, $"Coupled joint family {family} has no observable numerical contribution: {difference}");
            Console.WriteLine($"Coupled negative control family={family}: disabled response differs by {difference:F6} squared velocity units.");
        }
    }

    private sealed class CoupledJointRig : IDisposable
    {
        internal readonly World World;
        internal readonly SceneTree Tree;
        internal readonly PhysicsSnapshotMap Map;
        internal int Events;
        internal readonly RID[] Bodies = new RID[4], Joints = new RID[4];
        private readonly CircleShape _shape = new() { Radius = 2 };
        private readonly Transform[] _poses = new Transform[4];
        private readonly Vector2[] _velocities = new Vector2[4];
        private readonly float[] _angular = new float[4];
        private readonly bool _sceneJoints;
        private int _ticks;
        internal float MaxPinError, MaxGrooveError, MaxMomentumError, MaxAngularError, MaxEnergy;
        internal CoupledJointRig(PhysicsServer.Backend backend, bool sceneJoints)
        {
            _sceneJoints = sceneJoints; World = new(backend);
            var root = new SubViewport { World = World };
            Vector2[] positions = [new(0, 0), new(40, 0), new(40, 40), new(0, 40)];
            for (var i = 0; i < 4; i += 2)
            {
                var body = new RigidBody
                {
                    Name = $"Body{i}",
                    Position = positions[i],
                    Mass = 2,
                    Inertia = 5,
                    GravityScale = 0,
                    CanSleep = false,
                    CollisionLayer = 0,
                    CollisionMask = 0,
                    LinearDamp = 0,
                    AngularDamp = 0,
                    LinearDampMode = RigidBody.DampMode.Replace,
                    AngularDampMode = RigidBody.DampMode.Replace
                };
                body.NotifyTransformChanges = true; body.TransformChanged += _ => Events++;
                body.AddChild(new CollisionShape { Shape = _shape }); root.AddChild(body); Bodies[i] = body.GetRID();
            }
            if (sceneJoints)
            {
                Joint[] nodes = [new PinJoint(), new GrooveJoint(), new DampedSpringJoint(), new PinJoint()];
                for (var i = 0; i < 4; i++) { nodes[i].Name = $"Joint{i}"; root.AddChild(nodes[i]); Joints[i] = nodes[i].GetRID(); }
            }
            Tree = new(root);
            PhysicsServer.AreaSetGravity(World.Space, 0); PhysicsServer.AreaSetLinearDamp(World.Space, 0); PhysicsServer.AreaSetAngularDamp(World.Space, 0);
            for (var i = 1; i < 4; i += 2)
            {
                Bodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMass(Bodies[i], 2); PhysicsServer.BodySetInertia(Bodies[i], 5);
                PhysicsServer.BodySetCanSleep(Bodies[i], false); PhysicsServer.BodySetCollisionLayer(Bodies[i], 0); PhysicsServer.BodySetCollisionMask(Bodies[i], 0);
                PhysicsServer.BodyAddShape(Bodies[i], _shape.GetRID()); PhysicsServer.BodySetTransform(Bodies[i], new(0, positions[i]));
                PhysicsServer.BodySetSpace(Bodies[i], World.Space);
            }
            if (!sceneJoints) for (var i = 0; i < 4; i++) Joints[i] = PhysicsServer.JointCreate();
            PhysicsServer.JointMakePin(Joints[0], new(20, 0), Bodies[0], Bodies[1]);
            PhysicsServer.JointMakeGroove(Joints[1], new(40, 10), new(40, 50), new(40, 40), Bodies[1], Bodies[2]);
            PhysicsServer.JointMakeDampedSpring(Joints[2], new(40, 35), new(0, 45), Bodies[2], Bodies[3]);
            PhysicsServer.DampedSpringJointSetRestLength(Joints[2], 40); PhysicsServer.DampedSpringJointSetStiffness(Joints[2], 30); PhysicsServer.DampedSpringJointSetDamping(Joints[2], 5);
            PhysicsServer.JointMakePin(Joints[3], new(0, 20), Bodies[3], Bodies[0]);
            PhysicsServer.BodyApplyImpulse(Bodies[0], new(20, 0), new(0, 2));
            Map = new(World.Space);
            for (var i = 0; i < 4; i++) { Map.Bind((ulong)(10 + i), 1, Bodies[i]); Map.Bind((ulong)(110 + i), 1, Joints[i]); }
            Check(World.PhysicsBackend == backend && World.PhysicsFallbackReason is null, "Coupled graph retains the explicit backend without fallback");
        }
        internal void Step()
        {
            Tree.PhysicsFrame(1d / 60); _ticks++;
            var center = Vector2.Zero; var momentum = Vector2.Zero;
            for (var i = 0; i < 4; i++)
            {
                _poses[i] = PhysicsServer.BodyGetTransform(Bodies[i]); _velocities[i] = PhysicsServer.BodyGetLinearVelocity(Bodies[i]);
                _angular[i] = PhysicsServer.BodyGetAngularVelocity(Bodies[i]);
                Check(_poses[i].IsFinite() && _velocities[i].IsFinite() && float.IsFinite(_angular[i]), "Coupled state is finite");
                center += _poses[i].Origin / 4; momentum += _velocities[i] * 2;
            }
            var angularMomentum = 0f; var energy = 0f;
            for (var i = 0; i < 4; i++)
            {
                angularMomentum += (_poses[i].Origin - center).Cross(_velocities[i] * 2) + 5 * _angular[i];
                energy += _velocities[i].LengthSquared() + 2.5f * _angular[i] * _angular[i];
            }
            var pin = MathF.Max(Anchor(0, new(20, 0)).DistanceTo(Anchor(1, new(-20, 0))), Anchor(3, new(0, -20)).DistanceTo(Anchor(0, new(0, 20))));
            var start = Anchor(1, new(0, 10)); var end = Anchor(1, new(0, 50)); var point = Anchor(2, Vector2.Zero);
            var along = Math.Clamp((point - start).Dot(end - start) / (end - start).LengthSquared(), 0, 1);
            var groove = point.DistanceTo(start.Lerp(end, along));
            var extension = Anchor(2, new(0, -5)).DistanceTo(Anchor(3, new(0, 5))) - 40;
            energy += 15 * extension * extension;
            var momentumError = momentum.DistanceTo(new(20, 0)); var angularError = MathF.Abs(angularMomentum - 360);
            Check(float.IsFinite(pin) && float.IsFinite(groove) && float.IsFinite(momentumError) && float.IsFinite(angularError) && float.IsFinite(energy) && center.IsFinite(),
                "Derived coupled physical invariants remain finite");
            MaxPinError = MathF.Max(MaxPinError, pin); MaxGrooveError = MathF.Max(MaxGrooveError, groove);
            MaxMomentumError = MathF.Max(MaxMomentumError, momentumError); MaxAngularError = MathF.Max(MaxAngularError, angularError); MaxEnergy = MathF.Max(MaxEnergy, energy);
            if (pin >= .5f || groove >= .5f) throw new InvalidOperationException($"Coupled anchor constraint failed at {_ticks}: pin={pin}, groove={groove}");
            if (momentumError >= .03f || angularError >= 2) throw new InvalidOperationException($"Internal joint momentum changed at {_ticks}: linear={momentumError}, angular={angularError}");
            if (energy >= 600 || center.DistanceTo(new(20 + _ticks / 24f, 20)) >= .05f) throw new InvalidOperationException($"Coupled energy/center drift at {_ticks}: energy={energy}, center={center}");
        }
        internal void ImportTicks(CoupledJointRig source)
        {
            _ticks = source._ticks;
            Check(PhysicsServer.SpaceGetTick(World.Space) == PhysicsServer.SpaceGetTick(source.World.Space), "Coupled correction retains authoritative physical clock");
        }
        internal void VerifyReplay()
        {
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(World.Space); var ticks = _ticks;
            for (var i = 0; i < 32; i++) Step();
            var poses = (Transform[])_poses.Clone(); var velocities = (Vector2[])_velocities.Clone(); var angular = (float[])_angular.Clone();
            checkpoint.Restore(); _ticks = ticks;
            for (var i = 0; i < 32; i++) Step();
            for (var i = 0; i < 4; i++)
                Check(_poses[i].Origin.DistanceTo(poses[i].Origin) < .005f && MathF.Abs(_poses[i].Rotation - poses[i].Rotation) < .002f &&
                    _velocities[i].DistanceTo(velocities[i]) < .005f && MathF.Abs(_angular[i] - angular[i]) < .002f,
                    "Checkpoint replay retains complete coupled pin/groove/spring response");
        }
        private Vector2 Anchor(int body, Vector2 local) => _poses[body] * local;
        public void Dispose()
        {
            if (!_sceneJoints) foreach (var joint in Joints) PhysicsServer.FreeRID(joint);
            for (var i = 1; i < 4; i += 2) PhysicsServer.FreeRID(Bodies[i]);
            Tree.Dispose(); World.Dispose(); _shape.Dispose();
        }
    }
}
