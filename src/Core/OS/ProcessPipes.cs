namespace Electron2D;

/// <summary>Owns the typed stdin/stdout, stderr and process-ID result of OS.ExecuteWithPipe.</summary>
/// <remarks>Disposal closes both FileAccess streams without terminating the independent child. Stream closure is idempotent.
/// The process ID remains available for OS.IsProcessRunning, OS.GetProcessExitCode and OS.Kill.</remarks>
public sealed class ProcessPipes : IDisposable
{
    internal ProcessPipes(int pid, FileAccess io, FileAccess error) { PID = pid; StandardIO = io; StandardError = error; }
    /// <summary>Gets the independently running child's native process ID.</summary>
    /// <value>A positive native process ID; operating systems can reuse IDs after exit.</value>
    public int PID { get; }
    /// <summary>Gets the owned bidirectional stdin/stdout FileAccess stream.</summary>
    /// <value>A non-seekable stream that writes child input and reads child output.</value>
    public FileAccess StandardIO { get; }
    /// <summary>Gets the owned read-only stderr FileAccess stream.</summary>
    /// <value>A non-seekable stream independent of standard output.</value>
    public FileAccess StandardError { get; }
    private int _disposed;
    /// <summary>Closes both owned streams once without terminating the independent child.</summary>
    /// <remarks>Process identity and stream references remain readable; the FileAccess objects themselves are disposed.</remarks>
    public void Dispose() { if (Interlocked.Exchange(ref _disposed, 1) != 0) return; try { StandardIO.Dispose(); } finally { StandardError.Dispose(); } }
}
