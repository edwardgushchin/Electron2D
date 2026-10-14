using Electron2D;

public static partial class BackendChecks
{
    public static void RunDirectedGeometry(PhysicsServer.Backend? backend = null, bool threaded = false, bool angularOnly = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("DirectedSolver", () => new Solver());
        try
        {
            if (angularOnly) { AngularCCD(); return; }
            Queries(); Motion(); Filters(); Mass(); Pairs();
            for (var family = 0; family < 6; family++)
            {
                foreach (var mode in new[] { CCDMode.Disabled, CCDMode.CastRay, CCDMode.CastShape }) Dynamics(family, mode, false);
                Dynamics(family, CCDMode.CastShape, true);
            }
            Slope(false); Slope(true); Coupled(); Nonreciprocal(false); Nonreciprocal(true); Continuous(); AngularCCD(); Lifecycle(); Character(); Sleep();
            Console.WriteLine($"Directed geometry {backend?.ToString() ?? "registered"}, threaded={threaded}: surface entry, six queries, mass, contacts, CCD and zero warm frames passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        World Create() => backend is { } selected ? new World(selected) : new World("DirectedSolver");
        void Queries()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var ray = new SeparationRayShape { Length = 15 }; using var shortRay = new SeparationRayShape { Length = 5 };
            using var floorShape = new RectangleShape { Size = new(200, 10) }; using var circle = new CircleShape { Radius = 2 };
            var floor = new StaticBody { Position = new(40, 10), Rotation = .37f };
            floor.AddChild(new CollisionShape { Name = "Disabled", Shape = circle, Disabled = true });
            var slot = new CollisionShape { Name = "Floor", Shape = floorShape, Position = new(3, -1), Rotation = .2f }; floor.AddChild(slot); root.AddChild(floor);
            var pose = floor.Transform * slot.Transform; var axis = pose.Y.Normalized(); var from = pose * new Vector2(0, -15);
            var holder = new StaticBody { Name = "Ray", Position = from, Rotation = pose.Rotation, CollisionLayer = 4, CollisionMask = 4 };
            holder.AddChild(new CollisionShape { Shape = ray }); root.AddChild(holder);
            var area = new Area { Position = from, Rotation = pose.Rotation, CollisionMask = 2 };
            area.AddChild(new CollisionShape { Shape = ray }); root.AddChild(area);
            var peer = new StaticBody { Name = "Peer", Position = pose * new Vector2(0, -5), CollisionLayer = 2, CollisionMask = 2 };
            peer.AddChild(new CollisionShape { Shape = circle }); root.AddChild(peer);
            var entries = 0; var shapes = 0; area.BodyEntered += _ => entries++;
            area.BodyShapeEntered += (rid, _, remote, local) => { Check(rid == peer.GetRID() && remote == 0 && local == 0, "Directed Area retains the actual logical pair"); shapes++; };
            using var tree = new SceneTree(root);
            using var query = new PhysicsShapeQueryParameters { Shape = ray, Transform = new(pose.Rotation, from), Margin = 0, CollisionMask = 1 };
            using var point = new PhysicsPointQueryParameters { Position = from + axis * 10, CollisionMask = 4 };
            using var line = new PhysicsRayQueryParameters { From = point.Position - pose.X * 10, To = point.Position + pose.X * 10, CollisionMask = 4 };
            var hits = new PhysicsShapeResult[4]; var points = new PhysicsPointResult[4]; var contacts = new Vector2[8];
            Warm(Verify, "queries/Area");
            query.Transform = new(pose.Rotation, pose * new Vector2(0, -3)); Check(world.DirectSpaceState.IntersectShape(query, hits) == 0, "Filled origin containment rejects the entire ray");
            query.Transform = new(pose.Rotation + Mathf.Pi, pose * new Vector2(0, 15));
            Check(world.DirectSpaceState.GetRestInfo(query) is { } reverse && reverse.Normal.Dot(axis) > .999f, "Reversed directed query finds opposite surface");
            query.Transform = new(pose.Rotation, from); ray.Length = 5; query.Motion = pose.X * 200;
            Check(world.DirectSpaceState.IntersectShape(query, hits) == 0 && world.DirectSpaceState.CastMotion(query).UnsafeFraction == 1, "Transverse ray motion does not create a swept solid");
            query.Motion = Vector2.Zero; query.Margin = 6; Check(world.DirectSpaceState.GetRestInfo(query) is not null, "Margin extends the directed endpoint along its axis");
            query.Margin = 0; ray.Length = 15; query.Exclude = [floor.GetRID()]; Check(world.DirectSpaceState.IntersectShape(query, hits) == 0, "Directed query exclusions apply");
            query.Exclude = []; query.CollisionMask = 4; Check(world.DirectSpaceState.IntersectShape(query, hits) == 0, "Ray/ray query pairs never contact");
            query.Shape = circle; query.Transform = new(0, pose * new Vector2(-20, -10)); query.Motion = pose.X * 40;
            Check(world.DirectSpaceState.IntersectShape(query, hits) == 1 && hits[0].ColliderRID == holder.GetRID(), "Moving ordinary source crosses a stationary ray");
            var cast = world.DirectSpaceState.CastMotion(query); Check(MathF.Abs(cast.UnsafeFraction - .45f) < 1f / 256 + .00005f, "Reverse cast uses the swept circle boundary");
            Check(world.DirectSpaceState.GetRestInfo(query) is not null, "Reverse directed rest retains target identity");
            peer.Position = from; tree.PhysicsFrame(1d / 60); Check(!area.OverlapsBody(peer), "Area ray ignores a peer containing its origin");
            void Verify()
            {
                tree.PhysicsFrame(1d / 60); Check(entries == 1 && shapes == 1 && area.OverlapsBody(peer), "Directed Area emits one surface-entry event");
                Check(world.DirectSpaceState.IntersectPoint(point, points) == 0 && world.DirectSpaceState.IntersectRay(line) is null, "Directed fixtures are invisible to point and ray queries");
                query.Shape = ray; query.Transform = new(pose.Rotation, from); query.Motion = Vector2.Zero;
                Check(world.DirectSpaceState.IntersectShape(query, hits) == 1 && hits[0].ColliderRID == floor.GetRID() && hits[0].ShapeIndex == 1, "Directed shape entry preserves disabled slot gap");
                Check(world.DirectSpaceState.CollideShape(query, contacts) == 1 && contacts[0].DistanceTo(from + axis * 15) < .003f && contacts[1].DistanceTo(from + axis * 10) < .003f, "Contact pair contains the ray endpoint and target boundary");
                var rest = world.DirectSpaceState.GetRestInfo(query);
                Check(rest is { } r && r.ColliderRID == floor.GetRID() && r.Normal.Dot(-axis) > .999f && r.Point.DistanceTo(from + axis * 10) < .003f, "Rest publishes the directed normal and surface point");
                query.Shape = shortRay; query.Motion = axis * 20; var f = world.DirectSpaceState.CastMotion(query);
                Check(MathF.Abs(f.UnsafeFraction - .25f) < 1f / 256 + .00005f, "Axial motion extends the endpoint to first entry");
                query.Shape = ray; query.Motion = Vector2.Zero;
            }
        }
        void Motion()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var ray = new SeparationRayShape { Length = 5 };
            using var floorShape = new RectangleShape { Size = new(200, 10) };
            var floor = new StaticBody { Position = new(0, 25) }; floor.AddChild(new CollisionShape { Shape = floorShape }); root.AddChild(floor);
            using var tree = new SceneTree(root); var body = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Kinematic); PhysicsServer.BodyAddShape(body, ray.GetRID()); PhysicsServer.BodySetSpace(body, world.Space);
                using var test = new PhysicsTestMotionParameters { Motion = new(0, 30), Margin = 0 }; using var result = new PhysicsTestMotionResult();
                Check(!PhysicsServer.BodyTestMotion(body, test, result), "Non-sliding rays require explicit motion policy"); test.CollideSeparationRay = true;
                Check(PhysicsServer.BodyTestMotion(body, test, result) && MathF.Abs(result.GetCollisionUnsafeFraction() - .5f) < 1f / 256 + .00005f, "Enabled ray motion brackets the authored endpoint");
                ray.SlideOnSlope = true; test.CollideSeparationRay = false;
                Check(PhysicsServer.BodyTestMotion(body, test, result), "Sliding ray participates without explicit flag");
                ray.SlideOnSlope = false; ray.Length = 20; test.From = new(0, new Vector2(0, 5)); test.Motion = Vector2.Zero; test.RecoveryAsCollision = true;
                Warm(Recover, "body recovery");
                void Recover()
                {
                    tree.PhysicsFrame(1d / 60);
                    Check(PhysicsServer.BodyTestMotion(body, test, result) && result.GetTravel().Y < -4 && result.GetCollisionNormal().Y < -.999f && result.GetColliderRID() == floor.GetRID(), "Recovery always includes directed penetration without the flag");
                }
            }
            finally { PhysicsServer.FreeRID(body); }
        }
        void Filters()
        {
            for (var variant = 0; variant < 5; variant++)
            {
                using var world = Create(); using var root = new SubViewport { World = world }; using var ray = new SeparationRayShape { Length = variant == 3 ? 0 : 20 }; using var box = new RectangleShape { Size = new(200, 10) };
                var floor = new StaticBody { Position = new(0, 40), CollisionMask = variant == 4 ? 0u : 1u };
                floor.AddChild(new CollisionShape { Shape = variant == 2 ? ray : box }); root.AddChild(floor);
                var body = Dynamic(new(0, 14), ray); body.LinearVelocity = new(0, 60); body.ContinuousCD = CCDMode.CastShape;
                if (variant == 0) body.CollisionMask = 0; root.AddChild(body); using var tree = new SceneTree(root);
                if (variant == 1) body.AddCollisionExceptionWith(floor);
                for (var i = 0; i < 45; i++) tree.PhysicsFrame(1d / 60);
                Check(variant == 4 ? body.Position.Y < 21 : body.Position.Y > 50, $"Directed masks/exceptions/ray pair/zero length variant {variant}: {body.Position}.");
            }
        }
        void Mass()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var ray = new SeparationRayShape();
            var body = Dynamic(Vector2.Zero, ray); body.Mass = 3; root.AddChild(body); using var tree = new SceneTree(root);
            using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            Check(state.InverseInertia == 0 && state.InverseMass == 1f / 3 && state.CenterOfMassLocal == Vector2.Zero, "Ray geometry contributes neither area nor rod inertia");
            body.Inertia = 20; tree.PhysicsFrame(1d / 2048); body.ApplyTorqueImpulse(20); tree.PhysicsFrame(1d / 2048);
            Check(MathF.Abs(body.AngularVelocity - 1) < .0001f, "Explicit ray body inertia remains effective");
        }
        void Pairs()
        {
            using var world = Create(); using var ray = new SeparationRayShape { Length = 20 }; using var box = new RectangleShape { Size = new(200, 10) }; using var plane = new WorldBoundaryShape();
            var output = new Vector2[8];
            Check(PhysicsServer.ShapeCollide(world.Space, ray.GetRID(), Transform.Identity, new(0, 20), box.GetRID(), new(0, new Vector2(0, 30)), new(0, 100), output, out var count) && count == 1, "Standalone rays use axial source motion and ignore counterpart motion");
            Check(!PhysicsServer.ShapeCollide(world.Space, ray.GetRID(), Transform.Identity, new(100, 0), box.GetRID(), new(0, new Vector2(0, 30)), new(0, -20), output, out _), "Transverse standalone ray and counterpart motion do not fabricate contact");
            Check(!PhysicsServer.ShapeCollide(world.Space, ray.GetRID(), Transform.Identity, Vector2.Zero, ray.GetRID(), Transform.Identity, Vector2.Zero, output, out _), "Standalone ray/ray pair is excluded");
            ray.Length = 0; Check(!PhysicsServer.ShapeCollide(world.Space, ray.GetRID(), Transform.Identity, Vector2.Zero, plane.GetRID(), Transform.Identity, Vector2.Zero, output, out _), "Zero ray has no boundary contact");
            ray.Length = 20; Check(PhysicsServer.ShapeCollide(world.Space, ray.GetRID(), new(Mathf.Pi / 2, new Vector2(-5, 3)), Vector2.Zero, plane.GetRID(), Transform.Identity, Vector2.Zero, output, out count) && count > 0 && output[1].Y == 0, "World boundary separates a contained nonzero ray toward the free normal");
        }
        void Dynamics(int family, CCDMode mode, bool reverse)
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var ray = new SeparationRayShape { Length = 20 }; using var shape = Family(family);
            using var material = new PhysicsMaterial { Friction = .5f, Bounce = .25f };
            var top = family is 0 or 2 or 4 ? 10 : family == 1 ? 5 : 0;
            var target = new StaticBody { Position = new(0, 40 + top), PhysicsMaterialOverride = material, ConstantLinearVelocity = new(2, 0) };
            target.AddChild(new CollisionShape { Shape = shape }); root.AddChild(target);
            var body = Dynamic(new(0, reverse ? 40 + 2 * top + 20 - .025f : 20 + .025f), ray);
            body.Rotation = reverse ? Mathf.Pi : 0; body.LinearVelocity = new(10, reverse ? -20 : 20); body.LockRotation = true; body.ContactMonitor = true;
            body.PhysicsMaterialOverride = material; body.ContinuousCD = mode; root.AddChild(body);
            var entries = 0; body.BodyEntered += _ => entries++;
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!; using var history = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            if (reverse) Frame(); else Warm(Frame, $"family {family}/{mode} contact/replay");
            void Frame()
            {
                var before = entries; history.Restore(); Check(entries == before, "Directed history restore emits no gameplay events"); tree.PhysicsFrame(1d / 2048);
                var impulse = Vector2.Zero; var expected = new Vector2(10, reverse ? -20 : 20);
                for (var i = 0; i < state.GetContactCount(); i++)
                {
                    Check(state.GetContactCollider(i) == target.GetRID() && state.GetContactColliderShape(i) == 0 && state.GetContactLocalShape(i) == 0 &&
                        state.GetContactLocalNormal(i).Dot(reverse ? Vector2.Down : Vector2.Up) > .999f, "Directed contact normal and logical identity are preserved");
                    impulse += state.GetContactImpulse(i);
                }
                if (!(state.GetContactCount() > 0 && entries == before + 1 && (impulse - (body.LinearVelocity - expected)).Length() < .03f &&
                    (reverse ? body.LinearVelocity.Y > .1f : body.LinearVelocity.Y < -.1f) && body.LinearVelocity.X < 9.9f && target.Position.Y == 40 + top))
                    throw new InvalidOperationException($"Directed response family={family}, mode={mode}, reverse={reverse}, n={state.GetContactCount()}, v={body.LinearVelocity}, impulse={impulse}, events={entries - before}.");
            }
        }
        void Slope(bool slide)
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var ray = new SeparationRayShape { Length = 20, SlideOnSlope = slide }; using var floorShape = new RectangleShape { Size = new(200, 10) };
            var floor = new StaticBody { Position = new(0, 40), Rotation = .35f }; floor.AddChild(new CollisionShape { Shape = floorShape }); root.AddChild(floor);
            var body = Dynamic(new(0, 15), ray); body.LockRotation = true; body.LinearVelocity = new(0, 60); root.AddChild(body);
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!; using var history = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            Warm(Frame, $"slope {slide}");
            void Frame()
            {
                history.Restore(); tree.PhysicsFrame(1d / 120);
                Check(state.GetContactCount() > 0 && (slide ? MathF.Abs(state.GetContactLocalNormal(0).X) > .25f : MathF.Abs(state.GetContactLocalNormal(0).X) < .001f), "Slide policy chooses the surface normal or the opposite ray axis");
            }
        }
        void Coupled()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var ray = new SeparationRayShape(); using var box = new RectangleShape { Size = new(100, 10) };
            var a = Dynamic(new(0, 20.025f), ray); a.Mass = 2; a.LockRotation = true; a.LinearVelocity = new(0, 300); root.AddChild(a);
            var b = Dynamic(new(0, 45), box); b.Name = "Target"; b.Mass = 3; b.LockRotation = true; root.AddChild(b);
            using var tree = new SceneTree(root); using var sa = PhysicsServer.BodyGetDirectState(a.GetRID())!; using var sb = PhysicsServer.BodyGetDirectState(b.GetRID())!; using var history = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            Warm(Frame, "coupled momentum/replay");
            void Frame()
            {
                history.Restore(); tree.PhysicsFrame(1d / 2048); var ia = Vector2.Zero; var ib = Vector2.Zero;
                for (var i = 0; i < sa.GetContactCount(); i++) ia += sa.GetContactImpulse(i);
                for (var i = 0; i < sb.GetContactCount(); i++) ib += sb.GetContactImpulse(i);
                Check(MathF.Abs(2 * a.LinearVelocity.Y + 3 * b.LinearVelocity.Y - 600) < .02f && MathF.Abs(a.LinearVelocity.Y - 120) < .05f && MathF.Abs(b.LinearVelocity.Y - 120) < .05f &&
                    (ia + ib).Length() < .02f && ia.DistanceTo(new(0, -360)) < .03f, "Directed coupled mass conserves momentum and reports both complete impulses");
            }
        }
        void Nonreciprocal(bool reverse)
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var ray = new SeparationRayShape(); using var box = new RectangleShape { Size = new(100, 5) };
            var passive = Dynamic(reverse ? Vector2.Zero : new(0, 42.5f), reverse ? ray : box); passive.Name = "Passive"; passive.Mass = 3; passive.LockRotation = true; passive.CollisionLayer = 8; passive.CollisionMask = 0;
            var active = Dynamic(reverse ? new(0, 15) : new(0, 20.025f), reverse ? box : ray); active.Name = "Active"; active.Mass = 2; active.LockRotation = true; active.CollisionLayer = 0; active.CollisionMask = 8;
            active.LinearVelocity = new(0, reverse ? -20 : 300); root.AddChild(passive); root.AddChild(active);
            using var tree = new SceneTree(root); var position = passive.Position; tree.PhysicsFrame(1d / 2048);
            Check(passive.Position.DistanceTo(position) < .0002f && passive.LinearVelocity == Vector2.Zero && passive.AngularVelocity == 0 &&
                MathF.Abs(active.LinearVelocity.Y) < .05f && PhysicsServer.BodyGetMass(passive.GetRID()) == 3 && PhysicsServer.BodyGetMass(active.GetRID()) == 2,
                "Directed effective contact mass applies only to the matching endpoint, including zero layer and reversed geometry roles");
        }
        void AngularCCD()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var ray = new SeparationRayShape { Length = 160, SlideOnSlope = true }; using var circle = new CircleShape { Radius = .5f };
            using var material = new PhysicsMaterial { Friction = 0, Bounce = 0 };
            var center = new Vector2(3, 0); var body = Dynamic(Vector2.Zero, ray); body.Mass = 2; body.Inertia = 20000;
            body.CenterOfMassMode = RigidCenterOfMassMode.Custom; body.CenterOfMass = center;
            body.AngularVelocity = 30; body.ContinuousCD = CCDMode.CastShape; body.PhysicsMaterialOverride = material; root.AddChild(body);
            var floor = new StaticBody { Position = center + (new Vector2(0, 160) - center).Rotated(.225f), PhysicsMaterialOverride = material };
            floor.AddChild(new CollisionShape { Shape = circle }); root.AddChild(floor); using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            tree.PhysicsFrame(.015);
            for (var i = 0; i < state.GetContactCount(); i++)
                Console.WriteLine($"Angular contact {i}: normal={state.GetContactLocalNormal(i)}, point={state.GetContactLocalPosition(i)}, impulse={state.GetContactImpulse(i)}, inertia={state.InverseInertia}.");
            if (!(state.GetContactCount() > 0 && body.AngularVelocity < 29.9f && body.Position.IsFinite() && body.LinearVelocity.LengthSquared() + 10000 * body.AngularVelocity * body.AngularVelocity <= 9180000))
                throw new InvalidOperationException($"Directed angular: n={state.GetContactCount()}, position={body.Position}, velocity={body.LinearVelocity}, angular={body.AngularVelocity}, rotation={body.Rotation}.");
        }
        void Continuous()
        {
            foreach (var mode in new[] { CCDMode.CastRay, CCDMode.CastShape })
            {
                using var world = Create(); using var root = new SubViewport { World = world }; using var ray = new SeparationRayShape(); using var box = new RectangleShape { Size = new(200, .2f) };
                var floor = new StaticBody { Position = new(0, 40) }; floor.AddChild(new CollisionShape { Shape = box }); root.AddChild(floor);
                var body = Dynamic(Vector2.Zero, ray); body.LockRotation = true; body.LinearVelocity = new(0, 6000); body.ContinuousCD = mode; root.AddChild(body);
                using var tree = new SceneTree(root); tree.PhysicsFrame(1d / 60);
                Check(MathF.Abs(body.Position.Y - 19.9f) < .501f && MathF.Abs(body.LinearVelocity.Y) < 1, "Directed CCD stops at the authored tip on a thin floor");
            }
        }
        void Lifecycle()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var ray = new SeparationRayShape { Length = 30 }; using var box = new RectangleShape { Size = new(200, 10) };
            var floor = new StaticBody { Position = new(0, 25) }; floor.AddChild(new CollisionShape { Shape = box }); root.AddChild(floor); using var tree = new SceneTree(root);
            using var query = new PhysicsShapeQueryParameters { Shape = ray, Margin = 0 }; var hits = new PhysicsShapeResult[2];
            Check(world.DirectSpaceState.IntersectShape(query, hits) == 1, "Directed source is initially attached to query");
            ray.Length = 0; Check(world.DirectSpaceState.IntersectShape(query, hits) == 0, "Zero length retires directed collision");
            ray.Length = .2f; query.Transform = new(0, new Vector2(0, 19.9f)); Check(world.DirectSpaceState.IntersectShape(query, hits) == 1, "Sub-slop nonzero ray keeps exact directed length");
            ray.Length = 30; query.Transform = new(0, new Vector2(0, 25)); Check(world.DirectSpaceState.IntersectShape(query, hits) == 0, "Full origin containment creates no internal seam contact");
            ray.Changed += _ => throw new InvalidOperationException("Changed observer failed");
            Reject<InvalidOperationException>(() => ray.Length = 10); query.Transform = new(0, new Vector2(0, 15));
            Check(world.DirectSpaceState.IntersectShape(query, hits) == 1, "Committed directed geometry reaches queries after a failed Changed observer");
        }
        void Character()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var ray = new SeparationRayShape { Length = 20, SlideOnSlope = true }; using var box = new RectangleShape { Size = new(200, 10) };
            var floor = new StaticBody { Position = new(0, 30) }; floor.AddChild(new CollisionShape { Shape = box }); root.AddChild(floor);
            var character = new CharacterBody { PhysicsProcessEnabled = true, FloorSnapLength = 10 }; character.AddChild(new CollisionShape { Shape = ray }); root.AddChild(character);
            using var tree = new SceneTree(root); tree.PhysicsFrame(1d / 60); character.Velocity = new(0, 600);
            Check(character.MoveAndSlide() && character.IsOnFloor() && character.Velocity.Y == 0 && character.GetSlideCollision(0).GetColliderRID() == floor.GetRID(), "Character movement consumes registered directed motion and floor identity");
            character.Velocity = new(0, -600); character.MoveAndSlide(); character.MoveAndSlide();
            ray.SlideOnSlope = false; character.Position = Vector2.Zero; character.Velocity = Vector2.Zero; character.ApplyFloorSnap();
            Check(character.IsOnFloor() && character.Position.Y is > 4.8f and < 5.1f, "Character snap explicitly includes a non-sliding ray");
        }
        void Sleep()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var ray = new SeparationRayShape(); using var box = new RectangleShape { Size = new(200, 10) };
            var floor = new StaticBody { Position = new(0, 40) }; floor.AddChild(new CollisionShape { Shape = box }); root.AddChild(floor);
            var body = Dynamic(new(0, 14), ray); body.GravityScale = 1; body.CanSleep = true; body.LockRotation = true; root.AddChild(body);
            using var tree = new SceneTree(root); for (var i = 0; i < 240; i++) tree.PhysicsFrame(1d / 60);
            Check(body.Sleeping && body.Position.Y is > 14 and < 16, "Directed support settles and sleeps at the authored tip");
            ray.Length = 0; body.Sleeping = false; for (var i = 0; i < 30; i++) tree.PhysicsFrame(1d / 60);
            Check(body.Position.Y > 50, "A zero-length edit removes directed support from a woken body");
        }
        static Shape Family(int family) => family switch
        {
            0 => new CircleShape { Radius = 10 },
            1 => new RectangleShape { Size = new(200, 10) },
            2 => new CapsuleShape { Radius = 10, Height = 20 },
            3 => new SegmentShape { A = new(-50, 0), B = new(50, 0) },
            4 => new ConvexPolygonShape { Points = [new(-10, -10), new(10, -10), new(10, 10), new(-10, 10)] },
            _ => new ConcavePolygonShape { Segments = [new(-50, 0), new(50, 0)] }
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
            Check(own == 0 && all == 0, $"Directed {label} allocated {own}/{all} B"); Console.WriteLine($"Directed {label}: 128 full frames {own}/{all} B.");
        }
    }
}
