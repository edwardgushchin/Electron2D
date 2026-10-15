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
    private bool _stdout = true, _errors = true;
    [ThreadStatic] private static int _dispatchDepth;
    private sealed record NativeSink(nint Output, nint Data, SDL3.SDL.LogOutputFunction? Root);
    private nint _nativeHandle;
    private NativeSink? _previousSink;
    private bool _nativeInstalled;
    private delegate* unmanaged[Cdecl]<nint*, nint*, void> _getOutput;
    private delegate* unmanaged[Cdecl]<nint, nint, void> _setOutput;
    private static nint NativeCallback => (nint)(delegate* unmanaged[Cdecl]<nint, int, int, byte*, void>)&NativeLog;

    internal bool PrintToSTDOUT { get => Volatile.Read(ref _stdout); set => SetOutput(value, error: false); }
    internal bool PrintErrorMessages { get => Volatile.Read(ref _errors); set => SetOutput(value, error: true); }
    private void SetOutput(bool value, bool error)
    {
        lock (_gate)
        {
            var previous = error ? _errors : _stdout;
            if (error) Volatile.Write(ref _errors, value); else Volatile.Write(ref _stdout, value);
            try { UpdateNative(); }
            catch { if (error) Volatile.Write(ref _errors, previous); else Volatile.Write(ref _stdout, previous); throw; }
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

    internal void Message(string message, bool error)
    {
        ArgumentNullException.ThrowIfNull(message);
        var nested = _dispatchDepth++ != 0;
        try
        {
            var targets = Volatile.Read(ref _loggers);
            Exception? outputFailure = null;
            try { if (error ? PrintErrorMessages : PrintToSTDOUT) (error ? Console.Error : Console.Out).WriteLine(message); }
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
            try { if (PrintErrorMessages) Console.Error.WriteLine($"{type}: {rationale}" + (function.Length == 0 ? "" : $" [{file}:{line} @ {function}]")); }
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
        try { if (PrintErrorMessages) Console.Error.WriteLine("Diagnostic receiver failed: " + error.Message); }
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
        var wanted = _loggers.Length != 0 || !_stdout || !_errors;
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
