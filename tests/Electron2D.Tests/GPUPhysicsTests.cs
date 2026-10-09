using Box2D.NET;
using Electron2D;

internal static partial class GPUPhysicsTests
{
    internal static void Run()
    {
        using var gpu = new GPUPhysicsWorld();
        VerifyBroadPhase(gpu);
        foreach (var count in new[] { 1, 63, 64, 65, 4097, 65536 }) VerifyIntegration(gpu, count);
        VerifyConstraints(gpu);
        VerifyBodyFinalization();
        VerifyManifolds(gpu);
        VerifyGeometryResidency();
        VerifyContactUpdates();
        VerifyContactCreation();
        VerifyIslandGraph();
        VerifyIslandSplitting();
        VerifyResidentConstraints(gpu);
        VerifyWarmHistory(gpu);
        PhysicsSurfaceVelocityTests.Run(true);
        PhysicsServerStateTests.Run(true);
        PhysicsContactImpulseTests.Run(true);
        PhysicsServerJointTests.VerifyFrameReattachment(true);
        PhysicsJointPolicyTests.Run(true);
        VerifyWorld(false); VerifyWorld(true);
        VerifyWorld(true, true);
        VerifyWorld(true, pairFailure: true);
        VerifyWorld(true, creationFailure: true);
        VerifyWorld(true, removalFailure: true);
        VerifyWorld(true, splitFailure: true);
        VerifyWorld(true, islandFailure: true);
        VerifyWorld(true, colorFailure: true);
        VerifyWorld(true, finalizationFailure: true);
        VerifyOwnedWorldFailure();
        GPUPhysicsBodyStoreTests.Run();
        GPUPhysicsBodyParameterTests.Run();
        GPUPhysicsTransientForceTests.Run();
        GPUPhysicsKinematicTests.Run();
        GPUPhysicsMassStoreTests.Run();
        GPUPhysicsSleepStoreTests.Run();
        GPUPhysicsCCDStoreTests.Run();
        GPUPhysicsSpatialTests.Run();
        GPUPhysicsContactStoreTests.Run();
        GPUPhysicsSolverStoreTests.Run();
        GPUPhysicsJointStoreTests.Run();
        GPUPhysicsExceptionStoreTests.Run();
        GPUPhysicsOneWayTests.Run();
        VerifyDeviceLifetime("gpu");
        VerifyDeviceLifetime("compatibility");
        Console.WriteLine($"GPU integration passed on {gpu.Driver}: forces, damping, locks, speed limits, rotations and dispatch boundaries.");
    }

    private static B2StepContext Context(int count)
    {
        var world = new B2World { gravity = new(1.25f, 9.8f) };
        world.solverSets.data = [new(), new(), new()]; world.solverSets.count = world.solverSets.capacity = 3;
        world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodyStates.count = count;
        var result = new B2StepContext
        {
            world = world,
            h = 1f / 240,
            inv_dt = 60,
            inv_h = 240,
            subStepCount = 4,
            maxLinearVelocity = 400,
            states = new B2BodyState[count],
            sims = new B2BodySim[count]
        };
        world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodyStates.data = result.states;
        world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodySims.data = result.sims;
        return result;
    }

