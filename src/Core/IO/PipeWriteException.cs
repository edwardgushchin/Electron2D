namespace Electron2D;

/// <summary>Reports the consumed prefix of a process-pipe write that could not complete.</summary>
/// <remarks>The caller may retry only the remaining bytes. A full nonblocking pipe is a temporary failure;
/// a closed or failed pipe cannot accept a retry. <see cref="BytesWritten"/> describes this write call.</remarks>
public sealed class PipeWriteException : IOException
{
    internal PipeWriteException(int bytesWritten, string message) : base(message) { BytesWritten = bytesWritten; }
    /// <summary>Gets the number of bytes accepted by the pipe before the write failed.</summary>
    /// <value>A nonnegative prefix length of the complete write, including any framing emitted by the operation.</value>
    public int BytesWritten { get; }
}
