using Electron2D;

public static partial class BackendChecks
{
    public static void RunSharedData(bool gpu = false, bool threaded = false, bool reverse = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("SharedData", () => solver = new());
        var worlds = new World[3]; var roots = new SubViewport[3]; var trees = new SceneTree[3];
        var colliders = new RID[3, 4]; var observers = new RID[3, 4]; var nodes = new CollisionObject?[3, 4];
        var moving = new RigidBody[3]; var events = new int[3, 4]; var forces = new int[3];
        using var disk = new CircleShape { Radius = .5f };
        using var original = new CircleShape { Radius = 3 }; using var replacement = new CircleShape { Radius = 8 };
        using var procedural = new ProceduralDataShape(ShapeGeometry.Circle(8));
        using var point = new PhysicsPointQueryParameters { CollisionMask = 1, CollideWithAreas = true };
        var hits = new PhysicsPointResult[4]; var shape = PhysicsServer.CircleShapeCreate(); var faults = 0;
        try
        {
            PhysicsServer.ShapeSetData(shape, original);
            for (var i = 0; i < 3; i++)
            {
                var index = reverse ? 2 - i : i;
                var world = worlds[index] = index == 2 ? new World("SharedData") : new World(gpu && index == 1 ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU);
                Check(world.PhysicsBackend == (index == 2 ? PhysicsServer.Backend.Extension : gpu && index == 1 ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU),
                    "Each shared-resource world uses its actual explicit implementation");
                roots[index] = new SubViewport { World = world };
                moving[index] = new RigidBody
                {
                    Position = new(0, -200),
                    LinearVelocity = new(6, 0),
                    GravityScale = 0,
                    CanSleep = false,
                    CollisionLayer = 0,
                    LinearDampMode = RigidBody.DampMode.Replace,
                    AngularDampMode = RigidBody.DampMode.Replace
                };
                moving[index].AddChild(new CollisionShape { Shape = disk }); roots[index].AddChild(moving[index]);
                nodes[index, 0] = new StaticBody { CollisionMask = 0 }; nodes[index, 1] = new Area();
                for (var role = 0; role < 2; role++)
                {
                    nodes[index, role]!.Position = Origin(role); roots[index].AddChild(nodes[index, role]!);
                }
                trees[index] = new SceneTree(roots[index]);
                for (var role = 0; role < 4; role++)
                {
                    var area = IsArea(role);
                    var id = colliders[index, role] = nodes[index, role]?.GetRID() ?? (area ? PhysicsServer.AreaCreate() : PhysicsServer.BodyCreate());
                    if (role >= 2)
                    {
                        if (area) PhysicsServer.AreaSetTransform(id, new(0, Origin(role)));
                        else { PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetTransform(id, new(0, Origin(role))); }
                    }
                    if (area)
                    {
                        PhysicsServer.AreaAddShape(id, shape, new(0, new Vector2(-10, 0))); PhysicsServer.AreaAddShape(id, shape, disabled: true);
                        PhysicsServer.AreaAddShape(id, shape, new(0, new Vector2(10, 0))); PhysicsServer.AreaSetMonitorable(id, true);
                        if (role >= 2) PhysicsServer.AreaSetSpace(id, world.Space);
                    }
                    else
                    {
                        PhysicsServer.BodyAddShape(id, shape, new(0, new Vector2(-10, 0))); PhysicsServer.BodyAddShape(id, shape, disabled: true);
                        PhysicsServer.BodyAddShape(id, shape, new(0, new Vector2(10, 0))); PhysicsServer.BodySetCollisionMask(id, 0);
                        if (role >= 2) PhysicsServer.BodySetSpace(id, world.Space);
                    }
                    var receiver = observers[index, role] = PhysicsServer.AreaCreate();
                    PhysicsServer.AreaSetCollisionLayer(receiver, 0); PhysicsServer.AreaSetTransform(receiver, new(0, Origin(role) + new Vector2(12, 0)));
                    PhysicsServer.AreaAddShape(receiver, disk.GetRID()); PhysicsServer.AreaSetSpace(receiver, world.Space);
                    var capturedWorld = index; var capturedRole = role;
                    Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int> callback = (_, rid, _, remote, local) =>
                    {
                        Check(rid == colliders[capturedWorld, capturedRole] && remote == 2 && local == 0,
                            "Shared replacement preserves exact receiver identity and disabled logical gap"); events[capturedWorld, capturedRole]++;
                    };
                    if (area) PhysicsServer.AreaSetAreaMonitorCallback(receiver, callback); else PhysicsServer.AreaSetMonitorCallback(receiver, callback);
                }
                PhysicsServer.BodySetForceIntegrationCallback(moving[index].GetRID(), _ => forces[index]++);
                trees[index].PhysicsFrame(1d / 60);
            }
            foreach (var count in events) Check(count == 1, "Every shared-resource receiver observed its exact initial entry");
            var statusCalls = 0;
            solver!.StatusHook = () => { GuardParticipants(); statusCalls++; };
            try { PhysicsServer.ShapeSetData(shape, original); } finally { solver.StatusHook = null; }
            Check(statusCalls > 0, "Shared replacement validates selected status while every participant is borrowed");
            solver.StatusHook = () => { GuardParticipants(); statusCalls++; };
            try { using var snapshot = PhysicsServer.ShapeGetData(shape); } finally { solver.StatusHook = null; }
            var rejectedViews = new List<Shape>();
            foreach (var data in new Shape[] { replacement, procedural })
                for (var retries = 0; retries < 2; retries++)
                    for (var retirement = 0; retirement < 2; retirement++)
                    {
                        var views = Views(); var before = EventCount(); var ticks = worlds.Select(w => PhysicsServer.SpaceGetTick(w.Space)).ToArray();
                        var apply = new ApplicationException("shared-data-apply"); var rollback = new ApplicationException("shared-data-rollback");
                        var retire = new ApplicationException("shared-data-rejected-retirement"); var attempts = 0; Shape? rejected = null;
                        solver!.ShapeHook = id =>
                        {
                            if (id != colliders[2, 3]) return;
                            GuardParticipants();
                            attempts++;
                            if (attempts == 1)
                            {
                                rejected = View(0); rejectedViews.Add(rejected);
                                rejected.Disposed += _ => { GuardParticipants(); if (retirement != 0) throw retire; };
                            }
                            if (attempts == 1 || attempts == 2 && retries != 0)
                            {
                                solver.Bodies[id].Shapes = 0; Array.Clear(solver.Bodies[id].Parts);
                                throw attempts == 1 ? apply : rollback;
                            }
                        };
                        var errors = ReleaseErrors(() => PhysicsServer.ShapeSetData(shape, data)); solver.ShapeHook = null;
                        Check(errors.Contains(apply) && errors.Contains(rollback) == (retries != 0) && errors.Contains(retire) == (retirement != 0) &&
                            errors.Length == 1 + retries + retirement,
                            $"Shared apply/rollback/retirement preserve exactly the original errors: {string.Join(" | ", errors.Select(error => error.Message))}");
                        Check(rejected is { IsDisposed: true } && Views().Zip(views).All(pair => ReferenceEquals(pair.First, pair.Second) && !pair.First.IsDisposed),
                            "Failed multiworld data replacement preserves every original private scene view and retires its rejected copy");
                        Query(false);
                        for (var i = 0; i < 3; i++) Check(PhysicsServer.SpaceGetTick(worlds[i].Space) == ticks[i], "Failed shared authoring does not advance a numerical interval");
                        Frame(false); Check(EventCount() == before, "Failure repair preserves sensor histories without duplicate effects"); faults++;
                    }
            foreach (var data in new Shape[] { replacement, procedural })
            {
                var before = EventCount(); var previous = View(0); var retired = false;
                previous.Disposed += _ =>
                {
                    nodes[0, 0]!.CollisionLayer = 1; PhysicsServer.AreaSetCollisionMask(colliders[1, 3], 1); retired = true;
                };
                solver!.ShapeHook = _ => GuardParticipants();
                PhysicsServer.ShapeSetData(shape, data); solver.ShapeHook = null;
                Check(retired && previous.IsDisposed, "Committed retirement callbacks retain ordinary authored mutation access");
                Query(true); Frame(true); Check(EventCount() == before, "Accepted data reaches every selected implementation without replaying stable sensor effects");
                PhysicsServer.ShapeSetData(shape, original); Frame(false);
            }
            Check(rejectedViews.All(view => view.IsDisposed), "Every rejected copy has completed retirement");
            for (var i = 0; i < 128; i++) Frame(false);
            var positions = moving.Select(body => body.Position.X).ToArray(); var starts = forces.ToArray(); var ids = moving.Select(body => body.GetRID()).ToArray();
            Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame(false);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Shared worlds full steps: {owner}/{all} bytes");
            for (var i = 0; i < 3; i++) Check(moving[i].GetRID() == ids[i] && forces[i] == starts[i] + 128 &&
                Math.Abs(moving[i].Position.X - positions[i] - 12.8f) < .01f, "All three worlds retain actual motion, callback counts and RID through warm steps");
            Console.WriteLine($"Shared shape data gpu={gpu}, threaded={threaded}, reverse={reverse}: twelve roles, {faults} partial failures, both input forms, exact rollback/history; 128 groups of three real steps {owner}/{all} B.");

            void GuardParticipants()
            {
                for (var i = 0; i < 3; i++)
                {
                    var index = i;
                    var denied = false;
                    try { nodes[index, 0]!.CollisionLayer = 1; } catch (InvalidOperationException) { denied = true; }
                    Check(denied,
                        $"Shared replacement numerical hook must borrow participating world {index} before authored writes");
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetCollisionMask(colliders[index, 2], 0));
                    Reject<InvalidOperationException>(() => PhysicsServer.AreaSetCollisionMask(colliders[index, 3], 1));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(colliders[index, 2], default));
                    Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(colliders[index, 3]));
                    Reject<InvalidOperationException>(() => trees[index].Dispose());
                }
            }
            Shape View(int world) { var scene = nodes[world, 0]!; return scene.ShapeOwnerGetShape(scene.ShapeFindOwner(2), 0); }
            Shape[] Views() => Enumerable.Range(0, 3).Select(View).ToArray();
            int EventCount() { var sum = 0; foreach (var count in events) sum += count; return sum; }
            void Query(bool expanded)
            {
                for (var i = 0; i < 3; i++)
                    for (var role = 0; role < 4; role++)
                        for (var probe = 0; probe < 2; probe++)
                        {
                            point.Position = Origin(role) + new Vector2(probe == 0 ? 12 : 16, 0);
                            var count = worlds[i].DirectSpaceState.IntersectPoint(point, hits); var expected = probe == 0 || expanded ? 1 : 0;
                            Check(count == expected && (count == 0 || hits[0].ColliderRID == colliders[i, role] && hits[0].ShapeIndex == 2),
                                "Shared numerical geometry is exact in every scene/raw body/Area after commit or rollback");
                        }
            }
            void Frame(bool expanded) { Query(expanded); foreach (var tree in trees) tree.PhysicsFrame(1d / 60); }
        }
        finally
        {
            if (solver is not null) { solver.ShapeHook = null; solver.StatusHook = null; }
            for (var i = 0; i < 3; i++)
                for (var role = 0; role < 4; role++)
                {
                    if (observers[i, role] != default) PhysicsServer.FreeRID(observers[i, role]);
                    if (role >= 2 && colliders[i, role] != default) PhysicsServer.FreeRID(colliders[i, role]);
                }
            PhysicsServer.FreeRID(shape);
            foreach (var tree in trees) tree?.Dispose(); foreach (var world in worlds) world?.Dispose();
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old);
        }
        static bool IsArea(int role) => (role & 1) != 0;
        static Vector2 Origin(int role) => new(0, role * 50);
    }
}
