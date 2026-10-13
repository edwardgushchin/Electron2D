using Electron2D;

public static partial class BackendChecks
{
    public static void RunMassRoles(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (backend is null) PhysicsServerManager.RegisterServer("MassRoles", () => new Solver());
            using var world = backend is { } selected ? new World(selected) : new World("MassRoles");
            using var shape = new CircleShape { Radius = 1 }; using var root = new SubViewport { World = world };
            var scene = new RigidBody
            {
                GravityScale = 0,
                CanSleep = false,
                CollisionMask = 0,
                LinearDamp = 0,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDamp = 0,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            scene.AddChild(new CollisionShape { Name = "Right", Shape = shape, Position = new(8, 0) });
            scene.AddChild(new CollisionShape { Name = "Left", Shape = shape, Position = new(-2, 0) }); root.AddChild(scene);
            using var tree = new SceneTree(root); var raw = PhysicsServer.BodyCreate(); PhysicsDirectBodyState? rawState = null;
            try
            {
                PhysicsServer.BodyAddShape(raw, shape.GetRID(), new(0, new Vector2(8, 0)));
                PhysicsServer.BodyAddShape(raw, shape.GetRID(), new(0, new Vector2(-2, 0)));
                PhysicsServer.BodySetCollisionMask(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
                PhysicsServer.BodySetGravityScale(raw, 0);
                PhysicsServer.BodySetLinearDampMode(raw, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(raw, 0);
                PhysicsServer.BodySetAngularDampMode(raw, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(raw, 0);
                PhysicsServer.BodySetSpace(raw, world.Space);
                var sceneRID = scene.GetRID();
                using var sceneState = PhysicsServer.BodyGetDirectState(sceneRID)!;
                rawState = PhysicsServer.BodyGetDirectState(raw)!;
                using var direct = world.DirectSpaceState; using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[2];
                Check(PhysicsServer.SpaceGetBackend(world.Space) == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
                    "Mass roles use the requested selected implementation without fallback");
                var modes = new[] { PhysicsServer.BodyMode.Rigid, PhysicsServer.BodyMode.Static, PhysicsServer.BodyMode.Kinematic, PhysicsServer.BodyMode.RigidLinear };
                Cycle();
                foreach (var id in new[] { sceneRID, raw })
                {
                    PhysicsServer.BodySetCenterOfMass(id, new(1, -2));
                    foreach (var mode in modes) { Configure(id, mode); Verify(id, mode, new(1, -2)); }
                    PhysicsServer.BodyResetMassProperties(id);
                }
                PhysicsServer.BodySetSpace(raw, default);
                rawState.Dispose();
                Check(PhysicsServer.BodyGetCenterOfMass(raw).DistanceTo(new(3, 0)) < .002f, "Detached raw configuration retains the resolved automatic center");
                PhysicsServer.BodySetSpace(raw, world.Space); rawState = PhysicsServer.BodyGetDirectState(raw)!; Cycle();
                Collect(); for (var i = 0; i < 128; i++) Cycle();
                var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Cycle();
                var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
                Check(ownerBytes == 0 && allBytes == 0,
                    $"Warmed complete frames with all role transitions and retained mass reads allocate zero owner/all-thread bytes: {ownerBytes}/{allBytes}");
                Console.WriteLine($"Mass roles {backend?.ToString() ?? "registered"}, threaded={threaded}: automatic/custom centers, origin point velocity, inverse values, reentry and 0/0 warm cycles passed.");

                void Cycle()
                {
                    foreach (var mode in modes)
                    {
                        Configure(sceneRID, mode); Configure(raw, mode);
                        Verify(sceneRID, mode, new(3, 0)); Verify(raw, mode, new(3, 0));
                        tree.PhysicsFrame(.01);
                        Verify(sceneRID, mode, new(3, 0)); Verify(raw, mode, new(3, 0));
                    }
                }
                void Configure(RID id, PhysicsServer.BodyMode mode)
                {
                    PhysicsServer.BodySetMode(id, mode);
                    PhysicsServer.BodySetTransform(id, new(.7f, new Vector2(id == raw ? 200 : 100, 50)));
                    PhysicsServer.BodySetLinearVelocity(id, new(2, -3)); PhysicsServer.BodySetAngularVelocity(id, 4);
                }
                void Verify(RID id, PhysicsServer.BodyMode mode, Vector2 center)
                {
                    var state = id == raw ? rawState! : sceneState;
                    var rotated = center.Rotated(state.Transform.Rotation);
                    if (!(state.CenterOfMassLocal.DistanceTo(center) < .002f && state.CenterOfMass.DistanceTo(rotated) < .002f &&
                        PhysicsServer.BodyGetCenterOfMass(id).DistanceTo(center) < .002f))
                        throw new InvalidOperationException($"Resolved center survives scene/raw {mode} while inverse dynamics change: {state.CenterOfMassLocal}/{center}");
                    var dynamic = mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
                    Check(MathF.Abs(state.InverseMass - (dynamic ? 1 / PhysicsServer.BodyGetMass(id) : 0)) < .0001f &&
                        (mode == PhysicsServer.BodyMode.Rigid ? state.InverseInertia > 0 : state.InverseInertia == 0),
                        "Inverse mass/inertia follow the actual role without erasing resolved profile metadata");
                    var offset = new Vector2(5, -2); var arm = offset - rotated;
                    var expected = state.LinearVelocity + new Vector2(-arm.Y, arm.X) * state.AngularVelocity;
                    if (!(state.GetVelocityAtLocalPosition(offset).DistanceTo(expected) < .002f))
                        throw new InvalidOperationException($"Point velocity projects the retained global-axis center on {mode}");
                    for (var index = 0; index < 2; index++)
                    {
                        point.Position = state.Transform * new Vector2(index == 0 ? 8 : -2, 0);
                        Check(direct.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == id && hits[0].ShapeIndex == index,
                            "Both logical fixtures retain real query geometry and identity across role changes");
                    }
                }
            }
            finally { rawState?.Dispose(); PhysicsServer.FreeRID(raw); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
