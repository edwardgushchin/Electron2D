using Electron2D;

public static partial class BackendChecks
{
    public static void RunContactCoupling(PhysicsServer.Backend backend, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            using var world = new World(backend); var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
            using var boundary = new WorldBoundaryShape { Normal = Vector2.Up };
            using var shape = new SeparationRayShape { Length = 10, SlideOnSlope = true };
            var floor = new StaticBody { Position = new(1300, 100), CollisionLayer = 4, CollisionMask = 4 };
            floor.AddChild(new CollisionShape { Shape = boundary }); root.AddChild(floor);
            var body = new RigidBody
            {
                Mass = 1,
                Inertia = 2,
                LinearDamp = .2f,
                AngularDamp = 4,
                CollisionLayer = 4,
                CollisionMask = 4,
                MaxContactsReported = 8,
                ContactMonitor = true,
                ContinuousCD = CCDMode.CastShape
            };
            body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
            PhysicsServer.AreaSetGravity(world.Space, 980); PhysicsServer.AreaSetLinearDamp(world.Space, .1f); PhysicsServer.AreaSetAngularDamp(world.Space, 1);
            PhysicsServer.SpaceSetSolverIterations(world.Space, 16);
            PhysicsServer.SpaceSetContactMaxAllowedPenetration(world.Space, .3f);
            var rid = body.GetRID();
            for (var i = 0; i < 128; i++) Frame();
            Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            var state = PhysicsServer.BodyGetDirectState(rid)!; var impulse = Vector2.Zero;
            for (var i = 0; i < state.GetContactCount(); i++)
            {
                Check(state.GetContactCollider(i) == floor.GetRID() && ReferenceEquals(state.GetContactColliderObject(i), floor), "Rotating CCD contacts retain the actual plane identity");
                impulse += state.GetContactImpulse(i);
            }
            var free = new Vector2(84.94213f, 46.233803f) * (1 - .3f / 60) + new Vector2(0, 980f / 60);
            Check(own == 0 && all == 0 && body.GetRID() == rid && body.GetWorld() == world && state.GetContactCount() > 0 &&
                body.Position.Y + 10 * MathF.Cos(body.Rotation) <= 100.31f && body.AngularVelocity > .1f &&
                (body.LinearVelocity - free - impulse).Length() < .002f,
                $"Oblique ray CCD retains complete motion, contact momentum and zero warmed allocations: {own}/{all} B");
            Console.WriteLine($"Contact coupling {backend}, threaded={threaded}: 128 warm/128 reset full frames, 0/0 B, stable RID and physical impulse.");
            void Frame()
            {
                body.Position = new(1702.198f, 91.44969f); body.Rotation = .5388814f;
                body.LinearVelocity = new(84.94213f, 46.233803f); body.AngularVelocity = 9.433882f;
                tree.PhysicsFrame(1d / 60);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
