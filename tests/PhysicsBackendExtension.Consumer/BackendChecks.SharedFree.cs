using Electron2D;

public static partial class BackendChecks
{
    public static void RunSharedFree(bool gpu = false, bool threaded = false, bool reverse = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("SharedFree", () => solver = new());
        var worlds = new World[3]; var trees = new SceneTree[3];
        var colliders = new RID[3, 4]; var observers = new RID[3, 4]; var nodes = new CollisionObject[3, 2];
        var added = new int[3, 4]; var removed = new int[3, 4]; var moving = new RigidBody[3]; var forces = new int[3];
        using var disk = new CircleShape { Radius = .5f }; using var data = new CircleShape { Radius = 3 };
        using var spareWorld = new World(PhysicsServer.Backend.CPU);
        var spareBody = PhysicsServer.BodyCreate(); var spareArea = PhysicsServer.AreaCreate();
        var spareRoot = new SubViewport { World = spareWorld }; var spareSceneBody = new StaticBody(); var spareSceneArea = new Area();
        spareRoot.AddChild(spareSceneBody); spareRoot.AddChild(spareSceneArea); using var spareTree = new SceneTree(spareRoot);
        var owner = spareSceneArea.CreateShapeOwner(null); var child = new CollisionShape { Shape = disk }; spareSceneBody.AddChild(child);
        using var point = new PhysicsPointQueryParameters { CollisionMask = 1, CollideWithAreas = true };
        var hits = new PhysicsPointResult[4]; RID shape = default; RID guarded = default; Shape? view = null; var guardCalls = 0;
        try
        {
            PhysicsServer.BodyAddShape(spareBody, disk.GetRID()); PhysicsServer.BodySetSpace(spareBody, spareWorld.Space);
            PhysicsServer.AreaAddShape(spareArea, disk.GetRID()); PhysicsServer.AreaSetSpace(spareArea, spareWorld.Space);
            for (var i = 0; i < 3; i++)
            {
                var index = reverse ? 2 - i : i;
                var world = worlds[index] = index == 2 ? new World("SharedFree") : new World(gpu && index == 1 ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU);
                Check(world.PhysicsBackend == (index == 2 ? PhysicsServer.Backend.Extension : gpu && index == 1 ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU), "Shared release uses each explicitly selected implementation");
                var root = new SubViewport { World = world };
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
                moving[index].AddChild(new CollisionShape { Shape = disk }); root.AddChild(moving[index]);
                nodes[index, 0] = new StaticBody { Position = Origin(0), CollisionMask = 0 }; nodes[index, 1] = new Area { Position = Origin(1) };
                root.AddChild(nodes[index, 0]); root.AddChild(nodes[index, 1]); trees[index] = new SceneTree(root);
                for (var role = 0; role < 4; role++)
                {
                    var area = IsArea(role); var id = colliders[index, role] = role < 2 ? nodes[index, role].GetRID() : area ? PhysicsServer.AreaCreate() : PhysicsServer.BodyCreate();
                    if (area)
                    {
                        if (role >= 2) PhysicsServer.AreaSetTransform(id, new(0, Origin(role)));
                        PhysicsServer.AreaAddShape(id, disk.GetRID(), new(0, new Vector2(30, 0))); PhysicsServer.AreaSetMonitorable(id, true);
                        if (role >= 2) PhysicsServer.AreaSetSpace(id, world.Space);
                    }
                    else
                    {
                        if (role >= 2) { PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetTransform(id, new(0, Origin(role))); }
                        PhysicsServer.BodyAddShape(id, disk.GetRID(), new(0, new Vector2(30, 0))); PhysicsServer.BodySetCollisionMask(id, 0);
                        if (role >= 2) PhysicsServer.BodySetSpace(id, world.Space);
                    }
                    var receiver = observers[index, role] = PhysicsServer.AreaCreate();
                    PhysicsServer.AreaSetCollisionLayer(receiver, 0); PhysicsServer.AreaSetTransform(receiver, new(0, Origin(role) + new Vector2(12, 0)));
                    PhysicsServer.AreaAddShape(receiver, disk.GetRID()); PhysicsServer.AreaSetSpace(receiver, world.Space);
                    var capturedWorld = index; var capturedRole = role;
                    Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int> callback = (status, rid, _, remote, local) =>
                    {
                        Check(rid == colliders[capturedWorld, capturedRole] && remote == 3 && local == 0, "Shared release keeps exact removed slot and collider identities");
                        if (status == PhysicsServer.AreaBodyStatus.Added) added[capturedWorld, capturedRole]++; else removed[capturedWorld, capturedRole]++;
                    };
                    if (area) PhysicsServer.AreaSetAreaMonitorCallback(receiver, callback); else PhysicsServer.AreaSetMonitorCallback(receiver, callback);
                }
                PhysicsServer.BodySetForceIntegrationCallback(moving[index].GetRID(), _ => forces[index]++);
            }
            for (var cycle = 0; cycle < 4; cycle++)
            {
                guarded = shape = PhysicsServer.CircleShapeCreate(); PhysicsServer.ShapeSetData(shape, data);
                for (var i = 0; i < 3; i++)
                    for (var role = 0; role < 4; role++)
                    {
                        var id = colliders[i, role];
                        if (IsArea(role))
                        {
                            PhysicsServer.AreaAddShape(id, shape, new(0, new Vector2(-10, 0))); PhysicsServer.AreaAddShape(id, shape, disabled: true);
                            PhysicsServer.AreaAddShape(id, shape, new(0, new Vector2(10, 0)));
                        }
                        else
                        {
                            PhysicsServer.BodyAddShape(id, shape, new(0, new Vector2(-10, 0))); PhysicsServer.BodyAddShape(id, shape, disabled: true);
                            PhysicsServer.BodyAddShape(id, shape, new(0, new Vector2(10, 0)));
                        }
                    }
                view = nodes[0, 0].ShapeOwnerGetShape(nodes[0, 0].ShapeFindOwner(3), 0);
                Frame(true); Events(cycle + 1, cycle);
                var ticks = worlds.Select(w => PhysicsServer.SpaceGetTick(w.Space)).ToArray(); var statusFailure = new ApplicationException("free-shared-status");
                solver!.StatusHook = () => { Guard(); throw statusFailure; };
                var statusErrors = ReleaseErrors(() => PhysicsServer.FreeRID(shape)); solver.StatusHook = null;
                Check(statusErrors.Length == 1 && ReferenceEquals(statusErrors[0], statusFailure) && !view.IsDisposed, $"Failed release preflight preserves exact status error and live resource: {string.Join(" | ", statusErrors.Select(error => error.Message))}; spare slots={PhysicsServer.BodyGetShapeCount(spareBody)}");
                Query(true);
                for (var i = 0; i < 3; i++) Check(PhysicsServer.SpaceGetTick(worlds[i].Space) == ticks[i], "Failed release preflight does not advance physics");
                Frame(true); Events(cycle + 1, cycle);
                var apply = new ApplicationException("free-shared-rebuild"); var retire = new ApplicationException("free-shared-retirement");
                var failApply = (cycle & 1) != 0; var failRetire = (cycle & 2) != 0; var retired = 0;
                solver.ShapeHook = id =>
                {
                    Guard();
                    if (id == colliders[2, 3] && failApply)
                    {
                        solver.Bodies[id].Shapes = 0; Array.Clear(solver.Bodies[id].Parts); throw apply;
                    }
                };
                view.Disposed += _ => { Guard(); retired++; if (failRetire) throw retire; };
                Exception[] errors = [];
                if (failApply || failRetire) errors = ReleaseErrors(() => PhysicsServer.FreeRID(shape)); else PhysicsServer.FreeRID(shape);
                solver.ShapeHook = null; var released = shape; shape = default;
                Check(errors.Length == (failApply ? 1 : 0) + (failRetire ? 1 : 0) && errors.Contains(apply) == failApply && errors.Contains(retire) == failRetire,
                    "Committed release preserves exactly the original rebuild and retirement failures");
                Check(retired == 1 && view.IsDisposed, "Shared private geometry retires exactly once despite cleanup errors");
                Reject<ArgumentException>(() => PhysicsServer.ShapeGetType(released)); Reject<ArgumentException>(() => PhysicsServer.FreeRID(released));
                for (var i = 0; i < 3; i++)
                    for (var role = 0; role < 4; role++)
                    {
                        var id = colliders[i, role]; var area = IsArea(role);
                        Check((area ? PhysicsServer.AreaGetShapeCount(id) : PhysicsServer.BodyGetShapeCount(id)) == 1 &&
                            (area ? PhysicsServer.AreaGetShape(id, 0) : PhysicsServer.BodyGetShape(id, 0)) == disk.GetRID(), "Release removes every matching slot and preserves the exact unrelated slot");
                    }
                nodes[0, 0].CollisionLayer = 1; PhysicsServer.AreaSetCollisionMask(colliders[1, 3], 1);
                Query(false); Frame(false); Events(cycle + 1, cycle + 1); Frame(false); Events(cycle + 1, cycle + 1);
                PhysicsServer.BodyAddShape(spareBody, disk.GetRID(), disabled: true); PhysicsServer.BodyRemoveShape(spareBody, 1);
            }
            for (var i = 0; i < 128; i++) Frame(false);
            var positions = moving.Select(body => body.Position.X).ToArray(); var starts = forces.ToArray(); var ids = moving.Select(body => body.GetRID()).ToArray();
            Collect(); var ownerBytes = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame(false);
            ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownerBytes == 0 && all == 0, $"Shared release full physical intervals allocate {ownerBytes}/{all} bytes"); Events(4, 4);
            for (var i = 0; i < 3; i++) Check(moving[i].GetRID() == ids[i] && forces[i] == starts[i] + 128 && Math.Abs(moving[i].Position.X - positions[i] - 12.8f) < .01f,
                "All actual implementations preserve awake motion, RID and full-step callbacks after shared release");
            Console.WriteLine($"Shared release gpu={gpu}, threaded={threaded}, reverse={reverse}: twelve roles, four cleanup combinations, exact histories, {guardCalls} guards; 128 groups of three real steps {ownerBytes}/{all} B.");

            void Guard()
            {
                if (view!.IsDisposed)
                {
                    Reject<ArgumentException>(() => PhysicsServer.BodyAddShape(spareBody, guarded));
                    Reject<ArgumentException>(() => PhysicsServer.AreaSetShape(spareArea, 0, guarded));
                    Reject<ObjectDisposedException>(() => spareSceneArea.ShapeOwnerAddShape(owner, view));
                    Reject<ObjectDisposedException>(() => child.Shape = view);
                }
                else
                {
                    Reject<InvalidOperationException>(() => PhysicsServer.BodyAddShape(spareBody, guarded));
                    Reject<InvalidOperationException>(() => PhysicsServer.AreaAddShape(spareArea, guarded, disabled: true));
                    Reject<InvalidOperationException>(() => PhysicsServer.AreaSetShape(spareArea, 0, guarded));
                    Reject<InvalidOperationException>(() => spareSceneArea.ShapeOwnerAddShape(owner, view));
                    Reject<InvalidOperationException>(() => child.Shape = view);
                }
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(guarded));
                for (var i = 0; i < 3; i++)
                {
                    var index = i;
                    Reject<InvalidOperationException>(() => nodes[index, 0].CollisionLayer = 1);
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(colliders[index, 2], default));
                    Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(colliders[index, 3]));
                    Reject<InvalidOperationException>(() => trees[index].Dispose());
                }
                PhysicsServer.BodySetCollisionMask(spareBody, 0);
                Check(PhysicsServer.BodyGetShapeCount(spareBody) == 1 && PhysicsServer.AreaGetShapeCount(spareArea) == 1 &&
                    spareSceneArea.ShapeOwnerGetShapeCount(owner) == 0 && ReferenceEquals(child.Shape, disk), "Rejected release reentry leaves unrelated consumers unchanged");
                guardCalls++;
            }
            void Events(int expectedAdded, int expectedRemoved)
            { for (var i = 0; i < 3; i++) for (var role = 0; role < 4; role++) Check(added[i, role] == expectedAdded && removed[i, role] == expectedRemoved, "Each original receiver observes exactly one entry/exit without duplicate effects"); }
            void Query(bool present)
            {
                for (var i = 0; i < 3; i++)
                    for (var role = 0; role < 4; role++)
                        for (var probe = 0; probe < 2; probe++)
                        {
                            point.Position = Origin(role) + new Vector2(probe == 0 ? 12 : 30, 0);
                            var count = worlds[i].DirectSpaceState.IntersectPoint(point, hits); var expected = probe != 0 || present ? 1 : 0;
                            Check(count == expected && (count == 0 || hits[0].ColliderRID == colliders[i, role] && hits[0].ShapeIndex == (probe == 0 ? 3 : 0)), "Queries retry partial removal and retain the exact unrelated numerical fixture");
                        }
            }
            void Frame(bool present) { Query(present); foreach (var tree in trees) tree.PhysicsFrame(1d / 60); }
        }
        finally
        {
            if (solver is not null) { solver.ShapeHook = null; solver.StatusHook = null; }
            if (shape != default) PhysicsServer.FreeRID(shape);
            for (var i = 0; i < 3; i++)
                for (var role = 0; role < 4; role++)
                {
                    if (observers[i, role] != default) PhysicsServer.FreeRID(observers[i, role]);
                    if (role >= 2 && colliders[i, role] != default) PhysicsServer.FreeRID(colliders[i, role]);
                }
            PhysicsServer.FreeRID(spareBody); PhysicsServer.FreeRID(spareArea);
            foreach (var tree in trees) tree?.Dispose(); foreach (var world in worlds) world?.Dispose();
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old);
        }
        static bool IsArea(int role) => (role & 1) != 0;
        static Vector2 Origin(int role) => new(0, role * 50);
    }
}
