namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal RID RID { get; set; }
    internal float LastStep { get; private set; }
    private bool _dispatchingBodyStates;
    private readonly List<CallbackBody> _callbackBodies = [];
    private readonly record struct CallbackBody(PhysicsBodyRuntime Runtime, PhysicsColliderBackend Backend, long Attachment, PhysicsBody? Scene);

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
                runtime.ApplyResolvedFields(gravity, linearDamp, angularDamp, _defaultGravity, delta);
                if (body is CharacterBody character) character.SetResolvedGravity(runtime.Gravity);
            }
            runtime.ApplyBeforeStep(body);
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
                runtime.ApplyResolvedFields(gravity, linearDamp, angularDamp, _defaultGravity, delta);
            }
            runtime.ApplyBeforeStep(null);
            body.PrepareMotion(delta);
            captureCallbacks |= RequiresBodySnapshot(runtime, null);
            hasKinematicBodies |= body.Mode == PhysicsServer.BodyMode.Kinematic;
        }
        if (captureCallbacks)
        {
            // Keep the complete order when callbacks can enable later receivers during dispatch.
            foreach (var body in _bodies) _callbackBodies.Add(new(body.Runtime, body.Backend, body.Backend.AttachmentVersion, body));
            foreach (var body in _serverColliders) if (!body.IsArea) _callbackBodies.Add(new(body.Runtime, body.Backend, body.Backend.AttachmentVersion, null));
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
            return body.Scene?.IsDisposed != true && body.Runtime.Backend == body.Backend &&
                body.Backend.Space == this && body.Backend.AttachmentVersion == body.Attachment;
        }
        catch (ArgumentException) { return false; }
    }

    private void CaptureBodyStates()
    {
        foreach (var body in _callbackBodies)
        {
            if (body.Scene is not RigidBody &&
                (body.Runtime.MaxContacts > 0 || body.Runtime.View is { IsDisposed: false, HasCapturedContacts: true }) && Current(body))
                body.Runtime.GetView(this).CaptureContacts();
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
                if (body.Backend.HasMotionMode(PhysicsServer.BodyMode.Static) ||
                    !body.Runtime.ActiveBeforeStep && !body.Backend.IsAwake) continue;
                var state = body.Runtime.GetView(this);
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
