using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2Worlds;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    private readonly Storage<Float4> _contactMaterialStorage;
    private readonly b2TaskCallback _publishContactTask;
    private int _preSolveContactCount;
    internal long UpdatedContactCount { get; private set; }

    internal void UpdateContacts(B2StepContext context, int count)
    {
        var world = context.world;
        var frictionMode = world.frictionCallback == b2DefaultFrictionCallback ? 1u : world.frictionCallback == PhysicsSpace.CombineFriction ? 2u : 0u;
        var bounceMode = world.restitutionCallback == b2DefaultRestitutionCallback ? 1u : world.restitutionCallback == PhysicsSpace.CombineBounce ? 2u : 0u;
        var options = 1u | (world.enableSpeculative ? 0u : 2u) | (world.preSolveFcn is null ? 0u : 4u) | (frictionMode << 3) | (bounceMode << 5);
        GenerateManifolds(context, count, options);
        if (frictionMode == 0 || bounceMode == 0)
        {
            for (var i = 0; i < count; i++) PublishContact(context, i, 0, frictionMode, bounceMode);
        }
        else if (count != 0)
        {
            // Only mirror publication runs on CPU workers. Custom callbacks stay
            // on the owner after those workers have joined.
            var task = world.enqueueTaskFcn(_publishContactTask, count, 64, context, world.userTaskContext);
            world.taskCount++;
            if (task is not null) world.finishTaskFcn(task, world.userTaskContext);
            if (_preSolveContactCount != 0)
                for (var i = 0; i < count; i++)
                    if (NeedsOwnerContact(context, i)) PublishContact(context, i, 0, frictionMode, bounceMode);
        }
        UpdatedContactCount += count;
        context.generatedContactsUpdated = true;
    }

    private static bool NeedsOwnerContact(B2StepContext context, int index) => context.world.preSolveFcn is not null &&
        (context.contacts[index].simFlags & (uint)B2ContactSimFlags.b2_simEnablePreSolveEvents) != 0;

    private void PublishContactTask(int start, int end, uint threadIndex, object state)
    {
        var context = (B2StepContext)state;
        for (var i = start; i < end; i++)
            if (!NeedsOwnerContact(context, i)) PublishContact(context, i, (int)threadIndex, 1, 1);
    }

    private void PublishContact(B2StepContext context, int i, int threadIndex, uint frictionMode, uint bounceMode)
    {
        var world = context.world;
        var contact = context.contacts[i];
        var previous = contact.simFlags;
        var flags = ((uint)_manifoldStorage.Data[i].Normal.W >> 2) << 16;
        if ((flags & (uint)B2ContactSimFlags.b2_simDisjoint) == 0)
        {
            var a = world.shapes.data[contact.shapeIdA]; var b = world.shapes.data[contact.shapeIdB];
            var bodyA = world.bodies.data[a.bodyId]; var bodyB = world.bodies.data[b.bodyId];
            var simA = b2GetBodySim(world, bodyA); var simB = b2GetBodySim(world, bodyB);
            // Maintain the shared CPU query/graph mirror. Geometry, material
            // arithmetic and ordinary state classification are already done.
            contact.bodySimIndexA = bodyA.setIndex == (int)B2SolverSetType.b2_awakeSet ? bodyA.localIndex : -1;
            contact.bodySimIndexB = bodyB.setIndex == (int)B2SolverSetType.b2_awakeSet ? bodyB.localIndex : -1;
            contact.invMassA = simA.invMass; contact.invMassB = simB.invMass;
            contact.invIA = simA.invInertia; contact.invIB = simB.invInertia;
            contact.surfaceLinearA = simA.surfaceLinearVelocity; contact.surfaceAngularA = simA.surfaceAngularVelocity;
            contact.surfaceLinearB = simB.surfaceLinearVelocity; contact.surfaceAngularB = simB.surfaceAngularVelocity;
            contact.manifold = _manifolds[i];
            var material = _contactMaterialStorage.Data[i];
            contact.friction = frictionMode != 0 ? material.X : world.frictionCallback(a.material.friction, a.material.userMaterialId, b.material.friction, b.material.userMaterialId);
            contact.restitution = bounceMode != 0 ? material.Y : world.restitutionCallback(a.material.restitution, a.material.userMaterialId, b.material.restitution, b.material.userMaterialId);
            contact.rollingResistance = material.Z; contact.tangentSpeed = material.W;
            if (contact.manifold.pointCount > 0 && !b2InvokePreSolve(world, contact, a, b))
            {
                contact.manifold.pointCount = 0; contact.manifold.rollingImpulse = 0;
                flags &= ~(uint)(B2ContactSimFlags.b2_simTouchingFlag | B2ContactSimFlags.b2_simEnableHitEvent | B2ContactSimFlags.b2_simStartedTouching);
                if ((previous & (uint)B2ContactSimFlags.b2_simTouchingFlag) != 0) flags |= (uint)B2ContactSimFlags.b2_simStoppedTouching;
            }
            // A pre-solve hook observes the original deepest point before the
            // optional test-only pruning, matching the shared callback contract.
            if (!world.enableSpeculative && world.preSolveFcn is not null && (previous & (uint)B2ContactSimFlags.b2_simEnablePreSolveEvents) != 0)
                b2PruneSpeculativePoints(ref contact.manifold);
        }
        contact.simFlags = flags;
        if ((flags & (uint)(B2ContactSimFlags.b2_simDisjoint | B2ContactSimFlags.b2_simStartedTouching | B2ContactSimFlags.b2_simStoppedTouching)) != 0)
            B2BitSets.b2SetBit(ref world.taskContexts.data[threadIndex].contactStateBitSet, contact.contactId);
    }

}
