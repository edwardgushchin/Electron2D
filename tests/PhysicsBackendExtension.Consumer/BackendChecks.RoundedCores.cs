using Electron2D;

public static partial class BackendChecks
{
    public static void RunRoundedCores(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { RoundedCoresCore(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
    private static void RoundedCoresCore(PhysicsServer.Backend? backend, bool threaded)
    {
        var owner = Thread.CurrentThread;
        if (backend is null) PhysicsServerManager.RegisterServer("RoundedCores", () => new Solver
        { StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Rounded contacts retain the actual numerical lane") });
        using var world = backend is { } selected ? new World(selected) : new World("RoundedCores");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "Rounded contacts select their actual backend without fallback");
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        PhysicsServer.SpaceSetContactDefaultBias(world.Space, .8f); PhysicsServer.SpaceSetContactMaxAllowedPenetration(world.Space, .25f);
        Shape[] shapes = [new CircleShape { Radius = 3 }, new CapsuleShape { Radius = 3, Height = 10 },
            new SegmentShape { A = new(0, -5), B = new(0, 5) }, new CapsuleShape { Radius = 3, Height = 6 },
            new SegmentShape { A = Vector2.Zero, B = Vector2.Zero }];
        using var disabled = new CircleShape { Radius = .1f }; using var material = new PhysicsMaterial { Friction = 0, Bounce = 0 };
        int[] first = [0, 0, 1, 0, 2, 1, 3, 0, 4, 0]; int[] second = [0, 1, 0, 2, 0, 1, 0, 3, 0, 4];
        float[] radius = [3, 3, 0, 3, 0]; float[] half = [0, 2, 5, 0, 0];
        const int cases = 40; var bodies = new RigidBody[cases]; var peers = new RID[cases]; var starts = new Transform[cases]; var enters = new int[cases];
        var initialNormals = new Vector2[cases]; var initialCounts = new int[cases]; var initialIdentity = new bool[cases];
        var local = new Transform(.2f, new(1, .5f)); var failures = new List<string>();
        try
        {
            for (var i = 0; i < cases; i++)
            {
                var index = i; var family = i % 10; var rotation = i / 10 % 2 == 0 ? 0 : .37f;
                starts[i] = new(rotation, new(i % 8 * 20, i / 8 * 20));
                var body = new RigidBody
                {
                    Transform = starts[i],
                    Mass = 2,
                    Inertia = 4,
                    LockRotation = true,
                    GravityScale = 0,
                    CanSleep = false,
                    MaxContactsReported = 4,
                    ContactMonitor = true,
                    PhysicsMaterialOverride = material,
                    LinearDampMode = RigidBody.DampMode.Replace,
                    LinearDamp = 0,
                    AngularDampMode = RigidBody.DampMode.Replace,
                    AngularDamp = 0
                };
                bodies[i] = body; body.AddChild(new CollisionShape { Shape = disabled, Disabled = true }); body.AddChild(new CollisionShape { Shape = shapes[first[family]], Transform = local }); root.AddChild(body);
                peers[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(peers[i], i >= 20 ? PhysicsServer.BodyMode.RigidLinear : PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(peers[i], disabled.GetRID(), disabled: true); PhysicsServer.BodyAddShape(peers[i], shapes[second[family]].GetRID(), local);
                PhysicsServer.BodySetMass(peers[i], 5); PhysicsServer.BodySetInertia(peers[i], 4); PhysicsServer.BodySetGravityScale(peers[i], 0); PhysicsServer.BodySetCanSleep(peers[i], false);
                PhysicsServer.BodySetMaxContactsReported(peers[i], 4); PhysicsServer.BodySetFriction(peers[i], 0); PhysicsServer.BodySetBounce(peers[i], 0);
                PhysicsServer.BodySetLinearDampMode(peers[i], RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(peers[i], 0);
                PhysicsServer.BodySetAngularDampMode(peers[i], RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(peers[i], 0);
                PhysicsServer.BodySetTransform(peers[i], starts[i]); PhysicsServer.BodySetSpace(peers[i], world.Space);
                body.BodyShapeEntered += (rid, _, remote, own) => { Check(rid == peers[index] && remote == 1 && own == 1, "Rounded contact events preserve both logical slots"); enters[index]++; };
            }
            tree.PhysicsFrame(.01);
            for (var i = 0; i < cases; i++)
            {
                var state = PhysicsServer.BodyGetDirectState(bodies[i].GetRID())!;
                initialCounts[i] = state.GetContactCount(); initialIdentity[i] = true;
                if (initialCounts[i] > 0) initialNormals[i] = state.GetContactLocalNormal(0);
                for (var j = 0; j < initialCounts[i]; j++) initialIdentity[i] &= state.GetContactCollider(j) == peers[i] && state.GetContactColliderShape(j) == 1 && state.GetContactLocalShape(j) == 1 && state.GetContactLocalPosition(j).IsFinite() && state.GetContactColliderPosition(j).IsFinite();
            }
            for (var frame = 1; frame < 24; frame++) tree.PhysicsFrame(.01);
            for (var i = 0; i < cases; i++)
            {
                var family = i % 10; var normal = initialNormals[i]; var gap = Gap(i); var pose = PhysicsServer.BodyGetTransform(peers[i]);
                var valid = initialCounts[i] > 0 && initialIdentity[i] && enters[i] == 0 && normal.IsFinite() && MathF.Abs(normal.Length() - 1) < .001f && gap >= -.28f && gap <= .35f;
                if (half[first[family]] + half[second[family]] > 0) valid &= MathF.Abs(normal.Dot(Axis(i))) < .002f;
                valid &= bodies[i].Position.IsFinite() && pose.Origin.IsFinite() && bodies[i].LinearVelocity.IsFinite();
                if (i < 20) valid &= pose == starts[i];
                else valid &= ((bodies[i].Position - starts[i].Origin) * 2 + (pose.Origin - starts[i].Origin) * 5).Length() < .02f;
                if (valid) continue;
                failures.Add($"case={i}, pair={first[family]}/{second[family]}, initialCount={initialCounts[i]}, normal={normal}, enters={enters[i]}, gap={gap}, position={bodies[i].Position}, peer={pose.Origin}");
            }
            foreach (var failure in failures) Console.WriteLine(failure);
            Check(failures.Count == 0, "Rounded core failures: " + string.Join("; ", failures));
            for (var frame = 0; frame < 128; frame++) Frame(); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var frame = 0; frame < 128; frame++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Rounded contact frames allocated {own}/{all} B");
            Console.WriteLine($"Rounded cores {backend?.ToString() ?? "registered"}, separate={threaded}: 40 scene/server pairs, unit normals, correction, metadata, momentum and 128 deep-contact frames {own}/{all} B.");
            void Frame()
            {
                for (var i = 0; i < cases; i++)
                {
                    PhysicsServer.BodySetTransform(bodies[i].GetRID(), starts[i]); PhysicsServer.BodySetLinearVelocity(bodies[i].GetRID(), -initialNormals[i] * 8);
                    PhysicsServer.BodySetTransform(peers[i], starts[i]); PhysicsServer.BodySetLinearVelocity(peers[i], Vector2.Zero);
                }
                tree.PhysicsFrame(.01);
                for (var i = 0; i < cases; i++)
                {
                    var gap = Gap(i); var family = i % 10;
                    Check(gap > -radius[first[family]] - radius[second[family]] + .01f, "A measured rounded contact executes actual geometric correction");
                    var state = PhysicsServer.BodyGetDirectState(bodies[i].GetRID())!; var impulse = Vector2.Zero;
                    Check(state.GetContactCount() > 0, "A measured rounded pair retains actual contact");
                    for (var j = 0; j < state.GetContactCount(); j++)
                    {
                        Check(MathF.Abs(state.GetContactLocalNormal(j).Length() - 1) < .001f && state.GetContactCollider(j) == peers[i] && state.GetContactLocalShape(j) == 1 && state.GetContactColliderShape(j) == 1,
                            "Measured rounded contacts retain unit normals and logical identity");
                        impulse += state.GetContactImpulse(j);
                    }
                    var incoming = -initialNormals[i] * 8;
                    Check(impulse.DistanceTo((bodies[i].LinearVelocity - incoming) * 2) < .08f, "Rounded reported impulse agrees with full-step momentum change");
                    if (i >= 20) Check((bodies[i].LinearVelocity * 2 + PhysicsServer.BodyGetLinearVelocity(peers[i]) * 5 - incoming * 2).Length() < .02f, "Free rounded pairs conserve linear momentum");
                }
            }
            Vector2 Axis(int i) => new Vector2(0, 1).Rotated(starts[i].Rotation + local.Rotation);
            float Gap(int i)
            {
                var family = i % 10; var delta = PhysicsServer.BodyGetTransform(peers[i]).Origin - bodies[i].Position; var axis = Axis(i);
                var perpendicular = delta.Dot(new(axis.Y, -axis.X)); var axial = MathF.Max(0, MathF.Abs(delta.Dot(axis)) - half[first[family]] - half[second[family]]);
                return MathF.Sqrt(perpendicular * perpendicular + axial * axial) - radius[first[family]] - radius[second[family]];
            }
        }
        finally
        {
            foreach (var peer in peers) if (peer.IsValid()) PhysicsServer.FreeRID(peer);
            foreach (var body in bodies) body?.Dispose(); foreach (var shape in shapes) shape.Dispose();
        }
    }
}
