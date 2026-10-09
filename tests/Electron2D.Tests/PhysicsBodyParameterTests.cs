using Electron2D;

internal static class PhysicsBodyParameterTests
{
    internal static void Run(PhysicsServer.Backend backend = PhysicsServer.Backend.CPU)
    {
        VerifyConfigurationAndMaterialOwnership(backend);
        VerifyServerFieldResponse(backend);
        VerifySignedMaterialResponse(backend);
        VerifySceneAndCharacterProjection(backend);
        VerifyFailuresAndWarmChanges(backend);
        VerifyPersistentIntegration(backend);
        Console.WriteLine($"Typed body material/field parameters, physical response, ownership and allocation checks passed on {backend}.");
    }

    private static void VerifyConfigurationAndMaterialOwnership(PhysicsServer.Backend backend)
    {
        var server = PhysicsServer.Service; var rid = PhysicsServer.BodyCreate();
        try
        {
            Check(PhysicsServer.BodyGetFriction(rid) == 1 && PhysicsServer.BodyGetBounce(rid) == 0 && PhysicsServer.BodyGetGravityScale(rid) == 1 &&
                PhysicsServer.BodyGetLinearDamp(rid) == 0 && PhysicsServer.BodyGetAngularDamp(rid) == 0 &&
                PhysicsServer.BodyGetLinearDampMode(rid) == RigidBody.DampMode.Combine && PhysicsServer.BodyGetAngularDampMode(rid) == RigidBody.DampMode.Combine,
                "Detached parameter defaults retain the full body contract.");
            PhysicsServer.BodySetFriction(rid, -2); PhysicsServer.BodySetBounce(rid, -0.5f); PhysicsServer.BodySetGravityScale(rid, -3);
            PhysicsServer.BodySetLinearDamp(rid, -2); PhysicsServer.BodySetAngularDamp(rid, -3);
            PhysicsServer.BodySetLinearDampMode(rid, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(rid, RigidBody.DampMode.Replace);
            Check(PhysicsServer.BodyGetFriction(rid) == -2 && PhysicsServer.BodyGetBounce(rid) == -0.5f && PhysicsServer.BodyGetGravityScale(rid) == -3 &&
                PhysicsServer.BodyGetLinearDamp(rid) == -2 && PhysicsServer.BodyGetAngularDamp(rid) == -3, "Signed finite detached values are retained.");
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetFriction(rid, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetLinearDampMode(rid, (RigidBody.DampMode)42));
            Check(PhysicsServer.BodyGetFriction(rid) == -2 && PhysicsServer.BodyGetLinearDampMode(rid) == RigidBody.DampMode.Replace,
                "Invalid parameters reject without replacing stored state.");
        }
        finally { PhysicsServer.FreeRID(rid); }
        Reject<ArgumentException>(() => PhysicsServer.BodyGetBounce(rid));
        using var circle = new CircleShape(); using var material = new PhysicsMaterial { Friction = 0.2f, Bounce = 0.3f, Rough = true, Absorbent = true };
        using var world = new World(backend); var root = new SubViewport { World = world }; var first = new RigidBody { Name = "first", PhysicsMaterialOverride = material }; var second = new StaticBody { Name = "second", Position = new(100, 0), PhysicsMaterialOverride = material };
        Add(first, circle); Add(second, circle); root.AddChild(first); root.AddChild(second); using var tree = new SceneTree(root);
        Check(PhysicsServer.BodyGetFriction(first.GetRID()) == -0.2f && PhysicsServer.BodyGetBounce(first.GetRID()) == -0.3f, "Material modifiers project signed raw parameters.");
        PhysicsServer.BodySetFriction(first.GetRID(), 0.8f); PhysicsServer.BodySetBounce(first.GetRID(), 0.6f);
        Check(material.Friction == 0.2f && PhysicsServer.BodyGetFriction(second.GetRID()) == -0.2f && PhysicsServer.BodyGetFriction(first.GetRID()) == 0.8f,
            "Body-local server writes never mutate a shared borrowed resource.");
        material.Friction = 0.4f;
        Check(PhysicsServer.BodyGetFriction(first.GetRID()) == -0.4f && PhysicsServer.BodyGetBounce(first.GetRID()) == -0.3f,
            "A material revision reloads both raw body channels.");
        PhysicsServer.BodySetFriction(first.GetRID(), 0.9f); material.Dispose();
        Check(PhysicsServer.BodyGetFriction(first.GetRID()) == 1 && PhysicsServer.BodyGetBounce(first.GetRID()) == 0, "Material disposal reloads default coefficients.");
    }

    private static void VerifyServerFieldResponse(PhysicsServer.Backend backend)
    {
        using var region = new RectangleShape { Size = new(2000, 2000) }; using var world = new World(backend); var root = new SubViewport { World = world };
        var area = new Area
        {
            GravitySpaceOverride = Area.SpaceOverride.Replace,
            Gravity = 120,
            GravityDirection = Vector2.Right,
            LinearDampSpaceOverride = Area.SpaceOverride.Replace,
            LinearDamp = 3,
            AngularDampSpaceOverride = Area.SpaceOverride.Replace,
            AngularDamp = 2
        };
        Add(area, region); root.AddChild(area); using var tree = new SceneTree(root);
        var server = PhysicsServer.Service; var body = PhysicsServer.BodyCreate(); var circle = PhysicsServer.CircleShapeCreate();
        try
        {
            PhysicsServer.BodyAddShape(body, circle); PhysicsServer.BodySetSpace(body, area.GetWorld()!.Space);
            PhysicsServer.BodySetGravityScale(body, 2); PhysicsServer.BodySetLinearDamp(body, 1); PhysicsServer.BodySetAngularDamp(body, 4);
            var state = PhysicsServer.BodyGetDirectState(body)!; state.LinearVelocity = new(100, 0); state.AngularVelocity = 12;
            tree.PhysicsFrame(1d / 60);
            Check(state.TotalGravity == new Vector2(240, 0) && state.TotalLinearDamp == 4 && state.TotalAngularDamp == 6 &&
                Near(state.LinearVelocity.X, 100 * (1 - 4f / 60) + 240f / 60) && Near(state.AngularVelocity, 12 * (1 - 6f / 60)),
                "Server Combine damping and gravity scaling drive real solver state with damping-before-gravity order.");
            PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace);
            PhysicsServer.BodySetLinearDamp(body, 0); PhysicsServer.BodySetAngularDamp(body, 0); PhysicsServer.BodySetGravityScale(body, -1);
            state.LinearVelocity = Vector2.Zero; state.AngularVelocity = 0; tree.PhysicsFrame(1d / 60);
            Check(state.TotalLinearDamp == 0 && state.TotalAngularDamp == 0 && state.TotalGravity == new Vector2(-120, 0) &&
                Near(state.LinearVelocity.X, -2), "Replace zero and signed gravity retain meaningful physics.");
            PhysicsServer.BodySetLinearDamp(body, -3); state.LinearVelocity = new(100, 0); tree.PhysicsFrame(1d / 60);
            Check(Near(state.LinearVelocity.X, 103), "Negative body damping accelerates before selected gravity.");
            PhysicsServer.BodySetSpace(body, default); PhysicsServer.BodySetSpace(body, area.GetWorld()!.Space);
            Check(PhysicsServer.BodyGetLinearDamp(body) == -3 && PhysicsServer.BodyGetGravityScale(body) == -1, "Detached reentry retains all body parameters.");
            state = PhysicsServer.BodyGetDirectState(body)!; state.Sleeping = true; PhysicsServer.BodySetGravityScale(body, 0); tree.PhysicsFrame(1d / 60);
            state.Sleeping = true; PhysicsServer.BodySetGravityScale(body, 1); Check(!state.Sleeping, "Leaving zero gravity scale wakes a body immediately.");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(circle); }
    }

