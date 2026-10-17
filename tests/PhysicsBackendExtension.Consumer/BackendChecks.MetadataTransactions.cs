using Electron2D;

public static partial class BackendChecks
{
    public static void RunMetadataTransactions(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("MetadataTransactions", () => solver = new());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("MetadataTransactions");
            using var root = new SubViewport { World = world };
            using var circle = new CircleShape { Radius = 2 }; using var first = new Node(); using var second = new Node();
            CollisionObject?[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(),
                new RigidBody { GravityScale = 0 }, new PhysicalBone(), new Area(), null, null];
            var ids = new RID[8];
            for (var i = 0; i < nodes.Length; i++)
                if (nodes[i] is { } node) { node.Position = new(i * 100, 0); node.CollisionMask = 0; root.AddChild(node); }
            var moving = new RigidBody
            {
                Position = new(0, 1000),
                GravityScale = 0,
                CanSleep = false,
                CollisionLayer = 0,
                CollisionMask = 0,
                LinearVelocity = new(6, 0),
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            moving.AddChild(new CollisionShape { Shape = circle }); root.AddChild(moving);
            using var tree = new SceneTree(root);
            const ulong oldCanvas = 0xAA51, newCanvas = 0xFEDCBA9876543210;
            using var point = new PhysicsPointQueryParameters { CollideWithAreas = true }; var hits = new PhysicsPointResult[1];
            var callbacks = 0; var rebuilds = 0; var faults = 0; var operations = 0;
            try
            {
                for (var i = 0; i < ids.Length; i++)
                {
                    ids[i] = nodes[i]?.GetRID() ?? (IsArea(i) ? PhysicsServer.AreaCreate() : PhysicsServer.BodyCreate());
                    if (nodes[i] is null)
                    {
                        if (IsArea(i)) PhysicsServer.AreaSetTransform(ids[i], new(0, new(i * 100, 0)));
                        else { PhysicsServer.BodySetTransform(ids[i], new(0, new(i * 100, 0))); PhysicsServer.BodySetGravityScale(ids[i], 0); }
                    }
                    if (IsArea(i)) { PhysicsServer.AreaAddShape(ids[i], circle.GetRID(), disabled: true); PhysicsServer.AreaAddShape(ids[i], circle.GetRID()); PhysicsServer.AreaSetSpace(ids[i], world.Space); }
                    else { PhysicsServer.BodyAddShape(ids[i], circle.GetRID(), disabled: true); PhysicsServer.BodyAddShape(ids[i], circle.GetRID()); PhysicsServer.BodySetSpace(ids[i], world.Space); }
                    Reset(i);
                }
                PhysicsServer.BodySetStateSyncCallback(moving.GetRID(), _ => callbacks++); tree.PhysicsFrame(.01);
                if (solver is not null) solver.ShapeHook = _ => rebuilds++;
                for (var i = 0; i < ids.Length; i++)
                    for (var operation = 0; operation < OperationCount(i); operation++)
                    {
                        if (solver is not null)
                            for (var path = 0; path < 2; path++)
                                for (var failures = 1; failures <= 2; failures++)
                                {
                                    Reset(i); using var candidate = new Node(); var target = operation == 0 ? candidate : first;
                                    var attempts = 0; var apply = new ApplicationException("metadata-apply"); var retry = new ArithmeticException("metadata-retry");
                                    Action hook = () =>
                                    {
                                        if (++attempts > failures) return;
                                        var body = solver.Bodies[ids[i]];
                                        if (operation < 2) { body.ObjectID = 0; body.Canvas = 0; } else body.Priority = 0;
                                        Reject<InvalidOperationException>(() => target.Dispose());
                                        Authored(i, target, operation == 1 ? newCanvas : oldCanvas, operation >= 2 ? 3 : 1);
                                        if (attempts == 1) throw apply; throw retry;
                                    };
                                    if (operation < 2) solver.IdentityHook = id => { if (id == ids[i]) hook(); };
                                    else solver.PriorityHook = hook;
                                    var tick = PhysicsServer.SpaceGetTick(world.Space); var count = callbacks;
                                    var errors = ReleaseErrors(() => Set(i, operation, candidate, newCanvas, 3));
                                    Check(errors.Length == 1 && ReferenceEquals(errors[0], apply) && attempts == 1, "Failed metadata publication retains its original error and committed ownership");
                                    Authored(i, target, operation == 1 ? newCanvas : oldCanvas, operation >= 2 ? 3 : 1);
                                    if (failures == 2)
                                    {
                                        errors = ReleaseErrors(() => Recover(path));
                                        Check(errors.Length == 1 && ReferenceEquals(errors[0], retry), "Failed metadata preparation propagates the original retry error");
                                        Check(PhysicsServer.SpaceGetTick(world.Space) == tick && callbacks == count, "A failed metadata preparation does not solve or publish a tick");
                                    }
                                    Recover(path); solver.IdentityHook = null; solver.PriorityHook = null;
                                    Query(i, target, operation == 1 ? newCanvas : oldCanvas, operation >= 2 ? 3 : 1);
                                    Check(attempts == failures + 1 && PhysicsServer.SpaceGetTick(world.Space) == tick + (ulong)path && callbacks == count + path,
                                        "Only explicit successful preparation reaches numerical publication");
                                    Reset(i); faults++;
                                    void Recover(int lane)
                                    {
                                        if (lane == 0) Query(i, target, operation == 1 ? newCanvas : oldCanvas, operation >= 2 ? 3 : 1);
                                        else tree.PhysicsFrame(.01);
                                    }
                                }
                        Reset(i); Set(i, operation, second, newCanvas, 3);
                        Query(i, operation == 0 ? second : first, operation == 1 ? newCanvas : oldCanvas, operation >= 2 ? 3 : 1);
                        Reset(i); operations++;
                    }
                Check(rebuilds == 0, "Live metadata repair never rebuilds geometry");
                if (solver is not null)
                    for (var i = 6; i < ids.Length; i++)
                        for (var phase = 0; phase < 3; phase++)
                        {
                            if (IsArea(i)) PhysicsServer.AreaSetSpace(ids[i], default); else PhysicsServer.BodySetSpace(ids[i], default);
                            using var target = new Node(); Set(i, 0, target, oldCanvas, 1);
                            var failure = new ApplicationException("initial-metadata");
                            Action hook = () =>
                            {
                                Reject<InvalidOperationException>(() => target.Dispose());
                                Reject<InvalidOperationException>(() => Set(i, 0, target, oldCanvas, 1));
                                Authored(i, target, oldCanvas, 1); throw failure;
                            };
                            if (phase == 0) solver.ColliderFactoryHook = id => { if (id == ids[i]) hook(); };
                            else if (phase == 1) solver.IdentityHook = id => { if (id == ids[i]) hook(); };
                            else solver.PriorityHook = hook;
                            var errors = ReleaseErrors(() => { if (IsArea(i)) PhysicsServer.AreaSetSpace(ids[i], world.Space); else PhysicsServer.BodySetSpace(ids[i], world.Space); });
                            solver.ColliderFactoryHook = null; solver.IdentityHook = null; solver.PriorityHook = null;
                            Check(errors.Length == 1 && ReferenceEquals(errors[0], failure) && !solver.Bodies.ContainsKey(ids[i]) &&
                                !(IsArea(i) ? PhysicsServer.AreaGetSpace(ids[i]) : PhysicsServer.BodyGetSpace(ids[i])).IsValid(), "Failed initial metadata retires the attachment and preserves its original error");
                            target.Dispose(); Reset(i);
                            if (IsArea(i)) PhysicsServer.AreaSetSpace(ids[i], world.Space); else PhysicsServer.BodySetSpace(ids[i], world.Space);
                            Query(i, first, oldCanvas, 1);
                        }
                rebuilds = 0;
                for (var i = 0; i < 128; i++) Frame(i);
                var x = moving.Position.X; var before = callbacks; Collect();
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame(i);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0 && callbacks == before + 128 && Math.Abs(moving.Position.X - x - 7.68f) < .01f && rebuilds == 0,
                    $"128 metadata edit/full frames: {own}/{all} B, callbacks={callbacks - before}, motion={moving.Position.X - x}, rebuilds={rebuilds}");
                Console.WriteLine($"Metadata transactions {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: {operations} operations, {faults} failures, borrowed targets, dirty repair, real queries and 128 edited full frames {own}/{all} B passed.");
            }
            finally
            {
                if (solver is not null) { solver.IdentityHook = null; solver.PriorityHook = null; solver.ShapeHook = null; solver.ColliderFactoryHook = null; }
                for (var i = 6; i < ids.Length; i++) if (ids[i].IsValid()) PhysicsServer.FreeRID(ids[i]);
            }
            void Set(int i, int operation, Node target, ulong canvas, float priority)
            {
                switch (operation)
                {
                    case 0: if (IsArea(i)) PhysicsServer.AreaAttachObject(ids[i], target); else PhysicsServer.BodyAttachObject(ids[i], target); break;
                    case 1: if (IsArea(i)) PhysicsServer.AreaAttachCanvasInstanceID(ids[i], canvas); else PhysicsServer.BodyAttachCanvasInstanceID(ids[i], canvas); break;
                    case 2: if (IsArea(i)) nodes[i]!.CollisionPriority = priority; else PhysicsServer.BodySetCollisionPriority(ids[i], priority); break;
                    default: nodes[i]!.CollisionPriority = priority; break;
                }
            }
            void Reset(int i)
            {
                Set(i, 0, first, oldCanvas, 1); Set(i, 1, first, oldCanvas, 1);
                if (!IsArea(i) || nodes[i] is not null) Set(i, 2, first, oldCanvas, 1);
            }
            void Authored(int i, Node target, ulong canvas, float priority)
            {
                Check((IsArea(i) ? PhysicsServer.AreaGetObjectInstanceID(ids[i]) : PhysicsServer.BodyGetObjectInstanceID(ids[i])) == target.InstanceID &&
                    (IsArea(i) ? PhysicsServer.AreaGetCanvasInstanceID(ids[i]) : PhysicsServer.BodyGetCanvasInstanceID(ids[i])) == canvas, "Committed identity/canvas authoring remains readable");
                if (!IsArea(i)) Check(PhysicsServer.BodyGetCollisionPriority(ids[i]) == priority, "Committed body priority remains readable");
                else if (nodes[i] is { } scene) Check(scene.CollisionPriority == priority, "Inherited Area priority retains authored state");
            }
            void Query(int i, Node target, ulong canvas, float priority)
            {
                point.Position = new(i * 100, 0); point.CanvasInstanceID = canvas;
                var found = world.DirectSpaceState.IntersectPoint(point, hits);
                Check(found == (i == 4 ? 0 : 1) && (found == 0 || hits[0].ColliderRID == ids[i] && hits[0].ColliderID == target.InstanceID && hits[0].ShapeIndex == 1), "Repaired metadata reaches actual filtered queries and logical indices");
                if (solver is not null) Check(solver.Bodies[ids[i]].ObjectID == target.InstanceID && solver.Bodies[ids[i]].Canvas == canvas &&
                    (IsArea(i) && nodes[i] is null || solver.Bodies[ids[i]].Priority == priority), "The selected implementation receives the complete current metadata");
            }
            void Frame(int frame)
            {
                var target = (frame & 1) == 0 ? first : second; var canvas = (frame & 1) == 0 ? oldCanvas : newCanvas; var priority = (frame & 1) == 0 ? 1 : 3;
                for (var i = 0; i < ids.Length; i++)
                {
                    Set(i, 0, target, canvas, priority); Set(i, 1, target, canvas, priority);
                    if (!IsArea(i) || nodes[i] is not null) Set(i, 2, target, canvas, priority);
                }
                tree.PhysicsFrame(.01);
                for (var i = 0; i < ids.Length; i++) Query(i, target, canvas, priority);
            }
            static bool IsArea(int i) => i is 5 or 7;
            static int OperationCount(int i) => i == 7 ? 2 : i is 5 or 6 ? 3 : 4;
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
