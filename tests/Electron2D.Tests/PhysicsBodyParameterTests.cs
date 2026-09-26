using Electron2D;

internal static class PhysicsBodyParameterTests
{
    internal static void Run()
    {
        VerifyConfigurationAndMaterialOwnership();
        VerifyServerFieldResponse();
        VerifySignedMaterialResponse();
        VerifySceneAndCharacterProjection();
        VerifyFailuresAndWarmChanges();
        Console.WriteLine("Typed body material/field parameters, physical response, ownership and allocation checks passed.");
    }

    private static void VerifyConfigurationAndMaterialOwnership()
    {
        var server = PhysicsServer.Instance; var rid = server.BodyCreate();
        try
        {
            Check(server.BodyGetFriction(rid) == 1 && server.BodyGetBounce(rid) == 0 && server.BodyGetGravityScale(rid) == 1 &&
                server.BodyGetLinearDamp(rid) == 0 && server.BodyGetAngularDamp(rid) == 0 &&
                server.BodyGetLinearDampMode(rid) == RigidBody.DampMode.Combine && server.BodyGetAngularDampMode(rid) == RigidBody.DampMode.Combine,
                "Detached parameter defaults retain the full body contract.");
            server.BodySetFriction(rid, -2); server.BodySetBounce(rid, -0.5f); server.BodySetGravityScale(rid, -3);
            server.BodySetLinearDamp(rid, -2); server.BodySetAngularDamp(rid, -3);
            server.BodySetLinearDampMode(rid, RigidBody.DampMode.Replace); server.BodySetAngularDampMode(rid, RigidBody.DampMode.Replace);
            Check(server.BodyGetFriction(rid) == -2 && server.BodyGetBounce(rid) == -0.5f && server.BodyGetGravityScale(rid) == -3 &&
                server.BodyGetLinearDamp(rid) == -2 && server.BodyGetAngularDamp(rid) == -3, "Signed finite detached values are retained.");
            Reject<ArgumentOutOfRangeException>(() => server.BodySetFriction(rid, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => server.BodySetLinearDampMode(rid, (RigidBody.DampMode)42));
            Check(server.BodyGetFriction(rid) == -2 && server.BodyGetLinearDampMode(rid) == RigidBody.DampMode.Replace,
                "Invalid parameters reject without replacing stored state.");
        }
        finally { server.FreeRID(rid); }
        Reject<ArgumentException>(() => server.BodyGetBounce(rid));
        using var circle = new CircleShape(); using var material = new PhysicsMaterial { Friction = 0.2f, Bounce = 0.3f, Rough = true, Absorbent = true };
        var root = new Node(); var first = new RigidBody { Name = "first", PhysicsMaterialOverride = material }; var second = new StaticBody { Name = "second", Position = new(100, 0), PhysicsMaterialOverride = material };
        Add(first, circle); Add(second, circle); root.AddChild(first); root.AddChild(second); using var tree = new SceneTree(root);
        Check(server.BodyGetFriction(first.GetRID()) == -0.2f && server.BodyGetBounce(first.GetRID()) == -0.3f, "Material modifiers project signed raw parameters.");
        server.BodySetFriction(first.GetRID(), 0.8f); server.BodySetBounce(first.GetRID(), 0.6f);
        Check(material.Friction == 0.2f && server.BodyGetFriction(second.GetRID()) == -0.2f && server.BodyGetFriction(first.GetRID()) == 0.8f,
            "Body-local server writes never mutate a shared borrowed resource.");
        material.Friction = 0.4f;
        Check(server.BodyGetFriction(first.GetRID()) == -0.4f && server.BodyGetBounce(first.GetRID()) == -0.3f,
            "A material revision reloads both raw body channels.");
        server.BodySetFriction(first.GetRID(), 0.9f); material.Dispose();
        Check(server.BodyGetFriction(first.GetRID()) == 1 && server.BodyGetBounce(first.GetRID()) == 0, "Material disposal reloads default coefficients.");
    }

    private static void VerifyServerFieldResponse()
    {
        using var region = new RectangleShape { Size = new(2000, 2000) }; var root = new Node();
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
        var server = PhysicsServer.Instance; var body = server.BodyCreate(); var circle = server.CircleShapeCreate();
        try
        {
            server.BodyAddShape(body, circle); server.BodySetSpace(body, area.GetWorld2D()!.Space);
            server.BodySetGravityScale(body, 2); server.BodySetLinearDamp(body, 1); server.BodySetAngularDamp(body, 4);
            var state = server.BodyGetDirectState(body)!; state.LinearVelocity = new(100, 0); state.AngularVelocity = 12;
            tree.PhysicsFrame(1d / 60);
            Check(state.TotalGravity == new Vector2(240, 0) && state.TotalLinearDamp == 4 && state.TotalAngularDamp == 6 &&
                Near(state.LinearVelocity.X, 100 * (1 - 4f / 60) + 240f / 60) && Near(state.AngularVelocity, 12 * (1 - 6f / 60)),
                "Server Combine damping and gravity scaling drive real solver state with damping-before-gravity order.");
            server.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); server.BodySetAngularDampMode(body, RigidBody.DampMode.Replace);
            server.BodySetLinearDamp(body, 0); server.BodySetAngularDamp(body, 0); server.BodySetGravityScale(body, -1);
            state.LinearVelocity = Vector2.Zero; state.AngularVelocity = 0; tree.PhysicsFrame(1d / 60);
            Check(state.TotalLinearDamp == 0 && state.TotalAngularDamp == 0 && state.TotalGravity == new Vector2(-120, 0) &&
                Near(state.LinearVelocity.X, -2), "Replace zero and signed gravity retain meaningful physics.");
            server.BodySetLinearDamp(body, -3); state.LinearVelocity = new(100, 0); tree.PhysicsFrame(1d / 60);
            Check(Near(state.LinearVelocity.X, 103), "Negative body damping accelerates before selected gravity.");
            server.BodySetSpace(body, default); server.BodySetSpace(body, area.GetWorld2D()!.Space);
            Check(server.BodyGetLinearDamp(body) == -3 && server.BodyGetGravityScale(body) == -1, "Detached reentry retains all body parameters.");
            state = server.BodyGetDirectState(body)!; state.Sleeping = true; server.BodySetGravityScale(body, 0); tree.PhysicsFrame(1d / 60);
            state.Sleeping = true; server.BodySetGravityScale(body, 1); Check(!state.Sleeping, "Leaving zero gravity scale wakes a body immediately.");
        }
        finally { server.FreeRID(body); server.FreeRID(circle); }
    }