    private static void VerifySignedMaterialResponse(PhysicsServer.Backend backend)
    {
        using var floorShape = new RectangleShape { Size = new(400, 20) }; using var world = new World(backend); var root = new SubViewport { World = world };
        var floor = new StaticBody { Name = "floor", Position = new(0, 100) }; Add(floor, floorShape); root.AddChild(floor);
        using var tree = new SceneTree(root); var server = PhysicsServer.Service;
        var bouncy = PhysicsServer.BodyCreate(); var absorbent = PhysicsServer.BodyCreate(); var shape = PhysicsServer.CircleShapeCreate();
        try
        {
            foreach (var rid in new[] { bouncy, absorbent }) { PhysicsServer.BodyAddShape(rid, shape); PhysicsServer.BodySetSpace(rid, floor.GetWorld()!.Space); }
            PhysicsServer.BodySetTransform(bouncy, new(0, Vector2.One, 0, new(-50, 0))); PhysicsServer.BodySetTransform(absorbent, new(0, Vector2.One, 0, new(50, 0)));
            PhysicsServer.BodySetBounce(floor.GetRID(), 0.5f); PhysicsServer.BodySetBounce(bouncy, 0.5f); PhysicsServer.BodySetBounce(absorbent, -1);
            var bounceState = PhysicsServer.BodyGetDirectState(bouncy)!; var absorbState = PhysicsServer.BodyGetDirectState(absorbent)!;
            var upward = 0f;
            for (var frame = 0; frame < 100; frame++) { tree.PhysicsFrame(1d / 60); upward = MathF.Min(upward, bounceState.LinearVelocity.Y); }
            Check(upward < -100 && absorbState.Transform.Origin.Y is > 75 and < 83, $"Positive restitution rebounds while signed absorbency suppresses rebound: upward={upward}, absorbent pose={absorbState.Transform.Origin}, velocity={absorbState.LinearVelocity}.");
            PhysicsServer.BodySetBounce(floor.GetRID(), 0); PhysicsServer.BodySetBounce(bouncy, 0);
            PhysicsServer.BodySetTransform(bouncy, new(0, Vector2.One, 0, new(-50, 80))); bounceState.LinearVelocity = new(100, 0);
            PhysicsServer.BodySetFriction(floor.GetRID(), 0); PhysicsServer.BodySetFriction(bouncy, 1);
            for (var frame = 0; frame < 30; frame++) tree.PhysicsFrame(1d / 60);
            var slippery = MathF.Abs(bounceState.LinearVelocity.X);
            PhysicsServer.BodySetFriction(floor.GetRID(), -2); bounceState.LinearVelocity = new(100, 0);
            for (var frame = 0; frame < 30; frame++) tree.PhysicsFrame(1d / 60);
            Check(slippery > 80 && MathF.Abs(bounceState.LinearVelocity.X) < slippery * 0.8f,
                "Signed rough precedence changes actual contact friction instead of storing inert metadata.");
        }
        finally { PhysicsServer.FreeRID(bouncy); PhysicsServer.FreeRID(absorbent); PhysicsServer.FreeRID(shape); }
    }

