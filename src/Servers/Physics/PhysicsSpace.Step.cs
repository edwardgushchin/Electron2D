namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal void ExecuteStep(PhysicsWorldBackend backend, double delta)
    {
        _stepping = true;
        List<Exception>? errors = null; Exception? failure = null;
        var profileMark = ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        if (ProfilingEnabled) _profileAllocated = GC.GetAllocatedBytesForCurrentThread();
        try
        {
            backend.BeginStep(delta);
            foreach (var body in _bodies) body.PrepareBackend();
            foreach (var area in _areas) area.PrepareBackend();
            foreach (var body in _serverColliders) body.PrepareBackend();
            foreach (var joint in _joints) joint.PrepareBackend();
            RecordStepPhase(0, ref profileMark);
            backend.PrepareWorld(delta); RecordStepPhase(1, ref profileMark);
            backend.PrepareBodies(delta); RecordStepPhase(2, ref profileMark);
            backend.Solve(delta); RecordStepPhase(3, ref profileMark);
            backend.SyncResults(); RecordStepPhase(4, ref profileMark);
            foreach (var body in _serverColliders) backend.CompleteBody(body);
            foreach (var body in _bodies)
            {
                try { backend.CompleteBody(body); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
            backend.CollectContacts();
            foreach (var body in _bodies)
                if (body is RigidBody rigid)
                {
                    if (rigid.TakeSleepChange()) _sleepEvents.Add(rigid);
                    rigid.QueueContactChanges(_contactEvents);
                }
            RecordStepPhase(5, ref profileMark);
            backend.ScanAreas(); CaptureBodyStates(); PublishStatistics();
            RecordStepPhase(6, ref profileMark);
        }
        catch (Exception error) { failure = error; (errors ??= []).Add(error); }
        finally
        {
            try { backend.EndStep(failure); }
            catch (Exception error) { (errors ??= []).Add(error); }
            finally { _stepping = false; }
        }
        try { if (backend.ResultsReady) DispatchBodyStates(); else _callbackBodies.Clear(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        try { DispatchEvents(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        RecordStepPhase(7, ref profileMark);
        if (errors is not null) throw new AggregateException(backend.Kind == PhysicsServer.Backend.GPU ? "GPU physics-world step failed." : "Physics-world step failed.", errors);
    }
    internal void AdvanceStepTick() => Tick++;
    internal void ClearCallbackBodies() => _callbackBodies.Clear();
    internal void PrepareCallbackBodies()
    {
        foreach (var body in _bodies) _callbackBodies.Add(new(body.Runtime, body.Backend, body.Backend.AttachmentVersion, body));
        foreach (var body in _serverColliders) if (!body.IsArea) _callbackBodies.Add(new(body.Runtime, body.Backend, body.Backend.AttachmentVersion, null));
    }
    internal void FailGPUStep(Exception error) => _gpuFailure = error;
}
