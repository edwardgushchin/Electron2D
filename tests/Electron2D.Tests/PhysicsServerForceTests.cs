using Electron2D;

internal static class PhysicsServerForceTests
{
    internal static void Run()
    {
        VerifyDetachedAndAttachedActions();
        VerifySceneRemoval();
        VerifyModesSleepAndOmission();
        VerifyDetachedGeometry();
        VerifyInvalidAndPhaseAccess();
        VerifyWarmPaths();
        Console.WriteLine("Server force/impulse API, detached geometry, pending lifetime, modes and allocation checks passed.");
    }

    private static void VerifyDetachedAndAttachedActions()
    {
        using var region = new RectangleShape { Size = new(2000, 2000) };
        var root = new Node(); var area = ZeroField(); area.AddChild(new CollisionShape { Shape = region }); root.AddChild(area);
        using var tree = new SceneTree(root);
        var server = PhysicsServer.Service; var body = PhysicsServer.BodyCreate(); var shape = PhysicsServer.CircleShapeCreate();
        try
        {
            PhysicsServer.BodyAddShape(body, shape, new(0, Vector2.One, 0, new(20, 0)));
            PhysicsServer.BodySetTransform(body, new(Mathf.Pi / 2, Vector2.One, 0, new(5, 7)));
            PhysicsServer.BodySetMass(body, 2); PhysicsServer.BodySetInertia(body, 200); PhysicsServer.BodySetCenterOfMass(body, new(5, 0));
            PhysicsServer.BodyApplyCentralImpulse(body, new(2, 0));
            PhysicsServer.BodyApplyImpulse(body, new(0, 2), new(20, 0)); PhysicsServer.BodyApplyTorqueImpulse(body, 20);
            PhysicsServer.BodyAddConstantCentralForce(body, new(3, 0));
            PhysicsServer.BodyAddConstantForce(body, new(0, 60), new(20, 0)); PhysicsServer.BodyAddConstantTorque(body, 5);
            Check(PhysicsServer.BodyGetConstantForce(body) == new Vector2(3, 60) && Near(PhysicsServer.BodyGetConstantTorque(body), 1205),
                "All detached persistent operations share world-axis position and current rotated center.");
            PhysicsServer.BodySetConstantForce(body, Vector2.Zero); PhysicsServer.BodySetConstantTorque(body, 0);
            PhysicsServer.BodyApplyCentralForce(body, new(120, 0)); PhysicsServer.BodyApplyForce(body, new(0, 120), new(20, 0)); PhysicsServer.BodyApplyTorque(body, 120);
            PhysicsServer.BodySetSpace(body, area.GetWorld()!.Space);
            var state = PhysicsServer.BodyGetDirectState(body)!;
            Check(state.LinearVelocity.IsEqualApprox(new(1, 1)) && Near(state.AngularVelocity, 0.3f),
                "Detached impulses change retained state before attachment, using configured mass/inertia.");
            tree.PhysicsFrame(1d / 60);
            Check(Near(state.LinearVelocity.X, 2) && Near(state.LinearVelocity.Y, 2) && Near(state.AngularVelocity, 0.51f),
                $"Queued forces integrate once: {state.LinearVelocity}, {state.AngularVelocity}.");
            tree.PhysicsFrame(1d / 60);
            Check(Near(state.LinearVelocity.X, 2) && Near(state.AngularVelocity, 0.51f), "Transient inputs do not repeat as persistent forces.");
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyApplyCentralImpulse(body, new(100, 0)); PhysicsServer.BodyApplyTorqueImpulse(body, 100);
            Check(state.LinearVelocity == Vector2.Zero && state.AngularVelocity == 0, "Static inverse values ignore impulses.");
            PhysicsServer.BodyApplyCentralForce(body, new(60, 0)); tree.PhysicsFrame(1d / 60);
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid); tree.PhysicsFrame(1d / 60);
            Check(Near(state.LinearVelocity.X, 0.5f), "A transient force waits through static participation for the next eligible step.");
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.RigidLinear); PhysicsServer.BodyApplyTorqueImpulse(body, 100);
            Check(state.AngularVelocity == 0, "Linear-only mode rejects angular impulse response.");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(shape); }
    }

    private static void VerifySceneRemoval()
    {
        using var circle = new CircleShape(); var root = new Node();
        var body = NewBody(4); body.Sleeping = true; Add(body, circle);
        var server = PhysicsServer.Service; var rid = body.GetRID();
        PhysicsServer.BodyApplyCentralImpulse(rid, new(4, 0));
        Check(body.LinearVelocity.IsEqualApprox(new(1, 0)) && !body.Sleeping, "Detached scene impulse uses retained state and wakes future participation.");
        PhysicsServer.BodySetConstantForce(rid, new(5, 0)); body.ConstantTorque = 3;
        Check(body.ConstantForce == new Vector2(5, 0) && PhysicsServer.BodyGetConstantTorque(rid) == 3, "Scene and RID persistent configuration is bidirectional.");
        PhysicsServer.BodySetConstantForce(rid, Vector2.Zero); PhysicsServer.BodySetConstantTorque(rid, 0);
        root.AddChild(body); using var tree = new SceneTree(root);
        PhysicsServer.BodyApplyCentralImpulse(rid, new(4, 0)); PhysicsServer.BodyApplyCentralForce(rid, new(120, 0));
        body.ProcessMode = ProcessMode.Disabled;
        Check(body.LinearVelocity.IsEqualApprox(new(2, 0)), "Removal captures immediate native impulse state before destroying the attachment.");
        PhysicsServer.BodyApplyCentralImpulse(rid, new(4, 0)); body.ProcessMode = ProcessMode.Always;
        tree.PhysicsFrame(1d / 60);
        Check(Near(body.LinearVelocity.X, 3.5f), "Impulses and pending force survive disable/remove/reentry without replay or loss.");
        tree.PhysicsFrame(1d / 60); Check(Near(body.LinearVelocity.X, 3.5f), "Preserved transient force is consumed only once.");
    }

    private static void VerifyModesSleepAndOmission()
    {
        using var circle = new CircleShape(); var root = new Node(); var body = NewBody(); Add(body, circle); root.AddChild(body);
        using var tree = new SceneTree(root); var server = PhysicsServer.Service; var rid = body.GetRID();
        var state = PhysicsServer.BodyGetDirectState(rid)!;
        PhysicsServer.BodyApplyCentralForce(rid, new(60, 0)); body.Sleeping = true; tree.PhysicsFrame(1d / 60);
        Check(state.Sleeping && state.LinearVelocity == Vector2.Zero, "A later explicit Sleeping assignment wins over force-call wakeup.");
        body.Sleeping = false; tree.PhysicsFrame(1d / 60);
        Check(Near(state.LinearVelocity.X, 1), "Dormant pending force is retained until the body is active.");
        body.CustomIntegrator = true; PhysicsServer.BodyApplyCentralForce(rid, new(60, 0)); PhysicsServer.BodyApplyTorque(rid, 30);
        PhysicsServer.BodyApplyCentralImpulse(rid, new(1, 0)); tree.PhysicsFrame(1d / 60);
        Check(Near(state.LinearVelocity.X, 2) && state.AngularVelocity == 0, "Omission discards eligible force/torque but preserves impulses.");
        body.CustomIntegrator = false; tree.PhysicsFrame(1d / 60);
        Check(Near(state.LinearVelocity.X, 2), "Omitted transient inputs cannot return in a later default-integrated frame.");
        PhysicsServer.BodySetConstantTorque(rid, 1); body.Sleeping = true; PhysicsServer.BodySetConstantForce(rid, Vector2.Zero);
        Check(state.Sleeping, "Clearing force does not reassign or wake an unchanged nonzero torque channel.");
        PhysicsServer.BodySetConstantForce(rid, new(1, 0)); body.Sleeping = true; PhysicsServer.BodySetConstantTorque(rid, 0);
        Check(state.Sleeping, "Clearing torque does not wake an unchanged force channel.");
        PhysicsServer.BodyAddConstantCentralForce(rid, Vector2.Zero); Check(!state.Sleeping, "Add operations retain unconditional wakeup semantics even for zero input.");
        body.LockRotation = true; PhysicsServer.BodyApplyTorqueImpulse(rid, 100); Check(state.AngularVelocity == 0, "Rotation lock preserves angular impulse invariants.");
    }

    private static void VerifyDetachedGeometry()
    {
        Shape[] shapes = [new CircleShape(), new RectangleShape { Size = new(30, 20) }, new CapsuleShape { Radius = 8, Height = 24 },
            new SegmentShape { A = new(-30, 0), B = new(30, 0) },
            new ConvexPolygonShape { Points = [new(0, 0), new(40, 0), new(10, 30)] },
            new ConcavePolygonShape { Segments = [new(-20, 0), new(20, 0), new(20, 0), new(20, 30)] }, new SeparationRayShape()];
        try
        {
            foreach (var shape in shapes)
            {
                var root = new Node(); var body = NewBody(2); var owner = body.CreateShapeOwner(null); body.ShapeOwnerAddShape(owner, shape);
                body.ShapeOwnerSetTransform(owner, new(0.3f, Vector2.One, 0, new(5, 9))); root.AddChild(body);
                var server = PhysicsServer.Service; PhysicsServer.BodyApplyCentralImpulse(body.GetRID(), new(2, 0));
                var detached = server.BodyRuntime(body.GetRID()).MassProperties;
                using var tree = new SceneTree(root);
                var attached = server.BodyRuntime(body.GetRID()).MassProperties;
                Check(Near(detached.Center.X, attached.Center.X) && Near(detached.Center.Y, attached.Center.Y) &&
                    Near(detached.Inertia, attached.Inertia), "Detached resource proxies match native fixture mass for " + shape.GetType().Name);
            }
        }
        finally { foreach (var shape in shapes) shape.Dispose(); }
    }

    private static void VerifyInvalidAndPhaseAccess()
    {
        using var circle = new CircleShape(); var root = new Node(); var body = new ForceDuringPose
        {
            GravityScale = 0,
            CanSleep = false,
            LinearVelocity = new(10, 0),
            NotifyLocalTransformChanges = true
        }; Add(body, circle); root.AddChild(body);
        using var tree = new SceneTree(root); var server = PhysicsServer.Service; var rid = body.GetRID();
        PhysicsServer.BodySetConstantForce(rid, new(3, 4)); PhysicsServer.BodySetConstantTorque(rid, 5);
        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyAddConstantForce(rid, new(0, float.MaxValue), new(float.MaxValue, 0)));
        Check(PhysicsServer.BodyGetConstantForce(rid) == new Vector2(3, 4) && PhysicsServer.BodyGetConstantTorque(rid) == 5, "Invalid moment cannot partially change persistent totals.");
        PhysicsServer.BodyApplyCentralForce(rid, new(1, 2)); var runtime = server.BodyRuntime(rid); var pending = runtime.PendingForce;
        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyApplyForce(rid, new(0, float.MaxValue), new(float.MaxValue, 0)));
        Check(runtime.PendingForce == pending && runtime.PendingTorque == 0, "Invalid positioned force cannot partially replace transient channels.");
        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodyApplyCentralImpulse(rid, new(float.NaN, 0)));
        Check(Task.Run(() => Capture(() => PhysicsServer.BodyApplyTorque(rid, 1))).Result is InvalidOperationException, "Attached force writes enforce owner-thread access.");
        tree.PhysicsFrame(1d / 60); Check(body.Rejected, "Solver-owned pose callbacks reject force writes before state mutation.");
        Reject<ArgumentException>(() => PhysicsServer.BodyApplyCentralForce(default, Vector2.Zero));
        var area = PhysicsServer.AreaCreate(); try { Reject<ArgumentException>(() => PhysicsServer.BodyApplyCentralImpulse(area, Vector2.Zero)); } finally { PhysicsServer.FreeRID(area); }
    }

    private static void VerifyWarmPaths()
    {
        using var circle = new CircleShape(); var root = new Node(); var body = NewBody(); body.CanSleep = false; Add(body, circle); root.AddChild(body);
        using var detached = NewBody(); Add(detached, circle); using var tree = new SceneTree(root);
        var server = PhysicsServer.Service; var rid = body.GetRID(); var detachedRID = detached.GetRID();
        for (var pass = 0; pass < 64; pass++) { Act(); tree.PhysicsFrame(1d / 60); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 64; pass++) { Act(); tree.PhysicsFrame(1d / 60); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed attached/detached force, impulse, getter and solver paths allocate zero managed bytes.");
        void Act()
        {
            PhysicsServer.BodyApplyCentralImpulse(rid, Vector2.Zero); PhysicsServer.BodyApplyForce(rid, new(0, 1), new(2, 0));
            PhysicsServer.BodyApplyTorque(rid, 1); PhysicsServer.BodyAddConstantCentralForce(rid, Vector2.Zero); PhysicsServer.BodyAddConstantTorque(rid, 1);
            PhysicsServer.BodySetConstantTorque(rid, 0); PhysicsServer.BodySetConstantForce(rid, Vector2.Zero); _ = PhysicsServer.BodyGetConstantForce(rid);
            PhysicsServer.BodyApplyCentralImpulse(detachedRID, Vector2.Zero); PhysicsServer.BodyAddConstantForce(detachedRID, Vector2.Zero);
        }
    }

    private sealed class ForceDuringPose : RigidBody
    {
        internal bool Rejected;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationLocalTransformChanged && IsInsideTree)
                Rejected = Capture(() => PhysicsServer.BodyApplyCentralForce(GetRID(), new(1, 0))) is InvalidOperationException;
        }
    }

    private static Area ZeroField() => new()
    {
        GravitySpaceOverride = Area.SpaceOverride.Replace,
        Gravity = 0,
        LinearDampSpaceOverride = Area.SpaceOverride.Replace,
        LinearDamp = 0,
        AngularDampSpaceOverride = Area.SpaceOverride.Replace,
        AngularDamp = 0
    };
    private static RigidBody NewBody(float mass = 1) => new()
    {
        Mass = mass,
        GravityScale = 0,
        LinearDampMode = RigidBody.DampMode.Replace,
        LinearDamp = 0,
        AngularDampMode = RigidBody.DampMode.Replace,
        AngularDamp = 0
    };
    private static void Add(CollisionObject body, Shape shape) => body.ShapeOwnerAddShape(body.CreateShapeOwner(null), shape);
    private static bool Near(float a, float b) => MathF.Abs(a - b) <= 0.005f * MathF.Max(1, MathF.Abs(b));
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