    private static void VerifySceneAndCharacterProjection(PhysicsServer.Backend backend)
    {
        using var circle = new CircleShape(); using var world = new World(backend); var root = new SubViewport { World = world }; var body = new RigidBody { Name = "body" };
        var character = new CharacterBody { Name = "character", Position = new(100, 0) }; Add(body, circle); Add(character, circle); root.AddChild(body); root.AddChild(character);
        using var tree = new SceneTree(root); var server = PhysicsServer.Service;
        PhysicsServer.BodySetGravityScale(body.GetRID(), 2); PhysicsServer.BodySetLinearDamp(body.GetRID(), 3); PhysicsServer.BodySetAngularDamp(body.GetRID(), 4);
        PhysicsServer.BodySetLinearDampMode(body.GetRID(), RigidBody.DampMode.Replace);
        Check(body.GravityScale == 2 && body.LinearDamp == 3 && body.AngularDamp == 4 && body.LinearDampMode == RigidBody.DampMode.Replace,
            "Scene RigidBody fields share typed server parameter state.");
        body.AngularDampMode = RigidBody.DampMode.Replace; Check(PhysicsServer.BodyGetAngularDampMode(body.GetRID()) == RigidBody.DampMode.Replace, "Scene-to-server policy reads are bidirectional.");
        PhysicsServer.BodySetGravityScale(character.GetRID(), 0.5f); tree.PhysicsFrame(1d / 60);
        Check(character.GetGravity().IsEqualApprox(new(0, 490)) && PhysicsServer.BodyGetDirectState(character.GetRID())!.TotalGravity == new Vector2(0, 490),
            "Non-rigid scene profiles expose the same scaled resolved gravity in both API lanes.");
    }

