using Electron2D;

public static partial class BackendChecks
{
    private static void ParameterReadChecks()
    {
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
            Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) { Read(raw); Read(scene.GetRID()); }
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) { Read(raw); Read(scene.GetRID()); }
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Authored parameter reads allocated {own}/{all} owner/all-thread B");
            Task.Run(() => Reject<InvalidOperationException>(() => PhysicsServer.BodyGetMass(raw))).GetAwaiter().GetResult();
            Console.WriteLine($"Registered parameter reads: no numerical preparation, preserved automatic mass and 0/0 owner/all-thread B");
        }
        finally
        {
            solver!.IdentityHook = null; solver.BodyHook = null; solver.ShapeHook = null; solver.PolicyHook = null;
            PhysicsServer.FreeRID(raw);
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
