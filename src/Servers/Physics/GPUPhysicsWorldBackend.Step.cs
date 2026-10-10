using static Electron2D.PhysicsSpace;

namespace Electron2D;

internal sealed partial class GPUPhysicsWorldBackend
{
    private bool _callbacks, _captureActivity, _intervalEntered, _resultsReady;
    private long _intervalSubmissions;
    internal override bool ResultsReady => _resultsReady;
    internal override void BeginStep(double delta)
    {
        _resultsReady = false; _intervalEntered = false; _intervalSubmissions = 0;
        EnsureAccess(); Space.LastStep = (float)delta;
    }
    internal override void PrepareWorld(double delta)
    {
        var start = ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        _callbacks = false; var pendingForces = false;
        foreach (var body in Space.Bodies)
        {
            var runtime = body.Runtime; body.Backend.PrepareGPUParameters(runtime, Space.ForceGPUParameterRefresh);
            _callbacks |= RequiresBodySnapshot(runtime, body);
            pendingForces |= runtime.PendingForce != Vector2.Zero || runtime.PendingTorque != 0;
        }
        foreach (var body in Space.ServerColliders)
            if (!body.IsArea)
            {
                var runtime = body.Runtime; body.Backend.PrepareGPUParameters(runtime, Space.ForceGPUParameterRefresh);
                _callbacks |= RequiresBodySnapshot(runtime, null);
                pendingForces |= runtime.PendingForce != Vector2.Zero || runtime.PendingTorque != 0;
            }
        if (ProfilingEnabled) Space.GPUPrepareBodiesMS = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        // Policy edits must finish before the shared activity snapshot consumed by force callbacks.
        _captureActivity = _callbacks || pendingForces;
        if (_captureActivity) Space.PublishGPU();
        uint areaOrder = 0;
        foreach (var area in Space.Areas) GPUStore.SetAreaFields(area.Backend.GPUHandle, GPUFields(area.Fields), areaOrder++);
        foreach (var body in Space.ServerColliders)
            if (body.IsArea) GPUStore.SetAreaFields(body.Backend.GPUHandle, GPUFields(body.AreaFields!), areaOrder++);
    }
    internal override void PrepareBodies(double delta)
    {
        var start = ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        Space.ClearCallbackBodies();
        foreach (var body in Space.Bodies) Space.PrepareGPUMotion(body.Runtime, body, delta, _captureActivity);
        foreach (var body in Space.ServerColliders)
            if (!body.IsArea) Space.PrepareGPUMotion(body.Runtime, null, delta, _captureActivity);
        if (_callbacks) Space.PrepareCallbackBodies();
        foreach (var joint in Space.SnapshotJoints) joint.ApplySolverPolicy();
        Space.SyncGPUExceptions();
        var mark = ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        if (ProfilingEnabled) Space.GPUPrepareBodiesMS += System.Diagnostics.Stopwatch.GetElapsedTime(start, mark).TotalMilliseconds;
        Space.PrepareGPUReports();
        if (ProfilingEnabled) { Space.GPUPrepareReportsMS = System.Diagnostics.Stopwatch.GetElapsedTime(mark).TotalMilliseconds; mark = System.Diagnostics.Stopwatch.GetTimestamp(); }
        var wait = ProfilingEnabled ? GPUStore.WaitMS : 0;
        Space.FlushGPUWakes();
        if (ProfilingEnabled) { Space.GPUPrepareWakesMS = System.Diagnostics.Stopwatch.GetElapsedTime(mark).TotalMilliseconds; Space.GPUPrepareWakeWaitMS = GPUStore.WaitMS - wait; }
    }
    internal override void Solve(double delta)
    {
        _intervalSubmissions = GPUStore.SubmissionCount; _intervalEntered = true;
        GPUStore.SimulateFields((float)delta, GPUFields(Space.DefaultAreaFields));
        Space.AdvanceStepTick(); Space.CaptureDebugContacts();
    }
    internal override void SyncResults()
    {
        Space.InvalidateGPUStates(wake: false);
        Space.PublishGPUCompletion(_callbacks); Space.ReadGPUReports(); _resultsReady = true;
    }
    internal override void CompleteBody(PhysicsServerCollider body)
    {
        if (RequiresGPUCompletion(body, _callbacks)) body.Backend.PublishGPUFields(body.Runtime);
        body.CompleteMotion();
    }
    internal override void CompleteBody(PhysicsBody body)
    {
        body.Backend.PublishGPUFields(body.Runtime); body.CompleteBackend();
        if (body is AnimatableBody animatable) animatable.SyncPose();
        else if (body is CharacterBody character) { character.CaptureSolverPose(); character.SetResolvedGravity(body.Runtime.Gravity); }
    }
    internal override void CollectContacts() => CollectBodyContactRange(0, Space.Bodies.Count, 0, Space);
    internal override void ScanAreas() => Space.ScanGPUAreas();
    internal override void EndStep(Exception? failure)
    {
        if (failure is not null && _intervalEntered && (GPUStore.HasFailed || GPUStore.SubmissionCount != _intervalSubmissions)) Space.FailGPUStep(failure);
    }
}
