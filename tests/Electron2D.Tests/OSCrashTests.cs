using System.Diagnostics;
using System.Reflection;
using System.Text;
using Electron2D;
using static TextureArrayTests;

internal static class OSCrashTests
{
    private const string FatalReason = "Fatal error requested by OS.Crash.";
    private const string Message = "Crash probe: Ж ☃";

    internal static void Run()
    {
        foreach (var mode in new[] { "null", "plain", "active", "worker-empty", "throwing-sinks", "reentry" })
        {
            var start = new ProcessStartInfo(Environment.ProcessPath!)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(start.FileName), "dotnet", StringComparison.OrdinalIgnoreCase))
                start.ArgumentList.Add(Assembly.GetEntryAssembly()!.Location);
            start.ArgumentList.Add("--os-crash-child");
            start.ArgumentList.Add(mode);

            using var child = Process.Start(start)!;
            var output = child.StandardOutput.ReadToEndAsync();
            var error = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30_000))
            {
                child.Kill(entireProcessTree: true);
                throw new TimeoutException($"The {mode} child did not exit.");
            }
            var stdout = output.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            var stderr = error.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            var lines = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var details = $"{mode}: exit={child.ExitCode}\nstdout:\n{stdout}\nstderr:\n{stderr}";
            if (mode == "null")
            {
                Check(child.ExitCode == 0 && lines.SequenceEqual(
                    ["loop started", "before", "null rejected", "finally", "loop finalized", "process exit"]) &&
                    stderr.Length == 0, details);
            }
            else
            {
                string[] expected = mode switch
                {
                    "active" => ["loop started", "before"],
                    "throwing-sinks" => ["before", "throwing logger", "error logger"],
                    "worker-empty" => ["before", "error logger"],
                    "reentry" => ["before", "outer logger"],
                    _ => ["before"]
                };
                Check(child.ExitCode != 0 && lines.SequenceEqual(expected), details);
                Check(stderr.Contains(mode == "worker-empty" ? FatalReason : $"{FatalReason} {Message}",
                    StringComparison.Ordinal), details);
                if (mode is "plain" or "active")
                    Check(stderr.Contains($"Error: {Message}", StringComparison.Ordinal), details);
                else
                    Check(!stderr.Contains("Error: ", StringComparison.Ordinal), details);
            }
            Console.WriteLine($"OS.Crash {mode}: exit={child.ExitCode}, markers=[{string.Join(", ", lines)}]");
        }
    }

    internal static void Child(string mode)
    {
        Console.OutputEncoding = Encoding.UTF8;
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Console.WriteLine("process exit");
        AppDomain.CurrentDomain.UnhandledException += (_, _) => Console.WriteLine("unhandled exception");
        var message = mode == "worker-empty" ? "" : Message;
        if (mode is "null" or "active")
        {
            using var loop = new CrashLoop();
            Engine.Start(loop);
            try { InvokeCrash(mode == "null" ? null : message); }
            finally { Engine.Stop(); }
            return;
        }
        if (mode == "worker-empty")
        {
            var worker = new Thread(() =>
            {
                Engine.PrintErrorMessages = false;
                using var logger = new CrashLogger(message);
                OS.AddLogger(logger);
                InvokeCrash(message);
            });
            worker.Start();
            worker.Join();
            return;
        }

        if (mode == "throwing-sinks")
        {
            Console.SetError(new RefusingWriter());
            OS.AddLogger(new CrashLogger(message, throws: true));
            OS.AddLogger(new CrashLogger(message));
        }
        if (mode == "reentry")
        {
            Engine.PrintToSTDOUT = false;
            Engine.PrintErrorMessages = false;
            OS.AddLogger(new ReentrantLogger());
        }
        InvokeCrash(message, mode == "reentry");
    }

    private static void InvokeCrash(string? message, bool reentrant = false)
    {
        Console.WriteLine("before");
        try
        {
            if (reentrant) OS.LogMessage("outer");
            else OS.Crash(message!);
            Console.WriteLine("returned");
        }
        catch (ArgumentNullException error) when (message is null && error.ParamName == "message")
        {
            Console.WriteLine("null rejected");
        }
        catch (Exception)
        {
            Console.WriteLine("caught");
        }
        finally
        {
            Console.WriteLine("finally");
        }
    }

    private sealed class CrashLogger(string message, bool throws = false) : Logger
    {
        private readonly int _thread = Environment.CurrentManagedThreadId;

        protected override void OnLogError(string function, string file, int line, string code, string rationale,
            bool editorNotify, LoggerErrorType errorType, ReadOnlySpan<ScriptBacktrace> scriptBacktraces)
        {
            if (rationale != message || code != FatalReason || function != nameof(OS.Crash) || editorNotify ||
                errorType != LoggerErrorType.Error || Environment.CurrentManagedThreadId != _thread ||
                scriptBacktraces.IsEmpty)
            {
                Console.WriteLine("invalid error callback");
                return;
            }
            Console.WriteLine(throws ? "throwing logger" : "error logger");
            if (throws) throw new InvalidOperationException("Intentional logger failure.");
        }
    }

    private sealed class ReentrantLogger : Logger
    {
        protected override void OnLogMessage(string message, bool error)
        {
            Console.WriteLine("outer logger");
            OS.Crash(Message);
        }

        protected override void OnLogError(string function, string file, int line, string code, string rationale,
            bool editorNotify, LoggerErrorType errorType, ReadOnlySpan<ScriptBacktrace> scriptBacktraces) =>
            Console.WriteLine("recursive error callback");
    }

    private sealed class RefusingWriter : StringWriter
    {
        public override void WriteLine(string? value) => throw new IOException("Intentional stderr writer failure.");
    }

    private sealed class CrashLoop : MainLoop
    {
        protected override void OnInitialize() => Console.WriteLine("loop started");
        protected override void OnFinalize() => Console.WriteLine("loop finalized");
        protected override void OnNotification(int what)
        {
            if (what == NotificationCrash) Console.WriteLine("crash notification");
        }
    }
}
