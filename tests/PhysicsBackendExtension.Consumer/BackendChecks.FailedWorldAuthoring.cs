using Electron2D;

public static partial class BackendChecks
{
    public static void RunFailedWorldAuthoring(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null; var rejected = 0;
        if (backend is null) PhysicsServerManager.RegisterServer("FailedWorldAuthoring", () => solver = new());
        try
        {
            if (backend is null)
                for (var failureKind = 0; failureKind < 3; failureKind++)
                    for (var direct = 0; direct < 2; direct++) Run(failureKind, direct != 0);
            Run(-1, false);
            Console.WriteLine($"Failed-world authoring {backend?.ToString() ?? "registered"}/{threaded}: {rejected} rejected operations; healthy callback frames passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }

        void Run(int failureKind, bool direct)
        {
            using var world = backend is { } selected ? new World(selected) : new World("FailedWorldAuthoring");
            using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 2 };
            var rig = new Skeleton(); var guide = new Bone { Name = "Guide", Position = new(400, 0) }; rig.AddChild(guide); root.AddChild(rig);
            var bone = new PhysicalBone { Name = "Body", BoneNodePath = "../Guide", SimulatePhysics = true };
            PhysicsBody[] bodies = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), bone];
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i]; body.Position = new(i * 100, 0); body.CollisionMask = 0;
                body.AddChild(new CollisionShape { Shape = circle });
                if (body == bone) rig.AddChild(body); else root.AddChild(body);
                if (body is StaticBody surface) { surface.ConstantLinearVelocity = new(30, -4); surface.ConstantAngularVelocity = 2; }
                if (body is RigidBody rigid) { rigid.GravityScale = 0; rigid.CanSleep = false; rigid.LinearVelocity = new(6, 0); rigid.AngularVelocity = 3; }
            }
            using var tree = new SceneTree(root); var raw = PhysicsServer.BodyCreate();
            var ids = bodies.Select(body => body.GetRID()).Append(raw).ToArray();
            var views = new PhysicsDirectBodyState[ids.Length];
            try
            {
                PhysicsServer.BodySetSpace(raw, world.Space); PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
                for (var i = 0; i < ids.Length; i++)
                {
                    if (direct && i < bodies.Length) PhysicsServer.BodySetMode(ids[i], PhysicsServer.BodyMode.Rigid);
                    if (direct || i == bodies.Length) { PhysicsServer.BodySetLinearVelocity(ids[i], new(6, 0)); PhysicsServer.BodySetAngularVelocity(ids[i], 3); }
                    views[i] = PhysicsServer.BodyGetDirectState(ids[i])!;
                }
                if (failureKind < 0)
                {
                    var mover = (RigidBody)bodies[3]; var surface = (StaticBody)bodies[0]; var callbacks = 0;
                    PhysicsServer.BodySetForceIntegrationCallback(raw, _ =>
                    {
                        mover.LinearVelocity = new(8, 0); mover.AngularVelocity = 1; mover.CanSleep = false; mover.Sleeping = false;
                        surface.ConstantLinearVelocity = new(31, -4); surface.ConstantAngularVelocity = 3;
                        Check(!mover.Sleeping, "Healthy force callbacks retain numerical scene access"); callbacks++;
                    });
                    PhysicsServer.BodySetStateSyncCallback(raw, _ =>
                    {
                        mover.LinearVelocity = new(6, 0); mover.AngularVelocity = 3;
                        surface.ConstantLinearVelocity = new(30, -4); surface.ConstantAngularVelocity = 2; callbacks++;
                    });
                    for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
                    Collect(); for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
                    var before = callbacks; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                    for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
                    own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                    Check(callbacks - before == 256 && own == 0 && all == 0,
                        $"128 healthy callback frames: callbacks={callbacks - before}, allocated={own}/{all} owner/all-thread B");
                    Console.WriteLine($"Healthy callbacks {backend?.ToString() ?? "registered"}/{threaded}: 128 full frames {own}/{all} owner/all-thread B.");
                    PhysicsServer.BodySetForceIntegrationCallback(raw, null); PhysicsServer.BodySetStateSyncCallback(raw, null);
                    return;
                }
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
                using var map = new PhysicsSnapshotMap(world.Space); var snapshot = new PhysicsSnapshot();
                for (var i = 0; i < ids.Length; i++) map.Bind((ulong)i + 1, 1, ids[i]);
                map.Capture(snapshot);
                var failure = new ApplicationException("selected-world-execution-failure");
                if (failureKind < 2) solver!.RestoreExecutionHook = () => throw failure;
                else solver!.StepHook = () => throw failure;
                var errors = ReleaseErrors(() => { if (failureKind == 0) checkpoint.Restore(); else if (failureKind == 1) map.Apply(snapshot); else tree.PhysicsFrame(.01); });
                Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), "Terminal world retains the original execution error");
                solver!.RestoreExecutionHook = solver.StepHook = null;
                var mutations = 0; var solves = solver.Steps;
                solver.ModeHook = solver.RotationHook = solver.BodyHook = solver.AngularHook = solver.SurfaceHook = solver.AwakeHook = solver.CanSleepHook = _ => mutations++;
                Denied(() => _ = PhysicsServer.SpaceIsActive(world.Space));
                Denied(() => tree.PhysicsFrame(.01));
                foreach (var policy in WorldPolicies(world.Space))
                {
                    Denied(() => _ = policy.Get());
                    Denied(() => policy.Set(policy.First));
                }
                for (var i = 0; i < ids.Length; i++)
                {
                    var id = ids[i]; var view = views[i];
                    foreach (var action in ForceCalls(id).Concat(ForceCalls(id, view))) Denied(action);
                    Denied(() => PhysicsServer.BodyAddCollisionException(id, default));
                    Denied(() => PhysicsServer.BodyRemoveCollisionException(id, default));
                    Denied(() => PhysicsServer.BodySetLinearVelocity(id, Vector2.One));
                    Denied(() => PhysicsServer.BodySetAngularVelocity(id, 9));
                    Denied(() => PhysicsServer.BodySetCanSleep(id, true));
                    Denied(() => PhysicsServer.BodySetSleeping(id, true));
                    Denied(() => view.LinearVelocity = Vector2.One); Denied(() => view.AngularVelocity = 9);
                    Denied(() => view.Sleeping = true); Denied(() => _ = view.InverseMass);
                }
                foreach (var body in bodies)
                {
                    Denied(() => body.AddCollisionExceptionWith(bodies[0]));
                    Denied(() => body.RemoveCollisionExceptionWith(bodies[0]));
                    Denied(() => body.DisableMode = CollisionDisableMode.KeepActive);
                    Denied(() => body.DisableMode = body.DisableMode);
                    if (body is StaticBody surface)
                    {
                        Denied(() => surface.ConstantLinearVelocity = Vector2.One); Denied(() => surface.ConstantAngularVelocity = 9);
                        Denied(() => surface.ConstantLinearVelocity = new(30, -4)); Denied(() => surface.ConstantAngularVelocity = 2);
                        Check(surface.ConstantLinearVelocity == new Vector2(30, -4) && surface.ConstantAngularVelocity == 2,
                            "Terminal surface writes preserve non-preparing authored values");
                    }
                    if (body is RigidBody rigid)
                    {
                        Denied(() => rigid.ConstantForce = Vector2.Zero);
                        Denied(() => rigid.ConstantTorque = 0);
                        Denied(() => rigid.ConstantForce = Vector2.One);
                        Denied(() => rigid.ConstantTorque = 9);
                        Check(rigid.ConstantForce == Vector2.Zero && rigid.ConstantTorque == 0, "Terminal constant writes retain both authored channels");
                        Denied(() => rigid.LinearVelocity = Vector2.One); Denied(() => rigid.AngularVelocity = 9);
                        Denied(() => rigid.LinearVelocity = new(6, 0)); Denied(() => rigid.AngularVelocity = 3);
                        Denied(() => rigid.CanSleep = true); Denied(() => rigid.CanSleep = false);
                        Denied(() => rigid.Sleeping = true); Denied(() => rigid.Sleeping = false);
                        Denied(() => rigid.Freeze = true); Denied(() => rigid.Freeze = false);
                        Denied(() => rigid.FreezeMode = RigidFreezeMode.Kinematic); Denied(() => rigid.FreezeMode = RigidFreezeMode.Static);
                        Denied(() => rigid.LockRotation = true); Denied(() => rigid.LockRotation = false);
                        Denied(() => _ = rigid.Sleeping);
                        if (direct) { Denied(() => _ = rigid.LinearVelocity); Denied(() => _ = rigid.AngularVelocity); }
                        else Check(rigid.LinearVelocity == new Vector2(6, 0) && rigid.AngularVelocity == 3, "Native authored motion stays non-preparing");
                        Check(!rigid.CanSleep && !rigid.Freeze && !rigid.LockRotation && rigid.FreezeMode == RigidFreezeMode.Static,
                            "Rejected role/sleep writes leave authored policy unchanged");
                    }
                }
                Denied(() => bone.SimulatePhysics = false); Denied(() => bone.SimulatePhysics = true);
                Check(bone.SimulatePhysics && solver.Steps == solves && mutations == 0, "Terminal writes invoke no selected mutation or replay");
                solver.ModeHook = solver.RotationHook = solver.BodyHook = solver.AngularHook = solver.SurfaceHook = solver.AwakeHook = solver.CanSleepHook = null;
                foreach (var body in bodies)
                {
                    body.Parent!.RemoveChild(body);
                    if (body is RigidBody rigid)
                    {
                        var inactive = body is PhysicalBone && !direct;
                        if (!(rigid.LinearVelocity == new Vector2(6, 0) && rigid.AngularVelocity == 3 && rigid.Sleeping == inactive))
                            throw new InvalidOperationException($"Retirement {body.GetType().Name}/{failureKind}/{direct}: velocity {rigid.LinearVelocity}/{rigid.AngularVelocity}, sleeping {rigid.Sleeping}, expected inactive {inactive}.");
                    }
                    body.Dispose();
                }
                void Denied(Action action)
                {
                    var denied = ReleaseErrors(action); rejected++;
                    Check(denied.Length != 0 && denied.All(error => error is InvalidOperationException), "Terminal access reports the world failure");
                    Check(mutations == 0 && solver.Steps == solves, "Rejected access neither publishes nor solves");
                }
            }
            finally
            {
                if (solver is not null) { solver.RestoreExecutionHook = solver.StepHook = null; solver.ModeHook = solver.RotationHook = solver.BodyHook = solver.AngularHook = solver.SurfaceHook = solver.AwakeHook = solver.CanSleepHook = null; }
                foreach (var view in views) view?.Dispose();
                PhysicsServer.FreeRID(raw);
            }
        }
    }
}
