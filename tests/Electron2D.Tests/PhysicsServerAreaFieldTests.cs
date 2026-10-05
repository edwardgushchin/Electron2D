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
        var server = PhysicsServer.Service; var area = PhysicsServer.AreaCreate(); var body = PhysicsServer.BodyCreate();
        using var scene = new Area(); var rid = scene.GetRID();
        try
        {
            Check(PhysicsServer.AreaGetGravity(area) == 9.80665f && PhysicsServer.AreaGetGravityVector(area) == new Vector2(0, -1) &&
                PhysicsServer.AreaGetGravity(rid) == 980 && PhysicsServer.AreaGetGravityVector(rid) == new Vector2(0, 1), "Server and scene initial gravity profiles differ.");
            Check(PhysicsServer.AreaGetGravitySpaceOverride(area) == Area.SpaceOverride.Disabled && !PhysicsServer.AreaGetGravityPoint(area) &&
                PhysicsServer.AreaGetGravityPointUnitDistance(area) == 0 && PhysicsServer.AreaGetLinearDampSpaceOverride(area) == Area.SpaceOverride.Disabled &&
                PhysicsServer.AreaGetAngularDampSpaceOverride(area) == Area.SpaceOverride.Disabled && PhysicsServer.AreaGetLinearDamp(area) == 0.1f &&
                PhysicsServer.AreaGetAngularDamp(area) == 1 && PhysicsServer.AreaGetPriority(area) == 0, "All ten server field defaults.");
            PhysicsServer.AreaSetGravitySpaceOverride(rid, Area.SpaceOverride.ReplaceCombine); PhysicsServer.AreaSetGravity(rid, -20);
            PhysicsServer.AreaSetGravityVector(rid, new(2, -3)); PhysicsServer.AreaSetGravityPoint(rid, true); PhysicsServer.AreaSetGravityPointUnitDistance(rid, -4);
            PhysicsServer.AreaSetLinearDampSpaceOverride(rid, Area.SpaceOverride.CombineReplace); PhysicsServer.AreaSetLinearDamp(rid, -2);
            PhysicsServer.AreaSetAngularDampSpaceOverride(rid, Area.SpaceOverride.Replace); PhysicsServer.AreaSetAngularDamp(rid, -3); PhysicsServer.AreaSetPriority(rid, int.MinValue);
            Check(scene.GravitySpaceOverride == Area.SpaceOverride.ReplaceCombine && scene.Gravity == -20 && scene.GravityPoint &&
                scene.GravityDirection == new Vector2(2, -3) && scene.GravityPointCenter == scene.GravityDirection && scene.GravityPointUnitDistance == -4 &&
                scene.LinearDampSpaceOverride == Area.SpaceOverride.CombineReplace && scene.LinearDamp == -2 &&
                scene.AngularDampSpaceOverride == Area.SpaceOverride.Replace && scene.AngularDamp == -3 && scene.Priority == int.MinValue,
                "Concrete typed pairs share every scene descriptor, including direction/point storage.");
            scene.GravityPointCenter = new(7, 8); scene.GravityPoint = false; scene.Priority = int.MaxValue;
            Check(PhysicsServer.AreaGetGravityVector(rid) == new Vector2(7, 8) && !PhysicsServer.AreaGetGravityPoint(rid) && PhysicsServer.AreaGetPriority(rid) == int.MaxValue,
                "Scene edits project back to raw getters.");
            PhysicsServer.AreaSetGravity(area, -9); PhysicsServer.AreaSetGravityVector(area, new(4, 0)); PhysicsServer.AreaSetPriority(area, 12);
            Check(PhysicsServer.AreaGetGravity(area) == -9 && PhysicsServer.AreaGetGravityVector(area) == new Vector2(4, 0) && PhysicsServer.AreaGetPriority(area) == 12,
                "Detached server configuration retains signed finite values.");
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetGravity(area, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetGravityVector(area, new(float.PositiveInfinity, 0)));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetGravityPointUnitDistance(area, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetLinearDamp(area, float.PositiveInfinity));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetAngularDamp(area, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetGravitySpaceOverride(area, (Area.SpaceOverride)5));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetLinearDampSpaceOverride(area, (Area.SpaceOverride)(-1)));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetAngularDampSpaceOverride(area, (Area.SpaceOverride)99));
            Check(PhysicsServer.AreaGetGravity(area) == -9 && PhysicsServer.AreaGetGravitySpaceOverride(area) == Area.SpaceOverride.Disabled, "Invalid inputs do not mutate state.");
            Reject<ArgumentException>(() => PhysicsServer.AreaSetGravity(body, 1)); Reject<ArgumentException>(() => PhysicsServer.AreaGetGravity(default));
        }
        finally { PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(body); }
        Reject<ArgumentException>(() => PhysicsServer.AreaGetGravity(area));
    }

    private static void VerifyMixedPriorityAndFiltering()
    {
        using var circle = new CircleShape(); using var region = new RectangleShape { Size = new(1000, 1000) };
        var root = new Node(); var low = new Area { Gravity = 200, Priority = 5, GravitySpaceOverride = Area.SpaceOverride.Combine, Monitoring = false, Monitorable = false, CollisionMask = 1u << 31 };
        Add(low, region); root.AddChild(low);
        var rigid = new RigidBody { CanSleep = false, CollisionLayer = 1u << 31, CollisionMask = 0 }; Add(rigid, circle); root.AddChild(rigid);
        var character = new CharacterBody { CollisionLayer = 1u << 31, CollisionMask = 0, Position = new(100, 0) }; Add(character, circle); root.AddChild(character);
        using var tree = new SceneTree(root); var server = PhysicsServer.Service; var area = PhysicsServer.AreaCreate(); var shape = PhysicsServer.RectangleShapeCreate();
        try
        {
            PhysicsServer.ShapeSetData(shape, region);
            PhysicsServer.AreaAddShape(area, shape); PhysicsServer.AreaSetSpace(area, low.GetWorld()!.Space); PhysicsServer.AreaSetCollisionMask(area, 1u << 31);
            PhysicsServer.AreaSetGravity(area, 100); PhysicsServer.AreaSetGravityVector(area, new(1, 0)); PhysicsServer.AreaSetPriority(area, 10);
            var expected = new[] { new Vector2(0, 1180), new Vector2(100, 1180), new Vector2(100, 0), new Vector2(100, 0), new Vector2(100, 1180) };
            for (var mode = 0; mode <= 4; mode++)
            {
                PhysicsServer.AreaSetGravitySpaceOverride(area, (Area.SpaceOverride)mode); tree.PhysicsFrame(1d / 60);
                Near(rigid.GetGravity(), expected[mode], "Mixed fields execute each gravity mode."); Near(character.GetGravity(), expected[mode], "Character reads the same selected gravity.");
            }
            low.GravitySpaceOverride = Area.SpaceOverride.ReplaceCombine; tree.PhysicsFrame(1d / 60);
            Near(rigid.GetGravity(), new(0, 1180), "Lower-priority replacement erases earlier contributions.");
            low.GravitySpaceOverride = Area.SpaceOverride.Combine; PhysicsServer.AreaSetGravitySpaceOverride(area, Area.SpaceOverride.Replace); PhysicsServer.AreaSetPriority(area, -1);
            tree.PhysicsFrame(1d / 60); Near(rigid.GetGravity(), new(100, 0), "Lower-priority stopping replacement runs after the scene field.");
            PhysicsServer.AreaSetCollisionMask(area, 0); tree.PhysicsFrame(1d / 60); Near(rigid.GetGravity(), new(0, 1180), "Bit 32 selection is directional and mask edits affect fields.");
            PhysicsServer.AreaSetCollisionMask(area, 1u << 31); PhysicsServer.AreaSetShapeDisabled(area, 0, true); tree.PhysicsFrame(1d / 60);
            Near(rigid.GetGravity(), new(0, 1180), "Disabled receiver geometry contributes no field.");
            PhysicsServer.AreaSetShapeDisabled(area, 0, false); PhysicsServer.AreaSetSpace(area, default); tree.PhysicsFrame(1d / 60);
            Near(rigid.GetGravity(), new(0, 1180), "Detached fields cease participation.");
            PhysicsServer.AreaSetSpace(area, low.GetWorld()!.Space); tree.PhysicsFrame(1d / 60); Near(rigid.GetGravity(), new(100, 0), "Reentry preserves fields and priority.");
            PhysicsServer.FreeRID(area); area = default; tree.PhysicsFrame(1d / 60); Near(rigid.GetGravity(), new(0, 1180), "Freed field leaves no dangling reducer state.");
        }
        finally { if (area.IsValid()) PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(shape); }
    }

    private static void VerifyServerResponseAndSpaceDefaults()
    {
        var server = PhysicsServer.Service; var space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(space, true); var body = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate(); var shape = PhysicsServer.CircleShapeCreate();
        try
        {
            PhysicsServer.BodyAddShape(body, shape); PhysicsServer.AreaAddShape(area, shape); PhysicsServer.BodySetSpace(body, space); PhysicsServer.AreaSetSpace(area, space);
            Check(PhysicsServer.AreaGetPriority(space) == -1 && PhysicsServer.AreaGetGravity(space) == 980 && PhysicsServer.AreaGetGravityVector(space) == new Vector2(0, 1), "Space default Area starts from sampled project settings.");
            PhysicsServer.AreaSetGravity(area, 40); PhysicsServer.AreaSetGravityVector(area, new(1, 0)); PhysicsServer.AreaSetGravitySpaceOverride(area, Area.SpaceOverride.Replace);
            PhysicsServer.AreaSetLinearDamp(area, -2); PhysicsServer.AreaSetLinearDampSpaceOverride(area, Area.SpaceOverride.Replace);
            PhysicsServer.AreaSetAngularDamp(area, 3); PhysicsServer.AreaSetAngularDampSpaceOverride(area, Area.SpaceOverride.Replace);
            PhysicsServer.BodySetGravityScale(body, 2); PhysicsServer.BodySetLinearDamp(body, 1); PhysicsServer.BodySetAngularDamp(body, 4);
            var state = PhysicsServer.BodyGetDirectState(body)!; state.LinearVelocity = new(100, 0); state.AngularVelocity = 12;
            PhysicsServer.SpaceStep(space, 1d / 60);
            Near(state.TotalGravity, new(80, 0), "Server body sees Area gravity with body scaling.");
            Near(state.LinearVelocity.X, 100 * (1 + 1f / 60) + 80f / 60, "Signed linear damping executes before acceleration.");
            Near(state.AngularVelocity, 12 * (1 - 7f / 60), "Angular Area/body damping drives actual native motion.");
            Check(state.TotalLinearDamp == -1 && state.TotalAngularDamp == 7, "Direct totals share the selected server fields.");
            PhysicsServer.BodySetGravityScale(body, 1); PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(body, 0);
            PhysicsServer.AreaSetCollisionMask(area, 0); PhysicsServer.AreaSetGravity(space, 30); PhysicsServer.AreaSetGravityVector(space, new(0, -2)); PhysicsServer.AreaSetLinearDamp(space, 2);
            PhysicsServer.AreaSetAngularDamp(space, -3); PhysicsServer.AreaSetPriority(space, 999); PhysicsServer.AreaSetGravitySpaceOverride(space, Area.SpaceOverride.Replace);
            PhysicsServer.SpaceStep(space, 1d / 60); Near(state.TotalGravity, new(0, -60), "Space RID updates unbounded defaults with raw vector magnitude.");
            Check(state.TotalLinearDamp == 0 && state.TotalAngularDamp == 1, "Body Replace suppresses default damping independently.");
            state.Sleeping = true; PhysicsServer.AreaSetGravity(space, 60); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(!state.Sleeping && state.TotalGravity == new Vector2(0, -120), "Default field changes wake sleeping server dynamics.");
            PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.SpaceStep(space, 0); Near(state.TotalGravity, new(0, -120), "Zero step does not refresh fields.");
            PhysicsServer.SpaceStep(space, 1d / 60); Near(state.TotalGravity, Vector2.Zero, "Nonzero step refreshes space defaults.");
        }
        finally { PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(shape); PhysicsServer.FreeRID(space); }
        Reject<ArgumentException>(() => PhysicsServer.AreaGetGravity(space));
    }

    private static void VerifyPointGravityAndFailures()
    {
        var server = PhysicsServer.Service; var space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(space, true); var body = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate(); var shape = PhysicsServer.CircleShapeCreate();
        try
        {
            PhysicsServer.AreaAddShape(area, shape); PhysicsServer.BodyAddShape(body, shape); PhysicsServer.AreaSetSpace(area, space); PhysicsServer.BodySetSpace(body, space);
            PhysicsServer.AreaSetGravitySpaceOverride(area, Area.SpaceOverride.Replace); PhysicsServer.AreaSetGravity(area, 100); PhysicsServer.AreaSetGravityVector(area, new(10, 0));
            PhysicsServer.AreaSetTransform(area, new(MathF.PI / 2, Vector2.One, 0, new(10, 0))); PhysicsServer.AreaSetGravityPoint(area, true); PhysicsServer.AreaSetGravityPointUnitDistance(area, 10);
            var state = PhysicsServer.BodyGetDirectState(body)!; PhysicsServer.SpaceStep(space, 1d / 60);
            Near(state.TotalGravity, new Vector2(1, 1).Normalized() * 50, "Point center uses receiver global transform and inverse-square falloff.");
            PhysicsServer.AreaSetGravityPointUnitDistance(area, -1); PhysicsServer.BodySetTransform(body, Transform.Identity); state.LinearVelocity = Vector2.Zero;
            PhysicsServer.SpaceStep(space, 1d / 60); Near(state.TotalGravity, new Vector2(1, 1).Normalized() * 100, "Nonpositive unit distance gives constant-strength point gravity.");
            PhysicsServer.AreaSetTransform(area, Transform.Identity); PhysicsServer.AreaSetGravityVector(area, Vector2.Zero);
            PhysicsServer.BodySetTransform(body, Transform.Identity); state.LinearVelocity = Vector2.Zero;
            PhysicsServer.SpaceStep(space, 1d / 60); Near(state.TotalGravity, Vector2.Zero, "At the attraction center gravity is zero.");
            PhysicsServer.AreaSetTransform(area, new(MathF.PI / 2, Vector2.One, 0, Vector2.Zero));
            PhysicsServer.AreaSetGravityPoint(area, false); PhysicsServer.AreaSetGravityVector(area, new(2, 0)); PhysicsServer.SpaceStep(space, 1d / 60);
            Near(state.TotalGravity, new(200, 0), "Directional gravity ignores receiver rotation and is not normalized.");
            var before = state.LinearVelocity; PhysicsServer.AreaSetGravity(area, float.MaxValue); PhysicsServer.AreaSetGravityVector(area, new(float.MaxValue, 0));
            Check(Capture(() => PhysicsServer.SpaceStep(space, 1d / 60)) is AggregateException error &&
                error.Flatten().InnerExceptions.Any(e => e is InvalidOperationException) && state.LinearVelocity == before,
                "Nonfinite reduction reports a world-step failure before motion mutation.");
            PhysicsServer.AreaSetGravity(area, 0); PhysicsServer.AreaSetGravityVector(area, Vector2.Zero); PhysicsServer.SpaceStep(space, 1d / 60);
            PhysicsServer.AreaSetCollisionMask(area, 0); PhysicsServer.AreaSetGravityPoint(space, true); PhysicsServer.AreaSetGravityVector(space, new(50, 0)); PhysicsServer.AreaSetGravity(space, 20);
            PhysicsServer.BodySetTransform(body, Transform.Identity); state.LinearVelocity = Vector2.Zero; PhysicsServer.SpaceStep(space, 1d / 60);
            Near(state.TotalGravity, new(20, 0), "Default Area point gravity applies without receiver geometry.");
        }
        finally { PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(shape); PhysicsServer.FreeRID(space); }
    }

    private static void VerifyCallbacksGuardsAndWarmFields()
    {
        using var circle = new CircleShape(); var root = new Node(); var scene = new Area { Monitoring = false, Monitorable = false }; Add(scene, circle); root.AddChild(scene);
        var probe = new FieldDuringPose { Area = scene, CanSleep = false, NotifyLocalTransformChanges = true, CollisionMask = 0 }; Add(probe, circle); root.AddChild(probe);
        using var tree = new SceneTree(root); var server = PhysicsServer.Service; var rid = scene.GetRID(); var space = scene.GetWorld()!.Space;
        PhysicsServer.AreaSetGravitySpaceOverride(rid, Area.SpaceOverride.Replace); PhysicsServer.AreaSetGravity(rid, 20); var entries = 0;
        PhysicsServer.AreaSetMonitorCallback(rid, (status, _, _, _, _) => { if (status == PhysicsServer.AreaBodyStatus.Added) { entries++; PhysicsServer.AreaSetGravity(rid, 30); } });
        tree.PhysicsFrame(1d / 60); Check(entries == 1 && probe.Rejected && PhysicsServer.AreaGetGravity(rid) == 30, "Receiver callback can change fields for next step; solver pose edits reject.");
        tree.PhysicsFrame(1d / 60); Check(entries == 1 && probe.GetGravity() == new Vector2(0, 30), "Field updates do not reset raw pair history.");
        root.RemoveChild(probe); probe.Dispose(); PhysicsServer.AreaSetMonitorCallback(rid, null);
        Check(Task.Run(() => Capture(() => PhysicsServer.AreaGetGravity(rid))).Result is InvalidOperationException &&
            Task.Run(() => Capture(() => PhysicsServer.AreaSetLinearDamp(rid, 2))).Result is InvalidOperationException &&
            Task.Run(() => Capture(() => PhysicsServer.AreaSetGravity(space, 2))).Result is InvalidOperationException, "Area and default Area access enforces owner thread.");
        var body = new RigidBody { CanSleep = false, CollisionMask = 0 }; Add(body, circle); root.AddChild(body);
        var rawArea = PhysicsServer.AreaCreate(); var rawShape = PhysicsServer.CircleShapeCreate();
        try
        {
            PhysicsServer.AreaAddShape(rawArea, rawShape); PhysicsServer.AreaSetSpace(rawArea, space);
            PhysicsServer.AreaSetGravitySpaceOverride(rawArea, Area.SpaceOverride.Combine); PhysicsServer.AreaSetGravity(rawArea, 0);
            for (var pass = 0; pass < 64; pass++) Change(pass);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var pass = 0; pass < 64; pass++) Change(pass);
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed active scene/server fields, priority sort, reads and solver frames allocate zero managed bytes.");
            void Change(int pass)
            {
                PhysicsServer.AreaSetGravity(rid, pass % 2); PhysicsServer.AreaSetPriority(rid, pass % 2); PhysicsServer.AreaSetLinearDamp(rid, -1);
                PhysicsServer.AreaSetPriority(rawArea, pass % 2); PhysicsServer.AreaSetGravityVector(rawArea, new(pass % 2, 0));
                PhysicsServer.AreaSetLinearDampSpaceOverride(rid, Area.SpaceOverride.Replace); PhysicsServer.AreaSetGravity(space, 0); _ = PhysicsServer.AreaGetGravityVector(rid);
                tree.PhysicsFrame(1d / 60);
            }
        }
        finally { PhysicsServer.FreeRID(rawArea); PhysicsServer.FreeRID(rawShape); }
    }

    private sealed class FieldDuringPose : RigidBody
    {
        internal required Area Area;
        internal bool Rejected;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationLocalTransformChanged && IsInsideTree)
                Rejected = Capture(() => PhysicsServer.AreaSetGravity(Area.GetRID(), 1)) is InvalidOperationException &&
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
