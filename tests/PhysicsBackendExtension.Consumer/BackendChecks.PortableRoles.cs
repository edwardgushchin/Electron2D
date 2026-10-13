using Electron2D;

public static partial class BackendChecks
{
    public static void RunPortableRoles(bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            PhysicsServerManager.RegisterServer("PortableRoles", () => new Solver());
            using var cpu = new PortableRoleRig(new World(PhysicsServer.Backend.CPU), false);
            using var registered = new PortableRoleRig(new World("PortableRoles"), true);
            Check(cpu.World.PhysicsBackend == PhysicsServer.Backend.CPU && registered.World.PhysicsBackend == PhysicsServer.Backend.Extension,
                "Portable roles use independent selected numerical owners");
            var sent = new PhysicsSnapshot(65536); var received = new PhysicsSnapshot(65536); var packet = new byte[65536];
            var modes = new[] { PhysicsServer.BodyMode.Rigid, PhysicsServer.BodyMode.Static, PhysicsServer.BodyMode.Kinematic, PhysicsServer.BodyMode.RigidLinear };
            Cycle();
            cpu.Configure(PhysicsServer.BodyMode.Rigid, false, false); registered.Configure(PhysicsServer.BodyMode.Rigid, false, true);
            cpu.Map.Capture(sent); var size = sent.WriteTo(packet); received.ReadFrom(packet.AsSpan(0, size));
            PhysicsServer.BodySetMass(registered.Bodies[0], 3);
            var before = PhysicsServer.BodyGetTransform(registered.Bodies[0]); var tick = PhysicsServer.SpaceGetTick(registered.World.Space);
            Reject<System.IO.InvalidDataException>(() => registered.Map.Apply(received));
            Check(PhysicsServer.BodyGetTransform(registered.Bodies[0]) == before && PhysicsServer.SpaceGetTick(registered.World.Space) == tick,
                "Incompatible authoring rejects portable correction without changing pose or world tick");
            PhysicsServer.BodySetMass(registered.Bodies[0], 2); registered.Map.Apply(received); Compare(cpu, registered, new(3, 0));
            Collect(); for (var i = 0; i < 128; i++) Cycle();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Cycle();
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownerBytes == 0 && allBytes == 0, $"Portable role cycles allocate zero owner/all-thread bytes: {ownerBytes}/{allBytes}");
            Console.WriteLine($"Portable roles CPU/registered, threaded={threaded}: four scene/raw roles, automatic/custom centers, correction/continuation, queries, validation and 0/0 warm cycles passed.");

