using Electron2D;

public static partial class BackendChecks
{
    public static void RunHierarchyPoses(PhysicsServer.Backend? backend = null, bool threaded = false, bool areaOnly = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        var providers = new List<Solver>();
        if (backend is null) PhysicsServerManager.RegisterServer("HierarchyPoses", () => { var solver = new Solver(); providers.Add(solver); return solver; });
        var rejected = 0; var callbacks = 0; var measured = 0; var queries = 0; var owner = Thread.CurrentThread;
        using var shape = new RectangleShape { Size = new(2, 8) };
        try
        {
            for (var kind = areaOnly ? 5 : 0; kind < 6; kind++)
            {
                using var world = backend is { } selected ? new World(selected) : new World("HierarchyPoses"); var space = world.Space; var solver = backend is null ? providers[^1] : null;
                using var root = new SubViewport { World = world }; var parent = new Entity(); root.AddChild(parent);
                CollisionObject node = kind switch
                {
                    0 => new StaticBody(),
                    1 => new AnimatableBody(),
                    2 => new CharacterBody(),
                    3 => new RigidBody(),
                    4 => new PhysicalBone(),
                    _ => new Area()
                };
                var id = node.GetRID(); var isArea = node is Area;
                node.AddChild(new CollisionShape { Shape = shape, Disabled = true }); node.AddChild(new CollisionShape { Shape = shape }); parent.AddChild(node);
                if (!isArea) { PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetCanSleep(id, false); }
                var moving = new RigidBody
                {
                    Position = new(0, 1000),
                    GravityScale = 0,
                    CanSleep = false,
                    LinearVelocity = Vector2.Right,
                    LinearDampMode = RigidBody.DampMode.Replace,
                    AngularDampMode = RigidBody.DampMode.Replace
                };
                moving.AddChild(new CollisionShape { Shape = shape }); root.AddChild(moving);
                var factoryCalls = 0; var poseCalls = 0; var marker = new ApplicationException("Nonfinite selected pose was reached");
                using var point = new PhysicsPointQueryParameters { CollideWithBodies = true, CollideWithAreas = true }; var hits = new PhysicsPointResult[1];
                Poison();
                if (solver is not null) solver.ColliderFactoryHook = current => { if (current == id) { factoryCalls++; throw marker; } };
                var errors = ReleaseErrors(() => _ = new SceneTree(root));
                Check(factoryCalls == 0 && errors.Length > 0 && errors.All(error => error is InvalidOperationException),
                    $"Nonfinite hierarchy pose reached selected creation for {kind}: {factoryCalls} calls, {errors.Length} errors"); rejected++;
                Check(root.Tree is null && node.Tree is null && Space() == default, "Failed hierarchy activation retires membership without destroying authored nodes");
                if (solver is not null) solver.ColliderFactoryHook = null;
                Repair(); using var tree = new SceneTree(root);
                Check(Space() == space && node.IsNodeReady, "The original hierarchy activates after correcting finite local authoring");
                root.RemoveChild(parent); Poison();
                if (solver is not null) solver.ColliderFactoryHook = current => { if (current == id) { factoryCalls++; throw marker; } };
                errors = ReleaseErrors(() => root.AddChild(parent));
                Check(errors.Length > 0 && errors.All(error => error is InvalidOperationException) && factoryCalls == 0 && Space() == default && node.Tree == tree,
                    "Live entry rejects composed overflow before numerical creation while retaining structural membership"); rejected++;
                root.RemoveChild(parent);
                errors = ReleaseErrors(() => Bind(space));
                Check(errors.Length > 0 && errors.All(error => error is InvalidOperationException) && factoryCalls == 0 && Space() == default,
                    "Explicit detached scene binding uses the same composed finite-pose gate"); rejected++;
                if (solver is not null) solver.ColliderFactoryHook = null;
                Repair(); root.AddChild(parent); tree.PhysicsFrame(.01);
                Poison(); var tick = PhysicsServer.SpaceGetTick(space);
                if (solver is not null) solver.PoseHook = current => { if (current == id) { poseCalls++; throw marker; } };
                errors = ReleaseErrors(() => { point.Position = Vector2.Zero; _ = world.DirectSpaceState.IntersectPoint(point, hits); });
                Check(errors.Length > 0 && errors.All(error => error is InvalidOperationException) && poseCalls == 0 && PhysicsServer.SpaceGetTick(space) == tick,
                    "Queries reject composed overflow before a selected pose publication"); rejected++;
                errors = ReleaseErrors(() => tree.PhysicsFrame(.01));
                Check(errors.Length > 0 && errors.All(error => error is InvalidOperationException) && poseCalls == 0 && PhysicsServer.SpaceGetTick(space) == tick,
                    "An invalid composed pose cannot reach a numerical Solve or advance the tick"); rejected++;
                if (solver is not null) solver.PoseHook = null;
                Repair(); tree.PhysicsFrame(.01); Query(node.GlobalTransform, id, 1);
                node.TopLevel = true; node.Transform = Transform.Identity; parent.Position = new(float.MaxValue * .75f, 0);
                tree.PhysicsFrame(.01); Query(Transform.Identity, id, 1);
                Check(node.GlobalTransform.IsFinite(), "A top-level collider keeps its finite global pose independently of its ancestor");
                parent.Transform = Transform.Identity; node.TopLevel = false;
                var raw = PhysicsServer.AreaCreate();
                try
                {
                    PhysicsServer.AreaAddShape(raw, shape.GetRID(), disabled: true); PhysicsServer.AreaAddShape(raw, shape.GetRID());
                    PhysicsServer.AreaSetSpace(raw, space);
                    PhysicsServer.BodySetStateSyncCallback(moving.GetRID(), _ => { Check(Thread.CurrentThread == owner, "Recovered hierarchy callbacks retain the owner"); callbacks++; });
                    var scenePose = Transform.Identity; var rawPose = Transform.Identity;
                    if (solver is not null) solver.StepHook = () =>
                    {
                        Check((Thread.CurrentThread != owner) == threaded, "Hierarchy preparation retains the captured numerical lane");
                        Check(PoseNear(PhysicsServer.AreaGetTransform(raw), rawPose) && PoseNear(solver.Bodies[raw].Pose, rawPose), "The numerical hook reads prepared raw Area input");
                        if (isArea) Check(PoseNear(PhysicsServer.AreaGetTransform(id), scenePose) && PoseNear(solver.Bodies[id].Pose, scenePose), "The numerical hook reads prepared composed scene Area input");
                    };
                    for (var frame = 0; frame < 128; frame++) Frame(frame);
                    var calls = callbacks; var beforeQueries = queries; var start = moving.Position.X; Collect();
                    var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                    for (var frame = 0; frame < 128; frame++) Frame(frame);
                    own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                    Check(own == 0 && all == 0 && callbacks == calls + 128 && queries == beforeQueries + 512 && MathF.Abs(moving.Position.X - start - 1.28f) < .001f,
                        $"Hierarchy pose frames allocated {own}/{all} B or lost actual motion/query/callback work"); measured += 128;
                    void Frame(int frame)
                    {
                        parent.Transform = new((frame & 1) * Mathf.Pi / 2, new(10 + (frame & 1), 0)); node.Transform = new(.3f, new(3, 2)); scenePose = node.GlobalTransform;
                        if (isArea) PhysicsServer.AreaSetTransform(id, scenePose);
                        rawPose = new(.4f + (frame & 1) * Mathf.Pi / 2, new(100, frame & 1)); PhysicsServer.AreaSetTransform(raw, rawPose);
                        Check(PoseNear(PhysicsServer.AreaGetTransform(raw), rawPose) && (!isArea || PoseNear(PhysicsServer.AreaGetTransform(id), scenePose)), "Area getters retain complete authored world-space pose");
                        var before = PhysicsServer.SpaceGetTick(space); tree.PhysicsFrame(.01); Check(PhysicsServer.SpaceGetTick(space) == before + 1, "Each recovered hierarchy frame commits exactly one selected tick");
                        Query(scenePose, id, 1); Query(rawPose, raw, 1);
                    }
                }
                finally { if (solver is not null) solver.StepHook = null; PhysicsServer.FreeRID(raw); }
                Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "Hierarchy validation preserves actual selected backend identity without fallback");
                void Poison()
                {
                    parent.Position = new(float.MaxValue * .75f, 0); node.Position = new(float.MaxValue * .75f, 0);
                    Check(parent.Transform.IsFinite() && node.Transform.IsFinite() && !node.GlobalTransform.IsFinite(), "Finite local transforms can overflow their hierarchy-global translation");
                }
                void Repair() { parent.Transform = Transform.Identity; node.Transform = Transform.Identity; }
                RID Space() => isArea ? PhysicsServer.AreaGetSpace(id) : PhysicsServer.BodyGetSpace(id);
                void Bind(RID target) { if (isArea) PhysicsServer.AreaSetSpace(id, target); else PhysicsServer.BodySetSpace(id, target); }
                void Query(Transform pose, RID expected, int slot)
                {
                    point.Position = pose * new Vector2(0, 3); Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == expected && hits[0].ShapeIndex == slot, "Asymmetric geometry receives the actual composed pose and stable logical slot");
                    point.Position = pose * new Vector2(3, 0); Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0, "Rotated narrow axes remain empty in actual selected queries"); queries += 2;
                }
            }
            Console.WriteLine($"Hierarchy poses {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: {rejected} rejected global poses, {measured} complete frames with {measured} owner callbacks and {measured * 4} queries 0/0 B passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        static bool PoseNear(Transform a, Transform b) => a.Origin.DistanceTo(b.Origin) < .0001f && a.X.DistanceTo(b.X) < .0001f && a.Y.DistanceTo(b.Y) < .0001f;
    }
}
