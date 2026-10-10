namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal RID RID { get; }
    internal float LastStep { get; set; }
    private bool _dispatchingBodyStates;
    private readonly List<CallbackBody> _callbackBodies = [];
    internal List<CallbackBody> CallbackBodies => _callbackBodies;
    internal readonly record struct CallbackBody(PhysicsBodyRuntime Runtime, PhysicsColliderBackend Backend, long Attachment, PhysicsBody? Scene);

    private bool _creatingDirectState;
    internal PhysicsDirectSpaceState CreateDirectSpaceState()
    {
        if (_creatingDirectState) throw new InvalidOperationException("A space-state factory cannot recursively acquire its own view.");
        BeginExtensionCallback(); _creatingDirectState = true;
        try
        {
            var state = BackendImplementation.CreateDirectSpaceState();
            if (state is null) throw new InvalidOperationException("A space-state factory returned no view.");
            state.EnsureContext(this);
            return state;
        }
        finally { _creatingDirectState = false; EndExtensionCallback(); }
    }

    private void PrepareBodyViews()
    {
        // Factories can invoke guarded public hooks, so create views before solver and worker borrowing.
        foreach (var body in _bodies)
            if (body is RigidBody || RequiresBodySnapshot(body.Runtime, body))
            {
                body.PrepareBackend(); body.Runtime.GetView(this);
            }
        foreach (var body in _serverColliders)
            if (!body.IsArea && RequiresBodySnapshot(body.Runtime, null))
            {
                body.PrepareBackend(); body.Runtime.GetView(this);
            }
    }

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
                PhysicsDirectBodyState state;
                try { state = body.Runtime.GetView(this); }
                catch (Exception error) { (errors ??= []).Add(error); continue; }
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
