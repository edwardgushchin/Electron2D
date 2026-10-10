namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal RID RID { get; set; }
    internal float LastStep { get; set; }
    private bool _dispatchingBodyStates;
    private readonly List<CallbackBody> _callbackBodies = [];
    private readonly record struct CallbackBody(PhysicsBodyRuntime Runtime, PhysicsColliderBackend Backend, long Attachment, PhysicsBody? Scene);

    internal static bool RequiresBodySnapshot(PhysicsBodyRuntime runtime, PhysicsBody? scene) =>
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
