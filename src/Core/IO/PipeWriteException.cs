namespace Electron2D;

/// <summary>Reports the consumed prefix of a process-pipe write that could not complete.</summary>
/// <remarks>The caller may retry only the remaining bytes. A full nonblocking pipe is a temporary failure;
/// a closed or failed pipe cannot accept a retry. This projects partial-write error state without a shared last-error slot.</remarks>
public sealed class PipeWriteException : IOException
{
    internal PipeWriteException(int bytesWritten, string message) : base(message) { BytesWritten = bytesWritten; }
    /// <summary>Gets the byte count already delivered by the failed write operation.</summary>
    /// <value>A nonnegative prefix length of the supplied buffer.</value>
    public int BytesWritten { get; }
}
