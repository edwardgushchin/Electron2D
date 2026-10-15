using Electron2D;

public static partial class BackendChecks
{
    public static void RunJointContacts(PhysicsServer.Backend? backend = PhysicsServer.Backend.CPU, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (backend is null) PhysicsServerManager.RegisterServer("JointContacts", static () => new Solver());
            using (var absentFloor = new JointContactRig(backend, false, false))
            {
                absentFloor.DisableFloor(); for (var i = 0; i < 300; i++) absentFloor.Step();
                Reject<InvalidOperationException>(absentFloor.Verify);
            }
            using (var absentElastic = new JointContactRig(backend, false, false))
            {
                PhysicsServer.DampedSpringJointSetStiffness(absentElastic.Joints[2], 0);
                for (var i = 0; i < 300; i++) absentElastic.Step();
                Reject<InvalidOperationException>(absentElastic.Verify);
            }
            Console.WriteLine($"Joint/contact {backend?.ToString() ?? "registered"}: absent-floor and absent-elastic negative controls rejected.");
            foreach (var scenePeer in new[] { false, true })
                foreach (var sceneJoints in new[] { false, true })
                {
                    using var rig = new JointContactRig(backend, scenePeer, sceneJoints);
                    for (var i = 0; i < 300; i++) rig.Step();
                    rig.Verify(); Check(!scenePeer || rig.Entries == 1, "Real scene floor contact starts exactly one monitored episode");
                    for (var i = 0; i < 128; i++) { rig.Step(); rig.Verify(); }
                    Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                    for (var i = 0; i < 128; i++) { rig.Step(); rig.Verify(); }
                    owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                    Check(owner == 0 && all == 0, $"Joint/contact full frames allocate {owner}/{all} B");
                    using (var checkpoint = PhysicsServer.SpaceCreateCheckpoint(rig.World.Space))
                    {
                        var entries = rig.Entries;
                        for (var i = 0; i < 32; i++) { rig.Step(); rig.Verify(); }
                        var pose = PhysicsServer.BodyGetTransform(rig.Peer); var velocity = PhysicsServer.BodyGetLinearVelocity(rig.Peer);
                        checkpoint.Restore();
                        for (var i = 0; i < 32; i++) { rig.Step(); rig.Verify(); }
                        Check(PhysicsServer.BodyGetTransform(rig.Peer).Origin.DistanceTo(pose.Origin) < .005f &&
                            PhysicsServer.BodyGetLinearVelocity(rig.Peer).DistanceTo(velocity) < .005f && rig.Entries == entries,
                            "Joint/contact checkpoint replay preserves solved support and existing contact episodes");
                    }
                    foreach (var joint in rig.Joints) PhysicsServer.JointClear(joint);
                    PhysicsServer.BodySetLinearVelocity(rig.Peer, new(30, 0));
                    for (var i = 0; i < 60; i++) rig.Step();
                    Check(PhysicsServer.BodyGetTransform(rig.Peer).Origin.X > 20 && rig.HasFloorContact(),
                        "Cleared joint rows release motion while physical floor contact continues");
                    Console.WriteLine($"Joint/contact {backend?.ToString() ?? "registered"}, peerScene={scenePeer}, jointsScene={sceneJoints}, threaded={threaded}: support/friction impulse errors {rig.MaxNormalError:F6}/{rig.MaxFrictionError:F6}, replay/release, 128 complete measured frames {owner}/{all} B.");
                    using var continuous = new JointContactRig(backend, scenePeer, sceneJoints);
                    continuous.VerifyContinuousSleep();
                }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private sealed class JointContactRig : IDisposable
    {
        internal readonly World World;
        internal readonly SceneTree Tree;
        internal readonly RID Load, Peer;
        internal readonly RID[] Joints = new RID[3];
        private readonly CircleShape _circle = new() { Radius = 2 };
        private readonly RectangleShape _rectangle = new() { Size = new(200, 2) };
        private readonly PhysicsMaterial _material = new() { Friction = .6f };
        private readonly StaticBody _floor;
        private readonly RID _raw;
        private readonly bool _sceneJoints;
        private readonly PhysicsDirectBodyState _state;
        internal int Entries;
        internal float MaxNormalError, MaxFrictionError;
        private float _momentumBefore, _loadSpeedBefore;

        internal JointContactRig(PhysicsServer.Backend? backend, bool scenePeer, bool sceneJoints)
        {
            _sceneJoints = sceneJoints; World = backend is { } selected ? new(selected) : new("JointContacts");
            var root = new SubViewport { World = World }; var anchor = new StaticBody { Name = "Anchor" }; root.AddChild(anchor);
            _floor = new StaticBody { Name = "Floor", Position = new(0, 30), ConstantLinearVelocity = new(30, 0), PhysicsMaterialOverride = _material };
            _floor.AddChild(new CollisionShape { Shape = _rectangle }); root.AddChild(_floor);
            var scene = new RigidBody
            {
                Name = "Scene",
                Position = new(0, scenePeer ? 14 : 10),
                LockRotation = true,
                CanSleep = false,
                MaxContactsReported = 4,
                ContactMonitor = true,
                PhysicsMaterialOverride = _material,
                LinearDamp = 0,
                AngularDamp = 0,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            scene.BodyEntered += body => { Check(ReferenceEquals(body, _floor), "Contact monitor samples the real floor owner"); Entries++; };
            scene.AddChild(new CollisionShape { Shape = _circle }); root.AddChild(scene);
            if (sceneJoints)
            {
                Joint[] nodes = [new PinJoint(), new GrooveJoint(), new DampedSpringJoint()];
                for (var i = 0; i < 3; i++) { root.AddChild(nodes[i]); Joints[i] = nodes[i].GetRID(); }
            }
            Tree = new(root);
            PhysicsServer.AreaSetGravity(World.Space, 980); PhysicsServer.AreaSetLinearDamp(World.Space, 0); PhysicsServer.AreaSetAngularDamp(World.Space, 0);
            PhysicsServer.SpaceSetContactMaxAllowedPenetration(World.Space, .01f);
            _raw = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(_raw, PhysicsServer.BodyMode.RigidLinear);
            PhysicsServer.BodyAddShape(_raw, _circle.GetRID()); PhysicsServer.BodySetCanSleep(_raw, false); PhysicsServer.BodySetMaxContactsReported(_raw, 4);
            PhysicsServer.BodySetFriction(_raw, .6f); PhysicsServer.BodySetLinearDampMode(_raw, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(_raw, 0);
            PhysicsServer.BodySetTransform(_raw, new(0, new Vector2(0, scenePeer ? 10 : 14))); PhysicsServer.BodySetSpace(_raw, World.Space);
            Load = scenePeer ? _raw : scene.GetRID(); Peer = scenePeer ? scene.GetRID() : _raw;
            if (!sceneJoints) for (var i = 0; i < 3; i++) Joints[i] = PhysicsServer.JointCreate();
            PhysicsServer.JointMakePin(Joints[0], new(0, 12), Load, Peer);
            PhysicsServer.JointMakeGroove(Joints[1], Vector2.Zero, new(0, 100), new(0, 10), anchor.GetRID(), Load);
            PhysicsServer.JointMakeDampedSpring(Joints[2], Vector2.Zero, new(0, 10), anchor.GetRID(), Load);
            PhysicsServer.DampedSpringJointSetRestLength(Joints[2], 50); PhysicsServer.DampedSpringJointSetStiffness(Joints[2], 20); PhysicsServer.DampedSpringJointSetDamping(Joints[2], 4);
            _state = PhysicsServer.BodyGetDirectState(Peer)!;
            Check(World.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && World.PhysicsFallbackReason is null, "Joint/contact uses the explicit backend without fallback");
            Check(PhysicsServer.BodyGetMass(Load) == 1 && PhysicsServer.BodyGetMass(Peer) == 1 &&
                PhysicsServer.BodyGetGravityScale(Load) == 1 && PhysicsServer.BodyGetGravityScale(Peer) == 1,
                "Analytic support uses both actual unit masses and unscaled gravity");
        }
        internal void VerifyContinuousSleep()
        {
            _floor.ConstantLinearVelocity = Vector2.Zero;
            var bodies = new[] { Load, Peer };
            foreach (var body in bodies) PhysicsServer.BodySetCanSleep(body, true);
            PhysicsServer.SpaceSetBodyLinearVelocitySleepThreshold(World.Space, 1);
            PhysicsServer.SpaceSetBodyAngularVelocitySleepThreshold(World.Space, .1f);
            PhysicsServer.SpaceSetBodyTimeToSleep(World.Space, .05f);
            foreach (var mode in new[] { CCDMode.CastRay, CCDMode.CastShape })
            {
                foreach (var body in bodies) PhysicsServer.BodySetContinuousCollisionDetectionMode(body, mode);
                for (var i = 0; i < 300; i++) Step();
                if (!PhysicsServer.BodyGetSleeping(Load) || !PhysicsServer.BodyGetSleeping(Peer)) throw new InvalidOperationException($"Contact/joint sleep failed in {mode}: velocities={PhysicsServer.BodyGetLinearVelocity(Load)}/{PhysicsServer.BodyGetLinearVelocity(Peer)}, awake={!PhysicsServer.BodyGetSleeping(Load)}/{!PhysicsServer.BodyGetSleeping(Peer)}");
                for (var i = 0; i < 16; i++)
                {
                    Step(); Check(PhysicsServer.BodyGetSleeping(Load) && PhysicsServer.BodyGetSleeping(Peer), "Steady spring tension preserves the sleeping contact island");
                }
                var restLength = PhysicsServer.DampedSpringJointGetRestLength(Joints[2]);
                PhysicsServer.DampedSpringJointSetRestLength(Joints[2], restLength);
                PhysicsServer.DampedSpringJointSetStiffness(Joints[2], PhysicsServer.DampedSpringJointGetStiffness(Joints[2]));
                PhysicsServer.DampedSpringJointSetDamping(Joints[2], PhysicsServer.DampedSpringJointGetDamping(Joints[2])); Step();
                Check(PhysicsServer.BodyGetSleeping(Load) && PhysicsServer.BodyGetSleeping(Peer), "Equal spring coefficient writes retain the sleeping island");
                PhysicsServer.DampedSpringJointSetRestLength(Joints[2], restLength + 1); Step();
                Check(!PhysicsServer.BodyGetSleeping(Load) && !PhysicsServer.BodyGetSleeping(Peer), "An authored spring edit wakes the connected island");
                PhysicsServer.DampedSpringJointSetRestLength(Joints[2], restLength);
                for (var i = 0; i < 300; i++) Step();
                Check(PhysicsServer.BodyGetSleeping(Load) && PhysicsServer.BodyGetSleeping(Peer), "Restored spring load reaches automatic sleep again");
                var anchor = Tree.Root.GetNode<StaticBody>("Anchor"); var anchorPosition = anchor.Position;
                anchor.Position += Vector2.Right; Step();
                Check(!PhysicsServer.BodyGetSleeping(Load) && !PhysicsServer.BodyGetSleeping(Peer), "Moving a static joint anchor wakes the connected island");
                anchor.Position = anchorPosition;
                for (var i = 0; i < 300; i++) Step();
                Check(PhysicsServer.BodyGetSleeping(Load) && PhysicsServer.BodyGetSleeping(Peer), "The restored anchor permits later quiet sleep");
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(World.Space);
                for (var i = 0; i < 128; i++) Impact();
                Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Impact();
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0, $"Sleep/CCD/coupled-contact replay allocated {owner}/{all} B");
                Console.WriteLine($"Coupled {mode} sleep/impact replay: 128 restored high-speed full frames {owner}/{all} B.");
                void Impact()
                {
                    checkpoint.Restore();
                    Check(PhysicsServer.BodyGetSleeping(Load) && PhysicsServer.BodyGetSleeping(Peer), "Private replay restores the sleeping coupled island");
                    foreach (var body in bodies)
                    {
                        var pose = PhysicsServer.BodyGetTransform(body);
                        PhysicsServer.BodySetTransform(body, new(pose.Rotation, pose.Origin + Vector2.Up * 10));
                    }
                    PhysicsServer.BodyApplyCentralImpulse(Peer, new(0, 12000)); Step();
                    Check(!PhysicsServer.BodyGetSleeping(Load) && !PhysicsServer.BodyGetSleeping(Peer) &&
                        PhysicsServer.BodyGetTransform(Load).Origin.Y < 31 && PhysicsServer.BodyGetTransform(Peer).Origin.Y < 31 && HasFloorContact(),
                        "A fast joint-connected waking island cannot cross the thin floor and publishes the actual floor contact");
                }
            }
        }
        internal void DisableFloor() { _floor.CollisionLayer = 0; _floor.CollisionMask = 0; }
        internal void Step()
        {
            _loadSpeedBefore = PhysicsServer.BodyGetLinearVelocity(Load).Y;
            _momentumBefore = _loadSpeedBefore + PhysicsServer.BodyGetLinearVelocity(Peer).Y;
            Tree.PhysicsFrame(1d / 60);
        }
        internal bool HasFloorContact()
        {
            for (var i = 0; i < _state.GetContactCount(); i++) if (_state.GetContactCollider(i) == _floor.GetRID()) return true;
            return false;
        }
        internal void Verify()
        {
            var load = PhysicsServer.BodyGetTransform(Load).Origin; var peer = PhysicsServer.BodyGetTransform(Peer).Origin;
            if (!(load.IsFinite() && peer.IsFinite() && MathF.Abs(load.X) < .15f && MathF.Abs(peer.X) < .15f &&
                MathF.Abs(load.Y - 23) < .15f && MathF.Abs(peer.Y - 27) < .15f && MathF.Abs(peer.Y - load.Y - 4) < .02f))
                throw new InvalidOperationException($"Pin/groove must retain the spring-loaded floor column: load={load}, peer={peer}, velocities={PhysicsServer.BodyGetLinearVelocity(Load)}/{PhysicsServer.BodyGetLinearVelocity(Peer)}, rotations={PhysicsServer.BodyGetTransform(Load).Rotation}/{PhysicsServer.BodyGetTransform(Peer).Rotation}, modes={PhysicsServer.BodyGetMode(Load)}/{PhysicsServer.BodyGetMode(Peer)}");
            Check(PhysicsServer.BodyGetLinearVelocity(Load).Length() < .1f && PhysicsServer.BodyGetLinearVelocity(Peer).Length() < .1f &&
                _floor.Position == new Vector2(0, 30), "Constrained support settles while virtual surface motion leaves the floor pose fixed");
            var impulse = Vector2.Zero; var count = _state.GetContactCount();
            Check(count > 0, "Actual floor contact is reported");
            for (var i = 0; i < count; i++)
            {
                Check(_state.GetContactCollider(i) == _floor.GetRID() && _state.GetContactColliderID(i) == _floor.InstanceID &&
                    ReferenceEquals(_state.GetContactColliderObject<StaticBody>(i), _floor) && _state.GetContactColliderShape(i) == 0 && _state.GetContactLocalShape(i) == 0,
                    "Complete contact identity retains real scene floor and logical slots");
                Check(_state.GetContactLocalNormal(i).DistanceTo(Vector2.Up) < .001f, "Contact reports the support normal for the constrained body");
                Check(_state.GetContactColliderVelocityAtPosition(i).DistanceTo(new(30, 0)) < .001f, "Contact data observes the stationary floor's virtual surface velocity");
                impulse += _state.GetContactImpulse(i);
            }
            var elastic = 20 * (50 - load.Y);
            var momentumChange = PhysicsServer.BodyGetLinearVelocity(Load).Y + PhysicsServer.BodyGetLinearVelocity(Peer).Y - _momentumBefore;
            var expected = (1960 + elastic) / 60 - momentumChange;
            var dampingLossBound = (elastic / 60 + MathF.Abs(_loadSpeedBefore) + 980f / 60) * (1 - MathF.Exp(-4f / 60));
            var deficit = expected + impulse.Y;
            var normalError = MathF.Abs(deficit); var frictionError = MathF.Abs(impulse.X / -impulse.Y - .6f);
            if (!impulse.IsFinite() || impulse.Y >= -1 || impulse.X <= 1 || deficit < -.15f || deficit > dampingLossBound + .15f || frictionError >= .08f)
                throw new InvalidOperationException($"Coupled support impulse={impulse}, expectedNormal={expected}, dampingBound={dampingLossBound}, errors={normalError}/{frictionError}, load={load}, peer={peer}");
            MaxNormalError = MathF.Max(MaxNormalError, normalError); MaxFrictionError = MathF.Max(MaxFrictionError, frictionError);
        }
        public void Dispose()
        {
            if (!_sceneJoints) foreach (var joint in Joints) PhysicsServer.FreeRID(joint);
            PhysicsServer.FreeRID(_raw); Tree.Dispose(); World.Dispose(); _circle.Dispose(); _rectangle.Dispose(); _material.Dispose();
        }
    }
}
