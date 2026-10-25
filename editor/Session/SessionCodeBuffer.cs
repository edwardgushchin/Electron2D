namespace Electron2D.Editor;

/// <summary>Bridges one local code view's pending input into the owner before observations or mutations.</summary>
/// <remarks>Some native widgets defer change notifications. The registered bounded reader is polled on the
/// owner thread before operations, so visible manual input cannot be overwritten or omitted from a snapshot.
/// Readers must be synchronous and side-effect-free. This first session supports one bound code ingress.</remarks>
public sealed class SessionCodeBuffer : IDisposable
{
    private readonly ProjectSession _owner;
    private Func<SessionCodeEdit?>? _read;
    private Action<SessionCodeEdit, Exception>? _rejected;
    internal string Origin { get; }
    internal SessionCodeBuffer(ProjectSession owner, string origin, Func<SessionCodeEdit?> read, Action<SessionCodeEdit, Exception>? rejected)
    { _owner = owner; Origin = origin; _read = read; _rejected = rejected; }
    internal SessionCodeEdit? Read() => _read?.Invoke();
    internal void Reject(SessionCodeEdit edit, Exception error) => _rejected?.Invoke(edit, error);
    /// <summary>Publishes pending manual input now; no pending edit is a no-op.</summary>
    /// <exception cref="SessionConflictException">The pending edit overlaps newer source.</exception>
    public void Flush() { ObjectDisposedException.ThrowIf(_read is null, this); _owner.FlushCodeBuffer(); }
    /// <summary>Unregisters this ingress without discarding a committed buffer.</summary>
    public void Dispose() { _owner.UnbindCodeBuffer(this); Release(); }
    internal void Release() { _read = null; _rejected = null; }
}

public sealed partial class ProjectSession
{
    private SessionCodeBuffer? _codeBuffer;
    private bool _drainingCode, _readingCode;
    internal SessionCodeBuffer BindCodeBuffer(string origin, Func<SessionCodeEdit?> read, Action<SessionCodeEdit, Exception>? rejected)
    {
        Check(); ArgumentNullException.ThrowIfNull(read);
        if (_busy || _readingCode || _codeBuffer is not null) throw new InvalidOperationException("The idle session admits one bound code ingress.");
        return _codeBuffer = new(this, origin, read, rejected);
    }
    internal void UnbindCodeBuffer(SessionCodeBuffer buffer)
    {
        if (!_disposed && _thread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Unbind on the session owner thread.");
        if (_readingCode) throw new InvalidOperationException("An input reader cannot mutate its binding.");
        if (ReferenceEquals(_codeBuffer, buffer)) _codeBuffer = null;
    }
    internal void FlushCodeBuffer()
    {
        Check();
        if (_busy || _drainingCode || _readingCode || _codeBuffer is not { } buffer) return;
        _drainingCode = true;
        try
        {
            SessionCodeEdit? edit;
            _readingCode = true;
            try { edit = buffer.Read(); } finally { _readingCode = false; }
            if (edit is null) return;
            try { Apply(buffer.Origin, "Edit source", [edit]); }
            catch (Exception error)
            {
                try { buffer.Reject(edit, error); }
                catch (Exception observer) { throw new AggregateException(error, observer); }
                throw;
            }
        }
        finally { _drainingCode = false; }
    }
}
