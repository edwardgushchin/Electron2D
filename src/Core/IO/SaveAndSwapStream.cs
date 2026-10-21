using System.Runtime.ExceptionServices;
using CryptographicRandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator;
using SDL3;

namespace Electron2D;

internal sealed class SaveAndSwapStream : Stream
{
    private readonly FileStream _file;
    private readonly string _target;
    private bool _failed, _cancelled, _flushed, _closed;
    internal string PhysicalPath => _file.Name;
    private SaveAndSwapStream(string temporary, string target)
    {
        _file = new FileStream(temporary, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None);
        _target = target;
    }
    internal static SaveAndSwapStream Open(string path)
    {
        var info = new FileInfo(path);
        var target = info.LinkTarget is null ? path : info.ResolveLinkTarget(true)!.FullName;
        var exists = SDL.GetPathInfo(target, out var status);
        if (exists && status.Type != SDL.PathType.File) throw new IOException("Save-and-swap requires a regular file destination.");
        if (!exists)
        {
            try { _ = File.GetAttributes(target); throw new IOException("Cannot inspect the save destination: " + SDL.GetError()); }
            catch (FileNotFoundException) { }
        }
        var directory = System.IO.Path.GetDirectoryName(target)!;
        var temporary = System.IO.Path.Combine(directory, System.IO.Path.GetFileName(target) + "-" + CryptographicRandomNumberGenerator.GetHexString(16) + ".tmp");
        var result = new SaveAndSwapStream(temporary, target);
        try
        {
            if (!OperatingSystem.IsWindows() && !OperatingSystem.IsBrowser())
                File.SetUnixFileMode(temporary, exists ? File.GetUnixFileMode(target) : (UnixFileMode)420);
            return result;
        }
        catch (Exception error)
        {
            result.Cancel();
            try { result.Dispose(); } catch (Exception cleanup) { throw new AggregateException(error, cleanup); }
            throw;
        }
    }
    internal void Cancel() => _cancelled = true;
    public override bool CanRead => _file.CanRead;
    public override bool CanSeek => _file.CanSeek;
    public override bool CanWrite => !_cancelled && !_failed && _file.CanWrite;
    public override long Length => _file.Length;
    public override long Position { get => _file.Position; set => _file.Position = value; }
    public override int Read(byte[] buffer, int offset, int count) => _file.Read(buffer, offset, count);
    public override int Read(Span<byte> buffer) => _file.Read(buffer);
    public override long Seek(long offset, SeekOrigin origin) => _file.Seek(offset, origin);
    private void CheckWrite()
    {
        ObjectDisposedException.ThrowIf(_closed, this);
        if (_failed || _cancelled) throw new IOException("The staged save was aborted.");
    }
    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        CheckWrite(); _flushed = false;
        try { _file.Write(buffer); } catch { _failed = true; throw; }
    }
    public override void WriteByte(byte value)
    {
        CheckWrite(); _flushed = false;
        try { _file.WriteByte(value); } catch { _failed = true; throw; }
    }
    public override void SetLength(long value)
    {
        CheckWrite(); _flushed = false;
        try { _file.SetLength(value); } catch { _failed = true; throw; }
    }
    public override void Flush()
    {
        if (_cancelled) return;
        CheckWrite();
        try { _file.Flush(true); _flushed = true; } catch { _failed = true; throw; }
    }
    protected override void Dispose(bool disposing)
    {
        if (!disposing || _closed) { base.Dispose(disposing); return; }
        _closed = true; Exception? failure = null;
        try
        {
            if (!_failed && !_cancelled && !_flushed)
            {
                try { _file.Flush(true); } catch (Exception error) { _failed = true; failure = error; }
            }
            try { _file.Dispose(); } catch (Exception error) { _failed = true; failure = Combine(failure, error); }
            if (_failed || _cancelled)
            {
                try { File.Delete(PhysicalPath); } catch (Exception error) { failure = Combine(failure, error); }
            }
            else Commit();
            if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
        }
        finally { base.Dispose(disposing); }
    }
    private static Exception Combine(Exception? first, Exception next) => first is null ? next : new AggregateException(first, next);
    private void Commit()
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                if (OperatingSystem.IsWindows() && File.Exists(_target))
                {
                    try { File.Replace(PhysicalPath, _target, null, ignoreMetadataErrors: true); }
                    catch (FileNotFoundException) when (!File.Exists(_target)) { File.Move(PhysicalPath, _target, overwrite: true); }
                }
                else File.Move(PhysicalPath, _target, overwrite: true);
                return;
            }
            catch (Exception error) when (OperatingSystem.IsWindows() && attempt < 999 && error is IOException or UnauthorizedAccessException)
            { Thread.Sleep(1); }
        }
    }
}
