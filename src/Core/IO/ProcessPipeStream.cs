using SDL3;

namespace Electron2D;

internal sealed unsafe class ProcessPipeStream : Stream
{
    private readonly OSProcess _owner;
    private readonly ProcessPipeHandle _read;
    private readonly ProcessPipeHandle? _write;
    private readonly bool _blocking;
    private bool _closed;
    private int _pending = -1;
    internal bool EOFReached { get; private set; }
    internal ProcessPipeStream(OSProcess owner, nint read, nint write, bool blocking)
    {
        if (read == 0) throw new IOException("The child output pipe is unavailable."); _owner = owner; _read = new(owner, read); try { _write = write == 0 ? null : new(owner, write); } catch { _read.Dispose(); throw; }
        _blocking = blocking;
    }
    public override bool CanRead => !_closed;
    public override bool CanWrite => !_closed && _write is not null;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException("Process pipes have no seekable length.");
    public override long Position { get => throw new NotSupportedException("Process pipes have no seekable cursor."); set => throw new NotSupportedException("Process pipes cannot seek."); }
    internal void PushBack(byte value) { _pending = value; EOFReached = false; }
    public override int ReadByte() { Span<byte> value = stackalloc byte[1]; return Read(value) == 0 ? -1 : value[0]; }
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_closed, this); if (buffer.IsEmpty) return 0;
        if (_pending >= 0) { buffer[0] = (byte)_pending; _pending = -1; return 1; }
        while (true)
        {
            lock (_owner.Gate)
            {
                ObjectDisposedException.ThrowIf(_closed, this);
                fixed (byte* data = buffer)
                {
                    var count = checked((int)SDL.ReadIO(_read.DangerousGetHandle(), (nint)data, (nuint)buffer.Length)); var status = SDL.GetIOStatus(_read.DangerousGetHandle());
                    if (status == SDL.IOStatus.Error) throw new IOException(SDL.GetError());
                    EOFReached = status == SDL.IOStatus.EOF; if (count != 0 || status != SDL.IOStatus.NotReady || !_blocking) return count;
                }
            }
            Thread.Sleep(1);
        }
    }
    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_closed, this); if (_write is null) throw new InvalidOperationException("The pipe is read-only."); var written = 0;
        while (written < buffer.Length)
        {
            lock (_owner.Gate)
            {
                if (_closed || _write.IsClosed) throw new PipeWriteException(written, "The pipe closed during the write.");
                fixed (byte* data = buffer)
                {
                    var count = checked((int)SDL.WriteIO(_write.DangerousGetHandle(), (nint)(data + written), (nuint)(buffer.Length - written))); written += count; var status = SDL.GetIOStatus(_write.DangerousGetHandle());
                    if (written == buffer.Length) return;
                    if (status != SDL.IOStatus.NotReady || !_blocking) throw new PipeWriteException(written, status == SDL.IOStatus.NotReady ? "The nonblocking pipe is full; retry the remaining bytes." : "The pipe write failed: " + SDL.GetError());
                }
            }
            Thread.Sleep(1);
        }
    }
    public override void Flush() { ObjectDisposedException.ThrowIf(_closed, this); if (_write is not null) lock (_owner.Gate) if (!SDL.FlushIO(_write.DangerousGetHandle())) throw new IOException(SDL.GetError()); }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("Process pipes cannot seek.");
    public override void SetLength(long value) => throw new NotSupportedException("Process pipes cannot resize.");
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_closed) { lock (_owner.Gate) { _closed = true; try { _write?.Dispose(); } finally { _read.Dispose(); } } }
        base.Dispose(disposing);
    }
}
