using Electron2D;

public static partial class BackendChecks
{
    public static void RunCCDPolicies(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("CCDPolicies", () => { var created = new Solver(); solver ??= created; return created; });
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("CCDPolicies");
            using var other = backend is { } peer ? new World(peer) : new World("CCDPolicies");
            using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 1 };
            PhysicsBody[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
            RID[] ids = [.. nodes.Select(node => node.GetRID()), PhysicsServer.BodyCreate()];
            var targets = new RID[ids.Length]; var views = new PhysicsDirectBodyState[ids.Length];
            var callbackCounts = new int[ids.Length]; var beforeCallbacks = new int[ids.Length];
            var numericalReads = 0; var probeWrites = true; var owner = Environment.CurrentManagedThreadId; var callbacks = 0; var faults = 0;
            try
            {
                for (var i = 0; i < ids.Length; i++)
                {
                    Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(ids[i]) == CCDMode.Disabled, "Every sibling and raw body starts with Disabled CCD");
                    PhysicsServer.BodySetContinuousCollisionDetectionMode(ids[i], CCDMode.CastRay);
                    if (i < nodes.Length) root.AddChild(nodes[i]);
                }
                using var tree = new SceneTree(root);
                for (var i = 0; i < ids.Length; i++)
                {
                    PhysicsServer.BodySetMode(ids[i], PhysicsServer.BodyMode.Rigid);
                    PhysicsServer.BodySetGravityScale(ids[i], 0); PhysicsServer.BodySetMass(ids[i], 2); PhysicsServer.BodySetCanSleep(ids[i], true);
                    PhysicsServer.BodySetLinearDampMode(ids[i], RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(ids[i], 0);
                    PhysicsServer.BodySetAngularDampMode(ids[i], RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(ids[i], 0);
                    PhysicsServer.BodySetFriction(ids[i], 0); PhysicsServer.BodySetBounce(ids[i], 0); PhysicsServer.BodySetMaxContactsReported(ids[i], 2);
                    PhysicsServer.BodyAddShape(ids[i], circle.GetRID(), disabled: true); PhysicsServer.BodyAddShape(ids[i], circle.GetRID());
                    PhysicsServer.BodySetTransform(ids[i], Start(i)); PhysicsServer.BodySetSpace(ids[i], world.Space);
                    var target = targets[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(target, PhysicsServer.BodyMode.Static);
                    PhysicsServer.BodyAddShape(target, circle.GetRID()); PhysicsServer.BodySetTransform(target, new(0, new(20, i * 100)));
                    PhysicsServer.BodySetFriction(target, 0); PhysicsServer.BodySetBounce(target, 0); PhysicsServer.BodySetSpace(target, world.Space);
                    var index = i; PhysicsServer.BodySetStateSyncCallback(ids[i], _ => { callbacks++; callbackCounts[index]++; }); views[i] = PhysicsServer.BodyGetDirectState(ids[i])!;
                }
                Check(world.PhysicsBackendName == (backend?.ToString() ?? "CCDPolicies") && world.PhysicsFallbackReason is null,
                    "CCD policy uses the actual requested implementation without fallback");
                var wrongArea = PhysicsServer.AreaCreate(); var stale = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(stale);
                try
                {
                    foreach (var wrong in new[] { default, wrongArea, circle.GetRID(), world.Space, stale })
                    {
                        Reject<ArgumentException>(() => PhysicsServer.BodyGetContinuousCollisionDetectionMode(wrong));
                        Reject<ArgumentException>(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(wrong, CCDMode.CastShape));
                    }
                }
                finally { PhysicsServer.FreeRID(wrongArea); }
                foreach (var id in ids)
                {
                    foreach (var invalid in new[] { (CCDMode)(-1), (CCDMode)3 }) Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(id, invalid));
                    Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(id) == CCDMode.CastRay, "Invalid modes preserve configured CCD");
                }
                Task.Run(() =>
                {
                    foreach (var id in ids)
                    {
                        Reject<InvalidOperationException>(() => PhysicsServer.BodyGetContinuousCollisionDetectionMode(id));
                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(id, CCDMode.Disabled));
                    }
                }).GetAwaiter().GetResult();
                for (var i = 0; i < ids.Length; i++)
                    foreach (var mode in new[] { CCDMode.Disabled, CCDMode.CastRay, CCDMode.CastShape })
                    {
                        PhysicsServer.BodySetContinuousCollisionDetectionMode(ids[i], mode);
                        PhysicsServer.BodySetSleeping(ids[i], true); PhysicsServer.BodySetContinuousCollisionDetectionMode(ids[i], mode);
                        Check(PhysicsServer.BodyGetSleeping(ids[i]), "An equal CCD assignment leaves dynamic sleep unchanged");
                        var next = (CCDMode)(((int)mode + 1) % 3); PhysicsServer.BodySetContinuousCollisionDetectionMode(ids[i], next);
                        Check(!PhysicsServer.BodyGetSleeping(ids[i]), "A changed CCD mode wakes its dynamic body");
                        foreach (var role in new[] { PhysicsServer.BodyMode.Static, PhysicsServer.BodyMode.Kinematic, PhysicsServer.BodyMode.RigidLinear, PhysicsServer.BodyMode.Rigid })
                        {
                            PhysicsServer.BodySetMode(ids[i], role);
                            Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(ids[i]) == next, "Motion-role changes retain the per-body policy");
                        }
                        PhysicsServer.BodySetSpace(ids[i], default); PhysicsServer.BodySetContinuousCollisionDetectionMode(ids[i], mode);
                        PhysicsServer.BodySetSpace(ids[i], other.Space);
                        Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(ids[i]) == mode, "Detach and transfer retain authored CCD");
                        PhysicsServer.BodySetSpace(ids[i], world.Space); views[i] = PhysicsServer.BodyGetDirectState(ids[i])!;
                        if (i is 3 or 4) Check(((RigidBody)nodes[i]).ContinuousCD == mode, "Rigid and bone descriptors share server CCD authoring");
                    }
                if (solver is not null) solver.StepHook = () =>
                {
                    Check((Environment.CurrentManagedThreadId != owner) == threaded, "CCD configured reads run on the selected numerical lane");
                    foreach (var id in ids) Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(id) == solver.Bodies[id].ContinuousMode,
                        "All six authored modes reach the selected integration configuration");
                    numericalReads++;
                    if (!probeWrites) return;
                    foreach (var id in ids)
                    {
                        var mode = PhysicsServer.BodyGetContinuousCollisionDetectionMode(id);
                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(id, mode));
                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(id, (CCDMode)(((int)mode + 1) % 3)));
                        Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(id) == mode, "Borrowed writes reject before configured CCD mutation");
                    }
                    probeWrites = false;
                };
                for (var i = 0; i < ids.Length; i++)
                {
                    PhysicsServer.BodySetContinuousCollisionDetectionMode(ids[i], (CCDMode)(i % 3));
                    for (var j = 0; j < ids.Length; j++) Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(ids[j]) ==
                        (j <= i ? (CCDMode)(j % 3) : CCDMode.CastShape), "Changing one body leaves every other configured mode unchanged");
                }
                tree.PhysicsFrame(.0001);
                if (solver is not null)
                    for (var i = 0; i < ids.Length; i++)
                        foreach (var mode in new[] { CCDMode.Disabled, CCDMode.CastRay, CCDMode.CastShape })
                            for (var failures = 1; failures <= 2; failures++)
                            {
                                solver.AwakeHook = null; PhysicsServer.BodySetContinuousCollisionDetectionMode(ids[i], (CCDMode)(((int)mode + 1) % 3));
                                PhysicsServer.BodySetSleeping(ids[i], true); var attempts = 0; var failure = new ApplicationException("ccd-wake");
                                solver.AwakeHook = id =>
                                {
                                    if (id != ids[i]) return;
                                    if (++attempts > failures) { solver.AwakeHook = null; return; }
                                    Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(id) == mode, "A failing wake observes committed CCD without recursive preparation");
                                    throw failure;
                                };
                                var errors = ReleaseErrors(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(ids[i], mode));
                                Check(errors.Length == 1 && ReferenceEquals(errors[0], failure) && attempts == 1, "CCD publication preserves the original wake failure");
                                Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(ids[i]) == mode && attempts == 1, "Authored reads do not repair a failed wake");
                                var tick = PhysicsServer.SpaceGetTick(world.Space);
                                if (failures == 2)
                                {
                                    errors = ReleaseErrors(() => _ = PhysicsServer.BodyGetDirectState(ids[i]));
                                    Check(errors.Length == 1 && ReferenceEquals(errors[0], failure) && attempts == 2, "A failed repair retains the same error and debt");
                                }
                                Check(ReferenceEquals(PhysicsServer.BodyGetDirectState(ids[i]), views[i]) && !PhysicsServer.BodyGetSleeping(ids[i]) &&
                                    PhysicsServer.SpaceGetTick(world.Space) == tick, "Successful wake repair preserves view identity and does not run simulation");
                                faults++;
                            }
                for (var frame = 0; frame < 128; frame++) Frame(frame);
                Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); var beforeReads = numericalReads;
                for (var frame = 0; frame < 128; frame++) Frame(frame);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"128 complete CCD policy frames allocated {own}/{all} owner/all-thread B");
                Check(solver is null || numericalReads == beforeReads + 128, "Every measured frame reads all six modes during actual Solve");
                if (solver is not null)
                {
                    solver.StepHook = null; solver.FailSolve = true; Reject<AggregateException>(() => tree.PhysicsFrame(.02));
                    foreach (var id in ids)
                    {
                        Reject<InvalidOperationException>(() => PhysicsServer.BodyGetContinuousCollisionDetectionMode(id));
                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(id, CCDMode.Disabled));
                    }
                }
                Console.WriteLine($"CCD policies {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: six owners, 54 lifecycle transitions, {faults} wake failures, 128 complete numerical/contact frames {own}/{all} B passed.");

                void Frame(int frame)
                {
                    // A discrete control must not share other bodies' continuous intervals.
                    var mode = (CCDMode)(frame % 3);
                    for (var i = 0; i < ids.Length; i++)
                    {
                        beforeCallbacks[i] = callbackCounts[i];
                        PhysicsServer.BodySetTransform(ids[i], Start(i)); PhysicsServer.BodySetLinearVelocity(ids[i], new(3000, 0));
                        PhysicsServer.BodySetContinuousCollisionDetectionMode(ids[i], mode);
                    }
                    var before = callbacks; tree.PhysicsFrame(.02); Check(callbacks == before + ids.Length, "Each CCD body publishes one callback per outer tick");
                    for (var i = 0; i < ids.Length; i++)
                    {
                        Check(callbackCounts[i] == beforeCallbacks[i] + 1, "Every individual body receives one outer callback");
                        var pose = PhysicsServer.BodyGetTransform(ids[i]).Origin;
                        var velocity = PhysicsServer.BodyGetLinearVelocity(ids[i]);
                        Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(ids[i]) == mode &&
                            PhysicsServer.BodyGetContinuousCollisionDetectionMode(targets[i]) == CCDMode.Disabled &&
                            ReferenceEquals(PhysicsServer.BodyGetDirectState(ids[i]), views[i]), "Per-body CCD and cached view identity survive real solver intervals");
                        Check(MathF.Abs(pose.Y - i * 100) < .02f, "Independent CCD lanes preserve transverse position");
                        if (mode == CCDMode.Disabled)
                        {
                            if (MathF.Abs(pose.X - 60) >= .02f || MathF.Abs(velocity.X - 3000) >= .02f)
                                throw new InvalidOperationException($"Disabled control owner={i}, frame={frame}, pose={pose}, velocity={velocity}, role={PhysicsServer.BodyGetMode(ids[i])}");
                        }
                        else
                        {
                            Check(MathF.Abs(pose.X - 18) < .02f && velocity.Length() < .05f, "Both continuous modes stop at the actual circle surface");
                            Check(views[i].GetContactCount() == 1 && views[i].GetContactCollider(0) == targets[i] &&
                                views[i].GetContactLocalShape(0) == 1 && views[i].GetContactColliderShape(0) == 0 &&
                                views[i].GetContactImpulse(0).DistanceTo((velocity - new Vector2(3000, 0)) * 2) < .1f,
                                "CCD retains logical shape gaps, collider identity and complete physical impulse");
                        }
                    }
                }
            }
            finally
            {
                if (solver is not null) { solver.AwakeHook = null; solver.StepHook = null; }
                foreach (var target in targets) if (target.IsValid()) PhysicsServer.FreeRID(target);
                PhysicsServer.FreeRID(ids[^1]);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        static Transform Start(int i) => new(0, new(0, i * 100));
    }
}
