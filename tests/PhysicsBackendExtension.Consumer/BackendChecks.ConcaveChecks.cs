using Electron2D;

public static partial class BackendChecks
{
    public static void RunConcaveGeometry(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("ConcaveSolver", () => new Solver());
        try
        {
            Queries(); Mass(); Corner(); MixedMass(); Limits(); QuerySource();
            foreach (var mode in new[] { CCDMode.Disabled, CCDMode.CastRay, CCDMode.CastShape })
            { Dynamics(false, mode); Dynamics(true, mode); }
            AngularContact(); Continuous(); AngularCCD(); Pairs(); CappedPairs();

            Console.WriteLine($"Concave geometry {backend?.ToString() ?? "registered"}, threaded={threaded}: transformed contours, six queries, mass, contacts, CCD and zero warm frames passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        World Create() => backend is { } selected ? new World(selected) : new World("ConcaveSolver");

        void Queries()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var segment = Frame(6); using var circle = new CircleShape { Radius = .5f };
            using var small = new RectangleShape { Size = new(2, 2) };
            var body = new StaticBody { Position = new(40, 10), Rotation = .37f };
            body.AddChild(new CollisionShape { Name = "Disabled", Shape = circle, Disabled = true });
            var slot = new CollisionShape { Name = "Segment", Shape = segment, Position = new(3, -1), Rotation = .2f };
            body.AddChild(slot); root.AddChild(body);
            var areaPose = body.Transform * slot.Transform;
            var area = new Area { Position = areaPose.Origin, Rotation = areaPose.Rotation };
            area.CollisionMask = 2; area.AddChild(new CollisionShape { Shape = segment }); root.AddChild(area);
            var peer = new StaticBody { Name = "SensorPeer", Position = areaPose * new Vector2(-5.75f, -5.75f), CollisionLayer = 2, CollisionMask = 2 };
            peer.AddChild(new CollisionShape { Shape = circle }); root.AddChild(peer);
            var areaEntries = 0; var areaShapes = 0; area.BodyEntered += other => { if (other == peer) areaEntries++; };
            area.BodyShapeEntered += (rid, _, remote, local) => { Check(rid == peer.GetRID() && remote == 0 && local == 0, "Area corner payload retains one logical shape pair"); areaShapes++; };
            using var tree = new SceneTree(root);
            var moving = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodySetMode(moving, PhysicsServer.BodyMode.Kinematic); PhysicsServer.BodyAddShape(moving, small.GetRID());
                PhysicsServer.BodySetTransform(moving, new(0, new Vector2(-1000, 0))); PhysicsServer.BodySetSpace(moving, world.Space);
                var pose = body.GlobalTransform * slot.Transform; var axis = pose.X.Normalized();
                var from = pose * new Vector2(-15, 0); var to = pose * new Vector2(15, 0);
                using var point = new PhysicsPointQueryParameters { CollisionMask = 1 }; using var ray = new PhysicsRayQueryParameters { From = from, To = to, CollisionMask = 1 };
                using var query = new PhysicsShapeQueryParameters { Shape = circle, Transform = new(0, from), Motion = to - from, Margin = 0, CollisionMask = 1 };
                using var motion = new PhysicsTestMotionParameters { From = new(pose.Rotation, from), Motion = to - from, Margin = 0 };
                using var result = new PhysicsTestMotionResult();
                var points = new PhysicsPointResult[4]; var hits = new PhysicsShapeResult[4]; var contacts = new Vector2[8];
                Verify(); for (var i = 0; i < 128; i++) Verify(); Collect();
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Verify();
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"Segment query/full frames allocated {own}/{all} B");
                Console.WriteLine($"Concave queries {world.PhysicsBackendName}: 128 full six-query frames {own}/{all} B.");
                peer.Position = pose.Origin; tree.PhysicsFrame(1d / 60);
                Check(!area.OverlapsBody(peer), "A body inside closed concave bounds overlaps no hollow edge");
                peer.Position = pose * new Vector2(-5.75f, -5.75f); tree.PhysicsFrame(1d / 60);
                Check(area.OverlapsBody(peer) && areaEntries == 2 && areaShapes == 2, "Returning to the corner delivers one new logical overlap");
                query.Shape = circle; query.Transform = new(0, pose * new Vector2(-5.75f, -5.6f)); query.Motion = Vector2.Zero;
                var cornerRest = world.DirectSpaceState.GetRestInfo(query);
                if (!(cornerRest is { } corner && corner.ColliderRID == body.GetRID() && corner.ShapeIndex == 1 &&
                    corner.Normal.Dot(axis) > .999f && MathF.Abs((corner.Point - pose.Origin).Dot(axis) + 6) < .01f))
                    throw new InvalidOperationException($"Corner rest: point={cornerRest?.Point}, normal={cornerRest?.Normal}, expected-axis={axis}, projected={(cornerRest?.Point - pose.Origin)?.Dot(axis)}, shape={cornerRest?.ShapeIndex}.");
                var deeper = PhysicsServer.BodyCreate();
                try
                {
                    PhysicsServer.BodySetMode(deeper, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(deeper, segment.GetRID());
                    PhysicsServer.BodySetTransform(deeper, new(pose.Rotation, pose.Origin - axis * .2f)); PhysicsServer.BodySetSpace(deeper, world.Space);
                    query.Shape = circle; query.Transform = new(0, pose * new Vector2(-6.25f, 0)); query.Motion = Vector2.Zero;
                    Check(world.DirectSpaceState.GetRestInfo(query)?.ColliderRID == deeper, "Rest selection prefers the genuinely deeper segment");
                    query.Exclude = [deeper];
                    Check(world.DirectSpaceState.GetRestInfo(query)?.ColliderRID == body.GetRID(), "Explicit exclusions preserve the next valid segment");
                    query.Exclude = []; query.CollisionMask = 4;
                    Check(world.DirectSpaceState.IntersectShape(query, hits) == 0, "Segment query masks reject both default layers");
                    query.CollisionMask = 1;
                }
                finally { PhysicsServer.FreeRID(deeper); }

                void Verify()
                {
                    tree.PhysicsFrame(1d / 60);
                    Check(area.OverlapsBody(peer) && areaEntries == 1 && areaShapes == 1, "Segment Area monitoring retains one actual overlap transition");
                    point.Position = pose.Origin;
                    Check(world.DirectSpaceState.IntersectPoint(point, points) == 0, "A zero-width segment has no filled point-query interior");
                    point.CollisionMask = 2; point.Position = peer.Position;
                    Check(world.DirectSpaceState.IntersectPoint(point, points) == 1 && points[0].ColliderRID == peer.GetRID(),
                        "Point-query layer filtering still resolves the actual circle sensor peer");
                    point.CollisionMask = 1; point.Position = pose.Origin;
                    point.CollideWithBodies = false; point.CollideWithAreas = true;
                    Check(world.DirectSpaceState.IntersectPoint(point, points) == 0, "A segment Area also has no filled point interior");
                    point.CollideWithBodies = true; point.CollideWithAreas = false;
                    point.Position = pose * new Vector2(0, 0);
                    Check(world.DirectSpaceState.IntersectPoint(point, points) == 0, "A point near the long side remains outside the actual thin segment");
                    var hit = world.DirectSpaceState.IntersectRay(ray);
                    Check(hit is { } h && h.ColliderRID == body.GetRID() && h.ShapeIndex == 1 && h.Position.DistanceTo(pose * new Vector2(-6, 0)) < .002f && h.Normal.Dot(-axis) > .999f,
                        "Ray hit point and normal follow the actual rotated face");
                    query.Shape = circle; query.Transform = new(0, from); query.Motion = to - from;
                    Check(world.DirectSpaceState.IntersectShape(query, hits) == 1 && hits[0].ShapeIndex == 1, "Swept circle intersects the physical rectangular region");
                    var cast = world.DirectSpaceState.CastMotion(query); var expected = (15 - 6 - .5f) / 30;
                    Check(MathF.Abs(cast.UnsafeFraction - expected) <= 1f / 256 + .00002f, "Circle cast brackets the face with its own radius");
                    query.Shape = small; query.Transform = new(pose.Rotation, from);
                    cast = world.DirectSpaceState.CastMotion(query); expected = (15 - 6 - 1) / 30f;
                    Check(MathF.Abs(cast.UnsafeFraction - expected) <= 1f / 256 + .00002f, "Segment cast uses its directional support");
                    Check(PhysicsServer.BodyTestMotion(moving, motion, result) && result.GetColliderRID() == body.GetRID() &&
                        result.GetColliderShape() == 1 && MathF.Abs(result.GetCollisionUnsafeFraction() - expected) <= 1f / 256 + .00002f,
                        "Body motion uses the moving segment's exact shape");
                    query.Shape = circle; query.Motion = Vector2.Zero; query.Transform = new(0, pose * new Vector2(-6.25f, 0));
                    Check(world.DirectSpaceState.CollideShape(query, contacts) > 0, "Shape contacts publish a real penetrated face pair");
                    var rest = world.DirectSpaceState.GetRestInfo(query);
                    Check(rest is { } r && r.ColliderRID == body.GetRID() && r.ShapeIndex == 1 && r.Normal.Dot(-axis) > .999f &&
                        MathF.Abs((r.Point - pose.Origin).Dot(axis) + 6) < .002f, "Rest geometry retains boundary point and outward normal");
                }
            }
            finally { PhysicsServer.FreeRID(moving); }
        }
        void Pairs()
        {
            using var world = Create(); using var frame = Frame(6); using var circle = new CircleShape { Radius = .5f };
            using var other = Frame(10); var output = new Vector2[16];
            Check(!PhysicsServer.ShapeCollide(world.Space, frame.GetRID(), Transform.Identity, new(100, 0),
                circle.GetRID(), Transform.Identity, Vector2.Zero, output, out _), "A circle inside concave bounds does not collide with hollow material or concave motion");
            Check(PhysicsServer.ShapeCollide(world.Space, frame.GetRID(), Transform.Identity, new(100, 0),
                circle.GetRID(), Transform.Identity, new(10, 0), output, out var count) && count > 0,
                "Convex counterpart motion crosses the actual hollow edge");
            Check(!PhysicsServer.ShapeCollide(world.Space, frame.GetRID(), Transform.Identity, Vector2.Zero,
                other.GetRID(), Transform.Identity, Vector2.Zero, output, out _), "Two concave resources are excluded from standalone collision");
            Check(PhysicsServer.ShapeCollide(world.Space, circle.GetRID(), Transform.Identity, new(10, 0),
                frame.GetRID(), Transform.Identity, new(-100, 0), output, out count) && count > 0,
                "Concave motion is ignored in either operand order");
        }
        void CappedPairs()
        {
            using var world = Create(); var edges = new Vector2[40];
            for (var i = 0; i < 20; i++) { var x = 20 - i; edges[2 * i] = new(x, -2); edges[2 * i + 1] = new(x, 2); }
            using var source = new ConcavePolygonShape { Segments = edges }; using var circle = new CircleShape { Radius = 24 };
            var output = new Vector2[8];
            Check(PhysicsServer.ShapeCollide(world.Space, source.GetRID(), Transform.Identity, Vector2.Zero,
                circle.GetRID(), Transform.Identity, Vector2.Zero, output, out var count) && count == 4,
                "Concave standalone contacts respect the supplied pair capacity");
            for (var i = 0; i < count; i++)
                Check(output[2 * i].DistanceTo(output[2 * i + 1]) >= 20 - .002f,
                    "Capped concave contacts retain deepest late edge pairs");
            if (backend is null)
            {
                using var ring = new CustomShape(Annulus.Create(5, 10)); using var line = new ConcavePolygonShape { Segments = [new(-1, 0), new(1, 0)] };
                Check(!ring.Collide(Transform.Identity, line, Transform.Identity), "Custom annulus sees an actual concave edge inside its hole");
                Check(ring.CollideWithMotion(Transform.Identity, Vector2.Zero, line, new(0, new Vector2(6, 0)), new(-100, 0)),
                    "Custom callback keeps concave material and ignores its supplied motion");
            }
        }
        void Corner()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var frame = Frame(6); using var circle = new CircleShape { Radius = 1 };
            using var material = new PhysicsMaterial { Friction = 0, Bounce = 0 };
            var boundary = new StaticBody { PhysicsMaterialOverride = material };
            boundary.AddChild(new CollisionShape { Name = "Disabled", Shape = circle, Disabled = true });
            boundary.AddChild(new CollisionShape { Name = "Frame", Shape = frame }); root.AddChild(boundary);
            var body = new RigidBody
            {
                Name = "CornerBody",
                Position = new(-5.5f, -5.5f),
                LinearVelocity = new(-5, -5),
                GravityScale = 0,
                CanSleep = false,
                LockRotation = true,
                ContactMonitor = true,
                MaxContactsReported = 8,
                PhysicsMaterialOverride = material,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            body.AddChild(new CollisionShape { Name = "Disabled", Shape = circle, Disabled = true });
            body.AddChild(new CollisionShape { Name = "Circle", Shape = circle }); root.AddChild(body);
            var entered = 0; var shapeEntered = 0; body.BodyEntered += _ => entered++;
            body.BodyShapeEntered += (rid, _, remote, local) => { Check(rid == boundary.GetRID() && remote == 1 && local == 1, "Corner shape events retain logical slots"); shapeEntered++; };
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            FrameStep(); for (var i = 0; i < 128; i++) FrameStep(); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) FrameStep();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Concave corner/replay allocated {own}/{all} B");
            Console.WriteLine($"Concave corner {world.PhysicsBackendName}: two features/one event, full impulse and 128 replay frames {own}/{all} B.");
            void FrameStep()
            {
                var prior = entered; var priorShape = shapeEntered; checkpoint.Restore();
                Check(entered == prior && shapeEntered == priorShape, "Corner restore is silent"); tree.PhysicsFrame(1d / 2048);
                var impulse = Vector2.Zero; var horizontal = false; var vertical = false;
                for (var i = 0; i < state.GetContactCount(); i++)
                {
                    Check(state.GetContactLocalShape(i) == 1 && state.GetContactColliderShape(i) == 1, "Every corner feature retains one logical shape pair");
                    var normal = state.GetContactLocalNormal(i); horizontal |= MathF.Abs(normal.X) > .999f; vertical |= MathF.Abs(normal.Y) > .999f;
                    impulse += state.GetContactImpulse(i);
                }
                Check(state.GetContactCount() >= 2 && horizontal && vertical && entered == prior + 1 && shapeEntered == priorShape + 1,
                    "Independent edge contacts publish separate normals and one monitored transition");
                Check((impulse - (body.LinearVelocity - new Vector2(-5, -5))).Length() < .02f && body.LinearVelocity.Length() < .002f,
                    "Two concave edges explain the entire stopped momentum");
            }
        }
        void Mass()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var shape = new ConcavePolygonShape { Segments = [new(0, 0), new(4, 0), new(-3, -4), new(-3, 4)] };
            var body = Dynamic(Vector2.Zero, shape, 0); body.Mass = 3; root.AddChild(body);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            tree.PhysicsFrame(1d / 60);
            var center = -4f / 3; var inertia = 3 * (4 * (16f / 12 + (2 - center) * (2 - center)) + 8 * (64f / 12 + (-3 - center) * (-3 - center))) / 12;
            Check(MathF.Abs(state.CenterOfMassLocal.X - center) < .0002f && MathF.Abs(state.CenterOfMassLocal.Y) < .0002f &&
                MathF.Abs(state.InverseMass - 1f / 3) < .0002f && MathF.Abs(state.InverseInertia - 1 / inertia) < .0002f,
                "Concave edge lengths own rod centroid and parallel-axis inertia within one slot");
            body.ApplyTorqueImpulse(inertia * 2); tree.PhysicsFrame(1d / 60);
            Check(MathF.Abs(body.AngularVelocity - 2) < .002f, "Concave rod mass produces actual torque response");
        }
        void MixedMass()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var segment = new ConcavePolygonShape { Segments = [new(-10, 0), new(10, 0), new(-10, 10), new(10, 10)] }; using var circle = new CircleShape { Radius = 2 };
            var body = Dynamic(Vector2.Zero, segment, 0); body.Mass = 3;
            ((CollisionShape)body.GetChild(0)).Position = new(20, 0);
            body.AddChild(new CollisionShape { Name = "Solid", Shape = circle, Position = new(-3, 0) }); root.AddChild(body);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            tree.PhysicsFrame(1d / 60);
            Check(state.CenterOfMassLocal.DistanceTo(new(-3, 0)) < .0002f && MathF.Abs(state.InverseInertia - 1f / 6) < .0002f,
                "Positive-area solids own mass before zero-area rods contribute");
        }
        void Limits()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var segment = new ConcavePolygonShape { Segments = [new(-5, 0), new(5, 0), new(-5, 10), new(5, 10)] };
            var body = new StaticBody(); body.AddChild(new CollisionShape { Shape = segment }); root.AddChild(body);
            using var tree = new SceneTree(root); tree.PhysicsFrame(1d / 60);
            using var ray = new PhysicsRayQueryParameters { From = new(-3, 0), To = new(3, 0) };
            Check(world.DirectSpaceState.IntersectRay(ray) is null, "A collinear segment ray has no transverse surface crossing");
            ray.From = new(0, 3); ray.To = new(0, -3);
            Check(world.DirectSpaceState.IntersectRay(ray) is { } hit && hit.Normal.Dot(Vector2.Down) > .999f,
                "A segment ray resolves its opposite two-sided normal");
            segment.Segments = [new(-.1f, 0), new(.1f, 0)];
            using var circle = new CircleShape { Radius = .05f }; using var query = new PhysicsShapeQueryParameters { Shape = circle, Transform = new(0, new Vector2(.08f, 0)), Margin = 0 };
            var hits = new PhysicsShapeResult[2];
            ray.From = new(.08f, 3); ray.To = new(.08f, -3);
            Check(world.DirectSpaceState.IntersectRay(ray) is null, "A short attached segment uses the accepted midpoint ray fixture");
            query.Transform = Transform.Identity;
            Check(world.DirectSpaceState.IntersectShape(query, hits) == 1, "The short segment midpoint remains queryable by actual shapes");
            segment.Segments = [new(3, 0), new(3, 0)]; query.Transform = new(0, new Vector2(3, 0));
            Check(world.DirectSpaceState.IntersectShape(query, hits) == 1, "A live coincident-endpoint edit retains valid point geometry");
            segment.Segments = [];
            Check(world.DirectSpaceState.IntersectShape(query, hits) == 0 && world.DirectSpaceState.IntersectRay(ray) is null,
                "Empty concave geometry retires every physical leaf");
            segment.Segments = [new(3, -2), new(3, 2)];
            Check(world.DirectSpaceState.IntersectShape(query, hits) == 1 && hits[0].ColliderRID == body.GetRID(),
                "Concave replacement preserves the original logical collider identity");
        }
        void QuerySource()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var circle = new CircleShape { Radius = 1 };
            using var source = new ConcavePolygonShape { Segments = [new(-.5f, -2), new(-.5f, 2), new(.5f, -2), new(.5f, 2)] };
            var target = new StaticBody(); target.AddChild(new CollisionShape { Shape = circle }); root.AddChild(target);
            using var tree = new SceneTree(root); tree.PhysicsFrame(1d / 60);
            using var query = new PhysicsShapeQueryParameters { Shape = source, Margin = 0 };
            var hits = new PhysicsShapeResult[4]; var contacts = new Vector2[8];
            Check(world.DirectSpaceState.IntersectShape(query, hits) == 1 && hits[0].ColliderRID == target.GetRID(),
                "Concave query source deduplicates one collider touched by two input edges");
            Check(world.DirectSpaceState.CollideShape(query, contacts) >= 2 && world.DirectSpaceState.GetRestInfo(query)?.ColliderRID == target.GetRID(),
                "Concave query source preserves actual edge contact and rest data");
            query.Transform = new(0, new Vector2(-5, 0)); query.Motion = new(10, 0);
            var motion = world.DirectSpaceState.CastMotion(query);
            Check(motion == (1, 1) && world.DirectSpaceState.IntersectShape(query, hits) == 0 && world.DirectSpaceState.CollideShape(query, contacts) == 0 && world.DirectSpaceState.GetRestInfo(query) is null,
                "Concave query displacement cannot manufacture a swept edge contact");
            source.Segments = [];
            Check(world.DirectSpaceState.IntersectShape(query, hits) == 0 && world.DirectSpaceState.CollideShape(query, contacts) == 0 &&
                world.DirectSpaceState.GetRestInfo(query) is null && world.DirectSpaceState.CastMotion(query).UnsafeFraction == 1,
                "Empty concave query source has no manufactured primitive");
        }
        void Dynamics(bool boxes, CCDMode mode)
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var segment = new ConcavePolygonShape { Segments = [new(-2, 0), new(2, 0), new(-2, 10), new(2, 10)] }; using var circle = new CircleShape { Radius = 1 };
            using var box = new RectangleShape { Size = new(4, 2) };
            using var material = new PhysicsMaterial { Bounce = .5f, Friction = 0 };
            var axis = Vector2.Down.Rotated(.37f);
            var distance = mode == CCDMode.Disabled ? .475f : 2.1f;
            var a = Dynamic(-axis * distance, segment, .37f); var b = Dynamic(axis * distance, boxes ? box : circle, .37f);
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
            Check(own == 0 && all == 0, $"Segment contact/replay frames allocated {own}/{all} B");
            Console.WriteLine($"Concave contacts boxes={boxes}, CCD={mode} {world.PhysicsBackendName}: momentum/restitution/full impulse and 128 replay frames {own}/{all} B.");
            void Frame()
            {
                var previousEntries = entries; checkpoint.Restore();
                Check(entries == previousEntries, "Segment checkpoint restoration is silent");
                tree.PhysicsFrame(mode == CCDMode.Disabled ? .01 : .4);
                Check(entries == previousEntries + 1, "Segment contact entry is delivered once for each replayed frame");
                Check(a.LinearVelocity.Dot(axis) < -4.98f && b.LinearVelocity.Dot(axis) > 4.98f &&
                    (a.LinearVelocity + b.LinearVelocity).Length() < .002f, "Elastic segment contact conserves paired linear momentum");
                var impulse = Vector2.Zero;
                for (var i = 0; i < state.GetContactCount(); i++) impulse += state.GetContactImpulse(i);
                Check(state.GetContactCount() > 0 && (impulse - (a.LinearVelocity - axis * 5)).Length() < .02f,
                    "Complete step contact impulse explains the actual change of momentum");
            }
        }
        void AngularContact()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var segment = new ConcavePolygonShape { Segments = [new(-2, 0), new(2, 0), new(-2, 10), new(2, 10)] }; using var circle = new CircleShape { Radius = 1.2f };
            using var material = new PhysicsMaterial { Friction = 0, Bounce = 0 };
            var body = Dynamic(Vector2.Zero, segment, 0); body.Mass = 2; body.Inertia = 20;
            body.LinearVelocity = new(0, -20); body.PhysicsMaterialOverride = material; root.AddChild(body);
            var wall = new StaticBody { Position = new(1.5f, -.9f), PhysicsMaterialOverride = material };
            wall.AddChild(new CollisionShape { Shape = circle }); root.AddChild(wall);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            using var history = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            Frame(); for (var i = 0; i < 128; i++) Frame(); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Concave angular contact/replay allocated {own}/{all} B");
            Console.WriteLine($"Concave angular contact {world.PhysicsBackendName}: lever/energy/full impulses, 128 replay frames {own}/{all} B.");
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
                    (impulse - 2 * (body.LinearVelocity - new Vector2(0, -20))).Length() < .03f &&
                    MathF.Abs(moment - 20 * body.AngularVelocity) < momentLimit))
                    throw new InvalidOperationException($"Corner contact: count={state.GetContactCount()}, velocity={body.LinearVelocity}, angular={body.AngularVelocity}, impulse={impulse}, moment={moment}, expected={20 * body.AngularVelocity}, limit={momentLimit}.");
                Check(body.LinearVelocity.LengthSquared() + 10 * body.AngularVelocity * body.AngularVelocity <= 400.1f,
                    "Inelastic segment corner response does not add energy");
            }
        }
        void Continuous()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var wallShape = new RectangleShape { Size = new(.5f, 20) }; using var boxShape = new ConcavePolygonShape { Segments = [new(0, -1), new(0, 1), new(-2, -1), new(-2, 1)] };
            var wall = new StaticBody { Position = new(10, 0) }; wall.AddChild(new CollisionShape { Shape = wallShape }); root.AddChild(wall);
            var body = Dynamic(Vector2.Zero, boxShape, 0); body.LinearVelocity = new(1000, 0); body.ContinuousCD = CCDMode.CastShape; root.AddChild(body);
            using var tree = new SceneTree(root); tree.PhysicsFrame(.02);
            Check(body.Position.X <= 9.75f + .501f && MathF.Abs(body.LinearVelocity.X) < .1f, "Continuous segment motion stops at the thin wall");
        }
        void AngularCCD()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var bar = new ConcavePolygonShape { Segments = [new(0, -160), new(0, 160), new(-20, -10), new(-20, 10)] }; using var circle = new CircleShape { Radius = .25f };
            using var material = new PhysicsMaterial { Friction = 0, Bounce = 0 };
            var body = Dynamic(Vector2.Zero, bar, 0); body.Mass = 2; body.Inertia = 20000;
            body.AngularVelocity = 30; body.ContinuousCD = CCDMode.CastShape; body.PhysicsMaterialOverride = material; root.AddChild(body);
            var peer = new StaticBody { Position = new Vector2(0, 160).Rotated(.225f), PhysicsMaterialOverride = material };
            peer.AddChild(new CollisionShape { Shape = circle }); root.AddChild(peer);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            tree.PhysicsFrame(.015);
            Check(state.GetContactCount() > 0 && body.AngularVelocity < 29.9f && body.Position.IsFinite() &&
                body.LinearVelocity.LengthSquared() + 10000 * body.AngularVelocity * body.AngularVelocity <= 9180000,
                "Rotational segment CCD sees the intermediate contour arc and preserves the energy ceiling");
        }
        static ConcavePolygonShape Frame(float size) => new()
        {
            Segments = [new(-size, -size), new(-size, size),
            new(size, -size), new(size, size), new(-size, -size), new(size, -size), new(-size, size), new(size, size)]
        };
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
