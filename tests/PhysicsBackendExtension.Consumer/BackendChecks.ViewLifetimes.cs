using Electron2D;

public static partial class BackendChecks
{
    public static void RunViewLifetimes(bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("ViewLifetimes", () => solver = new());
        try
        {
            using var world = new World("ViewLifetimes");
            using var shape = new CircleShape { Radius = 2 };
            var body = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodyAddShape(body, shape.GetRID());
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
                PhysicsServer.BodySetGravityScale(body, 0);
                PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetSpace(body, world.Space);
                var selected = solver!;
                PhysicsDirectSpaceState? candidateSpace = null;
                PhysicsDirectBodyState? candidateBody = null;
                var attempts = 0;
                selected.SpaceViewFactory = id =>
                {
                    candidateSpace = new CircleQueries(id, [new(body, shape, false)], shape);
                    selected.StatusHook = () => Guard(candidateSpace);
                    return candidateSpace;
                };
                var queries = world.DirectSpaceState;
                selected.StatusHook = null;
                Check(attempts > 0 && !queries.IsDisposed && ReferenceEquals(queries, candidateSpace),
                    "A factory query view stays live through status validation");
                selected.BodyViewFactory = id =>
                {
                    candidateBody = new OwnedState(selected, id);
                    selected.StatusHook = () => Guard(candidateBody);
                    return candidateBody;
                };
                var state = PhysicsServer.BodyGetDirectState(body)!;
                selected.StatusHook = null;
                Check(!state.IsDisposed && ReferenceEquals(state, candidateBody), "A factory body view stays live through status validation");
                using var independentSpace = new CircleQueries(world.Space, [new(body, shape, false)], shape);
                using var independentBody = new OwnedState(selected, body);
                selected.StatusHook = GuardAll;
                using var point = new PhysicsPointQueryParameters { Position = Vector2.Zero };
                Check(queries.IntersectPoint(point).Length == 1 && independentSpace.IntersectPoint(point).Length == 1,
                    "Status validation preserves cached and caller-created query dispatch");
                Check(state.LinearVelocity == Vector2.Zero && independentBody.LinearVelocity == Vector2.Zero,
                    "Status validation preserves cached and caller-created body dispatch");
                selected.StatusHook = null;
                queries.Dispose(); state.Dispose();
                selected.SpaceViewFactory = null; selected.BodyViewFactory = null;
                queries = world.DirectSpaceState; state = PhysicsServer.BodyGetDirectState(body)!;
                selected.StatusHook = GuardAll;
                Check(queries.IntersectPoint(point).Length == 1 && state.LinearVelocity == Vector2.Zero,
                    "Default views supplied by a registered backend retain the same lifetime guard");
                selected.StatusHook = null;
                selected.StepHook = GuardAll;
                using var root = new SubViewport { World = world };
                var scene = new RigidBody { GravityScale = 0, CanSleep = false, Position = new(100, 0) };
                scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
                using var tree = new SceneTree(root);
                tree.PhysicsFrame(1d / 120);
                selected.StepHook = null;
                Check(PhysicsServer.SpaceGetTick(world.Space) == 1 && !queries.IsDisposed && !state.IsDisposed,
                    "An actual numerical step cannot retire its world's views");
                var marker = new ApplicationException("Uncaught view release");
                selected.StatusHook = () =>
                {
                    try { queries.Dispose(); }
                    catch (InvalidOperationException) { throw marker; }
                };
                try { _ = queries.IntersectPoint(point); throw new InvalidOperationException("Expected status failure"); }
                catch (ApplicationException error) { Check(ReferenceEquals(error, marker), "An uncaught guard error preserves the original status exception"); }
                selected.StatusHook = null;
                Check(!queries.IsDisposed && queries.IntersectPoint(point).Length == 1,
                    "Rejected status disposal leaves the view and query context usable");
                using var other = new World();
                var foreign = other.DirectSpaceState;
                selected.StatusHook = foreign.Dispose;
                _ = state.LinearVelocity;
                selected.StatusHook = null;
                Check(foreign.IsDisposed, "A numerical hook does not borrow an unrelated world's view");
                var callbacks = 0;
                PhysicsServer.BodySetStateSyncCallback(body, _ => { callbacks++; queries.Dispose(); });
                tree.PhysicsFrame(1d / 120);
                Check(callbacks == 1 && queries.IsDisposed, "An ordinary gameplay observer may dispose an unborrowed sibling query view");
                queries = world.DirectSpaceState;
                Check(!queries.IsDisposed && !ReferenceEquals(queries, candidateSpace), "Owner disposal permits cached query recreation");
                state.Dispose(); state = PhysicsServer.BodyGetDirectState(body)!;
                Check(!state.IsDisposed && !ReferenceEquals(state, candidateBody), "Owner disposal permits cached body recreation");
                PhysicsServer.FreeRID(body); body = default;
                var finalized = 0;
                selected.FinishHook = () =>
                {
                    state.Dispose(); queries.Dispose(); independentBody.Dispose(); independentSpace.Dispose(); finalized++;
                };
                tree.Dispose(); root.Dispose();
                world.Dispose();
                Check(finalized == 1, "Backend finalization can retire views after their world closes");
                state.Dispose(); queries.Dispose(); independentBody.Dispose(); independentSpace.Dispose();
                Check(state.IsDisposed && queries.IsDisposed && independentBody.IsDisposed && independentSpace.IsDisposed,
                    "Expired views can be retired after body and world release");
                Console.WriteLine($"View lifetime guards, separate={threaded}: {attempts} rejected selected-hook releases; factory, default, independent and expired views passed.");

                void Guard(ElectronObject view)
                {
                    try { view.Dispose(); }
                    catch (InvalidOperationException) { attempts++; Check(!view.IsDisposed, "Rejected view release preserves lifetime"); return; }
                    throw new InvalidOperationException("Selected status hook disposed a live direct-state view");
                }
                void GuardAll()
                {
                    Guard(queries); Guard(state); Guard(independentSpace); Guard(independentBody);
                }
            }
            finally { if (body.IsValid()) PhysicsServer.FreeRID(body); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