    private static void VerifyIntegration(GPUPhysicsWorld gpu, int count)
    {
        var expected = Context(count); var actual = Context(count); var random = new Random(711 + count);
        float Number(float scale) => (float)(random.NextDouble() * 2 - 1) * scale;
        for (var i = 0; i < count; i++)
        {
            expected.states[i] = new B2BodyState
            {
                linearVelocity = new(Number(650), Number(650)),
                angularVelocity = Number(180),
                flags = (uint)(i % 8),
                deltaPosition = new(Number(100), Number(100)),
                deltaRotation = B2MathFunction.b2MakeRot(Number(3))
            };
            expected.sims[i] = new B2BodySim
            {
                force = new(Number(30), Number(30)),
                torque = Number(20),
                invMass = i % 7 == 0 ? 0 : .4f,
                invInertia = .2f,
                gravityScale = Number(2),
                linearDamping = .1f + i % 5,
                angularDamping = .2f + i % 3,
                flags = i % 9 == 0 ? (uint)B2BodyFlags.b2_allowFastRotation : 0
            };
            actual.states[i] = B2BodyState.Create(expected.states[i]);
            actual.sims[i] = new B2BodySim(); actual.sims[i].CopyFrom(expected.sims[i]);
        }
        B2Solvers.b2IntegrateVelocitiesTask(0, count, expected);
        gpu.Integrate(B2SolverStageType.b2_stageIntegrateVelocities, actual);
        Compare(expected, actual);
        B2Solvers.b2IntegratePositionsTask(0, count, expected);
        gpu.Integrate(B2SolverStageType.b2_stageIntegratePositions, actual);
        Compare(expected, actual);
        var before = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 8; i++) gpu.Integrate(B2SolverStageType.b2_stageIntegratePositions, actual);
        var bytes = GC.GetTotalAllocatedBytes(true) - before;
        if (bytes != 0) throw new InvalidOperationException($"Prepared GPU integration allocated {bytes} managed bytes.");
    }

    private static void Compare(B2StepContext expected, B2StepContext actual)
    {
        for (var i = 0; i < expected.states.Length; i++)
        {
            var e = expected.states[i]; var a = actual.states[i];
            Near(e.linearVelocity.X, a.linearVelocity.X); Near(e.linearVelocity.Y, a.linearVelocity.Y); Near(e.angularVelocity, a.angularVelocity);
            Near(e.deltaPosition.X, a.deltaPosition.X); Near(e.deltaPosition.Y, a.deltaPosition.Y);
            Near(e.deltaRotation.c, a.deltaRotation.c); Near(e.deltaRotation.s, a.deltaRotation.s);
            if (expected.sims[i].flags != actual.sims[i].flags) throw new InvalidOperationException("GPU speed-cap flags differ.");
        }
    }

    private static void Near(float expected, float actual)
    {
        if (!float.IsFinite(actual) || MathF.Abs(expected - actual) > 2e-5f * MathF.Max(1, MathF.Abs(expected)))
            throw new InvalidOperationException($"GPU integration differs: CPU {expected}, GPU {actual}.");
    }

    private static void VerifyWorld(bool solver, bool collisionFailure = false, bool pairFailure = false, bool creationFailure = false, bool removalFailure = false, bool splitFailure = false, bool islandFailure = false, bool colorFailure = false, bool finalizationFailure = false)
    {
        var cpuSpace = PhysicsServer.SpaceCreate(); var gpuSpace = PhysicsServer.SpaceCreate();
        var circle = PhysicsServer.CircleShapeCreate(); var rectangle = PhysicsServer.RectangleShapeCreate();
        var cpuBody = PhysicsServer.BodyCreate(); var gpuBody = PhysicsServer.BodyCreate();
        var cpuFloor = PhysicsServer.BodyCreate(); var gpuFloor = PhysicsServer.BodyCreate();
        try
        {
            using var circleData = new CircleShape { Radius = 12 }; using var rectangleData = new RectangleShape { Size = new(600, 32) };
            PhysicsServer.ShapeSetData(circle, circleData); PhysicsServer.ShapeSetData(rectangle, rectangleData);
            foreach (var (space, body, floor) in new[] { (cpuSpace, cpuBody, cpuFloor), (gpuSpace, gpuBody, gpuFloor) })
            {
                PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(floor, rectangle); PhysicsServer.BodySetTransform(floor, new(0, Vector2.One, 0, new(0, 160)));
                PhysicsServer.BodyAddShape(body, circle); PhysicsServer.BodySetMaxContactsReported(body, 4);
                PhysicsServer.BodySetSpace(floor, space); PhysicsServer.BodySetSpace(body, space);
                PhysicsServer.SpaceSetActive(space, true);
            }
            var world = PhysicsServer.Service.GetSceneSpace(gpuSpace); var gpu = solver ? world.EnableGPUSolver() : world.EnableGPUIntegration();
            var callbacks = 0; PhysicsServer.BodySetForceIntegrationCallback(gpuBody, _ => callbacks++);
            for (var tick = 0; tick < 120; tick++)
            {
                PhysicsServer.SpaceStep(cpuSpace, 1d / 60); PhysicsServer.SpaceStep(gpuSpace, 1d / 60);
                var expected = PhysicsServer.BodyGetTransform(cpuBody).Origin; var actual = PhysicsServer.BodyGetTransform(gpuBody).Origin;
                if (expected.DistanceTo(actual) > .03f) throw new InvalidOperationException("GPU/CPU falling-contact trajectories differ.");
            }
            using var view = PhysicsServer.BodyGetDirectState(gpuBody)!;
            if (gpu.DispatchCount == 0 || callbacks == 0 || view.GetContactCount() == 0)
                throw new InvalidOperationException("The ordinary physics world must execute GPU kernels, callbacks and contact capture.");
            if (solver && (gpu.CollisionSubmissionCount == 0 || gpu.UpdatedContactCount == 0))
                throw new InvalidOperationException("The GPU world must generate live contact manifolds.");
            using var query = new PhysicsPointQueryParameters { Position = view.Transform.Origin };
            if (!PhysicsServer.SpaceGetDirectState(gpuSpace).IntersectPoint(query).Any(hit => hit.ColliderRID == gpuBody))
                throw new InvalidOperationException("GPU-published body poses must reach direct queries.");
            if (finalizationFailure)
            {
                var native = B2Worlds.b2GetWorldFromId(world.WorldID); var consume = native.finalizeBodyStates;
                native.solveConstraints = context =>
                {
                    var before = gpu.BodyFinalizationBatchCount; gpu.Solve(context);
                    if (gpu.BodyFinalizationBatchCount == before) throw new Exception("The failure fixture must prepare GPU body finalization.");
                    native.finalizeBodyStates = prepared => { consume(prepared); throw new IOException("injected GPU finalization publication failure"); };
                };
            }
            else if (colorFailure)
            {
                var native = B2Worlds.b2GetWorldFromId(world.WorldID);
                for (var i = 0; i < native.contacts.count; i++)
                    if (native.contacts.data[i].contactId >= 0) B2Contacts.b2DestroyContact(native, native.contacts.data[i], false);
                for (var i = 0; i < native.shapes.count; i++)
                    if (native.shapes.data[i].proxyKey != -1) B2BoardPhases.b2BufferMove(native.broadPhase, native.shapes.data[i].proxyKey);
                var select = native.selectConstraintColor;
                native.selectConstraintColor = (w, kind, id, a, b, color) =>
                {
                    if (select(w, kind, id, a, b, color) < 0) throw new Exception("The failure fixture must consume GPU colors.");
                    throw new IOException("injected GPU color publication failure");
                };
            }
            else if (islandFailure)
            {
                var native = B2Worlds.b2GetWorldFromId(world.WorldID);
                var existing = native.contacts.data.First(c => c.contactId >= 0 && c.islandId >= 0);
                native.shapes.data[existing.shapeIdB].fatAABB = new() { lowerBound = new(100, 100), upperBound = new(101, 101) };
                var publish = native.finishIslandChanges;
                native.finishIslandChanges = w =>
                {
                    var before = gpu.IslandChangeCount; publish(w);
                    if (gpu.IslandChangeCount == before) throw new Exception("The failure fixture must publish GPU island changes.");
                    throw new IOException("injected GPU island graph publication failure");
                };
            }
            else if (splitFailure)
            {
                var native = B2Worlds.b2GetWorldFromId(world.WorldID);
                var body = native.bodies.data.First(b => b.id >= 0 && b.type == B2BodyType.b2_dynamicBody);
                B2Bodies.b2Body_SetAwake(B2Bodies.b2MakeBodyId(native, body.id), true);
                native.islands.data[body.islandId].constraintRemoveCount = 1; native.splitIslandId = body.islandId;
                var split = native.splitIsland;
                native.splitIsland = (w, id) =>
                {
                    var before = gpu.SplitIslandCount; split(w, id);
                    if (gpu.SplitIslandCount == before) throw new Exception("The failure fixture must execute GPU island splitting.");
                    throw new IOException("injected GPU island publication failure");
                };
            }
            else if (removalFailure)
            {
                var native = B2Worlds.b2GetWorldFromId(world.WorldID);
                var existing = native.contacts.data.First(c => c.contactId >= 0);
                native.shapes.data[existing.shapeIdB].fatAABB = new() { lowerBound = new(100, 100), upperBound = new(101, 101) };
                var publish = native.destroyDisjointContact;
                native.destroyDisjointContact = (w, c) => { publish(w, c); throw new IOException("injected GPU removal publication failure"); };
            }
            else if (creationFailure)
            {
                var native = B2Worlds.b2GetWorldFromId(world.WorldID);
                for (var i = 0; i < native.contacts.count; i++)
                    if (native.contacts.data[i].contactId >= 0) B2Contacts.b2DestroyContact(native, native.contacts.data[i], false);
                for (var i = 0; i < native.shapes.count; i++)
                    if (native.shapes.data[i].proxyKey != -1) B2BoardPhases.b2BufferMove(native.broadPhase, native.shapes.data[i].proxyKey);
                native.createBroadPhaseContacts = w =>
                {
                    var before = gpu.CreatedContactCount;
                    gpu.CreateContacts(w);
                    if (gpu.CreatedContactCount == before) throw new Exception("The failure fixture must create GPU contacts.");
                    throw new IOException("injected GPU contact creation failure");
                };
            }
            else if (pairFailure)
            {
                var native = B2Worlds.b2GetWorldFromId(world.WorldID);
                native.findBroadPhasePairs = w => { gpu.FindBroadPhasePairs(w); throw new IOException("injected GPU pair failure"); };
                for (var i = 0; i < native.shapes.count; i++)
                    if (native.shapes.data[i].proxyKey != -1) B2BoardPhases.b2BufferMove(native.broadPhase, native.shapes.data[i].proxyKey);
            }
            else if (collisionFailure) Box2D.NET.B2Worlds.b2GetWorldFromId(world.WorldID).generateManifolds = (context, count) => { gpu.UpdateContacts(context, count); throw new IOException("injected GPU collision failure"); };
            else if (solver) Box2D.NET.B2Worlds.b2GetWorldFromId(world.WorldID).solveConstraints = _ => throw new IOException("injected GPU failure");
            else Box2D.NET.B2Worlds.b2GetWorldFromId(world.WorldID).integrateBodyStage = (_, _) => throw new IOException("injected GPU failure");
            PhysicsServer.BodySetLinearVelocity(gpuBody, new(10, 0));
            try { PhysicsServer.SpaceStep(gpuSpace, 1d / 60); throw new Exception("GPU failure was not reported."); }
            catch (AggregateException ex) when (ex.InnerExceptions.Any(e => e is IOException)) { }
            try { PhysicsServer.SpaceStep(gpuSpace, 1d / 60); throw new Exception("A failed GPU interval was replayed."); }
            catch (InvalidOperationException ex) when (ex.InnerException is IOException) { }
            // Disposal must release live backend bodies and scratch storage after the failed interval.
        }
        finally
        {
            PhysicsServer.FreeRID(gpuSpace); PhysicsServer.FreeRID(cpuSpace);
            foreach (var rid in new[] { gpuBody, cpuBody, gpuFloor, cpuFloor, circle, rectangle }) PhysicsServer.FreeRID(rid);
        }
        Console.WriteLine("GPU world step, CPU comparison, contacts, post-solver callbacks, queries and failed-interval teardown passed.");
    }

    private static void VerifyOwnedWorldFailure()
    {
        var owned = new World(); var rid = owned.Space;
        var body = PhysicsServer.BodyCreate(); var shape = PhysicsServer.CircleShapeCreate();
        try
        {
            PhysicsServer.BodyAddShape(body, shape); PhysicsServer.BodySetSpace(body, rid);
            var space = PhysicsServer.Service.GetSceneSpace(rid); space.SetActive(true); space.EnableGPUSolver();
            B2Worlds.b2GetWorldFromId(space.WorldID).solveConstraints = _ => throw new IOException("injected owned-world GPU failure");
            try { space.Step(1d / 60); throw new Exception("Owned-world GPU failure was not reported."); }
            catch (AggregateException error) when (error.InnerExceptions.Any(e => e is IOException)) { }
            try { space.EnsureWorldBindingChange(); throw new Exception("A failed GPU world must reject rebinding."); }
            catch (InvalidOperationException error) when (error.InnerException is IOException) { }
            owned.Dispose();
            try { PhysicsServer.Service.GetSceneSpace(rid); throw new Exception("The failed owned world's space must expire after disposal."); }
            catch (ArgumentException) { }
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(shape); owned.Dispose(); }
        Console.WriteLine("Owned World releases its GPU space after a failed interval.");
    }

    private static void VerifyDeviceLifetime(string rendering)
    {
        var previous = ProjectSettings.GetWithOverride(ProjectSettings.RenderingMethod);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, rendering);
        GPUPhysicsWorld? gpu = null;
        var context = Context(1); context.states[0] = new() { deltaRotation = new(1, 0), linearVelocity = new(1, 0) };
        context.sims[0] = new();
        using var window = new Window { Size = new(512, 384) };
        var frames = 0;
        Action rendered = () => { if (++frames == 3) window.Tree!.Quit(); };
        window.Ready += _ =>
        {
            gpu = new GPUPhysicsWorld();
            gpu.Integrate(B2SolverStageType.b2_stageIntegratePositions, context);
            RenderingServer.FramePostDraw += rendered;
        };
        var activate = SDL3.SDL.GetHint(SDL3.SDL.Hints.WindowActivateWhenShown);
        SDL3.SDL.SetHint(SDL3.SDL.Hints.WindowActivateWhenShown, "0");
        try
        {
            if (Engine.Run(window) != 0 || frames != 3 || gpu is null) throw new InvalidOperationException("The native device lifetime host did not finish.");
            // The renderer has closed its handle. The compute host must still own a usable device reference.
            gpu.Integrate(B2SolverStageType.b2_stageIntegratePositions, context);
            if (gpu.DispatchCount != 2 || context.states[0].deltaPosition.X <= 0)
                throw new InvalidOperationException("Compute resources must survive renderer teardown.");
        }
        finally
        {
            if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= rendered;
            gpu?.Dispose(); ProjectSettings.Set(ProjectSettings.RenderingMethod, previous);
            if (activate is null) SDL3.SDL.ResetHint(SDL3.SDL.Hints.WindowActivateWhenShown);
            else SDL3.SDL.SetHint(SDL3.SDL.Hints.WindowActivateWhenShown, activate);
        }
        Console.WriteLine($"GPU compute lifetime independent of {rendering} renderer passed.");
    }
}
