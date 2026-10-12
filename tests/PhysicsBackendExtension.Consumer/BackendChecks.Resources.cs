using Electron2D;

public static partial class BackendChecks
{
    public static void RunResources(PhysicsServer.Backend? backend = null) => ResourceReleaseChecks(backend);

    private static void ResourceReleaseChecks(PhysicsServer.Backend? backend = null)
    {
        Solver? solver = null; Solver? sceneSolver = null;
        if (backend is null)
        {
            PhysicsServerManager.RegisterServer("ResourcesScene", () => sceneSolver = new());
            PhysicsServerManager.RegisterServer("ResourcesOwned", () =>
            {
                var created = new Solver(); solver = created;
                created.InitHook = () => Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(created.Space));
                created.FinishHook = () => Reject<ObjectDisposedException>(() => PhysicsServer.FreeRID(created.Space));
                return created;
            });
        }
        using var world = backend is { } selected ? new World(selected) : new World("ResourcesScene");
        using var root = new SubViewport { World = world };
        using var borrowed = new CircleShape { Radius = 3 };
        PhysicsBody[] bodies = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
        foreach (var body in bodies) root.AddChild(body);
        var sceneArea = new Area(); root.AddChild(sceneArea);
        Joint[] nodes = [new PinJoint(), new GrooveJoint(), new DampedSpringJoint()];
        foreach (var node in nodes) root.AddChild(node);
        using var tree = new SceneTree(root);
        var space = backend is { } chosen ? PhysicsServer.SpaceCreate(chosen) : PhysicsServer.SpaceCreate("ResourcesOwned");
        var first = PhysicsServer.BodyCreate(); var second = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        var shape = PhysicsServer.CircleShapeCreate();
        var joints = new[] { PhysicsServer.JointCreate(), PhysicsServer.JointCreate(), PhysicsServer.JointCreate() };
        var spaceView = PhysicsServer.SpaceGetDirectState(space);
        try
        {
            PhysicsServer.ShapeSetData(shape, borrowed);
            foreach (var id in new[] { first, second })
            {
                PhysicsServer.BodyAddShape(id, shape); PhysicsServer.BodySetGravityScale(id, 0);
                PhysicsServer.BodySetCanSleep(id, false); PhysicsServer.BodySetSpace(id, space);
            }
            PhysicsServer.BodySetTransform(second, new(0, new Vector2(40, 0)));
            PhysicsServer.AreaAddShape(area, shape); PhysicsServer.AreaSetSpace(area, space);
            for (var i = 0; i < bodies.Length; i++)
            {
                var id = bodies[i].GetRID();
                PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetGravityScale(id, 0);
                PhysicsServer.BodySetCanSleep(id, false); PhysicsServer.BodySetTransform(id, new(0, new Vector2(100 + 30 * i, 0)));
                PhysicsServer.BodyAddShape(id, shape);
            }
            PhysicsServer.AreaAddShape(sceneArea.GetRID(), shape);
            PhysicsServer.AreaSetTransform(sceneArea.GetRID(), new(0, new Vector2(-1000, 0)));
            for (var i = 0; i < nodes.Length; i++) MakeJoint(nodes[i].GetRID(), i, bodies[0].GetRID(), bodies[1].GetRID());
            foreach (var id in bodies.Select(body => body.GetRID()).Concat(new[] { sceneArea.GetRID(), world.Space, borrowed.GetRID() }).Concat(nodes.Select(node => node.GetRID())))
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(id));
            Reject<ArgumentException>(() => PhysicsServer.FreeRID(default));
            for (var i = 0; i < joints.Length; i++) MakeJoint(joints[i], i, first, second);
            ResourceReleaseGuards(solver, space, first, second, area, shape, joints);
            var firstView = PhysicsServer.BodyGetDirectState(first)!;
            var released = first;
            if (solver is not null)
            {
                solver.FailJointDetach = solver.FailBodyDetach = true; solver.MotionFailure = first;
                var errors = ReleaseErrors(() => PhysicsServer.FreeRID(first));
                solver.FailJointDetach = solver.FailBodyDetach = false; solver.MotionFailure = default;
                Check(errors.Length == 5 && errors.Count(error => error.Message == "Joint detach failure") == 3 &&
                    errors.Any(error => error.Message == "Motion capture failure") && errors.Any(error => error.Message == "Collider detach failure"),
                    "Dependent, motion and collider failures all survive completed retirement");
                Check(!solver.Bodies.ContainsKey(first), "Failed dependent cleanup still retires the numerical body");
            }
            else PhysicsServer.FreeRID(first);
            first = default;
            Reject<ArgumentException>(() => PhysicsServer.BodyGetSpace(released));
            Reject<ArgumentException>(() => PhysicsServer.FreeRID(released));
            Reject<ObjectDisposedException>(() => _ = firstView.LinearVelocity);
            foreach (var id in joints) Check(PhysicsServer.JointGetType(id) == PhysicsServer.JointType.Empty, "Body release clears every dependent role while retaining joint RID");

