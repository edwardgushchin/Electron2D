using Electron2D;

public static partial class BackendChecks
{
    public static void RunBodyContract(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("BodyContract", () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("BodyContract");
            using var root = new SubViewport { World = world };
            using var parent = new Entity { Position = new(31, -17), Rotation = .37f };
            root.AddChild(parent);
            PhysicsBody[] bodies = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
            using var circle = new CircleShape { Radius = 1 };
            using var localOwner = new Resource();
            using var peerOwner = new Resource();
            var peers = new StaticBody[bodies.Length];
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i];
                Check(!body.InputPickable && body.GetGravity() == Vector2.Zero && body.GetCollisionExceptions().Length == 0,
                    "Every detached PhysicsBody role has the declared base defaults");
                var property = body.GetPropertyList().OfType<PropertyDescriptor<CollisionObject, bool>>().Single(p => p.Name == nameof(CollisionObject.InputPickable));
                body.InputPickable = true;
                body.RevertProperty(property);
                Check(!body.InputPickable, "The inherited stored descriptor restores the body-specific picking default");
                var owner = body.CreateShapeOwner(localOwner);
                body.ShapeOwnerAddShape(owner, circle);
                parent.AddChild(body);
                body.GlobalTransform = Start(i);
                var peer = peers[i] = new StaticBody { Position = Start(i).Origin + new Vector2(6, 0) };
                owner = peer.CreateShapeOwner(peerOwner);
                peer.ShapeOwnerAddShape(owner, circle);
                root.AddChild(peer);
            }
            using var tree = new SceneTree(root);
            Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
                "The complete base contract uses its explicitly requested implementation");
            foreach (var body in bodies) PhysicsServer.BodySetMode(body.GetRID(), PhysicsServer.BodyMode.Static);
            PhysicsServer.AreaSetGravity(world.Space, 10);
            PhysicsServer.AreaSetGravityVector(world.Space, new(.6f, .8f));
            tree.PhysicsFrame(.01);
            CheckGravity(new(6, 8));
            PhysicsServer.AreaSetGravity(world.Space, 20);
            PhysicsServer.AreaSetGravityVector(world.Space, Vector2.Left);
            CheckGravity(new(6, 8));
            tree.PhysicsFrame(.01);
            CheckGravity(new(-20, 0));
            using var collision = new KinematicCollision();
            using var disposed = new KinematicCollision();
            disposed.Dispose();
            using var point = new PhysicsPointQueryParameters();
            var hits = new PhysicsPointResult[2];
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i];
                var peer = peers[i];
                var start = body.GlobalTransform;
                var identity = body.GetRID();
                var tick = PhysicsServer.SpaceGetTick(world.Space);
                Check(body.TestMove(start, new(8, 0), collision, 0), "Every body role tests a real common collision");
                var travel = collision.GetTravel();
                Console.WriteLine($"Body motion {body.ClassName}: start={body.GlobalTransform}, travel={travel}, remainder={collision.GetRemainder()}, depth={collision.GetDepth()}");
                // The shared eight-refinement cast brackets first touch within 3/512 of the displacement.
                Check(travel.X >= 4 - 3f * 8 / 512 - .0001f && travel.X <= 4.0001f && MathF.Abs(travel.Y) < .001f &&
                    (travel + collision.GetRemainder() - new Vector2(8, 0)).Length() < .001f,
                    "The geometric first touch and complete global displacement are preserved");
                Check(collision.GetColliderRID() == peer.GetRID() && ReferenceEquals(collision.GetCollider(), peer) &&
                    collision.GetColliderID() == peer.InstanceID && ReferenceEquals(collision.GetLocalShape(), localOwner) &&
                    ReferenceEquals(collision.GetColliderShape(), peerOwner) && collision.GetColliderShapeIndex() == 0 &&
                    collision.GetNormal().Dot(Vector2.Left) > .99f,
                    "Motion results retain scene, RID, logical shape-owner and normal identity");
                using var preview = body.MoveAndCollide(new(8, 0), testOnly: true, safeMargin: 0);
                Check(preview is not null && (preview.GetTravel() - travel).Length() < .001f &&
                    body.GlobalTransform == start && PhysicsServer.SpaceGetTick(world.Space) == tick,
                    "TestMove and test-only motion neither move nor simulate the body");
                using var defaultMargin = body.MoveAndCollide(new(8, 0), testOnly: true);
                using var explicitMargin = body.MoveAndCollide(new(8, 0), testOnly: true, safeMargin: .08f, recoveryAsCollision: false);
                Check(defaultMargin is not null && explicitMargin is not null &&
                    (defaultMargin.GetTravel() - explicitMargin.GetTravel()).Length() < .001f && body.TestMove(start, new(8, 0)),
                    "Omitted motion options retain the public defaults");
                Reject<ArgumentOutOfRangeException>(() => body.TestMove(start, new(float.NaN, 0), collision));
                Reject<ArgumentOutOfRangeException>(() => body.TestMove(start, Vector2.Zero, collision, -1));
                Reject<ArgumentException>(() => body.TestMove(new(0, new(2, 1), 0, start.Origin), Vector2.Zero, collision));
                Reject<ObjectDisposedException>(() => body.TestMove(start, Vector2.Zero, disposed));
                Check(collision.GetTravel() == travel && body.GlobalTransform == start,
                    "Rejected motion preserves caller output and scene pose");
                body.AddCollisionExceptionWith(peer);
                body.AddCollisionExceptionWith(peer);
                var exceptions = body.GetCollisionExceptions();
                Check(exceptions.Length == 1 && ReferenceEquals(exceptions[0], peer), "Scene exception writes deduplicate");
                exceptions[0] = null;
                Check(ReferenceEquals(body.GetCollisionExceptions()[0], peer), "Exception results are independent caller-owned arrays");
                Check(!body.TestMove(start, new(8, 0), collision, 0) && collision.GetColliderRID() == default &&
                    collision.GetTravel() == new Vector2(8, 0) && collision.GetRemainder() == Vector2.Zero,
                    "An explicit exception affects base motion and replaces a prior hit with a complete miss");
                Check(preview!.GetColliderRID() == peer.GetRID() && preview.GetTravel() == travel,
                    "A returned collision snapshot survives later query and exception changes");
                body.RemoveCollisionExceptionWith(peer);
                body.RemoveCollisionExceptionWith(peer);
                Check(body.GetCollisionExceptions().Length == 0 && body.TestMove(start, new(8, 0), collision, 0),
                    "Removing an exception restores the actual common collision");
                using var moved = body.MoveAndCollide(new(8, 0), safeMargin: 0);
                Check(moved is not null && (body.GlobalPosition - start.Origin - moved.GetTravel()).Length() < .001f &&
                    MathF.Abs(body.GlobalRotation - start.Rotation) < .001f && body.GetRID() == identity &&
                    PhysicsServer.SpaceGetTick(world.Space) == tick,
                    "Committed motion respects the transformed parent without changing identity, rotation or world tick");
                point.Position = body.GlobalPosition;
                Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == identity,
                    "A public query sees the committed scene motion before another step");
                body.GlobalTransform = start;
                Task.Run(() =>
                {
                    Reject<InvalidOperationException>(() => body.GetGravity());
                    Reject<InvalidOperationException>(() => body.GetCollisionExceptions());
                    Reject<InvalidOperationException>(() => body.TestMove(start, Vector2.Zero));
                    Reject<InvalidOperationException>(() => body.MoveAndCollide(Vector2.Zero));
                }).GetAwaiter().GetResult();
            }
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i];
                PhysicsServer.BodySetMode(body.GetRID(), PhysicsServer.BodyMode.Kinematic);
                PhysicsServer.BodySetTransform(body.GetRID(), Start(i));
                tree.PhysicsFrame(.01);
                var start = body.GlobalTransform;
                using var moved = body.MoveAndCollide(new(8, 0), safeMargin: 0);
                Check(moved is not null && (body.GlobalPosition - start.Origin - moved.GetTravel()).Length() < .001f,
                    "Base motion commits the authored target in the direct kinematic profile");
                point.Position = body.GlobalPosition;
                Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0,
                    "A later direct kinematic target retains its accepted fixed-step publication boundary");
                tree.PhysicsFrame(.01);
                Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == body.GetRID(),
                    "The next physical step publishes the same target through the selected implementation");
                PhysicsServer.BodySetMode(body.GetRID(), PhysicsServer.BodyMode.Static);
                body.GlobalTransform = Start(i);
            }
            for (var i = 0; i < 128; i++) Frame(i);
            Collect();
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread();
            var allBytes = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame(i);
            ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes;
            allBytes = GC.GetTotalAllocatedBytes(true) - allBytes;
            Check(ownerBytes == 0 && allBytes == 0, $"Base body frames/reused motion outputs allocated {ownerBytes}/{allBytes} B");
            Console.WriteLine($"Body contract {backend?.ToString() ?? "registered"}/{threaded}: five roles, global motion/results/defaults/exceptions, 128 full frames {ownerBytes}/{allBytes} B passed.");

            void Frame(int frame)
            {
                tree.PhysicsFrame(1d / 120);
                var i = frame % bodies.Length;
                Check(bodies[i].TestMove(Start(i), new(8, 0), collision, 0) && collision.GetColliderRID() == peers[i].GetRID(),
                    "Warmed full frames retain common motion and logical identity");
                Check(bodies[i].MoveAndCollide(Vector2.Zero, testOnly: true, safeMargin: 0) is null,
                    "An unobstructed zero displacement needs no allocated collision result");
            }

            void CheckGravity(Vector2 expected)
            {
                foreach (var body in bodies)
                    Check((body.GetGravity() - (body is StaticBody ? Vector2.Zero : expected)).Length() < .001f,
                        "Base gravity reads retain the last completed field snapshot and each body's role");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }

        static Transform Start(int index) => new(.23f, new Vector2(index * 100, 0));
    }
}
