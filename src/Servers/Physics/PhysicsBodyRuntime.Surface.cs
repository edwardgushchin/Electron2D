using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime
{
    internal static void SetSurfaceVelocity(B2BodyId id, Vector2 linear, float angular)
    {
        var world = b2GetWorld(id.world0);
        if (world.locked) throw new InvalidOperationException("Surface velocity cannot change inside the solver.");
        var body = b2GetBodyFullId(world, id);
        var sim = b2GetBodySim(world, body);
        var native = Shape.ToBackend(linear);
        if (sim.surfaceLinearVelocity == native && sim.surfaceAngularVelocity == angular) return;
        sim.surfaceLinearVelocity = native; sim.surfaceAngularVelocity = angular;
        b2WakeBody(world, body);
        for (var key = body.headContactKey; key != -1;)
        {
            var contact = world.contacts.data[key >> 1]; var edge = key & 1;
            key = contact.edges[edge].nextKey;
            b2WakeBody(world, world.bodies.data[contact.edges[1 - edge].bodyId]);
        }
    }
}
