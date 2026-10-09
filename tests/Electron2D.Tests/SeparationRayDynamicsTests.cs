using System.Diagnostics;
using Electron2D;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class SeparationRayDynamicsTests
{
    internal static void Run(bool gpu = false)
    {
        VerifyFamilies(gpu); VerifySlope(gpu); VerifyMaterials(gpu); VerifySleepAndAllocation(gpu); VerifyFilters(gpu); VerifyCoupled(gpu); VerifyCCD(gpu); VerifyScene(gpu);
        Console.WriteLine($"Directed ray dynamics passed ({(gpu ? "CPU host/GPU stages" : "CPU")}): solver, materials, mass, reports, sleep and allocation.");
    }
    private sealed class World : IDisposable
    {
        internal readonly RID Space = PhysicsServer.SpaceCreate();
        private readonly List<RID> _bodies = [];
        internal World(bool gpu)
        {
            PhysicsServer.SpaceSetActive(Space, true);
            var space = PhysicsServer.Service.GetSceneSpace(Space);
            space.DefaultAreaFields.Gravity = 0; space.DefaultAreaFields.LinearDamp = space.DefaultAreaFields.AngularDamp = 0;
            if (gpu) space.EnableGPUSolver();
        }
        internal RID Add(Shape shape, Vector2 position, bool dynamic = false, float rotation = 0)
        {
            var body = PhysicsServer.BodyCreate(); _bodies.Add(body);
            PhysicsServer.BodySetMode(body, dynamic ? Mode.Rigid : Mode.Static);
            PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetTransform(body, new(rotation, position));
            PhysicsServer.BodySetMaxContactsReported(body, 8); PhysicsServer.BodySetSpace(body, Space); return body;
        }
        internal void Step(int frames = 1) { for (var i = 0; i < frames; i++) PhysicsServer.SpaceStep(Space, 1d / 60); }
        public void Dispose() { foreach (var body in _bodies) PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(Space); }
    }
    private static void VerifyFamilies(bool gpu)
    {
        var vertices = new Vector2[12]; for (var i = 0; i < vertices.Length; i++) vertices[i] = new Vector2(10, 0).Rotated(i * Mathf.Tau / vertices.Length);
        Shape[] targets = [new RectangleShape { Size = new(100, 10) }, new CircleShape { Radius = 10 }, new CapsuleShape { Radius = 10, Height = 30 },
            new SegmentShape { A = new(-50, 0), B = new(50, 0) }, new ConcavePolygonShape { Segments = [new(-50, 0), new(50, 0)] }, new ConvexPolygonShape { Points = vertices }];
        using var ray = new SeparationRayShape();
        try
        {
            foreach (var shape in targets)
            {
                foreach (var reverse in new[] { false, true })
                {
                    using var world = new World(gpu);
                    var first = world.Add(reverse ? shape : ray, reverse ? new(0, 40) : default, true);
                    world.Add(reverse ? ray : shape, reverse ? default : new(0, 40));
                    PhysicsServer.BodySetLinearVelocity(first, new(0, reverse ? -60 : 60));
                    world.Step(45); using var state = PhysicsServer.BodyGetDirectState(first)!;
                    Check(reverse ? state.Transform.Origin.Y > 19 : state.Transform.Origin.Y < 21, $"Directed response for {shape.GetType().Name}, reversed={reverse}: {state.Transform.Origin}");
                    Check(MathF.Abs(state.LinearVelocity.Y) < 1, "Ray contact removes inward velocity.");
                    Check(state.GetContactCount() > 0 && state.GetContactCollider(0).IsValid(), "Directed contact is reported with collider identity.");
                    if (!reverse) Check(state.InverseInertia == 0, "Ray geometry has no rod inertia.");
                }
            }
            using var world2 = new World(gpu);
            var contained = world2.Add(ray, new(0, 40), true); world2.Add(targets[^1], new(0, 40));
            world2.Step(); using var inside = PhysicsServer.BodyGetDirectState(contained)!;
            Check(inside.GetContactCount() == 0 && inside.Transform.Origin.DistanceTo(new(0, 40)) < .0001f, $"Full compound containment does not create seam contacts: n={inside.GetContactCount()}, pose={inside.Transform.Origin}.");
        }
        finally { foreach (var shape in targets) shape.Dispose(); }
    }
    private static void VerifySlope(bool gpu)
    {
        using var floor = new RectangleShape { Size = new(200, 10) };
        foreach (var slide in new[] { false, true })
        {
            using var ray = new SeparationRayShape { SlideOnSlope = slide }; using var world = new World(gpu);
            var body = world.Add(ray, new(0, 15), true); world.Add(floor, new(0, 40), rotation: .35f);
            PhysicsServer.BodySetLinearVelocity(body, new(0, 60)); world.Step(8);
            using var state = PhysicsServer.BodyGetDirectState(body)!;
            Check(state.GetContactCount() > 0, "Slope has directed contact.");
            var normal = state.GetContactLocalNormal(0);
            Check(slide ? MathF.Abs(normal.X) > .25f : MathF.Abs(normal.X) < .001f, "Slope policy reaches solver normal.");
        }
    }
    private static void VerifyMaterials(bool gpu)
    {
        using var ray = new SeparationRayShape(); using var floor = new RectangleShape { Size = new(400, 10) };
        foreach (var bounce in new[] { 0f, 1f })
        {
            using var world = new World(gpu); var body = world.Add(ray, new(0, 12), true); var target = world.Add(floor, new(0, 40));
            PhysicsServer.BodySetBounce(body, bounce); PhysicsServer.BodySetBounce(target, bounce);
            PhysicsServer.BodySetFriction(body, 1); PhysicsServer.BodySetFriction(target, 1);
            PhysicsServer.BodySetLinearVelocity(body, new(30, 300)); world.Step(3);
            var v = PhysicsServer.BodyGetLinearVelocity(body);
            Check(bounce > 0 ? v.Y < -250 : MathF.Abs(v.Y) < 1 && MathF.Abs(v.X) < 1, $"Ray material impulses: bounce={bounce}, v={v}");
        }
    }
    private static void VerifySleepAndAllocation(bool gpu)
    {
        using var ray = new SeparationRayShape(); using var floor = new RectangleShape { Size = new(200, 10) }; using var world = new World(gpu);
        var body = world.Add(ray, new(0, 14), true); world.Add(floor, new(0, 40));
        PhysicsServer.Service.GetSceneSpace(world.Space).DefaultAreaFields.Gravity = 980;
        world.Step(180); Check(PhysicsServer.BodyGetSleeping(body), "Supported ray body sleeps.");
        ray.Length = 25; world.Step(30); Check(PhysicsServer.BodyGetTransform(body).Origin.Y < 12, "Geometry replacement updates directed support and wakes.");
        PhysicsServer.BodySetCanSleep(body, false); world.Step(128);
        var elapsed = new long[128]; var before = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < elapsed.Length; i++) { var start = Stopwatch.GetTimestamp(); world.Step(); elapsed[i] = Stopwatch.GetTimestamp() - start; }
        var allocated = GC.GetTotalAllocatedBytes(true) - before; Array.Sort(elapsed);
        var ms = 1000d / Stopwatch.Frequency;
        Console.WriteLine($"Directed ray active step: p50/p95/p99 {elapsed[64] * ms:F4}/{elapsed[121] * ms:F4}/{elapsed[126] * ms:F4} ms; {allocated} managed bytes / 128 warmed frames.");
        Check(allocated == 0, "Warmed active directed solver steps allocate zero managed bytes.");
    }
    private static void VerifyFilters(bool gpu)
    {
        using var ray = new SeparationRayShape(); using var floor = new RectangleShape { Size = new(200, 10) };
        for (var variant = 0; variant < 4; variant++)
        {
            using var world = new World(gpu); var body = world.Add(ray, new(0, 14), true); var target = world.Add(variant == 2 ? ray : floor, new(0, 40));
            if (variant == 0) PhysicsServer.BodySetCollisionMask(target, 0);
            if (variant == 1) PhysicsServer.BodyAddCollisionException(body, target);
            if (variant == 3) ray.Length = 0;
            PhysicsServer.BodySetContinuousCollisionDetectionMode(body, CCDMode.CastShape);
            PhysicsServer.BodySetLinearVelocity(body, new(0, 60)); world.Step(45);
            Check(PhysicsServer.BodyGetTransform(body).Origin.Y > 50, "Masks, exceptions, ray pairs and zero length reject response.");
        }
    }
    private static void VerifyCoupled(bool gpu)
    {
        using var ray = new SeparationRayShape(); using var block = new RectangleShape { Size = new(100, 10) }; using var world = new World(gpu);
        var a = world.Add(ray, new(0, 12), true); var b = world.Add(block, new(0, 40), true);
        PhysicsServer.BodySetMass(a, 2); PhysicsServer.BodySetMass(b, 3);
        PhysicsServer.BodySetLinearVelocity(a, new(0, 300));
        using var stateA = PhysicsServer.BodyGetDirectState(a)!; using var stateB = PhysicsServer.BodyGetDirectState(b)!;
        var impulseA = Vector2.Zero; var impulseB = Vector2.Zero;
        for (var i = 0; i < 8; i++)
        {
            world.Step();
            for (var j = 0; j < stateA.GetContactCount(); j++) impulseA += stateA.GetContactImpulse(j);
            for (var j = 0; j < stateB.GetContactCount(); j++) impulseB += stateB.GetContactImpulse(j);
        }
        // 1/60-second steps: momentum is 600 kg*u/s; an inelastic pair shares 120 u/s.
        Check(MathF.Abs(2 * stateA.LinearVelocity.Y + 3 * stateB.LinearVelocity.Y - 600) < .02f, "Directed coupled impulses conserve momentum.");
        Check(MathF.Abs(stateA.LinearVelocity.Y - 120) < 1 && MathF.Abs(stateB.LinearVelocity.Y - 120) < 1, "Configured mass reaches the directed constraint.");
        Check(impulseA.DistanceTo(-impulseB) < .02f && MathF.Abs(impulseA.Y + 360) < 2, $"Reported directed impulse matches momentum transfer: {impulseA}, {impulseB}.");
        PhysicsServer.BodySetInertia(a, 2); PhysicsServer.BodyApplyTorqueImpulse(a, 2);
        Check(MathF.Abs(PhysicsServer.BodyGetAngularVelocity(a) - 1) < .0001f, "Explicit inertia remains available on a ray-only body.");
    }
    private static void VerifyCCD(bool gpu)
    {
        using var ray = new SeparationRayShape(); using var floor = new RectangleShape { Size = new(200, .2f) };
        foreach (var mode in new[] { CCDMode.CastRay, CCDMode.CastShape })
        {
            using var world = new World(gpu); var body = world.Add(ray, default, true); world.Add(floor, new(0, 40));
            PhysicsServer.BodySetLinearVelocity(body, new(0, 6000)); PhysicsServer.BodySetContinuousCollisionDetectionMode(body, mode);
            world.Step();
            Check(PhysicsServer.BodyGetTransform(body).Origin.Y < 21 && MathF.Abs(PhysicsServer.BodyGetLinearVelocity(body).Y) < 1, $"Continuous directed contact stops at its tip ({mode}): {PhysicsServer.BodyGetTransform(body).Origin}, {PhysicsServer.BodyGetLinearVelocity(body)}.");
            Console.WriteLine($"Ray CCD {mode}: {PhysicsServer.BodyGetTransform(body).Origin}, {PhysicsServer.BodyGetLinearVelocity(body)}");
        }
        for (var reverse = 0; reverse < 2; reverse++)
        {
            using var world = new World(gpu); var body = world.Add(ray, new(0, 14), true); var target = world.Add(floor, new(0, 40));
            PhysicsServer.BodySetShapeAsOneWayCollision(target, 0, true, 1, reverse == 0 ? Vector2.Down : Vector2.Up);
            PhysicsServer.BodySetLinearVelocity(body, new(0, 60)); world.Step(40);
            Check(reverse == 0 ? PhysicsServer.BodyGetTransform(body).Origin.Y < 21 : PhysicsServer.BodyGetTransform(body).Origin.Y > 50, "Directed manifold respects one-way contact policy.");
        }
    }
    private static void VerifyScene(bool gpu)
    {
        using var ray = new SeparationRayShape(); using var floor = new RectangleShape { Size = new(200, 10) };
        var root = new Node(); var body = new RigidBody { Name = "RayBody", Position = new(0, 14), ContactMonitor = true, MaxContactsReported = 8 };
        var target = new StaticBody { Name = "Floor", Position = new(0, 40) };
        body.AddChild(new CollisionShape { Shape = ray }); target.AddChild(new CollisionShape { Shape = floor }); root.AddChild(body); root.AddChild(target);
        using var tree = new SceneTree(root); if (gpu) body.Space!.EnableGPUSolver(); var entered = 0; body.BodyEntered += _ => entered++;
        for (var i = 0; i < 60; i++) tree.PhysicsFrame(1d / 60);
        Check(entered == 1 && body.GetContactCount() > 0 && body.Position.Y < 16, "Scene directed body publishes one entry and ordinary contact snapshots.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
