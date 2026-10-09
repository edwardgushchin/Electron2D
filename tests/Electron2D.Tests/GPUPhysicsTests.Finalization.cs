using System.Runtime.InteropServices;
using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

internal static partial class GPUPhysicsTests
{
    internal static void VerifyBodyFinalization()
    {
        using var gpu = new GPUPhysicsWorld();
        if (Marshal.SizeOf<B2StepContext.BodyFinalization>() != 64) throw new Exception("Body finalization layout differs.");
        foreach (var count in new[] { 0, 1, 63, 64, 65, 257, 4097 }) VerifyBodyFinalization(gpu, count);
        VerifyFinalizationFailure(gpu);
        VerifyFinalizationCCD(gpu);
        Console.WriteLine("GPU body finalization matches CPU pose/locks/sleep decisions, move events, CCD continuation, reset and warmed allocation.");
    }

    private static void VerifyBodyFinalization(GPUPhysicsWorld gpu, int count)
    {
        var wd = b2DefaultWorldDef(); wd.gravity = new(0, 0); wd.enableContinuous = false;
        var id = b2CreateWorld(wd); var world = b2GetWorldFromId(id);
        for (var i = 0; i < count; i++)
        {
            var bd = b2DefaultBodyDef(); bd.type = i % 6 == 1 ? B2BodyType.b2_kinematicBody : B2BodyType.b2_dynamicBody;
            bd.position = new(i * 3, 0); bd.rotation = b2MakeRot(i * .031f);
            bd.motionLocks = new(i % 8 == 1, i % 8 == 2, i % 8 == 3);
            bd.enableSleep = i % 5 != 1; b2CreateBody(id, bd);
        }
        gpu.EnableBodyFinalization(world); var consume = world.finalizeBodyStates;
        var checkedBodies = 0;
        Action<B2StepContext> compare = context =>
        {
            consume(context);
            var prepared = context.finalizedBodies;
            var n = world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodyStates.count;
            context.finalizedBodies = null!;
            // Use the real CPU finalizer as the oracle on the same solved state.
            // This fixture has no shapes/CCD, so a second publication has no query side effects.
            B2Solvers.b2FinalizeBodiesTask(0, n, 0, context);
            for (var i = 0; i < n; i++)
            {
                var actual = prepared[i]; var sim = context.sims[i]; var state = context.states[i]; var body = world.bodies.data[sim.bodyId];
                if (sim.center != actual.center || sim.transform.p != actual.position || sim.transform.q.c != actual.rotation.c || sim.transform.q.s != actual.rotation.s)
                    throw new Exception($"Finalization bits differ at {count}/{sim.bodyId}: CPU {sim.center.X:R},{sim.center.Y:R}; {sim.transform.p.X:R},{sim.transform.p.Y:R}; {sim.transform.q.c:R},{sim.transform.q.s:R}; GPU {actual.center.X:R},{actual.center.Y:R}; {actual.position.X:R},{actual.position.Y:R}; {actual.rotation.c:R},{actual.rotation.s:R}.");
                Near(sim.center.X, actual.center.X); Near(sim.center.Y, actual.center.Y);
                Near(sim.transform.p.X, actual.position.X); Near(sim.transform.p.Y, actual.position.Y);
                Near(sim.transform.q.c, actual.rotation.c); Near(sim.transform.q.s, actual.rotation.s);
                Near(state.linearVelocity.X, actual.linearVelocity.X); Near(state.linearVelocity.Y, actual.linearVelocity.Y); Near(state.angularVelocity, actual.angularVelocity);
                var bits = body.sleepTime <= world.timeToSleep ? 2u : world.islands.data[body.islandId].constraintRemoveCount > 0 ? 4u : 0;
                if (actual.sleepTime != body.sleepTime || actual.bodyFlags != body.flags || actual.simFlags != sim.flags || actual.state != bits)
                    throw new Exception("GPU finalization sleep/flags differ from the CPU finalizer.");
            }
            context.finalizedBodies = prepared; world.finalizeBodyStates = consume; checkedBodies += n;
        };
        world.solveConstraints = context =>
        {
            world.finalizeBodyStates = consume;
            for (var i = 0; i < world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodyStates.count; i++)
            {
                var sim = context.sims[i]; var body = world.bodies.data[sim.bodyId]; var state = context.states[i];
                var index = sim.bodyId;
                sim.localCenter = new(.125f * (index % 3), -.125f);
                sim.center = sim.transform.p + b2RotateVector(sim.transform.q, sim.localCenter);
                sim.minExtent = .125f; sim.maxExtent = 1.75f;
                body.sleepThreshold = .25f;
                body.sleepTime = index % 3 == 0 ? .5f : .49f;
                world.islands.data[body.islandId].constraintRemoveCount = index % 4 == 0 ? 1 : 0;
                state.linearVelocity = index % 4 == 0 ? new(.25f, 0) : index % 4 == 1 ? new(3, -2) : new(0, 0);
                state.angularVelocity = index % 9 == 0 ? 2 : 0;
                state.deltaPosition = index % 7 == 0 ? new(.03f, -.01f) : new(0, 0);
                state.deltaRotation = index % 11 == 0 ? b2MakeRot(.025f) : new(1, 0);
                sim.flags |= (uint)(B2BodyFlags.b2_isFast | B2BodyFlags.b2_hadTimeOfImpact | B2BodyFlags.b2_isSpeedCapped);
                body.flags |= (uint)B2BodyFlags.b2_isFast;
            }
            gpu.Solve(context); world.finalizeBodyStates = compare;
        };
        var frame = 0;
        void Step()
        {
            for (var i = 0; i < count; i++) b2Body_SetAwake(b2MakeBodyId(world, i), true);
            world.enableSleep = frame++ % 3 != 0; world.splitIslandId = -1;
            b2World_Step(id, 1f / 60, 4);
            if (world.reusableStepContext.finalizedBodies is not null) throw new Exception("Body finalization leaked into a later step.");
        }
        try
        {
            for (var i = 0; i < 32; i++) Step();
            var before = GC.GetTotalAllocatedBytes(true); var batches = gpu.BodyFinalizationBatchCount;
            for (var i = 0; i < 16; i++) Step();
            var bytes = GC.GetTotalAllocatedBytes(true) - before;
            if (bytes != 0 || gpu.BodyFinalizationBatchCount - batches != (count == 0 ? 0 : 16))
                throw new Exception($"Prepared body finalization allocated {bytes} bytes or skipped GPU work.");
            if (checkedBodies != 48 * count) throw new Exception("The numeric finalization fixture skipped bodies.");
            batches = gpu.BodyFinalizationBatchCount; b2World_Step(id, 0, 4);
            if (gpu.BodyFinalizationBatchCount != batches) throw new Exception("A zero-duration step finalized bodies.");
            try { consume(world.reusableStepContext); throw new Exception("Stale finalization was published twice."); }
            catch (InvalidOperationException e) when (e.Message == "GPU body finalization is not prepared for this step.") { }
            world.reusableStepContext.finalizedBodies = new B2StepContext.BodyFinalization[1]; world.reusableStepContext.Reset();
            if (world.reusableStepContext.finalizedBodies is not null) throw new Exception("Step reset retained finalization output.");
            using (var other = new GPUPhysicsWorld())
            {
                other.EnableBodyFinalization(world); var callback = world.finalizeBodyStates;
                gpu.EnableBodyFinalization(new B2World());
                if (world.finalizeBodyStates != callback) throw new Exception("Finalization rebinding replaced another owner's callback.");
            }
            if (world.finalizeBodyStates is not null) throw new Exception("Finalization disposal retained its callback.");
        }
        finally
        {
            b2DestroyWorld(id);
            if (world.finalizeBodyStates is not null) throw new Exception("World reset retained body finalization.");
        }
    }

