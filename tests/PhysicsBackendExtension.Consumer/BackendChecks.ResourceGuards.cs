using Electron2D;

public static partial class BackendChecks
{
    private static void ResourceReleaseGuards(Solver? solver, RID space, RID first, RID second, RID area, RID shape, RID[] joints)
    {
        RID[] bound = [space, first, second, area, shape, .. joints];
        Task.Run(() => { foreach (var id in bound) Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(id)); }).GetAwaiter().GetResult();
        if (solver is null) return;
        Action<RID> guard = _ =>
        {
            var count = solver.Bodies.Count; var constraints = solver.Joints.Count;
            foreach (var id in bound) Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(id));
            Check(solver.Bodies.Count == count && solver.Joints.Count == constraints && PhysicsServer.BodyGetShapeCount(first) == 1 &&
                PhysicsServer.AreaGetShapeCount(area) == 1, "Rejected borrowed free preserves complete common/numerical ownership");
            for (var i = 0; i < joints.Length; i++) Check((int)PhysicsServer.JointGetType(joints[i]) == i, "Rejected joint free preserves its registry entry and role");
        };
        solver.BodyHook = solver.ShapeHook = solver.JointHook = solver.BodyReleaseHook = guard;
        try
        {
            PhysicsServer.BodySetLinearVelocity(first, Vector2.Zero);
            PhysicsServer.BodySetShapeDisabled(first, 0, true); PhysicsServer.BodySetShapeDisabled(first, 0, false);
            PhysicsServer.JointSetBias(joints[0], .2f);
            PhysicsServer.AreaSetSpace(area, default); PhysicsServer.AreaSetSpace(area, space);
        }
        finally { solver.BodyHook = solver.ShapeHook = solver.JointHook = solver.BodyReleaseHook = null; }
    }

    private static void ResourceReleaseSceneFailures(Solver? solver, RID space, PhysicsBody[] bodies, Area area)
    {
        if (solver is null) return;
        foreach (var body in bodies)
        {
            var id = body.GetRID(); var view = PhysicsServer.BodyGetDirectState(id)!;
            solver.MotionFailure = id; solver.FailBodyDetach = true;
            var errors = ReleaseErrors(() => PhysicsServer.BodySetSpace(id, default));
            solver.MotionFailure = default; solver.FailBodyDetach = false;
            Check(errors.Any(error => error.Message == "Collider detach failure"), "Scene body departure retains the detach error");
            if (PhysicsServer.BodyGetSpace(id) == default) PhysicsServer.BodySetSpace(id, space);
            Reject<ObjectDisposedException>(() => _ = view.LinearVelocity);
            Check(PhysicsServer.BodyGetSpace(id) == space && !body.IsDisposed && solver.Bodies.ContainsKey(id), "Every scene sibling recovers a fresh attachment after departure error");
        }
        var areaID = area.GetRID();
        solver.FailBodyDetach = true;
        var areaErrors = ReleaseErrors(() => PhysicsServer.AreaSetSpace(areaID, default));
        solver.FailBodyDetach = false;
        Check(areaErrors.Any(error => error.Message == "Collider detach failure"), "Scene Area departure retains its detach error");
        if (PhysicsServer.AreaGetSpace(areaID) == default) PhysicsServer.AreaSetSpace(areaID, space);
        Check(PhysicsServer.AreaGetSpace(areaID) == space && !area.IsDisposed && solver.Bodies.ContainsKey(areaID), "Scene Area recovers a fresh attachment after departure error");
    }
}
