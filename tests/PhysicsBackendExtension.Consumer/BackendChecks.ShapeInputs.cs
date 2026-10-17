using Electron2D;

public static partial class BackendChecks
{
    public static void RunShapeInputs(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ShapeInputs", () => solver = new());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("ShapeInputs");
            using var root = new SubViewport { World = world };
            using var circle = new CircleShape { Radius = 2 };
            var moving = new RigidBody
            {
                GravityScale = 0,
                CanSleep = false,
                LinearVelocity = new(3, 0),
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            CollisionObject[] scenes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), moving, new PhysicalBone(), new Area()];
            for (var i = 0; i < scenes.Length; i++) { scenes[i].Position = new(i * 100, 0); scenes[i].CollisionMask = 0; root.AddChild(scenes[i]); }
            using var tree = new SceneTree(root);
            var rawBody = PhysicsServer.BodyCreate(); var rawArea = PhysicsServer.AreaCreate(); var pending = PhysicsServer.CircleShapeCreate();
            (RID RID, bool Area)[] owners = [.. scenes.Select(scene => (scene.GetRID(), scene is Area)), (rawBody, false), (rawArea, true)];
            var guardedReads = owners.SelectMany(owner => new Action[] {
                () => { if (owner.Area) PhysicsServer.AreaGetShapeCount(owner.RID); else PhysicsServer.BodyGetShapeCount(owner.RID); },
                () => { if (owner.Area) PhysicsServer.AreaGetShape(owner.RID, 0); else PhysicsServer.BodyGetShape(owner.RID, 0); },
                () => { if (owner.Area) PhysicsServer.AreaGetShapeTransform(owner.RID, 0); else PhysicsServer.BodyGetShapeTransform(owner.RID, 0); }
            }).ToArray();
            var pose = new Transform(.3f, new(3, 2)); var circleRID = circle.GetRID(); var ownerThread = Environment.CurrentManagedThreadId;
            var expected = circleRID; var slots = 2; var hooks = 0;
            using var query = new PhysicsPointQueryParameters { Position = pose.Origin };
            var hits = new PhysicsPointResult[1];
            try
            {
                PhysicsServer.BodySetCollisionMask(rawBody, 0); PhysicsServer.BodySetGravityScale(rawBody, 0);
                PhysicsServer.BodySetTransform(rawBody, new(0, new(700, 0))); PhysicsServer.AreaSetTransform(rawArea, new(0, new(800, 0)));
                foreach (var owner in owners)
                {
                    if (owner.Area) { PhysicsServer.AreaAddShape(owner.RID, pending, disabled: true); PhysicsServer.AreaAddShape(owner.RID, circleRID, pose); }
                    else { PhysicsServer.BodyAddShape(owner.RID, pending, disabled: true); PhysicsServer.BodyAddShape(owner.RID, circleRID, pose); }
                }
                PhysicsServer.BodySetSpace(rawBody, world.Space); PhysicsServer.AreaSetSpace(rawArea, world.Space);
                ReadAll(); InvalidIndices();
                Task.Run(RejectReads).GetAwaiter().GetResult();
                if (solver is not null)
                {
                    solver.StepHook = () =>
                    {
                        ReadHook();
                        Reject<InvalidOperationException>(() => PhysicsServer.BodyClearShapes(rawBody));
                        Reject<InvalidOperationException>(() => PhysicsServer.AreaSetShapeDisabled(rawArea, 1, true));
                        Reject<InvalidOperationException>(() => world.DirectSpaceState.IntersectPoint(query, hits));
                        Reject<InvalidOperationException>(() => tree.Dispose());
                        Task.Run(RejectReads).GetAwaiter().GetResult();
                    };
                    tree.PhysicsFrame(.01); solver.StepHook = ReadHook;
                    solver.StatusHook = ReadAll;
                    ReadAll(); solver.StatusHook = null;
                }
                for (var i = 0; i < 128; i++) Frame();
                var x = moving.Position.X; var before = hooks; Collect();
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame();
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0 && Math.Abs(moving.Position.X - x - 3.84f) < .005f && (solver is null || hooks == before + 128),
                    $"128 shape-input full frames: {own}/{all} B, motion={moving.Position.X - x}, hooks={hooks - before}");
                circle.Dispose(); expected = default; ReadAll(); tree.PhysicsFrame(.01);
                PhysicsServer.FreeRID(pending); slots = 1; ReadAll(); InvalidIndices(); tree.PhysicsFrame(.01);
                if (solver is not null)
                {
                    solver.StepHook = null; solver.FailSolve = true;
                    Reject<AggregateException>(() => tree.PhysicsFrame(.01));
                    RejectReads();
                }
                Console.WriteLine($"Shape inputs {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: six getters, six scene roles/raw Body/Area, pending/disabled/retired/reindexed slots, guards and 128 full frames {own}/{all} B passed.");
            }
            finally
            {
                if (solver is not null) { solver.StepHook = null; solver.StatusHook = null; }
                PhysicsServer.FreeRID(rawBody); PhysicsServer.FreeRID(rawArea);
                try { PhysicsServer.FreeRID(pending); } catch (ArgumentException) { }
            }

            void Read((RID RID, bool Area) owner)
            {
                Check((owner.Area ? PhysicsServer.AreaGetShapeCount(owner.RID) : PhysicsServer.BodyGetShapeCount(owner.RID)) == slots, "Logical count includes inactive slots");
                if (slots == 2)
                    Check((owner.Area ? PhysicsServer.AreaGetShape(owner.RID, 0) : PhysicsServer.BodyGetShape(owner.RID, 0)) == pending &&
                        (owner.Area ? PhysicsServer.AreaGetShapeTransform(owner.RID, 0) : PhysicsServer.BodyGetShapeTransform(owner.RID, 0)) == Transform.Identity, "Unconfigured disabled slot retains RID and local pose");
                Check((owner.Area ? PhysicsServer.AreaGetShape(owner.RID, slots - 1) : PhysicsServer.BodyGetShape(owner.RID, slots - 1)) == expected &&
                    (owner.Area ? PhysicsServer.AreaGetShapeTransform(owner.RID, slots - 1) : PhysicsServer.BodyGetShapeTransform(owner.RID, slots - 1)) == pose, "Effective shape identity and local pose survive retirement and reindexing");
            }
            void ReadAll() { foreach (var owner in owners) Read(owner); }
            void RejectReads() { foreach (var read in guardedReads) Reject<InvalidOperationException>(read); }
            void ReadHook()
            {
                Check((Environment.CurrentManagedThreadId != ownerThread) == threaded, "Only the selected numerical lane borrows prepared inputs");
                ReadAll(); hooks++;
            }
            void InvalidIndices()
            {
                foreach (var owner in owners)
                    foreach (var index in new[] { -1, slots })
                    {
                        Reject<ArgumentOutOfRangeException>(() => { if (owner.Area) PhysicsServer.AreaGetShape(owner.RID, index); else PhysicsServer.BodyGetShape(owner.RID, index); });
                        Reject<ArgumentOutOfRangeException>(() => { if (owner.Area) PhysicsServer.AreaGetShapeTransform(owner.RID, index); else PhysicsServer.BodyGetShapeTransform(owner.RID, index); });
                    }
                Reject<ArgumentException>(() => PhysicsServer.BodyGetShapeCount(rawArea));
                Reject<ArgumentException>(() => PhysicsServer.AreaGetShapeCount(rawBody));
                Reject<ArgumentException>(() => PhysicsServer.BodyGetShapeCount(default));
            }
            void Frame()
            {
                tree.PhysicsFrame(.01); ReadAll();
                Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == owners[0].RID && hits[0].ShapeIndex == 1,
                    "Real selected queries retain the active logical index after a disabled gap");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
