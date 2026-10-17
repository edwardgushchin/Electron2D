using Electron2D;
using Mode = Electron2D.PhysicsServer.BodyMode;

public static partial class BackendChecks
{
    public static void RunSceneModeTransactions(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("SceneModeTransactions", () => solver = new());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("SceneModeTransactions");
            using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 2 };
            var rig = new Skeleton(); var guide = new Bone { Name = "Guide", Position = new(400, 0) }; rig.AddChild(guide); root.AddChild(rig);
            var bone = new PhysicalBone { Name = "Body", BoneNodePath = "../Guide", SimulatePhysics = true };
            PhysicsBody[] bodies = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), bone];
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i]; body.Position = new(i * 100, 0); body.CollisionMask = 0; body.DisableMode = CollisionDisableMode.KeepActive;
                body.AddChild(new CollisionShape { Shape = circle, Disabled = true }); body.AddChild(new CollisionShape { Shape = circle });
                if (body == bone) rig.AddChild(body); else root.AddChild(body);
                if (body is RigidBody rigid) { rigid.GravityScale = 0; rigid.Mass = 2; rigid.CanSleep = false; rigid.LinearDampMode = RigidBody.DampMode.Replace; rigid.AngularDampMode = RigidBody.DampMode.Replace; }
            }
            using var tree = new SceneTree(root);
            var views = bodies.Select(body => PhysicsServer.BodyGetDirectState(body.GetRID())!).ToArray();
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1]; var faults = 0;
            try
            {
                for (var role = 0; role < bodies.Length; role++)
                    for (var operation = 0; operation < (role >= 3 ? (role == 4 ? 11 : 9) : 4); operation++)
                    {
                        if (role == 0) { Setup(role, operation); Apply(role, operation); Verify(role, Target(role, operation)); continue; }
                        var stages = operation < 4
                            ? role >= 3 ? (operation % 2 == 0 ? new[] { 0, 1, 4, 5 } : new[] { 0, 1, 2, 3, 4, 5 })
                                : role == 1 ? new[] { 0, 4, 5, 6 } : new[] { 0, 4, 5 }
                            : operation is 4 or 6 ? new[] { 0, 1, 4, 5 } : new[] { 0, 1, 2, 3, 4, 5 };
                        foreach (var stage in stages)
                            for (var path = 0; path < (solver is null ? 1 : 5); path++)
                                for (var failures = 1; failures <= (solver is null ? 1 : 2); failures++)
                                {
                                    Setup(role, operation); var body = bodies[role]; var id = body.GetRID(); var expected = Target(role, operation);
                                    var attempts = 0; var first = new ApplicationException("scene-mode-publication"); var second = new ArithmeticException("scene-mode-retry");
                                    if (solver is not null) Install(stage, hit =>
                                    {
                                        if (hit != id) return;
                                        if (++attempts > failures) { Clear(); return; }
                                        var state = solver.Bodies[id];
                                        if (stage == 0) state.Mode = expected == Mode.Static ? Mode.Rigid : Mode.Static;
                                        if (stage == 1) state.Locked = expected != Mode.RigidLinear;
                                        if (stage is 4 or 5) state.Mass = 99;
                                        Reject<InvalidOperationException>(() => _ = views[role].InverseMass);
                                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetLinearVelocity(id, Vector2.One));
                                        if (attempts == 1) throw first; throw second;
                                    });
                                    var tick = PhysicsServer.SpaceGetTick(world.Space);
                                    if (solver is null) Apply(role, operation);
                                    else
                                    {
                                        var errors = ReleaseErrors(() => Apply(role, operation));
                                        Check(errors.Length == 1 && ReferenceEquals(errors[0], first) && attempts == 1,
                                            $"Original scene error {role}/{operation}/{stage}");
                                        Check(PhysicsServer.BodyGetMode(id) == expected, "Committed scene mode is an authored read during failure");
                                        if (failures == 2)
                                        {
                                            errors = ReleaseErrors(Recover);
                                            Check(errors.Length == 1 && ReferenceEquals(errors[0], second) && PhysicsServer.SpaceGetTick(world.Space) == tick,
                                                "Repeated preparation preserves the original selected error and tick");
                                        }
                                    }
                                    Recover(); Clear(); Verify(role, expected);
                                    Check(PhysicsServer.SpaceGetTick(world.Space) == tick + (path == 2 ? 1UL : 0), "Only a successful nonzero step advances the tick");
                                    if (solver is not null) { Check(attempts == failures + 1, "Every failed selected stage retries before publication"); faults++; }
                                    void Recover()
                                    {
                                        if (path == 0) _ = views[role].InverseMass;
                                        else if (path == 1) { point.Position = body.GlobalPosition; _ = world.DirectSpaceState.IntersectPoint(point, hits); }
                                        else if (path == 2) tree.PhysicsFrame(.01);
                                        else if (path == 3) _ = PhysicsServer.BodyGetDirectState(id);
                                        else { Apply(role, operation); _ = views[role].InverseMass; }
                                    }
                                }
                        if (solver is not null)
                            for (var lane = 0; lane < 2; lane++)
                            {
                                Setup(role, operation); var body = bodies[role]; var id = body.GetRID(); var attempts = 0;
                                var failure = new ApplicationException("retired-scene-mode"); var mode = Target(role, operation);
                                Install(0, hit => { if (hit == id) { attempts++; solver.Bodies[id].Mode = mode == Mode.Static ? Mode.Rigid : Mode.Static; throw failure; } });
                                var errors = ReleaseErrors(() => Apply(role, operation));
                                Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), "Retirement begins with failed committed scene mode");
                                var old = views[role]; var parent = body.Parent!;
                                if (lane == 0) PhysicsServer.BodySetSpace(id, default); else parent.RemoveChild(body);
                                Check(attempts == 1 && !PhysicsServer.BodyGetSpace(id).IsValid(), "Retiring scene attachment does not retry its failed role");
                                Reject<ObjectDisposedException>(() => _ = old.InverseMass); Clear();
                                if (lane == 0) PhysicsServer.BodySetSpace(id, world.Space);
                                else { if (body == bone) body.RequestReady(); parent.AddChild(body); }
                                views[role] = PhysicsServer.BodyGetDirectState(id)!;
                                try { Verify(role, mode); } catch (Exception error) { throw new InvalidOperationException($"Scene retirement {role}/{operation}/{lane}", error); }
                                faults++;
                            }
                    }
                for (var role = 0; role < 3; role++)
                {
                    Setup(role, 0); bodies[role].ProcessMode = ProcessMode.Always;
                    PhysicsServer.BodySetLinearVelocity(bodies[role].GetRID(), new(9, 0));
                    PhysicsServer.BodySetAngularVelocity(bodies[role].GetRID(), 3);
                    Check(views[role].GetVelocityAtLocalPosition(Vector2.Zero).DistanceTo(new(9, 0)) < .001f,
                        "Native fixed roles expose authored surface motion");
                    PhysicsServer.BodySetMode(bodies[role].GetRID(), Mode.Static);
                    Check(PhysicsServer.BodyGetLinearVelocity(bodies[role].GetRID()) == Vector2.Zero &&
                        views[role].GetVelocityAtLocalPosition(Vector2.Zero) == Vector2.Zero,
                        $"Direct role {role} replaces native surface motion with committed server input");
                    PhysicsServer.BodySetLinearVelocity(bodies[role].GetRID(), new(7, 0));
                    PhysicsServer.BodySetAngularVelocity(bodies[role].GetRID(), 2);
                    var shape = bodies[role].GetChildren().OfType<CollisionShape>().Last();
                    shape.Disabled = true; _ = views[role].InverseMass; shape.Disabled = false;
                    Check(views[role].GetVelocityAtLocalPosition(Vector2.Zero).DistanceTo(new(7, 0)) < .001f,
                        "Shape rebuild preserves the active direct surface profile");
                    PhysicsServer.BodySetMode(bodies[role].GetRID(), Mode.Rigid);
                    Check(views[role].GetVelocityAtLocalPosition(Vector2.Zero).DistanceTo(new(7, 0)) < .001f,
                        "Entering a dynamic role consumes the prior surface without losing committed linear motion");
                    PhysicsServer.BodySetMode(bodies[role].GetRID(), Mode.Static);
                    Check(views[role].GetVelocityAtLocalPosition(Vector2.Zero) == Vector2.Zero,
                        "Leaving a dynamic role clears committed motion without reviving a previous surface");
                }
                // A lock edit preserves actual impulse-derived translation and clears locked angular motion.
                var mover = (RigidBody)bodies[3]; Setup(3, 7); mover.ApplyCentralImpulse(new(4, 0));
                var velocity = views[3].LinearVelocity; mover.LockRotation = true;
                Check(views[3].LinearVelocity.DistanceTo(velocity) < .001f && Math.Abs(views[3].AngularVelocity) < .001f,
                    "Locking rotation preserves untouched selected linear impulse motion");
                Setup(3, 7); mover.FreezeMode = RigidFreezeMode.Kinematic; mover.Freeze = true;
                mover.GlobalPosition += new Vector2(3, 0); mover.LockRotation = true;
                tree.PhysicsFrame(.01);
                Check(Math.Abs(views[3].LinearVelocity.X - 300) < .001f,
                    $"Frozen lock edits retain deferred target motion: {views[3].LinearVelocity}");
                for (var frame = 0; frame < 128; frame++) Warm(frame);
                Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
                for (var frame = 0; frame < 128; frame++) Warm(frame);
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var frame = 0; frame < 128; frame++) Warm(frame);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"Scene mode full frames allocated {own}/{all} owner/all-thread bytes");
                Console.WriteLine($"Scene modes {backend?.ToString() ?? "registered"}, threaded={threaded}: {faults} failure cases; 128 full frames {own}/{all} B.");
            }
            finally { Clear(); foreach (var view in views) view.Dispose(); }

            void Setup(int role, int operation)
            {
                Clear(); var body = bodies[role]; body.ProcessMode = ProcessMode.Always; body.DisableMode = CollisionDisableMode.KeepActive;
                if (body is PhysicalBone physical) physical.SimulatePhysics = true;
                if (body is RigidBody rigid) { rigid.Freeze = false; rigid.LockRotation = false; rigid.FreezeMode = RigidFreezeMode.Static; rigid.LinearVelocity = new(6, 0); rigid.AngularVelocity = 4; }
                body.GlobalPosition = new(role * 100, 0);
                if (operation < 4)
                {
                    body.DisableMode = operation == 2 ? CollisionDisableMode.KeepActive : CollisionDisableMode.MakeStatic;
                    if (operation != 0) body.ProcessMode = ProcessMode.Disabled;
                }
                else if (operation == 5) ((RigidBody)body).Freeze = true;
                else if (operation == 6) ((RigidBody)body).Freeze = true;
                else if (operation == 8) ((RigidBody)body).LockRotation = true;
                else if (operation == 10) ((PhysicalBone)body).SimulatePhysics = false;
            }
            void Apply(int role, int operation)
            {
                var body = bodies[role];
                if (operation == 0) body.ProcessMode = ProcessMode.Disabled;
                else if (operation == 1) body.ProcessMode = ProcessMode.Always;
                else if (operation == 2) body.DisableMode = CollisionDisableMode.MakeStatic;
                else if (operation == 3) body.DisableMode = CollisionDisableMode.KeepActive;
                else if (operation == 4) ((RigidBody)body).Freeze = true;
                else if (operation == 5) ((RigidBody)body).Freeze = false;
                else if (operation == 6) ((RigidBody)body).FreezeMode = RigidFreezeMode.Kinematic;
                else if (operation == 7) ((RigidBody)body).LockRotation = true;
                else if (operation == 8) ((RigidBody)body).LockRotation = false;
                else ((PhysicalBone)body).SimulatePhysics = operation == 10;
            }
            Mode Target(int role, int operation) => operation switch
            {
                0 or 2 or 4 or 9 => Mode.Static,
                6 => Mode.Kinematic,
                7 => Mode.RigidLinear,
                _ => role == 0 ? Mode.Static : role < 3 ? Mode.Kinematic : Mode.Rigid
            };
            void Verify(int role, Mode mode)
            {
                var body = bodies[role]; var state = views[role];
                Check(PhysicsServer.BodyGetMode(body.GetRID()) == mode &&
                    Math.Abs(state.InverseMass - (mode >= Mode.Rigid ? .5f : 0)) < .0001f &&
                    (mode == Mode.Rigid ? state.InverseInertia > 0 : state.InverseInertia == 0), "Selected role and complete mass policy agree");
                point.Position = body.GlobalPosition;
                var count = world.DirectSpaceState.IntersectPoint(point, hits);
                if (body == bone && !bone.SimulatePhysics) Check(count == 0, "Inactive physical bone omits response geometry");
                else Check(count == 1 && hits[0].ColliderRID == body.GetRID() && hits[0].ShapeIndex == 1,
                    "Selected mode publication retains the active logical slot after a disabled gap");
            }
            void Install(int stage, Action<RID> hook)
            {
                if (stage == 0) solver!.ModeHook = hook;
                else if (stage == 1) solver!.RotationHook = hook;
                else if (stage == 2) solver!.BodyHook = hook;
                else if (stage == 3) solver!.AngularHook = hook;
                else if (stage == 4) solver!.ShapeHook = hook;
                else if (stage == 5) solver!.MassHook = hook;
                else solver!.SurfaceHook = hook;
            }
            void Clear()
            {
                if (solver is null) return;
                solver.ModeHook = solver.RotationHook = solver.BodyHook = solver.AngularHook = solver.ShapeHook = solver.MassHook = solver.SurfaceHook = null;
            }
            void Warm(int frame)
            {
                for (var role = 3; role <= 4; role++)
                {
                    var body = (RigidBody)bodies[role];
                    body.ProcessMode = ProcessMode.Always; body.DisableMode = CollisionDisableMode.KeepActive;
                    body.FreezeMode = (frame & 1) == 0 ? RigidFreezeMode.Static : RigidFreezeMode.Kinematic;
                    body.Freeze = (frame & 2) != 0; body.LockRotation = (frame & 1) == 0;
                }
                tree.PhysicsFrame(.01);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
