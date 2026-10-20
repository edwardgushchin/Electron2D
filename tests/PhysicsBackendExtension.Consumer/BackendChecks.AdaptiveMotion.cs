using Electron2D;

public static partial class BackendChecks
{
    public static void RunAdaptiveMotion(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread); var owner = Thread.CurrentThread; var solvedFrames = 0;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("AdaptiveMotion", () => new Solver
        { StepHook = () => { Check((Thread.CurrentThread != owner) == threaded, "Adaptive motion uses the selected numerical thread"); solvedFrames++; } });
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("AdaptiveMotion");
            Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "Adaptive motion keeps the requested implementation");
            using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
            using var ray = new SeparationRayShape { Length = 1, SlideOnSlope = true }; using var circle = new CircleShape { Radius = .5f };
            using var floorShape = new RectangleShape { Size = new(20, .02f) }; using var gap = new CircleShape { Radius = .03f };
            Shape[] sources = [ray, circle]; var floors = new StaticBody[2]; var raw = new RID[4]; var scene = new StaticBody[4]; var primary = new CollisionShape[4];
            using var query = new PhysicsTestMotionParameters { Motion = new(0, 1000), Margin = .0001f, CollideSeparationRay = true };
            using var result = new PhysicsTestMotionResult(); using var sceneResult = new KinematicCollision();
            var hits = new PhysicsShapeResult[4]; var contacts = new Vector2[8];
            using var shapeQuery = new PhysicsShapeQueryParameters { Motion = query.Motion, Margin = query.Margin };
            try
            {
                for (var family = 0; family < 2; family++)
                {
                    floors[family] = new StaticBody { Position = new(family * 200, 1.02f) };
                    floors[family].AddChild(new CollisionShape { Shape = gap, Disabled = true }); floors[family].AddChild(new CollisionShape { Shape = floorShape }); root.AddChild(floors[family]);
                    for (var order = 0; order < 2; order++)
                    {
                        var at = family * 2 + order; raw[at] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(raw[at], PhysicsServer.BodyMode.Kinematic);
                        PhysicsServer.BodySetCollisionLayer(raw[at], 0); PhysicsServer.BodyAddShape(raw[at], gap.GetRID(), disabled: true);
                        scene[at] = new StaticBody { Position = new(5000 + at * 50, 5000), CollisionLayer = 0 }; scene[at].AddChild(new CollisionShape { Shape = gap, Disabled = true });
                        for (var index = 1; index < 3; index++)
                        {
                            var pose = new Transform(0, index == order + 1 ? new Vector2(-3, 0) : new Vector2(3, -10));
                            PhysicsServer.BodyAddShape(raw[at], sources[family].GetRID(), pose); var slot = new CollisionShape { Shape = sources[family], Transform = pose }; scene[at].AddChild(slot);
                            if (index == order + 1) primary[at] = slot;
                        }
                        root.AddChild(scene[at]); PhysicsServer.BodySetTransform(raw[at], new(0, new Vector2(6000 + at * 50, 5000))); PhysicsServer.BodySetSpace(raw[at], world.Space);
                    }
                }
                Frame(); for (var i = 0; i < 128; i++) Frame(); Collect();
                var beforeFrames = solvedFrames; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame();
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(backend is not null || solvedFrames - beforeFrames == 512, "Measured batches execute every registered numerical frame");
                Check(own == 0 && all == 0, $"Adaptive motion allocated {own}/{all} B");
                Console.WriteLine($"Adaptive motion {world.PhysicsBackendName}/{threaded}: near-start/middle/end, two source orders, body/scene/direct cast, 128 batches / 512 physics frames and 8192 public queries {own}/{all} B.");
                void Frame()
                {
                    for (var edge = 0; edge < 4; edge++)
                    {
                        var distance = edge == 0 ? .01f : edge == 1 ? 100 : edge == 2 ? 300 : 999.99f;
                        for (var family = 0; family < 2; family++) floors[family].Position = new(family * 200, distance + (family == 0 ? 1 : .5f) + .01f);
                        tree.PhysicsFrame(1d / 60);
                        for (var family = 0; family < 2; family++)
                        {
                            query.From = new(0, new Vector2(family * 200, 0));
                            for (var order = 0; order < 2; order++)
                            {
                                var at = family * 2 + order;
                                if (!PhysicsServer.BodyTestMotion(raw[at], query, result))
                                    throw new InvalidOperationException($"Adaptive near-start motion miss family={family}, order={order}, edge={edge}, from={query.From.Origin}, target={floors[family].Position}, travel={result.GetTravel()}.");
                                Fractions(result.GetCollisionSafeFraction(), result.GetCollisionUnsafeFraction(), edge, distance);
                                Check(result.GetColliderRID() == floors[family].GetRID() && result.GetColliderShape() == 1 && result.GetCollisionLocalShape() == order + 1 &&
                                    result.GetCollisionNormal().Y < -.99f && result.GetTravel().DistanceTo(query.Motion * result.GetCollisionSafeFraction()) < .003f,
                                    "Adaptive body result identifies the winning logical source and target");
                                Check(scene[at].TestMove(query.From, query.Motion, sceneResult, query.Margin) && ReferenceEquals(sceneResult.GetLocalShape(), primary[at]) &&
                                    sceneResult.GetColliderRID() == floors[family].GetRID() && sceneResult.GetTravel().DistanceTo(result.GetTravel()) < .003f,
                                    "Adaptive scene motion retains its supplied pose and the same safe travel");
                            }
                            shapeQuery.Shape = sources[family]; shapeQuery.Transform = query.From; shapeQuery.Motion = Vector2.Zero;
                            Check(world.DirectSpaceState.IntersectShape(shapeQuery, hits) == 0 && world.DirectSpaceState.CollideShape(shapeQuery, contacts) == 0 && world.DirectSpaceState.GetRestInfo(shapeQuery) is null,
                                "A finite positive initial gap does not become overlap, contact or rest");
                            shapeQuery.Motion = query.Motion;
                            var fractions = world.DirectSpaceState.CastMotion(shapeQuery); Fractions(fractions.SafeFraction, fractions.UnsafeFraction, edge, distance);
                        }
                    }
                }
            }
            finally { foreach (var body in raw) if (body.IsValid()) PhysicsServer.FreeRID(body); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }
    private static void Fractions(float safe, float unsafeFraction, int edge, float distance)
    {
        var actual = distance / 1000;
        Check(safe >= 0 && safe <= unsafeFraction && unsafeFraction <= 1 && safe <= actual + .000002f && unsafeFraction >= actual - .000002f,
            "Adaptive bracket contains the analytic first-surface arrival");
        Check(unsafeFraction - safe <= (edge is 0 or 3 ? .00007f : 3f / 512 + .000002f) && (edge != 3 || safe > .9999f),
            "Endpoint refinement preserves progress and a narrow arrival bracket");
    }
}
