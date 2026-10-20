using Electron2D;

public static partial class BackendChecks
{
    public static void RunCrossedCapsules(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        var owner = Thread.CurrentThread;
        if (backend is null) PhysicsServerManager.RegisterServer("CrossedCapsules", () => new Solver
        {
            StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Crossed capsule solve uses the requested thread")
        });
        try
        {
            foreach (var mode in new[] { CCDMode.Disabled, CCDMode.CastRay, CCDMode.CastShape })
                for (var roles = 0; roles < 4; roles++)
                    foreach (var locked in new[] { false, true }) Run(mode, roles, locked);
            Console.WriteLine($"Crossed capsules {backend?.ToString() ?? "registered"}, threaded={threaded}: 24 scene/raw pairs, full paired impulses, angular momentum, energy, portable wire state and silent replay passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }

        void Run(CCDMode mode, int roles, bool locked)
        {
            using var world = backend is { } selected ? new World(selected) : new World("CrossedCapsules");
            Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
                "Crossed capsules retain their selected numerical owner without fallback");
            using var root = new SubViewport { World = world };
            using var capsule = new CapsuleShape { Radius = 1, Height = 6 };
            using var disabled = new CircleShape { Radius = 10 };
            var aScene = new RigidBody { Name = "First" }; var bScene = new RigidBody { Name = "Second" };
            var a = (roles & 1) == 0 ? Scene(aScene) : PhysicsServer.BodyCreate();
            var b = (roles & 2) == 0 ? Scene(bScene) : PhysicsServer.BodyCreate();
            var ticker = new RigidBody
            {
                Name = "Ticker",
                Mass = 2,
                GravityScale = 0,
                CanSleep = false,
                CollisionLayer = 0,
                CollisionMask = 0,
                LinearDampMode = RigidBody.DampMode.Replace,
                LinearDamp = 0,
                ConstantForce = new Vector2(6, 0)
            };
            root.AddChild(ticker); using var tree = new SceneTree(root);
            try
            {
                var rotation = mode == CCDMode.Disabled ? 0 : .41f;
                var frame = new Transform(rotation, mode == CCDMode.Disabled ? Vector2.Zero : new Vector2(13, -9));
                var normal = new Vector2(MathF.Sqrt(3.64f) / 2, .3f).Rotated(rotation);
                var gap = mode == CCDMode.CastShape ? .05f : -.01f;
                Configure(a, 2, frame, normal * 5);
                Configure(b, 3, frame * new Transform(MathF.Atan2(.6f, .8f), new Vector2(1.2f + MathF.Sqrt(3.64f), 1) + gap * normal.Rotated(-rotation)), -normal * 5);
                using var stateA = PhysicsServer.BodyGetDirectState(a)!; using var stateB = PhysicsServer.BodyGetDirectState(b)!;
                var callbacks = 0; var entries = 0;
                PhysicsServer.BodySetStateSyncCallback(ticker.GetRID(), _ => callbacks++);
                if ((roles & 1) == 0) { aScene.ContactMonitor = true; aScene.BodyEntered += _ => entries++; }
                using var map = new PhysicsSnapshotMap(world.Space); map.Bind(10, 1, a); map.Bind(20, 1, b); map.Bind(30, 1, ticker.GetRID());
                var snapshot = new PhysicsSnapshot(65536); var received = new PhysicsSnapshot(65536); var packet = new byte[65536];
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
                var dt = mode == CCDMode.CastShape ? .01 : 1d / 2048;
                Frame(); for (var i = 0; i < 128; i++) Frame(); Collect();
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame();
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"Crossed capsule {mode}/{roles}/{locked} replay allocated {own}/{all} B");
                Console.WriteLine($"Crossed capsule {mode}, roles={roles}, locked={locked}: 128 warm/128 measured full frames {own}/{all} B.");

                void Frame()
                {
                    var count = callbacks; var enters = entries; checkpoint.Restore();
                    Check(callbacks == count && entries == enters, "Checkpoint restoration is silent");
                    var tick = PhysicsServer.SpaceGetTick(world.Space); tree.PhysicsFrame(dt);
                    Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1 && callbacks == count + 1,
                        "Crossed contact completes one world tick and state callback");
                    Check(MathF.Abs(ticker.LinearVelocity.X - 3 * (float)dt) < .0001f, "A separate scene control integrates its force once");
                    if ((roles & 1) == 0 && (roles & 2) == 0)
                        Check(entries == enters + 1, "Each replayed crossed contact delivers one scene entry");
                    var va = stateA.LinearVelocity; var vb = stateB.LinearVelocity;
                    var ja = Verify(stateA, a, b, (roles & 2) == 0 ? bScene : null, 2, normal * 5);
                    var jb = Verify(stateB, b, a, (roles & 1) == 0 ? aScene : null, 3, -normal * 5);
                    Check((2 * va + 3 * vb + normal * 5).Length() < .03f && (ja + jb).Length() < .03f,
                        "Crossed contacts conserve paired linear momentum and report opposite impulses");
                    var energy = va.LengthSquared() + 1.5f * vb.LengthSquared() + 10 * stateA.AngularVelocity * stateA.AngularVelocity + 15 * stateB.AngularVelocity * stateB.AngularVelocity;
                    if (!(energy <= 62.6f && (vb - va).Dot(normal) > 0))
                        throw new InvalidOperationException($"Crossed capsule response {mode}/{roles}/{locked}: velocities={va}/{vb}, angular={stateA.AngularVelocity}/{stateB.AngularVelocity}, impulses={ja}/{jb}, energy={energy}, separation={(vb - va).Dot(normal)}.");
                    if (locked) Check(stateA.AngularVelocity == 0 && stateB.AngularVelocity == 0, "Locked capsule axes retain zero angular velocity");
                    else Check(stateA.AngularVelocity > .01f && stateB.AngularVelocity < -.01f, "Crossed endcaps produce opposite off-centre rotation");
                    var beforeApply = entries; map.Capture(snapshot); var size = snapshot.WriteTo(packet);
                    received.ReadFrom(packet.AsSpan(0, size)); map.Apply(received);
                    Check(entries == beforeApply && callbacks == count + 1 && PhysicsServer.SpaceGetTick(world.Space) == tick + 1,
                        "Portable crossed-contact encoding and application preserve tick and observers silently");
                    Check(Verify(stateA, a, b, (roles & 2) == 0 ? bScene : null, 2, normal * 5).DistanceTo(ja) < .0001f &&
                        Verify(stateB, b, a, (roles & 1) == 0 ? aScene : null, 3, -normal * 5).DistanceTo(jb) < .0001f,
                        "Portable application retains complete paired impulses and angular state");
                }

                Vector2 Verify(PhysicsDirectBodyState state, RID body, RID peer, RigidBody? peerScene, float mass, Vector2 before)
                {
                    var impulse = Vector2.Zero; var moment = 0f;
                    var centre = PhysicsServer.BodyGetTransform(body) * state.CenterOfMassLocal;
                    Check(state.GetContactCount() > 0, "Crossed capsule overlap produces a real solved contact");
                    for (var i = 0; i < state.GetContactCount(); i++)
                    {
                        Check(state.GetContactCollider(i) == peer && state.GetContactLocalShape(i) == 1 && state.GetContactColliderShape(i) == 1 &&
                            ReferenceEquals(state.GetContactColliderObject(i), peerScene), "Crossed contact retains enabled logical slots and destination object identity");
                        var value = state.GetContactImpulse(i); var arm = state.GetContactLocalPosition(i) - centre;
                        impulse += value; moment += arm.X * value.Y - arm.Y * value.X;
                    }
                    Check((impulse - mass * (state.LinearVelocity - before)).Length() < .03f, "Complete crossed contact impulse explains linear momentum change");
                    // Published contact anchors can precede final pose integration by this interval.
                    var limit = .01f + (float)dt * MathF.Max(5, state.LinearVelocity.Length()) * impulse.Length();
                    if (!locked) Check(MathF.Abs(moment - mass * 10 * state.AngularVelocity) < limit,
                        "Complete crossed contact moment explains angular momentum change");
                    return impulse;
                }

                void Configure(RID body, float mass, Transform pose, Vector2 velocity)
                {
                    PhysicsServer.BodySetMode(body, locked ? PhysicsServer.BodyMode.RigidLinear : PhysicsServer.BodyMode.Rigid);
                    if (body != aScene.GetRID() && body != bScene.GetRID())
                    { PhysicsServer.BodyAddShape(body, disabled.GetRID(), disabled: true); PhysicsServer.BodyAddShape(body, capsule.GetRID()); }
                    PhysicsServer.BodySetMass(body, mass); PhysicsServer.BodySetInertia(body, mass * 10);
                    PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
                    PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(body, 0);
                    PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(body, 0);
                    PhysicsServer.BodySetFriction(body, 0); PhysicsServer.BodySetBounce(body, .5f);
                    PhysicsServer.BodySetMaxContactsReported(body, 8); PhysicsServer.BodySetContinuousCollisionDetectionMode(body, mode);
                    PhysicsServer.BodySetTransform(body, pose); PhysicsServer.BodySetLinearVelocity(body, velocity);
                    if (body != aScene.GetRID() && body != bScene.GetRID()) PhysicsServer.BodySetSpace(body, world.Space);
                }
            }
            finally
            {
                if ((roles & 1) != 0) PhysicsServer.FreeRID(a);
                if ((roles & 2) != 0) PhysicsServer.FreeRID(b);
                aScene.Dispose(); bScene.Dispose();
            }
            RID Scene(RigidBody body)
            {
                body.AddChild(new CollisionShape { Name = "Disabled", Shape = disabled, Disabled = true });
                body.AddChild(new CollisionShape { Name = "Capsule", Shape = capsule });
                root.AddChild(body); return body.GetRID();
            }
        }
    }
}
