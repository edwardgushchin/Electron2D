using System.Runtime.CompilerServices;

namespace Electron2D;

public sealed partial class OS
{
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

    /// <summary>Sends an engine message to registered loggers and its permitted physical output stream.</summary>
    /// <param name="message">Diagnostic text; physical output appends a newline.</param>
    /// <param name="error">Whether to use stderr instead of stdout.</param>
    /// <remarks>Engine output flags affect the console, while callbacks still receive the message. Direct
    /// consumer Console writes remain host-owned output. Recursive log messages are printed without callbacks.</remarks>
    /// <exception cref="ArgumentNullException">Message is null.</exception>
    /// <exception cref="IOException">The host output writer fails.</exception>
    public static void LogMessage(string message, bool error = false) => RuntimeDiagnostics.Instance.Message(message, error);

    /// <summary>Publishes a structured engine diagnostic with available C# stack metadata.</summary>
    /// <param name="rationale">Human-readable diagnostic.</param>
    /// <param name="errorType">Diagnostic classification.</param>
    /// <param name="code">Optional failing expression or error identity.</param>
    /// <param name="function">Origin function, supplied by the compiler by default.</param>
    /// <param name="file">Origin file, supplied by the compiler by default.</param>
    /// <param name="line">Origin line, supplied by the compiler by default.</param>
    /// <exception cref="ArgumentNullException">A text argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Type is unknown or line is negative.</exception>
    /// <exception cref="IOException">The host error writer fails.</exception>
    public static void LogError(string rationale, LoggerErrorType errorType = LoggerErrorType.Error, string code = "",
        [CallerMemberName] string function = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) =>
        RuntimeDiagnostics.Instance.Error(function, file, line, code, rationale, errorType);
}
