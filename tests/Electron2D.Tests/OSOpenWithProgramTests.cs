using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Electron2D;
using static TextureArrayTests;

internal static class OSOpenWithProgramTests
{
    private const string ChildReport = "ELECTRON2D_OPEN_PROGRAM_CHILD";

    internal static bool TryRunChild(string[] arguments)
    {
        var report = Environment.GetEnvironmentVariable(ChildReport);
        if (string.IsNullOrEmpty(report)) return false;
        File.WriteAllText(report + ".started.tmp", OS.GetProcessID().ToString());
        File.Move(report + ".started.tmp", report + ".started");
        var watch = Stopwatch.StartNew();
        while (!File.Exists(report + ".release"))
        {
            if (watch.Elapsed > TimeSpan.FromSeconds(15)) throw new TimeoutException("The launch test did not release its child.");
            Thread.Sleep(5);
        }
        var files = arguments.Where(File.Exists).ToDictionary(path => path, File.ReadAllText);
        var directories = arguments.Where(Directory.Exists).ToArray();
        File.WriteAllText(report, JsonSerializer.Serialize(new { Paths = arguments, Files = files, Directories = directories }));
        return true;
    }

    internal static void Run()
    {
        var executable = Environment.ProcessPath!;
        if (System.IO.Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            executable = System.IO.Path.Combine(AppContext.BaseDirectory, "Electron2D.Tests" + (OperatingSystem.IsWindows() ? ".exe" : ""));
        Check(File.Exists(executable), "The executable consumer apphost exists.");
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-open-program-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var document = System.IO.Path.Combine(directory, "Документ λ with spaces.txt");
        File.WriteAllText(document, "opened by the selected application");
        var previous = OS.HasEnvironment(ChildReport) ? OS.GetEnvironment(ChildReport) : null;
        try
        {
            foreach (var paths in new[] { Array.Empty<string>(), new[] { document, directory, "folder with spaces/file.txt", "", "каталог/λ.txt", "quote\"'", "; touch unintended", "$(touch unintended)", "--", "trailing\\" } })
            {
                var report = System.IO.Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
                OS.SetEnvironment(ChildReport, report);
                var expected = (string[])paths.Clone();
                OS.OpenWithProgram(executable, paths);
                if (paths.Length != 0) paths[0] = "caller edited its array";
                WaitFor(() => File.Exists(report + ".started"));
                var pid = int.Parse(File.ReadAllText(report + ".started"));
                try
                {
                    Check(OS.IsProcessRunning(pid) && !File.Exists(report), "Launch returns while the independent application still waits for its input gate.");
                    File.WriteAllText(report + ".release", "release");
                    WaitFor(() => !OS.IsProcessRunning(pid));
                    using var result = JsonDocument.Parse(File.ReadAllText(report));
                    Check(OS.GetProcessExitCode(pid) == 0 && result.RootElement.GetProperty("Paths").Deserialize<string[]>()!.SequenceEqual(expected), "Every literal path, empty list and caller-owned snapshot reaches the application unchanged.");
                    if (expected.Length != 0)
                        Check(result.RootElement.GetProperty("Files").GetProperty(document).GetString() == "opened by the selected application" && result.RootElement.GetProperty("Directories")[0].GetString() == directory, "The chosen application opens the real Unicode document and directory.");
                }
                finally { if (OS.IsProcessRunning(pid)) { OS.Kill(pid); WaitFor(() => !OS.IsProcessRunning(pid)); } }
            }
            var rejected = System.IO.Path.Combine(directory, "rejected.json");
            OS.SetEnvironment(ChildReport, rejected);
            Reject<ArgumentNullException>(() => OS.OpenWithProgram(null!, []));
            Reject<ArgumentException>(() => OS.OpenWithProgram("", []));
            Reject<ArgumentException>(() => OS.OpenWithProgram("bad\0program", []));
            Reject<EncoderFallbackException>(() => OS.OpenWithProgram("bad\uD800", []));
            Reject<ArgumentNullException>(() => OS.OpenWithProgram(executable, ["valid", null!]));
            Reject<ArgumentException>(() => OS.OpenWithProgram(executable, ["valid", "bad\0path"]));
            Reject<EncoderFallbackException>(() => OS.OpenWithProgram(executable, ["valid", "bad\uD800"]));
            Check(!File.Exists(rejected + ".started"), "The complete input is validated before launching any application.");
            if (OperatingSystem.IsWindows()) Reject<System.ComponentModel.Win32Exception>(() => OS.OpenWithProgram(System.IO.Path.Combine(directory, "missing-program"), []));
            else Reject<IOException>(() => OS.OpenWithProgram(System.IO.Path.Combine(directory, "missing-program"), []));
            Console.WriteLine("OpenWithProgram: independent native launch, literal snapshots, empty paths and invalid-input/failure checks passed.");
        }
        finally
        {
            if (previous is null) OS.UnsetEnvironment(ChildReport); else OS.SetEnvironment(ChildReport, previous);
            Directory.Delete(directory, true);
        }
    }

    private static void WaitFor(Func<bool> completed)
    {
        var watch = Stopwatch.StartNew();
        while (!completed())
        {
            if (watch.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("The application launch did not complete.");
            Thread.Sleep(5);
        }
    }
}
