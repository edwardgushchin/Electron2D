using Electron2D;

public static partial class BackendChecks
{
    public static void RunContactResponse(PhysicsServer.Backend? backend = null, bool? threaded = null)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        if (threaded is { } separate) ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        try { RunContactResponseCore(backend); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void RunContactResponseCore(PhysicsServer.Backend? backend)
    {
        if (backend is null) PhysicsServerManager.RegisterServer("ContactResponse", () => new Solver());
        using var world = backend is { } selected ? new World(selected) : new World("ContactResponse");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
            "Contact response uses the requested implementation without fallback");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 2 };
        using var material = new PhysicsMaterial();
        var scene = new RigidBody
        {
            Mass = 2,
            Inertia = 2,
            GravityScale = 0,
            CanSleep = false,
            LockRotation = true,
            MaxContactsReported = 4,
            PhysicsMaterialOverride = material
        };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetMass(raw, 2); PhysicsServer.BodySetInertia(raw, 2);
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodySetMaxContactsReported(raw, 4); PhysicsServer.BodySetSpace(raw, world.Space);
            foreach (var body in new[] { scene.GetRID(), raw })
            {
                PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(body, 0);
                PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(body, 0);
            }
            Bounce(0, false, 0, 0); Bounce(.25f, false, .5f, .75f); Bounce(.8f, false, .8f, 1);
            Bounce(.8f, true, .8f, 0); Bounce(-.2f, false, .7f, .5f);
            var ordinary = Slide(.1f, false, 1); var rough = Slide(.1f, false, -1);
            var sceneRough = Slide(1, true, .1f); var bothRough = Slide(.1f, true, -1);
            Check(rough + 5 < ordinary && MathF.Abs(sceneRough - rough) < .2f && MathF.Abs(bothRough - rough) < .2f,
                "Signed/rough friction selects the stronger coefficient, including two rough surfaces");
            Surface(new(0, -4), 0, new(0, 8), new(0, -4));
            Surface(new(5, 0), 0, new(0, 20), new(5, 0));
            Surface(new(5, 0), 2, new(0, 20), new(9, 0));
            Surface(new(5, 0), -2, new(0, 20), new(1, 0));
            scene.LockRotation = false; var rotating = Slide(1, false, 1);
            Check(rotating > 7.5f && rotating < 8.5f && scene.AngularVelocity > 3.5f && scene.AngularVelocity < 4.5f,
                "Tangential contact impulse includes the dynamic body's lever arm and inertia");
            scene.LockRotation = true;
            circle.Radius = 3; Surface(new(5, 0), 2, new(0, 20), new(11, 0)); circle.Radius = 2;
            PrepareSurface(Vector2.Zero, 0, Vector2.Zero); tree.PhysicsFrame(1d / 1024); scene.Sleeping = true;
            PhysicsServer.BodySetLinearVelocity(raw, new(0, -4));
            Check(!PhysicsServer.BodyGetSleeping(scene.GetRID()), "Changed static surface velocity wakes the touching dynamic body immediately");
            PrepareSurface(new(5, 0), 2, new(0, 20));
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            tree.PhysicsFrame(1d / 1024); var expected = scene.LinearVelocity;
            var point = ContactImpulse();
            checkpoint.Restore(); tree.PhysicsFrame(1d / 1024);
            Check(scene.LinearVelocity.DistanceTo(expected) < .001f && ContactImpulse().DistanceTo(point) < .01f,
                "Private replay restores actual material/surface contact response and complete impulse");
            for (var i = 0; i < 128; i++) Cycle();
            Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Cycle();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Material/surface edited full frames and replay allocated {own}/{all} B");
            Console.WriteLine($"Contact response on {backend?.ToString() ?? "registered"}: signed materials, normal/tangent/angular surfaces, inertia, full impulses and 128 edited replay cycles {own}/{all} B passed.");
            void Cycle()
            {
                checkpoint.Restore(); material.Friction = .8f; PhysicsServer.BodySetFriction(raw, 1);
                PhysicsServer.BodySetAngularVelocity(raw, -2); tree.PhysicsFrame(1d / 1024);
                Check(scene.LinearVelocity.X > .8f && scene.LinearVelocity.X < 1.2f, "Measured edited contact transfers the actual surface velocity");
                checkpoint.Capture(); checkpoint.Restore();
                PrepareSurface(new(5, 0), 2, new(0, 20)); checkpoint.Capture();
            }
            void Bounce(float first, bool absorbent, float second, float coefficient)
            {
                material.Friction = 0; material.Bounce = first; material.Absorbent = absorbent; material.Rough = false;
                PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetFriction(raw, 0);
                PhysicsServer.BodySetBounce(raw, second); Reset(new(-1.99f, 0), new(1.99f, 0), new(30, 0), new(-30, 0));
                tree.PhysicsFrame(1d / 120);
                Check(MathF.Abs(scene.LinearVelocity.X + 30 * coefficient) < .5f &&
                    MathF.Abs(PhysicsServer.BodyGetLinearVelocity(raw).X - 30 * coefficient) < .5f,
                    $"Actual signed bounce mix {first}/{absorbent}/{second} gives {coefficient}: {scene.LinearVelocity}/{PhysicsServer.BodyGetLinearVelocity(raw)}");
                Check((scene.LinearVelocity + PhysicsServer.BodyGetLinearVelocity(raw)).Length() < .01f, "Equal-mass impact conserves momentum");
            }
            float Slide(float first, bool rough, float second)
            {
                material.Friction = first; material.Rough = rough; material.Bounce = 0; material.Absorbent = false;
                PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetFriction(raw, second);
                PhysicsServer.BodySetBounce(raw, 0); Reset(Vector2.Zero, new(0, 4), new(10, 20), Vector2.Zero);
                tree.PhysicsFrame(1d / 1024); VerifyImpulse(new(10, 20)); return scene.LinearVelocity.X;
            }
            void PrepareSurface(Vector2 linear, float angular, Vector2 velocity)
            {
                material.Friction = 1; material.Rough = false; material.Bounce = 0; material.Absorbent = false;
                PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetFriction(raw, 1);
                PhysicsServer.BodySetBounce(raw, 0); Reset(Vector2.Zero, new(0, circle.Radius * 2), velocity, linear);
                PhysicsServer.BodySetAngularVelocity(raw, angular);
            }
            void Surface(Vector2 linear, float angular, Vector2 velocity, Vector2 expectedVelocity)
            {
                PrepareSurface(linear, angular, velocity); var pose = PhysicsServer.BodyGetTransform(raw);
                tree.PhysicsFrame(1d / 1024);
                Check(scene.LinearVelocity.DistanceTo(expectedVelocity) < .2f,
                    $"Actual normal/tangent/angular surface response {linear}/{angular}: {scene.LinearVelocity}, expected {expectedVelocity}");
                Check(PhysicsServer.BodyGetTransform(raw) == pose, "Virtual surface motion leaves the static pose fixed"); VerifyImpulse(velocity);
            }
            void Reset(Vector2 first, Vector2 second, Vector2 firstVelocity, Vector2 secondVelocity)
            {
                PhysicsServer.BodySetTransform(scene.GetRID(), new(0, first)); PhysicsServer.BodySetTransform(raw, new(0, second));
                PhysicsServer.BodySetLinearVelocity(scene.GetRID(), firstVelocity); PhysicsServer.BodySetLinearVelocity(raw, secondVelocity);
                PhysicsServer.BodySetAngularVelocity(scene.GetRID(), 0); PhysicsServer.BodySetAngularVelocity(raw, 0);
            }
            Vector2 ContactImpulse()
            {
                using var state = PhysicsServer.BodyGetDirectState(scene.GetRID())!;
                Check(state.GetContactCount() == 1 && state.GetContactCollider(0) == raw, "The solved contact retains the raw peer identity");
                return state.GetContactImpulse(0);
            }
            void VerifyImpulse(Vector2 before) => Check(ContactImpulse().DistanceTo((scene.LinearVelocity - before) * 2) < .1f,
                "Reported normal plus tangent impulse equals the actual full-step momentum change");
        }
        finally { PhysicsServer.FreeRID(raw); }
        InheritedSurface(backend);
        VerifyMaterialCopy(backend);
    }

    private static void InheritedSurface(PhysicsServer.Backend? backend)
    {
        using var world = backend is { } selected ? new World(selected) : new World("ContactResponse");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 2 };
        var floor = new AnimatableBody { Position = new(0, 4), ConstantLinearVelocity = new(5, 0), ConstantAngularVelocity = 2 };
        var body = new RigidBody
        {
            Mass = 2,
            Inertia = 2,
            GravityScale = 0,
            CanSleep = false,
            LockRotation = true,
            LinearVelocity = new(0, 20),
            LinearDampMode = RigidBody.DampMode.Replace,
            LinearDamp = 0
        };
        floor.AddChild(new CollisionShape { Shape = circle }); body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(floor); root.AddChild(body); using var tree = new SceneTree(root);
        var pose = floor.GlobalTransform; tree.PhysicsFrame(1d / 1024);
        Check(body.LinearVelocity.DistanceTo(new(9, 0)) < .2f && floor.GlobalTransform == pose,
            "AnimatableBody inherits virtual surface motion without integrating it into the target pose");
    }
}
