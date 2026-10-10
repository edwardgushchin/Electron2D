using System.Diagnostics;
using Electron2D;
using static Box2D.NET.B2Worlds;

internal static class PhysicsBackendOwnershipTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        TickBoundary(backend);
        PreparationFailure(backend);
        WorldPolicy(backend);
        JointAttachment(backend);
        ColliderAttachment(backend);
        QueryOwnership(backend);
        MotionOwnership(backend);
        CallbackBorrow(backend);
        var first = PhysicsServer.SpaceCreate(backend); var second = PhysicsServer.SpaceCreate(backend);
        var left = PhysicsServer.Service.GetSceneSpace(first); var right = PhysicsServer.Service.GetSceneSpace(second);
        var implementation = left.BackendImplementation;
        var cpuID = backend == PhysicsServer.Backend.CPU ? left.WorldID : default;
        var gpuStore = left.GPUStore;
        try
        {
            Check(!ReferenceEquals(implementation, right.BackendImplementation) && implementation.Kind == backend &&
                implementation.Requested == backend && implementation.FallbackReason is null, "Fresh immutable per-world solver ownership");
            if (backend == PhysicsServer.Backend.CPU)
            {
                Check(implementation is CPUPhysicsWorldBackend && left.GPUStore is null && left.WorldID.index1 != right.WorldID.index1,
                    "CPU implementations own distinct native worlds and no resident GPU store");
            }
            else
            {
                Check(implementation is GPUPhysicsWorldBackend && !ReferenceEquals(left.GPUStore, right.GPUStore), "GPU implementations own distinct resident stores");
                Reject<InvalidOperationException>(() => _ = left.WorldID);
                Reject<InvalidOperationException>(() => left.EnableGPUSolver());
            }
            using var shape = new CircleShape { Radius = 3 };
            const int count = 512;
            var bodies = new RID[count];
            try
            {
                PhysicsServer.SpaceSetActive(first, true);
                for (var i = 0; i < count; i++)
                {
                    var body = bodies[i] = PhysicsServer.BodyCreate();
                    PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetCanSleep(body, false);
                    PhysicsServer.BodySetTransform(body, new(0, new((i % 32) * 40, (i / 32) * 40)));
                    PhysicsServer.BodySetLinearVelocity(body, new(20, 0)); PhysicsServer.BodySetSpace(body, first);
                }
                Collect();
                for (var i = 0; i < 64; i++) PhysicsServer.SpaceStep(first, 1d / 60);
                var before = PhysicsServer.BodyGetTransform(bodies[0]).Origin;
                var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 64; i++) PhysicsServer.SpaceStep(first, 1d / 60);
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0, $"World dispatch allocated {owner}/{all} owner/all-thread bytes");
                Check(left.Tick == 128 && right.Tick == 0 && PhysicsServer.BodyGetTransform(bodies[0]).Origin.X > before.X + 15,
                    "Complete selected solver step advances only its own world and publishes real motion");
                using var query = new PhysicsRayQueryParameters { From = new(-10, before.Y), To = new(200, before.Y), HitFromInside = true };
                _ = PhysicsServer.SpaceGetDirectState(first).IntersectRay(query);
                Console.WriteLine($"{backend}: selected implementation, 512 bodies/64 full warmed steps: {owner}/{all} owner/all-thread B; independent ticks and native lifetime passed.");
            }
            finally { foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body); }
        }
        finally { PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(second); }
        Reject<ObjectDisposedException>(implementation.EnsureAccess);
        if (backend == PhysicsServer.Backend.CPU)
        {
            Check(!b2World_IsValid(cpuID), "CPU solver world released by its implementation");
            Reject<ObjectDisposedException>(() => ((CPUPhysicsWorldBackend)implementation).GetJointWorldBody());
        }
        else Reject<ObjectDisposedException>(() => gpuStore!.Read([], []));
        implementation.Dispose();
        if (backend == PhysicsServer.Backend.CPU) CleanupFailure();
    }
    private static void PreparationFailure(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 2 };
        var body = new RigidBody { CanSleep = false, GravityScale = 0 };
        var collision = new CollisionShape { Shape = shape }; body.AddChild(collision); root.AddChild(body);
        using var tree = new SceneTree(root); tree.PhysicsFrame(1d / 60);
        var space = PhysicsServer.Service.GetSceneSpace(world.Space); var tick = PhysicsServer.SpaceGetTick(world.Space);
        collision.Scale = new(2, 1);
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        Check(!space.BackendImplementation.ResultsReady && !space.HasBackendFailure && PhysicsServer.SpaceGetTick(world.Space) == tick,
            "Rejected authoring preparation publishes no solved tick and does not fail the idle implementation");
        collision.Scale = Vector2.One; tree.PhysicsFrame(1d / 60);
        Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1 && PhysicsServer.BodyGetTransform(body.GetRID()).IsFinite(),
            "The common finally releases solver access after preparation failure");
        Console.WriteLine($"{backend}: invalid authored geometry, unchanged tick/results and real scene recovery after preparation failure passed.");
    }
    private static void TickBoundary(PhysicsServer.Backend backend)
    {
        var space = PhysicsServer.SpaceCreate(backend); var body = PhysicsServer.BodyCreate();
        try
        {
            var data = PhysicsServer.Service.GetSceneSpace(space); var owner = data.BackendImplementation;
            PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
            PhysicsServer.BodySetSpace(body, space); PhysicsServer.SpaceSetActive(space, true);
            var order = 0; var fail = false; var forceCalls = 0; var syncCalls = 0;
            PhysicsServer.BodySetForceIntegrationCallback(body, view =>
            {
                order = order * 10 + 1; forceCalls++;
                Check(owner.ResultsReady && PhysicsServer.SpaceGetTick(space) == (ulong)forceCalls &&
                    MathF.Abs(view.Step - 1f / 60) < 1e-7f, "A borrowed integration view sees the solved tick after result synchronization");
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(space, 1d / 60));
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(space));
                view.LinearVelocity = new(30, 0);
                if (fail) throw new InvalidOperationException("Expected callback failure");
            });
            PhysicsServer.BodySetStateSyncCallback(body, view =>
            {
                order = order * 10 + 2; syncCalls++;
                Check(owner.ResultsReady && view.LinearVelocity.DistanceTo(new(30, 0)) < .001f,
                    "Synchronization follows integration even when integration throws");
                _ = PhysicsServer.BodyGetTransform(body);
            });
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(order == 12 && forceCalls == 1 && syncCalls == 1, "Completed result callbacks retain ordered delivery");
            order = 0; fail = true;
            Reject<AggregateException>(() => PhysicsServer.SpaceStep(space, 1d / 60));
            Check(order == 12 && PhysicsServer.SpaceGetTick(space) == 2 && !data.HasBackendFailure,
                "User callback failure preserves a completed usable world and attempts later callbacks");
            PhysicsServer.BodySetForceIntegrationCallback(body, null); PhysicsServer.BodySetStateSyncCallback(body, null);
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(PhysicsServer.SpaceGetTick(space) == 3 && PhysicsServer.BodyGetTransform(body).Origin.X > .5f,
                "The next real interval proceeds after callback cleanup");
            Console.WriteLine($"{backend}: selected step phases, solved tick/views, callback order, reentrancy, borrowed release and callback-failure continuation passed.");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
    }
    private static void WorldPolicy(PhysicsServer.Backend backend)
    {
        var first = PhysicsServer.SpaceCreate(backend); var other = PhysicsServer.SpaceCreate(backend);
        var body = PhysicsServer.BodyCreate();
        try
        {
            var space = PhysicsServer.Service.GetSceneSpace(first); var owner = space.BackendImplementation;
            var untouched = PhysicsServer.SpaceGetSolverIterations(other);
            PhysicsServer.SpaceSetBodyLinearVelocitySleepThreshold(first, 3); PhysicsServer.SpaceSetBodyAngularVelocitySleepThreshold(first, .2f);
            PhysicsServer.SpaceSetBodyTimeToSleep(first, .75f);
            PhysicsServer.SpaceSetContactDefaultBias(first, .3f); PhysicsServer.SpaceSetContactMaxAllowedPenetration(first, .25f);
            PhysicsServer.SpaceSetContactRecycleRadius(first, 2); PhysicsServer.SpaceSetContactMaxSeparation(first, 3);
            PhysicsServer.SpaceSetSolverIterations(first, 7); PhysicsServer.SpaceSetConstraintDefaultBias(first, .4f);
            Check(space.SleepSettings == new PhysicsSleepSettings(3, .2f, .75f) &&
                space.ContactSettings == new PhysicsContactSettings(.3f, .25f, 2, 3) && space.SolverIterations == 7 &&
                space.ConstraintDefaultBias == .4f && PhysicsServer.SpaceGetSolverIterations(other) == untouched,
                "Typed world policy is retained by the selected world without changing another world");
            if (backend == PhysicsServer.Backend.CPU)
            {
                var native = b2GetWorldFromId(space.WorldID);
                Check(native.sleepAngularThreshold == .2f && native.timeToSleep == .75f && native.solverIterations == 7 &&
                    native.contactBias == .3f && native.contactAllowedPenetration == .25f * PhysicsSpace.MetersPerUnit,
                    "The CPU owner applies authored policy to its actual native solver");
            }
            else Reject<InvalidOperationException>(owner.PrepareInterval);
            var before = space.ContactSettings;
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.SpaceSetContactDefaultBias(first, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.SpaceSetSolverIterations(first, 0));
            Check(space.ContactSettings == before && space.SolverIterations == 7, "Invalid policy leaves authored and solver settings unchanged");
            PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodySetSpace(body, first);
            PhysicsServer.SpaceSetActive(first, true); PhysicsServer.SpaceStep(first, 1d / 60);
            Check(owner.ReadStatistics() == space.PublishedStatistics && space.PublishedStatistics.Active == 1,
                "The completed process snapshot comes from the selected solver");
            if (owner is CPUPhysicsWorldBackend cpu)
            {
                Check(!cpu.MaySleep, "All no-sleep dynamic bodies skip dormant-body scanning");
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCanSleep(body, true);
                Check(!cpu.MaySleep, "Static sleep policy does not activate dormant simulation storage");
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
                Check(cpu.MaySleep, "A live switch to a sleep-capable dynamic role prepares dormant capacity");
                PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.SpaceStep(first, 1d / 60);
            }
            if (backend == PhysicsServer.Backend.GPU)
            {
                var motions = (Array)typeof(PhysicsSpace).GetField("_bodyMotions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(space)!;
                Check(motions.Length == 0, "GPU capacity preparation does not allocate a CPU motion mirror");
            }
            Console.WriteLine($"{backend}: world policy/validation, isolated settings, selected solver statistics and capacity ownership passed.");
            PhysicsServer.FreeRID(body); body = default; PhysicsServer.FreeRID(first); first = default;
            Reject<ObjectDisposedException>(owner.PrepareSolverCapacity);
            Reject<ObjectDisposedException>(owner.PrepareMonitoringCapacity);
            Reject<ObjectDisposedException>(() => owner.ReadStatistics());
            Reject<ObjectDisposedException>(() => owner.SetSleepSettings(new(3, .2f, .75f)));
        }
        finally { if (body.IsValid()) PhysicsServer.FreeRID(body); if (first.IsValid()) PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(other); }
    }
    private static void JointAttachment(PhysicsServer.Backend backend)
    {
        var firstSpace = PhysicsServer.SpaceCreate(backend); var secondSpace = PhysicsServer.SpaceCreate();
        var first = PhysicsServer.BodyCreate(); var second = PhysicsServer.BodyCreate(); var joint = PhysicsServer.JointCreate();
        try
        {
            PhysicsServer.BodySetGravityScale(first, 0); PhysicsServer.BodySetGravityScale(second, 0);
            PhysicsServer.BodySetTransform(first, new(.3f, new(10, 20))); PhysicsServer.BodySetTransform(second, new(-.2f, new(40, 30)));
            PhysicsServer.BodySetSpace(first, firstSpace); PhysicsServer.BodySetSpace(second, firstSpace);
            var a = PhysicsServer.Service.BodyRuntime(first).Backend; var b = PhysicsServer.Service.BodyRuntime(second).Backend;
            var failed = new PhysicsJointBackend();
            Reject<Exception>(() => failed.Attach(PhysicsServer.Service.GetSceneSpace(firstSpace), a, b, new(default)));
            Check(!failed.IsAttached && failed.Implementation is null && PhysicsServer.BodyGetSpace(first) == firstSpace,
                "Unsupported internal role retires the failed joint attachment without releasing endpoints");
            var source = PhysicsServer.Service.GetSceneSpace(firstSpace);
            foreach (var role in new[] { PhysicsServer.JointType.Pin, PhysicsServer.JointType.Groove, PhysicsServer.JointType.DampedSpring })
            {
                if (role == PhysicsServer.JointType.Pin) PhysicsServer.JointMakePin(joint, new(25, 25), first, second);
                else if (role == PhysicsServer.JointType.Groove) PhysicsServer.JointMakeGroove(joint, new(10, 20), new(40, 30), new(25, 25), first, second);
                else PhysicsServer.JointMakeDampedSpring(joint, new(10, 20), new(40, 30), first, second);
                var runtime = source.SnapshotJoints.Single(value => value.RID == joint);
                var frameA = runtime.FrameA; var frameB = runtime.FrameB;
                var previous = runtime.Backend.Implementation!;
                PhysicsServer.JointSetBias(joint, .25f); PhysicsServer.JointSetMaxForce(joint, 10); PhysicsServer.JointSetMaxBias(joint, 50);
                PhysicsServer.BodySetSpace(first, secondSpace);
                Check(!runtime.HasBackend && !previous.IsAttached && runtime.Backend.Implementation is null,
                    "A suspended connection retires its selected constraint before endpoint world transfer");
                PhysicsServer.BodySetSpace(second, secondSpace);
                Check(runtime.HasBackend && !ReferenceEquals(previous, runtime.Backend.Implementation) && runtime.FrameA == frameA && runtime.FrameB == frameB &&
                    PhysicsServer.JointGetType(joint) == role && PhysicsServer.JointGetBias(joint) == .25f && PhysicsServer.JointIsDisabledCollisionsBetweenBodies(joint),
                    "Reconnection uses a fresh selected implementation and preserves RID, local bases and authored policy exactly");
                PhysicsServer.BodySetSpace(first, firstSpace); PhysicsServer.BodySetSpace(second, firstSpace);
                Check(runtime.HasBackend && runtime.FrameA == frameA && runtime.FrameB == frameB, "Reverse transfer preserves sampled local frames");
                PhysicsServer.JointClear(joint);
                Check(!runtime.HasBackend && runtime.Backend.Implementation is null && PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Empty,
                    "Clearing a role retains the public RID and retires concrete constraint storage");
            }
            PhysicsServer.JointMakePin(joint, new(10, 20), first);
            var worldPin = source.SnapshotJoints.Single(value => value.RID == joint);
            Check(worldPin.HasBackend, "A single-body pin uses the selected world-anchor policy");
            if (backend == PhysicsServer.Backend.CPU)
            {
                var owner = (CPUPhysicsWorldBackend)source.BackendImplementation;
                Check(owner.GetJointWorldBody().Equals(owner.GetJointWorldBody()), "The CPU world owns and reuses its one shape-free anchor");
            }
            else Reject<InvalidOperationException>(() => _ = source.WorldID);
            Console.WriteLine($"{backend}: all three joint roles, creation failure, cross-world suspension/reconnect, exact frames/policy and world-anchor ownership passed.");
        }
        finally { PhysicsServer.FreeRID(joint); PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(second); PhysicsServer.FreeRID(firstSpace); PhysicsServer.FreeRID(secondSpace); }
    }
    private static void ColliderAttachment(PhysicsServer.Backend backend)
    {
        using var geometry = new CircleShape { Radius = 5 };
        using var association = new Node();
        using var point = new PhysicsPointQueryParameters { Position = new(10, 20), CollideWithAreas = true };
        var first = PhysicsServer.SpaceCreate(backend); var second = PhysicsServer.SpaceCreate();
        var body = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        try
        {
            var retained = PhysicsServer.Service.BodyRuntime(body).Backend;
            var initialVersion = retained.AttachmentVersion;
            Reject<ArgumentOutOfRangeException>(() => retained.Attach(PhysicsServer.Service.GetSceneSpace(first), default, 0,
                new((PhysicsServer.BodyMode)999)));
            Check(retained.Space is null && retained.Implementation is null && retained.AttachmentVersion == initialVersion,
                "Rejected native creation leaves the retained collider detached and reusable");
            PhysicsServer.BodyAddShape(body, geometry.GetRID()); PhysicsServer.AreaAddShape(area, geometry.GetRID());
            PhysicsServer.BodySetTransform(body, new(0, point.Position)); PhysicsServer.AreaSetTransform(area, new(0, point.Position));
            PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetMass(body, 2); PhysicsServer.BodySetInertia(body, 20);
            PhysicsServer.BodySetLinearVelocity(body, new(3, 4)); PhysicsServer.BodySetAngularVelocity(body, .25f);
            PhysicsServer.BodySetConstantForce(body, new(5, 6)); PhysicsServer.BodyAttachObject(body, association);
            PhysicsColliderImplementation? previous = null;
            PhysicsDirectBodyState? stale = null;
            for (var i = 0; i < 4; i++)
            {
                var target = i % 2 == 0 ? first : second;
                PhysicsServer.BodySetSpace(body, target); PhysicsServer.AreaSetSpace(area, target);
                Check(retained.Implementation is not null && !ReferenceEquals(previous, retained.Implementation) && retained.AttachmentVersion == initialVersion + i + 1,
                    "Every attachment comes from its selected world and advances the retained identity epoch");
                if (previous is CPUPhysicsColliderImplementation cpu) Check(cpu.BodyID.index1 == 0, "Retired CPU attachment clears its borrowed native ID");
                if (previous is GPUPhysicsColliderImplementation gpu) Check(gpu.GPUHandle.Generation == 0, "Retired GPU attachment clears its resident handle");
                if (stale is not null) Reject<InvalidOperationException>(() => _ = stale.Transform);
                var view = PhysicsServer.BodyGetDirectState(body)!;
                Check(view.Transform.Origin.IsEqualApprox(point.Position) && view.LinearVelocity.IsEqualApprox(new(3, 4)) && MathF.Abs(view.AngularVelocity - .25f) < .0001f &&
                    MathF.Abs(view.InverseMass - .5f) < .0001f && view.GetConstantForce() == new Vector2(5, 6),
                    "Attachment transfer preserves scene-unit motion, mass and authored force within .0001 rounding tolerance");
                var hits = PhysicsServer.SpaceGetDirectState(target).IntersectPoint(point);
                Check(hits.Length == 2 && hits[0].ColliderRID == body && hits[0].ColliderObject == association && hits[1].ColliderRID == area,
                    "Body/Area geometry and sampled object association use the selected attachment with stable public RIDs");
                previous = retained.Implementation; stale = view;
            }
            PhysicsServer.BodySetSpace(body, default); PhysicsServer.AreaSetSpace(area, default);
            Check(retained.Space is null && retained.Implementation is null && retained.ShapeCount == 0 && retained.RID == body,
                "Detachment retires concrete storage while retaining common resource identity");
            Console.WriteLine($"{backend}: body/Area attachment creation failure, four world transfers, public state/identity and native retirement passed.");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(second); }
    }
    private static void QueryOwnership(PhysicsServer.Backend backend)
    {
        using var geometry = new CircleShape { Radius = 5 };
        using var queryGeometry = new CircleShape { Radius = 2 };
        using var ray = PhysicsRayQueryParameters.Create(new(-20, 0), new(20, 0));
        using var point = new PhysicsPointQueryParameters();
        using var shape = new PhysicsShapeQueryParameters { Shape = queryGeometry };
        using var sweep = new PhysicsShapeQueryParameters { Shape = queryGeometry, Transform = new(0, new(-20, 0)), Motion = new(40, 0) };
        var points = new PhysicsPointResult[2]; var shapes = new PhysicsShapeResult[2]; var contacts = new Vector2[5];
        contacts[^1] = new(999, 999);
        var first = PhysicsServer.SpaceCreate(backend); var second = PhysicsServer.SpaceCreate(backend);
        var body = PhysicsServer.BodyCreate(); var other = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetMode(other, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(body, geometry.GetRID()); PhysicsServer.BodyAddShape(other, geometry.GetRID());
            PhysicsServer.BodySetTransform(other, new(0, new(100, 0)));
            PhysicsServer.BodySetSpace(body, first); PhysicsServer.BodySetSpace(other, second);
            var direct = PhysicsServer.SpaceGetDirectState(first); var separate = PhysicsServer.SpaceGetDirectState(second);
            Check(direct.IntersectPoint(point).Length == 1 && direct.IntersectShape(shape).Length == 1 && direct.CollideShape(shape).Length > 0,
                "Copied results execute the selected query family");
            Evaluate(direct, true); Evaluate(separate, false);
            Task.Run(() => GuardAll<InvalidOperationException>(direct)).GetAwaiter().GetResult();
            if (backend == PhysicsServer.Backend.CPU)
            {
                var world = b2GetWorldFromId(PhysicsServer.Service.GetSceneSpace(first).WorldID);
                world.locked = true;
                try { GuardAll<InvalidOperationException>(direct); }
                finally { world.locked = false; }
            }
            Collect();
            for (var i = 0; i < 64; i++) { Evaluate(direct, true); Evaluate(separate, false); }
            var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) { Evaluate(direct, true); Evaluate(separate, false); }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Complete query dispatch allocated {owner}/{all} owner/all-thread bytes");
            ray.Exclude = [body]; point.Exclude = [body]; shape.Exclude = [body]; sweep.Exclude = [body];
            Evaluate(direct, false);
            ray.Exclude = []; point.Exclude = []; shape.Exclude = []; sweep.Exclude = [];
            Evaluate(direct, true);
            direct.Dispose(); GuardAll<ObjectDisposedException>(direct);
            var reopened = PhysicsServer.SpaceGetDirectState(first);
            Check(!ReferenceEquals(direct, reopened), "Disposed view reopens on the same selected world");
            Evaluate(reopened, true);
            if (backend == PhysicsServer.Backend.GPU)
            {
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
                PhysicsServer.SpaceSetActive(first, true);
                Reject<AggregateException>(() => PhysicsServer.SpaceStep(first, float.MaxValue));
                GuardAll<InvalidOperationException>(reopened);
            }
            PhysicsServer.FreeRID(first); first = default;
            GuardAll<ArgumentException>(reopened);
            Evaluate(separate, false);
            Console.WriteLine($"{backend}: six query families, alternating hit/miss worlds, all overload guards and 64 warmed cycles: {owner}/{all} owner/all-thread B passed.");

            void Evaluate(PhysicsDirectSpaceState view, bool hit)
            {
                var rayHit = view.IntersectRay(ray);
                Check(hit ? rayHit is { ColliderRID: var rid } && rid == body && MathF.Abs(rayHit.Value.Position.X + 5) < .05f : rayHit is null,
                    "Ray uses its own world and preserves scene-unit geometry within .05 units");
                var pointCount = view.IntersectPoint(point, points); var shapeCount = view.IntersectShape(shape, shapes);
                Check(pointCount == (hit ? 1 : 0) && shapeCount == pointCount && (!hit || points[0].ColliderRID == body && shapes[0].ColliderRID == body),
                    "Point and shape dispatch preserve physical RID identity");
                var pairCount = view.CollideShape(shape, contacts);
                Check((pairCount > 0) == hit && contacts[^1] == new Vector2(999, 999), "Contact pairs preserve unused odd storage");
                var rest = view.GetRestInfo(shape);
                Check(hit ? rest is { ColliderRID: var restRID } && restRID == body && rest.Value.Point.IsFinite() : rest is null,
                    "Rest information uses the selected geometry");
                var fractions = view.CastMotion(sweep);
                Check(hit ? fractions.SafeFraction is > .2f and < .5f && fractions.UnsafeFraction >= fractions.SafeFraction : fractions == (1f, 1f),
                    "Motion fractions bracket the new collision or preserve the no-hit sentinel");
            }
            void GuardAll<T>(PhysicsDirectSpaceState view) where T : Exception
            {
                Reject<T>(() => view.IntersectRay(ray));
                Reject<T>(() => view.IntersectPoint(point)); Reject<T>(() => view.IntersectPoint(point, points));
                Reject<T>(() => view.IntersectPoint(point, Span<PhysicsPointResult>.Empty));
                Reject<T>(() => view.IntersectShape(shape)); Reject<T>(() => view.IntersectShape(shape, shapes));
                Reject<T>(() => view.IntersectShape(shape, Span<PhysicsShapeResult>.Empty));
                Reject<T>(() => view.CastMotion(sweep));
                Reject<T>(() => view.CollideShape(shape)); Reject<T>(() => view.CollideShape(shape, contacts));
                Reject<T>(() => view.CollideShape(shape, Span<Vector2>.Empty));
                Reject<T>(() => view.GetRestInfo(shape));
            }
        }
        finally
        {
            PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(other);
            if (first.IsValid()) PhysicsServer.FreeRID(first);
            PhysicsServer.FreeRID(second);
        }
    }
    private static void MotionOwnership(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var otherWorld = new World(backend);
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 5 }; using var rectangle = new RectangleShape { Size = new(200, 20) };
        var surface = new StaticBody
        {
            Position = new(10000, -8000),
            CollisionLayer = 8,
            ConstantLinearVelocity = new(3, -4),
            ConstantAngularVelocity = 2
        };
        surface.AddChild(new CollisionShape { Shape = circle }); root.AddChild(surface);
        using var tree = new SceneTree(root);
        var mover = PhysicsServer.BodyCreate(); var floor = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        using var hit = new PhysicsTestMotionParameters { Motion = new(0, 120) };
        using var miss = new PhysicsTestMotionParameters { Motion = new(0, -120) };
        using var result = new PhysicsTestMotionResult();
        var space = PhysicsServer.Service.GetSceneSpace(world.Space); var other = PhysicsServer.Service.GetSceneSpace(otherWorld.Space);
        try
        {
            PhysicsServer.BodySetMode(mover, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(mover, circle.GetRID()); PhysicsServer.BodyAddShape(floor, rectangle.GetRID());
            PhysicsServer.BodySetTransform(floor, new(0, new(0, 100))); PhysicsServer.BodySetCollisionLayer(floor, 4);
            PhysicsServer.BodySetCollisionMask(mover, 4);
            PhysicsServer.BodySetLinearVelocity(floor, new(3, -4)); PhysicsServer.BodySetAngularVelocity(floor, 2);
            PhysicsServer.BodySetSpace(mover, world.Space); PhysicsServer.BodySetSpace(floor, world.Space);
            PhysicsServer.AreaSetSpace(area, world.Space);
            Evaluate();
            Check(space.TryGetBodyPointMotion(surface.GetRID(), surface.Position + new Vector2(12, 7), out var velocity, out var layer) &&
                velocity.DistanceTo(new(-11, 20)) < .002f && layer == 8,
                "Scene surface world-point velocity preserves scene/metre rounding within .002 units at a distant authored pose");
            CheckMiss(default); CheckMiss(area);
            Task.Run(() => Reject<InvalidOperationException>(() => space.TryGetBodyPointMotion(floor, Vector2.Zero, out _, out _))).GetAwaiter().GetResult();
            PhysicsServer.BodySetSpace(floor, otherWorld.Space); CheckMiss(floor);
            Check(other.TryGetBodyPointMotion(floor, new(12, 107), out velocity, out layer) && velocity.DistanceTo(new(-11, 20)) < .002f && layer == 4,
                "Indexed lookup follows the fresh selected attachment after world transfer");
            PhysicsServer.BodySetSpace(floor, default); CheckMiss(floor);
            PhysicsServer.BodySetSpace(floor, world.Space);
            hit.ExcludeBodies = [floor];
            Check(!PhysicsServer.BodyTestMotion(mover, hit, result) && result.GetColliderRID() == default, "Excluded owner clears the reusable result");
            hit.ExcludeBodies = [];
            Collect();
            for (var i = 0; i < 64; i++) Evaluate();
            var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) Evaluate();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Motion dispatch and indexed platform lookup allocated {owner}/{all} owner/all-thread bytes");
            root.RemoveChild(surface); CheckMiss(surface.GetRID()); surface.Dispose();
            var freed = floor; PhysicsServer.FreeRID(floor); floor = default; CheckMiss(freed);
            Console.WriteLine($"{backend}: selected hit/miss motion, exact identity/filter/exclusion, distant scene/raw surface velocity, transfer/stale lookup and 64 warmed cycles: {owner}/{all} owner/all-thread B passed.");

            void Evaluate()
            {
                Check(PhysicsServer.BodyTestMotion(mover, hit, result) && result.GetColliderRID() == floor &&
                    result.GetColliderShape() == 0 && result.GetCollisionLocalShape() == 0 && result.GetCollisionNormal().Y < -.9f &&
                    result.GetTravel().Y is > 80 and < 90 && PhysicsServer.BodyGetTransform(mover) == Transform.Identity,
                    "Selected motion owner returns real sweep geometry and logical identities without moving the tested body");
                Check(!PhysicsServer.BodyTestMotion(mover, miss, result) && result.GetTravel().DistanceTo(miss.Motion) < .0001f && result.GetColliderRID() == default,
                    "Alternating hit/miss calls clear identity and preserve travel within .0001 scene units for native unit conversion");
                Check(space.TryGetBodyPointMotion(floor, new(12, 107), out var value, out var mask) && value.DistanceTo(new(-11, 20)) < .002f && mask == 4,
                    "Raw static world-point motion uses selected surface translation and rotation");
            }
            void CheckMiss(RID rid) => Check(!space.TryGetBodyPointMotion(rid, Vector2.Zero, out var value, out var mask) && value == Vector2.Zero && mask == 0,
                "Invalid, Area, foreign, detached and freed identities yield empty platform motion");
        }
        finally { PhysicsServer.FreeRID(mover); if (floor.IsValid()) PhysicsServer.FreeRID(floor); PhysicsServer.FreeRID(area); }
    }
    private static void CallbackBorrow(PhysicsServer.Backend backend)
    {
        using var shape = new CircleShape { Radius = 5 };
        var space = PhysicsServer.SpaceCreate(backend); var body = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        var calls = 0;
        try
        {
            PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.AreaAddShape(area, shape.GetRID());
            PhysicsServer.BodySetSpace(body, space); PhysicsServer.AreaSetSpace(area, space);
            PhysicsServer.AreaSetMonitorCallback(area, (_, _, _, _, _) =>
            {
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(space));
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(space, 1d / 60));
                Check(PhysicsServer.SpaceGetBackend(space) == backend, "Rejected callback release preserves the registry and implementation");
                calls++;
            });
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(calls > 0, "A real monitor callback exercises the borrowed world guard");
            PhysicsServer.AreaSetMonitorCallback(area, null);
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(space); }
    }
    private static void CleanupFailure()
    {
        var rid = PhysicsServer.SpaceCreate(); var space = PhysicsServer.Service.GetSceneSpace(rid); var id = space.WorldID;
        var bodies = new RID[512];
        try
        {
            PhysicsServer.SpaceSetActive(rid, true);
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodySetSpace(body, rid);
            }
            PhysicsServer.SpaceStep(rid, 1d / 60);
            foreach (var body in bodies) PhysicsServer.FreeRID(body);
            Array.Clear(bodies);
            space.BackendImplementation.Tasks.Dispose();
            Reject<AggregateException>(() => PhysicsServer.FreeRID(rid));
            Check(!b2World_IsValid(id), "Worker cleanup failure cannot skip native world destruction");
            Reject<ArgumentException>(() => PhysicsServer.SpaceGetBackend(rid));
            Reject<ObjectDisposedException>(space.BackendImplementation.EnsureAccess);
            Console.WriteLine("Injected worker cleanup failure is aggregated after native world release.");
        }
        finally
        {
            foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
            if (b2World_IsValid(id)) PhysicsServer.FreeRID(rid);
        }
    }
    internal static void RunNoDevice()
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(typeof(PhysicsBackendOwnershipTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_BACKEND_OWNERSHIP"] = "no-device-child";
        start.Environment["SDL_GPU_DRIVER"] = "electron2d-no-such-driver"; start.Environment["SDL_VIDEODRIVER"] = "dummy";
        start.Environment["XDG_SESSION_TYPE"] = "unspecified";
        start.Environment.Remove("DISPLAY"); start.Environment.Remove("WAYLAND_DISPLAY");
        using var child = Process.Start(start)!;
        var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(60_000)) { child.Kill(true); throw new InvalidOperationException("Backend ownership no-device child timed out."); }
        Check(child.ExitCode == 0, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
        Console.Write(stdout.GetAwaiter().GetResult());
    }
    internal static void RunNoDeviceChild()
    {
        Run(PhysicsServer.Backend.CPU);
        PhysicsBodyTransformTests.Run(PhysicsServer.Backend.CPU);
        Reject<InvalidOperationException>(() => PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU));
        var fallback = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU, allowCPUFallback: true);
        try
        {
            var space = PhysicsServer.Service.GetSceneSpace(fallback);
            Check(space.BackendImplementation is CPUPhysicsWorldBackend && space.RequestedBackend == PhysicsServer.Backend.GPU &&
                space.ActualBackend == PhysicsServer.Backend.CPU && !string.IsNullOrWhiteSpace(space.BackendFallbackReason), "Startup fallback owns an actual CPU implementation with retained request diagnostics");
        }
        finally { PhysicsServer.FreeRID(fallback); }
        Console.WriteLine("Fresh CPU implementation, strict unavailable GPU and explicit startup fallback passed without a graphics device.");
    }
    private static void Collect() { GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
}
