namespace Electron2D;

/// <summary>Receives messages and structured errors from the engine diagnostic stream.</summary>
/// <remarks>Register through OS.AddLogger. Callbacks execute synchronously on sending threads and may run
/// concurrently. Reentrant logging reaches the console without invoking loggers again. Disposal unregisters
/// this instance. Registration retains the instance; callers own its lifetime.</remarks>
public class Logger : ElectronObject
{
    /// <summary>Creates an unregistered diagnostic receiver.</summary>
    public Logger() { }

    /// <summary>Receives an ordinary diagnostic message.</summary>
    /// <param name="message">The message without an automatically appended newline.</param>
    /// <param name="error">Whether the message belongs to the error output stream.</param>
    protected virtual void OnLogMessage(string message, bool error) { }

    /// <summary>Receives a structured diagnostic and immutable stack snapshots.</summary>
    /// <param name="function">Origin function, or an empty string when unavailable.</param>
    /// <param name="file">Origin source file, or an empty string when unavailable.</param>
    /// <param name="line">Source line, or zero when unavailable.</param>
    /// <param name="code">The failing expression or exception type, when available.</param>
    /// <param name="rationale">Human-readable diagnostic text.</param>
    /// <param name="editorNotify">Whether an editor requested notification; false for the runtime host.</param>
    /// <param name="errorType">Error classification.</param>
    /// <param name="scriptBacktraces">Borrowed read-only snapshot references; immutable objects may be retained.</param>
    protected virtual void OnLogError(string function, string file, int line, string code, string rationale,
        bool editorNotify, LoggerErrorType errorType, ReadOnlySpan<ScriptBacktrace> scriptBacktraces)
    { }

    internal void DeliverMessage(string message, bool error) { if (!IsDisposed) OnLogMessage(message, error); }
    internal void DeliverError(string function, string file, int line, string code, string rationale,
        LoggerErrorType type, ReadOnlySpan<ScriptBacktrace> traces)
    { if (!IsDisposed) OnLogError(function, file, line, code, rationale, false, type, traces); }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        try { RuntimeDiagnostics.Instance.Remove(this); }
        finally { base.Dispose(disposing); }
    }
}
