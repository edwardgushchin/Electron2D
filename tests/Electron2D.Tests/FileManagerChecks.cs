using Electron2D;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using IOPath = System.IO.Path;

internal static class FileManagerChecks
{
    private static int _checks;
    private static string _probe = "";
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); _checks++; }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void Run(string probe, string output)
    {
        _probe = probe; Directory.CreateDirectory(output);
        var folder = IOPath.Combine(output, "private-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var child = IOPath.Combine(folder, "directory &,; 日本語"); Directory.CreateDirectory(child);
        var item = IOPath.Combine(child, "item %,;& 日本語.txt"); File.WriteAllText(item, "Owned Shell selection fixture.");
        var before = Native("baseline").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var service = Engine.GetSingleton<OS>("OS");
        try
        {
            Reject<ArgumentNullException>(() => OS.ShellShowInFileManager(null!));
            Reject<ArgumentException>(() => OS.ShellShowInFileManager(""));
            Reject<ArgumentException>(() => OS.ShellShowInFileManager("bad\0path"));
            Reject<IOException>(() => OS.ShellShowInFileManager(IOPath.Combine(folder, "absent.txt")));
            OS.ShellShowInFileManager(child); Check(Native("verify", child, "-").Contains("selection=0"), "Fresh directory entry opens the directory without selecting an item.");
            OS.ShellShowInFileManager(item); Check(Native("verify", child, item).Contains("selection=1"), "A file request selects the exact Unicode/metacharacter Shell item.");
            OS.ShellShowInFileManager(child, false); Check(Native("verify", folder, child).Contains("selection=1"), "openFolder=false selects the directory in its parent.");
            var relative = IOPath.GetRelativePath(Environment.CurrentDirectory, item);
            OS.ShellShowInFileManager(relative, false); Check(Native("verify", child, item).Contains("selection=1"), "Relative paths resolve before native navigation.");
            OS.ShellShowInFileManager(child + IOPath.DirectorySeparatorChar); Check(Native("verify", child, item).Contains("selection=1"), "Directory entry reuses native view state without selecting the directory itself.");
            RunThread(ApartmentState.STA, () => { OS.ShellShowInFileManager(item); });
            Check(Native("verify", child, item).Contains("selection=1"), "Existing STA caller requests the same item.");
            RunThread(ApartmentState.MTA, () => { OS.ShellShowInFileManager(child, false); });
            Check(Native("verify", folder, child).Contains("selection=1"), "Existing MTA caller requests the same directory item.");
            Check(!DisplayServer.IsAvailable && ReferenceEquals(service, Engine.GetSingleton<OS>("OS")), "Shell navigation preserves permanent OS identity and does not open an engine display.");
        }
        finally
        {
            var cleanup = new List<string> { "close", folder }; cleanup.AddRange(before.Select(value => value.Trim())); Native(cleanup.ToArray());
        }
        Console.WriteLine($"Windows file manager: {_checks} checks passed.");
    }
    private static void RunThread(ApartmentState state, Action action)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() => { try { var before = Thread.CurrentThread.GetApartmentState(); action(); Check(Thread.CurrentThread.GetApartmentState() == before, "Caller apartment is unchanged."); } catch (Exception error) { failure = ExceptionDispatchInfo.Capture(error); } });
        thread.SetApartmentState(state); thread.Start(); thread.Join(); failure?.Throw();
    }
    private static string Native(params string[] args)
    {
        var start = new ProcessStartInfo(_probe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true }; foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start Shell observer.");
        if (!process.WaitForExit(15000)) { process.Kill(); throw new TimeoutException("Shell observer timed out."); }
        var output = process.StandardOutput.ReadToEnd(); var error = process.StandardError.ReadToEnd();
        if (process.ExitCode != 0) throw new InvalidOperationException("Native Shell observer " + args[0] + " exit=" + process.ExitCode + ": " + output + error);
        return output.Trim();
    }
}
