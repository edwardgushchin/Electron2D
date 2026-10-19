using Electron2D;

public static partial class BackendChecks
{
    public static void RunContactCache(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        const string registered = "ContactCache";
        if (backend is null) PhysicsServerManager.RegisterServer(registered, () => solver = new());
        var name = backend?.ToString() ?? registered;
        try
        {
            if (backend is null)
            {
                var cold = Stack(0); var warm = Stack(10);
                Check(warm < cold * .75f, $"Recycled impulses must improve loaded one-sweep convergence: cold={cold:R}, warm={warm:R}");
                Console.WriteLine($"Loaded contact cache: cold/warm residual speed={cold:R}/{warm:R} scene units/s.");
            }
            for (var family = 0; family < 7; family++)
            {
                using var shape = ContactPolicyShape(family);
                using var replacement = ContactPolicyShape(family);
                using var rig = new CacheRig(name, shape);
                var owner = solver;
                foreach (var (radius, separation, reused) in new[] { (10f, 10f, true), (0f, 10f, false), (.5f, 10f, false),
                    (4f, 10f, false), (10f, .5f, false), (10f, 4f, true), (10f, 3.999f, false), (10f, 10f, true) })
                {
                    rig.Policy(radius, separation);
                    for (var i = 0; i < 8; i++) rig.Step();
                    var sample = rig.Read(0);
                    Check(sample.Contacts > 0 && MathF.Abs(sample.Velocity.X - 400) < .02f && MathF.Abs(sample.Velocity.Y) < .02f &&
                        sample.Impulse.DistanceTo(new(0, -9.8f)) < .04f, "Cache limits preserve actual support, tangent motion and full-step momentum");
                    if (owner is not null) Check((owner.ReusedContactPoints > 0) == reused,
                        $"Cache policy {family}: radius={radius}, separation={separation}, expected={reused}, points={owner.ReusedContactPoints}");
                }
                PhysicsServer.BodySetLinearVelocity(rig.Bodies[0], Vector2.Zero); rig.Policy(10, 0);
                for (var i = 0; i < 4; i++) rig.Step();
                if (owner is not null) Check(owner.ReusedContactPoints > 0, "Zero maximum separation permits stable penetrating contacts");
                PhysicsServer.BodySetLinearVelocity(rig.Bodies[0], new(400, 0)); rig.Policy(10, 10);
                PhysicsServer.BodySetShape(rig.Bodies[0], 0, replacement.GetRID()); rig.Step();
                if (owner is not null) Check(owner.ReusedContactPoints == 0, "Rebuilt geometry never reuses a retired shape's contact history");
                rig.Policy(10, 10);
                for (var i = 0; i < 64; i++) rig.Step();
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(rig.Space);
                rig.Step(); var expected = rig.Read(0);
                for (var i = 0; i < 64; i++) Replay();
                Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Replay();
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"Cache restore/step {family} allocated {own}/{all} B");
                Console.WriteLine($"Contact cache {name}, family={family}, separate={threaded}: 128 gap/restore/step cycles, owner/all={own}/{all} B.");
                void Replay()
                {
                    PhysicsServer.BodySetTransform(rig.Bodies[0], new(0, new(0, -500))); rig.Step();
                    if (owner is not null) Check(owner.ReusedContactPoints == 0, "Fresh separated geometry never reuses an old contact");
                    checkpoint.Restore(); rig.Step(); var current = rig.Read(0);
                    Check(current.Pose.Origin.DistanceTo(expected.Pose.Origin) < .001f && current.Velocity.DistanceTo(expected.Velocity) < .001f &&
                        current.Impulse.DistanceTo(expected.Impulse) < .04f && current.Contacts == expected.Contacts, "Private restore recovers physical contact response");
                    if (owner is not null) Check(owner.ReusedContactPoints > 0, "Private restore recovers cache anchors and impulses after a contact-free interval");
                }
            }
            using var ball = new CircleShape { Radius = 5 };
            using var stack = new CacheRig(name, ball, 8);
            foreach (var id in stack.Bodies) PhysicsServer.BodySetLinearVelocity(id, Vector2.Zero);
            stack.Policy(10, 10); PhysicsServer.SpaceSetSolverIterations(stack.Space, 1);
            for (var i = 0; i < 48; i++) stack.Step();
            using var saved = PhysicsServer.SpaceCreateCheckpoint(stack.Space);
            stack.Step(); var states = new CacheSample[stack.Bodies.Length]; for (var i = 0; i < states.Length; i++) states[i] = stack.Read(i);
            for (var cycle = 0; cycle < 64; cycle++)
            {
                for (var i = 0; i < stack.Bodies.Length; i++) PhysicsServer.BodySetTransform(stack.Bodies[i], new(0, new(0, -500 - 30 * i)));
                stack.Step(); saved.Restore(); stack.Step();
                for (var i = 0; i < states.Length; i++)
                {
                    var current = stack.Read(i);
                    Check(current.Pose.Origin.DistanceTo(states[i].Pose.Origin) < .002f && current.Velocity.DistanceTo(states[i].Velocity) < .002f,
                        "Restored coupled cache preserves the next physical step at one solver sweep");
                }
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }

        float Stack(float radius)
        {
            using var ball = new CircleShape { Radius = 5 }; using var rig = new CacheRig(name, ball, 8);
            foreach (var body in rig.Bodies) PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero);
            rig.Policy(radius, 10); PhysicsServer.SpaceSetSolverIterations(rig.Space, 1);
            for (var i = 0; i < 48; i++) rig.Step();
            var squared = 0f; foreach (var body in rig.Bodies) squared += PhysicsServer.BodyGetLinearVelocity(body).LengthSquared();
            return MathF.Sqrt(squared / rig.Bodies.Length);
        }
    }

    private readonly record struct CacheSample(Transform Pose, Vector2 Velocity, Vector2 Impulse, int Contacts);
    private sealed class CacheRig : IDisposable
    {
        internal readonly RID Space, Floor;
        internal readonly RID[] Bodies;
        private readonly PhysicsDirectBodyState[] _views;
        private readonly WorldBoundaryShape _plane = new() { Normal = Vector2.Up };
        internal CacheRig(string name, Shape shape, int count = 1)
        {
            Space = PhysicsServer.SpaceCreate(name);
            PhysicsServer.AreaSetGravity(Space, 980); PhysicsServer.AreaSetGravityVector(Space, Vector2.Down);
            Floor = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(Floor, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(Floor, _plane.GetRID()); PhysicsServer.BodySetFriction(Floor, 0); PhysicsServer.BodySetSpace(Floor, Space);
            Bodies = new RID[count]; _views = new PhysicsDirectBodyState[count];
            for (var i = 0; i < count; i++)
            {
                var body = Bodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.RigidLinear);
                PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetMass(body, 1);
                PhysicsServer.BodySetTransform(body, new(0, new(0, -4.9f - 9.9f * i)));
                PhysicsServer.BodySetFriction(body, 0); PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(body, 0);
                PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(body, 0);
                PhysicsServer.BodySetLinearVelocity(body, new(400, 0)); PhysicsServer.BodySetMaxContactsReported(body, 8);
                PhysicsServer.BodySetSpace(body, Space); _views[i] = PhysicsServer.BodyGetDirectState(body)!;
            }
            PhysicsServer.SpaceSetActive(Space, true);
        }
        internal void Policy(float radius, float separation)
        { PhysicsServer.SpaceSetContactRecycleRadius(Space, radius); PhysicsServer.SpaceSetContactMaxSeparation(Space, separation); }
        internal void Step() => PhysicsServer.SpaceStep(Space, .01);
        internal CacheSample Read(int index)
        {
            var view = _views[index]; var count = view.GetContactCount(); var impulse = Vector2.Zero;
            for (var i = 0; i < count; i++) impulse += view.GetContactImpulse(i);
            return new(view.Transform, view.LinearVelocity, impulse, count);
        }
        public void Dispose()
        {
            foreach (var view in _views) view.Dispose();
            foreach (var body in Bodies) PhysicsServer.FreeRID(body);
            PhysicsServer.FreeRID(Floor); PhysicsServer.FreeRID(Space); _plane.Dispose();
        }
    }
}
