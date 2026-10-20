using Electron2D;

public static partial class BackendChecks
{
    public static void RunCoupledJoints(PhysicsServer.Backend? backend = PhysicsServer.Backend.CPU, bool threaded = false, bool largeGraph = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (backend is null) PhysicsServerManager.RegisterServer("CoupledJoints", static () => new Solver());
            var count = largeGraph ? 64 : 4;
            VerifyCoupledContributions(backend, count);
            foreach (var sceneJoints in new[] { false, true })
            {
                using var rig = new CoupledJointRig(backend, sceneJoints, count);
                for (var i = 0; i < 128; i++) rig.Step();
                Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) rig.Step();
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0, $"Coupled joints allocate {owner}/{all} B");
                rig.VerifyReplay();
                using var authority = new CoupledJointRig(PhysicsServer.Backend.CPU, sceneJoints, count);
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
                Console.WriteLine($"Coupled bodies={count}, {backend?.ToString() ?? "registered"}, sceneJoints={sceneJoints}, threaded={threaded}: pin={rig.MaxPinError:F6}, groove={rig.MaxGrooveError:F6}, momentum={rig.MaxMomentumError:F6}, angular={rig.MaxAngularError:F6}, energy={rig.MaxEnergy:F6}; replay and {transmissions} portable corrections; 128 full frames and 128 full transport cycles 0/0 B.");
                void Cycle()
                {
                    authority.Step(); authority.Map.Capture(sent); var packetLength = sent.WriteTo(packet); received.ReadFrom(packet.AsSpan(0, packetLength));
                    var events = rig.Events; rig.Map.Apply(received); rig.ImportTicks(authority);
                    Check(rig.Events == events, "Authoritative coupled correction is silent");
                    for (var body = 0; body < count; body++)
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

    private static void VerifyCoupledContributions(PhysicsServer.Backend? backend, int count)
    {
        using var baseline = new CoupledJointRig(backend, false, count);
        for (var i = 0; i < 32; i++) baseline.Step();
        for (var family = 0; family < 3; family++)
        {
            using var disabled = new CoupledJointRig(backend, false, count);
            for (var joint = 0; joint < count; joint++)
            {
                if (disabled.Families[joint] != family) continue;
                if (family == 2)
                {
                    PhysicsServer.DampedSpringJointSetStiffness(disabled.Joints[joint], 0); PhysicsServer.DampedSpringJointSetDamping(disabled.Joints[joint], 0);
                }
                else PhysicsServer.JointSetMaxForce(disabled.Joints[joint], 0);
            }
            for (var i = 0; i < 32; i++) disabled.Tree.PhysicsFrame(1d / 60); var difference = 0f;
            for (var body = 0; body < count; body++) difference +=
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
        internal readonly RID[] Bodies, Joints;
        internal readonly int[] Families;
        private readonly CircleShape _shape = new() { Radius = 2 };
        private readonly Transform[] _poses;
        private readonly Vector2[] _velocities;
        private readonly Vector2[] _anchorA, _anchorB, _guideEnd;
        private readonly float[] _restLength;
        private readonly Vector2 _initialCenter, _momentum;
        private readonly float _angularMomentum, _energyLimit;
        private readonly float[] _angular;
        private readonly bool _sceneJoints;
        private int _ticks;
        internal float MaxPinError, MaxGrooveError, MaxMomentumError, MaxAngularError, MaxEnergy;
        internal CoupledJointRig(PhysicsServer.Backend? backend, bool sceneJoints, int count)
        {
            Bodies = new RID[count]; Joints = new RID[count]; Families = new int[count];
            _poses = new Transform[count]; _velocities = new Vector2[count]; _angular = new float[count];
            _anchorA = new Vector2[count]; _anchorB = new Vector2[count]; _guideEnd = new Vector2[count]; _restLength = new float[count];
            _sceneJoints = sceneJoints; World = backend is { } selected ? new(selected) : new("CoupledJoints");
            var root = new SubViewport { World = World };
            Vector2[] positions = [new(0, 0), new(40, 0), new(40, 40), new(0, 40)];
            if (count != 4)
            {
                positions = new Vector2[count]; var radius = 10 / MathF.Sin(MathF.PI / count);
                for (var i = 0; i < count; i++)
                {
                    var angle = Mathf.Tau * i / count;
                    positions[i] = new(radius * (MathF.Cos(angle) - 1), radius * MathF.Sin(angle));
                }
            }
            for (var i = 0; i < count; i++) { Families[i] = i % 3; _initialCenter += positions[i] / count; }
            for (var i = 0; i < count; i += 2)
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
                var nodes = new Joint[count];
                for (var i = 0; i < count; i++) nodes[i] = Families[i] switch { 0 => new PinJoint(), 1 => new GrooveJoint(), _ => new DampedSpringJoint() };
                for (var i = 0; i < count; i++) { nodes[i].Name = $"Joint{i}"; root.AddChild(nodes[i]); Joints[i] = nodes[i].GetRID(); }
            }
            Tree = new(root);
            PhysicsServer.AreaSetGravity(World.Space, 0); PhysicsServer.AreaSetLinearDamp(World.Space, 0); PhysicsServer.AreaSetAngularDamp(World.Space, 0);
            for (var i = 1; i < count; i += 2)
            {
                Bodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMass(Bodies[i], 2); PhysicsServer.BodySetInertia(Bodies[i], 5);
                PhysicsServer.BodySetCanSleep(Bodies[i], false); PhysicsServer.BodySetCollisionLayer(Bodies[i], 0); PhysicsServer.BodySetCollisionMask(Bodies[i], 0);
                PhysicsServer.BodyAddShape(Bodies[i], _shape.GetRID()); PhysicsServer.BodySetTransform(Bodies[i], new(0, positions[i]));
                PhysicsServer.BodySetSpace(Bodies[i], World.Space);
            }
            if (!sceneJoints) for (var i = 0; i < count; i++) Joints[i] = PhysicsServer.JointCreate();
            if (count == 4)
            {
                Pin(0, new(20, 0)); Groove(1, new(40, 10), new(40, 50), new(40, 40));
                Spring(2, new(40, 35), new(0, 45), 40); Pin(3, new(0, 20));
                _momentum = new(20, 0); PhysicsServer.BodyApplyImpulse(Bodies[0], _momentum, new(0, 2));
                _angularMomentum = 360; _energyLimit = 600;
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var next = (i + 1) % count; var edge = positions[next] - positions[i]; var middle = (positions[i] + positions[next]) * .5f;
                    if (Families[i] == 0) Pin(i, middle);
                    else if (Families[i] == 1)
                    {
                        var tangent = edge.Orthogonal().Normalized(); Groove(i, middle - tangent * 4, middle + tangent * 4, middle);
                    }
                    else
                    {
                        var a = positions[i] + edge * .25f; var b = positions[next] - edge * .25f; Spring(i, a, b, a.DistanceTo(b));
                    }
                }
                var initialEnergy = 0f;
                for (var i = 0; i < count; i++)
                {
                    var angle = Mathf.Tau * i / count;
                    var impulse = new Vector2(3 * MathF.Cos(5 * angle), 2 * MathF.Sin(7 * angle));
                    if (i == 0) impulse += new Vector2(20, 8);
                    var offset = new Vector2(2, 3); var torque = offset.Cross(impulse);
                    PhysicsServer.BodyApplyImpulse(Bodies[i], impulse, offset);
                    _momentum += impulse; _angularMomentum += (positions[i] - _initialCenter).Cross(impulse) + torque;
                    initialEnergy += impulse.LengthSquared() / 4 + torque * torque / 10;
                }
                // Four substeps keep h*sqrt(k/m) below .03; allow 10% energy growth from finite-step correction.
                _energyLimit = initialEnergy * 1.1f;
            }
            void Pin(int i, Vector2 anchor)
            {
                var next = (i + 1) % count; _anchorA[i] = anchor - positions[i]; _anchorB[i] = anchor - positions[next];
                PhysicsServer.JointMakePin(Joints[i], anchor, Bodies[i], Bodies[next]);
            }
            void Groove(int i, Vector2 start, Vector2 end, Vector2 anchor)
            {
                var next = (i + 1) % count; _anchorA[i] = start - positions[i]; _guideEnd[i] = end - positions[i]; _anchorB[i] = anchor - positions[next];
                PhysicsServer.JointMakeGroove(Joints[i], start, end, anchor, Bodies[i], Bodies[next]);
            }
            void Spring(int i, Vector2 a, Vector2 b, float rest)
            {
                var next = (i + 1) % count; _anchorA[i] = a - positions[i]; _anchorB[i] = b - positions[next]; _restLength[i] = rest;
                PhysicsServer.JointMakeDampedSpring(Joints[i], a, b, Bodies[i], Bodies[next]);
                PhysicsServer.DampedSpringJointSetRestLength(Joints[i], rest); PhysicsServer.DampedSpringJointSetStiffness(Joints[i], 30); PhysicsServer.DampedSpringJointSetDamping(Joints[i], 5);
            }
            Map = new(World.Space);
            for (var i = 0; i < count; i++) { Map.Bind((ulong)(10 + i), 1, Bodies[i]); Map.Bind((ulong)(110 + i), 1, Joints[i]); }
            Check(World.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && World.PhysicsFallbackReason is null, "Coupled graph retains the explicit backend without fallback");
        }
        internal void Step()
        {
            var count = Bodies.Length;
            Tree.PhysicsFrame(1d / 60); _ticks++;
            var center = Vector2.Zero; var momentum = Vector2.Zero;
            for (var i = 0; i < count; i++)
            {
                _poses[i] = PhysicsServer.BodyGetTransform(Bodies[i]); _velocities[i] = PhysicsServer.BodyGetLinearVelocity(Bodies[i]);
                _angular[i] = PhysicsServer.BodyGetAngularVelocity(Bodies[i]);
                Check(_poses[i].IsFinite() && _velocities[i].IsFinite() && float.IsFinite(_angular[i]), "Coupled state is finite");
                center += _poses[i].Origin / count; momentum += _velocities[i] * 2;
            }
            var angularMomentum = 0f; var energy = 0f;
            for (var i = 0; i < count; i++)
            {
                angularMomentum += (_poses[i].Origin - center).Cross(_velocities[i] * 2) + 5 * _angular[i];
                energy += _velocities[i].LengthSquared() + 2.5f * _angular[i] * _angular[i];
            }
            var pin = 0f; var groove = 0f;
            for (var i = 0; i < count; i++)
            {
                var next = (i + 1) % count; var a = Anchor(i, _anchorA[i]); var b = Anchor(next, _anchorB[i]);
                if (Families[i] == 0) pin = MathF.Max(pin, a.DistanceTo(b));
                else if (Families[i] == 1)
                {
                    var end = Anchor(i, _guideEnd[i]); var along = Math.Clamp((b - a).Dot(end - a) / (end - a).LengthSquared(), 0, 1);
                    groove = MathF.Max(groove, b.DistanceTo(a.Lerp(end, along)));
                }
                else { var extension = a.DistanceTo(b) - _restLength[i]; energy += 15 * extension * extension; }
            }
            var momentumError = momentum.DistanceTo(_momentum); var angularError = MathF.Abs(angularMomentum - _angularMomentum);
            Check(float.IsFinite(pin) && float.IsFinite(groove) && float.IsFinite(momentumError) && float.IsFinite(angularError) && float.IsFinite(energy) && center.IsFinite(),
                "Derived coupled physical invariants remain finite");
            MaxPinError = MathF.Max(MaxPinError, pin); MaxGrooveError = MathF.Max(MaxGrooveError, groove);
            MaxMomentumError = MathF.Max(MaxMomentumError, momentumError); MaxAngularError = MathF.Max(MaxAngularError, angularError); MaxEnergy = MathF.Max(MaxEnergy, energy);
            if (pin >= .5f || groove >= .5f) throw new InvalidOperationException($"Coupled anchor constraint failed at {_ticks}: pin={pin}, groove={groove}");
            if (momentumError >= .03f || angularError >= 2) throw new InvalidOperationException($"Internal joint momentum changed at {_ticks}: linear={momentumError}, angular={angularError}");
            if (energy >= _energyLimit || center.DistanceTo(_initialCenter + _momentum * (_ticks / (120f * count))) >= .05f) throw new InvalidOperationException($"Coupled energy/center drift at {_ticks}: energy={energy}, center={center}");
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
            for (var i = 0; i < Bodies.Length; i++)
                Check(_poses[i].Origin.DistanceTo(poses[i].Origin) < .005f && MathF.Abs(_poses[i].Rotation - poses[i].Rotation) < .002f &&
                    _velocities[i].DistanceTo(velocities[i]) < .005f && MathF.Abs(_angular[i] - angular[i]) < .002f,
                    "Checkpoint replay retains complete coupled pin/groove/spring response");
        }
        private Vector2 Anchor(int body, Vector2 local) => _poses[body] * local;
        public void Dispose()
        {
            if (!_sceneJoints) foreach (var joint in Joints) PhysicsServer.FreeRID(joint);
            for (var i = 1; i < Bodies.Length; i += 2) PhysicsServer.FreeRID(Bodies[i]);
            Tree.Dispose(); World.Dispose(); _shape.Dispose();
        }
    }
}
