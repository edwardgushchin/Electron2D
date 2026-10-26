using Electron2D;

public static partial class BackendChecks
{
    public static void RunBodyParameters(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (backend is null) PhysicsServerManager.RegisterServer("BodyParameters", () => new Solver());
            PhysicsBodyParameterTests.Run(backend);
            if (backend is null) ParameterReadChecks();
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    public static void RunParameterReads() => ParameterReadChecks();
    private static void ParameterReadChecks()
    {
        var ownerThread = Thread.CurrentThread;
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("ParameterReads", () => solver = new());
        using var world = new World("ParameterReads"); var space = world.Space;
        using var circle = new CircleShape { Radius = 3 };
        using var root = new SubViewport { World = world };
        var scene = new RigidBody
        {
            Mass = 2,
            Inertia = 8,
            GravityScale = 3,
            LinearDamp = 4,
            AngularDamp = 5,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace,
            CenterOfMassMode = RigidCenterOfMassMode.Custom,
            CenterOfMass = new(1, 0)
        };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        var raw = PhysicsServer.BodyCreate(); var preparations = 0; var rawRead = false; var sceneRead = false;
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID());
            PhysicsServer.BodySetMass(raw, 2); PhysicsServer.BodySetInertia(raw, 8); PhysicsServer.BodySetCenterOfMass(raw, new(1, 0));
            PhysicsServer.BodySetGravityScale(raw, 3); PhysicsServer.BodySetLinearDamp(raw, 4); PhysicsServer.BodySetAngularDamp(raw, 5);
            PhysicsServer.BodySetLinearDampMode(raw, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(raw, RigidBody.DampMode.Replace);
            PhysicsServer.BodySetFriction(raw, .7f); PhysicsServer.BodySetBounce(raw, .2f);
            PhysicsServer.BodySetFriction(scene.GetRID(), .7f); PhysicsServer.BodySetBounce(scene.GetRID(), .2f);
            solver!.ShapeHook = _ => preparations++; solver.PolicyHook = _ => preparations++;
            solver.IdentityHook = Read;
            PhysicsServer.BodySetSpace(raw, space);
            using var tree = new SceneTree(root);
            tree.PhysicsFrame(0); solver.IdentityHook = null;
            Check(rawRead && sceneRead, "Initial scene/raw hooks can read authored body parameters");
            Read(raw); Read(scene.GetRID());
            solver.BodyHook = Read;
            PhysicsServer.BodySetLinearVelocity(raw, Vector2.One); PhysicsServer.BodySetLinearVelocity(scene.GetRID(), Vector2.One);
            solver.BodyHook = null;
            PhysicsServer.BodyResetMassProperties(raw);
            solver.IdentityHook = _ =>
            {
                var before = preparations;
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetInertia(raw));
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetCenterOfMass(raw));
                Check(preparations == before && PhysicsServer.BodyGetMass(raw) == 2,
                    "Automatic geometry reads reject borrowing before preparation while authored mass stays readable");
            };
            PhysicsServer.BodyAttachCanvasInstanceID(raw, 17); solver.IdentityHook = null;
            Check(MathF.Abs(PhysicsServer.BodyGetInertia(raw) - 9) < .0001f && PhysicsServer.BodyGetCenterOfMass(raw) == Vector2.Zero,
                "Automatic mass getters prepare selected geometry outside numerical hooks");
            circle.Radius = 6;
            Check(MathF.Abs(PhysicsServer.BodyGetInertia(raw) - 36) < .0001f,
                "Automatic inertia observes ordinary resource edits before the next step");
            PhysicsServer.BodySetInertia(raw, 8); PhysicsServer.BodySetCenterOfMass(raw, new(1, 0));
            using var material = new PhysicsMaterial();
            Action<Resource> fail = _ => throw new ApplicationException("Injected earlier material subscriber failure");
            material.Changed += fail; scene.PhysicsMaterialOverride = material;
            PhysicsServer.BodySetFriction(scene.GetRID(), .7f); PhysicsServer.BodySetBounce(scene.GetRID(), .2f);
            var beforeMaterialRead = preparations;
            Reject<ApplicationException>(() => material.Friction = .3f);
            Check(PhysicsServer.BodyGetFriction(scene.GetRID()) == .3f && PhysicsServer.BodyGetBounce(scene.GetRID()) == 0 &&
                preparations == beforeMaterialRead, "Material revision repairs authored overrides without numerical preparation");
            Reject<ApplicationException>(() => material.Bounce = .4f);
            Check(PhysicsServer.BodyGetBounce(scene.GetRID()) == .4f && preparations == beforeMaterialRead,
                "Bounce reload preserves the same nonpreparing material revision repair");
            material.Changed -= fail; scene.PhysicsMaterialOverride = null;
            PhysicsServer.BodySetFriction(scene.GetRID(), .7f); PhysicsServer.BodySetBounce(scene.GetRID(), .2f);
            foreach (var id in new[] { raw, scene.GetRID() })
            {
                PhysicsServer.BodySetContinuousCollisionDetectionMode(id, CCDMode.CastShape);
                PhysicsServer.BodySetCanSleep(id, false);
                PhysicsServer.BodySetOmitForceIntegration(id, true);
                PhysicsServer.BodySetConstantForce(id, new(2, 3)); PhysicsServer.BodySetConstantTorque(id, 4);
                PhysicsServer.BodySetMaxContactsReported(id, 2); PhysicsServer.BodySetContactsReportedDepthThreshold(id, .5f);
            }
            var solvedReads = 0; var checkedSolveGuards = false;
            solver.StepHook = () =>
            {
                Read(raw); Read(scene.GetRID()); ReadPolicies(raw); ReadPolicies(scene.GetRID()); solvedReads++;
                if (checkedSolveGuards) return;
                checkedSolveGuards = true;
                if (Thread.CurrentThread != ownerThread)
                    Reject<InvalidOperationException>(() => _ = scene.CanSleep);
                foreach (var id in new[] { raw, scene.GetRID() })
                {
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Rigid));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(id, CCDMode.CastShape));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetCanSleep(id, false));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetOmitForceIntegration(id, true));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetConstantForce(id, new(2, 3)));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetConstantTorque(id, 4));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetMaxContactsReported(id, 2));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetContactsReportedDepthThreshold(id, .5f));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodyGetLinearVelocity(id));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodyGetDirectState(id));
                }
            };
            tree.PhysicsFrame(1d / 120);
            Check(solvedReads == 1, "The complete configured body family is readable inside the actual numerical solve");
            Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) { Read(raw); Read(scene.GetRID()); tree.PhysicsFrame(1d / 120); }
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) { Read(raw); Read(scene.GetRID()); tree.PhysicsFrame(1d / 120); }
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Authored parameter reads allocated {own}/{all} owner/all-thread B");
            Task.Run(() => Reject<InvalidOperationException>(() => PhysicsServer.BodyGetMass(raw))).GetAwaiter().GetResult();
            PhysicsServer.BodyResetMassProperties(raw);
            solver.StepHook = () =>
            {
                var before = preparations;
                Check(PhysicsServer.BodyGetMass(raw) == 2, "Authored mass remains readable for an automatic profile while solving");
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetInertia(raw));
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetCenterOfMass(raw));
                Check(preparations == before, "Automatic geometry reads reject before numerical preparation while solving");
            };
            tree.PhysicsFrame(1d / 120);
            PhysicsServer.BodySetInertia(raw, 8); PhysicsServer.BodySetCenterOfMass(raw, new(1, 0));
            Action<RID>[] reads = [id => PhysicsServer.BodyGetMass(id), id => PhysicsServer.BodyGetInertia(id),
                id => PhysicsServer.BodyGetCenterOfMass(id), id => PhysicsServer.BodyGetFriction(id), id => PhysicsServer.BodyGetBounce(id),
                id => PhysicsServer.BodyGetGravityScale(id), id => PhysicsServer.BodyGetLinearDamp(id), id => PhysicsServer.BodyGetAngularDamp(id),
                id => PhysicsServer.BodyGetLinearDampMode(id), id => PhysicsServer.BodyGetAngularDampMode(id),
                id => PhysicsServer.BodyGetMode(id), id => PhysicsServer.BodyGetContinuousCollisionDetectionMode(id),
                id => PhysicsServer.BodyGetCanSleep(id), id => PhysicsServer.BodyIsOmittingForceIntegration(id),
                id => PhysicsServer.BodyGetConstantForce(id), id => PhysicsServer.BodyGetConstantTorque(id),
                id => PhysicsServer.BodyGetMaxContactsReported(id), id => PhysicsServer.BodyGetContactsReportedDepthThreshold(id)];
            Task.Run(() => { foreach (var id in new[] { raw, scene.GetRID() }) foreach (var read in reads) Reject<InvalidOperationException>(() => read(id)); }).GetAwaiter().GetResult();
            using var wrong = new Area();
            foreach (var read in reads) Reject<ArgumentException>(() => read(wrong.GetRID()));
            var stale = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(stale);
            foreach (var read in reads) Reject<ArgumentException>(() => read(stale));
            solver.StepHook = null; solver.FailSolve = true;
            Reject<AggregateException>(() => tree.PhysicsFrame(1d / 120));
            foreach (var id in new[] { raw, scene.GetRID() }) foreach (var read in reads) Reject<InvalidOperationException>(() => read(id));
            Console.WriteLine($"Registered parameter reads: complete configured family inside solving, 128 active full frames and 0/0 owner/all-thread B");
        }
        finally
        {
            solver!.IdentityHook = null; solver.BodyHook = null; solver.ShapeHook = null; solver.PolicyHook = null; solver.StepHook = null;
            PhysicsServer.FreeRID(raw);
        }
        void ReadPolicies(RID id)
        {
            var before = preparations;
            Check(PhysicsServer.BodyGetMode(id) == PhysicsServer.BodyMode.Rigid &&
                PhysicsServer.BodyGetContinuousCollisionDetectionMode(id) == CCDMode.CastShape &&
                !PhysicsServer.BodyGetCanSleep(id) && PhysicsServer.BodyIsOmittingForceIntegration(id) &&
                PhysicsServer.BodyGetConstantForce(id) == new Vector2(2, 3) && PhysicsServer.BodyGetConstantTorque(id) == 4 &&
                PhysicsServer.BodyGetMaxContactsReported(id) == 2 && PhysicsServer.BodyGetContactsReportedDepthThreshold(id) == .5f,
                "Numerical hooks read the complete authored policy without observing solver state");
            Check(preparations == before, "Policy reads do not reenter numerical preparation");
        }
        void Read(RID id)
        {
            var before = preparations;
            Check(PhysicsServer.BodyGetMass(id) == 2 && PhysicsServer.BodyGetInertia(id) == 8 && PhysicsServer.BodyGetCenterOfMass(id) == new Vector2(1, 0) &&
                PhysicsServer.BodyGetGravityScale(id) == 3 && PhysicsServer.BodyGetLinearDamp(id) == 4 && PhysicsServer.BodyGetAngularDamp(id) == 5 &&
                PhysicsServer.BodyGetLinearDampMode(id) == RigidBody.DampMode.Replace && PhysicsServer.BodyGetAngularDampMode(id) == RigidBody.DampMode.Replace &&
                PhysicsServer.BodyGetFriction(id) == .7f && PhysicsServer.BodyGetBounce(id) == .2f, "Authored getters preserve every body parameter value");
            Check(preparations == before, "Authored getters do not recursively invoke numerical preparation hooks");
            rawRead |= id == raw; sceneRead |= id == scene.GetRID();
        }
    }
}