            foreach (var body in bodies) PhysicsServer.BodySetSpace(body.GetRID(), space);
            PhysicsServer.AreaSetSpace(sceneArea.GetRID(), space);
            var retained = bodies.Select(body => PhysicsServer.BodyGetDirectState(body.GetRID())!).ToArray();
            if (solver is not null) solver.ShapeFailure = second;
            var oldShape = shape;
            if (solver is not null) Check(ReleaseErrors(() => PhysicsServer.FreeRID(shape)).Single().Message == "Shape retirement failure", "Shape retirement preserves its error after all owners are processed");
            else PhysicsServer.FreeRID(shape);
            shape = default; if (solver is not null) solver.ShapeFailure = default;
            Reject<ArgumentException>(() => PhysicsServer.ShapeGetType(oldShape));
            Check(PhysicsServer.BodyGetShapeCount(second) == 0 && PhysicsServer.AreaGetShapeCount(area) == 0 && PhysicsServer.AreaGetShapeCount(sceneArea.GetRID()) == 0,
                "Freed shared shape leaves no raw or scene Area/body slots");
            foreach (var body in bodies) Check(PhysicsServer.BodyGetShapeCount(body.GetRID()) == 0, "All scene sibling shape slots release shared geometry");
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(solver is null || solver.Bodies.Values.All(body => body.Shapes == 0), "Failed numerical shape preparation retries from retained dirty authoring before the next step");
            for (var i = 0; i < joints.Length; i++) { MakeJoint(joints[i], i, second, bodies[^1].GetRID()); PhysicsServer.JointSetMaxForce(joints[i], 0); }
            for (var i = 0; i < 64; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            GC.Collect(); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; total = GC.GetTotalAllocatedBytes(true) - total;
            Check(owner == 0 && total == 0, $"Post-release active steps allocated {owner}/{total} B");
            var oldJoint = joints[2];
            if (solver is not null)
            {
                solver.FailJointDetach = true;
                Check(ReleaseErrors(() => PhysicsServer.FreeRID(oldJoint)).Single().Message == "Joint detach failure", "Joint free retains its error after retiring the RID");
                solver.FailJointDetach = false;
            }
            else PhysicsServer.FreeRID(oldJoint);
            joints[2] = PhysicsServer.JointCreate(); MakeJoint(joints[2], 2, second, bodies[^1].GetRID());
            Reject<ArgumentException>(() => PhysicsServer.JointGetType(oldJoint));
            var oldArea = area;
            if (solver is not null)
            {
                solver.FailBodyDetach = true;
                Check(ReleaseErrors(() => PhysicsServer.FreeRID(area)).Single().Message == "Collider detach failure", "Area free retains its error after retiring the RID");
                solver.FailBodyDetach = false;
            }
            else PhysicsServer.FreeRID(area);
            area = PhysicsServer.AreaCreate(); PhysicsServer.AreaSetSpace(area, space);
            Reject<ArgumentException>(() => PhysicsServer.AreaGetSpace(oldArea));
            ResourceReleaseSceneFailures(solver, space, bodies, sceneArea);
            if (solver is not null)
            {
                solver.FailJointDetach = solver.FailBodyDetach = solver.FailFinish = solver.FailDisposal = true;
                var errors = ReleaseErrors(() => PhysicsServer.FreeRID(space));
                solver.FailJointDetach = solver.FailBodyDetach = solver.FailFinish = solver.FailDisposal = false;
                Check(errors.Length == 13 && errors.Count(error => error.Message == "Collider detach failure") == 8 &&
                    errors.Count(error => error.Message == "Joint detach failure") == 3 && errors.Any(error => error.Message == "Finish failure") &&
                    errors.Any(error => error.Message == "Disposal failure") && solver.Finished && solver.IsDisposed && solver.Bodies.Count == 0 && solver.Joints.Count == 0,
                    "Space retirement attempts every attachment, Finish and disposal and retains every failure");
            }
            else PhysicsServer.FreeRID(space);
            var retiredSpace = space; space = default;
            Reject<ArgumentException>(() => PhysicsServer.SpaceGetTick(retiredSpace));
            foreach (var view in retained) Reject<ObjectDisposedException>(() => _ = view.Transform);
            using var point = new PhysicsPointQueryParameters();
            Reject<ArgumentException>(() => spaceView.IntersectPoint(point));
            Check(PhysicsServer.BodyGetSpace(second) == default && PhysicsServer.AreaGetSpace(area) == default, "Owned space retirement detaches retained raw resources");
            foreach (var body in bodies)
            {
                Check(PhysicsServer.BodyGetSpace(body.GetRID()) == default, "Owned space retirement detaches retained scene identities");
                PhysicsServer.BodySetSpace(body.GetRID(), world.Space); PhysicsServer.BodyAddShape(body.GetRID(), borrowed.GetRID());
            }
            PhysicsServer.AreaSetSpace(sceneArea.GetRID(), world.Space); PhysicsServer.AreaAddShape(sceneArea.GetRID(), borrowed.GetRID());
            tree.PhysicsFrame(1d / 60);
            Check(sceneSolver is null || sceneSolver.Bodies.Count == 6, "Scene siblings recover in their original world with fresh attachments");
            ResourceReleaseEvents(backend);
            Console.WriteLine($"Resource release on {backend?.ToString() ?? "registered"}: all five RID kinds, scene siblings, errors/guards/expired views/reentry and 64 active steps {owner}/{total} B passed.");
        }
        finally
        {
            if (solver is not null)
            {
                solver.BodyHook = solver.ShapeHook = solver.JointHook = solver.BodyReleaseHook = null;
                solver.FailJointDetach = solver.FailBodyDetach = solver.FailFinish = solver.FailDisposal = false;
                solver.MotionFailure = solver.ShapeFailure = default;
            }
            foreach (var id in joints) PhysicsServer.FreeRID(id);
            if (first.IsValid()) PhysicsServer.FreeRID(first);
            PhysicsServer.FreeRID(second); PhysicsServer.FreeRID(area);
            if (shape.IsValid()) PhysicsServer.FreeRID(shape);
            if (space.IsValid()) PhysicsServer.FreeRID(space);
        }
    }

    private static Exception[] ReleaseErrors(Action action)
    {
        try { action(); }
        catch (AggregateException error) { return error.Flatten().InnerExceptions.ToArray(); }
        catch (Exception error) { return [error]; }
        throw new InvalidOperationException("Expected a cleanup failure");
    }
}
