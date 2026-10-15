using Electron2D;
using System.Runtime.InteropServices;
using Sdl = SDL3.SDL;
using EOs = Electron2D.OS;

internal static class DiagnosticsNativeTests
{
    private static int _checks, _previousCalls, _foreignCalls, _badData;
    private static readonly Sdl.LogOutputFunction Previous = PreviousLog;
    private static readonly Sdl.LogOutputFunction Foreign = ForeignLog;
    private static void PreviousLog(IntPtr data, Sdl.LogCategory category, Sdl.LogPriority priority, string message) { if (data != (IntPtr)123) Interlocked.Increment(ref _badData); Interlocked.Increment(ref _previousCalls); }
    private static void ForeignLog(IntPtr data, Sdl.LogCategory category, Sdl.LogPriority priority, string message) => Interlocked.Increment(ref _foreignCalls);
    private sealed class Receiver : Logger
    {
        internal int Messages, Errors; internal string Last = ""; internal LoggerErrorType LastType;
        internal Action? NativeAction;
        protected override void OnLogMessage(string message, bool error) { Interlocked.Increment(ref Messages); Last = message; NativeAction?.Invoke(); }
        protected override void OnLogError(string function, string file, int line, string code, string rationale, bool editorNotify, LoggerErrorType errorType, ReadOnlySpan<ScriptBacktrace> traces) { Interlocked.Increment(ref Errors); Last = rationale; LastType = errorType; NativeAction?.Invoke(); }
    }
    private static void Check(bool condition, string message) { _checks++; if (!condition) throw new InvalidOperationException(message); }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference<Sdl.LogOutputFunction> InstallTemporarySink(Receiver receiver)
    {
        var state = new object();
        Sdl.LogOutputFunction output = (data, category, priority, message) => { GC.KeepAlive(state); Interlocked.Increment(ref _previousCalls); };
        Sdl.SetLogOutputFunction(output, (IntPtr)987); EOs.AddLogger(receiver);
        return new(output);
    }
    internal static void Run()
    {
        Sdl.GetLogOutputFunction(out var original, out var originalData);
        var oldPriority = Sdl.GetLogPriority(Sdl.LogCategory.Application);
        var oldOut = Engine.PrintToSTDOUT; var oldErr = Engine.PrintErrorMessages;
        using var receiver = new Receiver();
        try
        {
            Sdl.SetLogPriority(Sdl.LogCategory.Application, Sdl.LogPriority.Trace);
            Sdl.SetLogOutputFunction(Previous, (IntPtr)123); EOs.AddLogger(receiver);
            Sdl.LogMessage(Sdl.LogCategory.Application, Sdl.LogPriority.Info, "native Кириллица 😀");
            Check(receiver.Messages == 1 && receiver.Errors == 0 && receiver.Last == "native Кириллица 😀" && _previousCalls == 1 && _badData == 0, "Native message, UTF8, prior sink and userdata.");
            Engine.PrintToSTDOUT = false; Sdl.LogMessage(Sdl.LogCategory.Application, Sdl.LogPriority.Info, "hidden-info");
            Check(receiver.Messages == 2 && _previousCalls == 1, "Native stdout flag preserves receiver delivery.");
            Engine.PrintErrorMessages = false; Sdl.LogMessage(Sdl.LogCategory.Application, Sdl.LogPriority.Warn, "warning");
            Check(receiver.Errors == 1 && receiver.LastType == LoggerErrorType.Warning && _previousCalls == 1, "Native warning category and output suppression.");
            Sdl.LogMessage(Sdl.LogCategory.Application, Sdl.LogPriority.Error, "error"); Sdl.LogMessage(Sdl.LogCategory.Application, Sdl.LogPriority.Critical, "critical");
            Check(receiver.Errors == 3 && receiver.LastType == LoggerErrorType.Error, "Native error and critical categories.");
            var before = receiver.Messages; Parallel.For(0, 128, i => Sdl.LogMessage(Sdl.LogCategory.Application, Sdl.LogPriority.Info, "concurrent-native"));
            Check(receiver.Messages == before + 128, "Concurrent native delivery.");
            Engine.PrintToSTDOUT = true; Engine.PrintErrorMessages = true; receiver.NativeAction = () => EOs.RemoveLogger(receiver);
            Sdl.LogMessage(Sdl.LogCategory.Application, Sdl.LogPriority.Info, "remove-from-callback");
            Sdl.GetLogOutputFunction(out var restored, out var data);
            Check(Marshal.GetFunctionPointerForDelegate(restored) == Marshal.GetFunctionPointerForDelegate(Previous) && data == (IntPtr)123, "Unregister from native callback restores the exact previous sink.");
            receiver.NativeAction = null; EOs.AddLogger(receiver); Sdl.SetLogOutputFunction(Foreign, (IntPtr)321); EOs.RemoveLogger(receiver);
            Sdl.GetLogOutputFunction(out restored, out data); Check(Marshal.GetFunctionPointerForDelegate(restored) == Marshal.GetFunctionPointerForDelegate(Foreign) && data == (IntPtr)321, "Foreign replacement is preserved during unregistration.");
            Sdl.LogMessage(Sdl.LogCategory.Application, Sdl.LogPriority.Info, "foreign"); Check(_foreignCalls == 1, "Foreign callback remains executable.");
            var weak = InstallTemporarySink(receiver); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            Check(weak.TryGetTarget(out var alive), "A borrowed managed native sink remains rooted while replaced.");
            Sdl.LogMessage(Sdl.LogCategory.Application, Sdl.LogPriority.Info, "retained-sink"); EOs.RemoveLogger(receiver); GC.KeepAlive(alive);
        }
        finally
        {
            EOs.RemoveLogger(receiver); Engine.PrintToSTDOUT = oldOut; Engine.PrintErrorMessages = oldErr;
            Sdl.SetLogOutputFunction(original, originalData); Sdl.SetLogPriority(Sdl.LogCategory.Application, oldPriority);
        }
        Console.WriteLine($"Native diagnostics: {_checks} assertions passed.");
    }
}
