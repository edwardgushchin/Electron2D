using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal RID RID { get; set; }
    internal float LastStep { get; private set; }
    private BodyMotion[] _bodyMotions = [];
    private readonly record struct BodyMotion(B2Vec2 Center, B2Vec2 Velocity, float Angular, bool Active);
    private bool _dispatchingBodyStates;
    private readonly List<CallbackBody> _callbackBodies = [];
    private readonly record struct CallbackBody(PhysicsBodyRuntime Runtime, B2BodyId ID);

    private void CaptureBodyMotions()
    {
        Array.Clear(_bodyMotions);
        var world = b2GetWorldFromId(_worldID);
        var awake = world.solverSets.data[(int)B2SolverSetType.b2_awakeSet];
        for (var i = 0; i < awake.bodySims.count; i++)
        {
            var sim = awake.bodySims.data[i];
            var state = awake.bodyStates.data[i];
            _bodyMotions[sim.bodyId] = new(sim.center, state.linearVelocity, state.angularVelocity, true);
        }
    }

    internal B2Vec2 SolvedPointVelocity(B2BodyId id, B2Vec2 point)
    {
        ref readonly var motion = ref _bodyMotions[id.index1 - 1];
        return motion.Active ? b2Add(motion.Velocity, b2CrossSV(motion.Angular, b2Sub(point, motion.Center))) : default;
    }

    private void PrepareBodyStates(double delta)
    {
        LastStep = (float)delta;
        _callbackBodies.Clear();
        foreach (var body in _bodies)
        {
            var runtime = body.Runtime;
            runtime.ApplyBeforeStep();
            runtime.GetView(this, body.BackendID);
            _callbackBodies.Add(new(runtime, body.BackendID));
        }
        foreach (var body in _serverColliders)
        {
            if (body.IsArea) continue;
            var runtime = PhysicsServer.Service.BodyRuntime(body.RID);
            runtime.ApplyBeforeStep();
            runtime.GetView(this, body.BackendID);
            _callbackBodies.Add(new(runtime, body.BackendID));
        }
    }

    private bool Current(CallbackBody body)
    {
        try
        {
            var owner = body.Runtime.Owners;
            return (owner.Scene?.Space ?? owner.Server?.Space) == this &&
                (owner.Scene?.BackendID ?? owner.Server!.BackendID) == body.ID;
        }
        catch (ArgumentException) { return false; }
    }

    private void CaptureBodyStates()
    {
        foreach (var body in _callbackBodies)
        {
            if (Current(body) && body.Runtime.Owners.Scene is not RigidBody)
                body.Runtime.GetView(this, body.ID).CaptureContacts();
        }
    }

    private void DispatchBodyStates()
    {
        _dispatchingBodyStates = true;
        List<Exception>? errors = null;
        try
        {
            foreach (var body in _callbackBodies)
            {
                if (!Current(body)) continue;
                var owner = body.Runtime.Owners.Scene;
                if (body.Runtime.ForceCallback is null && body.Runtime.SyncCallback is null &&
                    (owner is not RigidBody || owner.GetType() == typeof(RigidBody))) continue;
                if (b2Body_GetType(body.ID) == B2BodyType.b2_staticBody ||
                    !body.Runtime.ActiveBeforeStep && !b2Body_IsAwake(body.ID)) continue;
                var state = body.Runtime.GetView(this, body.ID);
                try { state.BeginCallback(); body.Runtime.ForceCallback?.Invoke(state); }
                catch (Exception error) { (errors ??= []).Add(error); }
                finally { state.EndCallback(); }
                if (!Current(body)) continue;
                if (body.Runtime.Owners.Scene is RigidBody rigid)
                {
                    try { rigid.PrepareBackend(); rigid.CompleteBackend(); state.BeginCallback(); rigid.InvokeIntegration(state); }
                    catch (Exception error) { (errors ??= []).Add(error); }
                    finally { if (state.CallbackActive) state.EndCallback(); }
                }
                if (!Current(body)) continue;
                try { state.BeginCallback(); body.Runtime.SyncCallback?.Invoke(state); }
                catch (Exception error) { (errors ??= []).Add(error); }
                finally { state.EndCallback(); }
                if (!Current(body)) continue;
                if (body.Runtime.Owners.Scene is { } scene)
                {
                    try { scene.PrepareBackend(); scene.CompleteBackend(); }
                    catch (Exception error) { (errors ??= []).Add(error); }
                }
            }
        }
        finally { _dispatchingBodyStates = false; _callbackBodies.Clear(); }
        if (errors is not null) throw new AggregateException("Body integration callbacks failed.", errors);
    }
}
