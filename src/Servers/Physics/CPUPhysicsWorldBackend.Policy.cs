using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    internal override void SetSleepSettings(in PhysicsSleepSettings settings)
    {
        EnsureAccess();
        var world = b2GetWorldFromId(WorldID);
        world.sleepAngularThreshold = settings.AngularThreshold; world.timeToSleep = settings.TimeToSleep;
        for (var i = 0; i < world.bodies.count; i++)
        {
            var body = world.bodies.data[i];
            if (body.id < 0) continue;
            body.sleepThreshold = settings.LinearThreshold * PhysicsSpace.MetersPerUnit;
            body.sleepTime = 0;
            if (body.type != B2BodyType.b2_staticBody) b2WakeBody(world, body);
        }
    }
    internal override void SetContactSettings(in PhysicsContactSettings settings)
    {
        EnsureAccess();
        var world = b2GetWorldFromId(WorldID);
        world.contactRecycleRadius = settings.RecycleRadius * PhysicsSpace.MetersPerUnit;
        world.contactMaxSeparation = settings.MaxSeparation * PhysicsSpace.MetersPerUnit;
        world.contactBias = settings.Bias; world.contactAllowedPenetration = settings.AllowedPenetration * PhysicsSpace.MetersPerUnit;
        WakeDynamicBodies();
    }
    internal override void SetSolverIterations(int value)
    {
        EnsureAccess(); b2GetWorldFromId(WorldID).solverIterations = value; WakeDynamicBodies();
    }
    internal override void SetConstraintDefaultBias(float value)
    {
        EnsureAccess(); foreach (var joint in Space.SnapshotJoints) joint.ApplySolverPolicy();
    }
    private void WakeDynamicBodies()
    {
        var world = b2GetWorldFromId(WorldID);
        for (var i = 0; i < world.bodies.count; i++)
        {
            var body = world.bodies.data[i];
            if (body.id < 0 || body.type != B2BodyType.b2_dynamicBody) continue;
            body.sleepTime = 0; b2WakeBody(world, body);
        }
    }
    internal override void PrepareInterval()
    {
        EnsureAccess(); var world = b2GetWorldFromId(WorldID);
        world.workerCount = (_stageGPU is null || world.solveConstraints is not null) &&
            world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodySims.count >= 256 ? _tasks.WorkerCount : 1;
    }
}
