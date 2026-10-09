using Electron2D;

internal sealed class RigidFreezeModeTests(PhysicsServer.Backend backend)
{
    internal static void Run(PhysicsServer.Backend backend = PhysicsServer.Backend.CPU) => new RigidFreezeModeTests(backend).RunCore();

    private void RunCore()
    {
        VerifyRolesAndMotion();
        VerifyDynamicContact();
        VerifyFastPathContact();
        VerifyLifetimeAndPolicy();
        VerifyStorageAndFailures();
        VerifySiblingIdleRotation();
        VerifyWarmMotion();
        Console.WriteLine($"Rigid freeze roles, manual kinematic motion, contact response and allocation checks passed on {backend}.");
    }

    private void VerifyRolesAndMotion()
    {
        using var circle = new CircleShape(); using var world = new World(backend); var root = new SubViewport { World = world };
        var body = new RigidBody { Mass = 2, Inertia = 100, LockRotation = true, CanSleep = false };
        Add(body, circle); root.AddChild(body); using var tree = new SceneTree(root);
        var server = PhysicsServer.Service; var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        Check(body.FreezeMode == RigidFreezeMode.Static && (int)RigidFreezeMode.Kinematic == 1, "Freeze enum values and default.");
        body.FreezeMode = RigidFreezeMode.Kinematic;
        Check(!body.Freeze && body.Backend.HasMotionMode(PhysicsServer.BodyMode.Rigid) && Near(state.InverseMass, 0.5f),
            "Stored mode has no effect on an unfrozen body's dynamic role.");
        body.Freeze = true;
        Check(state.InverseMass == 0 && state.InverseInertia == 0 && body.Backend.HasMotionMode(PhysicsServer.BodyMode.Kinematic),
            "Kinematic freeze changes the physical role without discarding the configured mass.");
        body.ApplyCentralImpulse(new(100, 100)); body.ConstantForce = new(100, 100); body.ConstantTorque = 100;
        body.Position = new(20, 0); body.Rotation = 0.1f;
        state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        Check(state.Transform.Origin.IsEqualApprox(new(20, 0)), "A forced query presents the latest manual pose immediately.");
        tree.PhysicsFrame(0);
        Check(body.Position.IsEqualApprox(new(20, 0)), "Zero delta retains the manual target.");
        tree.PhysicsFrame(1d / 60);
        Check(Near(body.LinearVelocity.X, 1200) && MathF.Abs(body.AngularVelocity - 6) < 0.1f && body.LockRotation &&
            body.Position.IsEqualApprox(new(20, 0)), $"Manual kinematic result: {body.LinearVelocity}, angular {body.AngularVelocity}, pose {body.Position}, rotation {body.Rotation}.");
        tree.PhysicsFrame(1d / 60);
        Check(body.LinearVelocity.IsZeroApprox() && Near(body.AngularVelocity, 0) && body.Position.IsEqualApprox(new(20, 0)),
            "Idle frozen kinematic frames ignore gravity/forces and clear contact velocity.");
        body.Freeze = false;
        Check(Near(state.InverseMass, 0.5f) && state.InverseInertia == 0 && body.FreezeMode == RigidFreezeMode.Kinematic,
            "Unfreezing restores dynamic mass and configured rotation lock while retaining the mode.");
        body.LockRotation = false; Check(Near(1 / state.InverseInertia, 100), "Unlock restores explicit inertia.");
    }

    private void VerifyDynamicContact()
    {
        using var box = new RectangleShape { Size = new(20, 20) }; using var circle = new CircleShape { Radius = 5 };
        using var world = new World(backend); var root = new SubViewport { World = world }; var pusher = new RigidBody { Name = "pusher", FreezeMode = RigidFreezeMode.Kinematic, Freeze = true };
        var target = new RigidBody
        {
            Name = "target",
            Position = new(25, 0),
            GravityScale = 0,
            CanSleep = false,
            LinearDampMode = RigidBody.DampMode.Replace,
            LinearDamp = 0
        };
        Add(pusher, box); Add(target, circle); root.AddChild(pusher); root.AddChild(target); using var tree = new SceneTree(root);
        for (var frame = 0; frame < 15; frame++) { pusher.Position += new Vector2(2, 0); tree.PhysicsFrame(1d / 60); }
        Check(target.Position.X > 40 && target.LinearVelocity.X > 100 && Near(pusher.LinearVelocity.X, 120),
            $"Animated frozen body pushes a real dynamic contact: {target.Position}, {target.LinearVelocity}.");
    }