    private static void VerifyFailuresAndWarmChanges(PhysicsServer.Backend backend)
    {
        using var circle = new CircleShape(); using var world = new World(backend); var root = new SubViewport { World = world }; var body = new RigidBody { GravityScale = 0, CanSleep = false }; Add(body, circle); root.AddChild(body);
        using var tree = new SceneTree(root); var server = PhysicsServer.Service; var rid = body.GetRID();
        using var changedMaterial = new PhysicsMaterial();
        Action<Resource> fail = _ => throw new ApplicationException("material revision");
        changedMaterial.Changed += fail; body.PhysicsMaterialOverride = changedMaterial; PhysicsServer.BodySetFriction(rid, 0.5f);
        Check(Capture(() => changedMaterial.Friction = 0.3f) is ApplicationException && PhysicsServer.BodyGetFriction(rid) == 0.3f,
            "Material revision polling repairs reload even when an earlier subscriber prevented the body callback.");
        changedMaterial.Changed -= fail;
        var probe = new ParameterDuringPose
        {
            Name = "probe",
            GravityScale = 0,
            CanSleep = false,
            LinearVelocity = new(10, 0),
            NotifyLocalTransformChanges = true
        }; Add(probe, circle); root.AddChild(probe);
        tree.PhysicsFrame(1d / 60); Check(probe.Rejected, "Solver pose callbacks reject material and field changes before mutation.");
        root.RemoveChild(probe); probe.Dispose();
        Check(Task.Run(() => Capture(() => PhysicsServer.BodySetBounce(rid, 0.5f))).Result is InvalidOperationException, "Attached parameters enforce owner-thread writes.");
        var area = PhysicsServer.AreaCreate(); try { Reject<ArgumentException>(() => PhysicsServer.BodySetGravityScale(area, 1)); } finally { PhysicsServer.FreeRID(area); }
        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetAngularDamp(rid, float.PositiveInfinity));
        for (var pass = 0; pass < 64; pass++) { Configure(); tree.PhysicsFrame(1d / 60); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 64; pass++) { Configure(); tree.PhysicsFrame(1d / 60); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed field parameter/read and active solver paths allocate zero managed bytes.");
        void Configure() { PhysicsServer.BodySetLinearDamp(rid, 2); PhysicsServer.BodySetAngularDamp(rid, 3); PhysicsServer.BodySetGravityScale(rid, 0); _ = PhysicsServer.BodyGetFriction(rid); _ = PhysicsServer.BodyGetLinearDamp(rid); }
    }

    private static void VerifyPersistentIntegration(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world };
        using var body = new RigidBody { Mass = 2, Inertia = 4, GravityScale = 0, CanSleep = false };
        root.AddChild(body); using var tree = new SceneTree(root);
        PhysicsServer.AreaSetGravity(world.Space, 0); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
        var raw = PhysicsServer.BodyCreate(); var ids = new[] { body.GetRID(), raw };
        try
        {
            PhysicsServer.BodySetMass(raw, 2); PhysicsServer.BodySetInertia(raw, 4); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodySetSpace(raw, world.Space);
            SetConstants(new(120, 0), 240); Step(1, 1);
            SetConstants(new(240, 0), -240); Reset(); Step(2, -1);
            body.CustomIntegrator = true; PhysicsServer.BodySetOmitForceIntegration(raw, true); Reset();
            foreach (var id in ids) { PhysicsServer.BodyApplyCentralImpulse(id, new(2, 0)); PhysicsServer.BodyApplyTorqueImpulse(id, 4); }
            Step(1, 1);
            body.CustomIntegrator = false; PhysicsServer.BodySetOmitForceIntegration(raw, false);
            body.LockRotation = true; PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.RigidLinear); Reset(); Step(2, 0);
            body.LockRotation = false; PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Rigid); Reset(); Step(2, -1);
            root.RemoveChild(body); PhysicsServer.BodySetSpace(raw, default);
            SetConstants(new(60, 0), 120);
            root.AddChild(body); PhysicsServer.BodySetSpace(raw, world.Space); Reset(); Step(.5f, .5f);
        }
        finally { PhysicsServer.FreeRID(raw); }
        void SetConstants(Vector2 force, float torque)
        {
            body.ConstantForce = force; body.ConstantTorque = torque;
            PhysicsServer.BodySetConstantForce(raw, force); PhysicsServer.BodySetConstantTorque(raw, torque);
        }
        void Reset()
        { foreach (var id in ids) { PhysicsServer.BodySetLinearVelocity(id, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(id, 0); } }
        void Step(float linear, float angular)
        {
            tree.PhysicsFrame(1d / 60);
            foreach (var id in ids)
                Check(Near(PhysicsServer.BodyGetLinearVelocity(id).X, linear) && Near(PhysicsServer.BodyGetAngularVelocity(id), angular),
                    "Scene/server persistent forces, omission, rotation locks and reentry follow mass/inertia over 1/60 s within one percent or .01 units/s");
        }
    }

    private sealed class ParameterDuringPose : RigidBody
    {
        internal bool Rejected;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationLocalTransformChanged && IsInsideTree)
            {
                var server = PhysicsServer.Service; var rid = GetRID();
                Rejected = Capture(() => PhysicsServer.BodySetFriction(rid, 0.8f)) is InvalidOperationException &&
                    Capture(() => PhysicsServer.BodySetGravityScale(rid, 2)) is InvalidOperationException &&
                    Capture(() => PhysicsServer.BodySetLinearDampMode(rid, RigidBody.DampMode.Replace)) is InvalidOperationException;
            }
        }
    }

    private static void Add(CollisionObject body, Shape shape) => body.ShapeOwnerAddShape(body.CreateShapeOwner(null), shape);
    private static bool Near(float a, float b) => MathF.Abs(a - b) < 0.01f * MathF.Max(1, MathF.Abs(b));
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
