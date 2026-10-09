using Electron2D;

internal static class PhysicsMassProfileTests
{
    internal static void Run()
    {
        VerifyDeferredMassCompletion();
        VerifyRotatedGeometry();
        VerifySceneProfile();
        VerifyServerProfile();
        VerifySegmentsAndEmptyBodies();
        VerifyStorageAndFailures();
        VerifyWarmChanges();
        Console.WriteLine("Rigid and server mass, custom center/inertia, restoration and allocation checks passed.");
    }

    private static void VerifyRotatedGeometry()
    {
        using var polygon = new ConvexPolygonShape { Points = [Vector2.Zero, new(20, 0), new(0, 30)] };
        using var lines = new ConcavePolygonShape { Segments = [new(-10, 0), new(10, 0), new(10, 0), new(10, 30)] };
        (Shape Shape, Vector2 Center, float Inertia)[] cases = [(polygon, new(20f / 3, 10), 3 * 1300f / 18), (lines, new(6, 9), 409)];
        var space = PhysicsServer.SpaceCreate();
        try
        {
            foreach (var sample in cases)
            {
                var body = PhysicsServer.BodyCreate();
                try
                {
                    var pose = new Transform(0.3f, new(8, 11));
                    PhysicsServer.BodySetMass(body, 3); PhysicsServer.BodyAddShape(body, sample.Shape.GetRID(), pose);
                    PhysicsServer.BodyApplyCentralImpulse(body, new(1, 0));
                    var center = pose * sample.Center;
                    Check(PhysicsServer.BodyGetCenterOfMass(body).DistanceTo(center) < 0.001f && MathF.Abs(PhysicsServer.BodyGetInertia(body) - sample.Inertia) < 0.01f,
                        "Detached rotated mass uses the authored unit basis and analytic polar moment.");
                    PhysicsServer.BodySetSpace(body, space);
                    Check(PhysicsServer.BodyGetCenterOfMass(body).DistanceTo(center) < 0.001f && MathF.Abs(PhysicsServer.BodyGetInertia(body) - sample.Inertia) < 0.01f,
                        "Attached fixtures preserve the same rotated centroid and polar moment.");
                }
                finally { PhysicsServer.FreeRID(body); }
            }
        }
        finally { PhysicsServer.FreeRID(space); }
    }

