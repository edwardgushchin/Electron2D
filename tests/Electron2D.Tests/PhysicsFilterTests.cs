using Electron2D;

internal static class PhysicsFilterTests
{
    internal static void Run(bool gpu = false)
    {
        var backend = gpu ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
        MeasureRaw(backend); SceneAndServer(backend); ContactTransitions(backend); MeasureActive(backend); OneSidedResponse(backend); ContinuousResponse(backend); SensorTransitions(backend); WakeNeighbors(backend); Guards(backend); Callbacks(backend);
        Console.WriteLine($"Physics filters {backend} passed: scene/server access, all bits, queries, motion, contact/sensor transitions, lifetime, guards and zero warmed allocations.");
    }
    private static void MeasureRaw(PhysicsServer.Backend backend)
    {
        using var shape = new CircleShape { Radius = 10 }; var space = PhysicsServer.SpaceCreate(backend); var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetSpace(body, space);
            using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1]; var direct = PhysicsServer.SpaceGetDirectState(space);
            for (var i = 0; i < 96; i++) Edit(i);
            Collect(); for (var i = 0; i < 96; i++) Edit(i);
            var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 128; i++) Edit(i);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Warmed raw filter edits allocated {owner}/{all} bytes");
            Console.WriteLine($"Filter changes {backend}: {owner}/{all} owner/all B for 128 layer+mask changes, point queries and full steps");
            void Edit(int i)
            {
                var layer = (i & 1) == 0 ? 1u : 0x80000000u;
                PhysicsServer.BodySetCollisionLayer(body, layer); PhysicsServer.BodySetCollisionMask(body, ~layer);
                query.CollisionMask = layer; Check(direct.IntersectPoint(query, hits) == 1, "Changed category is queryable immediately");
                PhysicsServer.SpaceStep(space, 1d / 60);
            }
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
    }
    private static void SceneAndServer(PhysicsServer.Backend backend)
    {
        using var shape = new CircleShape { Radius = 10 }; using var world = new World(backend); using var root = new SubViewport { World = world };
        PhysicsBody[] bodies = [new RigidBody { GravityScale = 0 }, new StaticBody(), new AnimatableBody(), new CharacterBody()];
        for (var i = 0; i < bodies.Length; i++)
        {
            var body = bodies[i]; body.Name = "Body" + i; body.Position = new(i * 50, 0); body.AddChild(new CollisionShape { Shape = shape });
            var rid = body.GetRID(); Check(PhysicsServer.BodyGetCollisionLayer(rid) == 1 && PhysicsServer.BodyGetCollisionMask(rid) == 1, "Detached defaults");
            PhysicsServer.BodySetCollisionLayer(rid, uint.MaxValue); PhysicsServer.BodySetCollisionMask(rid, 0);
            Check(body.CollisionLayer == uint.MaxValue && body.CollisionMask == 0, "Server edits project into detached scene authoring"); root.AddChild(body);
        }
        using var tree = new SceneTree(root); var direct = world.DirectSpaceState; using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[8];
        foreach (var body in bodies)
        {
            var rid = body.GetRID(); var geometry = PhysicsServer.BodyGetShape(rid, 0); var owner = body.ShapeFindOwner(0);
            query.Position = body.Position;
            foreach (var bits in new[] { 0u, 1u, 0x80000000u, uint.MaxValue })
            {
                PhysicsServer.BodySetCollisionLayer(rid, bits); PhysicsServer.BodySetCollisionMask(rid, ~bits);
                Check(PhysicsServer.BodyGetCollisionLayer(rid) == bits && PhysicsServer.BodyGetCollisionMask(rid) == ~bits && body.CollisionLayer == bits && body.CollisionMask == ~bits, "Scene and server filter views agree");
                query.CollisionMask = 0x80000000u; Check(direct.IntersectPoint(query, hits) == ((bits & query.CollisionMask) == 0 ? 0 : 1), "Immediate point filtering includes bit 32 and zero");
                Check(PhysicsServer.BodyGetShape(rid, 0) == geometry && body.ShapeFindOwner(0) == owner, "Filter edits preserve logical geometry and shape owner");
            }
            body.CollisionLayer = 8; body.CollisionMask = 4;
            Check(PhysicsServer.BodyGetCollisionLayer(rid) == 8 && PhysicsServer.BodyGetCollisionMask(rid) == 4, "Node edits project into server reads");
            root.RemoveChild(body); Check(PhysicsServer.BodyGetCollisionLayer(rid) == 8 && PhysicsServer.BodyGetCollisionMask(rid) == 4, "Tree exit preserves filter authoring"); root.AddChild(body);
        }
        var rigid = (RigidBody)bodies[0]; rigid.Sleeping = true;
        PhysicsServer.BodySetCollisionLayer(rigid.GetRID(), rigid.CollisionLayer); PhysicsServer.BodySetCollisionMask(rigid.GetRID(), rigid.CollisionMask);
        tree.PhysicsFrame(1d / 60); Check(!rigid.Sleeping, "Filter assignments retain the wake behavior even for unchanged bits");
        rigid.Sleeping = true;
        PhysicsServer.BodySetCollisionMask(rigid.GetRID(), rigid.CollisionMask | 16); tree.PhysicsFrame(1d / 60); Check(!rigid.Sleeping, "Changed filter wakes the body");
        rigid.ProcessMode = ProcessMode.Disabled; Check(PhysicsServer.BodyGetCollisionLayer(rigid.GetRID()) == 8, "Disabled scene body retains readable metadata");
        PhysicsServer.BodySetCollisionLayer(rigid.GetRID(), 32); rigid.ProcessMode = ProcessMode.Inherit;
        query.Position = rigid.Position; query.CollisionMask = 32; Check(direct.IntersectPoint(query, hits) == 1, "Reenabled body uses edits made while detached from physics");
        for (var i = 0; i < 96; i++) Edit(i);
        Collect(); for (var i = 0; i < 96; i++) Edit(i);
        var all = GC.GetTotalAllocatedBytes(true); var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) Edit(i);
        before = GC.GetAllocatedBytesForCurrentThread() - before; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(before == 0 && all == 0, $"Warmed scene filter edits allocated {before}/{all} bytes");
        Console.WriteLine($"Scene filters {backend}: {before}/{all} owner/all B for 128 changed-filter frames on four body roles");
        void Edit(int frame)
        {
            var bits = (frame & 1) == 0 ? 1u : 0x80000000u;
            foreach (var body in bodies)
            {
                var rid = body.GetRID(); PhysicsServer.BodySetCollisionLayer(rid, bits); PhysicsServer.BodySetCollisionMask(rid, 0);
                Check(PhysicsServer.BodyGetCollisionLayer(rid) == bits && PhysicsServer.BodyGetCollisionMask(rid) == 0, "Warmed metadata reads");
                query.Position = body.Position; query.CollisionMask = bits; Check(direct.IntersectPoint(query, hits) == 1, "Warmed scene query");
            }
            tree.PhysicsFrame(1d / 60);
        }
    }
    private static void ContactTransitions(PhysicsServer.Backend backend)
    {
        using var shape = new CircleShape { Radius = 10 }; using var world = new World(backend); using var root = new SubViewport { World = world };
        var body = new RigidBody { Name = "Body", GravityScale = 0, CanSleep = false, CollisionLayer = 2, CollisionMask = 0, ContactMonitor = true, MaxContactsReported = 8 };
        body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body); var target = new Entity { Name = "Target" }; root.AddChild(target);
        using var tree = new SceneTree(root); var wall = PhysicsServer.BodyCreate(); var enter = 0; var exit = 0;
        body.BodyEntered += other => { Check(other == target, "Contact owner"); enter++; }; body.BodyExited += other => { Check(other == target, "Departure owner"); exit++; };
        try
        {
            PhysicsServer.BodySetMode(wall, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetTransform(wall, new(0, new(18, 0)));
            PhysicsServer.BodyAddShape(wall, shape.GetRID()); PhysicsServer.BodyAttachObject(wall, target); PhysicsServer.BodySetCollisionLayer(wall, 0x80000000u); PhysicsServer.BodySetCollisionMask(wall, 0); PhysicsServer.BodySetSpace(wall, world.Space);
            tree.PhysicsFrame(1d / 60); Check(enter == 0, "Both masks reject contact");
            using var parameters = new PhysicsTestMotionParameters { From = new(0, new(-30, 0)), Motion = new(60, 0) }; using var result = new PhysicsTestMotionResult();
            Check(!PhysicsServer.BodyTestMotion(body.GetRID(), parameters, result), "Motion respects rejecting masks");
            PhysicsServer.BodySetCollisionMask(body.GetRID(), 0x80000000u);
            Check(PhysicsServer.BodyTestMotion(body.GetRID(), parameters, result) && result.GetColliderRID() == wall, "Motion sees a changed mask before the next step");
            tree.PhysicsFrame(1d / 60); Check(enter == 1 && body.GetCollidingBodies().Contains(target), "Mask enables actual solved contact");
            PhysicsServer.BodySetCollisionMask(body.GetRID(), 0x80000000u); PhysicsServer.BodySetCollisionLayer(wall, 0x80000000u); tree.PhysicsFrame(1d / 60);
            Check(enter == 1 && exit == 0, "Equal writes do not replay contact transitions");
            PhysicsServer.BodySetCollisionMask(body.GetRID(), 0); tree.PhysicsFrame(1d / 60); Check(exit == 1 && body.GetCollidingBodies().Length == 0, "Mask removes actual contact and publishes one departure");
            body.Position = default; PhysicsServer.BodySetCollisionMask(wall, 2);
            Check(!PhysicsServer.BodyTestMotion(body.GetRID(), parameters, result), "The target mask cannot enable motion queries"); tree.PhysicsFrame(1d / 60); Check(enter == 2, "The other body's matching mask also enables contact");
        }
        finally { PhysicsServer.FreeRID(wall); }
    }
    private static void MeasureActive(PhysicsServer.Backend backend)
    {
        using var shape = new CircleShape { Radius = 10 }; using var world = new World(backend); using var root = new SubViewport { World = world };
        var body = new RigidBody { Name = "Body", GravityScale = 0, CanSleep = false, CollisionLayer = 2, CollisionMask = 0, ContactMonitor = true, MaxContactsReported = 8 };
        var wall = new StaticBody { Name = "Wall", Position = new(18, 0), CollisionLayer = 0x80000000u, CollisionMask = 0 };
        var sensor = new Area { Name = "Sensor", CollisionMask = 2 };
        foreach (var collider in new CollisionObject[] { body, wall, sensor }) { collider.AddChild(new CollisionShape { Shape = shape }); root.AddChild(collider); }
        using var tree = new SceneTree(root); var enters = 0; var exits = 0;
        body.BodyEntered += _ => enters++; body.BodyExited += _ => exits++;
        for (var i = 0; i < 96; i++) Edit(i);
        Collect(); for (var i = 0; i < 96; i++) Edit(i);
        var startEnters = enters; var startExits = exits;
        var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) Edit(i);
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(enters - startEnters == 64 && exits - startExits == 64, "Changed filters publish every real contact entry and departure once");
        Check(owner == 0 && all == 0, $"Warmed active contact/filter cycles allocated {owner}/{all} bytes");
        Console.WriteLine($"Active filters {backend}: {owner}/{all} owner/all B for 128 changed-contact frames with sensor membership");
        void Edit(int frame)
        {
            body.Position = default; body.LinearVelocity = default; body.AngularVelocity = 0;
            PhysicsServer.BodySetCollisionMask(body.GetRID(), (frame & 1) == 0 ? 0x80000000u : 0);
            tree.PhysicsFrame(1d / 60);
            Check(sensor.OverlapsBody(body), "Contact filter mutation preserves independent sensor membership");
        }
    }
    private static void OneSidedResponse(PhysicsServer.Backend backend)
    {
        using var shape = new RectangleShape { Size = new(20, 20) };
        foreach (var reverse in new[] { false, true })
            foreach (var passiveMoves in new[] { false, true })
                foreach (var withJoint in new[] { false, true })
                {
                    using var world = new World(backend); using var root = new SubViewport { World = world };
                    var passive = MakeBody("Passive", new(0, 0), 8, 0);
                    var active = MakeBody("Active", new(18, 7), 0, 8);
                    passive.LinearVelocity = passiveMoves ? new(30, 0) : default;
                    active.LinearVelocity = passiveMoves ? default : new(-30, 0);
                    var initialVelocity = passive.LinearVelocity;
                    if (reverse) { root.AddChild(active); root.AddChild(passive); } else { root.AddChild(passive); root.AddChild(active); }
                    if (withJoint) root.AddChild(new PinJoint { Name = "Pin", NodeA = "../Active", Position = active.Position });
                    using var tree = new SceneTree(root);
                    for (var i = 0; i < 4; i++) tree.PhysicsFrame(1d / 60);
                    Check((passive.LinearVelocity - initialVelocity).Length() < .001f && MathF.Abs(passive.AngularVelocity) < .001f,
                        $"{backend}, joint={withJoint}: rejected direction receives no linear/angular impulse: {passive.LinearVelocity}, {passive.AngularVelocity}");
                    Check((passive.Position - initialVelocity * (4f / 60)).Length() < .01f, "Passive endpoint receives no positional correction");
                    Check(active.LinearVelocity.X > (passiveMoves ? 1 : -25), $"{backend}: matching mask receives the collision impulse: {active.LinearVelocity}");
                    Check(PhysicsServer.BodyGetMass(passive.GetRID()) == 1 && PhysicsServer.BodyGetMass(active.GetRID()) == 1, "Contact masks do not mutate body masses");
                    passive.ApplyCentralImpulse(new(0, 5)); tree.PhysicsFrame(1d / 60);
                    Check(passive.LinearVelocity.Y > 4.9f, "Body remains responsive to independent impulses");
                    RigidBody MakeBody(string name, Vector2 position, uint layer, uint mask)
                    {
                        var body = new RigidBody
                        {
                            Name = name,
                            Position = position,
                            CollisionLayer = layer,
                            CollisionMask = mask,
                            GravityScale = 0,
                            CanSleep = false,
                            LinearDamp = 0,
                            AngularDamp = 0,
                            LinearDampMode = RigidBody.DampMode.Replace,
                            AngularDampMode = RigidBody.DampMode.Replace
                        };
                        body.AddChild(new CollisionShape { Shape = shape }); return body;
                    }
                }
    }
    private static void ContinuousResponse(PhysicsServer.Backend backend)
    {
        using var ball = new CircleShape { Radius = 1 }; using var wallShape = new RectangleShape { Size = new(.2f, 100) };
        foreach (var mode in new[] { CCDMode.CastRay, CCDMode.CastShape })
            foreach (var accepts in new[] { false, true })
            {
                using var world = new World(backend); using var root = new SubViewport { World = world };
                var body = new RigidBody
                {
                    Name = "Fast",
                    GravityScale = 0,
                    CanSleep = false,
                    LinearDamp = 0,
                    AngularDamp = 0,
                    LinearDampMode = RigidBody.DampMode.Replace,
                    AngularDampMode = RigidBody.DampMode.Replace,
                    ContinuousCD = mode,
                    LinearVelocity = new(3000, 0),
                    CollisionLayer = 2,
                    CollisionMask = accepts ? 4u : 0u
                };
                body.AddChild(new CollisionShape { Shape = ball }); root.AddChild(body);
                var wall = new StaticBody { Name = "Wall", Position = new(20, 0), CollisionLayer = 4, CollisionMask = accepts ? 0u : 2u };
                wall.AddChild(new CollisionShape { Shape = wallShape }); root.AddChild(wall); using var tree = new SceneTree(root);
                tree.PhysicsFrame(.02);
                Check(accepts ? body.Position.X < 20 : body.Position.X > 50 && MathF.Abs(body.LinearVelocity.X - 3000) < .1f,
                    $"{backend} {mode}: CCD follows the moving endpoint's mask ({accepts}): {body.Position}, {body.LinearVelocity}");
            }
    }
    private static void SensorTransitions(PhysicsServer.Backend backend)
    {
        using var shape = new CircleShape { Radius = 10 }; using var world = new World(backend); using var root = new SubViewport { World = world };
        var area = new Area { Name = "Area", CollisionMask = 0x80000000u }; area.AddChild(new CollisionShape { Shape = shape }); root.AddChild(area);
        var body = new StaticBody { Name = "Body", CollisionLayer = 1 }; body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        using var tree = new SceneTree(root); var entered = 0; var exited = 0;
        area.BodyEntered += other => { Check(other == body, "Sensor owner"); entered++; }; area.BodyExited += _ => exited++;
        tree.PhysicsFrame(1d / 60); Check(entered == 0, "Sensor starts filtered");
        body.CollisionLayer = 0x80000000u; tree.PhysicsFrame(1d / 60); Check(entered == 1 && area.OverlapsBody(body), "Body category change enters sensor");
        PhysicsServer.AreaSetCollisionMask(area.GetRID(), 1); tree.PhysicsFrame(1d / 60); Check(exited == 1 && !area.OverlapsBody(body), "Sensor mask change leaves body");
        body.CollisionLayer = 1; tree.PhysicsFrame(1d / 60); Check(entered == 2, "Later category change reenters sensor");
        var raw = PhysicsServer.AreaCreate();
        try
        {
            PhysicsServer.AreaAddShape(raw, shape.GetRID()); PhysicsServer.AreaSetMonitorable(raw, true); PhysicsServer.AreaSetCollisionLayer(raw, 8); PhysicsServer.AreaSetSpace(raw, world.Space);
            using var query = new PhysicsPointQueryParameters { CollideWithAreas = true, CollideWithBodies = false, CollisionMask = 16 }; var hits = new PhysicsPointResult[4];
            Check(world.DirectSpaceState.IntersectPoint(query, hits) == 0, "Raw area starts excluded from point query");
            PhysicsServer.AreaSetCollisionLayer(raw, 16); Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1, "Raw area changed filter updates its existing sensor geometry");
        }
        finally { PhysicsServer.FreeRID(raw); }
    }
    private static void WakeNeighbors(PhysicsServer.Backend backend)
    {
        using var shape = new CircleShape { Radius = 10 }; using var world = new World(backend); using var root = new SubViewport { World = world };
        var wall = new StaticBody { Name = "Wall" }; wall.AddChild(new CollisionShape { Shape = shape }); root.AddChild(wall);
        var body = new RigidBody { Name = "Body", Position = new(20, 0), GravityScale = 0 }; body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var i = 0; i < 120; i++) tree.PhysicsFrame(1d / 60);
        Check(body.Sleeping, "Contact neighbor reaches sleep before the filter edit");
        PhysicsServer.BodySetCollisionMask(wall.GetRID(), wall.CollisionMask);
        tree.PhysicsFrame(1d / 60); Check(!body.Sleeping, "Even an unchanged static-body filter wakes its sleeping contact neighbor");
    }
    private sealed class CallbackBody : RigidBody
    {
        internal Action<PhysicsDirectBodyState>? Callback;
        protected override void IntegrateForces(PhysicsDirectBodyState state) => Callback?.Invoke(state);
    }
    private static void Callbacks(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world };
        var first = new CallbackBody { Name = "First", GravityScale = 0, CanSleep = false };
        var second = new CallbackBody { Name = "Second", GravityScale = 0, CanSleep = false }; root.AddChild(first); root.AddChild(second);
        using var tree = new SceneTree(root); var observed = 0;
        first.Callback = _ => { PhysicsServer.BodySetCollisionLayer(first.GetRID(), 0x80000000u); first.CollisionMask = 0; throw new ArgumentException("Filter callback"); };
        second.Callback = _ => { Check(PhysicsServer.BodyGetCollisionLayer(first.GetRID()) == 0x80000000u && PhysicsServer.BodyGetCollisionMask(first.GetRID()) == 0, "Later callbacks see committed filter changes"); observed++; };
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60)); Check(observed == 1, "A throwing callback does not skip later bodies");
        first.Callback = null; tree.PhysicsFrame(1d / 60); Check(observed == 2, "Callback failure leaves the world available for the next step");
    }
    private static void Guards(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world }; var body = new RigidBody(); root.AddChild(body); using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        try
        {
            PhysicsServer.BodySetSpace(raw, world.Space);
            foreach (var id in new[] { body.GetRID(), raw })
            {
                Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.BodyGetCollisionLayer(id)).GetAwaiter().GetResult());
                Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.BodyGetCollisionMask(id)).GetAwaiter().GetResult());
                Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.BodySetCollisionLayer(id, 8)).GetAwaiter().GetResult());
                Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.BodySetCollisionMask(id, 8)).GetAwaiter().GetResult());
                Check(PhysicsServer.BodyGetCollisionLayer(id) == 1 && PhysicsServer.BodyGetCollisionMask(id) == 1, "Rejected access preserves filter state");
            }
            Reject<ArgumentException>(() => PhysicsServer.BodyGetCollisionLayer(area)); Reject<ArgumentException>(() => PhysicsServer.BodyGetCollisionMask(area));
            Reject<ArgumentException>(() => PhysicsServer.BodySetCollisionLayer(area, 0)); Reject<ArgumentException>(() => PhysicsServer.BodySetCollisionMask(area, 0));
        }
        finally { PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(area); }
        Reject<ArgumentException>(() => PhysicsServer.BodyGetCollisionLayer(raw)); Reject<ArgumentException>(() => PhysicsServer.BodyGetCollisionMask(raw));
    }
    private static void Collect() { GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
