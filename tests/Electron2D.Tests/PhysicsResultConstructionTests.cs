using Electron2D;
using static Box2D.NET.B2Worlds;

internal static class PhysicsResultConstructionTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        Electron2D.Examples.PhysicsResultConstruction.ResultConstructionChecks.Run(backend);
        if (backend != PhysicsServer.Backend.CPU) return;
        using var shape = new CircleShape();
        using var motion = new PhysicsTestMotionResult();
        var space = PhysicsServer.SpaceCreate();
        var body = PhysicsServer.BodyCreate(); var collider = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodyAddShape(collider, shape.GetRID());
            PhysicsServer.BodySetSpace(body, space); PhysicsServer.BodySetSpace(collider, space);
            var world = b2GetWorldFromId(PhysicsServer.Service.GetSceneSpace(space).WorldID);
            world.locked = true;
            try
            {
                Reject(() => new PhysicsRayResult(collider, 0, default, default));
                Reject(() => new PhysicsPointResult(collider, 0));
                Reject(() => new PhysicsShapeResult(collider, 0));
                Reject(() => new PhysicsRestInfo(collider, 0, default, default, default));
                Reject(() => motion.SetCollision(body, 0, collider, 0, default, default, 0, default, default, default, 0, 1));
            }
            finally { world.locked = false; }
            Console.WriteLine("Every public collision-result construction path rejects a solver-owned CPU world.");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(collider); PhysicsServer.FreeRID(space); }
    }
    private static void Reject(Action action)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("A solver-owned world accepted result construction.");
    }
}