            void Cycle()
            {
                for (var custom = 0; custom < 2; custom++)
                    foreach (var mode in modes)
                    {
                        var center = custom == 0 ? new Vector2(3, 0) : new Vector2(1, -2);
                        cpu.Configure(mode, custom != 0, false); registered.Configure(mode, custom != 0, true);
                        cpu.Tree.PhysicsFrame(.01);
                        var dynamic = mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
                        var linear = cpu.States[0].LinearVelocity; var angular = cpu.States[0].AngularVelocity;
                        var inverseInertia = cpu.States[0].InverseInertia;
                        if (dynamic)
                            foreach (var id in cpu.Bodies)
                            { PhysicsServer.BodyApplyCentralForce(id, new(4, -2)); PhysicsServer.BodyApplyTorque(id, 6); }
                        Transfer(cpu, registered); Compare(cpu, registered, center);
                        cpu.Tree.PhysicsFrame(.01); registered.Tree.PhysicsFrame(.01); Compare(cpu, registered, center);
                        if (dynamic)
                            foreach (var state in cpu.States)
                                Check(state.LinearVelocity.DistanceTo(linear + new Vector2(.02f, -.01f)) < .0001f &&
                                    MathF.Abs(state.AngularVelocity - angular - 6 * inverseInertia * .01f) < .0001f,
                                    "A pending portable force/torque changes actual motion by exactly one fixed interval");
                        Transfer(registered, cpu); Compare(cpu, registered, center);
                        linear = cpu.States[0].LinearVelocity; angular = cpu.States[0].AngularVelocity;
                        cpu.Tree.PhysicsFrame(.01); registered.Tree.PhysicsFrame(.01); Compare(cpu, registered, center);
                        if (dynamic)
                            foreach (var state in cpu.States)
                                Check(state.LinearVelocity.DistanceTo(linear) < .0001f && MathF.Abs(state.AngularVelocity - angular) < .0001f,
                                    "Reverse portable correction does not revive an already consumed force/torque interval");
                    }
            }
            void Transfer(PortableRoleRig source, PortableRoleRig target)
            {
                source.Map.Capture(sent); var bytes = sent.WriteTo(packet); received.ReadFrom(packet.AsSpan(0, bytes)); target.Map.Apply(received);
                Check(PhysicsServer.SpaceGetTick(source.World.Space) == PhysicsServer.SpaceGetTick(target.World.Space),
                    "Portable correction applies the authoritative world tick across numerical owners");
            }
            static void Compare(PortableRoleRig first, PortableRoleRig second, Vector2 center)
            {
                for (var i = 0; i < 2; i++)
                {
                    var a = first.States[i]; var b = second.States[i];
                    Check(a.Transform.Origin.DistanceTo(b.Transform.Origin) < .002f && MathF.Abs(Mathf.AngleDifference(a.Transform.Rotation, b.Transform.Rotation)) < .002f &&
                        a.LinearVelocity.DistanceTo(b.LinearVelocity) < .002f && MathF.Abs(a.AngularVelocity - b.AngularVelocity) < .002f,
                        "Correction and free continuation preserve scene/raw pose and motion across independent numerical owners");
                    Check(MathF.Abs(a.InverseMass - b.InverseMass) < .0001f && MathF.Abs(a.InverseInertia - b.InverseInertia) < .0001f,
                        "Portable correction preserves the role's actual inverse dynamics");
                    first.Verify(i, center); second.Verify(i, center);
                }
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private sealed class PortableRoleRig : IDisposable
    {
        internal readonly World World;
        internal readonly SceneTree Tree;
        internal readonly PhysicsSnapshotMap Map;
        internal readonly RID[] Bodies;
        internal readonly PhysicsDirectBodyState[] States;
        private readonly CircleShape _circle = new() { Radius = 1 };
        private readonly PhysicsDirectSpaceState _direct;
        private readonly PhysicsPointQueryParameters _point = new();
        private readonly PhysicsPointResult[] _hits = new PhysicsPointResult[2];
        private RID _raw;
        internal PortableRoleRig(World world, bool reverse)
        {
            World = world; var root = new SubViewport { World = world }; Tree = new(root);
            if (reverse) AddRaw();
            var scene = new RigidBody { Name = "Scene" };
            scene.AddChild(new CollisionShape { Name = "Right", Shape = _circle, Position = new(8, 0) });
            scene.AddChild(new CollisionShape { Name = "Left", Shape = _circle, Position = new(-2, 0) }); root.AddChild(scene);
            if (!reverse) AddRaw();
            Bodies = [scene.GetRID(), _raw]; States = [PhysicsServer.BodyGetDirectState(Bodies[0])!, PhysicsServer.BodyGetDirectState(_raw)!];
            Map = new(World.Space); Map.Bind(10, 1, Bodies[0]); Map.Bind(20, 1, _raw); _direct = world.DirectSpaceState;
            foreach (var id in Bodies)
            {
                PhysicsServer.BodySetCollisionMask(id, 0); PhysicsServer.BodySetCanSleep(id, false); PhysicsServer.BodySetMass(id, 2); PhysicsServer.BodySetGravityScale(id, 0);
                PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(id, 0);
                PhysicsServer.BodySetAngularDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(id, 0);
            }
        }
        private void AddRaw()
        {
            _raw = PhysicsServer.BodyCreate(); PhysicsServer.BodyAddShape(_raw, _circle.GetRID(), new(0, new Vector2(8, 0)));
            PhysicsServer.BodyAddShape(_raw, _circle.GetRID(), new(0, new Vector2(-2, 0))); PhysicsServer.BodySetSpace(_raw, World.Space);
        }
        internal void Configure(PhysicsServer.BodyMode mode, bool custom, bool drift)
        {
            for (var i = 0; i < Bodies.Length; i++)
            {
                var id = Bodies[i]; PhysicsServer.BodySetMode(id, mode);
                if (custom) PhysicsServer.BodySetCenterOfMass(id, new(1, -2)); else PhysicsServer.BodyResetMassProperties(id);
                PhysicsServer.BodySetTransform(id, new(drift ? -.4f : .7f, new Vector2(i * 100 + (drift ? 50 : 0), 20)));
                PhysicsServer.BodySetLinearVelocity(id, drift ? new(-7, 6) : new(2, -3)); PhysicsServer.BodySetAngularVelocity(id, drift ? -2 : 4);
            }
        }
        internal void Verify(int body, Vector2 center)
        {
            var state = States[body]; var rotated = center.Rotated(state.Transform.Rotation); var offset = new Vector2(5, -2); var arm = offset - rotated;
            var mode = PhysicsServer.BodyGetMode(Bodies[body]); var dynamic = mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
            Check(MathF.Abs(state.InverseMass - (dynamic ? .5f : 0)) < .0001f &&
                (mode == PhysicsServer.BodyMode.Rigid ? state.InverseInertia > 0 : state.InverseInertia == 0),
                "Corrected inverse dynamics follow the selected role while configured mass remains two kilograms");
            Check(state.CenterOfMassLocal.DistanceTo(center) < .002f && state.CenterOfMass.DistanceTo(rotated) < .002f,
                "Portable role correction retains resolved automatic/custom center metadata");
            Check(state.GetVelocityAtLocalPosition(offset).DistanceTo(state.LinearVelocity + new Vector2(-arm.Y, arm.X) * state.AngularVelocity) < .002f,
                "Corrected point velocity uses the resolved origin-relative center");
            for (var index = 0; index < 2; index++)
            {
                _point.Position = state.Transform * new Vector2(index == 0 ? 8 : -2, 0);
                Check(_direct.IntersectPoint(_point, _hits) == 1 && _hits[0].ColliderRID == Bodies[body] && _hits[0].ShapeIndex == index,
                    "Correction preserves actual compound query geometry and local logical identity");
            }
        }
        public void Dispose()
        {
            Map.Dispose(); foreach (var state in States) state.Dispose(); _direct.Dispose(); _point.Dispose();
            PhysicsServer.FreeRID(_raw); Tree.Dispose(); World.Dispose(); _circle.Dispose();
        }
    }
}
