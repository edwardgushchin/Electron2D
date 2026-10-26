using Electron2D;

public static partial class BackendChecks
{
    public static void RunRigidBodyContract(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (backend is null) PhysicsServerManager.RegisterServer("RigidBodyContract", () => new Solver());
            VerifyRigidBodyStorage();
            VerifyReportedKinematicPath(backend);
            RigidBodyForceTests.Run(backend);
            RigidBodyContactTests.Run(backend);
            RigidFreezeModeTests.Run(backend);
            ShapePairEventTests.Run(backend);
            RunForces(backend, threaded);
            RunBodyParameters(backend, threaded);
            RunBodyStates(backend, threaded);
            RunMassRoles(backend, threaded);
            RunMassReset(backend, threaded);
            RunCCDPolicies(backend, threaded);
            RunIntegration(backend, threaded);
            RunContactResponse(backend, threaded);
            Console.WriteLine($"RigidBody declared contract {backend?.ToString() ?? "registered"}/{threaded}: storage, forces, contacts, fields, modes, mass, CCD and integration passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void VerifyReportedKinematicPath(PhysicsServer.Backend? backend)
    {
        using var world = backend is { } selected ? new World(selected) : new World("RigidBodyContract");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 2 };
        var mover = new RigidBody { Freeze = true, FreezeMode = RigidFreezeMode.Kinematic, CollisionMask = 0, ContactMonitor = true, MaxContactsReported = 2 };
        var wall = new StaticBody { Position = new(25, 0) };
        mover.AddChild(new CollisionShape { Shape = circle }); wall.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(mover); root.AddChild(wall);
        using var tree = new SceneTree(root);
        var entered = 0; var exited = 0;
        mover.BodyEntered += peer => { Check(ReferenceEquals(peer, wall), "Transient path contacts retain their scene peer"); entered++; };
        mover.BodyExited += _ => exited++;
        mover.Position = new(60, 0); tree.PhysicsFrame(1d / 60);
        var state = PhysicsServer.BodyGetDirectState(mover.GetRID())!;
        Check(entered == 1 && mover.GetContactCount() > 0 && state.GetContactImpulse(0) == Vector2.Zero && mover.Position.DistanceTo(new(60, 0)) < .001f,
            "A frozen kinematic path reports an intermediate static contact selected by the peer mask without blocking motion");
        Check(mover.ContinuousCD == CCDMode.Disabled && PhysicsServer.BodyGetContinuousCollisionDetectionMode(wall.GetRID()) == CCDMode.Disabled,
            "Kinematic path handling does not rewrite either body's configured CCD mode");
        tree.PhysicsFrame(1d / 60);
        Check(entered == 1 && exited == 1 && mover.GetContactCount() == 0,
            "An idle frame departs the transient path contact once without replaying it");
        wall.CollisionMask = 0; mover.Position = Vector2.Zero; tree.PhysicsFrame(1d / 60);
        Check(entered == 1 && exited == 1 && mover.GetContactCount() == 0, "Two rejecting masks suppress the reverse path contact");
    }

    private static void VerifyRigidBodyStorage()
    {
        using var body = new RigidBody();
        Check(body.Mass == 1 && body.Inertia == 0 && body.CenterOfMassMode == RigidCenterOfMassMode.Auto && body.CenterOfMass == Vector2.Zero &&
            body.GravityScale == 1 && body.LinearVelocity == Vector2.Zero && body.AngularVelocity == 0 &&
            body.LinearDamp == 0 && body.AngularDamp == 0 && body.LinearDampMode == RigidBody.DampMode.Combine && body.AngularDampMode == RigidBody.DampMode.Combine &&
            !body.Freeze && body.FreezeMode == RigidFreezeMode.Static && !body.LockRotation && body.CanSleep && !body.Sleeping &&
            body.ContinuousCD == CCDMode.Disabled && !body.CustomIntegrator && body.PhysicsMaterialOverride is null &&
            !body.ContactMonitor && body.MaxContactsReported == 0 && body.ConstantForce == Vector2.Zero && body.ConstantTorque == 0,
            "Every declared rigid-body property has the pinned detached default");
        using var material = new PhysicsMaterial { Friction = .4f, Bounce = .2f };
        body.CenterOfMassMode = RigidCenterOfMassMode.Custom; body.CenterOfMass = new(2, -3);
        body.Mass = 2; body.Inertia = 7; body.GravityScale = -.5f;
        body.LinearVelocity = new(3, -4); body.AngularVelocity = .25f;
        body.LinearDamp = .3f; body.AngularDamp = .6f;
        body.LinearDampMode = body.AngularDampMode = RigidBody.DampMode.Replace;
        body.ContinuousCD = CCDMode.CastShape; body.CustomIntegrator = true;
        body.PhysicsMaterialOverride = material; body.ContactMonitor = true; body.MaxContactsReported = 7;
        body.ConstantForce = new(5, 6); body.ConstantTorque = 8;
        body.LockRotation = true; body.CanSleep = false;
        body.FreezeMode = RigidFreezeMode.Kinematic; body.Freeze = true;
        using var packed = new PackedScene(); packed.Pack(body);
        using var copy = (RigidBody)packed.Instantiate();
        Check(copy.GetRID() != body.GetRID() && copy.Mass == 2 && copy.Inertia == 7 &&
            copy.CenterOfMassMode == RigidCenterOfMassMode.Custom && copy.CenterOfMass == new Vector2(2, -3) &&
            copy.GravityScale == -.5f && copy.LinearVelocity == new Vector2(3, -4) && copy.AngularVelocity == .25f &&
            copy.LinearDamp == .3f && copy.AngularDamp == .6f && copy.LinearDampMode == RigidBody.DampMode.Replace && copy.AngularDampMode == RigidBody.DampMode.Replace &&
            copy.Freeze && copy.FreezeMode == RigidFreezeMode.Kinematic && copy.LockRotation && !copy.CanSleep &&
            copy.ContinuousCD == CCDMode.CastShape && copy.CustomIntegrator && ReferenceEquals(copy.PhysicsMaterialOverride, material) &&
            copy.ContactMonitor && copy.MaxContactsReported == 7 && copy.ConstantForce == new Vector2(5, 6) && copy.ConstantTorque == 8,
            "Packing restores the combined declared configuration with independent body identity and borrowed material");
        copy.Freeze = false;
        Check(!copy.Sleeping && !body.IsDisposed && !material.IsDisposed,
            "A packed frozen profile retains its separate dynamic wake request and borrowed resources");
    }
}
