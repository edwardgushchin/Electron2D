using Electron2D;

public static partial class BackendChecks
{
    public static void RunFactoryViews(bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { FactoryViewsCore(threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void FactoryViewsCore(bool threaded)
    {
        var owner = Thread.CurrentThread;
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("FactoryViews", () => solver = new()
        { StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Factory views accompany the requested actual Solve lane") });
        using var world = new World("FactoryViews");
        using var circle = new CircleShape { Radius = 2 };
        using var root = new SubViewport { World = world };
        var scene = new ViewRigid { Name = "Scene", GravityScale = 0, CanSleep = false, Mass = 2, MaxContactsReported = 1 };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetMass(raw, 2);
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodySetMaxContactsReported(raw, 1); PhysicsServer.BodySetSpace(raw, world.Space);
            solver!.SpaceViewFactory = space => new CircleQueries(space, [new(scene.GetRID(), circle, false), new(raw, circle, false)], circle);
            solver.BodyViewFactory = body => new OwnedState(solver, body);
            var queries = world.DirectSpaceState;
            var sceneView = (OwnedState)PhysicsServer.BodyGetDirectState(scene.GetRID())!;
            var rawView = (OwnedState)PhysicsServer.BodyGetDirectState(raw)!;
            Check(queries is CircleQueries && ReferenceEquals(queries, world.DirectSpaceState) &&
                ReferenceEquals(sceneView, PhysicsServer.BodyGetDirectState(scene.GetRID())) && ReferenceEquals(rawView, PhysicsServer.BodyGetDirectState(raw)),
                "The external factory provides the cached scene/raw and space views");
            Check(sceneView.GetContactCount() == 0 && rawView.GetContactCount() == 0 && sceneView.ContactReads == 0 && rawView.ContactReads == 0,
                "Fresh cached factory views start with an empty contact snapshot without invoking publication hooks");
            rawView.Transform = new(0, new(20, 0));
            QueryChecks((CircleQueries)queries, scene.GetRID(), raw, circle);
            solver.FilterHook = id =>
            {
                var view = id == raw ? rawView : sceneView;
                Reject<InvalidOperationException>(() => view.CollisionLayer = 3);
                Reject<InvalidOperationException>(() => view.CollisionMask = 5);
                Reject<InvalidOperationException>(() => PhysicsServer.BodyAddShape(id, circle.GetRID()));
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(id));
            };
            StateAccess(rawView);
            StateAccess(sceneView);
            solver.FilterHook = null;
            var deliveries = 0;
            PhysicsServer.BodySetForceIntegrationCallback(raw, state => Observe(state, rawView));
            PhysicsServer.BodySetStateSyncCallback(raw, state => Observe(state, rawView));
            PhysicsServer.BodySetForceIntegrationCallback(scene.GetRID(), state => Observe(state, sceneView));
            scene.Observer = state => Observe(state, sceneView);
            Reset(); tree.PhysicsFrame(1d / 120);
            Check(deliveries == 4 && sceneView.GetContactCount() == 1 && rawView.GetContactCount() == 1,
                "Factory contact projection precedes callbacks with real independent circle response");
            rawView.ContactHook = () =>
            {
                Reject<InvalidOperationException>(() => rawView.Dispose());
                Reject<InvalidOperationException>(() => tree.Dispose());
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetTransform(raw, Transform.Identity));
            };
            Reset(); tree.PhysicsFrame(1d / 120); rawView.ContactHook = null;
            var tick = PhysicsServer.SpaceGetTick(world.Space); var previous = rawView.GetContactImpulse(0);
            rawView.BadContactCount = true; Reset();
            Reject<AggregateException>(() => tree.PhysicsFrame(1d / 120));
            Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1 && rawView.GetContactImpulse(0) == previous,
                "A bad factory contact receiver retains its snapshot while the committed world stays usable");
            rawView.BadContactCount = false; Reset(); tree.PhysicsFrame(1d / 120);
            var publishedReads = rawView.ContactReads;
            using (var independent = new OwnedState(solver, raw))
                Check(independent.GetContactCount() == 1 && independent.ContactReads == 1 && rawView.ContactReads == publishedReads,
                    "Caller-created views retain direct hook dispatch while the cached factory view stays immutable");
            var retainedPoint = sceneView.GetContactColliderPosition(0); var retainedImpulse = sceneView.GetContactImpulse(0);
            using var originalOwner = new Node(); using var replacementOwner = new Node();
            PhysicsServer.BodyAttachObject(raw, originalOwner); Reset(); tree.PhysicsFrame(1d / 120);
            var retainedID = sceneView.GetContactColliderID(0);
            PhysicsServer.BodyAttachObject(raw, replacementOwner); rawView.Transform = new(0, new(30, 0));
            Check(sceneView.GetContactColliderID(0) == retainedID && ReferenceEquals(sceneView.GetContactColliderObject<Node>(0), originalOwner) &&
                sceneView.GetContactImpulse(0) == retainedImpulse && sceneView.GetContactColliderPosition(0) == retainedPoint,
                "Contact identity, geometry and impulse snapshots survive post-publication owner/pose edits");
            Reset(); tree.PhysicsFrame(1d / 120);
            Check(ReferenceEquals(sceneView.GetContactColliderObject<Node>(0), replacementOwner), "The next publication samples the new association");
            PhysicsServer.BodySetMaxContactsReported(raw, 1);
            Check(rawView.GetContactCount() == 0, "Equal contact-cap assignment explicitly clears the cached snapshot");
            Reset(); tree.PhysicsFrame(1d / 120);
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[4];
            Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Cycle();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Cycle();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Factory views allocated {own}/{all} owner/all-thread B");
            FactoryFailures();
            Console.WriteLine($"Public factory views: 128 active measured scene/raw frames with contacts/callbacks/point queries, owner/all={own}/{all} B.");
            void Cycle()
            {
                Reset(); tree.PhysicsFrame(1d / 120); point.Position = sceneView.Transform.Origin;
                Check(queries.IntersectPoint(point, hits) > 0 && rawView.GetContactCount() == 1,
                    "Factory warm frames keep physical contacts and selected geometry queries active");
            }
            void FactoryFailures()
            {
                var goodSpace = solver.SpaceViewFactory; var goodBody = solver.BodyViewFactory;
                queries.Dispose(); solver.SpaceViewFactory = _ => PhysicsServer.SpaceGetDirectState(world.Space);
                Reject<InvalidOperationException>(() => _ = world.DirectSpaceState);
                solver.SpaceViewFactory = _ => null!; Reject<InvalidOperationException>(() => _ = world.DirectSpaceState);
                using var other = new World(); var foreign = other.DirectSpaceState;
                solver.SpaceViewFactory = _ => foreign; Reject<InvalidOperationException>(() => _ = world.DirectSpaceState);
                Check(!foreign.IsDisposed, "A rejected foreign query view remains caller-owned");
                solver.SpaceViewFactory = _ => queries; Reject<ObjectDisposedException>(() => _ = world.DirectSpaceState);
                solver.SpaceViewFactory = goodSpace; queries = world.DirectSpaceState;
                rawView.Dispose(); solver.BodyViewFactory = _ => PhysicsServer.BodyGetDirectState(raw)!;
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetDirectState(raw));
                solver.BodyViewFactory = _ => sceneView; Reject<InvalidOperationException>(() => PhysicsServer.BodyGetDirectState(raw));
                Check(!sceneView.IsDisposed && sceneView.Transform.IsFinite(), "Rejected sibling-body view remains valid");
                solver.BodyViewFactory = _ => null!; Reject<InvalidOperationException>(() => PhysicsServer.BodyGetDirectState(raw));
                solver.BodyViewFactory = _ => rawView; Reject<ObjectDisposedException>(() => PhysicsServer.BodyGetDirectState(raw));
                solver.BodyViewFactory = goodBody; rawView = (OwnedState)PhysicsServer.BodyGetDirectState(raw)!;
                Check(rawView.GetContactCount() == 0 && rawView.ContactReads == 0, "Recreated cached factory view starts empty without publication hooks");
                var retired = rawView; PhysicsServer.BodySetSpace(raw, default);
                Check(PhysicsServer.BodyGetDirectState(raw) is null, "Detached bodies have no cached direct state");
                Reject<ObjectDisposedException>(() => _ = retired.Transform);
                PhysicsServer.BodySetSpace(raw, world.Space); solver.BodyViewFactory = _ => retired;
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetDirectState(raw));
                solver.BodyViewFactory = goodBody; rawView = (OwnedState)PhysicsServer.BodyGetDirectState(raw)!;
                Reset(); tree.PhysicsFrame(1d / 120);
                Task.Run(() => Reject<InvalidOperationException>(() => _ = rawView.LinearVelocity)).GetAwaiter().GetResult();
                rawView.Dispose(); solver.BodyViewFactory = _ => throw new ApplicationException("Injected view factory failure");
                var before = PhysicsServer.SpaceGetTick(world.Space);
                Reject<AggregateException>(() => tree.PhysicsFrame(1d / 120));
                Check(PhysicsServer.SpaceGetTick(world.Space) == before, "Pre-solve factory failure publishes no new tick");
                solver.BodyViewFactory = goodBody; rawView = (OwnedState)PhysicsServer.BodyGetDirectState(raw)!;
                Reset(); tree.PhysicsFrame(1d / 120);
            }
            void Reset()
            {
                sceneView.Transform = new(0, Vector2.Zero); rawView.Transform = new(0, new(3, 0));
                sceneView.LinearVelocity = Vector2.Right; rawView.LinearVelocity = Vector2.Left;
            }
            void Observe(PhysicsDirectBodyState value, OwnedState expected)
            {
                Check(ReferenceEquals(value, expected) && ReferenceEquals(value.GetSpaceState(), queries), "Callbacks receive the factory-selected body and query views");
                Check(value.GetContactCount() == 1 && value.GetContactImpulse(0).Length() > 0,
                    "The cached contact carries the selected solver's actual impulse");
                deliveries++;
            }
        }
        finally { PhysicsServer.FreeRID(raw); }
    }

    private sealed class ViewRigid : RigidBody
    {
        internal Action<PhysicsDirectBodyState>? Observer;
        protected override void IntegrateForces(PhysicsDirectBodyState state) => Observer?.Invoke(state);
    }

    private static void StateAccess(OwnedState state)
    {
        Check(state.InverseMass == .5f && state.InverseInertia > 0 && state.CenterOfMass.IsFinite() && state.CenterOfMassLocal.IsFinite() &&
            state.TotalGravity.IsFinite() && float.IsFinite(state.TotalLinearDamp) && float.IsFinite(state.TotalAngularDamp) &&
            state.GetVelocityAtLocalPosition(Vector2.Right).IsFinite() && state.GetSpaceState() is CircleQueries && state.Step >= 0,
            "The complete scalar, vector, point-velocity and query-space family reaches factory state hooks");
        state.CollisionLayer = 3; state.CollisionMask = 5;
        Check(state.CollisionLayer == 3 && state.CollisionMask == 5, "Factory filter setters/readers preserve authored values");
        state.CollisionLayer = state.CollisionMask = 1;
        state.Sleeping = true; Check(state.Sleeping, "Factory sleep setter reaches numerical state"); state.Sleeping = false;
        state.LinearVelocity = Vector2.Zero; state.AngularVelocity = 0;
        state.ApplyCentralImpulse(new(2, 0));
        Check(state.LinearVelocity == Vector2.Right, "Factory impulse hook changes real mass-scaled velocity");
        state.ApplyImpulse(new(0, 2), Vector2.Right); state.ApplyTorqueImpulse(2);
        Check(state.AngularVelocity > 0 && state.LinearVelocity.Y == 1, "Positioned and angular impulses reach the selected inertia response");
        state.SetConstantForce(Vector2.Right); state.AddConstantCentralForce(Vector2.Left);
        state.SetConstantTorque(1); state.AddConstantTorque(2); state.AddConstantForce(Vector2.Right, Vector2.Down);
        Check(state.GetConstantForce() == Vector2.Right && float.IsFinite(state.GetConstantTorque()), "Factory persistent force/torque operations retain authored inputs");
        state.ApplyCentralForce(Vector2.Zero); state.ApplyForce(Vector2.Zero, Vector2.Right); state.ApplyTorque(0);
        state.SetConstantForce(Vector2.Zero); state.SetConstantTorque(0); state.IntegrateForces();
        state.AngularVelocity = 0;
    }

    private static void QueryChecks(CircleQueries query, RID scene, RID raw, CircleShape circle)
    {
        using var point = new PhysicsPointQueryParameters { Position = Vector2.Zero };
        using var ray = new PhysicsRayQueryParameters { From = new(-6, 0), To = new(6, 0) };
        using var shape = new PhysicsShapeQueryParameters { Shape = circle, Transform = new(0, new Vector2(.5f, 0)), Margin = 0 };
        var points = new PhysicsPointResult[4]; var shapes = new PhysicsShapeResult[4]; var pairs = new Vector2[8];
        Check(query.IntersectPoint(point, points) == 1 && points[0].ColliderRID == scene, "Factory point hooks return real scene membership");
        Check(query.IntersectRay(ray)?.ColliderRID == scene && query.IntersectShape(shape, shapes) == 1 &&
            query.CollideShape(shape, pairs) > 0 && query.GetRestInfo(shape)?.ColliderRID == scene,
            "All selected ray, shape, contact and rest hooks use actual circle geometry");
        shape.Transform = new(0, new Vector2(-8, 0)); shape.Motion = new(12, 0);
        var fractions = query.CastMotion(shape);
        Check(Math.Abs(fractions.SafeFraction - 1f / 3) < .0001f && fractions.UnsafeFraction == fractions.SafeFraction,
            "Factory cast hooks return the analytic circle entry fraction");
        query.Suppressed = scene; Check(query.IntersectPoint(point, points) == 0, "Selected view policy changes the observable point query"); query.Suppressed = default;
        query.CachedPoint = new(scene, 0); Check(query.IntersectPoint(point, points) == 1, "Validated retained hit projection works"); query.CachedPoint = null;
        query.BadCount = -1; Reject<InvalidOperationException>(() => query.IntersectPoint(point, points)); query.BadCount = 0;
        query.BadFractions = true; Reject<InvalidOperationException>(() => query.CastMotion(shape)); query.BadFractions = false;
        query.BadPoints = true; Reject<InvalidOperationException>(() => query.CollideShape(shape, pairs)); query.BadPoints = false;
        query.Duplicate = true; Reject<InvalidOperationException>(() => query.IntersectPoint(point, points)); query.Duplicate = false;
        query.Throw = true; Reject<InvalidOperationException>(() => query.IntersectPoint(point, points)); query.Throw = false;
        point.Exclude = [scene]; query.Injected = scene;
        Reject<InvalidOperationException>(() => query.IntersectPoint(point, points)); query.Injected = default;
        var nested = false;
        using var inner = new PhysicsPointQueryParameters { Position = Vector2.Zero, Exclude = [raw] };
        query.OnQuery = () =>
        {
            Reject<InvalidOperationException>(() => query.Dispose());
            if (nested) { Check(!query.IsBodyExcludedFromQuery(scene) && query.IsBodyExcludedFromQuery(raw), "Nested view query replaces exclusions"); return; }
            Check(query.IsBodyExcludedFromQuery(scene), "Outer exclusions reach the selected view");
            nested = true; query.IntersectPoint(inner, new PhysicsPointResult[4]); nested = false;
            Check(query.IsBodyExcludedFromQuery(scene) && !query.IsBodyExcludedFromQuery(raw), "Nested query restores outer exclusions");
        };
        Check(query.IntersectPoint(point, points) == 0, "Original exclusion still suppresses the final outer hit");
        query.OnQuery = null; point.Exclude = [];
        Check(!query.IsBodyExcludedFromQuery(scene) && query.IntersectPoint(point, points) == 1, "Failed and nested query scopes unwind for the next call");
    }
}
