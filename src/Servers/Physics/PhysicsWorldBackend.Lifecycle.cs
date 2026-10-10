namespace Electron2D;

internal abstract partial class PhysicsWorldBackend
{
    private bool _active, _synchronizing, _flushing;
    internal bool IsActive => Volatile.Read(ref _active);
    internal bool IsSynchronizing => _synchronizing;
    internal bool IsFlushingQueries => _flushing;
    internal virtual void Initialize() => EnsureAccess();
    internal void SetActive(bool active)
    {
        EnsureAccess();
        if (IsActive == active) return;
        Space.BeginBackendCallback();
        try { ApplyActive(active); }
        finally { Space.EndExtensionCallback(); }
        Volatile.Write(ref _active, active);
    }
    internal virtual void ApplyActive(bool active) => EnsureAccess();
    internal void SynchronizeResults()
    {
        _synchronizing = true;
        SyncResults();
    }
    internal void FlushQueryEvents()
    {
        var previous = _flushing;
        _flushing = true;
        Exception? failure = null;
        try
        {
            Space.BeginBackendCallback();
            try { FlushQueries(); }
            catch (Exception error) { failure = error; }
            finally { Space.EndExtensionCallback(); }
            try { Space.DispatchBackendEvents(); }
            catch (Exception error)
            {
                if (failure is not null) throw new AggregateException("Backend flush and observer delivery failed.", failure, error);
                throw;
            }
            if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        finally { _flushing = previous; }
    }
    internal virtual void FlushQueries() => Space.EnsureReleaseAccess();
    internal void EndSynchronization(Exception? failure)
    {
        try
        {
            Space.BeginBackendCallback();
            try { EndSync(failure); }
            finally { Space.EndExtensionCallback(); }
        }
        finally { _synchronizing = false; }
    }
    internal virtual void EndSync(Exception? failure) => _synchronizing = false;
}
