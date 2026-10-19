using Electron2D;

public static partial class BackendChecks
{
    private static void ResourceLifetimeContracts(bool threaded)
    {
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("ResourceLifetimeContracts", () => solver = new());
        foreach (var phase in new[] { LifecyclePhase.Solve, LifecyclePhase.Sync, LifecyclePhase.Statistics })
            for (var worldFirst = 0; worldFirst < 2; worldFirst++) Terminal(phase, worldFirst != 0);
        ResourceCallbackRetirement();
        Console.WriteLine($"Resource terminal retirement/{threaded}: six failure-phase/order cases and ordinary callback retirement passed.");

        void Terminal(LifecyclePhase phase, bool worldFirst)
        {
            var space = PhysicsServer.SpaceCreate("ResourceLifetimeContracts"); var body = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
            var shape = PhysicsServer.CircleShapeCreate(); var joint = PhysicsServer.JointCreate(); var original = new[] { body, area, shape, joint, space };
            using var disk = new CircleShape { Radius = 3 }; var owner = Thread.CurrentThread;
            try
            {
                PhysicsServer.ShapeSetData(shape, disk); PhysicsServer.BodyAddShape(body, shape); PhysicsServer.AreaAddShape(area, shape);
                PhysicsServer.BodySetSpace(body, space); PhysicsServer.AreaSetSpace(area, space); PhysicsServer.JointMakePin(joint, Vector2.Zero, body);
                var view = PhysicsServer.BodyGetDirectState(body)!; var spaceView = PhysicsServer.SpaceGetDirectState(space);
                var failure = new ApplicationException("resource-terminal-interval");
                solver!.LifecycleHook = current =>
                {
                    if (current == LifecyclePhase.Solve) Check((Thread.CurrentThread != owner) == threaded, "Terminal cleanup exercises the actual selected numerical thread");
                    if (current == phase) ThrowResourceRetirement(failure);
                };
                PhysicsServer.SpaceSetActive(space, true);
                Check(ReleaseErrors(() => PhysicsServer.SpaceStep(space, .01)).Any(e => ReferenceEquals(e, failure)), "Terminal interval preserves the original numerical failure");
                solver.LifecycleHook = null; var steps = solver.Steps;
                using var point = new PhysicsPointQueryParameters(); Reject<InvalidOperationException>(() => spaceView.IntersectPoint(point));
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(space, .01));
                Task.Run(() => { foreach (var id in new[] { body, area, shape, joint, space }) Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(id)); }).GetAwaiter().GetResult();
                if (worldFirst) { PhysicsServer.FreeRID(space); space = default; }
                PhysicsServer.FreeRID(joint); joint = default; PhysicsServer.FreeRID(body); body = default; PhysicsServer.FreeRID(area); area = default;
                PhysicsServer.FreeRID(shape); shape = default;
                if (space.IsValid()) { PhysicsServer.FreeRID(space); space = default; }
                Check(solver.Steps == steps && solver.Finished && solver.IsDisposed && solver.Bodies.Count == 0 && solver.Joints.Count == 0, "All five resource kinds retire after terminal failure without replaying Solve");
                Reject<ObjectDisposedException>(() => _ = view.Transform); Reject<ArgumentException>(() => spaceView.IntersectPoint(point));
                foreach (var id in original) Reject<ArgumentException>(() => PhysicsServer.FreeRID(id));
            }
            finally
            {
                solver!.LifecycleHook = null;
                foreach (var id in new[] { joint, body, area, shape, space }) if (id.IsValid()) PhysicsServer.FreeRID(id);
            }
        }
    }
    private static void ResourceCallbackRetirement()
    {
        PhysicsServerManager.RegisterServer("ResourceCallbackRetirement", () => new Solver());
        var space = PhysicsServer.SpaceCreate("ResourceCallbackRetirement"); var trigger = PhysicsServer.BodyCreate(); var other = PhysicsServer.BodyCreate();
        var area = PhysicsServer.AreaCreate(); var shape = PhysicsServer.CircleShapeCreate(); var joint = PhysicsServer.JointCreate();
        var targets = new[] { other, area, joint, shape }; var retired = false; var callbacks = 0;
        try
        {
            PhysicsServer.BodySetCanSleep(trigger, false); PhysicsServer.BodySetSpace(trigger, space); PhysicsServer.BodySetSpace(other, space);
            PhysicsServer.AreaSetSpace(area, space); PhysicsServer.JointMakePin(joint, Vector2.Zero, trigger, other);
            PhysicsServer.BodySetStateSyncCallback(trigger, _ =>
            {
                callbacks++; Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(space));
                foreach (var id in targets) PhysicsServer.FreeRID(id);
                retired = true;
            });
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, .01);
            PhysicsServer.BodySetStateSyncCallback(trigger, null); PhysicsServer.SpaceStep(space, .01);
            Check(callbacks == 1 && retired && PhysicsServer.BodyGetSpace(trigger) == space, "Ordinary callbacks may retire collider, Area, joint and shape while retaining their world");
            foreach (var id in targets) Reject<ArgumentException>(() => PhysicsServer.FreeRID(id));
        }
        finally
        {
            PhysicsServer.BodySetStateSyncCallback(trigger, null);
            if (!retired) foreach (var id in targets) PhysicsServer.FreeRID(id);
            PhysicsServer.FreeRID(trigger); PhysicsServer.FreeRID(space);
        }
    }

}
