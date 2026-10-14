using Electron2D;

public static partial class BackendChecks
{
    public static void RunBoundaryGeometry(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("BoundarySolver", () => new Solver());
        try
        {
            Queries(); QuerySource(); Mass(); Lifecycle(); SleepAndRemoval(); Pairs(); Corner();
            for (var family = 0; family < 6; family++)
                foreach (var mode in new[] { CCDMode.Disabled, CCDMode.CastRay, CCDMode.CastShape }) Dynamics(family, mode);
            Continuous(); AngularCCD(); RotatingPlane();
            Console.WriteLine($"Boundary geometry {backend?.ToString() ?? "registered"}, threaded={threaded}: infinite half-plane, six queries, mass, contacts, CCD and zero warm frames passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        World Create() => backend is { } selected ? new World(selected) : new World("BoundarySolver");
        void Queries()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var plane = new WorldBoundaryShape { Normal = new(0, -2), Distance = 10 };
            using var circle = new CircleShape { Radius = 2 }; using var box = new RectangleShape { Size = new(4, 4) };
            var floor = new StaticBody { Position = new(40, 10), Rotation = .37f };
            floor.AddChild(new CollisionShape { Name = "Disabled", Shape = circle, Disabled = true });
            var slot = new CollisionShape { Shape = plane, Position = new(3, -1), Rotation = .2f }; floor.AddChild(slot); root.AddChild(floor);
            var pose = floor.Transform * slot.Transform; var outward = pose.BasisXform(Vector2.Up);
            var onPlane = pose * new Vector2(6000, -5);
            var area = new Area { Position = pose.Origin, Rotation = pose.Rotation, CollisionMask = 2 };
            area.AddChild(new CollisionShape { Shape = plane }); root.AddChild(area);
            var peer = new StaticBody { Name = "Peer", Position = onPlane - outward, CollisionLayer = 2, CollisionMask = 2 };
            peer.AddChild(new CollisionShape { Shape = circle }); root.AddChild(peer);
            var entries = 0; var shapes = 0; area.BodyEntered += _ => entries++;
            area.BodyShapeEntered += (rid, _, remote, local) => { Check(rid == peer.GetRID() && remote == 0 && local == 0, "Boundary Area payload retains logical indices"); shapes++; };
            using var tree = new SceneTree(root);
            var moving = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodySetMode(moving, PhysicsServer.BodyMode.Kinematic); PhysicsServer.BodyAddShape(moving, box.GetRID());
                PhysicsServer.BodySetTransform(moving, new(0, new Vector2(-10000, -10000))); PhysicsServer.BodySetSpace(moving, world.Space);
                var from = onPlane + outward * 15; var motion = -outward * 30;
                using var point = new PhysicsPointQueryParameters { CollisionMask = 1, CollideWithAreas = false };
                using var ray = new PhysicsRayQueryParameters { From = from, To = from + motion, CollisionMask = 1, CollideWithAreas = false };
                using var query = new PhysicsShapeQueryParameters { Shape = circle, Transform = new(0, from), Motion = motion, Margin = 0, CollisionMask = 1, CollideWithAreas = false };
                using var test = new PhysicsTestMotionParameters { From = new(pose.Rotation, from), Motion = motion, Margin = 0 };
                using var result = new PhysicsTestMotionResult(); var points = new PhysicsPointResult[4]; var hits = new PhysicsShapeResult[4]; var contacts = new Vector2[8];
                Warm(Verify, "queries/Area");
                ray.From = onPlane - outward * 2; ray.To = onPlane + outward * 3;
                Check(world.DirectSpaceState.IntersectRay(ray) is null, "Inside ray rejects without HitFromInside"); ray.HitFromInside = true;
                Check(world.DirectSpaceState.IntersectRay(ray) is { Normal: var normal } && normal == Vector2.Zero, "Inside ray exposes a zero normal");
                query.Motion = Vector2.Zero; query.Transform = new(0, onPlane + outward * 1.75f); query.Margin = .5f;
                Check(world.DirectSpaceState.CollideShape(query, contacts) > 0, "Query margin expands finite support toward an infinite plane");
                query.Transform = new(0, onPlane - outward); Check(world.DirectSpaceState.CastMotion(query).UnsafeFraction == 1, "Direct cast ignores initial penetration");
                query.Exclude = [floor.GetRID()]; Check(world.DirectSpaceState.IntersectShape(query, hits) == 0, "Boundary exclusions apply to infinite candidates");
                query.Exclude = []; query.CollisionMask = 4; Check(world.DirectSpaceState.IntersectShape(query, hits) == 0, "Boundary query mask filters candidates");
                peer.Position = onPlane + outward * 10; tree.PhysicsFrame(1d / 60); Check(!area.OverlapsBody(peer), "A far free-side peer exits the half-plane Area");
                peer.Position = onPlane - outward; tree.PhysicsFrame(1d / 60); Check(entries == 2 && shapes == 2, "Reentry emits one logical event");
                void Verify()
                {
                    tree.PhysicsFrame(1d / 60); Check(entries == 1 && shapes == 1 && area.OverlapsBody(peer), "Infinite Area membership emits one transition");
                    point.Position = onPlane + outward; Check(world.DirectSpaceState.IntersectPoint(point, points) == 0, "Free-side point remains outside");
                    point.Position = onPlane - outward; var pointCount = world.DirectSpaceState.IntersectPoint(point, points);
                    if (!(pointCount == 1 && points[0].ShapeIndex == 1)) throw new InvalidOperationException($"Solid point count={pointCount}, index={points[0].ShapeIndex}, position={point.Position}, floor={floor.GetRID()}, actual={points[0].ColliderRID}.");
                    var hit = world.DirectSpaceState.IntersectRay(ray);
                    Check(hit is { } h && h.ColliderRID == floor.GetRID() && h.ShapeIndex == 1 && h.Position.DistanceTo(onPlane) < .005f && h.Normal.Dot(outward) > .999f, "Rotated offset nonunit plane ray has exact point and outward normal");
                    query.Shape = circle; query.Transform = new(0, from); query.Motion = motion;
                    Check(world.DirectSpaceState.IntersectShape(query, hits) == 1 && hits[0].ShapeIndex == 1, "Swept circle hits infinite material");
                    var cast = world.DirectSpaceState.CastMotion(query); Check(MathF.Abs(cast.UnsafeFraction - 13f / 30) < 1f / 256 + .00005f, "Circle cast brackets analytic first support");
                    query.Shape = box; query.Transform = new(pose.Rotation, from);
                    cast = world.DirectSpaceState.CastMotion(query); Check(MathF.Abs(cast.UnsafeFraction - 13f / 30) < 1f / 256 + .00005f, "Rectangle cast uses plane-facing support");
                    Check(PhysicsServer.BodyTestMotion(moving, test, result) && result.GetColliderRID() == floor.GetRID() && result.GetColliderShape() == 1 && MathF.Abs(result.GetCollisionUnsafeFraction() - 13f / 30) < 1f / 256 + .00005f, "Body motion preserves unbounded target and shape index");
                    query.Shape = circle; query.Motion = Vector2.Zero; query.Transform = new(0, onPlane + outward * 1.75f);
                    Check(world.DirectSpaceState.CollideShape(query, contacts) > 0, "Shape contacts expose penetration against plane");
                    var rest = world.DirectSpaceState.GetRestInfo(query);
                    Check(rest is { } r && r.ColliderRID == floor.GetRID() && r.ShapeIndex == 1 && r.Normal.Dot(outward) > .999f && MathF.Abs((r.Point - onPlane).Dot(outward)) < .005f, "Rest point lies on the actual infinite plane");
                }
            }
            finally { PhysicsServer.FreeRID(moving); }
        }
        void QuerySource()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var plane = new WorldBoundaryShape(); using var circle = new CircleShape { Radius = 1 };
            var peer = new StaticBody { Position = new(20, -5) }; peer.AddChild(new CollisionShape { Shape = circle }); root.AddChild(peer);
            using var tree = new SceneTree(root);
            using var query = new PhysicsShapeQueryParameters { Shape = plane, Margin = 0, Motion = new(0, -10) };
            var hits = new PhysicsShapeResult[2]; var contacts = new Vector2[4];
            Check(world.DirectSpaceState.IntersectShape(query, hits) == 1 && hits[0].ColliderRID == peer.GetRID(), "Infinite query source sweeps into the finite counterpart");
            var cast = world.DirectSpaceState.CastMotion(query); Check(MathF.Abs(cast.UnsafeFraction - .4f) < 1f / 256 + .00002f, "Boundary source cast brackets counterpart support");
            Check(world.DirectSpaceState.CollideShape(query, contacts) > 0 && world.DirectSpaceState.GetRestInfo(query)?.ColliderRID == peer.GetRID(), "Boundary source contacts and rest retain finite target");
            query.Motion = Vector2.Zero; query.Margin = 4.25f;
            Check(world.DirectSpaceState.IntersectShape(query, hits) == 1, "Margin shifts query plane outward without bounded inflation");
        }
        void SleepAndRemoval()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var plane = new WorldBoundaryShape(); using var circle = new CircleShape { Radius = 2 };
            var floor = new StaticBody(); floor.AddChild(new CollisionShape { Shape = plane }); root.AddChild(floor);
            var body = Dynamic(new(6000, -2), circle); body.GravityScale = 1; body.CanSleep = true; body.LockRotation = true; root.AddChild(body);
            using var tree = new SceneTree(root); for (var i = 0; i < 240; i++) tree.PhysicsFrame(1d / 60);
            if (!(body.Sleeping && body.Position.Y < -.49f)) throw new InvalidOperationException($"Boundary rest: sleeping={body.Sleeping}, position={body.Position}, velocity={body.LinearVelocity}, angular={body.AngularVelocity}.");
            floor.QueueFree(); tree.PhysicsFrame(1d / 60); body.Sleeping = false;
            for (var i = 0; i < 30; i++) tree.PhysicsFrame(1d / 60);
            Check(body.Position.Y > 20, "Removed boundary no longer constrains a woken neighbour");
        }
        void Corner()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var plane = new WorldBoundaryShape(); using var circle = new CircleShape { Radius = 1 };
            var walls = new StaticBody(); walls.AddChild(new CollisionShape { Name = "Floor", Shape = plane });
            walls.AddChild(new CollisionShape { Name = "Wall", Shape = plane, Rotation = Mathf.Pi / 2 }); root.AddChild(walls);
            var body = Dynamic(new(.975f, -.975f), circle); body.LinearVelocity = new(-20, 20); body.LockRotation = true; body.ContactMonitor = true; root.AddChild(body);
            var entries = 0; body.BodyEntered += _ => entries++;
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!; using var history = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            Warm(Frame, "two-plane corner/replay");
            void Frame()
            {
                var before = entries; history.Restore(); Check(entries == before, "Corner restore is silent"); tree.PhysicsFrame(1d / 2048);
                var impulse = Vector2.Zero; var floorHit = false; var wallHit = false;
                for (var i = 0; i < state.GetContactCount(); i++)
                {
                    impulse += state.GetContactImpulse(i); var n = state.GetContactLocalNormal(i);
                    floorHit |= state.GetContactColliderShape(i) == 0 && n.Dot(Vector2.Up) > .999f;
                    wallHit |= state.GetContactColliderShape(i) == 1 && n.Dot(Vector2.Right) > .999f;
                }
                Check(floorHit && wallHit && entries == before + 1 && body.LinearVelocity.Length() < .1f &&
                    (impulse - (body.LinearVelocity - new Vector2(-20, 20))).Length() < .03f, "Two infinite planes keep distinct contact normals, logical slots and full impulse under one object entry");
            }
        }
        void Mass()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var plane = new WorldBoundaryShape(); using var circle = new CircleShape { Radius = 2 };
            var body = Dynamic(Vector2.Zero, plane); body.Mass = 3; root.AddChild(body);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            Check(state.InverseMass == 1f / 3 && state.InverseInertia == 0 && state.CenterOfMassLocal == Vector2.Zero, "An infinite boundary has no geometric area, center or inertia");
            body.Inertia = 20; tree.PhysicsFrame(1d / 2048); Check(MathF.Abs(state.InverseInertia - .05f) < .00001f, "Explicit boundary inertia remains effective");
            body.Inertia = 0; body.AddChild(new CollisionShape { Name = "Finite", Shape = circle, Position = new(3, -2) }); tree.PhysicsFrame(1d / 2048);
            if (!(state.CenterOfMassLocal.DistanceTo(new(3, -2)) < .0002f && MathF.Abs(state.InverseInertia - 1f / 6) < .0002f)) throw new InvalidOperationException($"Mixed boundary mass: center={state.CenterOfMassLocal}, inverse={state.InverseInertia}.");
        }
        void Lifecycle()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var plane = new WorldBoundaryShape(); using var circle = new CircleShape { Radius = 2 };
            var floor = new StaticBody(); var slot = new CollisionShape { Shape = plane }; floor.AddChild(slot); root.AddChild(floor);
            using var tree = new SceneTree(root); var identity = floor.GetRID();
            using var point = new PhysicsPointQueryParameters { Position = new(4_000_000, 20) }; var output = new PhysicsPointResult[2];
            Check(world.DirectSpaceState.IntersectPoint(point, output) == 1, "World boundary remains infinite beyond editing bounds");
            plane.Distance = -10; Check(world.DirectSpaceState.IntersectPoint(point, output) == 1, "Distance edit updates half-plane without changing identity");
            plane.Normal = Vector2.Down; Check(world.DirectSpaceState.IntersectPoint(point, output) == 0, "Normal edit changes the infinite solid side");
            slot.Disabled = true; Check(world.DirectSpaceState.IntersectPoint(point, output) == 0, "Disabled boundary retires its geometry");
            slot.Disabled = false; slot.Shape = circle; Check(floor.GetRID() == identity && world.DirectSpaceState.IntersectPoint(point, output) == 0, "Replacing a boundary preserves body identity and finite extent");
            slot.Shape = plane; plane.Normal = Vector2.Up; Check(world.DirectSpaceState.IntersectPoint(point, output) == 1, "Reattached boundary restores infinite sensing");
            plane.Dispose(); Check(world.DirectSpaceState.IntersectPoint(point, output) == 0, "Disposed boundary retires borrowed geometry");
        }
        void Pairs()
        {
            using var world = Create(); using var plane = new WorldBoundaryShape(); using var circle = new CircleShape { Radius = 2 }; using var other = new WorldBoundaryShape();
            var output = new Vector2[8];
            Check(!PhysicsServer.ShapeCollide(world.Space, plane.GetRID(), Transform.Identity, Vector2.Zero, other.GetRID(), Transform.Identity, Vector2.Zero, output, out _), "Boundary/boundary pairs never collide");
            Check(!PhysicsServer.ShapeCollide(world.Space, plane.GetRID(), Transform.Identity, new(0, -100), circle.GetRID(), new(0, new Vector2(4_000_000, 5)), new(0, -10), output, out _), "Standalone boundary ignores its own motion and uses counterpart endpoint");
            Check(PhysicsServer.ShapeCollide(world.Space, circle.GetRID(), new(0, new Vector2(4_000_000, -10)), new(0, 11), plane.GetRID(), Transform.Identity, new(0, 100), output, out var count) && count == 1 && output[0].DistanceTo(new(4_000_000, 3)) < .001f && output[1].DistanceTo(new(4_000_000, 0)) < .001f, "Reverse boundary pair retains caller order and full separating depth");
            using var concave = new ConcavePolygonShape { Segments = [new(-3, -1), new(3, -1)] };
            Check(PhysicsServer.ShapeCollide(world.Space, plane.GetRID(), Transform.Identity, Vector2.Zero, concave.GetRID(), Transform.Identity, new(0, 2), output, out count) && count > 0, "Boundary endpoint policy takes precedence over concave motion exclusion");
            using var box = new RectangleShape { Size = new(4, 4) };
            Check(PhysicsServer.ShapeCollide(world.Space, plane.GetRID(), Transform.Identity, Vector2.Zero, box.GetRID(), new(0, new Vector2(0, -1)), Vector2.Zero, output, out count) && count == 2 &&
                output[0].DistanceTo(output[2]) > 3.9f, "Standalone boundary preserves both ends of a finite support face");
            Check(!PhysicsServer.ShapeCollide(world.Space, plane.GetRID(), Transform.Identity, Vector2.Zero, circle.GetRID(), new(0, new Vector2(0, -2)), Vector2.Zero, output, out _), "Standalone boundary exact touching has no penetrating pair");
            if (backend is null)
            {
                using var ring = new CustomShape(Annulus.Create(2, 5));
                Check(PhysicsServer.ShapeCollide(world.Space, ring.GetRID(), new(0, new Vector2(0, -4)), Vector2.Zero, plane.GetRID(), Transform.Identity, Vector2.Zero, output, out count) && count > 0, "Custom annulus callback supports infinite counterpart");
            }
        }
        void Dynamics(int family, CCDMode mode)
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var plane = new WorldBoundaryShape();
            using var shape = Family(family); using var material = new PhysicsMaterial { Friction = .5f, Bounce = .25f };
            var floor = new StaticBody { PhysicsMaterialOverride = material, ConstantLinearVelocity = new(2, 0) }; floor.AddChild(new CollisionShape { Shape = plane }); root.AddChild(floor);
            var support = family is 1 or 2 or 4 ? 2f : family is 3 or 5 ? 0 : 1;
            var body = Dynamic(new(6000, -support + .025f), shape); body.LockRotation = true; body.ContactMonitor = true;
            body.LinearVelocity = new(10, 20); body.PhysicsMaterialOverride = material; body.ContinuousCD = mode; root.AddChild(body);
            var entries = 0; var shapeEntries = 0; body.BodyEntered += _ => entries++;
            body.BodyShapeEntered += (rid, _, remote, local) => { Check(rid == floor.GetRID() && remote == 0 && local == 0, "Contact event retains boundary/body shape identity"); shapeEntries++; };
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!; using var history = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            Warm(Frame, $"family {family}/{mode} contact/replay");
            void Frame()
            {
                var before = entries; var beforeShapes = shapeEntries; history.Restore(); Check(entries == before && shapeEntries == beforeShapes, "Boundary replay restoration emits no gameplay events");
                tree.PhysicsFrame(1d / 2048); var impulse = Vector2.Zero;
                for (var i = 0; i < state.GetContactCount(); i++)
                {
                    Check(state.GetContactCollider(i) == floor.GetRID() && state.GetContactColliderShape(i) == 0 && state.GetContactLocalShape(i) == 0 && state.GetContactLocalNormal(i).Dot(Vector2.Up) > .999f, "Contact reports free-side plane normal and exact collider");
                    impulse += state.GetContactImpulse(i);
                }
                if (!(state.GetContactCount() > 0 && entries == before + 1 && shapeEntries == beforeShapes + 1 &&
                    (impulse - (body.LinearVelocity - new Vector2(10, 20))).Length() < .03f && body.LinearVelocity.Y < -.1f && body.LinearVelocity.X < 9.9f && floor.Position == Vector2.Zero))
                    throw new InvalidOperationException($"Boundary response family={family}, mode={mode}: contacts={state.GetContactCount()}, v={body.LinearVelocity}, impulse={impulse}, events={entries - before}/{shapeEntries - beforeShapes}.");
            }
        }
        void Continuous()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var plane = new WorldBoundaryShape(); using var circle = new CircleShape { Radius = 1 };
            var floor = new StaticBody(); floor.AddChild(new CollisionShape { Shape = plane }); root.AddChild(floor);
            var body = Dynamic(new(4_000_000, -10), circle); body.LockRotation = true; body.LinearVelocity = new(0, 1000); body.ContinuousCD = CCDMode.CastShape; root.AddChild(body);
            using var tree = new SceneTree(root); tree.PhysicsFrame(.02);
            Check(body.Position.Y <= -.499f && MathF.Abs(body.LinearVelocity.Y) < .1f, "Full-shape CCD stops on infinite plane after crossing the discrete endpoint");
        }
        void AngularCCD()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var plane = new WorldBoundaryShape { Normal = Vector2.Right, Distance = -12 }; using var bar = new RectangleShape { Size = new(2, 40) };
            var floor = new StaticBody(); floor.AddChild(new CollisionShape { Shape = plane }); root.AddChild(floor);
            var body = Dynamic(Vector2.Zero, bar); body.Mass = 2; body.Inertia = 300; body.AngularVelocity = 30; body.ContinuousCD = CCDMode.CastShape; root.AddChild(body);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!; tree.PhysicsFrame(.04);
            Check(state.GetContactCount() > 0 && body.AngularVelocity < 29.9f && body.Position.IsFinite() && body.LinearVelocity.LengthSquared() + 150 * body.AngularVelocity * body.AngularVelocity <= 137700, "Rotational contour CCD detects an intermediate plane-facing corner and preserves energy ceiling");
        }
        void RotatingPlane()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var plane = new WorldBoundaryShape(); using var circle = new CircleShape { Radius = 1 };
            var floor = Dynamic(Vector2.Zero, plane); floor.Name = "Plane"; floor.Mass = 1e9f; floor.Inertia = 1e9f; floor.AngularVelocity = -20; root.AddChild(floor);
            var body = Dynamic(new(80, -1.3f), circle); body.ContinuousCD = CCDMode.CastShape; root.AddChild(body);
            using var tree = new SceneTree(root); tree.PhysicsFrame(.02);
            var gap = Vector2.Up.Rotated(floor.Rotation).Dot(body.Position - floor.Position) - 1;
            Check(gap > -.75f && body.Position.IsFinite(), $"Rotating infinite plane CCD bounds penetration: {gap}.");
        }
        static Shape Family(int index) => index switch
        {
            0 => new CircleShape { Radius = 1 },
            1 => new RectangleShape { Size = new(4, 4) },
            2 => new CapsuleShape { Radius = 1, Height = 4 },
            3 => new SegmentShape { A = new(-2, 0), B = new(2, 0) },
            4 => new ConvexPolygonShape { Points = [new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)] },
            _ => new ConcavePolygonShape { Segments = [new(-2, 0), new(2, 0)] }
        };
        static RigidBody Dynamic(Vector2 position, Shape shape)
        {
            var body = new RigidBody
            {
                Position = position,
                GravityScale = 0,
                CanSleep = false,
                Mass = 1,
                MaxContactsReported = 8,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            body.AddChild(new CollisionShape { Shape = shape }); return body;
        }
        static void Warm(Action frame, string label)
        {
            frame(); for (var i = 0; i < 128; i++) frame(); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Boundary {label} allocated {own}/{all} B");
            Console.WriteLine($"Boundary {label}: 128 full frames {own}/{all} B.");
        }
    }
}
