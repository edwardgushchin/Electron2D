using Electron2D;

internal static class PhysicsServerAreaFieldTests
{
    internal static void Run()
    {
        VerifyConfigurationAndSceneProjection();
        VerifyMixedPriorityAndFiltering();
        VerifyServerResponseAndSpaceDefaults();
        VerifyPointGravityAndFailures();
        VerifyCallbacksGuardsAndWarmFields();
        Console.WriteLine("Typed server Area fields, mixed reduction, space defaults, lifetime and allocation checks passed.");
    }

    private static void VerifyConfigurationAndSceneProjection()
    {
        var server = PhysicsServer.Instance; var area = server.AreaCreate(); var body = server.BodyCreate();
        using var scene = new Area(); var rid = scene.GetRID();
        try
        {
            Check(server.AreaGetGravity(area) == 9.80665f && server.AreaGetGravityVector(area) == new Vector2(0, -1) &&
                server.AreaGetGravity(rid) == 980 && server.AreaGetGravityVector(rid) == new Vector2(0, 1), "Server and scene initial gravity profiles differ.");
            Check(server.AreaGetGravitySpaceOverride(area) == Area.SpaceOverride.Disabled && !server.AreaGetGravityPoint(area) &&
                server.AreaGetGravityPointUnitDistance(area) == 0 && server.AreaGetLinearDampSpaceOverride(area) == Area.SpaceOverride.Disabled &&
                server.AreaGetAngularDampSpaceOverride(area) == Area.SpaceOverride.Disabled && server.AreaGetLinearDamp(area) == 0.1f &&
                server.AreaGetAngularDamp(area) == 1 && server.AreaGetPriority(area) == 0, "All ten server field defaults.");
            server.AreaSetGravitySpaceOverride(rid, Area.SpaceOverride.ReplaceCombine); server.AreaSetGravity(rid, -20);
            server.AreaSetGravityVector(rid, new(2, -3)); server.AreaSetGravityPoint(rid, true); server.AreaSetGravityPointUnitDistance(rid, -4);
            server.AreaSetLinearDampSpaceOverride(rid, Area.SpaceOverride.CombineReplace); server.AreaSetLinearDamp(rid, -2);
            server.AreaSetAngularDampSpaceOverride(rid, Area.SpaceOverride.Replace); server.AreaSetAngularDamp(rid, -3); server.AreaSetPriority(rid, int.MinValue);
            Check(scene.GravitySpaceOverride == Area.SpaceOverride.ReplaceCombine && scene.Gravity == -20 && scene.GravityPoint &&
                scene.GravityDirection == new Vector2(2, -3) && scene.GravityPointCenter == scene.GravityDirection && scene.GravityPointUnitDistance == -4 &&
                scene.LinearDampSpaceOverride == Area.SpaceOverride.CombineReplace && scene.LinearDamp == -2 &&
                scene.AngularDampSpaceOverride == Area.SpaceOverride.Replace && scene.AngularDamp == -3 && scene.Priority == int.MinValue,
                "Concrete typed pairs share every scene descriptor, including direction/point storage.");
            scene.GravityPointCenter = new(7, 8); scene.GravityPoint = false; scene.Priority = int.MaxValue;
            Check(server.AreaGetGravityVector(rid) == new Vector2(7, 8) && !server.AreaGetGravityPoint(rid) && server.AreaGetPriority(rid) == int.MaxValue,
                "Scene edits project back to raw getters.");
            server.AreaSetGravity(area, -9); server.AreaSetGravityVector(area, new(4, 0)); server.AreaSetPriority(area, 12);
            Check(server.AreaGetGravity(area) == -9 && server.AreaGetGravityVector(area) == new Vector2(4, 0) && server.AreaGetPriority(area) == 12,
                "Detached server configuration retains signed finite values.");
            Reject<ArgumentOutOfRangeException>(() => server.AreaSetGravity(area, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => server.AreaSetGravityVector(area, new(float.PositiveInfinity, 0)));
            Reject<ArgumentOutOfRangeException>(() => server.AreaSetGravityPointUnitDistance(area, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => server.AreaSetLinearDamp(area, float.PositiveInfinity));
            Reject<ArgumentOutOfRangeException>(() => server.AreaSetAngularDamp(area, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => server.AreaSetGravitySpaceOverride(area, (Area.SpaceOverride)5));
            Reject<ArgumentOutOfRangeException>(() => server.AreaSetLinearDampSpaceOverride(area, (Area.SpaceOverride)(-1)));
            Reject<ArgumentOutOfRangeException>(() => server.AreaSetAngularDampSpaceOverride(area, (Area.SpaceOverride)99));
            Check(server.AreaGetGravity(area) == -9 && server.AreaGetGravitySpaceOverride(area) == Area.SpaceOverride.Disabled, "Invalid inputs do not mutate state.");
            Reject<ArgumentException>(() => server.AreaSetGravity(body, 1)); Reject<ArgumentException>(() => server.AreaGetGravity(default));
        }
        finally { server.FreeRID(area); server.FreeRID(body); }
        Reject<ArgumentException>(() => server.AreaGetGravity(area));
    }

    private static void VerifyMixedPriorityAndFiltering()
    {
        using var circle = new CircleShape(); using var region = new RectangleShape { Size = new(1000, 1000) };
        var root = new Node(); var low = new Area { Gravity = 200, Priority = 5, GravitySpaceOverride = Area.SpaceOverride.Combine, Monitoring = false, Monitorable = false, CollisionMask = 1u << 31 };
        Add(low, region); root.AddChild(low);
        var rigid = new RigidBody { CanSleep = false, CollisionLayer = 1u << 31, CollisionMask = 0 }; Add(rigid, circle); root.AddChild(rigid);
        var character = new CharacterBody { CollisionLayer = 1u << 31, CollisionMask = 0, Position = new(100, 0) }; Add(character, circle); root.AddChild(character);
        using var tree = new SceneTree(root); var server = PhysicsServer.Instance; var area = server.AreaCreate(); var shape = server.RectangleShapeCreate();
        try
        {
            server.ShapeSetData(shape, region);
            server.AreaAddShape(area, shape); server.AreaSetSpace(area, low.GetWorld2D()!.Space); server.AreaSetCollisionMask(area, 1u << 31);
            server.AreaSetGravity(area, 100); server.AreaSetGravityVector(area, new(1, 0)); server.AreaSetPriority(area, 10);
            var expected = new[] { new Vector2(0, 1180), new Vector2(100, 1180), new Vector2(100, 0), new Vector2(100, 0), new Vector2(100, 1180) };
            for (var mode = 0; mode <= 4; mode++)
            {
                server.AreaSetGravitySpaceOverride(area, (Area.SpaceOverride)mode); tree.PhysicsFrame(1d / 60);
                Near(rigid.GetGravity(), expected[mode], "Mixed fields execute each gravity mode."); Near(character.GetGravity(), expected[mode], "Character reads the same selected gravity.");
            }
            low.GravitySpaceOverride = Area.SpaceOverride.ReplaceCombine; tree.PhysicsFrame(1d / 60);
            Near(rigid.GetGravity(), new(0, 1180), "Lower-priority replacement erases earlier contributions.");
            low.GravitySpaceOverride = Area.SpaceOverride.Combine; server.AreaSetGravitySpaceOverride(area, Area.SpaceOverride.Replace); server.AreaSetPriority(area, -1);
            tree.PhysicsFrame(1d / 60); Near(rigid.GetGravity(), new(100, 0), "Lower-priority stopping replacement runs after the scene field.");
            server.AreaSetCollisionMask(area, 0); tree.PhysicsFrame(1d / 60); Near(rigid.GetGravity(), new(0, 1180), "Bit 32 selection is directional and mask edits affect fields.");
            server.AreaSetCollisionMask(area, 1u << 31); server.AreaSetShapeDisabled(area, 0, true); tree.PhysicsFrame(1d / 60);
            Near(rigid.GetGravity(), new(0, 1180), "Disabled receiver geometry contributes no field.");
            server.AreaSetShapeDisabled(area, 0, false); server.AreaSetSpace(area, default); tree.PhysicsFrame(1d / 60);
            Near(rigid.GetGravity(), new(0, 1180), "Detached fields cease participation.");
            server.AreaSetSpace(area, low.GetWorld2D()!.Space); tree.PhysicsFrame(1d / 60); Near(rigid.GetGravity(), new(100, 0), "Reentry preserves fields and priority.");
            server.FreeRID(area); area = default; tree.PhysicsFrame(1d / 60); Near(rigid.GetGravity(), new(0, 1180), "Freed field leaves no dangling reducer state.");
        }
        finally { if (area.IsValid()) server.FreeRID(area); server.FreeRID(shape); }
    }

    private static void VerifyServerResponseAndSpaceDefaults()
    {
        var server = PhysicsServer.Instance; var space = server.SpaceCreate(); server.SpaceSetActive(space, true); var body = server.BodyCreate(); var area = server.AreaCreate(); var shape = server.CircleShapeCreate();
        try
        {
            server.BodyAddShape(body, shape); server.AreaAddShape(area, shape); server.BodySetSpace(body, space); server.AreaSetSpace(area, space);
            Check(server.AreaGetPriority(space) == -1 && server.AreaGetGravity(space) == 980 && server.AreaGetGravityVector(space) == new Vector2(0, 1), "Space default Area starts from sampled project settings.");
            server.AreaSetGravity(area, 40); server.AreaSetGravityVector(area, new(1, 0)); server.AreaSetGravitySpaceOverride(area, Area.SpaceOverride.Replace);
            server.AreaSetLinearDamp(area, -2); server.AreaSetLinearDampSpaceOverride(area, Area.SpaceOverride.Replace);
            server.AreaSetAngularDamp(area, 3); server.AreaSetAngularDampSpaceOverride(area, Area.SpaceOverride.Replace);
            server.BodySetGravityScale(body, 2); server.BodySetLinearDamp(body, 1); server.BodySetAngularDamp(body, 4);
            var state = server.BodyGetDirectState(body)!; state.LinearVelocity = new(100, 0); state.AngularVelocity = 12;
            server.SpaceStep(space, 1d / 60);
            Near(state.TotalGravity, new(80, 0), "Server body sees Area gravity with body scaling.");
            Near(state.LinearVelocity.X, 100 * (1 + 1f / 60) + 80f / 60, "Signed linear damping executes before acceleration.");
            Near(state.AngularVelocity, 12 * (1 - 7f / 60), "Angular Area/body damping drives actual native motion.");
            Check(state.TotalLinearDamp == -1 && state.TotalAngularDamp == 7, "Direct totals share the selected server fields.");
            server.BodySetGravityScale(body, 1); server.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); server.BodySetLinearDamp(body, 0);
            server.AreaSetCollisionMask(area, 0); server.AreaSetGravity(space, 30); server.AreaSetGravityVector(space, new(0, -2)); server.AreaSetLinearDamp(space, 2);
            server.AreaSetAngularDamp(space, -3); server.AreaSetPriority(space, 999); server.AreaSetGravitySpaceOverride(space, Area.SpaceOverride.Replace);
            server.SpaceStep(space, 1d / 60); Near(state.TotalGravity, new(0, -60), "Space RID updates unbounded defaults with raw vector magnitude.");
            Check(state.TotalLinearDamp == 0 && state.TotalAngularDamp == 1, "Body Replace suppresses default damping independently.");
            state.Sleeping = true; server.AreaSetGravity(space, 60); server.SpaceStep(space, 1d / 60);
            Check(!state.Sleeping && state.TotalGravity == new Vector2(0, -120), "Default field changes wake sleeping server dynamics.");
            server.AreaSetGravity(space, 0); server.SpaceStep(space, 0); Near(state.TotalGravity, new(0, -120), "Zero step does not refresh fields.");
            server.SpaceStep(space, 1d / 60); Near(state.TotalGravity, Vector2.Zero, "Nonzero step refreshes space defaults.");
        }
        finally { server.FreeRID(area); server.FreeRID(body); server.FreeRID(shape); server.FreeRID(space); }
        Reject<ArgumentException>(() => server.AreaGetGravity(space));
    }

    private static void VerifyPointGravityAndFailures()
    {
        var server = PhysicsServer.Instance; var space = server.SpaceCreate(); server.SpaceSetActive(space, true); var body = server.BodyCreate(); var area = server.AreaCreate(); var shape = server.CircleShapeCreate();
        try
        {
            server.AreaAddShape(area, shape); server.BodyAddShape(body, shape); server.AreaSetSpace(area, space); server.BodySetSpace(body, space);
            server.AreaSetGravitySpaceOverride(area, Area.SpaceOverride.Replace); server.AreaSetGravity(area, 100); server.AreaSetGravityVector(area, new(10, 0));
            server.AreaSetTransform(area, new(MathF.PI / 2, Vector2.One, 0, new(10, 0))); server.AreaSetGravityPoint(area, true); server.AreaSetGravityPointUnitDistance(area, 10);
            var state = server.BodyGetDirectState(body)!; server.SpaceStep(space, 1d / 60);
            Near(state.TotalGravity, new Vector2(1, 1).Normalized() * 50, "Point center uses receiver global transform and inverse-square falloff.");
            server.AreaSetGravityPointUnitDistance(area, -1); server.BodySetTransform(body, Transform.Identity); state.LinearVelocity = Vector2.Zero;
            server.SpaceStep(space, 1d / 60); Near(state.TotalGravity, new Vector2(1, 1).Normalized() * 100, "Nonpositive unit distance gives constant-strength point gravity.");
            server.AreaSetTransform(area, Transform.Identity); server.AreaSetGravityVector(area, Vector2.Zero);
            server.BodySetTransform(body, Transform.Identity); state.LinearVelocity = Vector2.Zero;
            server.SpaceStep(space, 1d / 60); Near(state.TotalGravity, Vector2.Zero, "At the attraction center gravity is zero.");
            server.AreaSetTransform(area, new(MathF.PI / 2, Vector2.One, 0, Vector2.Zero));
            server.AreaSetGravityPoint(area, false); server.AreaSetGravityVector(area, new(2, 0)); server.SpaceStep(space, 1d / 60);
            Near(state.TotalGravity, new(200, 0), "Directional gravity ignores receiver rotation and is not normalized.");
            var before = state.LinearVelocity; server.AreaSetGravity(area, float.MaxValue); server.AreaSetGravityVector(area, new(float.MaxValue, 0));
            Check(Capture(() => server.SpaceStep(space, 1d / 60)) is AggregateException error &&
                error.Flatten().InnerExceptions.Any(e => e is InvalidOperationException) && state.LinearVelocity == before,
                "Nonfinite reduction reports a world-step failure before motion mutation.");
            server.AreaSetGravity(area, 0); server.AreaSetGravityVector(area, Vector2.Zero); server.SpaceStep(space, 1d / 60);
            server.AreaSetCollisionMask(area, 0); server.AreaSetGravityPoint(space, true); server.AreaSetGravityVector(space, new(50, 0)); server.AreaSetGravity(space, 20);
            server.BodySetTransform(body, Transform.Identity); state.LinearVelocity = Vector2.Zero; server.SpaceStep(space, 1d / 60);
            Near(state.TotalGravity, new(20, 0), "Default Area point gravity applies without receiver geometry.");
        }
        finally { server.FreeRID(area); server.FreeRID(body); server.FreeRID(shape); server.FreeRID(space); }
    }

    private static void VerifyCallbacksGuardsAndWarmFields()
    {
        using var circle = new CircleShape(); var root = new Node(); var scene = new Area { Monitoring = false, Monitorable = false }; Add(scene, circle); root.AddChild(scene);
        var probe = new FieldDuringPose { Area = scene, CanSleep = false, NotifyLocalTransformChanges = true, CollisionMask = 0 }; Add(probe, circle); root.AddChild(probe);
        using var tree = new SceneTree(root); var server = PhysicsServer.Instance; var rid = scene.GetRID(); var space = scene.GetWorld2D()!.Space;
        server.AreaSetGravitySpaceOverride(rid, Area.SpaceOverride.Replace); server.AreaSetGravity(rid, 20); var entries = 0;
        server.AreaSetMonitorCallback(rid, (status, _, _, _, _) => { if (status == PhysicsServer.AreaBodyStatus.Added) { entries++; server.AreaSetGravity(rid, 30); } });
        tree.PhysicsFrame(1d / 60); Check(entries == 1 && probe.Rejected && server.AreaGetGravity(rid) == 30, "Receiver callback can change fields for next step; solver pose edits reject.");
        tree.PhysicsFrame(1d / 60); Check(entries == 1 && probe.GetGravity() == new Vector2(0, 30), "Field updates do not reset raw pair history.");
        root.RemoveChild(probe); probe.Dispose(); server.AreaSetMonitorCallback(rid, null);
        Check(Task.Run(() => Capture(() => server.AreaGetGravity(rid))).Result is InvalidOperationException &&
            Task.Run(() => Capture(() => server.AreaSetLinearDamp(rid, 2))).Result is InvalidOperationException &&
            Task.Run(() => Capture(() => server.AreaSetGravity(space, 2))).Result is InvalidOperationException, "Area and default Area access enforces owner thread.");
        var body = new RigidBody { CanSleep = false, CollisionMask = 0 }; Add(body, circle); root.AddChild(body);
        var rawArea = server.AreaCreate(); var rawShape = server.CircleShapeCreate();
        try
        {
            server.AreaAddShape(rawArea, rawShape); server.AreaSetSpace(rawArea, space);
            server.AreaSetGravitySpaceOverride(rawArea, Area.SpaceOverride.Combine); server.AreaSetGravity(rawArea, 0);
            for (var pass = 0; pass < 64; pass++) Change(pass);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var pass = 0; pass < 64; pass++) Change(pass);
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed active scene/server fields, priority sort, reads and solver frames allocate zero managed bytes.");
            void Change(int pass)
            {
                server.AreaSetGravity(rid, pass % 2); server.AreaSetPriority(rid, pass % 2); server.AreaSetLinearDamp(rid, -1);
                server.AreaSetPriority(rawArea, pass % 2); server.AreaSetGravityVector(rawArea, new(pass % 2, 0));
                server.AreaSetLinearDampSpaceOverride(rid, Area.SpaceOverride.Replace); server.AreaSetGravity(space, 0); _ = server.AreaGetGravityVector(rid);
                tree.PhysicsFrame(1d / 60);
            }
        }
        finally { server.FreeRID(rawArea); server.FreeRID(rawShape); }
    }

    private sealed class FieldDuringPose : RigidBody
    {
        internal required Area Area;
        internal bool Rejected;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationLocalTransformChanged && IsInsideTree)
                Rejected = Capture(() => PhysicsServer.Instance.AreaSetGravity(Area.GetRID(), 1)) is InvalidOperationException &&
                    Capture(() => Area.LinearDamp = 1) is InvalidOperationException;
        }
    }

    private static void Add(CollisionObject collider, Shape shape) => collider.ShapeOwnerAddShape(collider.CreateShapeOwner(null), shape);
    private static void Near(Vector2 actual, Vector2 expected, string message) => Check((actual - expected).Length() < 0.02f, message + $" actual={actual} expected={expected}");
    private static void Near(float actual, float expected, string message) => Check(MathF.Abs(actual - expected) < 0.02f, message + $" actual={actual} expected={expected}");
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception
    {
        var error = Capture(action);
        Check(error is T, $"Expected {typeof(T).Name}, received {error?.GetType().Name ?? "no error"}.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
