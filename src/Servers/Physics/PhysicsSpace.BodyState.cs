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
    private readonly record struct CallbackBody(PhysicsBodyRuntime Runtime, B2BodyId ID, PhysicsBody? Scene);

    private void CaptureBodyMotions()
    {
        Array.Clear(_bodyMotions);
        var world = b2GetWorldFromId(_worldID);
        var awake = world.solverSets.data[(int)B2SolverSetType.b2_awakeSet];
        for (var i = 0; i < awake.bodySims.count; i++)
        {
            var sim = awake.bodySims.data[i];
            var state = awake.bodyStates.data[i];
            _bodyMotions[sim.bodyId] = new(sim.center, b2Add(state.linearVelocity, sim.surfaceLinearVelocity), state.angularVelocity + sim.surfaceAngularVelocity, true);
        }
        var stationary = world.solverSets.data[(int)B2SolverSetType.b2_staticSet];
        for (var i = 0; i < stationary.bodySims.count; i++)
        {
            var sim = stationary.bodySims.data[i];
            _bodyMotions[sim.bodyId] = new(sim.center, sim.surfaceLinearVelocity, sim.surfaceAngularVelocity, true);
        }
    }

    internal B2Vec2 SolvedPointVelocity(B2BodyId id, B2Vec2 point)
    {
        ref readonly var motion = ref _bodyMotions[id.index1 - 1];
        return motion.Active ? b2Add(motion.Velocity, b2CrossSV(motion.Angular, b2Sub(point, motion.Center))) : default;
    }

    private bool PrepareBodyStates(double delta)
    {
        LastStep = (float)delta;
        _callbackBodies.Clear();
        var hasKinematicBodies = false;
        var captureCallbacks = false;
        var uniform = _fieldAreas.Count == 0 && !DefaultAreaFields.GravityPoint;
        var gravity = DefaultAreaFields.GravityVector * DefaultAreaFields.Gravity;
        var linearDamp = DefaultAreaFields.LinearDamp;
        var angularDamp = DefaultAreaFields.AngularDamp;
        foreach (var body in _bodies)
        {
            var runtime = body.Runtime;
            if (body is RigidBody or CharacterBody)
            {
                if (!uniform) ResolveAreaFields(body.CollisionLayer, body.BackendShapes, body.GlobalPosition, out gravity, out linearDamp, out angularDamp);
                if (body is RigidBody rigid) rigid.ApplyAreaFields(gravity, linearDamp, angularDamp, _defaultGravity, delta);
                else
                {
                    runtime.ApplyResolvedFields(body.BackendID, gravity, linearDamp, angularDamp, _defaultGravity, delta);
                    ((CharacterBody)body).SetResolvedGravity(runtime.Gravity);
                }
            }
            runtime.ApplyBeforeStep(body.BackendID, body);
            captureCallbacks |= RequiresBodySnapshot(runtime, body);
            if (body is AnimatableBody animatable) animatable.PrepareMotion(delta);
            else if (body is CharacterBody character) character.PrepareMotion(delta);
            else if (body is RigidBody rigid) rigid.PrepareFrozenMotion(delta);
            hasKinematicBodies |= body.RequestedBodyMode == PhysicsServer.BodyMode.Kinematic && !body.PhysicsMadeStatic;
        }
        foreach (var body in _serverColliders)
        {
            if (body.IsArea) continue;
            var runtime = body.Runtime;
            if (body.Mode != PhysicsServer.BodyMode.Static)
            {
                if (!uniform) ResolveAreaFields(body.CollisionLayer, body.BackendShapes, body.GetTransform().Origin, out gravity, out linearDamp, out angularDamp);
                runtime.ApplyResolvedFields(body.BackendID, gravity, linearDamp, angularDamp, _defaultGravity, delta);
            }
            runtime.ApplyBeforeStep(body.BackendID, null);
            body.PrepareMotion(delta);
            captureCallbacks |= RequiresBodySnapshot(runtime, null);
            hasKinematicBodies |= body.Mode == PhysicsServer.BodyMode.Kinematic;
        }
        if (captureCallbacks)
        {
            // Keep the complete order when callbacks can enable later receivers during dispatch.
            foreach (var body in _bodies) _callbackBodies.Add(new(body.Runtime, body.BackendID, body));
            foreach (var body in _serverColliders) if (!body.IsArea) _callbackBodies.Add(new(body.Runtime, body.BackendID, null));
        }
        return hasKinematicBodies;
    }

    private static bool RequiresBodySnapshot(PhysicsBodyRuntime runtime, PhysicsBody? scene) =>
        runtime.ForceCallback is not null || runtime.SyncCallback is not null ||
        (scene is RigidBody ? scene.GetType() != typeof(RigidBody) : runtime.MaxContacts > 0 || runtime.View is { IsDisposed: false, HasCapturedContacts: true });

    private bool Current(CallbackBody body)
    {
        try
        {
            if (body.Scene is { } scene)
                return !scene.IsDisposed && scene.Space == this && scene.BackendID == body.ID;
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
            if (body.Scene is not RigidBody &&
                (body.Runtime.MaxContacts > 0 || body.Runtime.View is { IsDisposed: false, HasCapturedContacts: true }) && Current(body))
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
                var owner = body.Scene;
                if (body.Runtime.ForceCallback is null && body.Runtime.SyncCallback is null &&
                    (owner is not RigidBody || owner.GetType() == typeof(RigidBody))) continue;
                if (!Current(body)) continue;
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
