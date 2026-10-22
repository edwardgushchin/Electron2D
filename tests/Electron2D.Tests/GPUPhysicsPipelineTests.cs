using Electron2D;

internal static class GPUPhysicsPipelineTests
{
    internal static void Run()
    {
        GPUPhysicsPairHashTests.Run();
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        if (GPUPhysicsDevice.PipelineCreationForTesting is not null) throw new InvalidOperationException("The pipeline probe is already in use.");
        try
        {
            foreach (var threaded in new[] { false, true })
            {
                ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
                var records = new List<(string Name, int Owner, int Thread)>();
                GPUPhysicsDevice.PipelineCreationForTesting = (name, owner, thread) => records.Add((name, owner, thread));
                VerifyWorld(records);
                VerifyPreparationFailure();
                var offOwner = records.Where(row => row.Owner != row.Thread).Select(row => row.Name).ToArray();
                Console.WriteLine($"GPU cold pipelines: separate={threaded}, created={records.Count}, off-owner={offOwner.Length}: {string.Join(", ", offOwner)}.");
                if (records.Count < 8 || offOwner.Length != 0) throw new InvalidOperationException("Cold numerical pipelines must be prepared on the world owner before solver borrowing.");
            }
        }
        finally
        {
            GPUPhysicsDevice.PipelineCreationForTesting = null;
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous);
        }
    }

    private static void VerifyWorld(List<(string Name, int Owner, int Thread)> records)
    {
        var space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU); var bodies = new RID[32]; var joint = default(RID);
        using var shape = new CircleShape { Radius = 2 };
        try
        {
            PhysicsServer.SpaceSetActive(space, true);
            PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            PhysicsServer.SpaceStep(space, 1d / 120);
            if (records.Count != 1) throw new InvalidOperationException("An empty interval must not prepare unused numerical pipelines.");
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i] = PhysicsServer.BodyCreate();
                PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetTransform(body, new(0, new(i / 2 * 12, i % 2 * 3.5f)));
                PhysicsServer.BodySetSpace(body, space);
            }
            joint = PhysicsServer.JointCreate(); PhysicsServer.JointMakePin(joint, Vector2.Zero, bodies[0]);
            PhysicsServer.SpaceStep(space, 1d / 120);
            if (PhysicsServer.SpaceGetTick(space) != 2 || PhysicsServer.SpaceGetBackend(space) != PhysicsServer.Backend.GPU || !PhysicsServer.BodyGetTransform(bodies[1]).Origin.IsFinite())
                throw new InvalidOperationException("The pipeline probe must execute a real selected GPU interval and publish finite motion.");
            PhysicsServer.BodySetMode(bodies[^1], PhysicsServer.BodyMode.Kinematic);
            PhysicsServer.SpaceStep(space, 1d / 120);
            PhysicsServer.BodySetMode(bodies[^1], PhysicsServer.BodyMode.Rigid);
            PhysicsServer.BodySetContinuousCollisionDetectionMode(bodies[^1], CCDMode.CastShape);
            PhysicsServer.BodySetLinearVelocity(bodies[^1], new(100, 0));
            PhysicsServer.AreaSetLinearDamp(space, .1f);
            PhysicsServer.BodySetMaxContactsReported(bodies[1], 4);
            PhysicsServer.Service.GetSceneSpace(space).SetDebugContacts(8);
            PhysicsServer.SpaceStep(space, 1d / 120);
            foreach (var shader in new[] { "PhysicsResidentCCD.comp.spv", "PhysicsResidentFields.comp.spv", "PhysicsResidentReports.comp.spv", "PhysicsResidentDebugContacts.comp.spv" })
                if (!records.Any(row => row.Name == shader)) throw new InvalidOperationException("The cold feature transition did not exercise " + shader);
            for (var i = 0; i < 128; i++) PhysicsServer.SpaceStep(space, 1d / 120);
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); var pipelines = records.Count;
            for (var i = 0; i < 128; i++) PhysicsServer.SpaceStep(space, 1d / 120);
            if (owner != GC.GetAllocatedBytesForCurrentThread() || all != GC.GetTotalAllocatedBytes(true) || pipelines != records.Count)
                throw new InvalidOperationException("Prepared full GPU intervals must reuse pipelines and allocate zero owner/all-thread bytes.");
        }
        finally
        {
            if (joint.IsValid()) PhysicsServer.FreeRID(joint);
            foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
            PhysicsServer.FreeRID(space);
        }
    }

    private static void VerifyPreparationFailure()
    {
        var observer = GPUPhysicsDevice.PipelineCreationForTesting; var inject = true;
        GPUPhysicsDevice.PipelineCreationForTesting = (name, owner, thread) =>
        {
            observer?.Invoke(name, owner, thread);
            if (inject && name == "PhysicsResidentSolve.comp.spv") throw new IOException("Expected cold pipeline preparation failure.");
        };
        var space = default(RID); var body = default(RID);
        using var shape = new CircleShape { Radius = 2 };
        try
        {
            space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU); body = PhysicsServer.BodyCreate();
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetSpace(body, space);
            var rejected = false;
            try { PhysicsServer.SpaceStep(space, 1d / 120); }
            catch (AggregateException error) when (error.InnerExceptions.Any(value => value is IOException)) { rejected = true; }
            if (!rejected || PhysicsServer.SpaceGetTick(space) != 0 || PhysicsServer.Service.GetSceneSpace(space).HasBackendFailure || PhysicsServer.BodyGetCollisionLayer(body) != 1)
                throw new InvalidOperationException("A cold preparation failure must precede numerical entry and preserve the usable world and tick.");
            inject = false; PhysicsServer.SpaceStep(space, 1d / 120);
            if (PhysicsServer.SpaceGetTick(space) != 1 || PhysicsServer.SpaceGetBackend(space) != PhysicsServer.Backend.GPU)
                throw new InvalidOperationException("The same selected GPU world must retry cold preparation without CPU replay.");
        }
        finally
        {
            GPUPhysicsDevice.PipelineCreationForTesting = observer;
            if (body.IsValid()) PhysicsServer.FreeRID(body);
            if (space.IsValid()) PhysicsServer.FreeRID(space);
        }
    }
}