    private static void VerifyDeferredMassCompletion()
    {
        using var shape = new RectangleShape { Size = new(24, 30) };
        var root = new Node();
        var body = new RigidBody { Mass = 3, Inertia = 125, CenterOfMassMode = RigidCenterOfMassMode.Custom, CenterOfMass = new(2, 4) };
        body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        using var tree = new SceneTree(root);
        void Clean(RID rid)
        {
            var state = PhysicsServer.BodyGetDirectState(rid)!;
            var id = PhysicsServer.Service.BodyRuntime(rid).Backend.BodyID;
            var world = Box2D.NET.B2Worlds.b2GetWorldFromId(Box2D.NET.B2Bodies.b2Body_GetWorld(id));
            Check((Box2D.NET.B2Bodies.b2GetBodyFullId(world, id).flags & (uint)Box2D.NET.B2BodyFlags.b2_dirtyMass) == 0,
                "Applying a validated scene/server mass profile completes deferred fixture mass before the solver.");
        }
        Clean(body.GetRID());
        shape.Size = new(40, 20); Clean(body.GetRID());
        body.Freeze = true; Clean(body.GetRID()); body.Freeze = false; Clean(body.GetRID());
        tree.PhysicsFrame(1d / 60);
        Check(body.Mass == 3 && body.Inertia == 125 && body.CenterOfMass == new Vector2(2, 4), "Completing deferred mass preserves custom scene values.");
        var server = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(server, shape.GetRID()); PhysicsServer.BodySetSpace(server, body.GetWorld()!.Space);
            PhysicsServer.BodySetMass(server, 5); PhysicsServer.BodySetInertia(server, 220); PhysicsServer.BodySetCenterOfMass(server, new(3, 1));
            Clean(server); shape.Size = new(30, 30); Clean(server);
            tree.PhysicsFrame(1d / 60);
            Check(PhysicsServer.BodyGetMass(server) == 5 && Near(PhysicsServer.BodyGetInertia(server), 220), "Server mass completion preserves explicit overrides.");
        }
        finally { PhysicsServer.FreeRID(server); }
    }

    private static void VerifySceneProfile()
    {
        using var circle = new CircleShape { Radius = 10 };
        var root = new Node(); var body = new RigidBody { Mass = 2, GravityScale = 0, CanSleep = false, Rotation = Mathf.Pi / 2 };
        var owner = body.CreateShapeOwner(null); body.ShapeOwnerAddShape(owner, circle);
        body.ShapeOwnerSetTransform(owner, new(0, Vector2.One, 0, new(20, 0))); root.AddChild(body);
        Check(body.CenterOfMassMode == RigidCenterOfMassMode.Auto && body.CenterOfMass == Vector2.Zero && body.Inertia == 0 &&
            (int)RigidCenterOfMassMode.Custom == 1, "Stored automatic defaults and enum values.");
        body.CenterOfMass = Vector2.Zero;
        Reject<InvalidOperationException>(() => body.CenterOfMass = new(1, 0));
        using var tree = new SceneTree(root);
        var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        Check(state.CenterOfMassLocal.IsEqualApprox(new(20, 0)) && Near(1 / state.InverseInertia, 100),
            "Automatic native circle center and scene-unit polar inertia.");
        body.CenterOfMassMode = RigidCenterOfMassMode.Custom; body.CenterOfMass = new(5, 0);
        Check(state.CenterOfMassLocal.IsEqualApprox(new(5, 0)) && state.CenterOfMass.IsEqualApprox(new(0, 5)) &&
            Near(1 / state.InverseInertia, 550), "Custom center uses local coordinates and the parallel-axis theorem.");
        body.ApplyImpulse(new(2, 0), new(0, 5));
        Check(Near(state.AngularVelocity, 0), "Impulse at the rotated custom center has no angular moment.");
        body.Inertia = 200; body.Mass = 3;
        Check(body.Inertia == 200 && Near(1 / state.InverseInertia, 200), "Explicit inertia is independent of mass scaling.");
        Check(PhysicsColliderBackend.Simulation(body.BackendID).maxExtent > 0.2f,
            "Custom center changes recompute solver extents around the selected mass center.");
        body.CenterOfMassMode = RigidCenterOfMassMode.Auto;
        Check(body.CenterOfMass == Vector2.Zero && state.CenterOfMassLocal.IsEqualApprox(new(20, 0)) && Near(1 / state.InverseInertia, 200),
            "Returning to Auto clears the stored custom center and preserves explicit inertia.");
        body.Inertia = 0; circle.Radius = 5;
        Check(Near(PhysicsServer.BodyGetInertia(body.GetRID()), 37.5f) && body.Inertia == 0,
            "Automatic inertia recomputes after a live resource revision without replacing the stored zero.");
        var oldMass = body.Mass; var oldInverse = state.InverseMass;
        Reject<ArgumentOutOfRangeException>(() => body.Mass = float.MaxValue);
        Reject<ArgumentOutOfRangeException>(() => body.Inertia = float.Epsilon);
        Reject<ArgumentOutOfRangeException>(() => body.Inertia = -1);
        Reject<ArgumentOutOfRangeException>(() => body.CenterOfMassMode = (RigidCenterOfMassMode)99);
        Check(body.Mass == oldMass && state.InverseMass == oldInverse && body.Inertia == 0, "Numeric failures retain configuration and native data.");
        body.CenterOfMassMode = RigidCenterOfMassMode.Custom;
        Reject<ArgumentOutOfRangeException>(() => body.CenterOfMass = new(float.MaxValue, 0));
        body.CenterOfMass = new(3, 4); body.Inertia = 75;
        body.Freeze = true;
        Check(state.InverseMass == 0 && state.InverseInertia == 0 && state.CenterOfMassLocal.IsEqualApprox(new(3, 4)),
            "Frozen bodies retain configured center while exposing static inverse values.");
        body.Freeze = false; body.DisableMode = CollisionDisableMode.MakeStatic; body.ProcessMode = ProcessMode.Disabled;
        Check(state.InverseMass == 0 && state.CenterOfMassLocal.IsEqualApprox(new(3, 4)), "MakeStatic retains the mass profile and view.");
        body.ProcessMode = ProcessMode.Always;
        Check(Near(state.InverseMass, 1f / 3) && Near(1 / state.InverseInertia, 75), "Enable restores custom mass and inertia.");
        body.LockRotation = true; Check(state.InverseInertia == 0, "Rotation lock masks inverse inertia without discarding configured inertia.");
        body.LockRotation = false; Check(Near(1 / state.InverseInertia, 75), "Unlock restores the explicit polar moment.");
        var stationary = new StaticBody { Name = "stationary" };
        stationary.ShapeOwnerAddShape(stationary.CreateShapeOwner(null), circle); root.AddChild(stationary);
        var server = PhysicsServer.Service; PhysicsServer.BodySetMass(stationary.GetRID(), 6);
        PhysicsServer.BodySetCenterOfMass(stationary.GetRID(), new(3, 4)); PhysicsServer.BodySetInertia(stationary.GetRID(), 20);
        var stationaryState = PhysicsServer.BodyGetDirectState(stationary.GetRID())!;
        Check(stationaryState.InverseMass == 0 && stationaryState.InverseInertia == 0 &&
            stationaryState.CenterOfMassLocal.IsEqualApprox(new(3, 4)) && PhysicsServer.BodyGetMass(stationary.GetRID()) == 6,
            "Non-rigid scene bodies share configured server profiles with role-correct physical inverse values.");
        root.RemoveChild(stationary); root.AddChild(stationary);
        Check(PhysicsServer.BodyGetCenterOfMass(stationary.GetRID()).IsEqualApprox(new(3, 4)) && PhysicsServer.BodyGetInertia(stationary.GetRID()) == 20,
            "Non-rigid scene server profiles survive exit/reentry.");
    }

    private static void VerifyServerProfile()
    {
        var server = PhysicsServer.Service;
        var body = PhysicsServer.BodyCreate(); var shape = PhysicsServer.CircleShapeCreate(); var space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(space, true);
        try
        {
            Check(PhysicsServer.BodyGetMass(body) == 1 && PhysicsServer.BodyGetInertia(body) == 0 && PhysicsServer.BodyGetCenterOfMass(body) == Vector2.Zero,
                "Detached server bodies default to one kilogram and unresolved automatic geometry.");
            PhysicsServer.BodyAddShape(body, shape, new(0, Vector2.One, 0, new(20, 0)));
            PhysicsServer.BodySetSpace(body, space);
            var state = PhysicsServer.BodyGetDirectState(body)!;
            Check(state.InverseMass == 1 && Near(PhysicsServer.BodyGetInertia(body), 50) && PhysicsServer.BodyGetCenterOfMass(body).IsEqualApprox(new(20, 0)),
                "Server fixtures use configured kilograms instead of raw density mass.");
            PhysicsServer.BodySetMass(body, 4); PhysicsServer.BodySetCenterOfMass(body, new(10, 0));
            Check(Near(state.InverseMass, 0.25f) && Near(PhysicsServer.BodyGetInertia(body), 600), "Server custom center and geometry inertia execute.");
            PhysicsServer.BodySetInertia(body, 100); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            Check(state.InverseMass == 0 && PhysicsServer.BodyGetMass(body) == 4 && PhysicsServer.BodyGetInertia(body) == 100 &&
                state.CenterOfMassLocal.IsEqualApprox(new(10, 0)), "Static server mode preserves configured values.");
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.RigidLinear);
            Check(Near(state.InverseMass, 0.25f) && state.InverseInertia == 0, "RigidLinear applies mass and prevents angular response.");
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
            Check(Near(1 / state.InverseInertia, 100), "Dynamic mode restores the stored inertia override.");
            PhysicsServer.BodySetSpace(body, default); PhysicsServer.BodySetMass(body, 5); PhysicsServer.BodySetCenterOfMass(body, new(8, 0));
            PhysicsServer.BodySetSpace(body, space); state = PhysicsServer.BodyGetDirectState(body)!;
            Check(Near(state.InverseMass, 0.2f) && state.CenterOfMassLocal.IsEqualApprox(new(8, 0)), "Detached edits and reentry retain the configured profile.");
            PhysicsServer.BodyResetMassProperties(body);
            Check(PhysicsServer.BodyGetMass(body) == 5 && Near(PhysicsServer.BodyGetInertia(body), 250) &&
                PhysicsServer.BodyGetCenterOfMass(body).IsEqualApprox(new(20, 0)), "Reset selects both automatic channels while retaining mass.");
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetMass(body, 0));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetInertia(body, float.NaN));
            Check(PhysicsServer.BodyGetMass(body) == 5 && Near(PhysicsServer.BodyGetInertia(body), 250), "Rejected server edits cannot partially replace native data.");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(shape); PhysicsServer.FreeRID(space); }
        Reject<ArgumentException>(() => PhysicsServer.BodyGetMass(body));
        var area = PhysicsServer.AreaCreate();
        try { Reject<ArgumentException>(() => PhysicsServer.BodySetMass(area, 2)); }
        finally { PhysicsServer.FreeRID(area); }
    }

    private static void VerifySegmentsAndEmptyBodies()
    {
        using var segment = new SegmentShape { A = new(-30, 0), B = new(30, 0) };
        var root = new Node(); var body = new RigidBody { Mass = 2, GravityScale = 0 };
        body.ShapeOwnerAddShape(body.CreateShapeOwner(null), segment); root.AddChild(body);
        var empty = new RigidBody
        {
            Name = "empty",
            Mass = 3,
            Inertia = 12,
            CenterOfMassMode = RigidCenterOfMassMode.Custom,
            CenterOfMass = new(5, 0),
            GravityScale = 0
        };
        root.AddChild(empty); using var tree = new SceneTree(root);
        var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        Check(Near(1 / state.InverseInertia, 600), "Zero-area line mass keeps length-weighted rod inertia.");
        body.CenterOfMassMode = RigidCenterOfMassMode.Custom; body.CenterOfMass = new(10, 0);
        Check(Near(1 / state.InverseInertia, 800), "Custom centers shift segment rod inertia by the same parallel-axis policy.");
        var emptyState = PhysicsServer.BodyGetDirectState(empty.GetRID())!;
        Check(Near(emptyState.InverseMass, 1f / 3) && Near(1 / emptyState.InverseInertia, 12) &&
            emptyState.CenterOfMassLocal.IsEqualApprox(new(5, 0)), "Explicit inertia works without collision geometry.");
        empty.ApplyTorqueImpulse(12); Check(Near(emptyState.AngularVelocity, 1), "Unshaped explicit inertia gives real angular impulse response.");
        empty.Inertia = 0; Check(emptyState.InverseInertia == 0, "Automatic unshaped inertia is zero.");
    }

    private static void VerifyStorageAndFailures()
    {
        var root = new Node(); var body = new RigidBody
        {
            Mass = 4,
            Inertia = 30,
            CenterOfMassMode = RigidCenterOfMassMode.Custom,
            CenterOfMass = new(2, 3),
            Freeze = true
        };
        root.AddChild(body); body.Owner = root; using var scene = new PackedScene(); scene.Pack(root); root.Dispose();
        using var copy = scene.Instantiate(); using var tree = new SceneTree(copy);
        var stored = (RigidBody)copy.GetChild(0);
        Check(stored.Mass == 4 && stored.Inertia == 30 && stored.CenterOfMassMode == RigidCenterOfMassMode.Custom &&
            stored.CenterOfMass == new Vector2(2, 3), "PackedScene restores ordered mode/center/inertia configuration.");
        var server = PhysicsServer.Service;
        Action<ElectronObject> fail = _ => throw new ApplicationException("profile");
        stored.PropertyListChanged += fail;
        Check(Capture(() => PhysicsServer.BodyResetMassProperties(stored.GetRID())) is ApplicationException && stored.Inertia == 0 &&
            stored.CenterOfMassMode == RigidCenterOfMassMode.Auto && stored.CenterOfMass == Vector2.Zero,
            "Reset commits both channels before a property-list callback failure.");
        Check(Capture(() => PhysicsServer.BodySetCenterOfMass(stored.GetRID(), new(6, 7))) is ApplicationException && stored.CenterOfMass == new Vector2(6, 7),
            "Server custom center is atomic even if a mode notification throws.");
        stored.PropertyListChanged -= fail;
        Check(Task.Run(() => Capture(() => PhysicsServer.BodySetMass(stored.GetRID(), 2))).Result is InvalidOperationException,
            "Scene-owned server mass changes enforce owner thread even when frozen.");
        var moving = new MassDuringPose
        {
            Name = "moving",
            GravityScale = 0,
            CanSleep = false,
            LinearVelocity = new(20, 0),
            NotifyLocalTransformChanges = true
        };
        copy.AddChild(moving); tree.PhysicsFrame(1d / 60);
        Check(moving.Rejected && moving.Mass == 1 && moving.Inertia == 0 && moving.CenterOfMassMode == RigidCenterOfMassMode.Auto,
            "Solver pose synchronization rejects profile edits before native or stored state changes.");
        using var disposed = new RigidBody(); disposed.Dispose();
        Reject<ObjectDisposedException>(() => disposed.Inertia = 4);
        Reject<ObjectDisposedException>(() => _ = disposed.CenterOfMass);
    }

    private static void VerifyWarmChanges()
    {
        using var circle = new CircleShape();
        var root = new Node(); var body = new RigidBody
        {
            GravityScale = 0,
            CanSleep = false,
            CenterOfMassMode = RigidCenterOfMassMode.Custom
        };
        body.ShapeOwnerAddShape(body.CreateShapeOwner(null), circle); root.AddChild(body); using var tree = new SceneTree(root);
        var server = PhysicsServer.Service; var rid = body.GetRID();
        for (var pass = 0; pass < 64; pass++) { ChangeProfile(); tree.PhysicsFrame(1d / 60); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 64; pass++) { ChangeProfile(); tree.PhysicsFrame(1d / 60); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed profile changes and active solver frames allocate zero managed bytes.");
        void ChangeProfile() { body.Mass = 2; body.Inertia = 20; PhysicsServer.BodySetCenterOfMass(rid, new(2, 0)); PhysicsServer.BodySetMass(rid, 3); PhysicsServer.BodySetInertia(rid, 0); }
    }

    private sealed class MassDuringPose : RigidBody
    {
        internal bool Rejected;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != NotificationLocalTransformChanged || !IsInsideTree) return;
            Rejected = Capture(() => Mass = 2) is InvalidOperationException &&
                Capture(() => Inertia = 20) is InvalidOperationException &&
                Capture(() => CenterOfMassMode = RigidCenterOfMassMode.Custom) is InvalidOperationException &&
                Capture(() => PhysicsServer.BodySetCenterOfMass(GetRID(), new(4, 0))) is InvalidOperationException;
        }
    }

    private static bool Near(float a, float b) => MathF.Abs(a - b) <= 0.01f * MathF.Max(1, MathF.Abs(b));
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
