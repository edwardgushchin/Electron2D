using Box2D.NET;
using static Box2D.NET.B2Bodies;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal RID RID { get; set; }
    internal float LastStep { get; private set; }
    private bool _dispatchingBodyStates;
    private readonly List<CallbackBody> _callbackBodies = [];
    private readonly record struct CallbackBody(PhysicsBodyRuntime Runtime, B2BodyId ID);

    private void PrepareBodyStates(double delta)
    {
        LastStep = (float)delta;
        _callbackBodies.Clear();
        foreach (var body in _bodies)
        {
            var runtime = PhysicsServer.Service.BodyRuntime(body.GetRID());
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
        try { return body.Runtime.Space == this && body.Runtime.BodyID == body.ID; }
        catch (ArgumentException) { return false; }
    }

    private void CaptureBodyStates()
    {
        foreach (var body in _callbackBodies)
            if (Current(body)) body.Runtime.GetView(this, body.ID).CaptureContacts();
    }

    private void DispatchBodyStates()
    {
        _dispatchingBodyStates = true;
        List<Exception>? errors = null;
        try
        {
            foreach (var body in _callbackBodies)
            {
                if (!Current(body) || b2Body_GetType(body.ID) == B2BodyType.b2_staticBody ||
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
