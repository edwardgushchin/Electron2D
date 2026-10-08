using Electron2D;

internal static class PhysicsBodyStateTests
{
    internal static void Run()
    {
        VerifyLiveFieldsAndForces();
        VerifyBodyViewSynchronization();
        VerifyPoseNotificationWrites();
        VerifyCustomIntegration();
        VerifySolvedContacts();
        VerifyServerContactLimit();
        VerifyServerCallbacksAndLifetime();
        VerifyReattachmentViews();
        VerifyServerFieldsAndForces();
        VerifyCallbackFailureAndMutations();
        VerifyWarmAllocation();
        Console.WriteLine("Direct body state, post-solver callbacks, custom integration and allocation checks passed.");
    }

    private static void VerifyBodyViewSynchronization()
    {
        using var root = new Node();
        var target = new RigidBody { Name = "Target", GravityScale = 0 };
        var other = new RigidBody { Name = "Other", GravityScale = 0 };
        root.AddChild(target); root.AddChild(other);
        using var tree = new SceneTree(root);
        target.Position = new(10, 20); other.Position = new(30, 40);
        var view = PhysicsServer.BodyGetDirectState(target.GetRID())!;
        Check(view.Transform.Origin.IsEqualApprox(target.Position) &&
            Box2D.NET.B2Bodies.b2Body_GetPosition(other.BackendID) == default,
            "A direct body view synchronizes its own pose without scanning unrelated bodies.");
        tree.PhysicsFrame(1d / 60);
        Check(other.Position.DistanceTo(new(30, 40)) < .001f, "The world step synchronizes the remaining body normally.");
    }