    private static void VerifyFinalizationFailure(GPUPhysicsWorld gpu)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        foreach (var fault in new[] { 0, 1, 2, 3 })
        {
            var id = b2CreateWorld(b2DefaultWorldDef()); var world = b2GetWorldFromId(id);
            try
            {
                var bd = b2DefaultBodyDef(); bd.type = B2BodyType.b2_dynamicBody; bd.position = new(3, 4); var body = b2CreateBody(id, bd);
                gpu.EnableBodyFinalization(world);
                world.solveConstraints = context =>
                {
                    gpu.Solve(context);
                    var storage = typeof(GPUPhysicsWorld).GetField("_finalizationResults", flags)!.GetValue(gpu)!;
                    var data = (B2StepContext.BodyFinalization[])storage.GetType().GetField("Data", flags)!.GetValue(storage)!;
                    if (fault == 0) data[0].generation++;
                    else if (fault == 1) data[0].position.X = float.NaN;
                    else if (fault == 2) data[0].state ^= 2;
                    else data[0].rotation = new(0, 0);
                    typeof(GPUPhysicsWorld).GetMethod("ValidateFinalization", flags)!.Invoke(gpu, [context, 1]);
                };
                try { b2World_Step(id, 1f / 60, 4); throw new Exception("Invalid body finalization reached publication."); }
                catch (System.Reflection.TargetInvocationException e) when (e.InnerException is InvalidOperationException error &&
                    error.Message.StartsWith("GPU body finalization returned", StringComparison.Ordinal))
                { }
                if (b2Body_GetPosition(body) != new B2Vec2(3, 4)) throw new Exception("Rejected body finalization changed its transform.");
            }
            finally
            {
                world.locked = false; foreach (var arena in world.arena.AsSpan()) arena.Abort(); b2DestroyWorld(id);
            }
        }
    }

    private static void VerifyFinalizationCCD(GPUPhysicsWorld gpu)
    {
        B2WorldId Create()
        {
            var wd = b2DefaultWorldDef(); wd.gravity = new(0, 0); var id = b2CreateWorld(wd);
            var ground = b2DefaultBodyDef(); ground.position = new(4, 0);
            b2CreatePolygonShape(b2CreateBody(id, ground), b2DefaultShapeDef(), B2Geometries.b2MakeBox(.1f, 50));
            for (var i = 0; i < 3; i++)
            {
                var bd = b2DefaultBodyDef(); bd.type = B2BodyType.b2_dynamicBody; bd.position = new(0, i * 6);
                bd.linearVelocity = new(90, 0); bd.isBullet = i == 1; bd.enableSleep = false;
                b2CreateCircleShape(b2CreateBody(id, bd), b2DefaultShapeDef(), new B2Circle { radius = .25f, center = new(.125f, .125f) });
            }
            return id;
        }
        var c = Create(); var g = Create(); var cpu = b2GetWorldFromId(c); var actual = b2GetWorldFromId(g);
        gpu.EnableBodyFinalization(actual); actual.solveConstraints = gpu.Solve;
        try
        {
            for (var step = 0; step < 16; step++)
            {
                b2World_Step(c, 1f / 60, 4); b2World_Step(g, 1f / 60, 4); CompareIslands(cpu, actual);
                for (var i = 1; i < 4; i++)
                {
                    var e = b2Body_GetTransform(b2MakeBodyId(cpu, i)); var a = b2Body_GetTransform(b2MakeBodyId(actual, i));
                    Near(e.p.X, a.p.X); Near(e.p.Y, a.p.Y); Near(e.q.c, a.q.c); Near(e.q.s, a.q.s);
                    if (e.p.X > 4 || a.p.X > 4) throw new Exception("A finalized fast body crossed the wall.");
                }
                if (cpu.bodyMoveEvents.count != actual.bodyMoveEvents.count) throw new Exception("Finalized body move count differs.");
                for (var i = 0; i < cpu.bodyMoveEvents.count; i++)
                {
                    var e = cpu.bodyMoveEvents.data[i]; var a = actual.bodyMoveEvents.data[i];
                    Near(e.transform.p.X, a.transform.p.X); Near(e.transform.p.Y, a.transform.p.Y);
                    if (e.bodyId.index1 != a.bodyId.index1 || e.bodyId.generation != a.bodyId.generation || e.fellAsleep != a.fellAsleep)
                        throw new Exception("Finalized body move identity/order differs.");
                }
            }
        }
        finally { b2DestroyWorld(c); b2DestroyWorld(g); }
    }
}
