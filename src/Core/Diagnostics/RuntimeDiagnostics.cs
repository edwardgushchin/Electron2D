using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe class RuntimeDiagnostics
{
    internal static readonly RuntimeDiagnostics Instance = new();
    private readonly object _gate = new();
    private Logger[] _loggers = [];
    private readonly bool _commandLineVerbose = HasSwitch(Environment.GetCommandLineArgs(), "--verbose", "-v");
    private bool _stdout = true, _errors = true, _projectVerbose;
    private sealed record RunOutput(bool Stdout, bool Stderr, bool FlushStdout);
    private RunOutput? _runOutput;

    internal bool IsSTDOUTVerbose => _commandLineVerbose || Volatile.Read(ref _projectVerbose);

    private static bool HasSwitch(ReadOnlySpan<string> arguments, string option, string? alias = null)
    {
        for (var i = 1; i < arguments.Length; i++)
        {
            var argument = arguments[i];
            if (argument is "--" or "++") break;
            if (argument == option || argument == alias) return true;
        }
        return false;
    }

    internal void BeginRun(bool windowed = false)
    {
        var arguments = Environment.GetCommandLineArgs();
        var disableStdout = ProjectSettings.GetWithOverride(ProjectSettings.DisableSTDOUT) || HasSwitch(arguments, "--quiet", "-q");
        var disableStderr = ProjectSettings.GetWithOverride(ProjectSettings.DisableSTDERR);
        var flushStdout = ProjectSettings.GetWithOverride(ProjectSettings.FlushSTDOUTOnPrint);
        var printHeader = windowed && ProjectSettings.GetWithOverride(ProjectSettings.PrintHeader) && !HasSwitch(arguments, "--no-header");
        var verbose = ProjectSettings.GetWithOverride(ProjectSettings.VerboseSTDOUT);
        lock (_gate)
        {
            Volatile.Write(ref _runOutput, new(_stdout && !disableStdout, _errors && !disableStderr, flushStdout));
            try { UpdateNative(); }
            catch { Volatile.Write(ref _runOutput, null); throw; }
            Volatile.Write(ref _projectVerbose, verbose);
        }
        if (printHeader && PrintToSTDOUT)
            ReportMessage($"Electron2D v{Engine.VersionInfo.InformationalVersion}");
    }

    internal void EndRun()
    {
        lock (_gate)
        {
            Volatile.Write(ref _projectVerbose, false);
            if (_runOutput is null) return;
            Volatile.Write(ref _runOutput, null);
            UpdateNative();
        }
    }

    internal void ReportResourceRequest(string path)
    {
        if (!IsSTDOUTVerbose || _dispatchDepth != 0 || string.IsNullOrEmpty(path)) return;
        ReportMessage($"Resource load requested: {path}");
    }

    internal void ReportMessage(string message)
    {
        try { Message(message, error: false); }
        catch (Exception failure) { CallbackFailure(failure); }
    }

    [ThreadStatic] private static int _dispatchDepth;
    private sealed record NativeSink(nint Output, nint Data, SDL3.SDL.LogOutputFunction? Root);
    private nint _nativeHandle;
    private NativeSink? _previousSink;
    private bool _nativeInstalled;
    private delegate* unmanaged[Cdecl]<nint*, nint*, void> _getOutput;
    private delegate* unmanaged[Cdecl]<nint, nint, void> _setOutput;
    private static nint NativeCallback => (nint)(delegate* unmanaged[Cdecl]<nint, int, int, byte*, void>)&NativeLog;

    internal bool PrintToSTDOUT { get => Volatile.Read(ref _runOutput)?.Stdout ?? Volatile.Read(ref _stdout); set => SetOutput(value, error: false); }
    internal bool PrintErrorMessages { get => Volatile.Read(ref _runOutput)?.Stderr ?? Volatile.Read(ref _errors); set => SetOutput(value, error: true); }
    private void SetOutput(bool value, bool error)
    {
        lock (_gate)
        {
            var previous = error ? _errors : _stdout;
            var previousRun = _runOutput;
            if (error) Volatile.Write(ref _errors, value); else Volatile.Write(ref _stdout, value);
            if (previousRun is not null)
                Volatile.Write(ref _runOutput, error ? previousRun with { Stderr = value } : previousRun with { Stdout = value });
            try { UpdateNative(); }
            catch
            {
                if (error) Volatile.Write(ref _errors, previous); else Volatile.Write(ref _stdout, previous);
                Volatile.Write(ref _runOutput, previousRun);
                throw;
            }
        }
    }

    internal void Add(Logger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(logger.IsDisposed, logger);
            if (_loggers.Any(item => ReferenceEquals(item, logger))) return;
            var previous = _loggers;
            Volatile.Write(ref _loggers, [.. previous, logger]);
            try { UpdateNative(); }
            catch { Volatile.Write(ref _loggers, previous); throw; }
        }
    }
    internal void Remove(Logger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        lock (_gate)
        {
            if (!_loggers.Any(item => ReferenceEquals(item, logger))) return;
            var previous = _loggers;
            Volatile.Write(ref _loggers, previous.Where(item => !ReferenceEquals(item, logger)).ToArray());
            try { UpdateNative(); }
            catch { Volatile.Write(ref _loggers, previous); throw; }
        }
    }

    private void WriteConsole(string message, bool error)
    {
        var run = Volatile.Read(ref _runOutput);
        var enabled = error ? run?.Stderr ?? Volatile.Read(ref _errors) : run?.Stdout ?? Volatile.Read(ref _stdout);
        if (!enabled) return;
        var writer = error ? Console.Error : Console.Out;
        writer.WriteLine(message);
        if (error || run?.FlushStdout == true) writer.Flush();
    }

    internal void Message(string message, bool error)
    {
        ArgumentNullException.ThrowIfNull(message);
        var nested = _dispatchDepth++ != 0;
        try
        {
            var targets = Volatile.Read(ref _loggers);
            Exception? outputFailure = null;
            try { WriteConsole(message, error); }
            catch (Exception failure) { outputFailure = failure; }
            if (!nested)
                foreach (var logger in targets)
                    try { logger.DeliverMessage(message, error); }
                    catch (Exception failure) { CallbackFailure(failure); }
            if (outputFailure is not null) ExceptionDispatchInfo.Capture(outputFailure).Throw();
        }
        finally { _dispatchDepth--; }
    }

    internal void Error(string function, string file, int line, string code, string rationale, LoggerErrorType type, Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(function); ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(code); ArgumentNullException.ThrowIfNull(rationale);
        ArgumentOutOfRangeException.ThrowIfNegative(line);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        var nested = _dispatchDepth++ != 0;
        try
        {
            var targets = Volatile.Read(ref _loggers);
            ScriptBacktrace[] traces = [];
            if (!nested && (targets.Length != 0 || exception is not null && PrintErrorMessages))
            {
                var trace = ScriptBacktrace.Capture(exception is null ? new StackTrace(2, true) : new StackTrace(exception, true));
                traces = [trace];
                if (exception is not null && !trace.IsEmpty())
                {
                    if (function.Length == 0) function = trace.GetFrameFunction(0);
                    if (file.Length == 0) file = trace.GetFrameFile(0);
                    if (line == 0) line = trace.GetFrameLine(0);
                }
            }
            Exception? outputFailure = null;
            try { if (PrintErrorMessages) WriteConsole($"{type}: {rationale}" + (function.Length == 0 ? "" : $" [{file}:{line} @ {function}]"), error: true); }
            catch (Exception failure) { outputFailure = failure; }
            if (!nested && targets.Length != 0)
            {
                foreach (var logger in targets)
                    try { logger.DeliverError(function, file, line, code, rationale, type, traces); }
                    catch (Exception failure) { CallbackFailure(failure); }
            }
            if (outputFailure is not null) ExceptionDispatchInfo.Capture(outputFailure).Throw();
        }
        finally { _dispatchDepth--; }
    }

    internal void ReportException(Exception exception)
    {
        try { Error("", "", 0, exception.GetType().FullName ?? "Exception", exception.Message, LoggerErrorType.Script, exception); }
        catch (Exception failure) { CallbackFailure(failure); }
    }
    private void CallbackFailure(Exception error)
    {
        try { WriteConsole("Diagnostic receiver failed: " + error.Message, error: true); }
        catch { }
    }

    internal void SDLAvailable(nint handle)
    {
        lock (_gate)
        {
            _nativeHandle = handle;
            UpdateNative();
        }
    }
    private void UpdateNative()
    {
        if (_nativeHandle == 0) return;
        var wanted = _loggers.Length != 0 || !PrintToSTDOUT || !PrintErrorMessages;
        if (wanted && !_nativeInstalled)
        {
            _getOutput = (delegate* unmanaged[Cdecl]<nint*, nint*, void>)NativeLibrary.GetExport(_nativeHandle, "SDL_GetLogOutputFunction");
            _setOutput = (delegate* unmanaged[Cdecl]<nint, nint, void>)NativeLibrary.GetExport(_nativeHandle, "SDL_SetLogOutputFunction");
            nint output = 0, data = 0; _getOutput(&output, &data);
            var root = output == 0 ? null : Marshal.GetDelegateForFunctionPointer<SDL3.SDL.LogOutputFunction>(output);
            Volatile.Write(ref _previousSink, new NativeSink(output, data, root));
            _setOutput(NativeCallback, 0); _nativeInstalled = true;
        }
        else if (!wanted && _nativeInstalled)
        {
            nint output = 0, data = 0; _getOutput(&output, &data);
            if (output == NativeCallback && data == 0 && _previousSink is { } sink) _setOutput(sink.Output, sink.Data);
            _nativeInstalled = false;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void NativeLog(nint userData, int category, int priority, byte* message)
    {
        try { Instance.ReceiveNative(category, priority, message); }
        catch { }
    }
    private void ReceiveNative(int category, int priority, byte* message)
    {
        var nested = _dispatchDepth++ != 0;
        try
        {
            var error = priority >= (int)SDL3.SDL.LogPriority.Warn;
            var sink = Volatile.Read(ref _previousSink);
            if ((error ? PrintErrorMessages : PrintToSTDOUT) && sink is { Output: not 0 } && sink.Output != NativeCallback)
            {
                ((delegate* unmanaged[Cdecl]<nint, int, int, byte*, void>)sink.Output)(sink.Data, category, priority, message);
                GC.KeepAlive(sink.Root);
            }
            var targets = Volatile.Read(ref _loggers);
            if (nested || targets.Length == 0) return;
            var text = Marshal.PtrToStringUTF8((nint)message) ?? "";
            foreach (var logger in targets)
            {
                try
                {
                    if (error) logger.DeliverError("", "", 0, "", text, priority == (int)SDL3.SDL.LogPriority.Warn ? LoggerErrorType.Warning : LoggerErrorType.Error, []);
                    else logger.DeliverMessage(text, false);
                }
                catch (Exception failure) { CallbackFailure(failure); }
            }
        }
        finally { _dispatchDepth--; }
    }
}