    private void VerifyFastPathContact()
    {
        using var box = new RectangleShape { Size = new(10, 10) }; using var circle = new CircleShape { Radius = 5 };
        using var world = new World(backend); var root = new SubViewport { World = world }; var pusher = new RigidBody { Name = "pusher", Freeze = true, FreezeMode = RigidFreezeMode.Kinematic };
        var target = new RigidBody { Name = "target", Position = new(25, 0), GravityScale = 0, CanSleep = false };
        Add(pusher, box); Add(target, circle); root.AddChild(pusher); root.AddChild(target); using var tree = new SceneTree(root);
        var observer = new CountCallbacks
        {
            Name = "observer",
            Position = new(200, 0),
            Mass = 2,
            GravityScale = 0,
            CanSleep = false,
            LinearDampMode = RigidBody.DampMode.Replace,
            LinearDamp = 0
        };
        Add(observer, circle); root.AddChild(observer); observer.ApplyCentralForce(new(120, 0));
        pusher.Position = new(50, 0); tree.PhysicsFrame(1d / 60);
        Check(observer.Count == 1, "Internal collision intervals retain one outer-step integration callback.");
        Check(Near(observer.LinearVelocity.X, 1), "Internal collision steps preserve a force's full outer fixed-step duration.");
        Check(target.LinearVelocity.X > 1 || target.Position.X > 26,
            $"Kinematic frozen sweep crossing a dynamic body: {target.Position}, {target.LinearVelocity}.");
    }

    private void VerifyLifetimeAndPolicy()
    {
        using var circle = new CircleShape(); using var world = new World(backend); var root = new SubViewport { World = world };
        var body = new RigidBody
        {
            Freeze = true,
            FreezeMode = RigidFreezeMode.Kinematic,
            Mass = 3,
            DisableMode = CollisionDisableMode.MakeStatic
        };
        Add(body, circle); root.AddChild(body); using var tree = new SceneTree(root);
        body.ProcessMode = ProcessMode.Disabled;
        Check(body.Backend.HasMotionMode(PhysicsServer.BodyMode.Static) && body.Freeze && body.FreezeMode == RigidFreezeMode.Kinematic,
            "Disable MakeStatic overrides native participation while retaining configured freeze role.");
        body.Position = new(20, 0); tree.PhysicsFrame(1d / 60);
        Check(body.Position.IsEqualApprox(new(20, 0)), "A temporarily static frozen body still accepts manual teleports.");
        body.ProcessMode = ProcessMode.Always; tree.PhysicsFrame(1d / 60);
        Check(body.Backend.HasMotionMode(PhysicsServer.BodyMode.Kinematic) && body.LinearVelocity.IsZeroApprox(),
            "Enable restores kinematic freeze without stale movement from the static interval.");
        var rid = body.GetRID(); root.RemoveChild(body); body.Position = new(30, 0); root.AddChild(body); tree.PhysicsFrame(1d / 60);
        Check(body.GetRID() == rid && body.LinearVelocity.IsZeroApprox(), "Reentry resets solver history at the current scene pose.");
        body.FreezeMode = RigidFreezeMode.Static; body.Position = new(40, 0); tree.PhysicsFrame(1d / 60);
        Check(body.Backend.HasMotionMode(PhysicsServer.BodyMode.Static) && body.Position.IsEqualApprox(new(40, 0)),
            "Live mode change selects stationary manual movement.");
        body.FreezeMode = RigidFreezeMode.Kinematic; tree.PhysicsFrame(1d / 60);
        Check(body.LinearVelocity.IsZeroApprox(), "Switching back snapshots the current pose rather than replaying the last static move.");
    }

