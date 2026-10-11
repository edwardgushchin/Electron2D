using Electron2D;

public static partial class BackendChecks
{
    private static void MotionRoleChecks()
    {
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("MotionRoles", () => solver = new());
        using var world = new World("MotionRoles");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        var scene = new RigidBody { LockRotation = true, GravityScale = 0, CanSleep = false };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        var initialGuard = false;
        _ = world.Space;
        solver!.IdentityHook = _ =>
        {
            RejectRoleWrites(); initialGuard = true;
        };
        using var tree = new SceneTree(root);
        solver.IdentityHook = null;
        Check(initialGuard, "Initial attachment exercises direct scene role write guards");
        var implementation = solver!.Bodies[scene.GetRID()];
        Check(implementation.Mode == PhysicsServer.BodyMode.RigidLinear && implementation.Locked,
            "Initial locked scene body delivers the complete RigidLinear configuration");
        scene.LinearVelocity = new(2, 0); scene.AngularVelocity = 0;
        scene.LockRotation = false;
        Check(implementation.Mode == PhysicsServer.BodyMode.Rigid && !implementation.Locked,
            "Live unlock delivers Rigid as well as clearing the rotation constraint");
        Check(scene.LinearVelocity == new Vector2(2, 0), "Unlock retains the dynamic linear velocity");
        scene.LockRotation = true;
        Check(implementation.Mode == PhysicsServer.BodyMode.RigidLinear && implementation.Locked,
            "Live lock delivers RigidLinear as well as constraining rotation");
        scene.FreezeMode = RigidFreezeMode.Kinematic; scene.Freeze = true;
        Check(implementation.Mode == PhysicsServer.BodyMode.Kinematic && !implementation.Locked,
            "Kinematic freeze temporarily replaces the locked dynamic role");
        scene.LockRotation = false; scene.LockRotation = true;
        Check(implementation.Mode == PhysicsServer.BodyMode.Kinematic && !implementation.Locked,
            "Changing stored lock while frozen preserves the freeze role");
        scene.Freeze = false;
        Check(implementation.Mode == PhysicsServer.BodyMode.RigidLinear && implementation.Locked,
            "Unfreeze restores the exact locked dynamic role");
        scene.FreezeMode = RigidFreezeMode.Static; scene.Freeze = true;
        Check(implementation.Mode == PhysicsServer.BodyMode.Static && !implementation.Locked,
            "Static freeze overrides the stored rotation lock");
        scene.Freeze = false;
        scene.DisableMode = CollisionDisableMode.MakeStatic; scene.ProcessMode = ProcessMode.Disabled;
        Check(implementation.Mode == PhysicsServer.BodyMode.Static, "Disable policy selects the static role");
        scene.LockRotation = false; scene.LockRotation = true;
        Check(implementation.Mode == PhysicsServer.BodyMode.Static, "Stored lock changes preserve the disabled static role");
        scene.ProcessMode = ProcessMode.Inherit;
        Check(implementation.Mode == PhysicsServer.BodyMode.RigidLinear && implementation.Locked,
            "Enable restores the exact configured dynamic role");
        solver.IdentityHook = _ =>
        {
            RejectRoleWrites();
        };
        PhysicsServer.BodyAttachCanvasInstanceID(scene.GetRID(), 17); solver.IdentityHook = null;
        Check(scene.LockRotation && implementation.Mode == PhysicsServer.BodyMode.RigidLinear,
            "Numerical borrowing rejects a lock edit before changing authoring or implementation mode");
        scene.AngularVelocity = 0; scene.LinearVelocity = new(3, 0);
        for (var i = 0; i < 8; i++) tree.PhysicsFrame(1d / 60);
        Check(scene.Position.X > .3f && scene.Rotation == 0,
            "Locked dynamic role remains simulated and published through ordinary scene motion");
        Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(own == 0 && all == 0, $"Locked registered full frames allocated {own}/{all} owner/all-thread B");
        Task.Run(() => Reject<InvalidOperationException>(() => scene.LockRotation = false)).GetAwaiter().GetResult();
        using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
        var saved = scene.Position;
        tree.PhysicsFrame(1d / 60); checkpoint.Restore();
        Check(scene.Position.IsEqualApprox(saved) && implementation.Mode == PhysicsServer.BodyMode.RigidLinear,
            "Checkpoint retains the exact locked role and restores its simulated pose");
        var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.RigidLinear);
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetSpace(raw, world.Space);
            Check(solver.Bodies[raw].Mode == implementation.Mode && PhysicsServer.BodyGetMode(raw) == implementation.Mode,
                "Scene and raw locked bodies deliver the same exact implementation mode");
        }
        finally { PhysicsServer.FreeRID(raw); }
        Console.WriteLine("Registered motion roles: initial/live rotation lock, freeze, disable and scene/raw RigidLinear delivery passed.");
        void RejectRoleWrites()
        {
            Reject<InvalidOperationException>(() => scene.LockRotation = false);
            Reject<InvalidOperationException>(() => scene.Freeze = true);
            Reject<InvalidOperationException>(() => scene.FreezeMode = RigidFreezeMode.Kinematic);
        }
    }
}
