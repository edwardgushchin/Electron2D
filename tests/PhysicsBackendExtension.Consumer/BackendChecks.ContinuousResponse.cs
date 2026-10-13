using Electron2D;

public static partial class BackendChecks
{
    public static void RunContinuousResponse(PhysicsServer.Backend? backend = null)
    {
        if (backend is null) PhysicsServerManager.RegisterServer("ContinuousResponse", () => new Solver());
        using var world = backend is { } selected ? new World(selected) : new World("ContinuousResponse");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
            "Numerical CCD uses the requested implementation without fallback");
        using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 1 };
        var scene = new RigidBody
        {
            Mass = 2,
            Inertia = 2,
            GravityScale = 0,
            CanSleep = false,
            MaxContactsReported = 4,
            LinearDamp = 0,
            LinearDampMode = RigidBody.DampMode.Replace
        };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene); using var tree = new SceneTree(root);
        var target = PhysicsServer.BodyCreate(); var control = PhysicsServer.BodyCreate(); var callbacks = 0;
        try
        {
            foreach (var body in new[] { scene.GetRID(), target, control })
            {
                if (body != scene.GetRID()) { PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetMass(body, 2); PhysicsServer.BodySetInertia(body, 2); }
                PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(body, 0);
                PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(body, 0);
                PhysicsServer.BodySetFriction(body, 0); PhysicsServer.BodySetMaxContactsReported(body, 4);
                if (body != scene.GetRID()) PhysicsServer.BodySetSpace(body, world.Space);
            }
            PhysicsServer.BodySetCollisionLayer(control, 0); PhysicsServer.BodySetCollisionMask(control, 0);
            PhysicsServer.BodySetConstantForce(control, new(6, 0)); PhysicsServer.BodySetStateSyncCallback(control, _ => callbacks++);
            Setup(CCDMode.Disabled); Step(); Check(scene.Position.X > 50, "Disabled stays discrete and does not silently enable global CCD");
            foreach (var mode in new[] { CCDMode.CastRay, CCDMode.CastShape })
            {
                Setup(mode); Step(); Check(scene.Position.X < 20.1f && MathF.Abs(scene.LinearVelocity.X) < 5,
                    $"{mode} prevents actual circle tunneling: {scene.Position}/{scene.LinearVelocity}");
                Check(Impulse().DistanceTo(new(-6000, 0)) < 10, "Continuous contact reports the complete stopping impulse");
                Setup(mode); PhysicsServer.BodySetTransform(target, new(0, new Vector2(20, 1.99f)));
                Step(); Check(mode == CCDMode.CastShape ? scene.LinearVelocity.Y < -10 : scene.Position.X > 50 && MathF.Abs(scene.LinearVelocity.Y) < .01f,
                    $"Grazing motion preserves leading-point/full-shape distinction: {mode}/{scene.Position}/{scene.LinearVelocity}");
                Setup(mode); PhysicsServer.BodySetMode(target, PhysicsServer.BodyMode.Rigid);
                PhysicsServer.BodySetTransform(target, new(0, new Vector2(60, 0))); PhysicsServer.BodySetLinearVelocity(target, new(-3000, 0));
                Step(); Check(scene.Position.X < PhysicsServer.BodyGetTransform(target).Origin.X &&
                    (scene.LinearVelocity + PhysicsServer.BodyGetLinearVelocity(target)).Length() < .1f,
                    "Relative moving-body CCD preserves separation and equal-mass momentum without changing the peer mode");
                Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(target) == CCDMode.Disabled, "The moving peer keeps its discrete configuration");
                Setup(mode); PhysicsServer.BodySetBounce(scene.GetRID(), .5f); PhysicsServer.BodySetBounce(target, .5f);
                Step(); Check(scene.Position.X < -15 && MathF.Abs(scene.LinearVelocity.X + 3000) < 2,
                    "Elastic CCD impact preserves speed and consumes the remaining outer interval");
                Check(Impulse().DistanceTo(new(-12000, 0)) < 10, "Transient separating impact retains its complete full-frame impulse");
                Setup(mode); PhysicsServer.BodySetCollisionMask(scene.GetRID(), 0); PhysicsServer.BodySetCollisionMask(target, 0);
                Step(); Check(scene.Position.X > 50, "CCD preserves directional mask filtering");
                Setup(mode); PhysicsServer.BodyAddCollisionException(scene.GetRID(), target);
                Step(); Check(scene.Position.X > 50, "CCD preserves explicit collision exceptions");
                PhysicsServer.BodyRemoveCollisionException(scene.GetRID(), target);
            }
            Setup(CCDMode.CastShape); PhysicsServer.BodySetBounce(scene.GetRID(), .5f); PhysicsServer.BodySetBounce(target, .5f);
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            Step(); var pose = scene.GlobalTransform; var velocity = scene.LinearVelocity; var impulse = Impulse();
            checkpoint.Restore(); Step(); Check(scene.GlobalTransform.Origin.DistanceTo(pose.Origin) < .001f &&
                scene.LinearVelocity.DistanceTo(velocity) < .001f && Impulse().DistanceTo(impulse) < .01f,
                "Private replay restores continuous impact, remaining-time motion and full contact impulse");
            for (var i = 0; i < 128; i++) Cycle(); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Cycle();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Numerical CCD full-frame/replay cycles allocated {own}/{all} B");
            Console.WriteLine($"Numerical CCD on {backend?.ToString() ?? "registered"}: discrete/ray/shape, relative motion, elastic remaining time, full impulse, filters, force/callback budgets and 128 replay cycles {own}/{all} B passed.");
            void Cycle() { checkpoint.Restore(); Step(); Check(scene.Position.X < -15, "Measured CCD really resolves an impact and remaining motion"); checkpoint.Capture(); checkpoint.Restore(); Setup(CCDMode.CastShape); PhysicsServer.BodySetBounce(scene.GetRID(), .5f); PhysicsServer.BodySetBounce(target, .5f); checkpoint.Capture(); }
            void Setup(CCDMode mode)
            {
                PhysicsServer.BodySetMode(target, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetContinuousCollisionDetectionMode(target, CCDMode.Disabled);
                PhysicsServer.BodySetContinuousCollisionDetectionMode(scene.GetRID(), mode);
                PhysicsServer.BodySetBounce(scene.GetRID(), 0); PhysicsServer.BodySetBounce(target, 0);
                PhysicsServer.BodySetCollisionMask(scene.GetRID(), 1); PhysicsServer.BodySetCollisionMask(target, 1);
                PhysicsServer.BodySetTransform(scene.GetRID(), Transform.Identity); PhysicsServer.BodySetLinearVelocity(scene.GetRID(), new(3000, 0));
                PhysicsServer.BodySetTransform(target, new(0, new Vector2(20, 0))); PhysicsServer.BodySetLinearVelocity(target, Vector2.Zero);
                PhysicsServer.BodySetTransform(control, new(0, new Vector2(0, 1000))); PhysicsServer.BodySetLinearVelocity(control, Vector2.Zero);
            }
            void Step()
            {
                var before = callbacks; var tick = PhysicsServer.SpaceGetTick(world.Space); tree.PhysicsFrame(.02);
                Check(callbacks == before + 1 && PhysicsServer.SpaceGetTick(world.Space) == tick + 1,
                    "CCD internal intervals preserve one outer callback and public tick");
                Check(MathF.Abs(PhysicsServer.BodyGetLinearVelocity(control).X - .06f) < .001f,
                    "An unrelated mass-2 body receives force 6 for exactly the complete .02-second interval");
            }
            Vector2 Impulse()
            {
                using var state = PhysicsServer.BodyGetDirectState(scene.GetRID())!;
                Check(state.GetContactCount() == 1 && state.GetContactCollider(0) == target, "Continuous frame retains the transient contact and logical peer");
                return state.GetContactImpulse(0);
            }
        }
        finally { PhysicsServer.FreeRID(control); PhysicsServer.FreeRID(target); }
        if (backend is null) { ContinuousAnnulus(); ContinuousFailure(); }
    }

    private static void ContinuousAnnulus()
    {
        var space = PhysicsServer.SpaceCreate("ContinuousResponse"); using var circle = new CircleShape { Radius = 1 };
        PhysicsServer.SpaceSetActive(space, true);
        using var ring = new CustomShape(Annulus.Create(5, 10));
        var mover = PhysicsServer.BodyCreate(); var target = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(mover, circle.GetRID()); PhysicsServer.BodyAddShape(target, ring.GetRID());
            PhysicsServer.BodySetMode(target, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetGravityScale(mover, 0);
            PhysicsServer.BodySetLinearDampMode(mover, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(mover, 0);
            PhysicsServer.BodySetFriction(mover, 0); PhysicsServer.BodySetFriction(target, 0);
            PhysicsServer.BodySetMaxContactsReported(mover, 4);
            PhysicsServer.BodySetSpace(mover, space); PhysicsServer.BodySetSpace(target, space);
            foreach (var mode in new[] { CCDMode.CastRay, CCDMode.CastShape })
            {
                PhysicsServer.BodySetContinuousCollisionDetectionMode(mover, mode);
                PhysicsServer.BodySetTransform(mover, Transform.Identity); PhysicsServer.BodySetLinearVelocity(mover, new(3000, 0));
                PhysicsServer.BodySetBounce(mover, 0); PhysicsServer.SpaceStep(space, .02);
                Check(PhysicsServer.BodyGetTransform(mover).Origin.X <= 4.001f && MathF.Abs(PhysicsServer.BodyGetLinearVelocity(mover).X) < .01f,
                    $"Analytic custom CCD reaches the ring's inner boundary without replacing the hole by a solid disk: {PhysicsServer.BodyGetTransform(mover).Origin}/{PhysicsServer.BodyGetLinearVelocity(mover)}");
                PhysicsServer.BodySetTransform(mover, Transform.Identity); PhysicsServer.BodySetLinearVelocity(mover, new(3000, 0));
                PhysicsServer.BodySetBounce(mover, 1); PhysicsServer.SpaceStep(space, .02);
                var pose = PhysicsServer.BodyGetTransform(mover); var velocity = PhysicsServer.BodyGetLinearVelocity(mover);
                using var state = PhysicsServer.BodyGetDirectState(mover)!;
                Check(MathF.Abs(pose.Origin.X) <= 4.001f && MathF.Abs(MathF.Abs(velocity.X) - 3000) < .01f &&
                    state.GetContactCount() == 1 && state.GetContactImpulse(0).DistanceTo(velocity - new Vector2(3000, 0)) < .01f,
                    "Repeated opposite inner-wall impacts consume time and sum signed impulses over the full step");
            }
        }
        finally { PhysicsServer.FreeRID(target); PhysicsServer.FreeRID(mover); PhysicsServer.FreeRID(space); }
    }

    private static void ContinuousFailure()
    {
        var space = PhysicsServer.SpaceCreate("ContinuousResponse"); using var circle = new CircleShape { Radius = 1 };
        PhysicsServer.SpaceSetActive(space, true);
        var mover = PhysicsServer.BodyCreate(); var target = PhysicsServer.BodyCreate(); var published = 0;
        try
        {
            PhysicsServer.BodyAddShape(mover, circle.GetRID()); PhysicsServer.BodyAddShape(target, circle.GetRID());
            PhysicsServer.BodySetMode(target, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetTransform(target, new(0, new Vector2(1_000_000, 0)));
            PhysicsServer.BodySetGravityScale(mover, 0); PhysicsServer.BodySetLinearVelocity(mover, new(100_000_000, 0));
            PhysicsServer.BodySetContinuousCollisionDetectionMode(mover, CCDMode.CastShape);
            PhysicsServer.BodySetStateSyncCallback(mover, _ => published++);
            PhysicsServer.BodySetSpace(mover, space); PhysicsServer.BodySetSpace(target, space);
            try { PhysicsServer.SpaceStep(space, .02); throw new Exception("Expected exhausted CCD budget."); }
            catch (AggregateException error)
            {
                Check(error.Flatten().InnerExceptions.Any(e => e is InvalidOperationException && e.Message.Contains("interval budget", StringComparison.Ordinal)),
                    "The failing control reaches the numerical interval limit rather than an authoring guard");
            }
            Check(published == 0 && PhysicsServer.SpaceGetBackend(space) == PhysicsServer.Backend.Extension &&
                PhysicsServer.SpaceGetBackendFallbackReason(space) is null,
                "An exhausted numerical CCD budget publishes no result and performs no hidden backend replay");
            Reject<InvalidOperationException>(() => PhysicsServer.BodyGetTransform(mover));
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(space, .02));
        }
        finally { PhysicsServer.FreeRID(target); PhysicsServer.FreeRID(mover); PhysicsServer.FreeRID(space); }
    }
}
