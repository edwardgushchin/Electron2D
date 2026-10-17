using Electron2D;
using Mode = Electron2D.PhysicsServer.BodyMode;

public static partial class BackendChecks
{
    public static void RunPoseTransactions(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("PoseTransactions", () => solver = new());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("PoseTransactions");
            using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 2 };
            CollisionObject[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone(), new Area()];
            for (var i = 0; i < nodes.Length; i++) { nodes[i].Position = new(i * 100, 0); root.AddChild(nodes[i]); }
            using var tree = new SceneTree(root); var raw = PhysicsServer.BodyCreate(); var sensor = PhysicsServer.AreaCreate();
            RID[] ids = [.. nodes.Select(node => node.GetRID()), raw, sensor]; var failures = 0;
            using var point = new PhysicsPointQueryParameters { CollideWithAreas = true }; var hits = new PhysicsPointResult[1];
            try
            {
                for (var i = 0; i < ids.Length; i++)
                {
                    if (IsArea(i))
                    {
                        PhysicsServer.AreaAddShape(ids[i], circle.GetRID(), disabled: true); PhysicsServer.AreaAddShape(ids[i], circle.GetRID());
                        PhysicsServer.AreaSetSpace(ids[i], world.Space);
                    }
                    else
                    {
                        PhysicsServer.BodyAddShape(ids[i], circle.GetRID(), disabled: true); PhysicsServer.BodyAddShape(ids[i], circle.GetRID());
                        PhysicsServer.BodySetSpace(ids[i], world.Space); PhysicsServer.BodySetGravityScale(ids[i], 0); PhysicsServer.BodySetCanSleep(ids[i], false);
                    }
                    Set(i, new(0, new(i * 100, 0)));
                }
                tree.PhysicsFrame(.01);
                for (var i = 0; i < ids.Length; i++)
                    foreach (var mode in new[] { Mode.Static, Mode.Kinematic, Mode.Rigid, Mode.RigidLinear })
                    {
                        if (IsArea(i) && mode != Mode.Static) continue;
                        for (var lane = 0; lane < 6; lane++)
                            for (var count = 1; count <= 2; count++)
                            {
                                Reset(i, mode); var held = IsArea(i) ? null : PhysicsServer.BodyGetDirectState(ids[i]);
                                var batchIDs = new[] { ids[i] }; var batchPoses = new Transform[1];
                                var target = count == 2 && i != 5 ? new Transform(0, new(i * 100, 0)) : new Transform(.3f, new(i * 100, 20)); var attempts = 0;
                                var first = new ApplicationException("pose-apply"); var second = new ArithmeticException("pose-prepare");
                                if (solver is not null) solver.PoseHook = id =>
                                {
                                    if (id != ids[i]) return;
                                    if (++attempts > count) { solver.PoseHook = null; return; }
                                    var body = solver.Bodies[id]; body.Position = target.Origin + new Vector2(40, 0); body.Rotation = 2;
                                    Reject<InvalidOperationException>(() => Set(i, Transform.Identity));
                                    if (attempts == 1) throw first; throw second;
                                };
                                var tick = PhysicsServer.SpaceGetTick(world.Space);
                                if (solver is null) Set(i, target);
                                else
                                {
                                    var errors = ReleaseErrors(() => { if (lane == 4 && held is not null) held.Transform = target; else Set(i, target); if (i == 5) Query(i, target); });
                                    Check(errors.Length == 1 && ReferenceEquals(errors[0], first), "A partial pose publication preserves its original error");
                                    if (i != 6) Check(PoseEqual(Get(i), target) && attempts == 1,
                                        "Scene and Area pose reads retain committed authoring without numerical preparation");
                                    if (i == 5 && count == 2)
                                    {
                                        target = new(0, new(i * 100, 0)); Set(i, target);
                                        Check(PoseEqual(Get(i), target) && attempts == 1, "Deferred Area authoring can return to its cached pose while selected publication is incomplete");
                                    }
                                    if (count == 2)
                                    {
                                        errors = ReleaseErrors(Recover);
                                        Check(errors.Length == 1 && ReferenceEquals(errors[0], second) && PhysicsServer.SpaceGetTick(world.Space) == tick,
                                            $"A failed pose retry stays pending without advancing the tick: {i}/{mode}/{lane}, attempts {attempts}, errors {string.Join(',', errors.Select(error => error.GetType().Name))}");
                                    }
                                }
                                Recover(); if (solver is not null) solver.PoseHook = null;
                                Query(i, target);
                                Check(PoseEqual(Get(i), target) && PhysicsServer.SpaceGetTick(world.Space) == tick + (lane == 1 ? 1UL : 0),
                                    $"Prepared pose and numerical query geometry agree without extra steps: {i}/{mode}/{lane}, pose {Get(i)}, target {target}, tick {PhysicsServer.SpaceGetTick(world.Space)}/{tick}");
                                if (solver is not null) Check(attempts == count + 1, "The selected pose is retried at the explicit preparation boundary");
                                failures += solver is null ? 0 : 1;
                                void Recover()
                                {
                                    if (lane == 1) tree.PhysicsFrame(.01);
                                    else if (lane == 2 && i == 6) _ = PhysicsServer.BodyGetTransform(ids[i]);
                                    else if (lane == 3 && !IsArea(i)) _ = PhysicsServer.BodyGetDirectState(ids[i]);
                                    else if (lane == 4 && !IsArea(i)) Check(PoseEqual(held!.Transform, target), "An existing body view retries incomplete pose publication");
                                    else if (lane == 5 && i == 6)
                                    {
                                        PhysicsServer.BodyGetTransform(world.Space, batchIDs, batchPoses);
                                        Check(PoseEqual(batchPoses[0], target), "Batch poses retain scalar scene/raw semantics after failure");
                                    }
                                    else Query(i, target);
                                }
                            }
                    }
                var primarySolver = solver;
                using (var other = backend is { } choice ? new World(choice) : new World("PoseTransactions"))
                {
                    _ = other.Space;
                    solver = primarySolver;
                    for (var i = 0; i < ids.Length; i++)
                        foreach (var mode in new[] { Mode.Static, Mode.Kinematic, Mode.Rigid, Mode.RigidLinear })
                        {
                            if (IsArea(i) && mode != Mode.Static) continue;
                            for (var transfer = 0; transfer < 2; transfer++)
                            {
                                Reset(i, mode); var old = IsArea(i) ? null : PhysicsServer.BodyGetDirectState(ids[i]);
                                var target = new Transform(.2f, new(i * 100, 30)); var attempts = 0;
                                var failure = new ApplicationException("retired-pose");
                                if (solver is not null) solver.PoseHook = id =>
                                {
                                    if (id != ids[i]) return;
                                    attempts++; solver.Bodies[id].Position = target.Origin + new Vector2(40, 0); throw failure;
                                };
                                if (solver is null) Set(i, target);
                                else
                                {
                                    var errors = ReleaseErrors(() => { Set(i, target); if (i == 5) Query(i, target); });
                                    Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), "Retirement begins with a failed selected pose publication");
                                }
                                Bind(i, transfer == 0 ? default : other.Space);
                                if (solver is not null) Check(attempts == 1, "Retirement does not repair the attachment being discarded");
                                if (old is not null) Reject<InvalidOperationException>(() => _ = old.Transform);
                                if (solver is not null) solver.PoseHook = null;
                                if (transfer != 0)
                                {
                                    point.Position = target.Origin;
                                    Check(other.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == ids[i],
                                        "A new world receives retained authoring instead of the failed solver pose");
                                }
                                Check(PoseEqual(Get(i), target), "Retirement preserves the committed pose");
                                Bind(i, world.Space); Query(i, target);
                                if (old is not null) Check(!ReferenceEquals(old, PhysicsServer.BodyGetDirectState(ids[i])), "Reattachment qualifies a fresh body view");
                                failures += solver is null ? 0 : 1;
                            }
                        }
                }
                if (solver is not null)
                    for (var i = 3; i <= 4; i++)
                    {
                        var rigid = (RigidBody)nodes[i]; rigid.LockRotation = false; Reset(i, Mode.Rigid);
                        var target = new Transform(.2f, new(i * 100, 40)); var attempts = 0; var failure = new ApplicationException("pose-role-takeover");
                        solver.PoseHook = id => { if (id != ids[i]) return; attempts++; solver.Bodies[id].Position = target.Origin + new Vector2(40, 0); throw failure; };
                        var errors = ReleaseErrors(() => Set(i, target));
                        Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), "Scene role takeover begins with incomplete pose publication");
                        errors = ReleaseErrors(() => rigid.LockRotation = true);
                        Check(errors.Length == 1 && ReferenceEquals(errors[0], failure) && attempts == 2 && !rigid.LockRotation && PhysicsServer.BodyGetMode(ids[i]) == Mode.Rigid,
                            "Scene role takeover repairs pending direct pose before changing descriptors");
                        solver.PoseHook = null; rigid.LockRotation = true;
                        Check(PoseEqual(PhysicsServer.BodyGetDirectState(ids[i])!.Transform, target), "Successful scene takeover retains the committed numerical pose");
                        rigid.LockRotation = false; PhysicsServer.BodySetMode(ids[i], Mode.RigidLinear); failures++;
                    }
                Reset(6, Mode.Kinematic); var firstPose = new Transform(0, new(600, 0)); var laterPose = new Transform(0, new(600, 10));
                Set(6, firstPose); Query(6, firstPose); Set(6, laterPose); Query(6, firstPose);
                var beforeZero = PhysicsServer.SpaceGetTick(world.Space); tree.PhysicsFrame(0); Query(6, firstPose);
                Check(PhysicsServer.SpaceGetTick(world.Space) == beforeZero, "Zero delta retains a later kinematic target");
                tree.PhysicsFrame(.01); Query(6, laterPose);
                Check(PoseEqual(Get(6), laterPose), "Later kinematic targets advance only on a nonzero step");
                PhysicsServer.BodySetMode(raw, Mode.RigidLinear);
                for (var frame = 0; frame < 128; frame++) Frame(frame);
                Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var frame = 0; frame < 128; frame++) Frame(frame);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"Changed-pose frames allocated {own}/{all} owner/all-thread bytes");
                Console.WriteLine($"Pose transactions {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: {failures} failure cases and 128 changed-pose full frames {own}/{all} B passed.");
            }
            finally { if (solver is not null) solver.PoseHook = null; PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(sensor); }
            bool PoseEqual(Transform a, Transform b) => (a.Origin - b.Origin).Length() < .001f && (a.X - b.X).Length() < .0001f && (a.Y - b.Y).Length() < .0001f;
            bool IsArea(int i) => i is 5 or 7;
            void Bind(int i, RID space) { if (IsArea(i)) PhysicsServer.AreaSetSpace(ids[i], space); else PhysicsServer.BodySetSpace(ids[i], space); }
            void Set(int i, Transform pose) { if (IsArea(i)) PhysicsServer.AreaSetTransform(ids[i], pose); else PhysicsServer.BodySetTransform(ids[i], pose); }
            Transform Get(int i) => IsArea(i) ? PhysicsServer.AreaGetTransform(ids[i]) : PhysicsServer.BodyGetTransform(ids[i]);
            void Query(int i, Transform pose)
            {
                point.Position = pose.Origin;
                if (world.DirectSpaceState.IntersectPoint(point, hits) != 1 || hits[0].ColliderRID != ids[i] || hits[0].ShapeIndex != 1)
                    throw new InvalidOperationException($"Queries must publish the committed pose with stable RID and logical disabled gap: {i}");
            }
            void Reset(int i, Mode mode)
            {
                if (solver is not null) solver.PoseHook = null;
                if (!IsArea(i)) PhysicsServer.BodySetMode(ids[i], Mode.Rigid);
                Set(i, new(0, new(i * 100, 0))); Query(i, new(0, new(i * 100, 0)));
                if (!IsArea(i)) { PhysicsServer.BodySetMode(ids[i], mode); PhysicsServer.BodySetLinearVelocity(ids[i], default); PhysicsServer.BodySetAngularVelocity(ids[i], 0); }
            }
            void Frame(int frame)
            {
                for (var i = 0; i < ids.Length; i++) Set(i, new(0, new(i * 100, frame & 1)));
                tree.PhysicsFrame(.01);
                for (var i = 0; i < ids.Length; i++) Query(i, new(0, new(i * 100, frame & 1)));
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
