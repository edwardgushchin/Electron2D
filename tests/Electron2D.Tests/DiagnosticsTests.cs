using Electron2D;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using EOs = Electron2D.OS;

internal static class DiagnosticsTests
{
    private static int _checks;
    private static void Check(bool condition, string text) { _checks++; if (!condition) throw new InvalidOperationException(text); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private sealed record Error(string Function, string File, int Line, string Code, string Text, bool Editor, LoggerErrorType Type, ScriptBacktrace[] Traces);
    private sealed class Receiver : Logger
    {
        internal readonly ConcurrentQueue<(string Text, bool Error)> Messages = new();
        internal readonly ConcurrentQueue<Error> Errors = new();
        internal Action<string, bool>? MessageAction;
        internal Action? ErrorAction;
        protected override void OnLogMessage(string message, bool error) { Messages.Enqueue((message, error)); MessageAction?.Invoke(message, error); }
        protected override void OnLogError(string function, string file, int line, string code, string rationale, bool editorNotify, LoggerErrorType errorType, ReadOnlySpan<ScriptBacktrace> scriptBacktraces)
        { Errors.Enqueue(new(function, file, line, code, rationale, editorNotify, errorType, scriptBacktraces.ToArray())); ErrorAction?.Invoke(); }
    }
    private sealed class Loop : MainLoop
    {
        internal readonly Exception Failure = new InvalidOperationException("gameplay failure");
        internal bool Throw = true;
        [MethodImpl(MethodImplOptions.NoInlining)] protected override bool OnProcess(double delta) { if (Throw) throw Failure; return false; }
    }
    [MethodImpl(MethodImplOptions.NoInlining)] private static ScriptBacktrace CaptureAtKnownFunction() => Engine.CaptureScriptBacktraces().Single();

    internal static void RunPortable()
    {
        using var receiver = new Receiver(); var output = Engine.PrintToSTDOUT; var errors = Engine.PrintErrorMessages;
        try
        {
            EOs.AddLogger(receiver); Engine.PrintToSTDOUT = false; Engine.PrintErrorMessages = false;
            EOs.LogMessage("managed-only"); EOs.LogError("managed-only error");
            Check(receiver.Messages.Count == 1 && receiver.Errors.Count == 1 && !CaptureAtKnownFunction().IsEmpty(), "Managed-only registration, message, error and snapshot.");
            using var node = new Node { Name = "NoNativeHost" }; node.PrintTree(); Check(receiver.Messages.Count == 2, "Managed-only scene diagnostic.");
        }
        finally { EOs.RemoveLogger(receiver); Engine.PrintToSTDOUT = output; Engine.PrintErrorMessages = errors; }
        Console.WriteLine("Managed-only diagnostics without a platform package passed.");
    }
    internal static void Run()
    {
        var stdout = Console.Out; var stderr = Console.Error; var outFlag = Engine.PrintToSTDOUT; var errFlag = Engine.PrintErrorMessages;
        using var output = new StringWriter(); using var errors = new StringWriter(); Console.SetOut(output); Console.SetError(errors); var installedOut = Console.Out; var installedError = Console.Error;
        using var receiver = new Receiver();
        try
        {
            Check(Engine.PrintToSTDOUT && Engine.PrintErrorMessages, "Default diagnostic output flags.");
            Check((int)LoggerErrorType.Error == 0 && (int)LoggerErrorType.Warning == 1 && (int)LoggerErrorType.Script == 2 && (int)LoggerErrorType.Shader == 3, "Exact diagnostic categories.");
            var owner = Engine.GetSingleton<Engine>("Engine");
            Check(owner.GetPropertyList().Any(p => p.Name == nameof(Engine.PrintToSTDOUT)) && owner.GetPropertyList().Any(p => p.Name == nameof(Engine.PrintErrorMessages)), "Retained Engine descriptor family.");
            EOs.AddLogger(receiver); EOs.AddLogger(receiver); EOs.LogMessage("ordinary"); EOs.LogMessage("error-stream", true);
            Check(receiver.Messages.Count == 2 && receiver.Messages.First() == ("ordinary", false) && output.ToString().Contains("ordinary") && errors.ToString().Contains("error-stream"), "Identity registration, messages and both physical outputs.");
            Check(ReferenceEquals(Console.Out, installedOut) && ReferenceEquals(Console.Error, installedError), "Host console remains usable.");
            output.GetStringBuilder().Clear(); errors.GetStringBuilder().Clear(); Engine.PrintToSTDOUT = false; Engine.PrintErrorMessages = false;
            EOs.LogMessage("suppressed"); EOs.LogMessage("suppressed-error", true); EOs.LogError("still observable");
            Check(output.ToString() == "" && errors.ToString() == "" && receiver.Messages.Count == 4 && receiver.Errors.Count == 1, "Output flags suppress physical output, not receivers.");
            Console.WriteLine("host output"); Check(output.ToString().Contains("host output"), "Direct host Console is outside the engine stream.");
            Engine.PrintToSTDOUT = true; Engine.PrintErrorMessages = true;
            foreach (var type in Enum.GetValues<LoggerErrorType>()) EOs.LogError("typed error " + type, type, "code", "known", "source.cs", 42);
            var detail = receiver.Errors.Last(); Check(detail.Function == "known" && detail.File == "source.cs" && detail.Line == 42 && detail.Code == "code" && !detail.Editor && detail.Type == LoggerErrorType.Shader, "Complete structured payload.");
            Check(detail.Traces.Length == 1 && detail.Traces[0].GetLanguageName() == "C#", "Immutable C# stack in error payload.");
            var trace = CaptureAtKnownFunction(); Check(!trace.IsEmpty() && trace.GetFrameCount() > 0, "Live call-stack capture.");
            Check(Enumerable.Range(0, trace.GetFrameCount()).Any(i => trace.GetFrameFunction(i).Contains(nameof(CaptureAtKnownFunction))), "Known caller identity survives capture. Snapshot=" + trace.Format() + " Raw=" + new System.Diagnostics.StackTrace(true));
            for (var i = 0; i < trace.GetFrameCount(); i++) Check(trace.GetFrameFile(i) is not null && trace.GetFrameLine(i) >= 0 && trace.GetFrameFunction(i).Length > 0, "Every frame has valid metadata.");
            Check(trace.Format() == trace.ToString() && trace.Format(2, 3).StartsWith("  C# backtrace:\n     0: "), "Stable formatting and independent indentation.");
            Reject<ArgumentOutOfRangeException>(() => trace.GetFrameFunction(-1)); Reject<ArgumentOutOfRangeException>(() => trace.GetFrameFile(trace.GetFrameCount())); Reject<ArgumentOutOfRangeException>(() => trace.GetFrameLine(int.MaxValue));
            Reject<ArgumentOutOfRangeException>(() => trace.Format(-1)); Reject<ArgumentOutOfRangeException>(() => trace.Format(indentFrames: 1025));
            Reject<ArgumentNullException>(() => EOs.AddLogger(null!)); Reject<ArgumentNullException>(() => EOs.RemoveLogger(null!)); Reject<ArgumentNullException>(() => EOs.LogMessage(null!));
            Reject<ArgumentNullException>(() => EOs.LogError(null!)); Reject<ArgumentOutOfRangeException>(() => EOs.LogError("bad type", (LoggerErrorType)4)); Reject<ArgumentOutOfRangeException>(() => EOs.LogError("bad line", line: -1));
            using (var node = new Node { Name = "LoggedRoot" }) { var count = receiver.Messages.Count; node.PrintTree(); node.PrintTreePretty(); Check(receiver.Messages.Count == count + 2, "Node tree diagnostics share the engine stream."); }
            ReentryAndMutation();
            var before = receiver.Messages.Count; Parallel.For(0, 128, i => EOs.LogMessage("parallel " + i)); Check(receiver.Messages.Count == before + 128, "Concurrent delivery without lost messages.");
            using (var loop = new Loop())
            {
                Engine.Start(loop); var count = receiver.Errors.Count; Exception? caught = null;
                try { Engine.AdvanceFrame(0); } catch (Exception failure) { caught = failure; }
                Check(ReferenceEquals(caught, loop.Failure) && ReferenceEquals(Engine.MainLoop, loop), "Original callback exception and running owner state survive diagnostics.");
                Check(receiver.Errors.Count == count + 1 && receiver.Errors.Last().Code.Contains(nameof(InvalidOperationException)) && receiver.Errors.Last().Function.Contains("OnProcess") && receiver.Errors.Last().Traces.Length == 1, "Manual frame failure publishes once.");
                loop.Throw = false; Engine.AdvanceFrame(0); Engine.Stop(); Check(Engine.MainLoop is null, "Recovery and shutdown remain executable.");
            }
            var oldTrace = trace.Format(); EOs.RemoveLogger(receiver); EOs.RemoveLogger(receiver); var last = receiver.Messages.Count; EOs.LogMessage("removed"); Check(receiver.Messages.Count == last && trace.Format() == oldTrace, "Removal preserves snapshots and releases delivery.");
            using var disposed = new Receiver(); EOs.AddLogger(disposed); disposed.Dispose(); Reject<ObjectDisposedException>(() => EOs.AddLogger(disposed)); EOs.RemoveLogger(disposed);
        }
        finally
        {
            EOs.RemoveLogger(receiver); Engine.PrintToSTDOUT = outFlag; Engine.PrintErrorMessages = errFlag; Console.SetOut(stdout); Console.SetError(stderr);
        }
        Console.WriteLine($"Runtime diagnostics: {_checks} assertions passed.");
    }
    private static void ReentryAndMutation()
    {
        using var first = new Receiver(); using var second = new Receiver();
        first.MessageAction = (_, _) => { EOs.RemoveLogger(second); EOs.LogMessage("nested message"); EOs.LogError("nested error"); };
        EOs.AddLogger(first); EOs.AddLogger(second); EOs.LogMessage("outer");
        Check(first.Messages.Count == 1 && first.Errors.IsEmpty && second.Messages.Count == 1 && second.Errors.IsEmpty, "Recursive output does not recursively dispatch; current snapshot retains removed identity.");
        EOs.LogMessage("next"); Check(first.Messages.Count == 2 && second.Messages.Count == 1, "Registry changes affect later sends.");
        EOs.RemoveLogger(first); first.MessageAction = (_, _) => throw new InvalidOperationException("throwing receiver"); first.ErrorAction = () => throw new InvalidOperationException("throwing error receiver");
        EOs.AddLogger(first); EOs.AddLogger(second); EOs.LogMessage("receiver failure"); EOs.LogError("receiver error failure");
        Check(second.Messages.Count == 2 && second.Errors.Count == 1, "A throwing receiver does not interrupt following receivers or the engine.");
        EOs.RemoveLogger(first); EOs.RemoveLogger(second);
    }
}
