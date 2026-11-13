using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class OSProcessTests
{
    [LibraryImport("kernel32.dll", EntryPoint = "GetConsoleWindow")]
    private static partial nint ConsoleWindow();
    [LibraryImport("kernel32.dll", EntryPoint = "GetConsoleProcessList", SetLastError = true)]
    private static partial uint ConsoleProcesses([Out] uint[] processes, uint count);
    [LibraryImport("user32.dll", EntryPoint = "ShowWindow")]
    private static partial int HideConsole(nint window, int command);
    [LibraryImport("kernel32.dll", EntryPoint = "GetStdHandle")]
    private static partial nint NativeOutput(int kind);
    [LibraryImport("kernel32.dll", EntryPoint = "WriteFile", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool NativeWrite(nint handle, byte[] bytes, int length, out int written, nint overlapped);
    private static bool WindowsChild(string[] arguments)
    {
        if (arguments[1] == "native-ordered")
        {
            for (var i = 0; i < 256; i++)
                foreach (var kind in new[] { -12, -11 })
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes($"{i}:{kind}:λ;");
                    Check(NativeWrite(NativeOutput(kind), bytes, bytes.Length, out var written, 0) && written == bytes.Length, "Complete native writes.");
                }
            return true;
        }
        if (arguments[1] is not ("console-report" or "console-parent")) return false;
        var window = ConsoleWindow(); if (window != 0) HideConsole(window, 0);
        if (arguments[1] == "console-parent") { WindowsParent(arguments[2]); return true; }
        var processes = new uint[32]; var count = ConsoleProcesses(processes, (uint)processes.Length);
        File.WriteAllLines(arguments[2] + ".tmp", [window.ToString(), string.Join(',', processes.Take((int)Math.Min(count, (uint)processes.Length)))]);
        File.Move(arguments[2] + ".tmp", arguments[2], true);
        Console.Write("OUT"); Console.Error.Write("ERR"); Environment.ExitCode = 137; return true;
    }
    private static void WindowsParent(string directory)
    {
        var parentWindow = ConsoleWindow(); Check(parentWindow != 0, "The test parent owns a native console.");
        foreach (var console in new[] { false, true })
        {
            var file = System.IO.Path.Combine(directory, "console-" + console); var child = Launch("console-report", file);
            void Inspect(int pid = 0)
            {
                var report = File.ReadAllLines(file); var window = nint.Parse(report[0]);
                Check(console ? window != 0 && window != parentWindow : window == 0, "New-console and hidden launch have distinct native console identities.");
                if (console && pid != 0) Check(report[1].Split(',').Contains(pid.ToString()) && !report[1].Split(',').Contains(Environment.ProcessId.ToString()), "New console membership contains the child and excludes its parent.");
            }
            var pid = OS.CreateProcess(child.Path, child.Arguments, console); Wait(pid); Inspect(pid); Check(OS.GetProcessExitCode(pid) == 137, "Console launch retains high exit status.");
            var output = new List<string>(); Check(OS.Execute(child.Path, child.Arguments, output, true, console) == 137 && output.Single() == "OUTERR", "New/hidden console capture shares one output pipe."); Inspect();
            output.Clear(); Check(OS.Execute(child.Path, child.Arguments, output, false, console) == 137 && output.Single() == "OUT", "Console stderr discard."); Inspect();
            Check(OS.Execute(child.Path, child.Arguments, null, true, console) == 137, "Console omitted output retains status.");
            var literal = new[] { "", "a b\\\\", "\\\\\"", "x\"y", "λ", "&echo forbidden" }; var argv = Launch("arguments", literal); output.Clear();
            Check(OS.Execute(argv.Path, argv.Arguments, output, true, console) == 23 && output[0] == System.Text.Json.JsonSerializer.Serialize(literal, OSProcessJSON.Default.StringArray) + "Ошибка λ\n", "Console capture preserves escaped literal Unicode argv.");
            Reject<System.ComponentModel.Win32Exception>(() => OS.CreateProcess("missing-process-T-0004.exe", [], console));
            Reject<System.ComponentModel.Win32Exception>(() => OS.Execute("missing-process-T-0004.exe", [], output, true, console));
        }
        var inheritedFile = System.IO.Path.Combine(directory, "console-inherited"); var inherited = Launch("console-report", inheritedFile);
        using (var pipes = OS.ExecuteWithPipe(inherited.Path, inherited.Arguments))
        {
            Check(System.Text.Encoding.UTF8.GetString(pipes.StandardIO.ReadBytes(3)) == "OUT" && System.Text.Encoding.UTF8.GetString(pipes.StandardError.ReadBytes(3)) == "ERR", "Inherited console keeps independent pipe channels."); Wait(pipes.PID);
            var report = File.ReadAllLines(inheritedFile); Check(nint.Parse(report[0]) == parentWindow && report[1].Split(',').Contains(Environment.ProcessId.ToString()), "Pipe launch inherits actual parent console membership.");
        }
        var instanceFile = System.IO.Path.Combine(directory, "console-instance"); var instance = OS.CreateInstance(["--os-process-child", "console-report", instanceFile]); Wait(instance);
        Check(File.ReadAllLines(instanceFile)[0] == "0", "CreateInstance uses the default hidden launch.");
        var openFile = System.IO.Path.Combine(directory, "console-open"); var open = Launch("console-report", openFile); File.Delete(openFile); OS.OpenWithProgram(open.Path, open.Arguments);
        var deadline = System.Diagnostics.Stopwatch.StartNew(); while (!File.Exists(openFile)) { if (deadline.Elapsed.TotalSeconds > 8) throw new InvalidOperationException("OpenWithProgram did not produce its report."); Thread.Sleep(5); }
        Check(File.ReadAllLines(openFile)[0] == "0", "OpenWithProgram shares the hidden launch.");
        Console.Write("CONSOLE-PASSED");
    }
    private static void RunWindows()
    {
        var directory = System.IO.Path.GetFullPath("bin/os-process-validation/windows"); Directory.CreateDirectory(directory);
        var parent = Launch("console-parent", directory); var output = new List<string>();
        var code = OS.Execute(parent.Path, parent.Arguments, output, true, true);
        Check(code == 0 && output.Single().EndsWith("CONSOLE-PASSED", StringComparison.Ordinal), "Native console parent verifies hidden, inherited and separate consoles: " + code + ": " + string.Join("", output));
        var high = Launch("exit-native"); Check(OS.Execute(high.Path, high.Arguments) == unchecked((int)0xc0000005), "Full Windows DWORD exit bits survive capture.");
        var pid = OS.CreateProcess(high.Path, high.Arguments); Wait(pid); Check(OS.GetProcessExitCode(pid) == unchecked((int)0xc0000005), "Full Windows DWORD exit bits survive independent launch.");
        using (var pipes = OS.ExecuteWithPipe(high.Path, high.Arguments)) { Wait(pipes.PID); Check(OS.GetProcessExitCode(pipes.PID) == unchecked((int)0xc0000005), "Full Windows exit bits survive the retained SDL pipe owner."); }
        var active = Launch("exit-still-active"); Check(OS.Execute(active.Path, active.Arguments) == 259, "A completed exit code 259 is distinct from a live process.");
        var activePID = OS.CreateProcess(active.Path, active.Arguments); Wait(activePID); Check(OS.GetProcessExitCode(activePID) == 259, "Native wait state preserves a completed STILL_ACTIVE-valued exit.");
        var ordered = Launch("native-ordered"); output.Clear();
        Check(OS.Execute(ordered.Path, ordered.Arguments, output, true) == 0 && output[0] == string.Concat(Enumerable.Range(0, 256).Select(i => $"{i}:-12:λ;{i}:-11:λ;")), "512 alternating native WriteFile operations retain exact shared-pipe order.");
        Task.WaitAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => { var captured = new List<string>(); Check(OS.Execute(ordered.Path, ordered.Arguments, captured, true) == 0 && captured.Single() == output[0], "Concurrent captures keep native handles isolated."); })).ToArray());
        var bulk = Launch("large"); Check(OS.Execute(bulk.Path, bulk.Arguments) == 0 && OS.Execute(bulk.Path, bulk.Arguments, null, true) == 0, "Large discarded stdout/stderr cannot fill an unread channel.");
    }
}
