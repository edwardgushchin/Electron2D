namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal void ExecuteStep(PhysicsWorldBackend backend, double delta)
    {
        List<Exception>? errors = null; Exception? failure = null;
        var profileMark = ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        if (ProfilingEnabled) _profileAllocated = GC.GetAllocatedBytesForCurrentThread();
        try
        {
            if (_separateSolverThread) _solverThread ??= new(this);
            backend.BeginStep(delta);
            PrepareBodyViews();
            _stepping = true;
            foreach (var body in _bodies) body.PrepareBackend();
            foreach (var area in _areas) area.PrepareBackend();
            foreach (var body in _serverColliders) body.PrepareBackend();
            foreach (var joint in _joints) joint.PrepareBackend();
            RecordStepPhase(0, ref profileMark);
            backend.PrepareWorld(delta); RecordStepPhase(1, ref profileMark);
            backend.PrepareBodies(delta); RecordStepPhase(2, ref profileMark);
            if (_solverThread is null) backend.Solve(delta); else _solverThread.Solve(backend, delta);
            RecordStepPhase(3, ref profileMark);
            backend.SynchronizeResults(); RecordStepPhase(4, ref profileMark);
            var completionMark = ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            foreach (var body in _serverColliders) backend.CompleteBody(body);
            foreach (var body in _bodies)
            {
                try { backend.CompleteBody(body); }
                catch (Exception error) { if (!backend.ResultsReady) throw; (errors ??= []).Add(error); }
            }
            RecordCompletionPhase(0, ref completionMark);
            backend.CollectContacts(ref errors);
            RecordCompletionPhase(1, ref completionMark);
            foreach (var body in _bodies)
                if (body is RigidBody rigid)
                {
                    if (rigid.NeedsContactSnapshot && rigid.Runtime.View is PhysicsDirectBodyStateExtension view)
                    {
                        try { view.ProjectContacts(); rigid.PublishContactMonitor(view); }
                        catch (Exception error) { (errors ??= []).Add(error); }
                    }
                    if (rigid.TakeSleepChange()) _sleepEvents.Add(rigid);
                    rigid.QueueContactChanges(_contactEvents);
                }
            RecordCompletionPhase(2, ref completionMark);
            RecordStepPhase(5, ref profileMark);
            backend.ScanAreas(ref errors); CaptureBodyStates(ref errors); PublishStatistics();
            RecordStepPhase(6, ref profileMark);
        }
        catch (Exception error) { failure = error; (errors ??= []).Add(error); }
        finally
        {
            _stepping = true;
            try { backend.EndStep(failure); }
            catch (Exception error) { (errors ??= []).Add(error); }
            finally { _stepping = false; }
        }
        try { if (backend.ResultsReady) DispatchBodyStates(); else _callbackBodies.Clear(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        try { backend.FlushQueryEvents(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        try { backend.EndSynchronization(failure ?? (errors is { Count: > 0 } ? errors[0] : null)); }
        catch (Exception error) { (errors ??= []).Add(error); }
        RecordStepPhase(7, ref profileMark);
        if (errors is not null) throw new AggregateException(backend.Kind == PhysicsServer.Backend.GPU ? "GPU physics-world step failed." : "Physics-world step failed.", errors);
    }
    internal void AdvanceStepTick() => Tick++;
    internal void BeginSolverBorrow(int threadID)
    {
        if (!IsOwnerThread || !_stepping || _solverBorrowThreadID != 0) throw new InvalidOperationException("A numerical interval can borrow one solver thread only after owner preparation.");
        GPUStore?.BeginSolverBorrow(threadID);
        _solverBorrowThreadID = threadID;
    }
    internal void EndSolverBorrow()
    {
        _solverBorrowThreadID = 0;
        GPUStore?.EndSolverBorrow();
    }
    internal void ClearCallbackBodies() => _callbackBodies.Clear();
    internal void PrepareCallbackBodies()
    {
        foreach (var body in _bodies) _callbackBodies.Add(new(body.Runtime, body.Backend, body.Backend.AttachmentVersion, body));
        foreach (var body in _serverColliders) if (!body.IsArea) _callbackBodies.Add(new(body.Runtime, body.Backend, body.Backend.AttachmentVersion, null));
    }
    internal void FailGPUStep(Exception error) => _gpuFailure = error;
}
