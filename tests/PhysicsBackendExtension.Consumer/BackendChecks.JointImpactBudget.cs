using Electron2D;

public static partial class BackendChecks
{
    public static void RunJointImpactBudget(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            var ownerThread = Thread.CurrentThread;
            if (backend is null) PhysicsServerManager.RegisterServer("JointImpactBudget", () => new Solver
            { StepHook = () => Check((Thread.CurrentThread != ownerThread) == threaded, "Joint impact uses the requested numerical lane") });
            using var world = backend is { } selected ? new World(selected) : new World("JointImpactBudget");
            using var shape = new CircleShape { Radius = 1 };
            using var root = new SubViewport { World = world };
            var scene = new RigidBody(); scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
            var joints = new RID[8];
            for (var body = 0; body < 2; body++)
            {
                Joint[] nodes = [new PinJoint(), new GrooveJoint()];
                for (var family = 0; family < 2; family++) { root.AddChild(nodes[family]); joints[body * 4 + 2 + family] = nodes[family].GetRID(); }
            }
            using var tree = new SceneTree(root);
            var raw = PhysicsServer.BodyCreate(); var target = PhysicsServer.BodyCreate();
            RID[] bodies = [scene.GetRID(), raw]; var callbacks = new int[2]; var states = new PhysicsDirectBodyState[2];
            var maxSpeedError = 0f; var maxJointImpulse = 0f; var maxReachExcess = 0f;
            try
            {
                PhysicsServer.AreaSetGravity(world.Space, 0); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
                PhysicsServer.BodyAddShape(raw, shape.GetRID()); PhysicsServer.BodySetSpace(raw, world.Space);
                PhysicsServer.BodySetMode(target, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(target, shape.GetRID());
                PhysicsServer.BodySetBounce(target, .5f); PhysicsServer.BodySetFriction(target, 0); PhysicsServer.BodySetSpace(target, world.Space);
                for (var body = 0; body < 2; body++)
                {
                    var id = bodies[body]; PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.RigidLinear); PhysicsServer.BodySetMass(id, 2); PhysicsServer.BodySetInertia(id, 2);
                    PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetCanSleep(id, false);
                    PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(id, 0);
                    PhysicsServer.BodySetAngularDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(id, 0);
                    PhysicsServer.BodySetBounce(id, .5f); PhysicsServer.BodySetFriction(id, 0); PhysicsServer.BodySetMaxContactsReported(id, 4);
                    var index = body; PhysicsServer.BodySetStateSyncCallback(id, _ => callbacks[index]++); states[body] = PhysicsServer.BodyGetDirectState(id)!;
                    for (var ownership = 0; ownership < 2; ownership++)
                        for (var family = 0; family < 2; family++)
                        {
                            var slot = body * 4 + ownership * 2 + family;
                            if (ownership == 0) joints[slot] = PhysicsServer.JointCreate();
                            if (family == 0) PhysicsServer.JointMakePin(joints[slot], Vector2.Zero, id, bodies[1 - body]);
                            else PhysicsServer.JointMakeGroove(joints[slot], Vector2.Zero, Vector2.Zero, Vector2.Zero, bodies[1 - body], id);
                            PhysicsServer.JointSetMaxBias(joints[slot], 0); PhysicsServer.JointSetMaxForce(joints[slot], 0);
                        }
                }
                Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
                    "Joint impact runs on the requested implementation without fallback");
                for (var variant = 0; variant < 128; variant++) Frame(variant, 50, true);
                for (var variant = 0; variant < 128; variant++) { Frame(variant, 0); Frame(variant, 50); }
                foreach (var variant in new[] { 0, 31, 63, 95, 127 })
                {
                    Configure(variant, 50, false); using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
                    var body = (variant >> 1) & 1; Step(body); Verify(variant, 50, false);
                    var pose = states[body].Transform; var velocity = states[body].LinearVelocity; var impulse = states[body].GetContactImpulse(0);
                    checkpoint.Restore(); Step(body); Verify(variant, 50, false);
                    Check(states[body].Transform.Origin.DistanceTo(pose.Origin) < .001f && states[body].LinearVelocity.DistanceTo(velocity) < .002f &&
                        states[body].GetContactImpulse(0).DistanceTo(impulse) < .01f, "Private replay retains impact budget and full contact impulse");
                }
                for (var variant = 0; variant < 256; variant++) Frame(variant & 127, (variant & 128) == 0 ? 0 : 50);
                Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var variant = 0; variant < 256; variant++) Frame(variant & 127, (variant & 128) == 0 ? 0 : 50);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"Complete reversing joint-impact frames allocate {own}/{all} B");
                Console.WriteLine($"Joint impact budget {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: 128 discrete controls, 256 ray/shape cases, five private replays, speed error={maxSpeedError:F6}, joint impulse={maxJointImpulse:F6}, reach excess={maxReachExcess:F6}; 256 measured moving frames {own}/{all} B.");

                void Frame(int variant, float force, bool discrete = false) { Configure(variant, force, discrete); Step((variant >> 1) & 1); Verify(variant, force, discrete); }
                void Configure(int variant, float force, bool discrete)
                {
                    var body = (variant >> 1) & 1; var slot = body * 4 + ((variant >> 2) & 1) * 2 + ((variant >> 3) & 1);
                    var sign = (variant & 16) == 0 ? 1 : -1; var position = 8 + 15 * ((variant >> 5) & 3);
                    for (var i = 0; i < joints.Length; i++) PhysicsServer.JointSetMaxForce(joints[i], i == slot ? force : 0);
                    for (var i = 0; i < bodies.Length; i++)
                    {
                        PhysicsServer.BodySetTransform(bodies[i], new(0, i == body ? Vector2.Zero : new Vector2(0, 1000)));
                        PhysicsServer.BodySetLinearVelocity(bodies[i], i == body ? new Vector2(sign * 3000, 0) : Vector2.Zero);
                        PhysicsServer.BodySetAngularVelocity(bodies[i], 0);
                        PhysicsServer.BodySetContinuousCollisionDetectionMode(bodies[i], i != body || discrete ? CCDMode.Disabled : (variant & 1) == 0 ? CCDMode.CastRay : CCDMode.CastShape);
                    }
                    PhysicsServer.BodySetTransform(target, new(0, new Vector2(sign * position, 0)));
                }
                void Step(int body)
                {
                    var before = callbacks[body]; var tick = PhysicsServer.SpaceGetTick(world.Space); tree.PhysicsFrame(.02);
                    Check(callbacks[body] == before + 1 && PhysicsServer.SpaceGetTick(world.Space) == tick + 1, "Impacts preserve one outer callback and tick");
                }
                void Verify(int variant, float force, bool discrete)
                {
                    var body = (variant >> 1) & 1; var slot = body * 4 + ((variant >> 2) & 1) * 2 + ((variant >> 3) & 1);
                    var sign = (variant & 16) == 0 ? 1 : -1; var state = states[body]; var velocity = state.LinearVelocity;
                    var witness = states[1 - body]; var allowance = force * .02f / 2;
                    // A free witness starting at rest needs total velocity variation at least |2*mean-final|.
                    var required = MathF.Abs(2 * witness.Transform.Origin.X / .02f - witness.LinearVelocity.X);
                    maxReachExcess = MathF.Max(maxReachExcess, required - allowance);
                    if (required > allowance + .003f) throw new InvalidOperationException($"Joint reused spent impulse variant={variant}, force={force}, discrete={discrete}: witness={witness.Transform.Origin}/{witness.LinearVelocity}, required={required}, allowance={allowance}");
                    var speedError = MathF.Abs(velocity.Length() - (3000 - allowance));
                    maxSpeedError = MathF.Max(maxSpeedError, speedError);
                    if (speedError >= .03f) throw new InvalidOperationException($"Reversing joint force budget variant={variant}, force={force}, discrete={discrete}: velocity={velocity}, speed error={speedError}");
                    Check(state.Transform.IsFinite() && MathF.Abs(velocity.Y) < .001f && MathF.Abs(state.AngularVelocity) < .001f &&
                        PhysicsServer.JointGetMaxForce(joints[slot]) == force, "Impact preserves transverse motion, rotation and authored force cap");
                    if (discrete)
                    {
                        Check(sign * state.Transform.Origin.X > 55 && sign * velocity.X > 0 && state.GetContactCount() == 0,
                            "The discrete control crosses the target between its samples without an impact"); return;
                    }
                    Check(sign * velocity.X < 0 && state.GetContactCount() == 1 && state.GetContactCollider(0) == target &&
                        state.GetContactLocalShape(0) == 0 && state.GetContactColliderShape(0) == 0, "Both CCD modes publish the actual reversing target contact");
                    var impulse = state.GetContactImpulse(0); var jointImpulse = (velocity - new Vector2(sign * 3000, 0)) * 2 - impulse;
                    maxJointImpulse = MathF.Max(maxJointImpulse, jointImpulse.Length());
                    Check(impulse.IsFinite() && MathF.Abs(impulse.Y) < .01f && sign * impulse.X < -11000 &&
                        jointImpulse.Length() <= force * .02f + .03f && (jointImpulse + witness.LinearVelocity * 2).Length() < .03f, "Full contact impulse and body momentum retain the joint's independent impulse budget");
                }
            }
            finally
            {
                for (var body = 0; body < 2; body++) for (var family = 0; family < 2; family++) PhysicsServer.FreeRID(joints[body * 4 + family]);
                PhysicsServer.FreeRID(target); PhysicsServer.FreeRID(raw);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
