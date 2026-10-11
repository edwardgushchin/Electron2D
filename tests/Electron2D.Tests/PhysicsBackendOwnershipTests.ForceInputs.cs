using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    internal static void ForceInputs(PhysicsServer.Backend backend)
    {
        using var world = new World(new WorldRuntime(false, backend, factory: space => new ViewBackend(space, backend)
        {
            ForceHook = static value => value with { PendingForce = value.PendingForce * 2, PendingTorque = value.PendingTorque * 2 }
        }));
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        var scene = new RigidBody
        {
            Name = "TypedForces",
            Mass = 2,
            Inertia = 8,
            CanSleep = false,
            GravityScale = 2,
            ConstantForce = new(10, 0),
            ConstantTorque = 4
        };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        PhysicsServer.AreaSetGravity(world.Space, 0); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
        var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetMass(raw, 4); PhysicsServer.BodySetInertia(raw, 16); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodySetGravityScale(raw, 3); PhysicsServer.BodySetConstantForce(raw, new(8, 0)); PhysicsServer.BodySetConstantTorque(raw, 6);
            PhysicsServer.BodySetTransform(raw, new(0, new(100, 0))); PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetSpace(raw, world.Space);
            var left = (WrappedCollider)scene.Backend.Implementation!;
            var right = (WrappedCollider)PhysicsServer.Service.BodyRuntime(raw).Backend.Implementation!;
            var sceneRuntime = PhysicsServer.Service.BodyRuntime(scene.GetRID()); var rawRuntime = PhysicsServer.Service.BodyRuntime(raw);
            scene.ApplyCentralForce(new(12, 0)); scene.ApplyTorque(8);
            PhysicsServer.BodyApplyCentralForce(raw, new(16, 0)); PhysicsServer.BodyApplyTorque(raw, 12);
            tree.PhysicsFrame(1d / 60);
            Check(left.ForceInput.PendingForce == new Vector2(12, 0) && left.ForceInput.PendingTorque == 8 &&
                left.ForceInput.ConstantForce == new Vector2(10, 0) && left.ForceInput.ConstantTorque == 4 && left.ForceInput.GravityScale == 2 &&
                !left.ForceInput.OmitForces && !left.ForceInput.RotationLocked && left.ForceInput.ApplyPersistentForces && !left.ForceInput.RefreshGravityScale,
                "Scene complete force intent is sampled once without exposing scene/runtime owners");
            Check(right.ForceInput.PendingForce == new Vector2(16, 0) && right.ForceInput.PendingTorque == 12 && right.ForceInput.GravityScale == 3 &&
                right.ForceInput.ConstantForce == new Vector2(8, 0) && right.ForceInput.ConstantTorque == 6 && right.ForceInput.RefreshGravityScale,
                "Raw complete intent uses the same selected value boundary");
            ForceNear(scene.LinearVelocity.X, 34f / 120, .001f, "Selected transient transformation changes real scene force response");
            ForceNear(PhysicsServer.BodyGetLinearVelocity(raw).X, 40f / 240, .001f, "Selected transient transformation changes real raw force response");
            ForceNear(scene.AngularVelocity, 20f / 480, .001f, "Scene angular force includes persistent and transformed transient torque");
            ForceNear(PhysicsServer.BodyGetAngularVelocity(raw), 30f / 960, .001f, "Raw angular force uses the same physical intent");
            Check(sceneRuntime.PendingForce == Vector2.Zero && sceneRuntime.PendingTorque == 0 && rawRuntime.PendingForce == Vector2.Zero && rawRuntime.PendingTorque == 0,
                "Engine consumes pending commands after successful selected preparation");

            var old = scene.LinearVelocity.X; var leftCalls = left.ForcePreparations; var rightCalls = right.ForcePreparations;
            tree.PhysicsFrame(1d / 60);
            Check(sceneRuntime.PendingForce == Vector2.Zero && rawRuntime.PendingForce == Vector2.Zero &&
                (left.ForcePreparations == leftCalls || left.ForceInput.PendingForce == Vector2.Zero) &&
                (right.ForcePreparations == rightCalls || right.ForceInput.PendingForce == Vector2.Zero), "Transient commands never replay; resident work does not require an empty preparation callback");
            ForceNear(scene.LinearVelocity.X - old, 10f / 120, .001f, "Persistent scene force remains active without transient replay");

            scene.CustomIntegrator = true; PhysicsServer.BodySetOmitForceIntegration(raw, true);
            scene.LinearVelocity = Vector2.Zero; scene.AngularVelocity = 0; PhysicsServer.BodySetLinearVelocity(raw, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(raw, 0);
            scene.ApplyCentralForce(new(12, 0)); PhysicsServer.BodyApplyCentralForce(raw, new(16, 0));
            tree.PhysicsFrame(1d / 60);
            Check(left.ForceInput.OmitForces && right.ForceInput.OmitForces && sceneRuntime.PendingForce == Vector2.Zero && rawRuntime.PendingForce == Vector2.Zero,
                "Omitted integration still consumes selected pending intent once");
            ForceNear(scene.LinearVelocity.Length(), 0, .001f, "Scene omitted integrator suppresses pending and persistent default forces");
            ForceNear(PhysicsServer.BodyGetLinearVelocity(raw).Length(), 0, .001f, "Raw omitted integrator suppresses the same default forces");

            scene.CustomIntegrator = false; PhysicsServer.BodySetOmitForceIntegration(raw, false);
            scene.ConstantForce = Vector2.Zero; scene.ConstantTorque = 0; PhysicsServer.BodySetConstantForce(raw, Vector2.Zero); PhysicsServer.BodySetConstantTorque(raw, 0);
            left.ForceHook = static _ => throw new InvalidOperationException("Expected input preparation failure");
            scene.ApplyCentralForce(new(12, 0)); var tick = PhysicsServer.SpaceGetTick(world.Space);
            Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
            Check(PhysicsServer.SpaceGetTick(world.Space) == tick && sceneRuntime.PendingForce == new Vector2(12, 0), "Rejected preparation retains engine-owned pending intent without committing a tick");
            left.ForceHook = null; tree.PhysicsFrame(1d / 60);
            ForceNear(scene.LinearVelocity.X, 12f / 120, .001f, "Failed pre-solve preparation resumes with one application");
            Check(sceneRuntime.PendingForce == Vector2.Zero, "Successful recovery retires the command");

            scene.MaxContactsReported = 4; scene.FreezeMode = RigidFreezeMode.Kinematic; scene.ConstantForce = new(20, 0); scene.Freeze = true;
            var frozenPose = scene.GlobalPosition;
            PhysicsServer.BodyApplyCentralForce(raw, new(.01f, 0)); tree.PhysicsFrame(1d / 60);
            Check(left.ForceInput.ConstantForce == new Vector2(20, 0) && !left.ForceInput.ApplyPersistentForces && scene.GlobalPosition == frozenPose,
                "Frozen reporting body retains authored constants but suppresses their default application");
            scene.Freeze = false; scene.ConstantForce = Vector2.Zero; scene.CanSleep = true;
            tree.PhysicsFrame(1d / 60); scene.Sleeping = true;
            var sleepingCalls = left.ForcePreparations;
            PhysicsServer.BodyApplyCentralForce(raw, new(.01f, 0)); tree.PhysicsFrame(1d / 60);
            Check(scene.Sleeping && left.ForcePreparations == sleepingCalls,
                $"Stable sleeping scene body skips force inputs: sleeping={scene.Sleeping}, preparations={sleepingCalls}/{left.ForcePreparations}");
            scene.Sleeping = false; scene.CanSleep = false;

            PhysicsServer.BodySetSpace(raw, default); PhysicsServer.BodySetSpace(raw, world.Space);
            right = (WrappedCollider)rawRuntime.Backend.Implementation!;
            PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.RigidLinear); PhysicsServer.BodyApplyTorque(raw, 12);
            tree.PhysicsFrame(1d / 60);
            Check(right.ForceInput.RotationLocked && rawRuntime.PendingTorque == 0, "Fresh attachment preserves rotation policy and consumes locked torque");
            ForceNear(PhysicsServer.BodyGetAngularVelocity(raw), 0, .001f, "Locked raw rotation remains physically locked");
            Collect(); for (var i = 0; i < 64; i++) Cycle();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Cycle();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Typed default force preparation allocated {owner}/{all} owner/all-thread bytes");
            Console.WriteLine($"{backend}: real selected scene/raw force value inputs, persistent/transient/omitted/locked policy, failure recovery/transfer and 64 warm full frames: {owner}/{all} B.");
            void Cycle() { scene.ApplyCentralForce(new(.01f, 0)); PhysicsServer.BodyApplyCentralForce(raw, new(.01f, 0)); tree.PhysicsFrame(1d / 60); }
        }
        finally { PhysicsServer.FreeRID(raw); }
    }
    private static void ForceNear(float actual, float expected, float tolerance, string message) =>
        Check(float.IsFinite(actual) && MathF.Abs(actual - expected) <= tolerance, $"{message}: {actual} versus {expected} ± {tolerance}");
}