    private static void VerifySignedMaterialResponse()
    {
        using var floorShape = new RectangleShape { Size = new(400, 20) }; var root = new Node();
        var floor = new StaticBody { Name = "floor", Position = new(0, 100) }; Add(floor, floorShape); root.AddChild(floor);
        using var tree = new SceneTree(root); var server = PhysicsServer.Instance;
        var bouncy = server.BodyCreate(); var absorbent = server.BodyCreate(); var shape = server.CircleShapeCreate();
        try
        {
            foreach (var rid in new[] { bouncy, absorbent }) { server.BodyAddShape(rid, shape); server.BodySetSpace(rid, floor.GetWorld2D()!.Space); }
            server.BodySetTransform(bouncy, new(0, Vector2.One, 0, new(-50, 0))); server.BodySetTransform(absorbent, new(0, Vector2.One, 0, new(50, 0)));
            server.BodySetBounce(floor.GetRID(), 0.5f); server.BodySetBounce(bouncy, 0.5f); server.BodySetBounce(absorbent, -1);
            var bounceState = server.BodyGetDirectState(bouncy)!; var absorbState = server.BodyGetDirectState(absorbent)!;
            var upward = 0f;
            for (var frame = 0; frame < 100; frame++) { tree.PhysicsFrame(1d / 60); upward = MathF.Min(upward, bounceState.LinearVelocity.Y); }
            Check(upward < -100 && absorbState.Transform.Origin.Y is > 75 and < 83, "Positive restitution rebounds while signed absorbency suppresses rebound.");
            server.BodySetBounce(floor.GetRID(), 0); server.BodySetBounce(bouncy, 0);
            server.BodySetTransform(bouncy, new(0, Vector2.One, 0, new(-50, 80))); bounceState.LinearVelocity = new(100, 0);
            server.BodySetFriction(floor.GetRID(), 0); server.BodySetFriction(bouncy, 1);
            for (var frame = 0; frame < 30; frame++) tree.PhysicsFrame(1d / 60);
            var slippery = MathF.Abs(bounceState.LinearVelocity.X);
            server.BodySetFriction(floor.GetRID(), -2); bounceState.LinearVelocity = new(100, 0);
            for (var frame = 0; frame < 30; frame++) tree.PhysicsFrame(1d / 60);
            Check(slippery > 80 && MathF.Abs(bounceState.LinearVelocity.X) < slippery * 0.8f,
                "Signed rough precedence changes actual contact friction instead of storing inert metadata.");
        }
        finally { server.FreeRID(bouncy); server.FreeRID(absorbent); server.FreeRID(shape); }
    }

