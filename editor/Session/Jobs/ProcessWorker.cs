using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Electron2D.Editor;

// The session assembly's private executable role watches the owner's pipe, even after an abrupt owner death.
internal static partial class ProcessWorker
{
    internal static async Task<int> Main(string[] args)
    {
        if (args.Length < 3 || args[0] != "--session-process-worker" || !OperatingSystem.IsLinux()) return 2;
        var group = Environment.ProcessId;
        if (GetProcessGroup() != group) return 7;
        var start = new ProcessStartInfo(args[2]) { WorkingDirectory = args[1], UseShellExecute = false };
        foreach (var arg in args.Skip(3)) start.ArgumentList.Add(arg);
        using var child = Process.Start(start) ?? throw new InvalidOperationException("The owned process did not start.");
        var gone = Console.OpenStandardInput().ReadAsync(new byte[1]).AsTask();
        var exited = child.WaitForExitAsync();
        if (await Task.WhenAny(exited, gone).ConfigureAwait(false) == gone)
        {
            Kill(-group, 9); return 130;
        }
        await exited.ConfigureAwait(false);
        // The command can exit while a compiler/game grandchild still owns pipes. Retire this exact session's members.
        var timer = Stopwatch.StartNew();
        while (true)
        {
            var found = false;
            foreach (var path in Directory.EnumerateDirectories("/proc"))
            {
                if (!int.TryParse(IOPath.GetFileName(path), CultureInfo.InvariantCulture, out var pid) || pid == group) continue;
                string data;
                try { data = File.ReadAllText(IOPath.Combine(path, "stat")); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { continue; }
                var close = data.LastIndexOf(')'); if (close < 0) continue;
                var fields = data[(close + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length < 4 || fields[0] == "Z" || !int.TryParse(fields[2], CultureInfo.InvariantCulture, out var pgrp) ||
                    !int.TryParse(fields[3], CultureInfo.InvariantCulture, out var session) || pgrp != group || session != group) continue;
                found = true; Kill(pid, 9);
            }
            if (!found) return child.ExitCode;
            if (timer.Elapsed > TimeSpan.FromSeconds(5)) { Kill(-group, 9); return 4; }
            await Task.Delay(10).ConfigureAwait(false);
        }
    }
    [LibraryImport("libc", EntryPoint = "getpgrp")]
    private static partial int GetProcessGroup();
    [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static partial int Kill(int pid, int signal);
}
