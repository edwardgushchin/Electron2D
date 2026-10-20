using Electron2D;

public static partial class BackendChecks
{
    public static void RunMotionOneWayPairs(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { MotionOneWayPairsCore(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
    private static void MotionOneWayPairsCore(PhysicsServer.Backend? backend, bool threaded)
    {
        var owner = Thread.CurrentThread;
        if (backend is null) PhysicsServerManager.RegisterServer("MotionOneWayPairs", () => new Solver
        { StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "One-way pair frames use the selected numerical lane") });
        using var world = backend is { } selected ? new World(selected) : new World("MotionOneWayPairs");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
            "One-way pair queries select the requested implementation without fallback");
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        using var circle = new CircleShape { Radius = 10 }; using var twin = new CircleShape { Radius = 10 };
        using var small = new CircleShape { Radius = 2 }; using var gap = new CircleShape { Radius = .1f };
        using var thin = new RectangleShape { Size = new(2, 200) }; using var boundary = new WorldBoundaryShape { Normal = Vector2.Left };
        using var split = new ConcavePolygonShape { Segments = [new(-3, -100), new(-3, 100), new(3, -100), new(3, 100)] };
        var raw = new RID[4]; var scene = new StaticBody[4]; var parked = new Transform[4]; var sourceOwners = new CollisionShape[4][];
        var targets = new RID[12]; var nodes = new StaticBody[6]; var tags = new Node[6];
        using var query = new PhysicsTestMotionParameters(); using var result = new PhysicsTestMotionResult(); using var sceneResult = new KinematicCollision();
        using var ticker = new RigidBody
        {
            Position = new(500, 500),
            GravityScale = 0,
            CanSleep = false,
            CollisionLayer = 0,
            CollisionMask = 0,
            LinearVelocity = Vector2.Right,
            LinearDampMode = RigidBody.DampMode.Replace,
            LinearDamp = 0
        };
        ticker.AddChild(new CollisionShape { Shape = gap }); root.AddChild(ticker);
        try
        {
            for (var i = 0; i < 4; i++)
            {
                parked[i] = new(0, new(1000 + i * 200, 500)); var aliases = i is 1 or 2;
                raw[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(raw[i], PhysicsServer.BodyMode.Static);
                PhysicsServer.BodySetCollisionLayer(raw[i], 0); PhysicsServer.BodySetCollisionMask(raw[i], 4);
                PhysicsServer.BodyAddShape(raw[i], gap.GetRID(), disabled: true);
                var firstShape = i == 3 ? small : circle; var firstPose = new Transform(0, aliases ? new(6, 0) : Vector2.Zero);
                PhysicsServer.BodyAddShape(raw[i], firstShape.GetRID(), firstPose);
                scene[i] = new StaticBody { Transform = parked[i], CollisionLayer = 0, CollisionMask = 4 };
                scene[i].AddChild(new CollisionShape { Shape = gap, Disabled = true });
                var first = new CollisionShape { Shape = firstShape, Transform = firstPose }; scene[i].AddChild(first);
                if (aliases)
                {
                    var secondShape = i == 1 ? circle : twin; var secondPose = new Transform(0, new(-5, 0));
                    PhysicsServer.BodyAddShape(raw[i], secondShape.GetRID(), secondPose);
                    var second = new CollisionShape { Shape = secondShape, Transform = secondPose }; scene[i].AddChild(second); sourceOwners[i] = [first, second];
                }
                else sourceOwners[i] = [first];
                root.AddChild(scene[i]); PhysicsServer.BodySetTransform(raw[i], parked[i]); PhysicsServer.BodySetSpace(raw[i], world.Space);
            }
            for (var i = 0; i < 6; i++)
            {
                var shape = i is 0 or 2 or 5 ? (Shape)boundary : i == 3 ? split : thin; var count = i == 4 ? 33 : 1; var policyMargin = i == 2 ? 11 : i == 5 ? 13 : 100;
                tags[i] = new Node(); root.AddChild(tags[i]);
                targets[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(targets[i], PhysicsServer.BodyMode.Static);
                PhysicsServer.BodySetCollisionLayer(targets[i], 0); PhysicsServer.BodySetCollisionMask(targets[i], 0); PhysicsServer.BodyAttachObject(targets[i], tags[i]);
                PhysicsServer.BodyAddShape(targets[i], gap.GetRID(), disabled: true);
                nodes[i] = new StaticBody { CollisionLayer = 0, CollisionMask = 0 }; nodes[i].AddChild(new CollisionShape { Shape = gap, Disabled = true });
                for (var j = 1; j <= count; j++)
                {
                    PhysicsServer.BodyAddShape(targets[i], shape.GetRID());
                    nodes[i].AddChild(new CollisionShape { Shape = shape, OneWayCollision = i != 0, OneWayCollisionDirection = Vector2.Right, OneWayCollisionMargin = policyMargin });
                    if (i != 0) PhysicsServer.BodySetShapeAsOneWayCollision(targets[i], j, true, policyMargin, Vector2.Right);
                }
                root.AddChild(nodes[i]); targets[i + 6] = nodes[i].GetRID(); PhysicsServer.BodySetSpace(targets[i], world.Space);
            }
            tree.PhysicsFrame(.01);
            for (var configuration = 0; configuration < 40; configuration++) Exercise(configuration, true);
            // Immutable one-way policy authoring is exercised cold; prepared queries and full steps stay in the warm bracket.
            for (var frame = 0; frame < 128; frame++) Frame(frame); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var frame = 0; frame < 128; frame++) Frame(frame + 128);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"One-way pair complete moving frames allocated {own}/{all} B");
            Console.WriteLine($"One-way recovery pairs {backend?.ToString() ?? "registered"}, separate={threaded}: side/depth rejection, last-pass/query reset, source-resource aliasing, accepted concave piece, 32-pair cap, raw/scene/TestMove/applied motion; 128 full frames {own}/{all} B.");
            void Frame(int frame)
            {
                Exercise(frame % 40, false); var x = ticker.Position.X; tree.PhysicsFrame(.01);
                Check(MathF.Abs(ticker.Position.X - x - .01f) < .0001f, "One-way pair frames perform the actual numerical step");
            }
            void Exercise(int configuration, bool apply)
            {
                var kind = configuration % 10; var angle = configuration / 10 % 2 == 0 ? 0 : .37f; var role = configuration / 20;
                var outer = new Transform(angle, angle == 0 ? Vector2.Zero : new(70, -30)); var axis = outer.BasisXform(Vector2.Right);
                var source = kind == 3 ? 1 : kind == 4 ? 2 : kind == 5 ? 3 : 0;
                var target = kind == 1 ? 2 : kind == 2 ? 5 : kind == 5 ? 3 : kind is 6 or 7 ? 4 : 1;
                var wall = targets[role * 6]; var peer = targets[role * 6 + target]; var useWall = kind is 0 or 1 or 2 or 6 or 7 or 8 or 9;
                var margin = kind == 5 ? 2 : kind >= 8 ? 10 : 1; var wallX = kind >= 8 ? 16.55f : 8;
                for (var i = 0; i < 12; i++)
                {
                    PhysicsServer.BodySetCollisionLayer(targets[i], targets[i] == peer || useWall && targets[i] == wall ? 4u : 0u);
                    PhysicsServer.BodySetTransform(targets[i], outer * new Transform(0, i % 6 == 0 ? new(wallX, 0) : Vector2.Zero));
                }
                if (apply && target is 2 or 5)
                {
                    PhysicsServer.BodySetShapeAsOneWayCollision(peer, 1, true, 99, Vector2.Right);
                    PhysicsServer.BodySetShapeAsOneWayCollision(peer, 1, true, kind == 1 ? 11 : 13, Vector2.Right);
                }
                if (target == 4) PhysicsServer.BodySetShapeDisabled(peer, 33, kind == 6);
                var linear = outer.BasisXform(new(4, -2));
                PhysicsServer.BodySetLinearVelocity(wall, linear); PhysicsServer.BodySetAngularVelocity(wall, .25f);
                PhysicsServer.BodySetLinearVelocity(peer, linear); PhysicsServer.BodySetAngularVelocity(peer, .25f);
                query.From = outer * new Transform(0, kind is 3 or 4 or 5 ? Vector2.Zero : new(15, 0));
                query.Motion = axis * (kind == 8 ? .25f : kind == 9 ? 20 : 0); query.Margin = margin; query.RecoveryAsCollision = kind < 8;
                var expectedHit = kind is not (3 or 8); var expectedLocal = kind == 4 ? 2 : 1;
                var expectedRID = kind is 2 or 4 or 5 or 7 ? peer : wall;
                var expectedPointX = kind is 2 ? 0 : kind is 4 or 7 ? -1 : kind == 5 ? 3 : wallX;
                for (var route = 0; route < 3; route++)
                {
                    var hit = route == 2 ? scene[source].TestMove(query.From, query.Motion, sceneResult, margin, query.RecoveryAsCollision) :
                        PhysicsServer.BodyTestMotion(route == 0 ? raw[source] : scene[source].GetRID(), query, result);
                    var travel = route == 2 ? sceneResult.GetTravel() : result.GetTravel(); var remainder = route == 2 ? sceneResult.GetRemainder() : result.GetRemainder();
                    var collider = route == 2 ? sceneResult.GetColliderRID() : result.GetColliderRID(); var depth = route == 2 ? sceneResult.GetDepth() : result.GetCollisionDepth();
                    var valid = hit == expectedHit && PhysicsServer.BodyGetTransform(raw[source]) == parked[source] && scene[source].GlobalTransform == parked[source];
                    if (hit)
                    {
                        var point = route == 2 ? sceneResult.GetPosition() : result.GetCollisionPoint(); var normal = route == 2 ? sceneResult.GetNormal() : result.GetCollisionNormal();
                        var velocity = route == 2 ? sceneResult.GetColliderVelocity() : result.GetColliderVelocity(); var slot = route == 2 ? sceneResult.GetColliderShapeIndex() : result.GetColliderShape();
                        var objectID = route == 2 ? sceneResult.GetColliderID() : result.GetColliderID(); var identity = role == 0 ? tags[(kind is 2 or 4 or 5 or 7) ? target : 0].InstanceID : nodes[(kind is 2 or 4 or 5 or 7) ? target : 0].InstanceID;
                        var origin = PhysicsServer.BodyGetTransform(expectedRID).Origin; var relative = point - origin;
                        var expectedVelocity = linear + new Vector2(-relative.Y, relative.X) * .25f;
                        valid &= collider == expectedRID && objectID == identity && (target == 4 && kind == 7 ? slot is >= 1 and <= 33 : slot == 1);
                        valid &= route == 2 ? ReferenceEquals(sceneResult.GetLocalShape(), sourceOwners[source][expectedLocal - 1]) : result.GetCollisionLocalShape() == expectedLocal;
                        valid &= MathF.Abs(normal.Length() - 1) < .001f && normal.Dot(-axis) > .999f && velocity.DistanceTo(expectedVelocity) < .01f;
                        valid &= MathF.Abs((outer.AffineInverse() * point).X - expectedPointX) < .003f;
                        var expectedDepth = kind switch { 0 or 1 or 6 => 2.37632f, 4 => .95072f, 5 => .21664f, 7 => 11.37632f, 9 => 10, _ => 26 + travel.Dot(axis) };
                        valid &= MathF.Abs(depth - expectedDepth) < .003f;
                        if (kind == 2) valid &= depth > 6 && depth < 7.3f;
                        if (kind == 9)
                        {
                            var expectedFraction = 7.17368f / 20;
                            if (route != 2) valid &= result.GetCollisionSafeFraction() <= expectedFraction + .0002f && result.GetCollisionUnsafeFraction() >= expectedFraction - .0002f && result.GetCollisionUnsafeFraction() - result.GetCollisionSafeFraction() <= 1f / 256 + .000002f;
                            valid &= (travel + remainder).DistanceTo(query.Motion - axis * 15.62368f) < .003f;
                        }
                        else valid &= remainder == Vector2.Zero;
                    }
                    else
                    {
                        var expectedTravel = kind == 3 ? -6.04928f : -15.37368f;
                        valid &= collider == default && depth == 0 && remainder == Vector2.Zero && travel.DistanceTo(axis * expectedTravel) < .003f;
                        if (route != 2) valid &= result.GetCollisionSafeFraction() == 1 && result.GetCollisionUnsafeFraction() == 1;
                    }
                    if (!valid) throw new InvalidOperationException($"One-way pair kind={kind}, role={role}, angle={angle}, route={route}: hit={hit}, collider={collider}, depth={depth}, travel={travel}, remainder={remainder}");
                }
                if (apply)
                {
                    var travel = sceneResult.GetTravel(); scene[source].GlobalTransform = query.From;
                    using var collision = scene[source].MoveAndCollide(query.Motion, safeMargin: margin, recoveryAsCollision: query.RecoveryAsCollision);
                    Check((collision is not null) == expectedHit && scene[source].GlobalTransform.Origin.DistanceTo(query.From.Origin + travel) < .003f,
                        "Applied scene movement consumes the retained rejection policy and final travel");
                    scene[source].Transform = parked[source];
                }
                // A new query must not inherit a previous recovery rejection.
                query.From = outer * new Transform(0, new(-30, 0)); query.Motion = axis * 25; query.Margin = 1; query.RecoveryAsCollision = false;
                PhysicsServer.BodySetCollisionLayer(wall, 0);
                var nextHit = PhysicsServer.BodyTestMotion(raw[0], query, result);
                if (!nextHit || result.GetColliderRID() != peer)
                    throw new InvalidOperationException($"One-way pair next query kind={kind}, role={role}, angle={angle}: hit={nextHit}, collider={result.GetColliderRID()}, expected={peer}");
            }
        }
        finally
        {
            foreach (var id in raw) if (id.IsValid()) PhysicsServer.FreeRID(id);
            for (var i = 0; i < 6; i++) if (targets[i].IsValid()) PhysicsServer.FreeRID(targets[i]);
            foreach (var body in scene) body?.Dispose(); foreach (var node in nodes) node?.Dispose(); foreach (var tag in tags) tag?.Dispose();
        }
    }
}
