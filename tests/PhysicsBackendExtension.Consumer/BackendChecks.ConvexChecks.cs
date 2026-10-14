using Electron2D;

public static partial class BackendChecks
{
    public static void RunConvexGeometry(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("ConvexSolver", () => new Solver());
        try
        {
            Queries(); Mass(); Asymmetric();
            foreach (var mode in new[] { CCDMode.Disabled, CCDMode.CastRay, CCDMode.CastShape })
            { Dynamics(false, mode); Dynamics(true, mode); }
            AngularContact(); Continuous(); AngularCCD(); Pairs();
            if (backend is null) Ring();
            Console.WriteLine($"Convex geometry {backend?.ToString() ?? "registered"}, threaded={threaded}: transformed contours, six queries, mass, contacts, CCD and zero warm frames passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        World Create() => backend is { } selected ? new World(selected) : new World("ConvexSolver");

        void Queries()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var polygon = Contour(60, 7.5f); using var circle = new CircleShape { Radius = 5 };
            using var small = Contour(10, 10);
            var body = new StaticBody { Position = new(40, 10), Rotation = .37f };
            body.AddChild(new CollisionShape { Name = "Disabled", Shape = circle, Disabled = true });
            var slot = new CollisionShape { Name = "Convex", Shape = polygon, Position = new(3, -1), Rotation = .2f };
            body.AddChild(slot); root.AddChild(body);
            var areaPose = body.Transform * slot.Transform;
            var area = new Area { Position = areaPose.Origin, Rotation = areaPose.Rotation };
            area.AddChild(new CollisionShape { Shape = polygon }); root.AddChild(area);
            var areaEntries = 0; area.BodyEntered += other => { if (other == body) areaEntries++; };
            using var tree = new SceneTree(root);
            var moving = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodySetMode(moving, PhysicsServer.BodyMode.Kinematic); PhysicsServer.BodyAddShape(moving, small.GetRID());
                PhysicsServer.BodySetTransform(moving, new(0, new Vector2(-1000, 0))); PhysicsServer.BodySetSpace(moving, world.Space);
                var pose = body.GlobalTransform * slot.Transform; var axis = pose.X.Normalized();
                var from = pose * new Vector2(-150, 0); var to = pose * new Vector2(150, 0);
                using var point = new PhysicsPointQueryParameters(); using var ray = new PhysicsRayQueryParameters { From = from, To = to };
                using var query = new PhysicsShapeQueryParameters { Shape = circle, Transform = new(0, from), Motion = to - from, Margin = 0 };
                using var motion = new PhysicsTestMotionParameters { From = new(pose.Rotation, from), Motion = to - from, Margin = 0 };
                using var result = new PhysicsTestMotionResult();
                var points = new PhysicsPointResult[4]; var hits = new PhysicsShapeResult[4]; var contacts = new Vector2[8];
                Verify(); for (var i = 0; i < 128; i++) Verify(); Collect();
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Verify();
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"Convex query/full frames allocated {own}/{all} B");
                Console.WriteLine($"Convex queries {world.PhysicsBackendName}: 128 full six-query frames {own}/{all} B.");
                var deeper = PhysicsServer.BodyCreate();
                try
                {
                    PhysicsServer.BodySetMode(deeper, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(deeper, polygon.GetRID());
                    PhysicsServer.BodySetTransform(deeper, new(pose.Rotation, pose.Origin - axis * .2f)); PhysicsServer.BodySetSpace(deeper, world.Space);
                    query.Shape = circle; query.Transform = new(0, pose * new Vector2(-60, 0)); query.Motion = Vector2.Zero;
                    Check(world.DirectSpaceState.GetRestInfo(query)?.ColliderRID == deeper, "Rest selection prefers the genuinely deeper polygon");
                    query.Exclude = [deeper];
                    Check(world.DirectSpaceState.GetRestInfo(query)?.ColliderRID == body.GetRID(), "Explicit exclusions preserve the next valid polygon");
                    query.Exclude = []; query.CollisionMask = 2;
                    Check(world.DirectSpaceState.IntersectShape(query, hits) == 0, "Convex query masks reject both default layers");
                    query.CollisionMask = 1;
                }
                finally { PhysicsServer.FreeRID(deeper); }

                void Verify()
                {
                    tree.PhysicsFrame(1d / 60);
                    Check(area.OverlapsBody(body) && areaEntries == 1, "Convex Area monitoring retains one actual overlap transition");
                    point.Position = pose * new Vector2(.5f, 0);
                    Check(world.DirectSpaceState.IntersectPoint(point, points) == 1 && points[0].ColliderRID == body.GetRID() && points[0].ShapeIndex == 1,
                        "Point input uses the transformed contour and logical enabled slot");
                    point.CollideWithBodies = false; point.CollideWithAreas = true;
                    Check(world.DirectSpaceState.IntersectPoint(point, points) == 1 && points[0].ColliderRID == area.GetRID(),
                        "Area point queries use the same real rectangular contour");
                    point.CollideWithBodies = true; point.CollideWithAreas = false;
                    point.Position = pose * new Vector2(0, 30);
                    Check(world.DirectSpaceState.IntersectPoint(point, points) == 0, "A point near the long side remains outside the actual thin polygon");
                    point.Position = pose * new Vector2(48, 6);
                    Check(world.DirectSpaceState.IntersectPoint(point, points) == 0, "A point inside the polygon bounds but outside its actual facets misses");
                    var hit = world.DirectSpaceState.IntersectRay(ray);
                    Check(hit is { } h && h.ColliderRID == body.GetRID() && h.ShapeIndex == 1 && h.Position.DistanceTo(pose * new Vector2(-60, 0)) < .002f && h.Normal.Dot(-axis) > .999f,
                        "Ray hit point and normal follow the actual rotated face");
                    query.Shape = circle; query.Transform = new(0, from); query.Motion = to - from;
                    Check(world.DirectSpaceState.IntersectShape(query, hits) == 1 && hits[0].ShapeIndex == 1, "Swept circle intersects the physical rectangular region");
                    var cast = world.DirectSpaceState.CastMotion(query); var expected = (150 - 60 - 5) / 300f;
                    Check(MathF.Abs(cast.UnsafeFraction - expected) <= 1f / 256 + .00002f, "Circle cast brackets the face with its own radius");
                    query.Shape = small; query.Transform = new(pose.Rotation, from);
                    cast = world.DirectSpaceState.CastMotion(query); expected = (150 - 60 - 10) / 300f;
                    Check(MathF.Abs(cast.UnsafeFraction - expected) <= 1f / 256 + .00002f, "Convex cast uses its directional support");
                    Check(PhysicsServer.BodyTestMotion(moving, motion, result) && result.GetColliderRID() == body.GetRID() &&
                        result.GetColliderShape() == 1 && MathF.Abs(result.GetCollisionUnsafeFraction() - expected) <= 1f / 256 + .00002f,
                        "Body motion uses the moving polygon's exact shape");
                    query.Shape = circle; query.Motion = Vector2.Zero; query.Transform = new(0, pose * new Vector2(-60, 0));
                    Check(world.DirectSpaceState.CollideShape(query, contacts) > 0, "Shape contacts publish a real penetrated face pair");
                    var rest = world.DirectSpaceState.GetRestInfo(query);
                    Check(rest is { } r && r.ColliderRID == body.GetRID() && r.ShapeIndex == 1 && r.Normal.Dot(-axis) > .999f &&
                        MathF.Abs((r.Point - pose.Origin).Dot(axis) + 60) < .002f, "Rest geometry retains boundary point and outward normal");
                }
            }
            finally { PhysicsServer.FreeRID(moving); }
        }
        void Pairs()
        {
            using var world = Create(); using var first = Contour(1.1f, 1.1f);
            using var second = Contour(1.1f, 1.1f); using var circle = new CircleShape { Radius = .5f };
            var contacts = new Vector2[8];
            Check(PhysicsServer.ShapeCollide(world.Space, first.GetRID(), new(0, new Vector2(-5, 0)), new(10, 0),
                second.GetRID(), new(0, new Vector2(0, -10)), new(0, 10), contacts, out var count) && count > 0,
                "Independently swept polygons intersect even when their synchronized paths do not meet");
            Check(PhysicsServer.ShapeCollide(world.Space, first.GetRID(), Transform.Identity, Vector2.Zero,
                circle.GetRID(), new(0, new Vector2(1.2f, 1.2f)), Vector2.Zero, contacts, out count) && count > 0,
                "Rounded circle/polygon corner contact is geometric");
            Check(!PhysicsServer.ShapeCollide(world.Space, first.GetRID(), Transform.Identity, Vector2.Zero,
                circle.GetRID(), new(0, new Vector2(1.6f, 1.6f)), Vector2.Zero, contacts, out _),
                "A circle outside the rounded corner misses the polygon");
        }
        void Ring()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var ring = new CustomShape(Annulus.Create(5, 10)); using var polygon = Contour(1.1f, 1.1f);
            var boundary = new StaticBody(); boundary.AddChild(new CollisionShape { Shape = ring }); root.AddChild(boundary);
            var body = Dynamic(Vector2.Zero, polygon, 0); body.LinearVelocity = new(10, 0); body.ContinuousCD = CCDMode.CastShape;
            root.AddChild(body); using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            Check(!ring.Collide(Transform.Identity, polygon, Transform.Identity), "A polygon remains outside the annulus material while inside its hole");
            Check(ring.CollideWithMotion(Transform.Identity, Vector2.Zero, polygon, Transform.Identity, new(10, 0)),
                "Independent swept polygon reaches actual custom ring material");
            tree.PhysicsFrame(.6);
            var maximumRadius = 0f;
            foreach (var corner in new Vector2[] { new(-1.1f, -1.1f), new(1.1f, -1.1f), new(1.1f, 1.1f), new(-1.1f, 1.1f) })
                maximumRadius = MathF.Max(maximumRadius, (body.GlobalTransform * corner).Length());
            var energy = .5f * body.LinearVelocity.LengthSquared() + body.AngularVelocity * body.AngularVelocity / (2 * state.InverseInertia);
            if (!(state.GetContactCount() > 0 && maximumRadius <= 5.301f && energy <= 50.1f))
                throw new InvalidOperationException($"Convex ring response: radius={maximumRadius}, position={body.Position}, velocity={body.LinearVelocity}, angular={body.AngularVelocity}, energy={energy}, contacts={state.GetContactCount()}.");
        }
        void Mass()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var polygon = new ConvexPolygonShape { Points = [new(-30, -7.5f), new(30, -7.5f), new(30, 7.5f), new(-30, 7.5f)] }; using var circle = new CircleShape { Radius = 5 };
            var body = Dynamic(new(0, 0), polygon, 0); body.Mass = 3;
            ((CollisionShape)body.GetChild(0)).Position = new(2, 0);
            body.AddChild(new CollisionShape { Name = "Circle", Shape = circle, Position = new(-3, 0) }); root.AddChild(body);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            tree.PhysicsFrame(1d / 60);
            var circleArea = MathF.PI * 25; var area = 900 + circleArea; var center = (1800 - circleArea * 3) / area;
            var inertia = 3 * (900 * ((3600 + 225f) / 12 + (2 - center) * (2 - center)) + circleArea * (12.5f + (-3 - center) * (-3 - center))) / area;
            Check(MathF.Abs(state.CenterOfMassLocal.X - center) < .0002f && MathF.Abs(state.CenterOfMassLocal.Y) < .0002f &&
                MathF.Abs(1 / state.InverseInertia - inertia) < .0002f * inertia, "Compound polygon/circle mass uses actual area, centroid and parallel-axis inertia");
        }
        void Asymmetric()
        {
            foreach (var reverse in new[] { false, true })
            {
                using var world = Create(); using var root = new SubViewport { World = world };
                var points = new Vector2[] { new(4, -7), new(10, -7), new(4, -4) };
                if (reverse) Array.Reverse(points);
                using var polygon = new ConvexPolygonShape { Points = [.. points, points[0]] };
                var body = Dynamic(new(40, 10), polygon, .37f); body.Mass = 3;
                var child = (CollisionShape)body.GetChild(0); child.Position = new(2, -1); child.Rotation = .2f; root.AddChild(body);
                using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
                tree.PhysicsFrame(1d / 60);
                var center = new Vector2(6, -6).Rotated(.2f) + new Vector2(2, -1);
                Check(state.CenterOfMassLocal.DistanceTo(center) < .0002f && MathF.Abs(1 / state.InverseInertia - 7.5f) < .0002f,
                    "Translated asymmetric triangle retains its analytic centroid and inertia in either winding");
                var pose = body.GlobalTransform * child.Transform;
                using var point = new PhysicsPointQueryParameters { Position = pose * new Vector2(5, -6) }; var hits = new PhysicsPointResult[2];
                Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1, "The translated triangle interior remains real filled geometry");
                point.Position = pose * new Vector2(9, -5);
                Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0, "Triangle bounds do not replace its diagonal face");
                using var ray = new PhysicsRayQueryParameters { From = pose * new Vector2(-1, -6), To = pose * new Vector2(12, -6) };
                Check(world.DirectSpaceState.IntersectRay(ray) is { } hit && hit.Position.DistanceTo(pose * new Vector2(4, -6)) < .002f &&
                    hit.Normal.Dot(-pose.X.Normalized()) > .999f, "Asymmetric polygon ray clips the real translated face");
                polygon.Points = [];
                Check(world.DirectSpaceState.IntersectRay(ray) is null && world.DirectSpaceState.IntersectPoint(point, hits) == 0,
                    "An empty live contour retires every numerical fixture");
                polygon.Points = [.. points, points[0]];
                Check(world.DirectSpaceState.IntersectRay(ray)?.ColliderRID == body.GetRID(), "Live contour replacement preserves logical collider identity");
            }
        }
        void Dynamics(bool boxes, CCDMode mode)
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var polygon = Contour(2, 1.1f); using var circle = new CircleShape { Radius = 1 };
            using var material = new PhysicsMaterial { Bounce = .5f, Friction = 0 };
            var axis = Vector2.Right.Rotated(.37f);
            var distance = mode == CCDMode.Disabled ? boxes ? 1.975f : 1.475f : 3.1f;
            var a = Dynamic(-axis * distance, polygon, .37f); var b = Dynamic(axis * distance, boxes ? polygon : circle, .37f);
            a.Name = "First"; b.Name = "Second"; a.ContinuousCD = b.ContinuousCD = mode;
            a.ContactMonitor = b.ContactMonitor = true; var entries = 0; a.BodyEntered += _ => entries++;
            a.LinearVelocity = axis * 5; b.LinearVelocity = -axis * 5; a.LockRotation = b.LockRotation = true;
            a.PhysicsMaterialOverride = b.PhysicsMaterialOverride = material; root.AddChild(a); root.AddChild(b);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(a.GetRID())!;
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            Frame(); for (var i = 0; i < 128; i++) Frame(); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Convex contact/replay frames allocated {own}/{all} B");
            Console.WriteLine($"Convex contacts boxes={boxes}, CCD={mode} {world.PhysicsBackendName}: momentum/restitution/full impulse and 128 replay frames {own}/{all} B.");
            void Frame()
            {
                var previousEntries = entries; checkpoint.Restore();
                Check(entries == previousEntries, "Convex checkpoint restoration is silent");
                tree.PhysicsFrame(mode == CCDMode.Disabled ? .01 : .4);
                Check(entries == previousEntries + 1, "Convex contact entry is delivered once for each replayed frame");
                Check(a.LinearVelocity.Dot(axis) < -4.98f && b.LinearVelocity.Dot(axis) > 4.98f &&
                    (a.LinearVelocity + b.LinearVelocity).Length() < .002f, "Elastic polygon contact conserves paired linear momentum");
                var impulse = Vector2.Zero;
                for (var i = 0; i < state.GetContactCount(); i++) impulse += state.GetContactImpulse(i);
                Check(state.GetContactCount() > 0 && (impulse - (a.LinearVelocity - axis * 5)).Length() < .02f,
                    "Complete step contact impulse explains the actual change of momentum");
            }
        }
        void AngularContact()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var polygon = Contour(2, 1.1f); using var circle = new CircleShape { Radius = 1.2f };
            using var material = new PhysicsMaterial { Friction = 0, Bounce = 0 };
            var body = Dynamic(Vector2.Zero, polygon, 0); body.Mass = 2; body.Inertia = 20;
            body.LinearVelocity = new(20, 0); body.PhysicsMaterialOverride = material; root.AddChild(body);
            var wall = new StaticBody { Position = new(2.8f, -1.8f), PhysicsMaterialOverride = material };
            wall.AddChild(new CollisionShape { Shape = circle }); root.AddChild(wall);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            using var history = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            Frame(); for (var i = 0; i < 128; i++) Frame(); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Convex angular contact/replay allocated {own}/{all} B");
            Console.WriteLine($"Convex angular contact {world.PhysicsBackendName}: lever/energy/full impulses, 128 replay frames {own}/{all} B.");
            void Frame()
            {
                history.Restore(); tree.PhysicsFrame(1d / 2048);
                var impulse = Vector2.Zero; var moment = 0f;
                for (var i = 0; i < state.GetContactCount(); i++)
                {
                    var value = state.GetContactImpulse(i); var point = state.GetContactLocalPosition(i);
                    var arm = point - (body.GlobalTransform * state.CenterOfMassLocal);
                    impulse += value; moment += arm.X * value.Y - arm.Y * value.X;
                }
                // A published contact point can precede final pose integration by this interval.
                var momentLimit = .005f + (1f / 2048) * MathF.Max(20, body.LinearVelocity.Length()) * impulse.Length();
                if (!(state.GetContactCount() > 0 && body.AngularVelocity > .01f &&
                    (impulse - 2 * (body.LinearVelocity - new Vector2(20, 0))).Length() < .03f &&
                    MathF.Abs(moment - 20 * body.AngularVelocity) < momentLimit))
                    throw new InvalidOperationException($"Corner contact: count={state.GetContactCount()}, velocity={body.LinearVelocity}, angular={body.AngularVelocity}, impulse={impulse}, moment={moment}, expected={20 * body.AngularVelocity}, limit={momentLimit}.");
                Check(body.LinearVelocity.LengthSquared() + 10 * body.AngularVelocity * body.AngularVelocity <= 400.1f,
                    "Inelastic polygon corner response does not add energy");
            }
        }
        void Continuous()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var wallShape = new RectangleShape { Size = new(.5f, 20) }; using var boxShape = Contour(1.1f, 1.1f);
            var wall = new StaticBody { Position = new(10, 0) }; wall.AddChild(new CollisionShape { Shape = wallShape }); root.AddChild(wall);
            var body = Dynamic(Vector2.Zero, boxShape, 0); body.LinearVelocity = new(1000, 0); body.ContinuousCD = CCDMode.CastShape; root.AddChild(body);
            using var tree = new SceneTree(root); tree.PhysicsFrame(.02);
            Check(body.Position.X <= 9.151f && MathF.Abs(body.LinearVelocity.X) < .1f, "Continuous polygon motion stops at the thin wall");
        }
        void AngularCCD()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var bar = Contour(160, 20); using var circle = new CircleShape { Radius = .25f };
            using var material = new PhysicsMaterial { Friction = 0, Bounce = 0 };
            var body = Dynamic(Vector2.Zero, bar, 0); body.Mass = 2; body.Inertia = 20000;
            body.AngularVelocity = 30; body.ContinuousCD = CCDMode.CastShape; body.PhysicsMaterialOverride = material; root.AddChild(body);
            var peer = new StaticBody { Position = new Vector2(160, 0).Rotated(.225f), PhysicsMaterialOverride = material };
            peer.AddChild(new CollisionShape { Shape = circle }); root.AddChild(peer);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            tree.PhysicsFrame(.015);
            Check(state.GetContactCount() > 0 && body.AngularVelocity < 29.9f && body.Position.IsFinite() &&
                body.LinearVelocity.LengthSquared() + 10000 * body.AngularVelocity * body.AngularVelocity <= 9180000,
                "Rotational polygon CCD sees the intermediate contour arc and preserves the energy ceiling");
        }
        static ConvexPolygonShape Contour(float x, float y)
        {
            if (x < 60) return new() { Points = [new(-x, -y), new(x, -y), new(x, y), new(-x, y)] };
            var points = new Vector2[12]; var factor = MathF.Cos(MathF.PI / 12);
            for (var i = 0; i < 12; i++)
            { var angle = MathF.PI / 12 + i * MathF.PI / 6; points[i] = new(x * MathF.Cos(angle) / factor, y * MathF.Sin(angle) / factor); }
            return new() { Points = points };
        }
        static RigidBody Dynamic(Vector2 position, Shape shape, float rotation)
        {
            var body = new RigidBody
            {
                Position = position,
                Rotation = rotation,
                GravityScale = 0,
                CanSleep = false,
                Mass = 1,
                MaxContactsReported = 8,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            body.AddChild(new CollisionShape { Shape = shape }); return body;
        }
    }
}