    private static void VerifySceneAndCharacterProjection()
    {
        using var circle = new CircleShape(); var root = new Node(); var body = new RigidBody { Name = "body" };
        var character = new CharacterBody { Name = "character", Position = new(100, 0) }; Add(body, circle); Add(character, circle); root.AddChild(body); root.AddChild(character);
        using var tree = new SceneTree(root); var server = PhysicsServer.Instance;
        server.BodySetGravityScale(body.GetRID(), 2); server.BodySetLinearDamp(body.GetRID(), 3); server.BodySetAngularDamp(body.GetRID(), 4);
        server.BodySetLinearDampMode(body.GetRID(), RigidBody.DampMode.Replace);
        Check(body.GravityScale == 2 && body.LinearDamp == 3 && body.AngularDamp == 4 && body.LinearDampMode == RigidBody.DampMode.Replace,
            "Scene RigidBody fields share typed server parameter state.");
        body.AngularDampMode = RigidBody.DampMode.Replace; Check(server.BodyGetAngularDampMode(body.GetRID()) == RigidBody.DampMode.Replace, "Scene-to-server policy reads are bidirectional.");
        server.BodySetGravityScale(character.GetRID(), 0.5f); tree.PhysicsFrame(1d / 60);
        Check(character.GetGravity().IsEqualApprox(new(0, 490)) && server.BodyGetDirectState(character.GetRID())!.TotalGravity == new Vector2(0, 490),
            "Non-rigid scene profiles expose the same scaled resolved gravity in both API lanes.");
    }

    private static void VerifyFailuresAndWarmChanges()
    {
        using var circle = new CircleShape(); var root = new Node(); var body = new RigidBody { GravityScale = 0, CanSleep = false }; Add(body, circle); root.AddChild(body);
        using var tree = new SceneTree(root); var server = PhysicsServer.Instance; var rid = body.GetRID();
        using var changedMaterial = new PhysicsMaterial();
        Action<Resource> fail = _ => throw new ApplicationException("material revision");
        changedMaterial.Changed += fail; body.PhysicsMaterialOverride = changedMaterial; server.BodySetFriction(rid, 0.5f);
        Check(Capture(() => changedMaterial.Friction = 0.3f) is ApplicationException && server.BodyGetFriction(rid) == 0.3f,
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
        Check(Task.Run(() => Capture(() => server.BodySetBounce(rid, 0.5f))).Result is InvalidOperationException, "Attached parameters enforce owner-thread writes.");
        var area = server.AreaCreate(); try { Reject<ArgumentException>(() => server.BodySetGravityScale(area, 1)); } finally { server.FreeRID(area); }
        Reject<ArgumentOutOfRangeException>(() => server.BodySetAngularDamp(rid, float.PositiveInfinity));
        for (var pass = 0; pass < 64; pass++) { Configure(); tree.PhysicsFrame(1d / 60); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 64; pass++) { Configure(); tree.PhysicsFrame(1d / 60); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed field parameter/read and active solver paths allocate zero managed bytes.");
        void Configure() { server.BodySetLinearDamp(rid, 2); server.BodySetAngularDamp(rid, 3); server.BodySetGravityScale(rid, 0); _ = server.BodyGetFriction(rid); _ = server.BodyGetLinearDamp(rid); }
    }

    private sealed class ParameterDuringPose : RigidBody
    {
        internal bool Rejected;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationLocalTransformChanged && IsInsideTree)
            {
                var server = PhysicsServer.Instance; var rid = GetRID();
                Rejected = Capture(() => server.BodySetFriction(rid, 0.8f)) is InvalidOperationException &&
                    Capture(() => server.BodySetGravityScale(rid, 2)) is InvalidOperationException &&
                    Capture(() => server.BodySetLinearDampMode(rid, RigidBody.DampMode.Replace)) is InvalidOperationException;
            }
        }
    }

    private static void Add(CollisionObject body, Shape shape) => body.ShapeOwnerAddShape(body.CreateShapeOwner(null), shape);
    private static bool Near(float a, float b) => MathF.Abs(a - b) < 0.01f * MathF.Max(1, MathF.Abs(b));
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