    private static void VerifyPoseNotificationWrites()
    {
        using var root = new Node();
        var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(10, 0) };
        root.AddChild(body);
        using var tree = new SceneTree(root);
        var notifications = 0;
        body.NotifyLocalTransformChanges = true;
        body.LocalTransformChanged += _ =>
        {
            notifications++;
            body.LinearVelocity = new(23, -7);
            body.AngularVelocity = 0.5f;
        };
        tree.PhysicsFrame(1d / 60);
        var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        // Only unit-conversion rounding is allowed: no further integration follows this notification.
        Check(notifications > 0 && (body.LinearVelocity - new Vector2(23, -7)).Length() < 0.0001f &&
            (state.LinearVelocity - body.LinearVelocity).Length() < 0.0001f &&
            MathF.Abs(body.AngularVelocity - 0.5f) < 0.000001f && MathF.Abs(state.AngularVelocity - 0.5f) < 0.000001f,
            "Solver motion is sampled after pose notifications, retaining user velocity edits in both scene and direct state.");
    }

    private static void VerifyLiveFieldsAndForces()
    {
        using var circle = new CircleShape();
        var root = new Node();
        var body = new RigidBody { Mass = 2, GravityScale = 0, Rotation = Mathf.Pi / 2, CanSleep = false };
        body.AddChild(new CollisionShape { Shape = circle, Position = new(20, 0) }); root.AddChild(body);
        using var tree = new SceneTree(root);
        var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        Check(ReferenceEquals(state, PhysicsServer.BodyGetDirectState(body.GetRID())) && state.Step == 0,
            "The server caches an attached view before the first physics tick.");
        Check(MathF.Abs(state.InverseMass - 0.5f) < 0.001f && MathF.Abs(state.InverseInertia - 0.01f) < 0.001f &&
            state.CenterOfMassLocal.IsEqualApprox(new(20, 0)) && state.CenterOfMass.IsEqualApprox(new(0, 20)),
            $"Mass and both center offsets: {state.InverseMass}, {state.InverseInertia}, {state.CenterOfMassLocal}, {state.CenterOfMass}.");
        state.LinearVelocity = new(10, 0); state.AngularVelocity = 2;
        Check(state.GetVelocityAtLocalPosition(state.CenterOfMass).IsEqualApprox(new(10, 0)) &&
            state.GetVelocityAtLocalPosition(Vector2.Zero).X > 49,
            "Point velocity uses a global-axis body-origin offset and the actual center of mass.");
        state.ApplyCentralImpulse(new(2, 0));
        Check(state.LinearVelocity.IsEqualApprox(new(11, 0)) && MathF.Abs(state.AngularVelocity - 2) < 0.001f,
            "A central impulse acts at the displaced center and produces no angular change.");
        state.ApplyImpulse(new(2, 0));
        Check(state.LinearVelocity.X > 11.9f && state.AngularVelocity is > 2.39f and < 2.41f,
            "An origin impulse produces the center-relative moment with scene inertia units.");
        state.ApplyTorqueImpulse(10);
        Check(state.AngularVelocity is > 2.49f and < 2.51f, "Angular impulses use scene-unit inertia.");
        state.SetConstantForce(new(1, 2)); state.AddConstantCentralForce(new(2, 1));
        state.SetConstantTorque(3); state.AddConstantTorque(2);
        Check(body.ConstantForce == new Vector2(3, 3) && body.ConstantTorque == 5 &&
            state.GetConstantForce() == body.ConstantForce && state.GetConstantTorque() == body.ConstantTorque,
            "The view shares the scene body's persistent-force state.");
        state.SetConstantForce(Vector2.Zero); state.SetConstantTorque(0);
        state.AddConstantForce(new(2, 0));
        Check(state.GetConstantTorque() is > 39.9f and < 40.1f, "Persistent positioned force records the center-relative moment.");
        state.SetConstantForce(Vector2.Zero); state.SetConstantTorque(0);
        state.CollisionLayer = uint.MaxValue; state.CollisionMask = 0x80000000;
        Check(body.CollisionLayer == uint.MaxValue && body.CollisionMask == 0x80000000, "All 32 layer/mask bits are shared.");
        state.Transform = new(0, Vector2.One, 0, new(50, 70));
        Check(body.GlobalPosition == new Vector2(50, 70) && state.Transform.Origin == body.GlobalPosition,
            "Changing state pose updates scene and backend before the next frame.");
        Reject<ArgumentException>(() => state.Transform = new(0, new(2, 1), 0, Vector2.Zero));
        Reject<ArgumentOutOfRangeException>(() => state.LinearVelocity = new(float.NaN, 0));
        Reject<ArgumentOutOfRangeException>(() => state.GetContactLocalPosition(0));
        Check(Task.Run(() => Capture(() => _ = state.LinearVelocity)).GetAwaiter().GetResult() is InvalidOperationException,
            "Live state access rejects the wrong thread.");
        state.Dispose();
        Reject<ObjectDisposedException>(() => _ = state.Sleeping);
        Check(!ReferenceEquals(state, PhysicsServer.BodyGetDirectState(body.GetRID())),
            "Caller disposal invalidates only the view; the server can recreate it.");
    }

    private static void VerifyCustomIntegration()
    {
        var root = new Node();
        var body = new ProbeBody { CustomIntegrator = true, CanSleep = false, ConstantForce = new(0, 1000), ConstantTorque = 100 };
        root.AddChild(body);
        using var tree = new SceneTree(root);
        body.ApplyCentralForce(new(0, 1000));
        tree.PhysicsFrame(1d / 60);
        Check(body.Calls == 1 && body.LastState is { TotalGravity.Y: > 979 } && body.GlobalPosition == Vector2.Zero &&
            body.LinearVelocity == Vector2.Zero && body.AngularVelocity == 0,
            "Custom integration omits gravity/damping/force accumulators while still calling the hook after the solver.");
        body.ConstantForce = Vector2.Zero; body.ConstantTorque = 0;
        body.Update = state => state.IntegrateForces();
        tree.PhysicsFrame(1d / 60);
        Check(body.LinearVelocity.Y is > 16.2f and < 16.4f && body.GlobalPosition == Vector2.Zero,
            "Manual integration changes velocity after this solved pose for the next tick.");
        tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y > 0.2f, "The next custom tick moves under the manually integrated velocity.");
        body.Update = null; body.CustomIntegrator = false;
        tree.PhysicsFrame(1d / 60);
        Check(body.LinearVelocity.Y > 40 && !PhysicsServer.BodyIsOmittingForceIntegration(body.GetRID()),
            "Disabling custom integration restores automatic gravity.");
        using var packed = new PackedScene();
        using var saved = new RigidBody { CustomIntegrator = true };
        packed.Pack(saved);
        using var copied = (RigidBody)packed.Instantiate();
        Check(copied.CustomIntegrator, "PackedScene preserves the custom-integration policy.");
    }

    private static void VerifySolvedContacts()
    {
        using var circle = new CircleShape();
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        var root = new Node();
        var body = new ProbeBody { MaxContactsReported = 4, CanSleep = false };
        body.AddChild(new CollisionShape { Shape = circle });
        var floor = new StaticBody { Position = new(0, 100) }; floor.AddChild(new CollisionShape { Shape = floorShape });
        root.AddChild(body); root.AddChild(floor);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        var state = body.LastState!;
        Check(state.GetContactCount() > 0 && state.GetContactCount() == body.GetContactCount() &&
            state.GetContactCollider(0) == floor.GetRID() && state.GetContactColliderID(0) == floor.InstanceID &&
            ReferenceEquals(state.GetContactColliderObject(0), floor) && state.GetContactLocalShape(0) == 0 &&
            state.GetContactColliderShape(0) == 0 && state.GetContactLocalNormal(0).Y < -0.99f,
            "Solved contacts retain both shape indices, collider identity and global normal without an object monitor.");
        Check(state.GetContactLocalPosition(0).Y is > 88 and < 92 && state.GetContactColliderPosition(0).Y is > 89 and < 91 &&
            state.GetContactImpulse(0).Y < -10 && state.GetContactLocalVelocityAtPosition(0).Length() < 1 &&
            state.GetContactColliderVelocityAtPosition(0) == Vector2.Zero,
            $"Contact fields: {state.GetContactLocalPosition(0)}, {state.GetContactColliderPosition(0)}, {state.GetContactImpulse(0)}.");
        using var query = new PhysicsPointQueryParameters { Position = floor.GlobalPosition };
        Check(state.GetSpaceState().IntersectPoint(query).Length > 0, "The live state exposes the existing space query view.");
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed solved-contact snapshots and callbacks allocate no managed bytes.");
        body.ContactMonitor = true;
        body.MaxContactsReported = 1;
        tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() == 1 && state.GetContactCount() == 1 &&
            body.GetCollidingBodies().Length == 1, "The monitor and direct view share the capped solved contact.");
        body.MaxContactsReported = 0;
        tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() == 0 && state.GetContactCount() == 0,
            "A zero contact cap clears both snapshots on the next solved step.");
        body.ContactMonitor = false;
        body.MaxContactsReported = 4;
        tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() == state.GetContactCount() && state.GetContactCount() > 0 &&
            body.GetCollidingBodies().Length == 0, "Direct contact fields remain available without object monitoring.");
        body.Update = view =>
        {
            view.CollisionMask = 0;
            view.GetSpaceState().IntersectPoint(query);
            Check(view.GetContactCollider(0) == floor.GetRID(), "Contact snapshots survive fixture edits and queries inside the callback.");
        };
        tree.PhysicsFrame(1d / 60);
    }

    private static void VerifyServerContactLimit()
    {
        using var circle = new CircleShape();
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        var space = PhysicsServer.SpaceCreate();
        var body = PhysicsServer.BodyCreate(); var floor = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.SpaceSetActive(space, true);
            PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(floor, floorShape.GetRID()); PhysicsServer.BodySetTransform(floor, new(0, new(0, 100)));
            PhysicsServer.BodySetSpace(floor, space); PhysicsServer.BodyAddShape(body, circle.GetRID());
            PhysicsServer.BodySetMaxContactsReported(body, 4); PhysicsServer.BodySetSpace(body, space);
            var state = PhysicsServer.BodyGetDirectState(body)!;
            for (var i = 0; i < 120; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            Check(state.GetContactCount() > 0, "Server views capture requested solved contacts without callbacks.");
            PhysicsServer.BodySetMaxContactsReported(body, 0);
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(state.GetContactCount() == 0, "Removing the server contact cap clears the previous snapshot on the next step.");
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 32; i++) { PhysicsServer.SpaceStep(space, 1d / 60); _ = state.Transform; }
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "A persistent zero-contact server view remains live without per-step allocations.");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(floor); PhysicsServer.FreeRID(space); }
    }

    private static void VerifyServerCallbacksAndLifetime()
    {
        var server = PhysicsServer.Service;
        var space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(space, true); var body = PhysicsServer.BodyCreate();
        Check(PhysicsServer.BodyGetDirectState(body) is null, "A detached server body has no direct view.");
        PhysicsServer.BodySetSpace(body, space); PhysicsServer.BodySetOmitForceIntegration(body, true);
        var sequence = new List<int>();
        PhysicsServer.BodySetForceIntegrationCallback(body, (state, data) =>
        {
            sequence.Add(data); state.LinearVelocity = new(10, 0);
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(space, 0.01));
            Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(space));
            Reject<InvalidOperationException>(state.Dispose);
        }, 1);
        PhysicsServer.BodySetStateSyncCallback(body, state => { sequence.Add(2); Check(MathF.Abs(state.LinearVelocity.X - 10) < 0.001f, "Sync sees force-callback writes."); });
        PhysicsServer.BodySetMaxContactsReported(body, 3);
        Check(PhysicsServer.BodyGetMaxContactsReported(body) == 3, "Server contact caps have an executable getter.");
        PhysicsServer.SpaceStep(space, 1d / 60);
        Check(sequence.SequenceEqual(new[] { 1, 2 }) && PhysicsServer.BodyIsOmittingForceIntegration(body), "Typed force data precedes sync without changing omission policy.");
        var state = PhysicsServer.BodyGetDirectState(body)!;
        var pose = state.Transform;
        var viewBytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++) Check(state.Transform == pose, "An unchanged live pose retains its exact transform.");
        Check(GC.GetAllocatedBytesForCurrentThread() == viewBytes, "Repeated live pose reads allocate no managed bytes.");
        state.Transform = new(.7f, pose.Origin + new Vector2(3, -4));
        Check(state.Transform == PhysicsServer.BodyGetTransform(body) && state.Transform != pose &&
            state.Transform.Origin.IsEqualApprox(pose.Origin + new Vector2(3, -4)),
            "Direct pose writes are immediately visible and match the live server pose.");
        PhysicsServer.BodySetTransform(body, new(.2f, pose.Origin));
        Check(state.Transform == PhysicsServer.BodyGetTransform(body) && state.Transform.Origin.IsEqualApprox(pose.Origin),
            "Server pose writes are visible through an already acquired view.");
        state.AngularVelocity = 2; state.SetConstantForce(new(1, 0)); state.SetConstantTorque(3);
        PhysicsServer.BodySetForceIntegrationCallback(body, null); PhysicsServer.BodySetStateSyncCallback(body, null);
        PhysicsServer.BodySetSpace(body, default);
        Reject<ObjectDisposedException>(() => _ = state.LinearVelocity);
        PhysicsServer.BodySetSpace(body, space);
        var fresh = PhysicsServer.BodyGetDirectState(body)!;
        Check(!ReferenceEquals(state, fresh) && fresh.AngularVelocity == 2 && fresh.GetConstantForce() == new Vector2(1, 0),
            "Reattachment invalidates old views and preserves body force/motion state.");
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodySetSpace(body, default); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetSpace(body, space);
        Check(PhysicsServer.BodyGetDirectState(body)!.AngularVelocity == 0, "A static-mode transition clears the retained angular velocity across attachments.");
        PhysicsServer.FreeRID(body); Reject<ObjectDisposedException>(() => _ = fresh.Step); PhysicsServer.FreeRID(space);
    }

    private static void VerifyReattachmentViews()
    {
        using var root = new Node();
        var scene = new RigidBody { GravityScale = 0 };
        root.AddChild(scene);
        using var tree = new SceneTree(root);
        var space = PhysicsServer.SpaceCreate();
        var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetSpace(body, space);
            var sceneView = PhysicsServer.BodyGetDirectState(scene.GetRID())!;
            var serverView = PhysicsServer.BodyGetDirectState(body)!;
            // 0.0001 scene units/s allows only float unit-conversion rounding, not simulation drift.
            for (var iteration = 0; iteration < 4; iteration++)
            {
                root.RemoveChild(scene);
                PhysicsServer.BodySetSpace(body, default);
                root.AddChild(scene);
                PhysicsServer.BodySetSpace(body, space);
                var nextScene = PhysicsServer.BodyGetDirectState(scene.GetRID())!;
                var nextServer = PhysicsServer.BodyGetDirectState(body)!;
                foreach (var pair in new[] { (Old: sceneView, Current: nextScene), (Old: serverView, Current: nextServer) })
                {
                    pair.Current.LinearVelocity = new(7, 11);
                    Reject<ObjectDisposedException>(() => _ = pair.Old.Transform);
                    Reject<ObjectDisposedException>(() => _ = pair.Old.GetContactCount());
                    Reject<ObjectDisposedException>(() => pair.Old.ApplyCentralImpulse(new(100, 0)));
                    Reject<ObjectDisposedException>(() => pair.Old.Sleeping = true);
                    Check((pair.Current.LinearVelocity - new Vector2(7, 11)).Length() <= 0.0001f && !pair.Current.Sleeping,
                        "An old attachment view cannot read or mutate a replacement body on the same RID and space.");
                }
                sceneView = nextScene; serverView = nextServer;
            }
            sceneView.Dispose();
            var replacement = PhysicsServer.BodyGetDirectState(scene.GetRID())!;
            Check(!ReferenceEquals(sceneView, replacement) && (replacement.LinearVelocity - new Vector2(7, 11)).Length() <= 0.0001f,
                "Replacing a caller-disposed view retains the active body's state.");
            Reject<ObjectDisposedException>(() => sceneView.SetConstantForce(Vector2.One));
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
    }

    private static void VerifyCallbackFailureAndMutations()
    {
        var root = new Node(); var first = new ProbeBody { Name = "First", GravityScale = 0, CanSleep = false }; var second = new ProbeBody { Name = "Second", GravityScale = 0, CanSleep = false };
        root.AddChild(first); root.AddChild(second);
        using var tree = new SceneTree(root);
        first.Update = state => { state.LinearVelocity = new(20, 0); throw new InvalidOperationException("User callback failed."); };
        Check(Capture(() => tree.PhysicsFrame(1d / 60)) is AggregateException && second.Calls == 1 && MathF.Abs(first.LinearVelocity.X - 20) < 0.001f,
            "A failed callback retains committed edits and allows the next body callback and pose sync.");
        first.Update = _ => { root.RemoveChild(second); };
        tree.PhysicsFrame(1d / 60);
        Check(second.Calls == 1 && !second.IsInsideTree, "Removing a later body safely skips its captured callback entry.");
        second.Dispose();
        var later = new RigidBody { Name = "LateReceiver", GravityScale = 0, CanSleep = false };
        root.AddChild(later); var calls = 0;
        first.Update = _ => PhysicsServer.BodySetStateSyncCallback(later.GetRID(), _ => calls++);
        tree.PhysicsFrame(1d / 60);
        Check(calls == 1, "An earlier integration callback can enable a later ordinary body's sync callback in the same frame.");
    }

    private static void VerifyServerFieldsAndForces()
    {
        using var areaShape = new RectangleShape { Size = new(1000, 1000) };
        var root = new Node();
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
        area.AddChild(new CollisionShape { Shape = areaShape }); root.AddChild(area);
        using var tree = new SceneTree(root);
        var server = PhysicsServer.Service;
        var body = PhysicsServer.BodyCreate(); var circle = PhysicsServer.CircleShapeCreate();
        PhysicsServer.BodyAddShape(body, circle); PhysicsServer.BodySetSpace(body, area.GetWorld()!.Space);
        var state = PhysicsServer.BodyGetDirectState(body)!;
        state.LinearVelocity = new(100, 0);
        var inverseMass = state.InverseMass;
        state.ApplyCentralForce(new(60, 0)); state.ApplyTorque(10); state.ApplyForce(new(0, 4), new(20, 0));
        tree.PhysicsFrame(1d / 60);
        Check(state.TotalGravity == new Vector2(120, 0) && state.TotalLinearDamp == 3 && state.TotalAngularDamp == 2 &&
            MathF.Abs(state.LinearVelocity.X - (95 + (120 + 60 * inverseMass) / 60)) < 0.1f && state.AngularVelocity > 0,
            "Server bodies share Area field reduction and positioned/central force accumulators.");
        PhysicsServer.BodySetOmitForceIntegration(body, true);
        state.LinearVelocity = Vector2.Zero; state.AngularVelocity = 0;
        state.ApplyCentralForce(new(0, 100)); state.SetConstantForce(new(100, 0));
        tree.PhysicsFrame(1d / 60);
        Check(state.LinearVelocity == Vector2.Zero, "Server omission clears force accumulators and skips gravity/damping.");
        state.IntegrateForces();
        Check(state.LinearVelocity.X is > 1.89f and < 1.91f, "Manual state integration uses gravity before selected damping.");
        PhysicsServer.BodySetOmitForceIntegration(body, false); state.SetConstantForce(Vector2.Zero);
        tree.PhysicsFrame(1d / 60);
        Check(state.LinearVelocity.X > 3.7f, "Disabling server omission restores automatic gravity.");
        state.Sleeping = true;
        area.Gravity = 240;
        tree.PhysicsFrame(1d / 60);
        Check(!state.Sleeping && state.TotalGravity.X == 240, "A changed Area field wakes a server body.");
        PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(circle);
    }

    private static void VerifyWarmAllocation()
    {
        var root = new Node(); var body = new ProbeBody { CustomIntegrator = true, CanSleep = false };
        body.Update = static state => state.LinearVelocity = new(10, 0); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed active post-solver callbacks and live view access allocate no managed bytes.");
    }

    private sealed class ProbeBody : RigidBody
    {
        internal int Calls;
        internal PhysicsDirectBodyState? LastState;
        internal Action<PhysicsDirectBodyState>? Update;
        protected override void IntegrateForces(PhysicsDirectBodyState state) { Calls++; LastState = state; Update?.Invoke(state); }
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        if (Capture(action) is not T) throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
