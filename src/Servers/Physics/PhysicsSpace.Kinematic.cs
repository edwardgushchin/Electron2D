using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private readonly List<KinematicStepForce> _kinematicStepForces = [];
    private readonly record struct KinematicStepForce(B2BodyId ID, B2Vec2 Force, float Torque);

    private void StepKinematicPaths(double delta)
    {
        var minimumExtent = B2_HUGE;
        foreach (var body in _bodies)
            if (body.BackendShapes.Count > 0 && b2Body_GetType(body.BackendID) == B2BodyType.b2_dynamicBody)
                minimumExtent = MathF.Min(minimumExtent, PhysicsBodyRuntime.Simulation(body.BackendID).minExtent);
        foreach (var body in _serverColliders)
            if (!body.IsArea && body.BackendShapes.Count > 0 && b2Body_GetType(body.BackendID) == B2BodyType.b2_dynamicBody)
                minimumExtent = MathF.Min(minimumExtent, PhysicsBodyRuntime.Simulation(body.BackendID).minExtent);
        if (minimumExtent == B2_HUGE) { b2World_Step(_worldID, (float)delta, 4); return; }
        var travel = 0d;
        foreach (var body in _bodies) MeasureTravel(body.BackendID, body.BackendShapes.Count, delta, ref minimumExtent, ref travel);
        foreach (var body in _serverColliders)
            if (!body.IsArea) MeasureTravel(body.BackendID, body.BackendShapes.Count, delta, ref minimumExtent, ref travel);
        // ponytail: Solver calls scale with travel and the smallest collider; use native kinematic TOI if profiling shows a cost.
        var interval = Math.Max(0.5 * minimumExtent, B2_LINEAR_SLOP);
        var countValue = Math.Ceiling(travel / interval);
        if (!double.IsFinite(countValue) || countValue > int.MaxValue)
            throw new InvalidOperationException("Kinematic displacement exceeds the finite integration range.");
        var steps = Math.Max(1, (int)countValue);
        if (steps == 1) { b2World_Step(_worldID, (float)delta, 4); return; }
        _kinematicStepForces.Clear();
        foreach (var body in _bodies) Capture(body.BackendID);
        foreach (var body in _serverColliders) if (!body.IsArea) Capture(body.BackendID);
        var subDelta = (float)(delta / steps);
        for (var step = 0; step < steps; step++)
        {
            if (step != 0)
                foreach (var body in _kinematicStepForces)
                {
                    var sim = PhysicsBodyRuntime.Simulation(body.ID);
                    sim.force = body.Force; sim.torque = body.Torque;
                }
            b2World_Step(_worldID, subDelta, 4);
        }
    }

    private static void MeasureTravel(B2BodyId id, int shapeCount, double delta, ref float minimumExtent, ref double travel)
    {
        if (shapeCount == 0 || b2Body_GetType(id) != B2BodyType.b2_kinematicBody) return;
        var sim = PhysicsBodyRuntime.Simulation(id);
        minimumExtent = MathF.Min(minimumExtent, sim.minExtent);
        var velocity = b2Body_GetLinearVelocity(id);
        travel = Math.Max(travel, delta * (Math.Sqrt((double)velocity.X * velocity.X + (double)velocity.Y * velocity.Y) +
            Math.Abs(b2Body_GetAngularVelocity(id)) * sim.maxExtent));
    }

    private void Capture(B2BodyId id)
    {
        if (b2Body_GetType(id) != B2BodyType.b2_dynamicBody) return;
        var sim = PhysicsBodyRuntime.Simulation(id);
        _kinematicStepForces.Add(new(id, sim.force, sim.torque));
    }
}
