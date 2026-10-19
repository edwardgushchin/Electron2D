using Electron2D;

public static partial class BackendChecks
{
    public static void RunAttachmentErrors(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("AttachmentErrors", () => solver = new());
        using var circle = new CircleShape { Radius = 1 };
        CollisionObject[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone(), new Area()];
        var raw = PhysicsServer.BodyCreate(); var rawArea = PhysicsServer.AreaCreate(); var stable = PhysicsServer.BodyCreate();
        var space = backend is { } selected ? PhysicsServer.SpaceCreate(selected) : PhysicsServer.SpaceCreate("AttachmentErrors");
        RID[] ids = [raw, rawArea, .. nodes.Select(node => node.GetRID())];
        var views = new PhysicsDirectBodyState?[ids.Length]; var faults = 0; var releases = 0;
        using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        var owner = Thread.CurrentThread; var callbacks = 0; var numerical = 0;
        try
        {
            Check(PhysicsServer.SpaceGetBackend(space) == (backend ?? PhysicsServer.Backend.Extension) && PhysicsServer.SpaceGetBackendFallbackReason(space) is null,
                "Attachment checks use the actual selected owner without fallback");
            PhysicsServer.BodyAddShape(stable, circle.GetRID()); PhysicsServer.BodySetTransform(stable, new(0, new(-100, 0)));
            PhysicsServer.BodySetGravityScale(stable, 0); PhysicsServer.BodySetCanSleep(stable, false); PhysicsServer.BodySetSpace(stable, space);
            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                if (IsArea(i)) { PhysicsServer.AreaAddShape(id, circle.GetRID()); PhysicsServer.AreaSetTransform(id, new(0, new(i * 100, 0))); }
                else
                {
                    PhysicsServer.BodyAddShape(id, circle.GetRID()); PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Rigid);
                    PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetCanSleep(id, false);
                    PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(id, RigidBody.DampMode.Replace);
                }
                if (solver is not null)
                    foreach (var phase in new[] { "factory", "identity", "shape" })
                        foreach (var failCleanup in new[] { false, true })
                        {
                            var original = new ApplicationException($"Initial {phase} failure for collider {i}"); var before = releases;
                            Action<RID> fail = current => { if (current == id) throw original; };
                            solver.ColliderFactoryHook = phase == "factory" ? fail : null;
                            solver.IdentityHook = phase == "identity" ? fail : null;
                            solver.ShapeHook = phase == "shape" ? fail : null;
                            solver.FailBodyDetach = failCleanup; solver.BodyReleaseHook = current => { if (current == id) releases++; };
                            Exception[] errors;
                            try { errors = ReleaseErrors(() => Bind(i, space)); }
                            finally
                            {
                                solver.ColliderFactoryHook = null; solver.IdentityHook = null; solver.ShapeHook = null;
                                solver.FailBodyDetach = false; solver.BodyReleaseHook = null;
                            }
                            Check(errors.Contains(original), $"Initial collider {phase} failure lost its original error for collider {i}");
                            Check(errors.Length == (failCleanup && phase != "factory" ? 2 : 1) && releases - before == (phase == "factory" ? 0 : 1),
                                "Partial attachment preserves every cleanup error and retires a claimed implementation once");
                            Check(Space(i) == default && !solver.Bodies.ContainsKey(id) && solver.Bodies.ContainsKey(stable),
                                "Failed attachment releases target membership and preserves an unrelated numerical body");
                            Check((IsArea(i) ? PhysicsServer.AreaGetShapeCount(id) : PhysicsServer.BodyGetShapeCount(id)) == 1,
                                "Failed creation preserves authored logical shape identity");
                            point.Position = new(-100, 0); point.CollideWithBodies = true; point.CollideWithAreas = false;
                            Check(PhysicsServer.SpaceGetDirectState(space).IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == stable,
                                "An unrelated live selected query survives failed creation and cleanup");
                            faults++;
                        }
                Bind(i, space);
                if (!IsArea(i))
                {
                    views[i] = PhysicsServer.BodyGetDirectState(id);
                    PhysicsServer.BodySetStateSyncCallback(id, _ => { Check(Thread.CurrentThread == owner, "Recovered attachment callbacks retain the owner"); callbacks++; });
                }
            }
            PhysicsServer.SpaceSetActive(space, true);
            if (solver is not null) solver.StepHook = () => { Check((Thread.CurrentThread != owner) == threaded, "Recovered attachments use the captured numerical lane"); numerical++; };
            for (var frame = 0; frame < 128; frame++) Frame(); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); var beforeCallbacks = callbacks; var beforeNumerical = numerical;
            for (var frame = 0; frame < 128; frame++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && callbacks == beforeCallbacks + 768 && (solver is null || numerical == beforeNumerical + 128),
                $"Recovered attachment complete frames allocated {own}/{all} B or lost owner/numerical callbacks");
            if (solver is not null) solver.StepHook = null;
            for (var i = 0; i < ids.Length; i++)
            {
                if (!IsArea(i)) PhysicsServer.BodySetStateSyncCallback(ids[i], null);
                Bind(i, default); Check(Space(i) == default, "Recovered attachments retire normally");
                if (views[i] is { } view) Reject<ObjectDisposedException>(() => _ = view.LinearVelocity);
            }
            Check(solver is null || solver.Bodies.Count == 1, "Retired recovered colliders leave only the unrelated live owner");
            Console.WriteLine($"Attachment errors {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: {faults} initial failures, {releases} cleanup calls, eight recovered colliders, 128 complete frames and 768 owner callbacks 0/0 B passed.");
        }
        finally
        {
            if (solver is not null) { solver.StepHook = null; solver.FailBodyDetach = false; solver.BodyReleaseHook = null; }
            foreach (var node in nodes) node.Dispose();
            PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(rawArea); PhysicsServer.FreeRID(stable); PhysicsServer.FreeRID(space);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous);
        }
        static bool IsArea(int i) => i is 1 or 7;
        RID Space(int i) => IsArea(i) ? PhysicsServer.AreaGetSpace(ids[i]) : PhysicsServer.BodyGetSpace(ids[i]);
        void Bind(int i, RID target) { if (IsArea(i)) PhysicsServer.AreaSetSpace(ids[i], target); else PhysicsServer.BodySetSpace(ids[i], target); }
        void Frame()
        {
            for (var i = 0; i < ids.Length; i++)
                if (!IsArea(i)) { PhysicsServer.BodySetTransform(ids[i], new(0, new(i * 100, 0))); PhysicsServer.BodySetLinearVelocity(ids[i], Vector2.Right); }
            var tick = PhysicsServer.SpaceGetTick(space); PhysicsServer.SpaceStep(space, .01);
            Check(PhysicsServer.SpaceGetTick(space) == tick + 1, "Recovered creation commits one complete selected tick");
            for (var i = 0; i < ids.Length; i++)
            {
                point.CollideWithBodies = !IsArea(i); point.CollideWithAreas = IsArea(i);
                point.Position = IsArea(i) ? PhysicsServer.AreaGetTransform(ids[i]).Origin : PhysicsServer.BodyGetTransform(ids[i]).Origin;
                Check(MathF.Abs(point.Position.X - i * 100 - (IsArea(i) ? 0 : .01f)) < .0001f,
                    "Recovered creation publishes actual scene/raw motion while Areas remain stationary");
                Check(PhysicsServer.SpaceGetDirectState(space).IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == ids[i] && hits[0].ShapeIndex == 0,
                    "Recovered selected queries preserve every scene/raw body/Area and logical shape identity");
            }
        }
    }
}
