using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Electron2D;

internal static partial class OSRestartTests
{
    private static readonly string[] LiteralArguments = ["with spaces", "", "quote\"slash\\", "--", "Ω λ", "++", "tail"];
    internal static void Run()
    {
        Check(!OS.IsRestartOnExitSet() && OS.GetRestartOnExitArguments().Length == 0, "Default restart state is disabled with empty arguments.");
        try
        {
            var source = new[] { "old", "" }; OS.SetRestartOnExit(true, source); source[0] = "edited";
            var result = OS.GetRestartOnExitArguments(); Check(result[0] == "old" && OS.IsRestartOnExitSet(), "Setter snapshots caller storage.");
            result[0] = "mutated"; Check(OS.GetRestartOnExitArguments()[0] == "old", "Getter snapshots private storage.");
            Throws<ArgumentNullException>(() => OS.SetRestartOnExit(false, ["valid", null!]));
            Throws<ArgumentException>(() => OS.SetRestartOnExit(false, ["bad\0value"]));
            Throws<EncoderFallbackException>(() => OS.SetRestartOnExit(false, ["\ud800"]));
            Check(OS.IsRestartOnExitSet() && OS.GetRestartOnExitArguments().SequenceEqual(new[] { "old", "" }), "Invalid requests preserve both committed values.");
            OS.SetRestartOnExit(false, ["retained"]); Check(!OS.IsRestartOnExitSet() && OS.GetRestartOnExitArguments()[0] == "retained", "Disabled configuration keeps supplied arguments.");
            OS.SetRestartOnExit(true); Check(OS.GetRestartOnExitArguments().Length == 0, "Omitted arguments replace the previous list.");
            OS.SetRestartOnExit(false);
            Task.Run(() => OS.SetRestartOnExit(false, ["worker"])).GetAwaiter().GetResult();
            Check(OS.GetRestartOnExitArguments()[0] == "worker", "Configuration is process-wide and thread-safe.");
            for (var i = 0; i < 64; i++) _ = OS.IsRestartOnExitSet(); var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1024; i++) Check(!OS.IsRestartOnExitSet(), "Warmed flag remains disabled.");
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed flag queries allocate zero managed bytes.");
        }
        finally { OS.SetRestartOnExit(false); }
        using var temp = DirAccess.CreateTemp("os-restart-"); var directory = temp.GetCurrentDir();
        Host(directory, "dotnet", "replace", "managed"); Host(directory, "apphost", "cancel", "managed");
        Host(directory, "apphost", "replace", "managed");
        if (OperatingSystem.IsLinux()) FailedLaunch(directory);
        Console.WriteLine("OS restart configuration, literal arguments, cancellation, one-shot process relaunch and original exit status passed.");
    }
    private static void Host(string directory, string launcher, string policy, string backend)
    {
        var journal = System.IO.Path.Combine(directory, $"{launcher}-{policy}-{backend}.jsonl");
        var runner = typeof(OSRestartTests).Assembly.Location;
        var path = launcher == "dotnet" ? "dotnet" : System.IO.Path.Combine(AppContext.BaseDirectory, "Electron2D.Tests" + (OperatingSystem.IsWindows() ? ".exe" : ""));
        var start = new ProcessStartInfo(path) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        if (launcher == "dotnet") start.ArgumentList.Add(runner);
        foreach (var argument in new[] { "--os-restart-child", journal, "0", policy, backend }) start.ArgumentList.Add(argument);
        start.Environment["SDL_VIDEODRIVER"] = "wayland"; start.Environment["SDL_AUDIODRIVER"] = "dummy";
        using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Restarting host did not exit."); }
        Check(process.ExitCode == 23, "Original application exit code is preserved.");
        var stdout = output.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        var stderr = error.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        Check(!stderr.Contains("Application restart failed", StringComparison.Ordinal), stderr);
        var rows = File.ReadAllLines(journal).Select(line => JsonSerializer.Deserialize<Report>(line)!).ToArray();
        Check(rows.Length == (policy == "cancel" ? 1 : 2) && rows[0].Stage == 0, "Exactly one replacement process, or cancellation.");
        Check(rows.All(r => r.Cleaned), "Every host cleans its loop before normal process exit.");
        Check(File.ReadAllText(journal + ".shutdown") == "captured", "Shutdown closes configuration after one captured request.");
        var evidence = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_OS_RESTART_OUTPUT");
        if (!string.IsNullOrEmpty(evidence))
        {
            Directory.CreateDirectory(evidence);
            File.Copy(journal, System.IO.Path.Combine(evidence, System.IO.Path.GetFileName(journal)), true);
            File.WriteAllText(System.IO.Path.Combine(evidence, System.IO.Path.GetFileName(journal) + ".stderr"), stderr);
        }
        if (policy != "cancel")
        {
            Check(rows[1].Stage == 1 && rows[0].PID != rows[1].PID, "Restart creates an independent process with a new PID.");
            Check(rows[1].Arguments.SequenceEqual(LiteralArguments), "Only the most recently captured literal arguments are supplied.");
            Check(!rows[1].RestartEnabled && stdout.Contains("Restart child completed"), "The replacement begins with disabled default state and completes.");
        }
    }
    private sealed record Report(int Stage, int PID, string[] Arguments, bool Cleaned, bool RestartEnabled);
    internal static void Child(string[] args)
    {
        var journal = args[1]; var stage = int.Parse(args[2]); var policy = args[3]; var backend = args.Length > 4 ? args[4] : "managed";
        Check(stage is 0 or 1 && !OS.IsRestartOnExitSet(), "Fresh host starts with default restart state.");
        if (stage == 0)
        {
            var stale = new[] { "--os-restart-child", journal + ".stale", "1", policy, backend };
            OS.SetRestartOnExit(true, stale);
            var final = new[] { "--os-restart-child", journal, "1", policy, backend }.Concat(LiteralArguments).ToArray();
            OS.SetRestartOnExit(true, final); final[1] = journal + ".mutated";
            if (policy == "cancel") OS.SetRestartOnExit(false, ["retained after cancel"]);
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                Throws<InvalidOperationException>(() => OS.SetRestartOnExit(false));
                if (policy != "cancel") Check(!OS.IsRestartOnExitSet() && OS.GetRestartOnExitArguments().Length == 0, "Attempted request is cleared before later shutdown callbacks.");
                File.WriteAllText(journal + ".shutdown", "captured");
            };
            var cleaned = RunLoop(backend);
            if (policy == "failure")
            {
                var executable = Environment.ProcessPath!;
                Check(System.IO.Path.GetDirectoryName(executable) == System.IO.Path.GetDirectoryName(journal), "Failure fixture owns its temporary executable.");
                File.Delete(executable);
            }
            File.AppendAllText(journal, JsonSerializer.Serialize(new Report(0, OS.GetProcessID(), [], cleaned, OS.IsRestartOnExitSet())) + "\n");
            Environment.ExitCode = 23;
        }
        else
        {
            var cleaned = RunLoop(backend);
            File.AppendAllText(journal, JsonSerializer.Serialize(new Report(1, OS.GetProcessID(), args[5..], cleaned, OS.IsRestartOnExitSet())) + "\n");
            Console.WriteLine("Restart child completed");
        }
    }
    private static void FailedLaunch(string directory)
    {
        var copy = System.IO.Path.Combine(directory, "failure"); Directory.CreateDirectory(copy);
        foreach (var name in new[] { "Electron2D.Tests", "Electron2D.Tests.dll", "Electron2D.Tests.deps.json", "Electron2D.Tests.runtimeconfig.json", "Electron2D.dll" })
            File.Copy(System.IO.Path.Combine(AppContext.BaseDirectory, name), System.IO.Path.Combine(copy, name));
        foreach (var dependency in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
            if (!File.Exists(System.IO.Path.Combine(copy, System.IO.Path.GetFileName(dependency)))) File.Copy(dependency, System.IO.Path.Combine(copy, System.IO.Path.GetFileName(dependency)));
        Directory.CreateSymbolicLink(System.IO.Path.Combine(copy, "runtimes"), System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes"));
        var journal = System.IO.Path.Combine(copy, "failure.jsonl");
        var start = new ProcessStartInfo(System.IO.Path.Combine(copy, "Electron2D.Tests")) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var value in new[] { "--os-restart-child", journal, "0", "failure", "managed" }) start.ArgumentList.Add(value);
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Failure fixture did not finish."); }
        var failureError = error.GetAwaiter().GetResult();
        Check(child.ExitCode == 23 && failureError.Contains("Application restart failed", StringComparison.Ordinal), $"Actual launch failure: exit={child.ExitCode}, stderr={failureError}");
        Check(File.ReadAllLines(journal).Length == 1 && File.ReadAllText(journal + ".shutdown") == "captured", "Failed request attempts once and completes ordinary shutdown.");
        _ = output.GetAwaiter().GetResult();
    }
    private static bool RunLoop(string backend)
    {
        if (backend != "managed") return RunWindow(backend);
        var root = new Node(); using var tree = new SceneTree(root);
        Engine.Start(tree); Engine.AdvanceFrame(.001); tree.Quit(23); Engine.Stop(); tree.Dispose();
        return root.IsDisposed && Engine.MainLoop is null;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
