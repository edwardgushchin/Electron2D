namespace Electron2D;

/// <summary>Provides a caller-owned counted signal for cooperating managed threads.</summary>
/// <remarks>The initial count is zero. Each successful Wait or TryWait consumes one posted token.
/// Join all waiting threads before disposal. Ordering and fairness are unspecified; scene/native access remains
/// thread-affine. Browser construction requires a threaded runtime and is currently rejected.</remarks>
public sealed class Semaphore : ElectronObject
{
    private readonly object _gate = new();
    private uint _count;
    private int _waiters;

    /// <summary>Creates a semaphore with no available tokens.</summary>
    /// <exception cref="PlatformNotSupportedException">Browser synchronization has no threaded runtime bootstrap.</exception>
    public Semaphore()
    {
        if (OperatingSystem.IsBrowser()) { CompleteFailedConstruction(); throw new PlatformNotSupportedException("Synchronization requires a threaded browser runtime."); }
    }

    /// <summary>Posts tokens and wakes waiting threads.</summary>
    /// <param name="count">Positive number of tokens to post; one by default.</param>
    /// <remarks>Available tokens are bounded to UInt32 capacity. Validation or overflow preserves the count.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Count is not positive.</exception>
    /// <exception cref="InvalidOperationException">The available count would overflow.</exception>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    public void Post(int count = 1)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            var next = (ulong)_count + (uint)count;
            if (next > uint.MaxValue) throw new InvalidOperationException("The semaphore token count would overflow.");
            _count = (uint)next;
            if (_waiters != 0) Monitor.PulseAll(_gate);
        }
    }

    /// <summary>Consumes one token, waiting while none is available.</summary>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    /// <exception cref="ThreadInterruptedException">The waiting thread is interrupted.</exception>
    public void Wait()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            _waiters++;
            try
            {
                while (_count == 0)
                {
                    ObjectDisposedException.ThrowIf(IsDisposed, this);
                    Monitor.Wait(_gate);
                }
                ObjectDisposedException.ThrowIf(IsDisposed, this);
                _count--;
            }
            finally { _waiters--; }
        }
    }

    /// <summary>Consumes an available token without waiting for a signal.</summary>
    /// <returns>True when one token was consumed; false when none was available.</returns>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    public bool TryWait()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (_count == 0) return false;
            _count--;
            return true;
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">A wait is still in flight.</exception>
    protected override void ValidateDisposal()
    {
        lock (_gate) if (_waiters != 0) throw new InvalidOperationException("Finish all semaphore waits before disposal.");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) { _count = 0; Monitor.PulseAll(_gate); }
        base.Dispose(disposing);
    }
}
