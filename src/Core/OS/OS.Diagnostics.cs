using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Electron2D;

public sealed partial class OS
{
    /// <summary>Reports a fatal error and terminates the current process to test its crash handler.</summary>
    /// <param name="message">The diagnostic rationale. An empty string is allowed.</param>
    /// <remarks>May be called from any thread, including before engine startup. Structured diagnostics
    /// use the normal synchronous logger rules. The runtime then terminates the process without unwinding
    /// caller frames, running normal shutdown handlers, or honoring restart-on-exit.</remarks>
    /// <exception cref="ArgumentNullException">The message is null.</exception>
    [DoesNotReturn]
    public static void Crash(string message) => Service.CrashCore(message);

    [DoesNotReturn]
    private void CrashCore(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        const string reason = "Fatal error requested by OS.Crash.";
        try
        {
            RuntimeDiagnostics.Instance.Error(nameof(Crash), "", 0, reason, message, LoggerErrorType.Error);
        }
        catch (Exception)
        {
            // A diagnostic failure must not escape to caller exception filters.
        }
        Environment.FailFast(message.Length == 0 ? reason : $"{reason} {message}");
    }

    /// <summary>Registers a borrowed logger once by identity, retaining it until removal or disposal.</summary>
    /// <param name="logger">A live diagnostic receiver.</param>
    /// <remarks>Callbacks run synchronously on sending threads and may overlap. Registry changes during a
    /// callback affect later messages. Registering does not initialize video or redirect host Console writers.</remarks>
    /// <exception cref="ArgumentNullException">Logger is null.</exception>
    /// <exception cref="ObjectDisposedException">Logger is disposed.</exception>
    public static void AddLogger(Logger logger) => RuntimeDiagnostics.Instance.Add(logger);

    /// <summary>Removes registration without disposing the logger; repeated removal is harmless.</summary>
    /// <param name="logger">The receiver identity.</param>
    /// <exception cref="ArgumentNullException">Logger is null.</exception>
    public static void RemoveLogger(Logger logger) => RuntimeDiagnostics.Instance.Remove(logger);

    /// <summary>Reports whether verbose stdout diagnostics are enabled.</summary>
    /// <returns>True for a host verbose switch or an active run with verbose diagnostics enabled.</returns>
    /// <remarks>The physical stdout gate is independent of this mode.</remarks>
    public static bool IsSTDOUTVerbose() => RuntimeDiagnostics.Instance.IsSTDOUTVerbose;

    /// <summary>Writes a diagnostic message when verbose stdout diagnostics are enabled.</summary>
    /// <param name="message">The diagnostic message delivered to stdout and registered loggers.</param>
    /// <remarks>Physical output follows Engine.PrintToSTDOUT. A disabled verbose mode emits nothing.</remarks>
    /// <exception cref="ArgumentNullException">The message is null.</exception>
    /// <exception cref="IOException">The host output writer fails.</exception>
    public static void LogVerbose(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (IsSTDOUTVerbose()) LogMessage(message);
    }

    /// <summary>Sends an engine message to registered loggers and its permitted physical output stream.</summary>
    /// <param name="message">Diagnostic text; physical output appends a newline.</param>
    /// <param name="error">Whether to use stderr instead of stdout.</param>
    /// <remarks>Engine output flags affect the console, while callbacks still receive the message. Direct
    /// consumer Console writes remain host-owned output. Recursive log messages are printed without callbacks.
    /// Compose multiple values before sending, for example with string.Concat or string.Join using a space
    /// or tab separator. Empty text is allowed; embedded newlines and trailing whitespace are preserved.
    /// Callbacks receive the original text even when physical output is disabled or its writer fails.</remarks>
    /// <exception cref="ArgumentNullException">Message is null.</exception>
    /// <exception cref="IOException">The host output writer fails.</exception>
    public static void LogMessage(string message, bool error = false) => RuntimeDiagnostics.Instance.Message(message, error);

    /// <summary>Writes diagnostic text to stdout without appending a newline.</summary>
    /// <param name="message">The exact text to write and deliver to registered loggers.</param>
    /// <remarks>The physical write follows <see cref="Engine.PrintToSTDOUT"/>. Registered loggers
    /// receive the original text even if physical output is disabled or fails.</remarks>
    /// <exception cref="ArgumentNullException">The message is null.</exception>
    /// <exception cref="IOException">The host output writer fails.</exception>
    public static void LogRaw(string message) =>
        RuntimeDiagnostics.Instance.Message(message, error: false, format: DiagnosticTextFormat.Raw);

    /// <summary>Writes a message with supported terminal markup and appends a newline.</summary>
    /// <param name="message">Text with optional style, color or layout tags.</param>
    /// <remarks>ANSI-capable output uses terminal styles. Other output retains text and layout.
    /// Registered loggers receive the original markup regardless of terminal capability,
    /// <see cref="Engine.PrintToSTDOUT"/>, or physical writer failure.</remarks>
    /// <exception cref="ArgumentNullException">The message is null.</exception>
    /// <exception cref="IOException">The host output writer fails.</exception>
    public static void LogRich(string message) =>
        RuntimeDiagnostics.Instance.Message(message, error: false, format: DiagnosticTextFormat.Rich);

    /// <summary>Publishes a structured engine diagnostic with available C# stack metadata.</summary>
    /// <param name="rationale">Human-readable diagnostic.</param>
    /// <param name="errorType">Diagnostic classification.</param>
    /// <param name="code">Optional failing expression or error identity.</param>
    /// <param name="function">Origin function, supplied by the compiler by default.</param>
    /// <param name="file">Origin file, supplied by the compiler by default.</param>
    /// <param name="line">Origin line, supplied by the compiler by default.</param>
    /// <remarks>Use Error for an error and Warning for a warning. Reporting alone does not throw or stop
    /// execution. Physical stderr includes classification and available origin, followed by a newline;
    /// callbacks receive separate text and metadata fields even when physical output is disabled or fails.
    /// Caller attributes describe the C# call site; wrappers may explicitly forward their own caller metadata.</remarks>
    /// <exception cref="ArgumentNullException">A text argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Type is unknown or line is negative.</exception>
    /// <exception cref="IOException">The host error writer fails.</exception>
    public static void LogError(string rationale, LoggerErrorType errorType = LoggerErrorType.Error, string code = "",
        [CallerMemberName] string function = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) =>
        RuntimeDiagnostics.Instance.Error(function, file, line, code, rationale, errorType);
}
