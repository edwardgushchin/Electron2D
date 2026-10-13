namespace Electron2D;

/// <summary>Provides a caller-owned recursive mutual-exclusion lock for shared managed data.</summary>
/// <remarks>Each successful Lock or TryLock requires one Unlock on the same managed thread.
/// Retain this object and finish all acquisitions before disposal. No scene or renderer thread access is granted.
/// Browser construction requires a threaded runtime and is currently rejected.</remarks>
public sealed class Mutex : ElectronObject
{
    private readonly object _gate = new();
    private int _pendingAcquisitions;
    private int _depth;

    /// <summary>Creates an unlocked recursive mutex.</summary>
    /// <exception cref="PlatformNotSupportedException">Browser synchronization has no threaded runtime bootstrap.</exception>
    public Mutex()
    {
        if (OperatingSystem.IsBrowser()) { CompleteFailedConstruction(); throw new PlatformNotSupportedException("Synchronization requires a threaded browser runtime."); }
    }

    /// <summary>Acquires the mutex, waiting while another thread owns it.</summary>
    /// <remarks>Reentrant acquisition by the owner increments its release count without waiting.</remarks>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    /// <exception cref="ThreadInterruptedException">The waiting thread is interrupted.</exception>
    /// <exception cref="OverflowException">The recursive acquisition count exceeds Int32 capacity.</exception>
    public void Lock() => Acquire(true);

    /// <summary>Attempts one acquisition without waiting for the mutex.</summary>
    /// <returns>True after acquiring or recursively reacquiring it; false while another thread owns it.</returns>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    /// <exception cref="OverflowException">The recursive acquisition count exceeds Int32 capacity.</exception>
    public bool TryLock() => Acquire(false);

    /// <summary>Releases one acquisition owned by this managed thread.</summary>
    /// <remarks>Use finally to release successful acquisitions. An existing owner may release its lock even
    /// if disposal raced its acquisition; this cannot grant a new acquisition on the disposed mutex.</remarks>
    /// <exception cref="SynchronizationLockException">This thread does not own an acquisition.</exception>
    /// <exception cref="ObjectDisposedException">The disposed mutex has no acquisition on this thread.</exception>
    public void Unlock()
    {
        if (!Monitor.IsEntered(_gate))
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            throw new SynchronizationLockException("Unlock requires an acquisition on this thread.");
        }
        _depth--;
        Monitor.Exit(_gate);
    }

    private bool Acquire(bool blocking)
    {
        Interlocked.Increment(ref _pendingAcquisitions);
        var entered = false; var accepted = false;
        try
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (blocking) Monitor.Enter(_gate, ref entered);
            else entered = Monitor.TryEnter(_gate);
            if (!entered) return false;
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            _depth = checked(_depth + 1);
            accepted = true;
            return true;
        }
        finally
        {
            if (entered && !accepted) Monitor.Exit(_gate);
            Interlocked.Decrement(ref _pendingAcquisitions);
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The mutex is held or an acquisition is in flight.</exception>
    protected override void ValidateDisposal()
    {
        if (Monitor.IsEntered(_gate) || !Monitor.TryEnter(_gate))
            throw new InvalidOperationException("Release all mutex acquisitions before disposal.");
        try
        {
            if (_depth != 0 || Volatile.Read(ref _pendingAcquisitions) != 0)
                throw new InvalidOperationException("Finish mutex acquisitions before disposal.");
        }
        finally { Monitor.Exit(_gate); }
    }
}
