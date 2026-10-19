using Electron2D;

public static partial class BackendChecks
{
    public static void RunColliderCreation(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ColliderCreation", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("ColliderCreation");
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root); using var circle = new CircleShape { Radius = 1 };
        var space = world.Space; var owner = Thread.CurrentThread; var rejected = 0; var entries = 0; var activations = 0;
        var probeCalls = 0; var checkedLock = false; var target = -1;
        CollisionObject[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone(), new Area()];
        var raw = PhysicsServer.BodyCreate(); var rawArea = PhysicsServer.AreaCreate();
        var stable = PhysicsServer.BodyCreate(); var stableArea = PhysicsServer.AreaCreate();
        RID[] ids = [raw, rawArea, .. nodes.Select(node => node.GetRID())]; var ready = new int[nodes.Length];
        using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        var callbacks = 0; var steps = 0;
        try
        {
            Check(solver is null || solver.Bodies.Count == 0, "Detached common RIDs do not allocate numerical colliders");
            Check(raw != rawArea && PhysicsServer.BodyGetSpace(raw) == default && PhysicsServer.AreaGetSpace(rawArea) == default &&
                PhysicsServer.BodyGetMode(raw) == PhysicsServer.BodyMode.Rigid && PhysicsServer.BodyGetContinuousCollisionDetectionMode(raw) == CCDMode.Disabled &&
                PhysicsServer.BodyGetTransform(raw) == Transform.Identity && PhysicsServer.BodyGetLinearVelocity(raw) == Vector2.Zero && PhysicsServer.BodyGetAngularVelocity(raw) == 0 &&
                !PhysicsServer.BodyGetSleeping(raw) && PhysicsServer.BodyGetCanSleep(raw) && PhysicsServer.BodyGetMass(raw) == 1 && PhysicsServer.BodyGetInertia(raw) == 0 &&
                PhysicsServer.BodyGetFriction(raw) == 1 && PhysicsServer.BodyGetBounce(raw) == 0 && PhysicsServer.BodyGetGravityScale(raw) == 1 &&
                PhysicsServer.BodyGetLinearDamp(raw) == 0 && PhysicsServer.BodyGetAngularDamp(raw) == 0 && PhysicsServer.BodyGetLinearDampMode(raw) == RigidBody.DampMode.Combine &&
                PhysicsServer.BodyGetAngularDampMode(raw) == RigidBody.DampMode.Combine && PhysicsServer.BodyGetCollisionLayer(raw) == 1 && PhysicsServer.BodyGetCollisionMask(raw) == 1 &&
                PhysicsServer.AreaGetCollisionLayer(rawArea) == 1 && PhysicsServer.AreaGetCollisionMask(rawArea) == 1 && PhysicsServer.BodyGetObjectInstanceID(raw) == 0 &&
                PhysicsServer.AreaGetObjectInstanceID(rawArea) == 0 && PhysicsServer.BodyGetShapeCount(raw) == 0 && PhysicsServer.AreaGetShapeCount(rawArea) == 0 && PhysicsServer.BodyGetDirectState(raw) is null,
                "Caller-owned Body/Area construction exposes independent detached identities and accepted defaults");
            PhysicsServer.BodyAddShape(stable, circle.GetRID()); PhysicsServer.BodySetTransform(stable, new(0, new(-100, 0)));
            PhysicsServer.BodySetGravityScale(stable, 0); PhysicsServer.BodySetCanSleep(stable, false); PhysicsServer.BodySetSpace(stable, space);
            PhysicsServer.AreaAddShape(stableArea, circle.GetRID()); PhysicsServer.AreaSetTransform(stableArea, new(0, new(-1000, 0))); PhysicsServer.AreaSetSpace(stableArea, space);
            PhysicsBackendCollider?[] reused = solver is null ? [] : [solver.Bodies[stable], solver.Bodies[stableArea], Retired(false), Retired(true)];
            if (solver is not null) solver.ColliderFactoryHook = Probe;
            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                if (IsArea(i)) { if (i < 2) PhysicsServer.AreaAddShape(id, circle.GetRID()); PhysicsServer.AreaSetTransform(id, new(.25f, new(i * 100, 0))); }
                else
                {
                    if (i < 2) PhysicsServer.BodyAddShape(id, circle.GetRID()); PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Rigid);
                    PhysicsServer.BodySetTransform(id, new(.25f, new(i * 100, 0))); PhysicsServer.BodySetMass(id, 2); PhysicsServer.BodySetInertia(id, 3);
                    PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetCanSleep(id, false);
                    PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(id, RigidBody.DampMode.Replace);
                }
                if (i >= 2) { var slot = i - 2; nodes[slot].AddChild(new CollisionShape { Shape = circle }); nodes[slot].Ready += _ => ready[slot]++; }
                var authored = AttachmentAuthoring(id, IsArea(i)); target = i;
                if (solver is not null)
                    for (var kind = 0; kind < 6; kind++)
                    {
                        var original = new ApplicationException($"Collider factory {i}/{kind}");
                        solver.NullColliderFactory = kind == 0; solver.ReusedCollider = kind is >= 1 and <= 4 ? reused[kind - 1] : null;
                        solver.ColliderFactoryHook = current => { Probe(current); if (current == id && kind == 5) throw original; };
                        var beforeTick = PhysicsServer.SpaceGetTick(space); Exception[] errors;
                        try { errors = ReleaseErrors(() => Attach(i)); }
                        finally { solver.NullColliderFactory = false; solver.ReusedCollider = null; solver.ColliderFactoryHook = Probe; }
                        Check(errors.Length > 0 && errors.All(error => kind == 5 ? ReferenceEquals(error, original) : error is InvalidOperationException),
                            "Null, live/cross-kind, retired and throwing collider factories reject through the public creation path");
                        Check(AttachmentAuthoring(id, IsArea(i)) == authored && Space(i) == default && !solver.Bodies.ContainsKey(id) &&
                            !solver.Bodies[stable].Detached && !solver.Bodies[stableArea].Detached && PhysicsServer.SpaceGetTick(space) == beforeTick,
                            "Rejected results retain common authoring, unrelated selected owners and the current tick");
                        FailedScene(i); StableQueries(); rejected++;
                    }
                if (solver is not null && i >= 2)
                    foreach (var phase in new[] { "identity", "shape", "capacity", "monitor" })
                        foreach (var cleanup in new[] { false, true })
                        {
                            var original = new ApplicationException($"Scene entry {i}/{phase}"); Action fail = () => { if (solver.Bodies.ContainsKey(id)) { PoisonAttachment(solver, id); throw original; } };
                            solver.IdentityHook = phase == "identity" ? current => { if (current == id) fail(); } : null;
                            solver.ShapeHook = phase == "shape" ? current => { if (current == id) fail(); } : null;
                            solver.CapacityHook = phase == "capacity" ? fail : null; solver.MonitorCapacityHook = phase == "monitor" ? fail : null; solver.FailBodyDetach = cleanup;
                            Exception[] errors;
                            try { errors = ReleaseErrors(() => Attach(i)); }
                            finally { solver.IdentityHook = null; solver.ShapeHook = null; solver.CapacityHook = null; solver.MonitorCapacityHook = null; solver.FailBodyDetach = false; }
                            Check(errors.Contains(original) && (!cleanup || errors.Any(error => !ReferenceEquals(error, original))) && Space(i) == default &&
                                AttachmentAuthoring(id, IsArea(i)) == authored && !solver.Bodies.ContainsKey(id), "Real live-tree entry rolls back numerical ownership and preserves preparation/cleanup errors");
                            FailedScene(i); StableQueries(); entries++;
                        }
                Attach(i); var calls = probeCalls; var native = solver?.Bodies[id]; var view = IsArea(i) ? null : PhysicsServer.BodyGetDirectState(id);
                Bind(i, space);
                Check(probeCalls == calls && (view is null || ReferenceEquals(view, PhysicsServer.BodyGetDirectState(id))) &&
                    (native is null || native.IsArea == IsArea(i)), "Unchanged membership keeps one selected attachment and cached view with the correct collider role");
                Detach(i);
                if (view is not null) Reject<ObjectDisposedException>(() => _ = view.LinearVelocity);
                if (solver is not null)
                {
                    Check(native!.Detached, "Normal departure retires the selected collider"); solver.ReusedCollider = native;
                    Exception[] errors;
                    try { errors = ReleaseErrors(() => Attach(i)); } finally { solver.ReusedCollider = null; }
                    Check(errors.Length > 0 && errors.All(error => error is InvalidOperationException), "Reclaiming the original retired result reports its permanent claim");
                    Check(Space(i) == default && !solver.Bodies.ContainsKey(id), "A retired result cannot be reclaimed even for its original RID");
                    FailedScene(i); rejected++;
                }
                Attach(i); Check(Space(i) == space && (solver is null || !ReferenceEquals(native, solver.Bodies[id])), "Reentry preserves the logical RID and obtains a fresh numerical owner");
                if (!IsArea(i)) PhysicsServer.BodySetStateSyncCallback(id, _ => { Check(Thread.CurrentThread == owner, "Created body callbacks retain the owner"); callbacks++; });
            }
            target = -1; if (solver is not null) { solver.ColliderFactoryHook = null; ColliderActivationCases(circle, ref activations); solver.StepHook = () => { Check((Thread.CurrentThread != owner) == threaded, "Creation captures the numerical lane"); steps++; }; }
            Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "Collider creation uses the actual selected backend without fallback");
            Task.Run(() => { for (var i = 0; i < ids.Length; i++) Reject<InvalidOperationException>(() => _ = Space(i)); }).GetAwaiter().GetResult();
            for (var frame = 0; frame < 128; frame++) Frame(); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); var beforeCallbacks = callbacks; var beforeSteps = steps;
            for (var frame = 0; frame < 128; frame++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && callbacks == beforeCallbacks + 768 && (solver is null || steps == beforeSteps + 128), $"Created collider tree frames allocated {own}/{all} B or lost callbacks");
            if (solver is not null) solver.StepHook = null;
            Console.WriteLine($"Collider creation {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: {rejected} rejected factory results, {entries} live-entry failures, {activations} activation failures, 128 complete tree frames and 768 owner callbacks 0/0 B passed.");
        }
        finally
        {
            if (solver is not null) { solver.ColliderFactoryHook = null; solver.NullColliderFactory = false; solver.ReusedCollider = null; solver.StepHook = null; }
            foreach (var node in nodes) node.Dispose(); PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(rawArea); PhysicsServer.FreeRID(stable); PhysicsServer.FreeRID(stableArea);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous);
        }
        static bool IsArea(int i) => i is 1 or 7;
        RID Space(int i) => IsArea(i) ? PhysicsServer.AreaGetSpace(ids[i]) : PhysicsServer.BodyGetSpace(ids[i]);
        void Bind(int i, RID selected) { if (IsArea(i)) PhysicsServer.AreaSetSpace(ids[i], selected); else PhysicsServer.BodySetSpace(ids[i], selected); }
        void Attach(int i) { if (i < 2) Bind(i, space); else { var before = ready[i - 2]; nodes[i - 2].RequestReady(); root.AddChild(nodes[i - 2]); Check(nodes[i - 2].IsNodeReady && ready[i - 2] == before + 1, "Successful reentry consumes one readiness request"); } }
        void Detach(int i) { if (i < 2) Bind(i, default); else root.RemoveChild(nodes[i - 2]); }
        void FailedScene(int i)
        {
            if (i < 2) return;
            Check(nodes[i - 2].Parent == root && nodes[i - 2].Tree == tree && !nodes[i - 2].IsNodeReady, "A failed live insertion retains structural membership and does not consume readiness");
            Check(root.RemoveChild(nodes[i - 2]) && nodes[i - 2].Tree is null, "The failed live insertion remains explicitly removable");
        }
        PhysicsBackendCollider Retired(bool area)
        {
            var id = area ? PhysicsServer.AreaCreate() : PhysicsServer.BodyCreate();
            if (area) PhysicsServer.AreaSetSpace(id, space); else PhysicsServer.BodySetSpace(id, space);
            var result = solver!.Bodies[id]; PhysicsServer.FreeRID(id); Check(result.Detached, "The saved factory control has completed retirement"); return result;
        }
        void Probe(RID id)
        {
            if (target < 0 || id != ids[target]) return;
            Check(Thread.CurrentThread == owner && Space(target) == space && (IsArea(target) ? PhysicsServer.AreaGetShapeCount(id) : PhysicsServer.BodyGetShapeCount(id)) >= 1,
                "Collider factories receive registered logical identities and guarded authored input on the owner");
            Reject<InvalidOperationException>(() => Bind(target, default)); probeCalls++;
            if (!checkedLock)
            {
                Task.Run(() => { var detached = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(detached); }).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult(); checkedLock = true;
            }
        }
        void StableQueries()
        {
            point.CollideWithBodies = true; point.CollideWithAreas = false; point.Position = new(-100, 0);
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == stable, "Rejected creation preserves a real unrelated body query");
            point.CollideWithBodies = false; point.CollideWithAreas = true; point.Position = new(-1000, 0);
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == stableArea, "Rejected creation preserves a real unrelated Area query");
        }
        void Frame()
        {
            for (var i = 0; i < ids.Length; i++) if (!IsArea(i)) { PhysicsServer.BodySetTransform(ids[i], new(0, new(i * 100, 0))); PhysicsServer.BodySetLinearVelocity(ids[i], Vector2.Right); }
            var tick = PhysicsServer.SpaceGetTick(space); tree.PhysicsFrame(.01); Check(PhysicsServer.SpaceGetTick(space) == tick + 1, "Created tree colliders commit one complete selected tick");
            for (var i = 0; i < ids.Length; i++)
            {
                point.CollideWithAreas = IsArea(i); point.CollideWithBodies = !IsArea(i); point.Position = IsArea(i) ? PhysicsServer.AreaGetTransform(ids[i]).Origin : PhysicsServer.BodyGetTransform(ids[i]).Origin;
                Check(MathF.Abs(point.Position.X - i * 100 - (IsArea(i) ? 0 : .01f)) < .0001f && world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == ids[i] && hits[0].ShapeIndex == 0,
                    "Created scene/raw bodies move and Areas remain queryable with their original logical identities");
            }
        }
    }

    private static void ColliderActivationCases(Shape shape, ref int cases)
    {
        Solver? provider = null; RID target = default; var nullFactory = true; ApplicationException? original = null;
        PhysicsServerManager.RegisterServer("CreationActivation", () =>
        {
            provider = new() { NullColliderFactory = nullFactory, FailBodyDetach = !nullFactory };
            var selected = provider; selected.CapacityHook = () => { if (!nullFactory && selected.Bodies.ContainsKey(target)) throw original!; }; return selected;
        });
        for (var kind = 0; kind < 6; kind++)
            foreach (var missing in new[] { true, false })
            {
                nullFactory = missing; using var world = new World("CreationActivation"); using var root = new SubViewport { World = world };
                CollisionObject node = kind switch
                {
                    0 => new StaticBody(),
                    1 => new AnimatableBody(),
                    2 => new CharacterBody(),
                    3 => new RigidBody(),
                    4 => new PhysicalBone(),
                    _ => new Area()
                };
                target = node.GetRID(); node.AddChild(new CollisionShape { Shape = shape }); if (node is PhysicsBody) { PhysicsServer.BodySetMode(target, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetGravityScale(target, 0); }
                root.AddChild(node); original = new ApplicationException($"Creation activation {kind}"); var errors = ReleaseErrors(() => _ = new SceneTree(root));
                Check((missing ? errors.Length > 0 && errors.All(error => error is InvalidOperationException) : errors.Contains(original)) && root.Tree is null && node.Tree is null && !node.IsNodeReady && provider!.Bodies.Count == 0 &&
                    (node is Area ? PhysicsServer.AreaGetSpace(target) : PhysicsServer.BodyGetSpace(target)) == default,
                    "Failed whole-tree activation releases every collider, resets readiness and preserves original errors");
                provider!.NullColliderFactory = false; provider.FailBodyDetach = false; provider.CapacityHook = null;
                using var recovered = new SceneTree(root); recovered.PhysicsFrame(.01);
                Check(node.IsNodeReady && (node is Area ? PhysicsServer.AreaGetSpace(target) : PhysicsServer.BodyGetSpace(target)) == world.Space,
                    "The original authored hierarchy successfully activates after a failed construction"); cases++;
            }
    }
}
