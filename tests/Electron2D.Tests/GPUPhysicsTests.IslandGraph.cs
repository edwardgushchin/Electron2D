using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

internal static partial class GPUPhysicsTests
{
    internal static void VerifyIslandGraph()
    {
        using var gpu = new GPUPhysicsWorld();
        VerifyContactRemovals(true);
        VerifyIslandGraphFailure();
        VerifyGraphShaderFailure(gpu);
        VerifyGraphReadbackLiveness(gpu);
        VerifyConstraintColorWake(gpu);
        VerifyConstraintColorFailure(gpu);
        foreach (var sleeping in new[] { false, true })
        {
            B2WorldId Create()
            {
                var wd = b2DefaultWorldDef(); wd.gravity = new(0, 0);
                var world = b2CreateWorld(wd);
                var bd = b2DefaultBodyDef(); bd.type = B2BodyType.b2_dynamicBody;
                var sd = b2DefaultShapeDef(); sd.enableContactEvents = true;
                var bodies = new B2BodyId[16];
                for (var i = 0; i < bodies.Length; i++)
                {
                    bd.position = new(1.99f * i, 0); bodies[i] = b2CreateBody(world, bd);
                    b2CreateCircleShape(bodies[i], sd, new B2Circle { radius = 1 });
                }
                // Unequal pre-existing joint islands test size-based winners and joint list concatenation.
                SplitJoint(world, bodies[1], bodies[2], 0); SplitJoint(world, bodies[2], bodies[3], 0);
                SplitJoint(world, bodies[5], bodies[6], 0);
                SplitJoint(world, bodies[8], bodies[9], 0); SplitJoint(world, bodies[9], bodies[10], 0);
                if (sleeping) for (var i = 1; i < bodies.Length; i++) b2Body_SetAwake(bodies[i], false);
                return world;
            }
            var c = Create(); var g = Create(); var cpu = b2GetWorldFromId(c); var actual = b2GetWorldFromId(g);
            gpu.EnableIslandChanges(actual); gpu.EnableIslandSplitting(actual); gpu.EnableConstraintColors(actual);
            void Step() { b2World_Step(c, 1f / 60, 4); b2World_Step(g, 1f / 60, 4); CompareIslands(cpu, actual); }
            try
            {
                var merges = gpu.MergedIslandCount;
                for (var i = 0; i < 20; i++) Step();
                if (gpu.MergedIslandCount == merges) throw new Exception("The joint/sleep fixture did not merge islands.");
                // Separate the last endpoint inside its fat overlap to exercise stopped-touching without deletion.
                var before = gpu.IslandChangeCount;
                var contact = actual.contacts.data.Take(actual.contacts.count).First(c => c.contactId >= 0 &&
                    (c.edges[0].bodyId == 14 && c.edges[1].bodyId == 15 || c.edges[0].bodyId == 15 && c.edges[1].bodyId == 14));
                var contactID = contact.contactId; var generation = contact.generation;
                var last = b2MakeBodyId(cpu, 15); var other = b2MakeBodyId(actual, 15);
                var position = b2Body_GetPosition(b2MakeBodyId(cpu, 14)); position.X += 2.1f;
                b2Body_SetTransform(last, position, new(1, 0)); b2Body_SetTransform(other, position, new(1, 0));
                b2Body_SetLinearVelocity(last, new(0, 0)); b2Body_SetLinearVelocity(other, new(0, 0)); Step();
                if (gpu.IslandChangeCount == before || contact.contactId != contactID || contact.generation != generation || contact.islandId != -1)
                    throw new Exception("The stopped-touching fixture must unlink without deleting its contact.");
                b2Body_SetAwake(last, false); b2Body_SetAwake(other, false); CompareIslands(cpu, actual);
                b2Body_SetAwake(last, true); b2Body_SetAwake(other, true); CompareIslands(cpu, actual);
                using (var replacement = new GPUPhysicsWorld())
                {
                    replacement.EnableIslandChanges(actual); replacement.EnableConstraintColors(actual);
                    var coloring = actual.selectConstraintColor; var callback = actual.beginIslandChanges; var observer = actual.islandGraphChanged;
                    gpu.EnableIslandChanges(cpu); gpu.EnableConstraintColors(cpu);
                    if (actual.selectConstraintColor != coloring) throw new Exception("Color rebinding replaced another host callback.");
                    if (actual.beginIslandChanges != callback || actual.islandGraphChanged != observer) throw new Exception("Graph rebinding replaced another host's callback.");
                }
                if (actual.beginConstraintColors is not null || actual.finishConstraintColors is not null || actual.selectConstraintColor is not null ||
                    actual.islandGraphChanged is not null || actual.beginIslandChanges is not null || actual.changeContactIsland is not null || actual.finishIslandChanges is not null)
                    throw new Exception("Graph disposal retained a callback.");
            }
            finally { b2DestroyWorld(c); b2DestroyWorld(g); }
        }
        Console.WriteLine("GPU island graph matches ordered CPU merges, removals, joint lists, sleeping-set wakeup, ID reuse and zero warmed allocations.");
    }
    private static void VerifyGraphShaderFailure(GPUPhysicsWorld gpu)
    {
        var worldID = b2CreateWorld(b2DefaultWorldDef()); var world = b2GetWorldFromId(worldID);
        try
        {
            var body = b2DefaultBodyDef(); body.type = B2BodyType.b2_dynamicBody;
            var shape = b2DefaultShapeDef();
            b2CreateCircleShape(b2CreateBody(worldID, body), shape, new B2Circle { radius = 1 });
            b2CreateCircleShape(b2CreateBody(worldID, body), shape, new B2Circle { radius = 1 });
            gpu.EnableIslandChanges(world); var begin = world.beginIslandChanges;
            world.beginIslandChanges = w => { w.bodies.data[0].islandId = w.islands.count; begin(w); };
            try { b2World_Step(worldID, 1f / 60, 4); throw new Exception("Malformed graph metadata reached publication."); }
            catch (InvalidOperationException error) when (error.Message.StartsWith("GPU island graph error", StringComparison.Ordinal)) { }
        }
        finally
        {
            world.locked = false; foreach (var arena in world.arena.AsSpan()) arena.Abort();
            b2DestroyWorld(worldID);
        }
    }

