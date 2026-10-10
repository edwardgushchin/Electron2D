using Electron2D;
using Box2D.NET;

internal static partial class PhysicsBackendOwnershipTests
{
    private static void Initialization(PhysicsServer.Backend backend)
    {
        var old = PhysicsServer.SpaceCreate(backend); var body = PhysicsServer.BodyCreate();
        using var circle = new CircleShape { Radius = 2 };
        PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetSpace(body, old);
        var attachment = PhysicsServer.Service.BodyRuntime(body).Backend.AttachmentVersion;
        try
        {
            var made = false; var initialized = false;
            var space = PhysicsServer.Service.CreateRegisteredSpace(backend, false, owned: true, factory: source =>
            {
                Check(ReferenceEquals(source, PhysicsServer.Service.GetSceneSpace(source.RID)), "Factory receives a registered immutable space RID");
                Check(ReferenceEquals(source, Task.Run(() => PhysicsServer.Service.GetSceneSpace(source.RID)).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult()),
                    "Factory runs outside the registry lock");
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetBackend(source.RID));
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(source.RID));
                Reject<InvalidOperationException>(() => source.InitializeBackend(backend, false, null, null));
                made = true;
                return PhysicsWorldBackend.Create(source, backend, false);
            }, initialize: source =>
            {
                Check(made && !source.IsActive && PhysicsServer.SpaceGetBackend(source.RID) == backend, "Initialization sees its retained selected implementation before activation");
                using var extension = new InitializationQueries(source.RID);
                EmptyQueries(extension);
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(source.RID, 1d / 60));
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetActive(source.RID, true));
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(source.RID));
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(body, source.RID));
                Check(PhysicsServer.BodyGetSpace(body) == old && PhysicsServer.Service.BodyRuntime(body).Backend.AttachmentVersion == attachment,
                    "Rejected initialization attachment transfer preserves the old native membership");
                initialized = true;
            });
            try
            {
                Check(initialized && !space.IsActive, "Explicit spaces publish initialized and inactive");
                Reject<InvalidOperationException>(() => space.InitializeBackend(backend, false, null, null));
                var moving = PhysicsServer.BodyCreate();
                try
                {
                    PhysicsServer.BodyAddShape(moving, circle.GetRID()); PhysicsServer.BodySetCanSleep(moving, false);
                    PhysicsServer.BodySetLinearVelocity(moving, new(30, 0)); PhysicsServer.BodySetSpace(moving, space.RID);
                    PhysicsServer.SpaceSetActive(space.RID, true); PhysicsServer.SpaceStep(space.RID, 1d / 60);
                    Check(PhysicsServer.SpaceGetTick(space.RID) == 1 && PhysicsServer.BodyGetTransform(moving).Origin.X > .4f,
                        "Initialized owner executes the real selected solver step and publishes motion");
                }
                finally { PhysicsServer.FreeRID(moving); }
            }
            finally { PhysicsServer.FreeRID(space.RID); }
            var sceneSpace = PhysicsServer.Service.CreateRegisteredSpace(backend, false, owned: false);
            try { Check(sceneSpace.IsActive, "Scene spaces activate only after shared initialization completes"); }
            finally { sceneSpace.Dispose(); PhysicsServer.Service.UnregisterSceneSpace(sceneSpace.RID); }
            RID failed = default; PhysicsSpace? rejected = null;
            Reject<InvalidOperationException>(() => PhysicsServer.Service.CreateRegisteredSpace(backend, false, true, factory: source =>
            {
                failed = source.RID; rejected = source; throw new InvalidOperationException("Expected factory failure");
            }));
            Retired();
            Reject<InvalidOperationException>(() => PhysicsServer.Service.CreateRegisteredSpace(backend, false, true, factory: source =>
            {
                failed = source.RID; rejected = source; return null!;
            }));
            Retired();
            Reject<InvalidOperationException>(() => PhysicsServer.Service.CreateRegisteredSpace(backend, false, true, factory: source =>
            {
                failed = source.RID; rejected = source; return PhysicsServer.Service.GetSceneSpace(old).BackendImplementation;
            }));
            Retired();
            Check(PhysicsServer.BodyGetTransform(body).IsFinite(), "A rejected foreign implementation is not disposed");
            B2WorldId native = default; GPUPhysicsBodyStore? store = null; PhysicsDirectSpaceState? stale = null;
            Reject<InvalidOperationException>(() => PhysicsServer.Service.CreateRegisteredSpace(backend, false, true, initialize: source =>
            {
                failed = source.RID; rejected = source;
                if (backend == PhysicsServer.Backend.CPU) native = source.WorldID; else store = source.GPUStore;
                stale = PhysicsServer.SpaceGetDirectState(source.RID); EmptyQueries(stale);
                throw new InvalidOperationException("Expected initialization failure after native allocation");
            }));
            Retired();
            using var point = new PhysicsPointQueryParameters();
            Reject<ArgumentException>(() => stale!.IntersectPoint(point));
            if (backend == PhysicsServer.Backend.CPU) Check(!B2Worlds.b2World_IsValid(native), "Failed initialization releases the concrete CPU world");
            else Reject<ObjectDisposedException>(() => store!.Read([], []));
            if (backend == PhysicsServer.Backend.CPU)
            {
                Reject<AggregateException>(() => PhysicsServer.Service.CreateRegisteredSpace(backend, false, true, initialize: source =>
                {
                    failed = source.RID; rejected = source; native = source.WorldID;
                    var tasks = source.BackendImplementation.Tasks;
                    B2Worlds.b2GetWorldFromId(native).workerCount = 2;
                    var job = tasks.Enqueue(static (_, _, _, _) => { }, 128, 32, new object(), tasks);
                    tasks.Finish(job, tasks);
                    source.BackendImplementation.Tasks.Dispose();
                    throw new InvalidOperationException("Expected initialization failure with worker cleanup failure");
                }));
                Retired(); Check(!B2Worlds.b2World_IsValid(native), "Secondary cleanup failure cannot leave the initialized native world or RID alive");
            }
            Console.WriteLine($"{backend}: live registered factory context, real direct-state extension initialization, activation/attachment/release guards, foreign-owner preservation and failed native cleanup passed.");
            void Retired()
            {
                Reject<ArgumentException>(() => PhysicsServer.Service.GetSceneSpace(failed));
                Reject<ArgumentException>(() => PhysicsServer.FreeRID(failed));
                Reject<ObjectDisposedException>(() => rejected!.EnsureQueryAccess());
            }
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(old); }
    }

    private static void EmptyQueries(PhysicsDirectSpaceState state)
    {
        using var ray = PhysicsRayQueryParameters.Create(Vector2.Zero, new(10, 0));
        using var point = new PhysicsPointQueryParameters(); using var circle = new CircleShape { Radius = 2 };
        using var shape = new PhysicsShapeQueryParameters { Shape = circle, Motion = new(10, 0) };
        Check(state.IntersectRay(ray) is null && state.IntersectPoint(point, new PhysicsPointResult[1]) == 0 &&
            state.IntersectShape(shape, new PhysicsShapeResult[1]) == 0 && state.CollideShape(shape, new Vector2[2]) == 0 &&
            state.CastMotion(shape) == (1f, 1f) && state.GetRestInfo(shape) is null, "All six extension queries execute the selected empty native world during initialization");
    }
    private sealed class InitializationQueries : PhysicsDirectSpaceStateExtension
    {
        private readonly PhysicsDirectSpaceState _inner;
        internal InitializationQueries(RID space) : base(space) => _inner = PhysicsServer.SpaceGetDirectState(space);
        protected override PhysicsRayResult? IntersectRayCore(Vector2 from, Vector2 to, uint mask, bool bodies, bool areas, bool inside)
        {
            using var p = new PhysicsRayQueryParameters { From = from, To = to, CollisionMask = mask, CollideWithBodies = bodies, CollideWithAreas = areas, HitFromInside = inside };
            return _inner.IntersectRay(p);
        }
        protected override int IntersectPointCore(Vector2 position, ulong canvas, uint mask, bool bodies, bool areas, Span<PhysicsPointResult> results)
        {
            using var p = new PhysicsPointQueryParameters { Position = position, CanvasInstanceID = canvas, CollisionMask = mask, CollideWithBodies = bodies, CollideWithAreas = areas };
            return _inner.IntersectPoint(p, results);
        }
        private static PhysicsShapeQueryParameters Parameters(RID shape, Transform transform, Vector2 motion, float margin, uint mask, bool bodies, bool areas) =>
            new() { ShapeRID = shape, Transform = transform, Motion = motion, Margin = margin, CollisionMask = mask, CollideWithBodies = bodies, CollideWithAreas = areas };
        protected override int IntersectShapeCore(RID shape, Transform transform, Vector2 motion, float margin, uint mask, bool bodies, bool areas, Span<PhysicsShapeResult> results)
        { using var p = Parameters(shape, transform, motion, margin, mask, bodies, areas); return _inner.IntersectShape(p, results); }
        protected override (float SafeFraction, float UnsafeFraction) CastMotionCore(RID shape, Transform transform, Vector2 motion, float margin, uint mask, bool bodies, bool areas)
        { using var p = Parameters(shape, transform, motion, margin, mask, bodies, areas); return _inner.CastMotion(p); }
        protected override int CollideShapeCore(RID shape, Transform transform, Vector2 motion, float margin, uint mask, bool bodies, bool areas, Span<Vector2> results)
        { using var p = Parameters(shape, transform, motion, margin, mask, bodies, areas); return _inner.CollideShape(p, results); }
        protected override PhysicsRestInfo? GetRestInfoCore(RID shape, Transform transform, Vector2 motion, float margin, uint mask, bool bodies, bool areas)
        { using var p = Parameters(shape, transform, motion, margin, mask, bodies, areas); return _inner.GetRestInfo(p); }
    }
}
