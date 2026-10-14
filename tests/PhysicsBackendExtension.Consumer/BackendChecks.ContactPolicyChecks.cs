using Electron2D;

public static partial class BackendChecks
{
    public static void RunContactPolicy(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("PolicySolver", () => new Solver());
        try
        {
            for (var family = 0; family < 7; family++) Correction(family);
            Convergence(false); Convergence(true); EmptyIterations();
            Console.WriteLine($"Contact policy {backend?.ToString() ?? "registered"}, threaded={threaded}: mixed correction, shared sweeps, momentum and zero warm frames passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        World Create() => backend is { } selected ? new World(selected) : new World("PolicySolver");
        static Shape ShapeFor(int family) => family switch
        {
            0 => new CircleShape { Radius = 5 },
            1 => new RectangleShape { Size = new(10, 10) },
            2 => new CapsuleShape { Radius = 5, Height = 10 },
            3 => new SegmentShape { A = new(-5, 5), B = new(5, 5) },
            4 => new ConvexPolygonShape { Points = [new(-5, -5), new(5, -5), new(5, 5), new(-5, 5)] },
            5 => new ConcavePolygonShape { Segments = [new(-5, 5), new(5, 5)] },
            _ => new SeparationRayShape { Length = 5 }
        };
        void Correction(int family)
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var shape = ShapeFor(family);
            using var plane = new WorldBoundaryShape { Normal = Vector2.Up, Distance = -5 };
            using var material = new PhysicsMaterial { Friction = .5f, Bounce = 0 };
            var floor = new StaticBody { PhysicsMaterialOverride = material }; floor.AddChild(new CollisionShape { Shape = plane }); root.AddChild(floor);
            var body = new RigidBody
            {
                Name = "Actor",
                Position = new(0, 1),
                GravityScale = 0,
                Mass = 2,
                LockRotation = true,
                CanSleep = false,
                LinearDampMode = RigidBody.DampMode.Replace,
                LinearDamp = 0,
                AngularDampMode = RigidBody.DampMode.Replace,
                AngularDamp = 0,
                MaxContactsReported = 8,
                PhysicsMaterialOverride = material,
                ContactMonitor = true
            };
            body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body); using var tree = new SceneTree(root);
            using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            var entries = 0; body.BodyEntered += _ => entries++;
            foreach (var (own, other, worldBias, effective) in new[] { (0f, 0f, .8f, .8f), (.25f, 0f, .8f, .25f),
                (0f, .5f, .8f, .5f), (.25f, .75f, .8f, .5f), (0f, 0f, 0f, 0f), (0f, 0f, 1f, 1f), (1f, 0f, .8f, 1f) })
            {
                shape.CustomSolverBias = own; plane.CustomSolverBias = other; Set(worldBias, .3f);
                foreach (var iterations in new[] { 1, 16, 128 }) foreach (var delta in new[] { .01, .04 })
                    {
                        PhysicsServer.SpaceSetSolverIterations(world.Space, iterations); Reset(); tree.PhysicsFrame(delta);
                        Near(body.Position.Y, 1 - effective * .7f, .004f, "Mixed bias consumes one outer correction budget");
                        Near(body.LinearVelocity.Length(), 0, .003f, "Position correction creates no kinetic velocity");
                        Near(Sum().Length(), 0, .01f, "Position correction creates no reported physical impulse");
                        Check(state.GetContactCount() > 0, "Zero bias/slack do not filter contacts");
                    }
            }
            shape.CustomSolverBias = plane.CustomSolverBias = 0; Set(.8f, 1.2f); Reset(); tree.PhysicsFrame(.01);
            Near(body.Position.Y, 1, .003f, "Penetration within allowed slack remains uncorrected"); Check(state.GetContactCount() > 0, "Slack retains collision geometry");
            Set(.8f, .3f); var geometry = shape.GetGeometryData(); var rid = shape.GetRID(); var inertia = state.InverseInertia;
            Action<Resource> fail = _ => throw new ApplicationException("Policy observer failure"); shape.Changed += fail;
            Reject<ApplicationException>(() => shape.CustomSolverBias = .25f); shape.Changed -= fail; Reset(); tree.PhysicsFrame(.01);
            Near(body.Position.Y, .825f, .004f, "Committed bias survives observer failure and lazy preparation");
            Check(ReferenceEquals(shape.GetGeometryData(), geometry) && shape.GetRID() == rid && state.InverseInertia == inertia, "Policy preserves geometry, identity and mass");
            shape.CustomSolverBias = plane.CustomSolverBias = 0; Set(.8f, .3f); PhysicsServer.SpaceSetSolverIterations(world.Space, 16);
            var tick = 0;
            Warm(() =>
            {
                var bias = (tick++ & 1) == 0 ? .25f : .75f; shape.CustomSolverBias = bias; Reset(); tree.PhysicsFrame(.01);
                Near(body.Position.Y, 1 - bias * .7f, .004f, "Live shape policy affects the next full frame");
                Check(state.GetContactCount() > 0 && ReferenceEquals(shape.GetGeometryData(), geometry) && shape.GetRID() == rid, "Live policy retains contact/shape identity");
            }, $"family {family}/live correction");
            shape.CustomSolverBias = 0; Set(.8f, .3f); Reset(); body.LinearVelocity = new(30, 80);
            var velocity = body.LinearVelocity; tree.PhysicsFrame(.01); var impulse = Sum();
            Check(impulse.DistanceTo((body.LinearVelocity - velocity) * 2) < .03f && impulse.X < -1 && impulse.Y < -1,
                "Physical normal and friction reports preserve full momentum independently of correction");
            body.CanSleep = true; body.Sleeping = true; Set(.5f, .3f); Check(!body.Sleeping, "Changed world settings wake dynamics");
            body.Sleeping = true; Set(.5f, .3f); Check(body.Sleeping, "Equal world settings preserve explicit sleep");
            var step = 0;
            Warm(() =>
            {
                PhysicsServer.SpaceSetSolverIterations(world.Space, (step++ & 1) == 0 ? 8 : 16); Reset(); tree.PhysicsFrame(.01);
                Check(!body.Sleeping && state.GetContactCount() > 0, "Changed sweep count operates on live contacts");
            }, $"family {family}/live iterations");
            Check(entries == 1, "Policy edits and correction retain one monitored contact episode");
            if (family == 0)
            {
                var extra = new StaticBody[11];
                try
                {
                    for (var i = 0; i < extra.Length; i++) { var wall = extra[i] = new StaticBody { Name = $"Extra{i}" }; wall.AddChild(new CollisionShape { Shape = plane }); root.AddChild(wall); }
                    Set(.5f, .3f);
                    Warm(() => { Reset(); tree.PhysicsFrame(.01); Near(body.Position.Y, .65f, .01f, "Coincident contacts share the outer correction budget"); }, "12 incident contacts");
                }
                finally { foreach (var wall in extra) wall?.Dispose(); }
                using var bulletShape = new CircleShape { Radius = 1 }; using var targetShape = new RectangleShape { Size = new(2, 2) };
                var target = new StaticBody { Name = "CCDTarget", Position = new(0, -1000) }; target.AddChild(new CollisionShape { Shape = targetShape }); root.AddChild(target);
                var bullet = new RigidBody
                {
                    Name = "Bullet",
                    Position = new(-100, -1000),
                    LinearVelocity = new(5000, 0),
                    ContinuousCD = CCDMode.CastShape,
                    GravityScale = 0,
                    CanSleep = false,
                    LinearDampMode = RigidBody.DampMode.Replace,
                    LinearDamp = 0
                };
                bullet.AddChild(new CollisionShape { Shape = bulletShape }); root.AddChild(bullet);
                Set(.8f, .3f); Reset(); tree.PhysicsFrame(.04);
                Near(body.Position.Y, .44f, .004f, "Independent CCD durations retain one outer bias budget");
                Check(bullet.Position.X < 0 && bullet.LinearVelocity.X < 1, "The control really collides during the CCD interval");
            }
            void Set(float bias, float slack) { PhysicsServer.SpaceSetContactDefaultBias(world.Space, bias); PhysicsServer.SpaceSetContactMaxAllowedPenetration(world.Space, slack); }
            void Reset() { body.Position = new(0, 1); body.LinearVelocity = Vector2.Zero; body.Sleeping = false; }
            Vector2 Sum() { var value = Vector2.Zero; for (var i = 0; i < state.GetContactCount(); i++) value += state.GetContactImpulse(i); return value; }
        }
        void Convergence(bool joints)
        {
            var low = Error(1); var high = Error(128);
            Check(high < low * .4f && high < .5f, "More complete contact/joint sweeps converge the common velocity");
            Console.WriteLine($"Policy {(joints ? "pins" : "contacts")}: rms 1={low}, 128={high}.");
            float Error(int iterations)
            {
                using var world = Create(); using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 5 };
                using var material = new PhysicsMaterial { Friction = 0, Bounce = 0 }; var bodies = new RigidBody[8];
                for (var i = 0; i < bodies.Length; i++)
                {
                    var b = bodies[i] = new RigidBody
                    {
                        Name = $"Body{i}",
                        Position = new(i * 10, 0),
                        Mass = 1,
                        GravityScale = 0,
                        CanSleep = false,
                        LockRotation = true,
                        LinearVelocity = i == 0 ? new(60, 0) : Vector2.Zero,
                        LinearDampMode = RigidBody.DampMode.Replace,
                        LinearDamp = 0,
                        PhysicsMaterialOverride = material
                    };
                    if (!joints) b.AddChild(new CollisionShape { Shape = shape }); root.AddChild(b);
                }
                using var tree = new SceneTree(root); PhysicsServer.SpaceSetContactDefaultBias(world.Space, 0);
                PhysicsServer.SpaceSetSolverIterations(world.Space, iterations); var links = new RID[joints ? 7 : 0];
                try
                {
                    for (var i = 0; i < links.Length; i++) { var joint = links[i] = PhysicsServer.JointCreate(); PhysicsServer.JointMakePin(joint, new(i * 10 + 5, 0), bodies[i].GetRID(), bodies[i + 1].GetRID()); PhysicsServer.JointSetMaxBias(joint, 0); }
                    using var history = PhysicsServer.SpaceCreateCheckpoint(world.Space);
                    var result = 0f;
                    Warm(() =>
                    {
                        history.Restore(); tree.PhysicsFrame(.01); var sum = Vector2.Zero; var squared = 0f;
                        foreach (var b in bodies) { sum += b.LinearVelocity; squared += (b.LinearVelocity - new Vector2(7.5f, 0)).LengthSquared(); }
                        Check(sum.DistanceTo(new(60, 0)) < .03f, "All internal contact/joint impulses conserve momentum"); result = MathF.Sqrt(squared / bodies.Length);
                    }, $"{(joints ? "pins" : "contacts")}/{iterations} sweeps replay");
                    return result;
                }
                finally { foreach (var joint in links) if (joint.IsValid()) PhysicsServer.FreeRID(joint); }
            }
        }
        void EmptyIterations()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(40, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            root.AddChild(body); using var tree = new SceneTree(root);
            body.ContinuousCD = CCDMode.CastShape;
            foreach (var count in new[] { 1, 16, 128, int.MaxValue })
            { PhysicsServer.SpaceSetSolverIterations(world.Space, count); body.Position = Vector2.Zero; tree.PhysicsFrame(.02); Near(body.Position.X, .8f, .00003f, "Iteration count does not multiply integration time"); }
        }
        static void Near(float actual, float expected, float tolerance, string label)
        { if (MathF.Abs(actual - expected) > tolerance) throw new InvalidOperationException($"{label}: {actual} expected {expected}"); }
        static void Warm(Action frame, string label)
        {
            frame(); for (var i = 0; i < 128; i++) frame(); Collect();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) frame();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Contact policy {label} allocated {owner}/{all} B"); Console.WriteLine($"Contact policy {label}: 128 full frames {owner}/{all} B.");
        }
    }
}
