using Electron2D;

internal static class PhysicsMovingOneWayTests
{
    internal static void Run(PhysicsServer.Backend backend, bool threaded)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            foreach (var mode in new[] { PhysicsServer.BodyMode.Static, PhysicsServer.BodyMode.Kinematic, PhysicsServer.BodyMode.Rigid, PhysicsServer.BodyMode.RigidLinear })
                foreach (var angle in new[] { 0f, .37f }) Verify(mode, angle, 0);
            for (var family = 1; family < 6; family++) Verify(PhysicsServer.BodyMode.Kinematic, .37f, family);
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Console.WriteLine($"Moving one-way {backend}, threaded={threaded}: role/direction/time, supplied pose, scene motion and zero warm frames passed.");

        void Verify(PhysicsServer.BodyMode mode, float angle, int family)
        {
            using var world = new World(backend); using var root = new SubViewport { World = world };
            using var circle = SourceShape(family); using var rectangle = new RectangleShape { Size = new(200, 20) };
            var scene = new CharacterBody { Name = "Actor", Position = new(5000, 5000) }; scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
            using var tree = new SceneTree(root); var target = PhysicsServer.BodyCreate(); var mover = PhysicsServer.BodyCreate();
            using var query = new PhysicsTestMotionParameters { RecoveryAsCollision = true }; using var result = new PhysicsTestMotionResult(); using var sceneResult = new KinematicCollision();
            try
            {
                PhysicsServer.BodySetMode(target, mode); PhysicsServer.BodyAddShape(target, circle.GetRID(), disabled: true);
                var local = new Transform(.2f, new Vector2(3, 0)); PhysicsServer.BodyAddShape(target, rectangle.GetRID(), local);
                PhysicsServer.BodySetShapeAsOneWayCollision(target, 1, true, 1); PhysicsServer.BodySetTransform(target, new(angle, Vector2.Zero));
                PhysicsServer.BodySetGravityScale(target, 0); PhysicsServer.BodySetCanSleep(target, false);
                PhysicsServer.BodySetLinearDampMode(target, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(target, 0);
                PhysicsServer.BodySetAngularDampMode(target, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(target, 0);
                PhysicsServer.BodySetCollisionMask(target, 0); PhysicsServer.BodySetSpace(target, world.Space);
                PhysicsServer.BodySetMode(mover, PhysicsServer.BodyMode.Kinematic); PhysicsServer.BodyAddShape(mover, circle.GetRID());
                PhysicsServer.BodySetTransform(mover, new(0, new Vector2(6000, 6000))); PhysicsServer.BodySetSpace(mover, world.Space);
                var pass = (PhysicsServer.BodyGetTransform(target) * local).Y.Normalized(); PhysicsServer.BodySetLinearVelocity(target, -pass * 300);
                SetFrom(); Check(!PhysicsServer.BodyTestMotion(mover, query, result), "No completed step means no moving-platform depth allowance");
                tree.PhysicsFrame(.01); CheckCase(mode != PhysicsServer.BodyMode.Static, "Rising platform expands accepted recovery depth");
                PhysicsServer.BodySetLinearVelocity(target, pass * 300); tree.PhysicsFrame(.01); CheckCase(false, "Receding platform does not expand recovery depth");
                PhysicsServer.BodySetLinearVelocity(target, new Vector2(-pass.Y, pass.X) * 300); tree.PhysicsFrame(.01); CheckCase(false, "Tangential velocity does not expand recovery depth");
                PhysicsServer.BodySetLinearVelocity(target, -pass * 300); tree.PhysicsFrame(.001); CheckCase(false, "Allowance follows the actual last step duration");
                tree.PhysicsFrame(.01); CheckCase(mode != PhysicsServer.BodyMode.Static, "Longer last step restores the moving allowance");
                using var history = PhysicsServer.SpaceCreateCheckpoint(world.Space); tree.PhysicsFrame(.001); CheckCase(false, "Shorter future step changes the allowance");
                history.Restore(); CheckCase(mode != PhysicsServer.BodyMode.Static, "Checkpoint restores motion-query timing");
                using var map = new PhysicsSnapshotMap(world.Space); map.Bind(1, 1, target); map.Bind(2, 1, mover); map.Bind(3, 1, scene.GetRID());
                var snapshot = new PhysicsSnapshot(); map.Capture(snapshot); tree.PhysicsFrame(.001);
                map.Apply(snapshot); CheckCase(mode != PhysicsServer.BodyMode.Static, "Portable restore preserves motion-query timing");
                PhysicsServer.BodySetShapeAsOneWayCollision(target, 1, true, 4); PhysicsServer.BodySetLinearVelocity(target, Vector2.Zero);
                tree.PhysicsFrame(.01); CheckCase(true, "Authored larger margin remains effective without motion"); PhysicsServer.BodySetShapeAsOneWayCollision(target, 1, true, 1);
                PhysicsServer.BodySetLinearVelocity(target, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(target, 20);
                tree.PhysicsFrame(.01); CheckCase(false, "Angular-only velocity does not enlarge recovery depth"); PhysicsServer.BodySetAngularVelocity(target, 0);
                var expected = mode != PhysicsServer.BodyMode.Static;
                if (family == 0) Warm(() => { PhysicsServer.BodySetLinearVelocity(target, -pass * 300); tree.PhysicsFrame(.01); CheckCase(expected, "Repeated full-frame recovery"); });
                Near(PhysicsServer.BodyGetTransform(mover).Origin, new(6000, 6000), .0001f, "Supplied-pose query preserves live body pose");
                Near(scene.Position, new(5000, 5000), .0001f, "Scene test-only motion preserves authored pose");
                if (family == 0) Console.WriteLine($"Moving one-way {mode}/{angle}: 128 complete frames 0/0 B.");

                void SetFrom()
                {
                    var pose = PhysicsServer.BodyGetTransform(target) * local;
                    query.From = new(pose.Rotation, pose.Origin - pose.Y.Normalized() * 12);
                }
                void CheckCase(bool expectedHit, string label)
                {
                    SetFrom(); var hit = PhysicsServer.BodyTestMotion(mover, query, result);
                    if (hit != expectedHit) throw new InvalidOperationException($"{label}: family={family}, mode={mode}, angle={angle}, velocity={PhysicsServer.BodyGetLinearVelocity(target)}, pose={PhysicsServer.BodyGetTransform(target)}, query={query.From}, travel={result.GetTravel()}");
                    Check(scene.TestMove(query.From, Vector2.Zero, sceneResult, recoveryAsCollision: true) == expectedHit, "Scene and raw motion share one-way timing");
                    if (!hit) { Check(result.GetColliderRID() == default && result.GetTravel() == Vector2.Zero, "Denied recovery clears collision and travel"); return; }
                    Check(result.GetColliderRID() == target && result.GetColliderShape() == 1 && result.GetCollisionLocalShape() == 0, "Moving recovery preserves logical slots and identities");
                    var outward = -(PhysicsServer.BodyGetTransform(target) * local).Y.Normalized();
                    Check(result.GetTravel().Dot(outward) > 2.5f && result.GetCollisionNormal().Dot(outward) > .99f, "Recovery separates towards the platform's solid side");
                    Near(result.GetTravel(), sceneResult.GetTravel(), .003f, "Both public callers publish the same supplied-pose recovery");
                }
            }
            finally { PhysicsServer.FreeRID(mover); PhysicsServer.FreeRID(target); }
        }
    }
    private static Shape SourceShape(int family) => family switch
    {
        0 => new CircleShape { Radius = 5 },
        1 => new RectangleShape { Size = new(10, 10) },
        2 => new CapsuleShape { Radius = 5, Height = 10 },
        3 => new SegmentShape { A = new(-5, 5), B = new(5, 5) },
        4 => new ConvexPolygonShape { Points = [new(-5, -5), new(5, -5), new(5, 5), new(-5, 5)] },
        _ => new ConcavePolygonShape { Segments = [new(-5, 5), new(5, 5)] }
    };
    private static void Near(Vector2 actual, Vector2 expected, float tolerance, string label)
    { if (actual.DistanceTo(expected) > tolerance) throw new InvalidOperationException($"{label}: {actual} expected {expected}"); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Warm(Action frame)
    {
        frame(); for (var i = 0; i < 128; i++) frame(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) frame();
        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        if (own != 0 || all != 0) throw new InvalidOperationException($"Moving one-way frames allocated {own}/{all} B.");
    }
}
