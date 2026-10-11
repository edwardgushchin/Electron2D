namespace Electron2D;

internal sealed partial class RegisteredPhysicsWorldBackend
{
    internal override void BeginStep(double delta)
    {
        EnsureAccess(); _resultsReady = false; _solving = false; Space.LastStep = (float)delta;
        Enter(); try { _implementation.BeginStep(delta); } finally { Exit(); }
    }
    internal override void PrepareWorld(double delta)
    {
        foreach (var area in Space.Areas)
        {
            var collider = _colliders[area.Backend]; collider.SetAreaFields(area.Fields.Configuration);
            collider.SetAreaMonitoring(area.Monitoring || PhysicsServer.Service.FindAreaRuntime(area.GetRID())?.Monitoring == true, area.Monitorable);
        }
        foreach (var collider in Space.ServerColliders)
            if (collider.IsArea)
            {
                var attached = _colliders[collider.Backend]; attached.SetAreaFields(collider.AreaFields!.Configuration);
                attached.SetAreaMonitoring(PhysicsServer.Service.FindAreaRuntime(collider.RID)?.Monitoring == true, collider.Monitorable);
            }
        Enter(); try { _implementation.PrepareWorld(delta, Space.DefaultAreaFields.Configuration); } finally { Exit(); }
    }
    internal override void PrepareBodies(double delta)
    {
        Space.ClearCallbackBodies();
        foreach (var body in Space.Bodies)
        {
            PrepareIntegration(body.Runtime, delta); body.Runtime.ApplyBeforeStep(body);
            if (body is AnimatableBody animatable) animatable.PrepareMotion(delta);
            else if (body is CharacterBody character) character.PrepareMotion(delta);
            else if (body is RigidBody rigid) rigid.PrepareFrozenMotion(delta);
        }
        foreach (var body in Space.ServerColliders)
            if (!body.IsArea) { PrepareIntegration(body.Runtime, delta); body.Runtime.ApplyBeforeStep(null); body.PrepareMotion(delta); }
        Space.PrepareCallbackBodies();
        foreach (var joint in Space.SnapshotJoints) joint.ApplySolverPolicy();
        SynchronizeCollisionExceptions();
        Enter(); try { _implementation.PrepareBodies(delta); } finally { Exit(); }
    }
    private void PrepareIntegration(PhysicsBodyRuntime body, double delta)
    {
        var collider = _colliders[body.Backend]; collider.PrepareIntegration(body.CaptureIntegrationConfiguration(), delta);
        var fields = collider.ReadResolvedFields(new(body.Gravity, body.LinearDamp, body.AngularDamp, body.FieldsInitialized));
        if (!fields.Gravity.IsFinite() || !float.IsFinite(fields.LinearDamp) || !float.IsFinite(fields.AngularDamp))
            throw new InvalidOperationException("A registered implementation returned nonfinite body fields.");
        body.Gravity = fields.Gravity; body.LinearDamp = fields.LinearDamp; body.AngularDamp = fields.AngularDamp; body.FieldsInitialized = fields.Initialized;
    }
    internal override void Solve(double delta)
    {
        _solving = true;
        Enter();
        try
        {
            _implementation.Solve(delta);
            if (_implementation.HasFailure) throw new InvalidOperationException("The registered solver reported failure during its interval.");
            Space.AdvanceStepTick();
        }
        finally { Exit(); }
    }
    internal override void SyncResults()
    {
        Enter();
        try { _implementation.SyncResults(); _resultsReady = !_implementation.HasFailure && _implementation.ResultsReady; }
        finally { Exit(); }
        if (!_resultsReady) throw new InvalidOperationException("A completed registered solve did not publish its results.");
        Space.CaptureDebugContacts();
    }
    internal override void CompleteBody(PhysicsServerCollider body)
    {
        Enter(); try { _implementation.CompleteBody(body.RID); } finally { Exit(); }
        if (!body.IsArea) body.Runtime.PublishFields(completingStep: true);
        body.CompleteMotion();
    }
    internal override void CompleteBody(PhysicsBody body)
    {
        Enter(); try { _implementation.CompleteBody(body.GetRID()); } finally { Exit(); }
        body.Runtime.PublishFields(completingStep: true); body.CompleteBackend();
        if (body is AnimatableBody animatable) animatable.SyncPose();
        else if (body is CharacterBody character) { character.CaptureSolverPose(); character.SetResolvedGravity(body.Runtime.Gravity); }
    }
    internal override void CollectContacts(ref List<Exception>? errors)
    {
        Enter(); try { _implementation.CollectContacts(); } finally { Exit(); }
        foreach (var body in Space.Bodies)
            if (body is RigidBody rigid && rigid.NeedsContactSnapshot)
            {
                try
                {
                    var view = rigid.Runtime.View!; view.CaptureContacts();
                    if (view is not PhysicsDirectBodyStateExtension) rigid.PublishContactMonitor(view);
                }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
    }
    internal override void EndStep(Exception? failure)
    {
        if (failure is not null && _solving) { _failure = failure; _resultsReady = false; }
        Enter(); try { _implementation.EndStep(failure); } finally { Exit(); _solving = false; }
    }
    internal override bool AreaContainsPoint(Area area, Vector2 point, uint mask)
    { Enter(); try { return _implementation.AreaContainsPoint(area.GetRID(), point, mask); } finally { Exit(); } }
}