    private void VerifyStorageAndFailures()
    {
        using var world = new World(backend); var root = new SubViewport { World = world }; var body = new RigidBody { FreezeMode = RigidFreezeMode.Kinematic, Freeze = true, Mass = 4 };
        root.AddChild(body); body.Owner = root; using var scene = new PackedScene(); scene.Pack(root); root.Dispose();
        using var copy = scene.Instantiate(); using var tree = new SceneTree(copy); var stored = (RigidBody)copy.GetChild(0);
        Check(stored.Freeze && stored.FreezeMode == RigidFreezeMode.Kinematic && stored.Mass == 4 &&
            stored.Backend.HasMotionMode(PhysicsServer.BodyMode.Kinematic), "PackedScene restores freeze policy and physical role.");
        Reject<ArgumentOutOfRangeException>(() => stored.FreezeMode = (RigidFreezeMode)42);
        Check(stored.FreezeMode == RigidFreezeMode.Kinematic, "Undefined modes reject before mutation.");
        Check(Task.Run(() => Capture(() => stored.FreezeMode = RigidFreezeMode.Static)).Result is InvalidOperationException,
            "Attached role changes enforce owner-thread access.");
        var moving = new ChangeDuringPose
        {
            Name = "moving",
            GravityScale = 0,
            CanSleep = false,
            LinearVelocity = new(10, 0),
            NotifyLocalTransformChanges = true
        };
        copy.AddChild(moving); tree.PhysicsFrame(1d / 60);
        Check(moving.Rejected && moving.FreezeMode == RigidFreezeMode.Static, "Solver pose callback rejects role changes before state writes.");
        using var disposed = new RigidBody(); disposed.Dispose();
        Reject<ObjectDisposedException>(() => disposed.FreezeMode = RigidFreezeMode.Kinematic);
    }

    private void VerifySiblingIdleRotation()
    {
        using var world = new World(backend); var root = new SubViewport { World = world }; var body = new AnimatableBody(); root.AddChild(body); using var tree = new SceneTree(root);
        body.Rotation = 0.1f; tree.PhysicsFrame(1d / 60); tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(PhysicsServer.BodyGetAngularVelocity(body.GetRID())) < 0.001f,
            "Synchronized kinematic siblings must not generate angular motion from decoding their own solved pose.");
    }

    private void VerifyWarmMotion()
    {
        using var box = new RectangleShape { Size = new(20, 20) }; using var world = new World(backend); var root = new SubViewport { World = world };
        var body = new RigidBody { Freeze = true, FreezeMode = RigidFreezeMode.Kinematic }; Add(body, box); root.AddChild(body);
        var distant = new RigidBody { Name = "distant", Position = new(5000, 0), GravityScale = 0, CanSleep = false };
        Add(distant, box); root.AddChild(distant);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 64; frame++) { body.Position += new Vector2(20, 0); tree.PhysicsFrame(1d / 60); }
        var before = GC.GetTotalAllocatedBytes(true);
        for (var frame = 0; frame < 64; frame++) { body.Position += new Vector2(20, 0); tree.PhysicsFrame(1d / 60); }
        Check(GC.GetTotalAllocatedBytes(true) == before, "Warmed frozen kinematic pose/solver frames allocate zero managed bytes.");
    }

    private sealed class CountCallbacks : RigidBody
    {
        internal int Count;
        protected override void IntegrateForces(PhysicsDirectBodyState state) => Count++;
    }

    private sealed class ChangeDuringPose : RigidBody
    {
        internal bool Rejected;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationLocalTransformChanged && IsInsideTree)
                Rejected = Capture(() => FreezeMode = RigidFreezeMode.Kinematic) is InvalidOperationException;
        }
    }
    private static void Add(CollisionObject body, Shape shape) => body.ShapeOwnerAddShape(body.CreateShapeOwner(null), shape);
    private static bool Near(float a, float b) => MathF.Abs(a - b) < 0.01f * MathF.Max(1, MathF.Abs(b));
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