    private static void VerifyGraphReadbackLiveness(GPUPhysicsWorld gpu)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        foreach (var (storageName, id, field, value, message) in new[]
        {
            ("_graphBodies", 3, "Visited", 1, "GPU island member liveness differs."),
            ("_graphJoints", 0, "Visited", 1, "GPU island member liveness differs."),
            ("_graphIslands", 3, "ID", 3, "GPU island liveness differs."),
            ("_graphBodies", 4, "Visited", 0, "GPU island member liveness differs."),
            ("_graphBodies", 4, "Root", 0, "GPU island member unexpectedly joined a component.")
        })
        {
            var worldID = b2CreateWorld(b2DefaultWorldDef()); var world = b2GetWorldFromId(worldID);
            try
            {
                var bd = b2DefaultBodyDef(); bd.type = B2BodyType.b2_dynamicBody;
                var sd = b2DefaultShapeDef();
                for (var i = 0; i < 4; i++)
                {
                    bd.position = new(1.99f * i, 0);
                    b2CreateCircleShape(b2CreateBody(worldID, bd), sd, new B2Circle { radius = 1 });
                }
                b2CreateBody(worldID, b2DefaultBodyDef()); // A live static body belongs to no island.
                b2DestroyBody(b2MakeBodyId(world, 3));
                B2Joints.b2DestroyJoint(SplitJoint(worldID, b2MakeBodyId(world, 0), b2MakeBodyId(world, 1), 0), true);
                gpu.EnableIslandChanges(world); var begin = world.beginIslandChanges;
                world.beginIslandChanges = w =>
                {
                    begin(w);
                    // Corrupt returned scratch, never the independent CPU expectations or live world.
                    var storage = typeof(GPUPhysicsWorld).GetField(storageName, flags)!.GetValue(gpu)!;
                    var data = (Array)storage.GetType().GetField("Data", flags)!.GetValue(storage)!;
                    var record = data.GetValue(id)!; record.GetType().GetField(field, flags)!.SetValue(record, value); data.SetValue(record, id);
                    Array.Clear((bool[])typeof(GPUPhysicsWorld).GetField("_graphFreed", flags)!.GetValue(gpu)!);
                    typeof(GPUPhysicsWorld).GetMethod("ValidateIslandChanges", flags)!.Invoke(gpu, [w]);
                    throw new Exception("Corrupted graph liveness reached publication.");
                };
                try { b2World_Step(worldID, 1f / 60, 4); throw new Exception("The readback fixture did not validate a graph batch."); }
                catch (System.Reflection.TargetInvocationException e) when (e.InnerException is InvalidOperationException error && error.Message == message) { }
            }
            finally
            {
                world.locked = false; foreach (var arena in world.arena.AsSpan()) arena.Abort();
                b2DestroyWorld(worldID);
            }
        }
    }

    private static void VerifyIslandGraphFailure()
    {
        using var shape = new CircleShape { Radius = 12 };
        using var root = new SubViewport();
        var first = new RigidBody { Name = "First" };
        var second = new RigidBody { Name = "Second", Position = new(18, 0) };
        var third = new RigidBody { Name = "Third", Position = new(36, 0) };
        foreach (var body in new[] { first, second, third }) { body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body); }
        var joint = new PinJoint { Name = "Pin", NodeA = "../Second", NodeB = "../Third" }; root.AddChild(joint);
        var area = new Area { Name = "Sensor" }; area.AddChild(new CollisionShape { Shape = shape }); root.AddChild(area);
        using var tree = new SceneTree(root);
        var space = first.Space!; space.EnableGPUSolver(); var native = b2GetWorldFromId(space.WorldID);
        var apply = native.changeContactIsland; var failed = false;
        native.changeContactIsland = (w, c, link) =>
        {
            var free = w.islandIdPool.freeArray.count; var result = apply(w, c, link);
            if (w.islandIdPool.freeArray.count > free) { failed = true; throw new IOException("injected partial island merge"); }
            return result;
        };
        try { space.Step(1d / 60); throw new Exception("The partial merge failure was not raised."); }
        catch (AggregateException e) when (e.InnerExceptions.Any(x => x is IOException)) { }
        if (!failed) throw new Exception("The failure fixture did not free an island.");
        try { space.Step(1d / 60); throw new Exception("A partial island batch was replayed."); }
        catch (InvalidOperationException e) when (e.InnerException is IOException) { }
        // Detach scene resources individually, then release all raw world storage.
        joint.Dispose(); second.Dispose(); area.Dispose();
        tree.Dispose();

        var spaceRID = PhysicsServer.SpaceCreate(); var shapeRID = PhysicsServer.CircleShapeCreate();
        var bodies = new[] { PhysicsServer.BodyCreate(), PhysicsServer.BodyCreate(), PhysicsServer.BodyCreate() };
        var jointRID = PhysicsServer.JointCreate(); var areaRID = PhysicsServer.AreaCreate();
        try
        {
            for (var i = 0; i < bodies.Length; i++)
            {
                PhysicsServer.BodyAddShape(bodies[i], shapeRID);
                PhysicsServer.BodySetTransform(bodies[i], new(0, Vector2.One, 0, new(12 * i, 0)));
                PhysicsServer.BodySetSpace(bodies[i], spaceRID);
            }
            PhysicsServer.JointMakePin(jointRID, new(12, 0), bodies[1], bodies[2]);
            PhysicsServer.AreaAddShape(areaRID, shapeRID); PhysicsServer.AreaSetSpace(areaRID, spaceRID);
            PhysicsServer.SpaceSetActive(spaceRID, true);
            var serverSpace = PhysicsServer.Service.GetSceneSpace(spaceRID); serverSpace.EnableGPUSolver();
            var serverWorld = b2GetWorldFromId(serverSpace.WorldID); var consume = serverWorld.changeContactIsland;
            serverWorld.changeContactIsland = (w, c, link) =>
            {
                var free = w.islandIdPool.freeArray.count; var result = consume(w, c, link);
                if (w.islandIdPool.freeArray.count > free) throw new IOException("injected partial server island merge");
                return result;
            };
            try { PhysicsServer.SpaceStep(spaceRID, 1d / 60); throw new Exception("The server merge failure was not raised."); }
            catch (AggregateException e) when (e.InnerExceptions.Any(x => x is IOException)) { }
            PhysicsServer.FreeRID(bodies[1]); bodies[1] = default;
            PhysicsServer.FreeRID(areaRID); areaRID = default;
            PhysicsServer.FreeRID(jointRID); jointRID = default;
        }
        finally
        {
            PhysicsServer.FreeRID(spaceRID);
            foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
            if (jointRID.IsValid()) PhysicsServer.FreeRID(jointRID);
            if (areaRID.IsValid()) PhysicsServer.FreeRID(areaRID);
            PhysicsServer.FreeRID(shapeRID);
        }
    }

}
