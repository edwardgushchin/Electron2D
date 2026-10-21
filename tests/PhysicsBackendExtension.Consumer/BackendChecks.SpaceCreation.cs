using Electron2D;

public static partial class BackendChecks
{
    public static void RunSpaceCreation(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        using var circle = new CircleShape { Radius = 1 };
        using var point = new PhysicsPointQueryParameters(); using var ray = new PhysicsRayQueryParameters { From = new(-2, 0), To = new(2, 0) };
        using var shape = new PhysicsShapeQueryParameters { Shape = circle, Motion = Vector2.Right };
        var hits = new PhysicsPointResult[1]; var shapes = new PhysicsShapeResult[1]; var contacts = new Vector2[2];
        var providers = new List<Solver>(); var views = new Dictionary<RID, PhysicsDirectSpaceState>(); var workers = new Dictionary<RID, Thread>();
        var owner = Thread.CurrentThread; var initializationQueries = 0; var measuredFrames = 0; var callbackFrames = 0; var failures = 0;
        var detached = PhysicsServer.BodyCreate(); var registeredOutsideLock = false;
        var name = backend?.ToString() ?? "SpaceCreation";
        PhysicsServerManager.RegisterServer("SpaceCreation", () =>
        {
            if (!registeredOutsideLock)
            {
                var registration = Task.Run(() => PhysicsServerManager.RegisterServer("SpaceCreationWorker", () => new Solver()));
                Check(registration.Wait(TimeSpan.FromSeconds(5)), "Worker registration finishes before the registry deadlock deadline");
                registration.GetAwaiter().GetResult();
                registeredOutsideLock = true;
            }
            var created = new Solver(); providers.Add(created);
            created.InitHook = () =>
            {
                Check(Thread.CurrentThread == owner && !PhysicsServer.SpaceIsActive(created.Space) && PhysicsServer.SpaceGetTick(created.Space) == 0,
                    "Selected initialization sees an inactive engine-assigned context on its owner");
                var view = PhysicsServer.SpaceGetDirectState(created.Space); views.Add(created.Space, view); var mask = 0;
                created.SpaceQueryHook = kind => mask |= 1 << kind;
                try { Empty(view); } finally { created.SpaceQueryHook = null; }
                Check(mask == 63, "All six real selected query families execute inside readonly initialization"); initializationQueries += 6;
                var stepErrors = ReleaseErrors(() => PhysicsServer.SpaceStep(created.Space, 0));
                Check(stepErrors.Length == 1 && stepErrors[0] is InvalidOperationException or ArgumentException && created.Steps == 0,
                    "Owned initialization and scene ownership both reject explicit stepping before any solve");
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetActive(created.Space, true));
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(created.Space));
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(detached, created.Space));
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetSolverIterations(created.Space, PhysicsServer.SpaceGetSolverIterations(created.Space)));
                Check(PhysicsServer.BodyGetSpace(detached) == default, "Rejected initialization attachment preserves detached authoring");
            };
            return created;
        });
        try
        {
            PhysicsServerManager.SetDefaultServer("SpaceCreation", int.MaxValue - 1);
            (Func<RID> Create, PhysicsServer.Backend Expected, string Name)[] forms = [
                (() => PhysicsServer.SpaceCreate(), PhysicsServer.Backend.CPU, "CPU"),
                (() => PhysicsServer.SpaceCreate(backend ?? PhysicsServer.Backend.CPU, true), backend ?? PhysicsServer.Backend.CPU, backend?.ToString() ?? "CPU"),
                (() => PhysicsServer.SpaceCreate(name), backend ?? PhysicsServer.Backend.Extension, name),
                (() => PhysicsServer.SpaceCreate(name, true), backend ?? PhysicsServer.Backend.Extension, name)];
            foreach (var form in forms)
            {
                var space = form.Create(); var body = PhysicsServer.BodyCreate(); var provider = providers.Find(value => value.Space == space);
                try
                {
                    Metadata(space, form.Expected, form.Name, false);
                    ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, !threaded);
                    var view = PhysicsServer.SpaceGetDirectState(space);
                    Check(!views.TryGetValue(space, out var initial) || ReferenceEquals(initial, view), "The initialization view remains the cached completed view");
                    Empty(view); PhysicsServer.SpaceStep(space, .01); Check(PhysicsServer.SpaceGetTick(space) == 0, "New explicit spaces skip inactive intervals");
                    Prepare(body, space); Exercise(space, [body], delta => PhysicsServer.SpaceStep(space, delta), provider);
                }
                finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded); PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
                Retired(space, provider);
            }
            using (var world = new World(name))
            using (var alias = (World)world.Duplicate())
            {
                var calls = providers.Count; ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, !threaded);
                var space = alias.Space; var provider = providers.Find(value => value.Space == space);
                Check(world.Space == space && (backend is not null || providers.Count == calls + 1), "Lazy World aliases allocate one selected physics space");
                Metadata(space, backend ?? PhysicsServer.Backend.Extension, name, true); world.Dispose(); Check(alias.Space == space, "A retained alias survives release of its original resource");
                using var root = new SubViewport { World = alias }; var node = new RigidBody { GravityScale = 0, CanSleep = false };
                node.AddChild(new CollisionShape { Shape = circle }); root.AddChild(node); using var tree = new SceneTree(root);
                var raw = PhysicsServer.BodyCreate();
                try
                {
                    Prepare(raw, space); PhysicsServer.BodySetLinearDampMode(node.GetRID(), RigidBody.DampMode.Replace);
                    PhysicsServer.BodySetAngularDampMode(node.GetRID(), RigidBody.DampMode.Replace);
                    Exercise(space, [node.GetRID(), raw], tree.PhysicsFrame, provider);
                }
                finally { PhysicsServer.FreeRID(raw); tree.Dispose(); }
                Check(PhysicsServer.SpaceGetBackendName(space) == name, "Resource ownership retains the unbound scene world after tree departure");
                alias.Dispose(); Retired(space, provider); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
            }
            using (var captured = new World("DEFAULT"))
            {
                PhysicsServerManager.SetDefaultServer("CPU", int.MaxValue); using var later = new World("DEFAULT");
                Check(captured.RequestedPhysicsBackendName == "DEFAULT" && captured.PhysicsBackendName == "SpaceCreation" && later.PhysicsBackendName == "CPU",
                    "DEFAULT captures a registration while later creation observes the new priority");
                var defaults = PhysicsServer.SpaceCreate("DEFAULT");
                try { Metadata(defaults, PhysicsServer.Backend.CPU, "DEFAULT", false, "CPU"); } finally { PhysicsServer.FreeRID(defaults); }
            }
            SpaceCreationFailures(providers, point, hits, ref failures);
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.SpaceCreate(PhysicsServer.Backend.Extension));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.SpaceCreate((PhysicsServer.Backend)(-1)));
            Reject<ArgumentException>(() => PhysicsServer.SpaceCreate("SpaceCreationMissing")); Reject<ArgumentException>(() => PhysicsServer.SpaceCreate(" "));
            Reject<ArgumentNullException>(() => PhysicsServer.SpaceCreate(null!));
            Console.WriteLine($"Space creation {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: {initializationQueries} readonly initialization queries, {failures} failures, {measuredFrames} complete measured frames and {callbackFrames} owner callbacks 0/0 B passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); PhysicsServer.FreeRID(detached); }

        void Empty(PhysicsDirectSpaceState view)
        {
            Check(view.IntersectPoint(point, hits) == 0 && view.IntersectRay(ray) is null && view.IntersectShape(shape, shapes) == 0 &&
                view.CastMotion(shape) == (1, 1) && view.CollideShape(shape, contacts) == 0 && view.GetRestInfo(shape) is null,
                "All six empty-world queries return real empty selected results");
        }
        void Prepare(RID body, RID space)
        {
            PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
            PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace);
            PhysicsServer.BodySetSpace(body, space);
        }
        void Exercise(RID space, RID[] bodies, Action<double> tick, Solver? provider)
        {
            PhysicsServer.SpaceSetActive(space, true); var view = PhysicsServer.SpaceGetDirectState(space); var callbacks = 0; var hooks = 0;
            foreach (var body in bodies) PhysicsServer.BodySetStateSyncCallback(body, _ => { Check(Thread.CurrentThread == owner, "Creation callbacks retain their owner"); callbacks++; });
            if (provider is not null) provider.StepHook = () =>
            {
                Check((Thread.CurrentThread != owner) == threaded, "The creation policy captures the actual numerical lane");
                if (threaded) workers[space] = Thread.CurrentThread; hooks++;
            };
            try
            {
                Task.Run(() => Reject<InvalidOperationException>(() => tick(0))).GetAwaiter().GetResult();
                tick(0); Check(PhysicsServer.SpaceGetTick(space) == 0, "A zero interval does not initialize or run a numerical worker");
                for (var frame = 0; frame < 128; frame++) Frame(); Collect();
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); var before = callbacks; var beforeHooks = hooks;
                for (var frame = 0; frame < 128; frame++) Frame();
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0 && callbacks == before + 128 * bodies.Length && (provider is null || hooks == beforeHooks + 128),
                    $"Created-space full frames allocated {own}/{all} B or omitted completed hooks/callbacks");
                measuredFrames += 128; callbackFrames += callbacks - before;
            }
            finally { if (provider is not null) provider.StepHook = null; foreach (var body in bodies) PhysicsServer.BodySetStateSyncCallback(body, null); }
            void Frame()
            {
                for (var i = 0; i < bodies.Length; i++)
                {
                    PhysicsServer.BodySetTransform(bodies[i], new(0, new(i * 100, 0))); PhysicsServer.BodySetLinearVelocity(bodies[i], Vector2.Right);
                }
                var at = PhysicsServer.SpaceGetTick(space); tick(.01);
                Check(PhysicsServer.SpaceGetTick(space) == at + 1, "Each active created-world interval commits one tick");
                for (var i = 0; i < bodies.Length; i++)
                {
                    var position = PhysicsServer.BodyGetTransform(bodies[i]).Origin;
                    Check(MathF.Abs(position.X - i * 100 - .01f) < .0001f && MathF.Abs(PhysicsServer.BodyGetLinearVelocity(bodies[i]).X - 1) < .0001f,
                        "Selected creation executes actual scene/raw motion in scene units");
                    point.Position = position;
                    Check(view.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == bodies[i] && hits[0].ShapeIndex == 0,
                        "Created-world queries retain the actual local body/shape identities");
                }
            }
        }
        void Retired(RID space, Solver? provider)
        {
            Reject<ArgumentException>(() => PhysicsServer.SpaceGetBackendName(space));
            if (views.TryGetValue(space, out var state)) Reject<ArgumentException>(() => state.IntersectPoint(point, hits));
            if (provider is not null) Check(provider.Finished && provider.IsDisposed, "Creation releases its selected implementation once");
            if (workers.TryGetValue(space, out var thread)) Check(!thread.IsAlive, "Freeing the captured world joins its retained numerical thread");
        }
        static void Metadata(RID space, PhysicsServer.Backend expected, string requested, bool active, string? actual = null)
        {
            Check(PhysicsServer.SpaceGetRequestedBackend(space) == expected && PhysicsServer.SpaceGetBackend(space) == expected &&
                PhysicsServer.SpaceGetRequestedBackendName(space) == requested && PhysicsServer.SpaceGetBackendName(space) == (actual ?? requested) &&
                PhysicsServer.SpaceGetBackendFallbackReason(space) is null && PhysicsServer.SpaceIsActive(space) == active && PhysicsServer.SpaceGetTick(space) == 0,
                "Creation publishes requested/actual identity, fallback diagnostics, initial activity and zero tick");
        }
    }

    private static void SpaceCreationFailures(List<Solver> providers, PhysicsPointQueryParameters point, PhysicsPointResult[] hits, ref int failures)
    {
        var stable = PhysicsServer.SpaceCreate("SpaceCreation"); var retained = providers[^1];
        try
        {
            foreach (var fallback in new[] { false, true })
                for (var mode = 0; mode < 5; mode++)
                {
                    var failureMode = mode; var name = $"SpaceCreationBad-{fallback}-{mode}"; var original = new ApplicationException(name); Solver? rejected = null;
                    PhysicsServerManager.RegisterServer(name, () =>
                    {
                        if (failureMode == 0) throw original;
                        if (failureMode == 1) return null!;
                        if (failureMode == 2) { rejected = new(); rejected.Dispose(); return rejected; }
                        if (failureMode == 3) return retained;
                        _ = PhysicsServer.SpaceCreate(name); return new Solver();
                    });
                    var errors = ReleaseErrors(() => PhysicsServer.SpaceCreate(name, fallback));
                    Check(errors.Length == 1 && (mode != 0 || ReferenceEquals(errors[0], original)) && !retained.IsDisposed &&
                        PhysicsServer.SpaceGetBackend(stable) == PhysicsServer.Backend.Extension && PhysicsServer.SpaceGetDirectState(stable).IntersectPoint(point, hits) == 0,
                        "Bad factories reject without fallback or damage to an existing world");
                    failures++;
                }
            for (var mask = 0; mask < 4; mask++)
            {
                var name = $"SpaceCreationInit-{mask}"; var original = new ApplicationException(name); Solver? rejected = null; RID rid = default; PhysicsDirectSpaceState? state = null;
                var failFinish = (mask & 1) != 0; var failDispose = (mask & 2) != 0;
                PhysicsServerManager.RegisterServer(name, () =>
                {
                    rejected = new() { FailFinish = failFinish, FailDisposal = failDispose };
                    rejected.InitHook = () => { rid = rejected.Space; state = PhysicsServer.SpaceGetDirectState(rid); throw original; };
                    return rejected;
                });
                var errors = ReleaseErrors(() => PhysicsServer.SpaceCreate(name, allowCPUFallback: true));
                Check(errors.Length == 1 + (failFinish ? 1 : 0) + (failDispose ? 1 : 0) && errors.Contains(original) && rejected!.Finished && rejected.IsDisposed,
                    "Initialization and secondary cleanup errors survive complete selected retirement");
                Reject<ArgumentException>(() => PhysicsServer.SpaceGetBackendName(rid)); Reject<ArgumentException>(() => state!.IntersectPoint(point, hits));
                Check(PhysicsServer.SpaceGetBackendName(stable) == "SpaceCreation" && !retained.IsDisposed, "Initialization cleanup preserves unrelated registration and world identity"); failures++;
            }
        }
        finally { PhysicsServer.FreeRID(stable); }
        Check(retained.Finished && retained.IsDisposed, "The preserved owner remains normally releasable after failed creations");
    }

    public static void RunSpaceStartupUnavailable(bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            foreach (var named in new[] { false, true })
            {
                Reject<InvalidOperationException>(() => { if (named) PhysicsServer.SpaceCreate("GPU"); else PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU); });
                var space = named ? PhysicsServer.SpaceCreate("GPU", true) : PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU, true);
                var body = PhysicsServer.BodyCreate();
                try
                {
                    Check(PhysicsServer.SpaceGetRequestedBackend(space) == PhysicsServer.Backend.GPU && PhysicsServer.SpaceGetBackend(space) == PhysicsServer.Backend.CPU &&
                        PhysicsServer.SpaceGetRequestedBackendName(space) == "GPU" && PhysicsServer.SpaceGetBackendName(space) == "CPU" &&
                        !string.IsNullOrEmpty(PhysicsServer.SpaceGetBackendFallbackReason(space)) && !PhysicsServer.SpaceIsActive(space),
                        "Unavailable GPU creation publishes an explicit inactive CPU startup fallback");
                    PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
                    PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace);
                    PhysicsServer.BodySetLinearVelocity(body, new(120, 0));
                    PhysicsServer.BodySetSpace(body, space); PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, 1d / 60);
                    Check(MathF.Abs(PhysicsServer.BodyGetTransform(body).Origin.X - 2) < .001f, "Fallback creates a real CPU solver without a graphics device");
                }
                finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
            }
            Console.WriteLine($"Unavailable GPU space creation/{(threaded ? "separate" : "owner")}: enum/named rejection, observable CPU fallback and actual motion passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
