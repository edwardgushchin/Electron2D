using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private readonly List<KinematicStepForce> _kinematicStepForces = [];
    private readonly Dictionary<int, (double X, double Y, double Angular)> _jointImpulseVelocities = [];
    private readonly record struct KinematicStepForce(B2BodyId ID, B2Vec2 Force, float Torque);

    private void StepKinematicPaths(double delta, bool hasKinematicBodies)
    {
        _aggregateContactImpulses = false; _frameContactIndices.Clear(); _frameContacts.Clear();
        if (!hasKinematicBodies) { StepBackend((float)delta); return; }
        var minimumExtent = B2_HUGE;
        foreach (var body in _bodies)
            if (body.BackendShapes.Count > 0 && b2Body_GetType(body.BackendID) == B2BodyType.b2_dynamicBody)
                minimumExtent = MathF.Min(minimumExtent, PhysicsColliderBackend.Simulation(body.BackendID).minExtent);
        foreach (var body in _serverColliders)
            if (!body.IsArea && body.BackendShapes.Count > 0 && b2Body_GetType(body.BackendID) == B2BodyType.b2_dynamicBody)
                minimumExtent = MathF.Min(minimumExtent, PhysicsColliderBackend.Simulation(body.BackendID).minExtent);
        if (minimumExtent == B2_HUGE) { StepBackend((float)delta); return; }
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
        if (steps == 1) { StepBackend((float)delta); return; }
        BeginFrameContacts();
        _kinematicStepForces.Clear();
        foreach (var body in _bodies) Capture(body.BackendID);
        foreach (var body in _serverColliders) if (!body.IsArea) Capture(body.BackendID);
        var subDelta = (float)(delta / steps);
        for (var step = 0; step < steps; step++)
        {
            if (step != 0)
                foreach (var body in _kinematicStepForces)
                {
                    var sim = PhysicsColliderBackend.Simulation(body.ID);
                    sim.force = body.Force; sim.torque = body.Torque;
                }
            StepBackend(subDelta);
            CaptureIntervalImpulses();
        }
    }

    private void StepBackend(float delta)
    {
        if (delta / 4 > 0 && HasContinuousBodies()) StepContinuous(delta); else StepDiscreteBackend(delta);
    }

    private void StepDiscreteBackend(float delta, bool applyJointForces = true, int substeps = 4)
    {
        if (applyJointForces)
        {
            foreach (var joint in _jointRuntimes) joint.PrepareSolverStep(delta);
            _jointImpulseVelocities.Clear();
            foreach (var joint in _jointRuntimes) joint.ValidateSolverStep(this);
            foreach (var joint in _jointRuntimes) joint.ApplySolverStep();
        }
        _contactStep++;
        try { b2World_Step(_worldID, delta, substeps); PruneOneWayPairs(); }
        catch (Exception failure) when (_gpuWorld is not null)
        {
            // A partially committed GPU interval cannot be replayed through the compatibility solver.
            _gpuFailure = failure;
            _tasks!.Drain();
            var world = b2GetWorldFromId(_worldID);
            world.locked = false;
            foreach (var arena in world.arena.AsSpan()) arena.Abort();
            world.reusableStepContext.Reset();
            throw;
        }
    }

    internal void ValidateJointImpulse(B2BodyId id, B2Vec2 impulse, B2Vec2 point)
    {
        if (b2Body_GetType(id) != B2BodyType.b2_dynamicBody) return;
        if (!_jointImpulseVelocities.TryGetValue(id.index1, out var velocity))
        {
            var linear = b2Body_GetLinearVelocity(id);
            velocity = (linear.X, linear.Y, b2Body_GetAngularVelocity(id));
        }
        var body = PhysicsColliderBackend.Simulation(id);
        velocity.X += (double)body.invMass * impulse.X;
        velocity.Y += (double)body.invMass * impulse.Y;
        var moment = ((double)point.X - body.center.X) * impulse.Y - ((double)point.Y - body.center.Y) * impulse.X;
        velocity.Angular += body.invInertia * moment;
        if (!float.IsFinite((float)velocity.X) || !float.IsFinite((float)velocity.Y) || !float.IsFinite((float)velocity.Angular))
            throw new InvalidOperationException("Combined joint impulses exceed the finite physics velocity range.");
        _jointImpulseVelocities[id.index1] = velocity;
    }

    private static void MeasureTravel(B2BodyId id, int shapeCount, double delta, ref float minimumExtent, ref double travel)
    {
        if (shapeCount == 0 || b2Body_GetType(id) != B2BodyType.b2_kinematicBody) return;
        var sim = PhysicsColliderBackend.Simulation(id);
        minimumExtent = MathF.Min(minimumExtent, sim.minExtent);
        var world = b2GetWorld(id.world0);
        var state = b2GetBodyState(world, b2GetBodyFullId(world, id));
        var velocity = state?.linearVelocity ?? default;
        travel = Math.Max(travel, delta * (Math.Sqrt((double)velocity.X * velocity.X + (double)velocity.Y * velocity.Y) +
            Math.Abs(state?.angularVelocity ?? 0) * sim.maxExtent));
    }

    private void Capture(B2BodyId id)
    {
        if (b2Body_GetType(id) != B2BodyType.b2_dynamicBody) return;
        var sim = PhysicsColliderBackend.Simulation(id);
        _kinematicStepForces.Add(new(id, sim.force, sim.torque));
    }
}
