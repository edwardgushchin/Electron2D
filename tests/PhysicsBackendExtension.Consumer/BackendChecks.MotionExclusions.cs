using Electron2D;

public static partial class BackendChecks
{
    public static void RunMotionExclusions(PhysicsServer.Backend? backend = null)
    {
        Solver? factory = null;
        if (backend is null) PhysicsServerManager.RegisterServer("MotionExclusions", () => factory = new());
        using var world = backend is { } selected ? new World(selected) : new World("MotionExclusions");
        Solver? solver = null;
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        var scene = new CharacterBody();
        var near = new StaticBody { Name = "Near", Position = new(20, 0) };
        var far = new StaticBody { Name = "Far", Position = new(35, 0) };
        foreach (var node in new PhysicsBody[] { scene, near, far })
        { node.AddChild(new CollisionShape { Shape = circle }); root.AddChild(node); }
        using var tree = new SceneTree(root);
        using var token = new Node();
        var raw = new[] { PhysicsServer.BodyCreate(), PhysicsServer.BodyCreate(), PhysicsServer.BodyCreate() };
        var inert = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(inert);
        var cases = new List<(RID Body, PhysicsTestMotionParameters Parameters, RID Hit, RID[] Bodies, ulong[] Objects)>();
        World? nestedWorld = null; Solver? nested = null; RID nestedBody = default, nestedObstacle = default;
        using var nestedParameters = new PhysicsTestMotionParameters { From = new(0, new Vector2(0, 120)), Motion = new(40, 0), Margin = 0 };
        using var nestedResult = new PhysicsTestMotionResult();
        using var nestedRay = new PhysicsRayQueryParameters { From = new(0, 120), To = new(0, 160) };
        using var result = new PhysicsTestMotionResult();
        RID[] currentBodies = []; ulong[] currentObjects = []; var hooks = 0;
        RID[] probeBodies = [near.GetRID(), far.GetRID(), raw[1], raw[2], inert, default];
        ulong[] probeObjects = [near.InstanceID, far.InstanceID, token.InstanceID, ulong.MaxValue, 0];
        var guardRecursion = true; RID recursionBody = default;
        try
        {
            for (var i = 0; i < raw.Length; i++)
            {
                PhysicsServer.BodyAddShape(raw[i], circle.GetRID());
                PhysicsServer.BodySetMode(raw[i], i == 0 ? PhysicsServer.BodyMode.Kinematic : PhysicsServer.BodyMode.Static);
                PhysicsServer.BodySetTransform(raw[i], new(0, new Vector2(i == 0 ? 0 : i == 1 ? 20 : 35, 60)));
                if (i != 0) PhysicsServer.BodyAttachObject(raw[i], token);
                PhysicsServer.BodySetSpace(raw[i], world.Space);
            }
            AddCases(scene.GetRID(), Vector2.Zero, near.GetRID(), far.GetRID(), near.InstanceID, far.InstanceID);
            AddCases(raw[0], new(0, 60), raw[1], raw[2], token.InstanceID, token.InstanceID);
            if (backend is null) solver = factory ?? throw new InvalidOperationException("Registered world did not initialize its implementation.");
            if (solver is not null)
            {
                using var unbound = new Solver();
                Reject<InvalidOperationException>(() => unbound.IsBodyExcludedFromMotionTest(inert));
                Reject<InvalidOperationException>(() => unbound.IsObjectExcludedFromMotionTest(ulong.MaxValue));
                nestedWorld = new World("MotionExclusions");
                nestedBody = PhysicsServer.BodyCreate(); nestedObstacle = PhysicsServer.BodyCreate();
                foreach (var id in new[] { nestedBody, nestedObstacle })
                {
                    PhysicsServer.BodyAddShape(id, circle.GetRID()); PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Static);
                    PhysicsServer.BodySetTransform(id, new(0, new Vector2(id == nestedBody ? 0 : 20, 120)));
                    PhysicsServer.BodySetSpace(id, nestedWorld.Space);
                }
                nested = factory!;
                Check(!ReferenceEquals(nested, solver), "Lazy world initialization creates an independent implementation");
                nestedParameters.ExcludeBodies = [near.GetRID()]; nestedParameters.ExcludeObjects = [near.InstanceID];
                nested.MotionHook = _ =>
                {
                    Check(nested.IsBodyExcludedFromMotionTest(near.GetRID()) && nested.IsObjectExcludedFromMotionTest(near.InstanceID), "Nested world reads its own exclusions");
                    Probe();
                };
                solver.MotionHook = body =>
                {
                    hooks++; Probe();
                    if (guardRecursion)
                    {
                        guardRecursion = false;
                        CheckWrongThread();
                        recursionBody = body;
                        Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(recursionBody, cases[0].Parameters, result));
                    }
                    Check(world.DirectSpaceState.IntersectRay(nestedRay) is null, "Nested ordinary query preserves its independent query inputs");
                    Probe();
                    Check(!nested.IsBodyExcludedFromMotionTest(near.GetRID()) && !nested.IsObjectExcludedFromMotionTest(near.InstanceID), "Inactive other world has no motion scope");
                    Check(PhysicsServer.BodyTestMotion(nestedBody, nestedParameters, nestedResult) && nestedResult.GetColliderRID() == nestedObstacle, "Nested other-world motion completes independently");
                    Probe();
                    Check(!nested.IsBodyExcludedFromMotionTest(near.GetRID()) && !nested.IsObjectExcludedFromMotionTest(near.InstanceID), "Nested completion clears only its own scope");
                };
                solver.StepHook = () => CheckOutside();
                CheckOutside();
                var regular = solver.MotionHook;
                currentBodies = cases[1].Bodies; currentObjects = cases[1].Objects;
                solver.MotionHook = _ =>
                {
                    cases[1].Parameters.ExcludeBodies = [];
                    cases[1].Parameters.ExcludeObjects = [ulong.MaxValue];
                    Probe();
                };
                Check(PhysicsServer.BodyTestMotion(cases[1].Body, cases[1].Parameters, result) && result.GetColliderRID() == far.GetRID(), "Input edits apply to the next invocation, preserving the active borrowed lists");
                CheckOutside();
                cases[1].Parameters.ExcludeBodies = currentBodies; cases[1].Parameters.ExcludeObjects = currentObjects;
                solver.MotionHook = regular;
            }
            for (var i = 0; i < 128; i++) Tick();
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Tick();
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            if (ownerBytes != 0 || allBytes != 0) Console.WriteLine($"Motion allocation diagnostic ownerBytes={ownerBytes} allBytes={allBytes}.");
            Check(ownerBytes == 0 && allBytes == 0, "Warm motion exclusions, nested tests and full scene frames allocate no managed memory");
            if (solver is not null)
            {
                CheckWrongThread();
                var regular = solver.MotionHook;
                solver.MotionHook = _ => { Probe(); throw new InvalidOperationException("Motion callback failure"); };
                currentBodies = cases[1].Bodies; currentObjects = cases[1].Objects;
                var previous = result.GetColliderRID();
                Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(cases[1].Body, cases[1].Parameters, result));
                Check(result.GetColliderRID() == previous, "A failed motion hook preserves caller output"); CheckOutside();
                solver.MotionHook = regular; Tick();
                solver.MotionHook = _ => solver.Bodies[near.GetRID()].ObjectID = 0;
                currentBodies = cases[2].Bodies; currentObjects = cases[2].Objects;
                previous = result.GetColliderRID();
                Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(cases[2].Body, cases[2].Parameters, result));
                Check(result.GetColliderRID() == previous, "An excluded forged motion result preserves caller output"); CheckOutside();
                solver.Bodies[near.GetRID()].ObjectID = near.InstanceID; solver.MotionHook = regular; Tick();
                foreach (var query in new[] { cases[0], cases[8] })
                {
                    currentBodies = query.Bodies; currentObjects = query.Objects;
                    Check(PhysicsServer.BodyTestMotion(query.Body, query.Parameters, result), "A valid motion result precedes each receiver-identity rejection");
                    var before = MotionSnapshot(result);
                    solver.MotionResultHook = (requested, scratch) =>
                    {
                        var surrogate = requested == scene.GetRID() ? raw[0] : scene.GetRID();
                        scratch.SetCollision(surrogate, scratch.GetCollisionLocalShape(), scratch.GetColliderRID(), scratch.GetColliderShape(),
                            scratch.GetCollisionPoint() + new Vector2(1, 2), -scratch.GetCollisionNormal(), scratch.GetCollisionDepth(),
                            scratch.GetColliderVelocity(), scratch.GetTravel() / 2, scratch.GetRemainder(),
                            scratch.GetCollisionSafeFraction(), scratch.GetCollisionUnsafeFraction());
                    };
                    Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(query.Body, query.Parameters, result));
                    Check(MotionSnapshot(result) == before, "Wrong moving-body identity preserves the entire prior scene/raw caller result"); CheckOutside();
                    Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(query.Body, query.Parameters)); CheckOutside();
                    solver.MotionResultHook = null;
                    Check(PhysicsServer.BodyTestMotion(query.Body, query.Parameters, result) && MotionSnapshot(result) == before,
                        "Receiver-identity rejection restores the query scope without poisoning the next result");
                }
            }
            Check(solver is null || hooks >= cases.Count * 64, "Registered checks execute real numerical motion hooks");
            Console.WriteLine($"Motion exclusions backend={world.PhysicsBackend} queries={cases.Count * 64} frames=64 ownerBytes={ownerBytes} allBytes={allBytes} hooks={hooks}.");
        }
        finally
        {
            if (solver is not null) { solver.MotionHook = null; solver.MotionResultHook = null; solver.StepHook = null; }
            foreach (var query in cases) query.Parameters.Dispose();
            foreach (var id in raw) PhysicsServer.FreeRID(id);
            if (nestedBody.IsValid()) PhysicsServer.FreeRID(nestedBody);
            if (nestedObstacle.IsValid()) PhysicsServer.FreeRID(nestedObstacle);
            nestedWorld?.Dispose();
        }
        if (solver is not null)
        {
            tree.Dispose(); world.Dispose();
            Reject<ObjectDisposedException>(() => solver.IsBodyExcludedFromMotionTest(inert));
            Reject<ObjectDisposedException>(() => solver.IsObjectExcludedFromMotionTest(ulong.MaxValue));
            var failedSpace = PhysicsServer.SpaceCreate("MotionExclusions");
            PhysicsServer.SpaceSetActive(failedSpace, true);
            var failed = factory!; failed.FailSolve = true;
            try
            {
                Reject<AggregateException>(() => PhysicsServer.SpaceStep(failedSpace, 1d / 120));
                Reject<InvalidOperationException>(() => failed.IsBodyExcludedFromMotionTest(inert));
                Reject<InvalidOperationException>(() => failed.IsObjectExcludedFromMotionTest(ulong.MaxValue));
            }
            finally { PhysicsServer.FreeRID(failedSpace); }
        }

        static (RID, ulong, ElectronObject?, int, int, Vector2, Vector2, float, Vector2, Vector2, Vector2, float, float) MotionSnapshot(PhysicsTestMotionResult value) =>
            (value.GetColliderRID(), value.GetColliderID(), value.GetCollider(), value.GetCollisionLocalShape(), value.GetColliderShape(),
                value.GetCollisionPoint(), value.GetCollisionNormal(), value.GetCollisionDepth(), value.GetColliderVelocity(), value.GetTravel(), value.GetRemainder(),
                value.GetCollisionSafeFraction(), value.GetCollisionUnsafeFraction());
        void AddCases(RID body, Vector2 origin, RID first, RID second, ulong firstObject, ulong secondObject)
        {
            Add(first, [], []);
            Add(second, [first], []);
            Add(firstObject == secondObject ? default : second, [], [firstObject]);
            Add(default, [first], [secondObject]);
            Add(default, [first, second], []);
            Add(default, [], [firstObject, secondObject]);
            Add(first, [inert], [ulong.MaxValue]);
            Add(first, [default], [0]);
            void Add(RID hit, RID[] bodies, ulong[] objects) => cases.Add((body,
                new PhysicsTestMotionParameters { From = new(0, origin), Motion = new(40, 0), Margin = 0, ExcludeBodies = bodies, ExcludeObjects = objects }, hit, bodies, objects));
        }
        void Probe()
        {
            foreach (var id in probeBodies)
                Check(solver!.IsBodyExcludedFromMotionTest(id) == currentBodies.AsSpan().Contains(id), "Body helper reports exact current membership");
            foreach (var id in probeObjects)
                Check(solver!.IsObjectExcludedFromMotionTest(id) == currentObjects.AsSpan().Contains(id), "Object helper reports exact current membership");
        }
        void CheckWrongThread()
        {
            var foreignThread = new Thread(() =>
            {
                Reject<InvalidOperationException>(() => solver!.IsBodyExcludedFromMotionTest(inert));
                Reject<InvalidOperationException>(() => solver!.IsObjectExcludedFromMotionTest(ulong.MaxValue));
            });
            foreignThread.Start(); foreignThread.Join();
        }
        void CheckOutside()
        {
            Check(!solver!.IsBodyExcludedFromMotionTest(inert) && !solver.IsObjectExcludedFromMotionTest(ulong.MaxValue), "No motion scope survives a return, failure or ordinary physics hook");
            foreach (var id in currentBodies) Check(!solver.IsBodyExcludedFromMotionTest(id), "All body exclusions clear outside motion");
            foreach (var id in currentObjects) Check(!solver.IsObjectExcludedFromMotionTest(id), "All object exclusions clear outside motion");
        }
        void Tick()
        {
            foreach (var query in cases)
            {
                currentBodies = query.Bodies; currentObjects = query.Objects;
                var hit = PhysicsServer.BodyTestMotion(query.Body, query.Parameters, result);
                Check(hit == query.Hit.IsValid() && result.GetColliderRID() == query.Hit, "Scene/raw motion returns the nearest remaining collider");
                if (hit)
                {
                    Check(result.GetColliderShape() == 0 && result.GetTravel().X > 0 && result.GetTravel().X < 40, "Retained collision includes its shape and actual swept travel");
                    var expectedObject = query.Hit == raw[1] || query.Hit == raw[2] ? token.InstanceID : query.Hit == near.GetRID() ? near.InstanceID : far.InstanceID;
                    Check(result.GetColliderID() == expectedObject, "Motion results retain actual scene/raw object association");
                }
                else Check(result.GetTravel().IsEqualApprox(new Vector2(40, 0)) && result.GetRemainder() == Vector2.Zero, "Excluding all obstacles completes the original displacement");
                if (solver is not null) CheckOutside();
            }
            tree.PhysicsFrame(1.0 / 120);
            // Float unit conversion permits a few ULPs at the authored 60-unit coordinate.
            Check(scene.Position.Length() <= 1e-5f && (PhysicsServer.BodyGetTransform(raw[0]).Origin - new Vector2(0, 60)).Length() <= 1e-5f,
                "Motion tests preserve actual physical poses within float conversion tolerance");
        }
    }
}
