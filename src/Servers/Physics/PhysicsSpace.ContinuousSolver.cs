using System.Runtime.InteropServices;
using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private struct ContinuousForce
    {
        internal B2BodyId ID;
        internal B2Vec2 Force;
        internal float Torque, Gravity;
        internal bool Applied;
    }
    private struct ContinuousJointBudget
    {
        internal B2JointId ID;
        internal float Linear, Angular, Motor, Duration;
        internal double UsedLinear, UsedAngular, UsedMotor;
    }
    private readonly List<ContinuousForce> _continuousForces = [];
    private readonly List<ContinuousJointBudget> _continuousJointBudgets = [];
    private Action<B2StepContext>? _continuousFinalize;
    private float _continuousAdvanced;

    private void StepContinuous(float delta)
    {
        if (!_aggregateContactImpulses) BeginFrameContacts();
        var world = b2GetWorldFromId(_worldID); var finalizer = world.finalizeBodyStates; var warm = world.enableWarmStarting;
        _continuousFinalize ??= FinalizeContinuousMotion;
        world.finalizeBodyStates = _continuousFinalize;
        _continuousForces.Clear();
        foreach (var body in _bodies) SaveContinuousForce(body.Backend);
        foreach (var body in _serverColliders) if (!body.IsArea) SaveContinuousForce(body.Backend);
        var nominal = delta / 4;
        try
        {
            for (var step = 0; step < 4; step++)
            {
                foreach (ref var force in CollectionsMarshal.AsSpan(_continuousForces))
                {
                    var sim = PhysicsColliderBackend.Simulation(force.ID);
                    sim.force = force.Force; sim.torque = force.Torque; sim.gravityScale = force.Gravity; force.Applied = false;
                }
                CaptureContinuousJointBudgets(world, nominal);
                var remaining = nominal;
                for (var interval = 0; ; interval++)
                {
                    if (interval == 4096) throw new InvalidOperationException("Continuous collision did not converge within its interval budget.");
                    world.enableWarmStarting = interval == 0 && warm;
                    foreach (ref var force in CollectionsMarshal.AsSpan(_continuousForces))
                    {
                        var sim = PhysicsColliderBackend.Simulation(force.ID);
                        sim.gravityScale = force.Applied ? 0 : force.Gravity;
                        if (force.Applied) { sim.force = default; sim.torque = 0; }
                    }
                    SetContinuousJointBudgets(world, remaining);
                    _continuousAdvanced = remaining;
                    StepDiscreteBackend(remaining, applyJointForces: interval == 0, substeps: 1);
                    CaptureIntervalImpulses();
                    if (!(_continuousAdvanced > 0) || _continuousAdvanced > remaining || !float.IsFinite(_continuousAdvanced))
                        throw new InvalidOperationException("Continuous collision returned an invalid advancement.");
                    remaining -= _continuousAdvanced;
                    if (remaining <= 0) break;
                }
                RestoreContinuousJointBudgets(world);
            }
        }
        catch (Exception failure)
        {
            _continuousFailure = failure;
            _tasks!.Drain(); world.locked = false;
            foreach (var arena in world.arena.AsSpan()) arena.Abort();
            world.reusableStepContext.Reset();
            throw;
        }
        finally
        {
            world.finalizeBodyStates = finalizer; world.enableWarmStarting = warm;
            if (!HasBackendFailure)
            {
                RestoreContinuousJointBudgets(world);
                foreach (var force in _continuousForces) PhysicsColliderBackend.Simulation(force.ID).gravityScale = force.Gravity;
            }
        }
    }

    private void FinalizeContinuousMotion(B2StepContext context)
    {
        AccumulateContinuousJointBudgets(context.world, context.dt);
        foreach (ref var force in CollectionsMarshal.AsSpan(_continuousForces)) force.Applied |= b2Body_IsAwake(force.ID);
        var interval = FindContinuousInterval(context); var fraction = interval / context.dt;
        if (!(fraction > 0) || fraction > 1 || !float.IsFinite(fraction) || !float.IsFinite(1 / interval)) throw new InvalidOperationException("Continuous sweep returned an invalid time fraction.");
        _continuousAdvanced = interval;
        if (fraction == 1) return;
        var awake = context.world.solverSets.data[(int)B2SolverSetType.b2_awakeSet];
        for (var i = 0; i < awake.bodySims.count; i++)
        {
            var state = context.states[i]; state.deltaPosition *= fraction;
            state.deltaRotation = b2NLerp(b2Rot_identity, state.deltaRotation, fraction);
        }
        // Velocity/force budgets use the nominal interval; pose publication and sleep age use elapsed time.
        context.dt = interval; context.inv_dt = 1 / interval;
    }

    private void SaveContinuousForce(PhysicsColliderBackend backend)
    {
        if (!backend.IsDynamic) return;
        var sim = PhysicsColliderBackend.Simulation(backend.BodyID);
        _continuousForces.Add(new() { ID = backend.BodyID, Force = sim.force, Torque = sim.torque, Gravity = sim.gravityScale });
    }

    private void CaptureContinuousJointBudgets(B2World world, float nominal)
    {
        _continuousJointBudgets.Clear();
        for (var i = 0; i < world.joints.count; i++)
        {
            var joint = world.joints.data[i]; if (joint.setIndex == B2Constants.B2_NULL_INDEX) continue;
            var sim = b2GetJointSim(world, joint);
            _continuousJointBudgets.Add(new()
            {
                ID = new(joint.jointId + 1, world.worldId, joint.generation),
                Linear = sim.maxLinearForce,
                Angular = sim.maxAngularForce,
                Motor = MotorLimit(sim)
            });
        }
    }
    private static float MotorLimit(B2JointSim sim) => sim.type == B2JointType.b2_revoluteJoint ? sim.uj.revoluteJoint.maxMotorTorque :
        sim.type == B2JointType.b2_wheelJoint ? sim.uj.wheelJoint.maxMotorTorque : float.MaxValue;
    private static void SetMotorLimit(B2JointSim sim, float value)
    {
        if (sim.type == B2JointType.b2_revoluteJoint) sim.uj.revoluteJoint.maxMotorTorque = value;
        else if (sim.type == B2JointType.b2_wheelJoint) sim.uj.wheelJoint.maxMotorTorque = value;
    }
    private static float RemainingForce(float limit, double used, float nominal, float interval) =>
        limit == float.MaxValue ? limit : (float)Math.Min(float.MaxValue, Math.Max(0, (double)limit * nominal - used) / interval);
    private void SetContinuousJointBudgets(B2World world, float interval)
    {
        foreach (var budget in _continuousJointBudgets)
        {
            var sim = b2GetJointSim(world, b2GetJointFullId(world, budget.ID));
            var duration = budget.Duration > 0 ? budget.Duration : interval;
            sim.maxLinearForce = RemainingForce(budget.Linear, budget.UsedLinear, duration, interval);
            sim.maxAngularForce = RemainingForce(budget.Angular, budget.UsedAngular, duration, interval);
            SetMotorLimit(sim, RemainingForce(budget.Motor, budget.UsedMotor, duration, interval));
        }
    }
    private void AccumulateContinuousJointBudgets(B2World world, float delta)
    {
        foreach (ref var budget in CollectionsMarshal.AsSpan(_continuousJointBudgets))
        {
            var joint = b2GetJointFullId(world, budget.ID);
            if (joint.setIndex != (int)B2SolverSetType.b2_awakeSet) continue;
            if (budget.Duration == 0) budget.Duration = delta;
            var sim = b2GetJointSim(world, joint);
            budget.UsedLinear += b2Length(b2GetJointConstraintForce(world, joint)) * delta;
            budget.UsedAngular += MathF.Abs(b2GetJointConstraintTorque(world, joint)) * delta;
            budget.UsedMotor += sim.type == B2JointType.b2_revoluteJoint ? MathF.Abs(sim.uj.revoluteJoint.motorImpulse) :
                sim.type == B2JointType.b2_wheelJoint ? MathF.Abs(sim.uj.wheelJoint.motorImpulse) : 0;
        }
    }
    private void RestoreContinuousJointBudgets(B2World world)
    {
        foreach (var budget in _continuousJointBudgets)
        {
            var sim = b2GetJointSim(world, b2GetJointFullId(world, budget.ID));
            sim.maxLinearForce = budget.Linear; sim.maxAngularForce = budget.Angular; SetMotorLimit(sim, budget.Motor);
        }
    }
}
