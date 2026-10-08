using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2BoardPhases;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Islands;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

internal static partial class GPUPhysicsTests
{
    internal static void VerifyIslandSplitting()
    {
        using var gpu = new GPUPhysicsWorld();
        var profiles = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_GPU_ISLANDS_PROFILE") == "1" ? new List<object>() : null;
        foreach (var count in new[] { 1, 2, 63, 64, 65, 257, 1025 }) VerifyIslandSplitting(gpu, count, false, profiles);
        VerifyIslandSplitting(gpu, 1025, true, profiles);
        if (profiles is not null)
        {
            Directory.CreateDirectory("bin/physics-sandbox");
            File.WriteAllText("bin/physics-sandbox/island-split-profile.json", System.Text.Json.JsonSerializer.Serialize(profiles, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        Console.WriteLine("GPU island splitting matches CPU components, DFS lists, ID reuse, static/disabled edges, sleep/wake and owner scheduling with zero warmed bytes.");
    }

    private static B2JointId SplitJoint(B2WorldId world, B2BodyId a, B2BodyId b, int kind)
    {
        var definition = b2DefaultJointDef(); definition.bodyIdA = a; definition.bodyIdB = b; definition.collideConnected = true;
        definition.localFrameB.p = b2Body_GetPosition(a) - b2Body_GetPosition(b);
        if (kind % 3 == 0) { var d = b2DefaultFilterJointDef(); d.@base = definition; return b2CreateFilterJoint(world, d); }
        if (kind % 3 == 1) { var d = b2DefaultRevoluteJointDef(); d.@base = definition; return b2CreateRevoluteJoint(world, d); }
        var wheel = b2DefaultWheelJointDef(); wheel.@base = definition; return b2CreateWheelJoint(world, wheel);
    }

    private static void VerifyIslandSplitting(GPUPhysicsWorld gpu, int count, bool connected, List<object>? profiles)
    {
        using var cpuScheduler = new PhysicsTaskScheduler(4); using var gpuScheduler = new PhysicsTaskScheduler(4);
        (B2WorldId ID, B2BodyId Ground, B2BodyId[] Bodies, B2JointId[] Bridges, int[] Path) Create(PhysicsTaskScheduler scheduler)
        {
            var wd = b2DefaultWorldDef(); wd.gravity = new(0, 0);
            if (count >= 257) { wd.workerCount = 4; wd.enqueueTask = scheduler.Enqueue; wd.finishTask = scheduler.Finish; }
            var id = b2CreateWorld(wd); var world = b2GetWorldFromId(id);
            if (count >= 257) scheduler.Bind(world);
            var bd = b2DefaultBodyDef(); var ground = b2CreateBody(id, bd);
            var sd = b2DefaultShapeDef(); sd.invokeContactCreation = true;
            b2CreatePolygonShape(ground, sd, b2MakeBox(2 * count + 2, .25f));
            var bodies = new B2BodyId[count]; var path = new int[count]; var bridges = new B2JointId[Math.Max(0, count - 1)];
            for (var i = 0; i < count; i++)
            {
                bd.type = count > 2 && i == count / 2 ? B2BodyType.b2_kinematicBody : B2BodyType.b2_dynamicBody;
                bd.position = new(2 * i, i % 5 == 0 ? .82f : .75f);
                bodies[i] = b2CreateBody(id, bd); b2CreateCircleShape(bodies[i], sd, new B2Circle { radius = .5f });
                path[i] = i % 2 == 0 ? i / 2 : count - 1 - i / 2;
            }
            // Alternating merge order makes the seed order differ from the path.
            for (var parity = 0; parity < 2; parity++)
                for (var i = parity; i + 1 < count; i += 2)
                {
                    var joint = SplitJoint(id, bodies[path[i]], bodies[path[i + 1]], i);
                    if (i % 5 == 4) bridges[i] = joint;
                }
            for (var i = 0; i + 4 < count; i += 5)
                SplitJoint(id, bodies[path[i]], bodies[path[i + 4]], i + 1); // cycles inside components
            SplitJoint(id, ground, bodies[0], 0);
            bd.type = B2BodyType.b2_dynamicBody; bd.isEnabled = false;
            var disabled = b2CreateBody(id, bd); SplitJoint(id, bodies[0], disabled, 0);
            b2UpdateBroadPhasePairs(world); b2Collide(new B2StepContext { world = world });
            if (world.userTreeTask is not null)
            {
                world.finishTaskFcn(world.userTreeTask, world.userTaskContext); world.userTreeTask = null!; world.activeTaskCount--;
            }
            B2ArenaAllocators.b2GrowArena(world.arena);
            return (id, ground, bodies, bridges, path);
        }
        var c = Create(cpuScheduler); var g = Create(gpuScheduler);
        var cpu = b2GetWorldFromId(c.ID); var actual = b2GetWorldFromId(g.ID);
        gpu.EnableIslandSplitting(actual);
        void Compare()
        {
            if (cpu.islandIdPool.nextIndex != actual.islandIdPool.nextIndex || cpu.islandIdPool.freeArray.count != actual.islandIdPool.freeArray.count)
                throw new Exception("Island pool counts differ.");
            for (var i = 0; i < cpu.islandIdPool.freeArray.count; i++)
                if (cpu.islandIdPool.freeArray.data[i] != actual.islandIdPool.freeArray.data[i]) throw new Exception("Island free-ID order differs.");
            for (var i = 0; i < cpu.islands.count; i++)
            {
                var a = cpu.islands.data[i]; var b = actual.islands.data[i];
                if (a.islandId != b.islandId || a.setIndex != b.setIndex || a.localIndex != b.localIndex) throw new Exception("Island identity/set differs.");
                if (a.islandId < 0) continue;
                if (a.headBody != b.headBody || a.tailBody != b.tailBody || a.bodyCount != b.bodyCount ||
                    a.headContact != b.headContact || a.tailContact != b.tailContact || a.contactCount != b.contactCount ||
                    a.headJoint != b.headJoint || a.tailJoint != b.tailJoint || a.jointCount != b.jointCount || a.constraintRemoveCount != b.constraintRemoveCount)
                    throw new Exception($"Island list/count differs at {i}/{count}.");
            }
            for (var i = 0; i < cpu.bodies.count; i++)
            {
                var a = cpu.bodies.data[i]; var b = actual.bodies.data[i];
                if (a.islandId != b.islandId || a.islandPrev != b.islandPrev || a.islandNext != b.islandNext || a.setIndex != b.setIndex || a.localIndex != b.localIndex)
                    throw new Exception("Island body order differs.");
            }
            for (var i = 0; i < cpu.contacts.count; i++)
            {
                var a = cpu.contacts.data[i]; var b = actual.contacts.data[i];
                if (a.contactId < 0) continue;
                if (a.islandId != b.islandId || a.islandPrev != b.islandPrev || a.islandNext != b.islandNext || a.setIndex != b.setIndex || a.localIndex != b.localIndex || a.colorIndex != b.colorIndex)
                    throw new Exception("Island contact order differs.");
            }
            for (var i = 0; i < cpu.joints.count; i++)
            {
                var a = cpu.joints.data[i]; var b = actual.joints.data[i];
                if (a.jointId < 0) continue;
                if (a.islandId != b.islandId || a.islandPrev != b.islandPrev || a.islandNext != b.islandNext || a.setIndex != b.setIndex || a.localIndex != b.localIndex || a.colorIndex != b.colorIndex)
                    throw new Exception("Island joint order differs.");
            }
        }
        void Break()
        {
            for (var i = 4; !connected && i < c.Bridges.Length; i += 5) { b2DestroyJoint(c.Bridges[i], true); b2DestroyJoint(g.Bridges[i], true); }
            b2DestroyJoint(SplitJoint(c.ID, c.Ground, c.Bodies[0], 0), true);
            b2DestroyJoint(SplitJoint(g.ID, g.Ground, g.Bodies[0], 0), true);
        }
        void Reconnect()
        {
            for (var i = 4; !connected && i < c.Bridges.Length; i += 5)
            {
                c.Bridges[i] = SplitJoint(c.ID, c.Bodies[c.Path[i]], c.Bodies[c.Path[i + 1]], i);
                g.Bridges[i] = SplitJoint(g.ID, g.Bodies[g.Path[i]], g.Bodies[g.Path[i + 1]], i);
            }
        }
        var measuring = false; double cpuMS = 0, gpuMS = 0;
        void Split()
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            b2SplitIsland(cpu, cpu.bodies.data[c.Bodies[0].index1 - 1].islandId);
            if (measuring) cpuMS += System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            start = System.Diagnostics.Stopwatch.GetTimestamp();
            b2SplitIsland(actual, actual.bodies.data[g.Bodies[0].index1 - 1].islandId);
            if (measuring) gpuMS += System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            Compare();
        }
        void Cycle() { Break(); Split(); Reconnect(); Compare(); B2ArenaAllocators.b2GrowArena(cpu.arena); }
        try
        {
            Compare();
            var beforeCount = gpu.SplitIslandCount;
            Split(); // clean island is a no-op
            if (gpu.SplitIslandCount != beforeCount) throw new Exception("A clean island must not dispatch a split.");
            for (var i = 0; i < 24; i++) Cycle();
            var components = gpu.SplitComponentCount; var batches = gpu.SplitConvergenceBatches;
            measuring = true;
            var bytes = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 16; i++) Cycle();
            bytes = GC.GetTotalAllocatedBytes(true) - bytes;
            measuring = false;
            profiles?.Add(new
            {
                bodies = count,
                connected,
                components = (gpu.SplitComponentCount - components) / 16,
                convergenceBatches = gpu.SplitConvergenceBatches - batches,
                cpuMeanMS = cpuMS / 16,
                gpuMeanMS = gpuMS / 16,
                allThreadsBytes = bytes
            });
            if (bytes != 0 || gpu.SplitIslandCount == beforeCount) throw new Exception($"GPU island split allocated {bytes} warmed bytes or did not execute.");
            Break();
            b2Body_SetAwake(c.Bodies[0], false); b2Body_SetAwake(g.Bodies[0], false); Compare();
            b2Body_SetAwake(c.Bodies[0], true); b2Body_SetAwake(g.Bodies[0], true); Compare(); Reconnect();
            if (count == 257)
            {
                Break();
                cpu.splitIslandId = cpu.bodies.data[c.Bodies[0].index1 - 1].islandId;
                actual.splitIslandId = actual.bodies.data[g.Bodies[0].index1 - 1].islandId;
                var splits = gpu.SplitIslandCount;
                b2World_Step(c.ID, 1f / 60, 4); b2World_Step(g.ID, 1f / 60, 4);
                if (gpu.SplitIslandCount == splits) throw new Exception("The four-worker solver must execute GPU splitting on the owner.");
                Compare();
            }
            using (var other = new GPUPhysicsWorld())
            {
                other.EnableIslandSplitting(actual); var callback = actual.splitIsland;
                // Rebinding the first host must detach only its own previously bound hook.
                gpu.EnableIslandSplitting(cpu);
                if (actual.splitIsland != callback) throw new Exception("Island rebinding replaced another host's callback.");
            }
            if (actual.splitIsland is not null) throw new Exception("Island split disposal did not detach its hook.");
        }
        finally
        {
            b2DestroyWorld(c.ID); b2DestroyWorld(g.ID);
            if (cpu.splitIsland is not null || actual.splitIsland is not null) throw new Exception("World reset retained the island splitter.");
        }
    }
}
