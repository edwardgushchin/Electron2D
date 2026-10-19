using Electron2D;

public static partial class BackendChecks
{
    private static void DebugContactRaw(Solver solver, World world, SceneTree tree, CircleShape circle)
    {
        var fixedBody = PhysicsServer.BodyCreate(); var moving = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        try
        {
            PhysicsServer.BodySetMode(fixedBody, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetCanSleep(moving, false);
            PhysicsServer.BodySetGravityScale(moving, 0);
            PhysicsServer.BodyAddShape(fixedBody, circle.GetRID()); PhysicsServer.BodyAddShape(moving, circle.GetRID());
            PhysicsServer.BodySetTransform(fixedBody, new(0, new Vector2(40, 0)));
            PhysicsServer.BodySetTransform(moving, new(0, new Vector2(40, 3)));
            PhysicsServer.BodySetSpace(fixedBody, world.Space); PhysicsServer.BodySetSpace(moving, world.Space);
            tree.PhysicsFrame(1d / 120);
            Check(solver.DebugLastCount == 2 && PhysicsServer.BodyGetMaxContactsReported(moving) == 0,
                "Raw bodies publish both boundary points independently of gameplay reporting");
            foreach (var point in solver.DebugPoints.AsSpan(0, solver.DebugLastCount))
                Check(Math.Abs(point.X - 40) < .00001f && point.Y >= 1 - .00001f && point.Y <= 2 + .00001f,
                    $"Raw diagnostic coordinates retain the actual circle surfaces within .00001 scene units: point={point}, fixed={PhysicsServer.BodyGetTransform(fixedBody)}, moving={PhysicsServer.BodyGetTransform(moving)}");
            PhysicsServer.BodySetSleeping(moving, true); tree.PhysicsFrame(1d / 120);
            Check(solver.DebugLastCount == 0, "Sleeping raw pairs do not publish active diagnostic samples");
            PhysicsServer.BodySetSleeping(moving, false); Reset(); tree.PhysicsFrame(1d / 120);
            Check(solver.DebugLastCount == 2, "Waking raw bodies restores actual samples");
            PhysicsServer.BodySetCollisionMask(fixedBody, 0); PhysicsServer.BodySetCollisionMask(moving, 0);
            Reset(); tree.PhysicsFrame(1d / 120);
            Check(solver.DebugLastCount == 0, "Bilateral collision filtering excludes raw diagnostic samples");
            PhysicsServer.BodySetCollisionMask(fixedBody, uint.MaxValue); PhysicsServer.BodySetCollisionMask(moving, uint.MaxValue);
            PhysicsServer.BodySetTransform(fixedBody, new(0, new Vector2(100, 0)));
            PhysicsServer.AreaAddShape(area, circle.GetRID()); PhysicsServer.AreaSetTransform(area, new(0, new Vector2(40, 3)));
            PhysicsServer.AreaSetSpace(area, world.Space); Reset(); tree.PhysicsFrame(1d / 120);
            Check(solver.DebugLastCount == 0, "A real raw sensor overlap supplies no diagnostic boundary sample");
            PhysicsServer.BodySetTransform(fixedBody, new(0, new Vector2(40, 0))); Reset(); tree.PhysicsFrame(1d / 120);
            Check(solver.DebugLastCount == 2, "A sensor does not duplicate the real raw body pair");
            PhysicsServer.FreeRID(moving); moving = default; tree.PhysicsFrame(1d / 120);
            Check(solver.DebugLastCount == 0, "Retiring the raw body replaces the sample with empty output");
        }
        finally
        {
            PhysicsServer.FreeRID(area);
            if (moving.IsValid()) PhysicsServer.FreeRID(moving);
            PhysicsServer.FreeRID(fixedBody);
        }
        void Reset()
        {
            PhysicsServer.BodySetTransform(moving, new(0, new Vector2(40, 3)));
            PhysicsServer.BodySetLinearVelocity(moving, Vector2.Zero);
        }
    }
}
