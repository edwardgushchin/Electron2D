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

    private sealed class VerboseLoop(Action initialize, Action? finalize = null) : MainLoop
    {
        protected override void OnInitialize() => initialize();
        protected override void OnFinalize() => finalize?.Invoke();
    }

    internal static void RunVerbose(string[] arguments)
    {
        var switchEnabled = arguments[0] == "enabled";
        using var receiver = new Receiver();
        var stdout = Console.Out;
        var previousOutput = Engine.PrintToSTDOUT;
        var setting = ProjectSettings.VerboseSTDOUT;
        var directory = Directory.CreateTempSubdirectory("electron2d-verbose-");
        var path = System.IO.Path.Combine(directory.FullName, "test.e2dres");
        var loadMessage = "Resource load requested: " + path;
        using var output = new StringWriter();
        EOs.AddLogger(receiver);
        try
        {
            Console.SetOut(output);
            Engine.PrintToSTDOUT = true;
            Check(!ProjectSettings.Get(setting) && EOs.IsSTDOUTVerbose() == switchEnabled, "The default and process switches are distinct.");
            EOs.LogVerbose("before run");
            Check(receiver.Messages.Contains(("before run", false)) == switchEnabled &&
                output.ToString().Contains("before run", StringComparison.Ordinal) == switchEnabled, "Host switches gate the actual message and stdout.");
            Reject<ArgumentNullException>(() => EOs.LogVerbose(null!));

            var user = directory.CreateSubdirectory("user").FullName;
            using (var saved = new ProjectSettingsRegistry(directory.FullName, user))
            {
                Check(!saved.Get(setting), "The typed definition is registered with its false default.");
                saved.Set(setting, true);
                saved.Save();
            }
            using (var loaded = new ProjectSettingsRegistry(directory.FullName, user))
            {
                loaded.Load();
                Check(loaded.Get(setting), "The verbose setting survives a project-file round trip.");
            }

            using (var resource = new NavigationPolygon()) ResourceSaver.Save(resource, path);
            ProjectSettings.SetFeatureOverride(setting, "verbose_fixture", true);
            ProjectSettings.AddCustomFeature("verbose_fixture");
            using (var loop = new VerboseLoop(() =>
            {
                Check(EOs.IsSTDOUTVerbose(), "The selected override is visible inside initialization.");
                EOs.LogVerbose("initialization");
            }, () => Check(EOs.IsSTDOUTVerbose(), "The run's setting stays active during finalization.")))
            {
                Engine.Start(loop);
                try
                {
                    ProjectSettings.ClearFeatureOverride(setting, "verbose_fixture");
                    Check(EOs.IsSTDOUTVerbose(), "A setting change takes effect on the next run.");
                    Engine.PrintToSTDOUT = false;
                    EOs.LogVerbose("suppressed physical output");
                    Check(receiver.Messages.Contains(("suppressed physical output", false)) &&
                        !output.ToString().Contains("suppressed physical output", StringComparison.Ordinal), "Physical suppression still delivers verbose callbacks.");

                    var nested = 0;
                    receiver.MessageAction = (message, _) =>
                    {
                        if (message != loadMessage || Interlocked.Increment(ref nested) != 1) return;
                        using var resource = ResourceLoader.Load<Resource>(path, ResourceLoader.CacheMode.Ignore);
                    };
                    using (var resource = ResourceLoader.Load<Resource>(path, ResourceLoader.CacheMode.Ignore))
                        Check(resource.ResourcePath == path && nested == 1, "A logger can load the reported resource without recursive delivery.");
                    receiver.MessageAction = null;
                    ResourceLoader.LoadThreadedRequest<Resource>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
                    using var threaded = ResourceLoader.LoadThreadedGet<Resource>(path);
                    Check(threaded.ResourcePath == path && receiver.Messages.Count(entry => entry.Text == loadMessage) == 2,
                        "Synchronous and threaded public loads report through the same verbose stream.");
                }
                finally { Engine.Stop(); }
            }
            Check(receiver.Messages.Contains(("initialization", false)) && EOs.IsSTDOUTVerbose() == switchEnabled,
                "Shutdown retires the sampled setting and retains the command-line override.");
            using (var loop = new VerboseLoop(() => Check(EOs.IsSTDOUTVerbose() == switchEnabled, "The next run resamples settings.")))
            {
                Engine.Start(loop);
                Engine.Stop();
            }
            ProjectSettings.Set(setting, true);
            using var failing = new VerboseLoop(() => throw new InvalidOperationException("startup failure"));
            Reject<InvalidOperationException>(() => Engine.Start(failing));
            Check(EOs.IsSTDOUTVerbose() == switchEnabled, "Failed startup retires the sampled setting.");
        }
        finally
        {
            ProjectSettings.Reset(setting);
            ProjectSettings.ClearFeatureOverride(setting, "verbose_fixture");
            ProjectSettings.RemoveCustomFeature("verbose_fixture");
            EOs.RemoveLogger(receiver);
            Engine.PrintToSTDOUT = previousOutput;
            Console.SetOut(stdout);
            directory.Delete(recursive: true);
        }
        Console.WriteLine("Verbose diagnostics settings, host switches, lifecycle and resource requests passed.");
    }

    private sealed class FlushWriter : StringWriter
    {
        internal int Flushes;
        public override void Flush() => Flushes++;
    }

    internal static void RunOutputPolicy(string mode, string? backend)
    {
        var stdout = Console.Out;
        var stderr = Console.Error;
        var oldOut = Engine.PrintToSTDOUT;
        var oldErr = Engine.PrintErrorMessages;
        var directory = Directory.CreateTempSubdirectory("electron2d-output-");
        var user = directory.CreateSubdirectory("user");
        const string feature = "output_fixture";
        using var output = new FlushWriter();
        using var errors = new FlushWriter();
        using var receiver = new Receiver();
        EOs.AddLogger(receiver);
        try
        {
            using (var saved = new ProjectSettingsRegistry(directory.FullName, user.FullName))
            {
                Check(!saved.Get(ProjectSettings.DisableSTDOUT) && !saved.Get(ProjectSettings.DisableSTDERR) &&
                    !saved.Get(ProjectSettings.FlushSTDOUTOnPrint) && saved.Get(ProjectSettings.PrintHeader), "Output defaults.");
                Check(saved.GetWithOverride(ProjectSettings.FlushSTDOUTOnPrint, ["debug"]) &&
                    !saved.GetWithOverride(ProjectSettings.FlushSTDOUTOnPrint, ["release"]), "The debug feature opts into flushing.");
                saved.Set(ProjectSettings.DisableSTDERR, true);
                saved.Set(ProjectSettings.PrintHeader, false);
                saved.SetFeatureOverride(ProjectSettings.FlushSTDOUTOnPrint, "debug", false);
                saved.Save();
            }
            using (var loaded = new ProjectSettingsRegistry(directory.FullName, user.FullName))
            {
                loaded.Load();
                Check(loaded.Get(ProjectSettings.DisableSTDERR) && !loaded.Get(ProjectSettings.PrintHeader) &&
                    !loaded.GetWithOverride(ProjectSettings.FlushSTDOUTOnPrint, ["debug"]), "Authored output overrides persist.");
            }
            Console.SetOut(output);
            Console.SetError(errors);
            Engine.PrintToSTDOUT = true;
            Engine.PrintErrorMessages = true;
            ProjectSettings.Set(ProjectSettings.DisableSTDERR, true);
            ProjectSettings.SetFeatureOverride(ProjectSettings.DisableSTDOUT, feature, true);
            ProjectSettings.SetFeatureOverride(ProjectSettings.FlushSTDOUTOnPrint, feature, true);
            ProjectSettings.AddCustomFeature(feature);

            using (var loop = new VerboseLoop(() =>
            {
                Check(!Engine.PrintToSTDOUT && !Engine.PrintErrorMessages, "Startup selects both physical output gates before initialization.");
                EOs.LogMessage("muted normal");
                EOs.LogError("muted error");
                Check(receiver.Messages.Contains(("muted normal", false)) && receiver.Errors.Any(error => error.Text == "muted error"), "Muted output still reaches loggers.");
                Check(output.GetStringBuilder().Length == 0 && errors.GetStringBuilder().Length == 0, "Muted writers receive nothing.");
                Console.Out.WriteLine("host output");
                Check(output.ToString().Contains("host output", StringComparison.Ordinal), "Host-owned output is independent.");
                ProjectSettings.ClearFeatureOverride(ProjectSettings.DisableSTDOUT, feature);
                Check(!Engine.PrintToSTDOUT, "Settings edits affect the next run.");
                Engine.PrintToSTDOUT = true;
                Engine.PrintErrorMessages = true;
                EOs.LogMessage("enabled normal");
                EOs.LogMessage("enabled error", error: true);
                Check(output.Flushes == 1 && errors.Flushes == 1, "Both emitted writers flushed.");
            }, () => EOs.LogMessage("final output")))
            {
                Engine.Start(loop);
                Engine.Stop();
            }
            Check(output.Flushes == 2 && Engine.PrintToSTDOUT && Engine.PrintErrorMessages, "Active flushing spans finalization and the host preferences survive retirement.");

            using (var failing = new VerboseLoop(() => throw new ArithmeticException("output startup failure")))
            {
                ProjectSettings.Set(ProjectSettings.DisableSTDOUT, true);
                Reject<ArithmeticException>(() => Engine.Start(failing));
                Check(Engine.PrintToSTDOUT && Engine.PrintErrorMessages && Engine.MainLoop is null, "Failed startup retires the sampled gates.");
                ProjectSettings.Reset(ProjectSettings.DisableSTDOUT);
            }
            ProjectSettings.SetFeatureOverride(ProjectSettings.FlushSTDOUTOnPrint, feature, false);
            using (var unflushed = new VerboseLoop(() =>
            {
                Engine.PrintToSTDOUT = true;
                EOs.LogMessage("normal without explicit flush");
                Check(output.Flushes == 2, "An authored false override does not explicitly flush stdout.");
            }))
            {
                Engine.Start(unflushed);
                Engine.Stop();
            }
            if (backend is not null)
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
                ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
                ProjectSettings.Set(ProjectSettings.PrintHeader, mode != "setting");
                var expectedHeader = mode == "header";
                var initialOutput = output.GetStringBuilder().Length;
                var heading = $"Electron2D v{Engine.VersionInfo.InformationalVersion}";
                using var window = new Window { Size = new(48, 48) };
                window.Ready += _ =>
                {
                    Check(output.ToString()[initialOutput..].Contains(heading, StringComparison.Ordinal) == expectedHeader, "The header setting and host switch govern real stdout before Ready.");
                    Check(receiver.Messages.Contains((heading, false)) == expectedHeader, "The header follows ordinary diagnostic delivery.");
                    Check(Engine.PrintToSTDOUT == (mode != "quiet"), "The quiet switch suppresses physical stdout.");
                    window.Tree!.Quit(6);
                };
                Check(Engine.Run(window) == 6 && Engine.PrintToSTDOUT && Engine.PrintErrorMessages, "Window shutdown retires project output policy.");
            }
        }
        finally
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);
            EOs.RemoveLogger(receiver);
            Engine.PrintToSTDOUT = oldOut;
            Engine.PrintErrorMessages = oldErr;
            ProjectSettings.ClearFeatureOverride(ProjectSettings.DisableSTDOUT, feature);
            ProjectSettings.ClearFeatureOverride(ProjectSettings.FlushSTDOUTOnPrint, feature);
            ProjectSettings.RemoveCustomFeature(feature);
            ProjectSettings.Reset(ProjectSettings.DisableSTDOUT);
            ProjectSettings.Reset(ProjectSettings.DisableSTDERR);
            ProjectSettings.Reset(ProjectSettings.PrintHeader);
            directory.Delete(recursive: true);
        }
        Console.WriteLine("Startup output policy, host writers, logger delivery and lifecycle passed.");
    }

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
