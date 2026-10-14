using Electron2D;

public static partial class BackendChecks
{
    public static void RunCapsuleGeometry(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("CapsuleSolver", () => new Solver());
        try
        {
            Queries(); Mass(); CircleLimit();
            foreach (var mode in new[] { CCDMode.Disabled, CCDMode.CastRay, CCDMode.CastShape })
            { Dynamics(false, mode); Dynamics(true, mode); }
            AngularContact(); Continuous(); AngularCCD(); Pairs();
            if (backend is null) Ring();
            Console.WriteLine($"Capsule geometry {backend?.ToString() ?? "registered"}, threaded={threaded}: transformed contours, six queries, mass, contacts, CCD and zero warm frames passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        World Create() => backend is { } selected ? new World(selected) : new World("CapsuleSolver");

        void Queries()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var capsule = new CapsuleShape { Radius = .75f, Height = 12 }; using var circle = new CircleShape { Radius = .5f };
            using var small = new CapsuleShape { Radius = 1, Height = 4 };
            var body = new StaticBody { Position = new(40, 10), Rotation = .37f };
            body.AddChild(new CollisionShape { Name = "Disabled", Shape = circle, Disabled = true });
            var slot = new CollisionShape { Name = "Capsule", Shape = capsule, Position = new(3, -1), Rotation = .2f };
            body.AddChild(slot); root.AddChild(body);
            var areaPose = body.Transform * slot.Transform;
            var area = new Area { Position = areaPose.Origin, Rotation = areaPose.Rotation };
            area.AddChild(new CollisionShape { Shape = capsule }); root.AddChild(area);
            var areaEntries = 0; area.BodyEntered += other => { if (other == body) areaEntries++; };
            using var tree = new SceneTree(root);
            var moving = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodySetMode(moving, PhysicsServer.BodyMode.Kinematic); PhysicsServer.BodyAddShape(moving, small.GetRID());
                PhysicsServer.BodySetTransform(moving, new(0, new Vector2(-1000, 0))); PhysicsServer.BodySetSpace(moving, world.Space);
                var pose = body.GlobalTransform * slot.Transform; var axis = pose.Y.Normalized();
                var from = pose * new Vector2(0, -15); var to = pose * new Vector2(0, 15);
                using var point = new PhysicsPointQueryParameters(); using var ray = new PhysicsRayQueryParameters { From = from, To = to };
                using var query = new PhysicsShapeQueryParameters { Shape = circle, Transform = new(0, from), Motion = to - from, Margin = 0 };
                using var motion = new PhysicsTestMotionParameters { From = new(pose.Rotation, from), Motion = to - from, Margin = 0 };
                using var result = new PhysicsTestMotionResult();
                var points = new PhysicsPointResult[4]; var hits = new PhysicsShapeResult[4]; var contacts = new Vector2[8];
                Verify(); for (var i = 0; i < 128; i++) Verify(); Collect();
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Verify();
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"Capsule query/full frames allocated {own}/{all} B");
                Console.WriteLine($"Capsule queries {world.PhysicsBackendName}: 128 full six-query frames {own}/{all} B.");
                var deeper = PhysicsServer.BodyCreate();
                try
                {
                    PhysicsServer.BodySetMode(deeper, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(deeper, capsule.GetRID());
                    PhysicsServer.BodySetTransform(deeper, new(pose.Rotation, pose.Origin - axis * .2f)); PhysicsServer.BodySetSpace(deeper, world.Space);
                    query.Shape = circle; query.Transform = new(0, pose * new Vector2(0, -6)); query.Motion = Vector2.Zero;
                    Check(world.DirectSpaceState.GetRestInfo(query)?.ColliderRID == deeper, "Rest selection prefers the genuinely deeper capsule");
                    query.Exclude = [deeper];
                    Check(world.DirectSpaceState.GetRestInfo(query)?.ColliderRID == body.GetRID(), "Explicit exclusions preserve the next valid capsule");
                    query.Exclude = []; query.CollisionMask = 2;
                    Check(world.DirectSpaceState.IntersectShape(query, hits) == 0, "Capsule query masks reject both default layers");
                    query.CollisionMask = 1;
                }
                finally { PhysicsServer.FreeRID(deeper); }

                void Verify()
                {
                    tree.PhysicsFrame(1d / 60);
                    Check(area.OverlapsBody(body) && areaEntries == 1, "Capsule Area monitoring retains one actual overlap transition");
                    point.Position = pose * new Vector2(0, .5f);
                    Check(world.DirectSpaceState.IntersectPoint(point, points) == 1 && points[0].ColliderRID == body.GetRID() && points[0].ShapeIndex == 1,
                        "Point input uses the transformed contour and logical enabled slot");
                    point.CollideWithBodies = false; point.CollideWithAreas = true;
                    Check(world.DirectSpaceState.IntersectPoint(point, points) == 1 && points[0].ColliderRID == area.GetRID(),
                        "Area point queries use the same real rounded contour");
                    point.CollideWithBodies = true; point.CollideWithAreas = false;
                    point.Position = pose * new Vector2(3, 0);
                    Check(world.DirectSpaceState.IntersectPoint(point, points) == 0, "A point near the long side remains outside the actual thin capsule");
                    var hit = world.DirectSpaceState.IntersectRay(ray);
                    if (!(hit is { } h && h.ColliderRID == body.GetRID() && h.ShapeIndex == 1 && h.Position.DistanceTo(pose * new Vector2(0, -6)) < .005f && h.Normal.Dot(-axis) > .999f))
                        throw new InvalidOperationException($"Capsule ray: hit={hit?.Position}, normal={hit?.Normal}, shape={hit?.ShapeIndex}, RID={hit?.ColliderRID}, expected={pose * new Vector2(0, -6)}, axis={axis}, radius={capsule.Radius}, height={capsule.Height}");
                    ray.From = pose * new Vector2(-10, 0); ray.To = pose * new Vector2(10, 0);
                    var sideHit = world.DirectSpaceState.IntersectRay(ray);
                    Check(sideHit is { } side && side.Position.DistanceTo(pose * new Vector2(-.75f, 0)) < .005f &&
                        side.Normal.Dot(-pose.X.Normalized()) > .999f, "Capsule ray resolves the actual straight side");
                    ray.From = pose * new Vector2(5, -10.25f); ray.To = pose * new Vector2(-1, -4.25f);
                    var roundedHit = world.DirectSpaceState.IntersectRay(ray);
                    var capNormal = pose.BasisXform(new Vector2(1, -1).Normalized());
                    var capPoint = pose * (new Vector2(0, -5.25f) + new Vector2(1, -1).Normalized() * .75f);
                    Check(roundedHit is { } curved && curved.Position.DistanceTo(capPoint) < .005f &&
                        curved.Normal.Dot(capNormal) > .999f, "Capsule cap uses the radial boundary, not its bounding box");
                    ray.From = from; ray.To = to;
                    query.Shape = circle; query.Transform = new(0, from); query.Motion = to - from;
                    Check(world.DirectSpaceState.IntersectShape(query, hits) == 1 && hits[0].ShapeIndex == 1, "Swept circle intersects the physical capsule region");
                    var cast = world.DirectSpaceState.CastMotion(query); var expected = (15 - 6 - .5f) / 30;
                    Check(MathF.Abs(cast.UnsafeFraction - expected) <= 1f / 256 + .00002f, "Circle cast brackets the face with its own radius");
                    query.Shape = small; query.Transform = new(pose.Rotation, from);
                    cast = world.DirectSpaceState.CastMotion(query); expected = (15 - 6 - 2) / 30f;
                    Check(MathF.Abs(cast.UnsafeFraction - expected) <= 1f / 256 + .00002f, "Capsule cast uses its directional support");
                    query.Margin = .25f; cast = world.DirectSpaceState.CastMotion(query);
                    Check(MathF.Abs(cast.UnsafeFraction - (15 - 6 - 2.25f) / 30f) <= 1f / 256 + .00002f,
                        "Capsule query margin expands its radius while preserving the segment");
                    query.Margin = 0;
                    Check(PhysicsServer.BodyTestMotion(moving, motion, result) && result.GetColliderRID() == body.GetRID() &&
                        result.GetColliderShape() == 1 && MathF.Abs(result.GetCollisionUnsafeFraction() - expected) <= 1f / 256 + .00002f,
                        "Body motion uses the moving capsule's exact shape");
                    query.Shape = circle; query.Motion = Vector2.Zero; query.Transform = new(0, pose * new Vector2(0, -6));
                    Check(world.DirectSpaceState.CollideShape(query, contacts) > 0, "Shape contacts publish a real penetrated face pair");
                    var rest = world.DirectSpaceState.GetRestInfo(query);
                    if (!(rest is { } r && r.ColliderRID == body.GetRID() && r.ShapeIndex == 1 && r.Normal.Dot(-axis) > .999f &&
                        MathF.Abs((r.Point - pose.Origin).Dot(axis) + 6) < .005f))
                        throw new InvalidOperationException($"Capsule rest: point={rest?.Point}, normal={rest?.Normal}, expected={pose * new Vector2(0, -6)}.");
                }
            }
            finally { PhysicsServer.FreeRID(moving); }
        }
        void Pairs()
        {
            using var world = Create(); using var first = new CapsuleShape { Radius = 1, Height = 4 };
            using var second = new CapsuleShape { Radius = 1, Height = 4 }; using var circle = new CircleShape { Radius = .5f };
            var contacts = new Vector2[8];
            Check(!PhysicsServer.ShapeCollide(world.Space, first.GetRID(), Transform.Identity, Vector2.Zero,
                second.GetRID(), new(0, new Vector2(3, 1)), Vector2.Zero, contacts, out _),
                "Parallel capsules with overlapping circumscribed disks keep their actual side gap");
            Check(PhysicsServer.ShapeCollide(world.Space, first.GetRID(), new(0, new Vector2(-5, 0)), new(10, 0),
                second.GetRID(), new(0, new Vector2(0, -10)), new(0, 10), contacts, out var count) && count > 0,
                "Independently swept capsules intersect even when their synchronized paths do not meet");
            Check(PhysicsServer.ShapeCollide(world.Space, first.GetRID(), Transform.Identity, Vector2.Zero,
                circle.GetRID(), new(0, new Vector2(1.2f, 1.2f)), Vector2.Zero, contacts, out count) && count > 0,
                "Rounded circle/capsule corner contact is geometric");
            Check(!PhysicsServer.ShapeCollide(world.Space, first.GetRID(), Transform.Identity, Vector2.Zero,
                circle.GetRID(), new(0, new Vector2(1.4f, 1.8f)), Vector2.Zero, contacts, out _),
                "A circle outside the rounded corner misses the capsule");
        }
        void Ring()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var ring = new CustomShape(Annulus.Create(5, 10)); using var capsule = new CapsuleShape { Radius = 1, Height = 4 };
            var boundary = new StaticBody(); boundary.AddChild(new CollisionShape { Shape = ring }); root.AddChild(boundary);
            var body = Dynamic(Vector2.Zero, capsule, 0); body.LinearVelocity = new(10, 0); body.ContinuousCD = CCDMode.CastShape;
            root.AddChild(body); using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            Check(!ring.Collide(Transform.Identity, capsule, Transform.Identity), "A capsule remains outside the annulus material while inside its hole");
            Check(ring.CollideWithMotion(Transform.Identity, Vector2.Zero, capsule, Transform.Identity, new(10, 0)),
                "Independent swept capsule reaches actual custom ring material");
            using var query = new PhysicsShapeQueryParameters { Shape = capsule, Motion = new(-10, 0), Margin = 0, Exclude = [body.GetRID()] };
            var fraction = world.DirectSpaceState.CastMotion(query);
            Check(MathF.Abs(fraction.UnsafeFraction - MathF.Sqrt(15) / 10) < .0001f,
                "Capsule sweep reaches the inner ring even with zero initial radial closing speed");
            var hits = new PhysicsShapeResult[4]; var pairs = new Vector2[4];
            Check(world.DirectSpaceState.IntersectShape(query, hits) == 1 && hits[0].ColliderRID == boundary.GetRID() &&
                world.DirectSpaceState.CollideShape(query, pairs) > 0 && world.DirectSpaceState.GetRestInfo(query)?.ColliderRID == boundary.GetRID(),
                "All capsule shape-query families follow the actual inner ring crossing");
            using var disk = new CapsuleShape { Radius = 1, Height = 2 }; query.Shape = disk;
            Check(MathF.Abs(world.DirectSpaceState.CastMotion(query).UnsafeFraction - .4f) < .0001f,
                "Disk-limit capsule leaves a ring hole from its exact center");
            tree.PhysicsFrame(.6);
            var maximumRadius = 0f;
            foreach (var cap in new Vector2[] { new(0, -1), new(0, 1) })
                maximumRadius = MathF.Max(maximumRadius, (body.GlobalTransform * cap).Length() + 1);
            var energy = .5f * body.LinearVelocity.LengthSquared() + body.AngularVelocity * body.AngularVelocity / (2 * state.InverseInertia);
            if (!(state.GetContactCount() > 0 && maximumRadius <= 5.301f && energy <= 50.1f))
                throw new InvalidOperationException($"Capsule ring response: radius={maximumRadius}, position={body.Position}, velocity={body.LinearVelocity}, angular={body.AngularVelocity}, energy={energy}, contacts={state.GetContactCount()}.");
        }
        void Mass()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var capsule = new CapsuleShape { Radius = .75f, Height = 6 }; using var circle = new CircleShape { Radius = .5f };
            var body = Dynamic(new(0, 0), capsule, 0); body.Mass = 3;
            ((CollisionShape)body.GetChild(0)).Position = new(2, 0);
            body.AddChild(new CollisionShape { Name = "Circle", Shape = circle, Position = new(-3, 0) }); root.AddChild(body);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            tree.PhysicsFrame(1d / 60);
            var circleArea = MathF.PI * .25f; var capsuleArea = 6.75f + MathF.PI * .5625f;
            var r = .75f; var h = 2.25f;
            var capsuleMoment = (4 * r * h * (r * r + h * h) / 3 + MathF.PI * r * r * (r * r / 2 + h * h) + 8 * h * r * r * r / 3) / capsuleArea;
            var area = capsuleArea + circleArea; var center = (capsuleArea * 2 - circleArea * 3) / area;
            var inertia = 3 * (capsuleArea * (capsuleMoment + (2 - center) * (2 - center)) + circleArea * (.125f + (-3 - center) * (-3 - center))) / area;
            Check(MathF.Abs(state.CenterOfMassLocal.X - center) < .0002f && MathF.Abs(state.CenterOfMassLocal.Y) < .0002f &&
                MathF.Abs(state.InverseInertia - 1 / inertia) < .0002f, "Compound capsule/circle mass uses actual area, centroid and parallel-axis inertia");
        }
        void CircleLimit()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var shape = new CapsuleShape { Radius = 1, Height = 2 };
            var body = Dynamic(Vector2.Zero, shape, .37f); body.Mass = 2; root.AddChild(body);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            using var point = new PhysicsPointQueryParameters { Position = new(.7f, .7f) };
            var results = new PhysicsPointResult[2]; tree.PhysicsFrame(1d / 60);
            Check(MathF.Abs(state.InverseInertia - 1) < .0002f && world.DirectSpaceState.IntersectPoint(point, results) == 1,
                "Zero-middle capsule is a circular contour with analytic disk inertia");
            point.Position = new(.8f, .8f);
            Check(world.DirectSpaceState.IntersectPoint(point, results) == 0, "Circular capsule limit rejects the square corner");
            shape.Height = 6; point.Position = body.GlobalTransform * new Vector2(0, 2.5f);
            Check(world.DirectSpaceState.IntersectPoint(point, results) == 1, "A live capsule edit immediately rebuilds its segment");
            shape.Height = 2;
            Check(world.DirectSpaceState.IntersectPoint(point, results) == 0, "A live edit back to a disk retires the old segment");
            using var line = new CapsuleShape { Radius = 0, Height = 6 }; using var circle = new CircleShape { Radius = .5f };
            var pairs = new Vector2[4];
            Check(PhysicsServer.ShapeCollide(world.Space, line.GetRID(), Transform.Identity, Vector2.Zero,
                circle.GetRID(), new(0, new Vector2(.4f, 0)), Vector2.Zero, pairs, out var count) && count > 0,
                "Zero-radius capsule retains a segment in standalone pair geometry");
            Check(!PhysicsServer.ShapeCollide(world.Space, line.GetRID(), Transform.Identity, Vector2.Zero,
                circle.GetRID(), new(0, new Vector2(.6f, 0)), Vector2.Zero, pairs, out _),
                "Zero-radius capsule rejects a circle outside the exact segment distance");
        }
        void Dynamics(bool capsules, CCDMode mode)
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var capsule = new CapsuleShape { Radius = 1, Height = 4 }; using var circle = new CircleShape { Radius = 1 };
            using var material = new PhysicsMaterial { Bounce = .5f, Friction = 0 };
            var axis = Vector2.Down.Rotated(.37f);
            var distance = mode == CCDMode.Disabled ? capsules ? 1.975f : 1.475f : 3.1f;
            var a = Dynamic(-axis * distance, capsule, .37f); var b = Dynamic(axis * distance, capsules ? capsule : circle, .37f);
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
            Check(own == 0 && all == 0, $"Capsule contact/replay frames allocated {own}/{all} B");
            Console.WriteLine($"Capsule contacts capsules={capsules}, CCD={mode} {world.PhysicsBackendName}: momentum/restitution/full impulse and 128 replay frames {own}/{all} B.");
            void Frame()
            {
                var previousEntries = entries; checkpoint.Restore();
                Check(entries == previousEntries, "Capsule checkpoint restoration is silent");
                tree.PhysicsFrame(mode == CCDMode.Disabled ? .01 : .4);
                Check(entries == previousEntries + 1, "Capsule contact entry is delivered once for each replayed frame");
                Check(a.LinearVelocity.Dot(axis) < -4.98f && b.LinearVelocity.Dot(axis) > 4.98f &&
                    (a.LinearVelocity + b.LinearVelocity).Length() < .002f, "Elastic capsule contact conserves paired linear momentum");
                var impulse = Vector2.Zero;
                for (var i = 0; i < state.GetContactCount(); i++) impulse += state.GetContactImpulse(i);
                Check(state.GetContactCount() > 0 && (impulse - (a.LinearVelocity - axis * 5)).Length() < .02f,
                    "Complete step contact impulse explains the actual change of momentum");
            }
        }
        void AngularContact()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var capsule = new CapsuleShape { Radius = 1, Height = 4 }; using var circle = new CircleShape { Radius = 1.2f };
            using var material = new PhysicsMaterial { Friction = 0, Bounce = 0 };
            var body = Dynamic(Vector2.Zero, capsule, 0); body.Mass = 2; body.Inertia = 20;
            body.LinearVelocity = new(0, -20); body.PhysicsMaterialOverride = material; root.AddChild(body);
            var wall = new StaticBody { Position = new(1.5f, -2.5f), PhysicsMaterialOverride = material };
            wall.AddChild(new CollisionShape { Shape = circle }); root.AddChild(wall);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            using var history = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            Frame(); for (var i = 0; i < 128; i++) Frame(); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Capsule angular contact/replay allocated {own}/{all} B");
            Console.WriteLine($"Capsule angular contact {world.PhysicsBackendName}: lever/energy/full impulses, 128 replay frames {own}/{all} B.");
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
                if (!(state.GetContactCount() > 0 && body.AngularVelocity < -.01f &&
                    (impulse - 2 * (body.LinearVelocity - new Vector2(0, -20))).Length() < .03f &&
                    MathF.Abs(moment - 20 * body.AngularVelocity) < momentLimit))
                    throw new InvalidOperationException($"Corner contact: count={state.GetContactCount()}, velocity={body.LinearVelocity}, angular={body.AngularVelocity}, impulse={impulse}, moment={moment}, expected={20 * body.AngularVelocity}, limit={momentLimit}.");
                Check(body.LinearVelocity.LengthSquared() + 10 * body.AngularVelocity * body.AngularVelocity <= 400.1f,
                    "Inelastic capsule corner response does not add energy");
            }
        }
        void Continuous()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var wallShape = new RectangleShape { Size = new(.5f, 20) }; using var boxShape = new CapsuleShape { Radius = 1, Height = 4 };
            var wall = new StaticBody { Position = new(10, 0) }; wall.AddChild(new CollisionShape { Shape = wallShape }); root.AddChild(wall);
            var body = Dynamic(Vector2.Zero, boxShape, 0); body.LinearVelocity = new(1000, 0); body.ContinuousCD = CCDMode.CastShape; root.AddChild(body);
            using var tree = new SceneTree(root); tree.PhysicsFrame(.02);
            if (!(body.Position.X <= 8.75f + .501f && MathF.Abs(body.LinearVelocity.X) < .1f))
                throw new InvalidOperationException($"Thin-wall capsule: position={body.Position}, velocity={body.LinearVelocity}, angular={body.AngularVelocity}.");
        }
        void AngularCCD()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var bar = new CapsuleShape { Radius = .25f, Height = 320 }; using var circle = new CircleShape { Radius = .25f };
            using var material = new PhysicsMaterial { Friction = 0, Bounce = 0 };
            var body = Dynamic(Vector2.Zero, bar, 0); body.Mass = 2; body.Inertia = 20000;
            body.AngularVelocity = 30; body.ContinuousCD = CCDMode.CastShape; body.PhysicsMaterialOverride = material; root.AddChild(body);
            var peer = new StaticBody { Position = new Vector2(0, 160).Rotated(.225f), PhysicsMaterialOverride = material };
            peer.AddChild(new CollisionShape { Shape = circle }); root.AddChild(peer);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            tree.PhysicsFrame(.015);
            Check(state.GetContactCount() > 0 && body.AngularVelocity < 29.9f && body.Position.IsFinite() &&
                body.LinearVelocity.LengthSquared() + 10000 * body.AngularVelocity * body.AngularVelocity <= 9180000,
                "Rotational capsule CCD sees the intermediate contour arc and preserves the energy ceiling");
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
