using Electron2D;

public static partial class BackendChecks
{
    public static void RunResourceTransactions(bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            Solver? solver = null;
            PhysicsServerManager.RegisterServer("ResourceTransactions", () => solver = new());
            for (var kind = 0; kind < 7; kind++) Run(kind);
            ResourceLifetimeContracts(threaded);
            Console.WriteLine($"Resource transactions/{threaded}: seven exact single-error retirement kinds passed.");

            void Run(int kind)
            {
                var space = PhysicsServer.SpaceCreate("ResourceTransactions");
                var first = PhysicsServer.BodyCreate(); var second = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
                var shape = PhysicsServer.CircleShapeCreate(); var joint = PhysicsServer.JointCreate();
                using var disk = new CircleShape { Radius = 3 };
                var owner = Thread.CurrentThread;
                try
                {
                    PhysicsServer.ShapeSetData(shape, disk);
                    foreach (var id in new[] { first, second })
                    {
                        PhysicsServer.BodyAddShape(id, shape); PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetSpace(id, space);
                    }
                    PhysicsServer.BodySetTransform(second, new(0, new Vector2(40, 0)));
                    PhysicsServer.AreaSetSpace(area, space); MakeJoint(joint, 0, first, second);
                    var view = PhysicsServer.BodyGetDirectState(first)!; var spaceView = PhysicsServer.SpaceGetDirectState(space);
                    solver!.StepHook = () =>
                    {
                        Check((Thread.CurrentThread != owner) == threaded, "Resource retirement follows an actual selected numerical interval");
                        foreach (var id in new[] { space, first, second, area, shape, joint }) Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(id));
                        foreach (var id in new[] { PhysicsServer.BodyCreate(), PhysicsServer.AreaCreate(), PhysicsServer.JointCreate(), PhysicsServer.CircleShapeCreate() }) PhysicsServer.FreeRID(id);
                    };
                    PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, .01);
                    var failure = new ApplicationException("single-resource-retirement");
                    if (kind == 0) solver.FinishHook = () => ThrowResourceRetirement(failure);
                    else if (kind == 1) solver.DisposalHook = () => ThrowResourceRetirement(failure);
                    else if (kind is 2 or 3 or 6) solver.BodyReleaseHook = id => { if (id == (kind == 3 ? area : first)) ThrowResourceRetirement(failure); };
                    else if (kind == 4) solver.JointReleaseHook = _ => ThrowResourceRetirement(failure);
                    else solver.ShapeHook = id => { if (id == first) ThrowResourceRetirement(failure); };
                    var target = kind switch { 2 => first, 3 => area, 4 => joint, 5 => shape, _ => space };
                    Exception? observed = null;
                    try { PhysicsServer.FreeRID(target); } catch (Exception error) { observed = error; }
                    if (kind == 2) first = default; else if (kind == 3) area = default; else if (kind == 4) joint = default; else if (kind == 5) shape = default; else space = default;
                    if (!ReferenceEquals(observed, failure)) throw new InvalidOperationException("Single resource retirement error was wrapped or replaced.", observed);
                    Check(observed!.StackTrace?.Contains(nameof(ThrowResourceRetirement)) == true, "A lone retirement error preserves its original throwing stack");
                    if (kind is 0 or 1 or 6)
                    {
                        Reject<ArgumentException>(() => PhysicsServer.SpaceGetTick(target));
                        using var point = new PhysicsPointQueryParameters(); Reject<ArgumentException>(() => spaceView.IntersectPoint(point));
                        Check(solver.IsDisposed && solver.Bodies.Count == 0 && solver.Joints.Count == 0 && PhysicsServer.BodyGetSpace(first) == default, "World retirement invalidates views and releases every selected attachment despite a lone error");
                    }
                    else if (kind == 2) { Reject<ArgumentException>(() => PhysicsServer.BodyGetSpace(target)); Reject<ObjectDisposedException>(() => _ = view.Transform); }
                    else if (kind == 3) Reject<ArgumentException>(() => PhysicsServer.AreaGetSpace(target));
                    else if (kind == 4) Reject<ArgumentException>(() => PhysicsServer.JointGetType(target));
                    else { Reject<ArgumentException>(() => PhysicsServer.ShapeGetType(target)); Check(PhysicsServer.BodyGetShapeCount(first) == 0 && PhysicsServer.BodyGetShapeCount(second) == 0, "Failed shape cleanup removes all common users"); }
                    Reject<ArgumentException>(() => PhysicsServer.FreeRID(target));
                }
                finally
                {
                    solver!.FinishHook = solver.DisposalHook = solver.StepHook = null; solver.BodyReleaseHook = solver.JointReleaseHook = solver.ShapeHook = null;
                    foreach (var id in new[] { joint, first, second, area, shape, space }) if (id.IsValid()) PhysicsServer.FreeRID(id);
                }
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void ThrowResourceRetirement(Exception error) => throw error;
}
