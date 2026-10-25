using Electron2D;

public static partial class BackendChecks
{
    public static void RunConstantWake(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            Solver? solver = null;
            if (backend is null) PhysicsServerManager.RegisterServer("ConstantWake", () => solver = new());
            using var world = backend is { } selected ? new World(selected) : new World("ConstantWake");
            using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 2 };
            RigidBody[] bodies = [new(), new() { Freeze = true }, new PhysicalBone()];
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i]; body.Position = new(i * 100, 0); body.Mass = 2; body.Inertia = 8;
                body.GravityScale = 0; body.CanSleep = false;
                body.LinearDampMode = body.AngularDampMode = RigidBody.DampMode.Replace;
                body.LinearDamp = body.AngularDamp = 0;
                body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
            }
            using var tree = new SceneTree(root); var cases = 0; var callbacks = 0; var frames = 0;
            var owner = Thread.CurrentThread;
            Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
                "Persistent wake retains the selected numerical implementation without fallback");
            using (var frozen = new RigidBody { Sleeping = true, Freeze = true })
            {
                frozen.ConstantForce = new(40, 0); frozen.ConstantTorque = 8; frozen.Freeze = false;
                Check(frozen.Sleeping, "Inactive configuration preserves the stored sleep request until dynamic participation resumes");
            }
            foreach (var mode in new[] { PhysicsServer.BodyMode.Rigid, PhysicsServer.BodyMode.RigidLinear, PhysicsServer.BodyMode.Static, PhysicsServer.BodyMode.Kinematic })
                foreach (var attached in new[] { false, true })
                    foreach (var property in new[] { false, true })
                        foreach (var torque in new[] { false, true })
                            for (var i = 0; i < bodies.Length; i++)
                            {
                                var body = bodies[i]; var id = body.GetRID();
                                PhysicsServer.BodySetSpace(id, attached ? world.Space : default); PhysicsServer.BodySetMode(id, mode);
                                Reset(body); Write(body, property, torque);
                                var dynamic = mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
                                if (dynamic && PhysicsServer.BodyGetSleeping(id))
                                    throw new InvalidOperationException($"Persistent {(torque ? "torque" : "force")} failed to wake {i}/{mode}, attached={attached}, property={property}.");
                                Check(body.ConstantForce == (torque ? Vector2.Zero : new Vector2(40, 0)) && body.ConstantTorque == (torque ? 8 : 0),
                                    "Property and RID authoring share both configured channels");
                                if (attached) { tree.PhysicsFrame(.01); Motion(body, mode, torque); }
                                cases++;
                            }
            foreach (var body in bodies)
            {
                PhysicsServer.BodySetSpace(body.GetRID(), world.Space);
                PhysicsServer.BodySetMode(body.GetRID(), PhysicsServer.BodyMode.Rigid);
                body.ConstantForce = new(40, 0); body.ConstantTorque = 8; body.Sleeping = true; body.ConstantForce = Vector2.Zero;
                Check(body.Sleeping && body.ConstantTorque == 8, "Zero force replacement preserves sleep and the torque channel");
                body.ConstantForce = new(40, 0); body.Sleeping = true; body.ConstantTorque = 0;
                Check(body.Sleeping && body.ConstantForce == new Vector2(40, 0), "Zero torque replacement preserves sleep and the force channel");
                PhysicsServer.BodySetStateSyncCallback(body.GetRID(), _ => { Check(Thread.CurrentThread == owner, "Wake observers remain on the owner"); callbacks++; });
            }
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            for (var i = 0; i < 128; i++) Frame(); Collect();
            var initialCallbacks = callbacks; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && callbacks - initialCallbacks == 384,
                $"Persistent wake frames allocated {own}/{all} B or lost callbacks: {callbacks - initialCallbacks}");
            if (solver is not null)
            {
                foreach (var body in bodies)
                    foreach (var torque in new[] { false, true })
                    {
                        PhysicsServer.BodySetMode(body.GetRID(), PhysicsServer.BodyMode.Rigid); Reset(body);
                        var error = new IOException("Expected persistent wake failure"); var attempts = 0;
                        solver.AwakeHook = id => { if (id == body.GetRID()) { attempts++; throw error; } };
                        Check(ReferenceEquals(CaptureCopy(() => Write(body, true, torque)), error), "Original selected wake error propagates");
                        Check(attempts == 1 && (torque ? body.ConstantTorque == 8 : body.ConstantForce == new Vector2(40, 0)),
                            "Committed persistent input survives the selected wake error");
                        solver.AwakeHook = id => { if (id == body.GetRID()) attempts++; };
                        Check(!PhysicsServer.BodyGetSleeping(body.GetRID()) && attempts == 2, "Numerical access retries the retained wake exactly once");
                        solver.AwakeHook = null;
                    }
            }
            Console.WriteLine($"Persistent wake {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: {cases} scene/RID, attached/detached, four-role force/torque cases; 128 changed complete frames, 384 callbacks and {own}/{all} B passed.");

            void Frame()
            {
                var index = frames++; var mode = (index & 1) == 0 ? PhysicsServer.BodyMode.Rigid : PhysicsServer.BodyMode.RigidLinear;
                var torque = (index & 2) != 0;
                foreach (var body in bodies)
                {
                    PhysicsServer.BodySetMode(body.GetRID(), mode); Reset(body); Write(body, true, torque);
                    PhysicsServer.BodySetSleeping(body.GetRID(), true); Write(body, true, torque);
                    Check(!PhysicsServer.BodyGetSleeping(body.GetRID()), "Equal nonzero property replacement wakes the actual dynamic role");
                }
                tree.PhysicsFrame(.01);
                foreach (var body in bodies)
                {
                    Motion(body, mode, torque); point.Position = PhysicsServer.BodyGetTransform(body.GetRID()).Origin;
                    Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == body.GetRID() && hits[0].ShapeIndex == 0,
                        "Persistent wake retains real motion and logical query identity");
                }
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
    private static void Reset(RigidBody body)
    {
        PhysicsServer.BodySetConstantForce(body.GetRID(), Vector2.Zero); PhysicsServer.BodySetConstantTorque(body.GetRID(), 0);
        PhysicsServer.BodySetLinearVelocity(body.GetRID(), Vector2.Zero); PhysicsServer.BodySetAngularVelocity(body.GetRID(), 0);
        PhysicsServer.BodySetSleeping(body.GetRID(), true);
    }
    private static void Write(RigidBody body, bool property, bool torque)
    {
        if (torque) { if (property) body.ConstantTorque = 8; else PhysicsServer.BodySetConstantTorque(body.GetRID(), 8); }
        else { if (property) body.ConstantForce = new(40, 0); else PhysicsServer.BodySetConstantForce(body.GetRID(), new(40, 0)); }
    }
    private static void Motion(RigidBody body, PhysicsServer.BodyMode mode, bool torque)
    {
        var velocity = PhysicsServer.BodyGetLinearVelocity(body.GetRID()); var angular = PhysicsServer.BodyGetAngularVelocity(body.GetRID());
        var expected = !torque && mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear ? new Vector2(.2f, 0) : Vector2.Zero;
        Check(velocity.DistanceTo(expected) < .0001f && MathF.Abs(angular - (torque && mode == PhysicsServer.BodyMode.Rigid ? .01f : 0)) < .0001f,
            "Persistent force and torque produce the mass/inertia-scaled response in the actual selected role");
    }
}
