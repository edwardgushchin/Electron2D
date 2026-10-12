using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Electron2D;
using static TextureArrayTests;
using IOFileAccess = Electron2D.FileAccess;

internal static class OSProcessTests
{
    internal static void Child(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false); Console.InputEncoding = new UTF8Encoding(false);
        switch (args[1])
        {
            case "arguments": Console.Write(JsonSerializer.Serialize(args[2..])); Console.Error.Write("Ошибка λ\n"); Environment.ExitCode = 23; break;
            case "detach-parent": var daughter = Launch("delayed-file", args[2]); var detached = OS.CreateProcess(daughter.Path, daughter.Arguments); File.WriteAllText(args[2] + ".parent", JsonSerializer.Serialize(new { Parent = OS.GetProcessID(), Child = detached })); break;
            case "delayed-file": Thread.Sleep(1500); File.WriteAllText(args[2], "survived"); break;
            case "file": System.IO.File.WriteAllText(args[2], JsonSerializer.Serialize(new { PID = OS.GetProcessID(), Arguments = args[3..], UserArguments = OS.GetCommandLineUserArgs(), Executable = OS.GetExecutablePath() })); break;
            case "echo": var text = Console.ReadLine(); Console.Write("OUT:" + text + "\rNEXT\n"); Console.Error.Write("ERR:" + text + "\n"); Environment.ExitCode = 17; break;
            case "ordered": Console.Error.Write("FIRST:"); Console.Error.Flush(); Console.Write("SECOND"); break;
            case "large": Console.Write(new string('O', 2 * 1024 * 1024)); Console.Error.Write(new string('E', 2 * 1024 * 1024)); break;
            case "duplex": Console.OpenStandardOutput().Write(new byte[1048576]); var duplex = new byte[1048576]; Console.OpenStandardInput().ReadExactly(duplex); Console.Write(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(duplex)) + "\n"); break;
            case "exit-high": Environment.ExitCode = 137; break;
            case "hold": Thread.Sleep(30000); break;
            case "transfer": var bytes = new byte[int.Parse(args[2])]; Console.OpenStandardInput().ReadExactly(bytes); Console.Write(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)) + "\n"); break;
            default: throw new ArgumentException("Unknown child mode.");
        }
    }
    private static (string Path, string[] Arguments) Launch(string mode, params string[] arguments)
    {
        var path = Environment.ProcessPath!; var prefix = System.IO.Path.GetFileNameWithoutExtension(path).Equals("dotnet", StringComparison.OrdinalIgnoreCase) ? new[] { Assembly.GetExecutingAssembly().Location } : Array.Empty<string>();
        return (path, [.. prefix, "--os-process-child", mode, .. arguments]);
    }
    private static void Wait(int pid)
    {
        var time = Stopwatch.StartNew(); while (OS.IsProcessRunning(pid)) { if (time.Elapsed > TimeSpan.FromSeconds(8)) { OS.Kill(pid); throw new InvalidOperationException("The child did not complete in time."); } Thread.Sleep(5); }
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int Forgotten(string path, string[] arguments) => OS.ExecuteWithPipe(path, arguments, false).PID;
    internal static void Run()
    {
        Check(OS.GetExecutablePath() == Environment.ProcessPath && OS.GetCommandLineArgs().SequenceEqual(Environment.GetCommandLineArgs()[1..]), "Current executable and copied command arguments.");
        Check(OS.GetProcessID() == Environment.ProcessId && OS.IsProcessRunning(OS.GetProcessID()), "Native current-process identity and external process lookup.");
        Reject<ArgumentException>(() => OS.CreateProcess("", [])); Reject<ArgumentException>(() => OS.CreateProcess("x\0", [])); Reject<ArgumentException>(() => OS.CreateProcess("x", ["bad\0arg"])); Reject<ArgumentOutOfRangeException>(() => OS.IsProcessRunning(0)); Reject<EncoderFallbackException>(() => OS.CreateProcess("valid", ["\ud800"]));
        Reject<IOException>(() => OS.CreateProcess("/missing/executable/invalid", [])); Reject<IOException>(() => OS.ExecuteWithPipe("/missing/executable/invalid", []));
        var literal = new[] { "", "two words", "quote\"\\tail", "$(touch never-executed); &", "Привет λ" }; var launch = Launch("arguments", literal); var output = new List<string> { "preserve" }; var code = OS.Execute(launch.Path, launch.Arguments, output, readStderr: true);
        Check(code == 23 && output.Count == 2 && output[0] == "preserve" && output[1] == JsonSerializer.Serialize(literal) + "Ошибка λ\n", "Literal argv, captured UTF8 stdout/stderr, append and actual nonzero exit.");
        var onlyOutput = new List<string>(); Check(OS.Execute(launch.Path, launch.Arguments, onlyOutput) == 23 && onlyOutput[0] == JsonSerializer.Serialize(literal), "Disabled stderr capture discards errors while preserving stdout."); Check(OS.Execute(launch.Path, launch.Arguments) == 23, "Omitted output discards capture and retains exit status.");
        var ordered = Launch("ordered"); var order = new List<string>(); Check(OS.Execute(ordered.Path, ordered.Arguments, order, true) == 0 && order[0] == "FIRST:SECOND", "Captured stdout/stderr preserve actual shared-pipe write order.");
        var large = Launch("large"); var bulk = new List<string>(); Check(OS.Execute(large.Path, large.Arguments, bulk, true) == 0 && bulk[0] == new string('O', 2 * 1024 * 1024) + new string('E', 2 * 1024 * 1024), "Both channels drain beyond native pipe capacity without deadlock.");
        var dir = System.IO.Path.GetFullPath("bin/os-process-validation/2026-10-12"); Directory.CreateDirectory(dir); var file = System.IO.Path.Combine(dir, "created.json"); var create = Launch("file", file, "literal value"); var pid = OS.CreateProcess(create.Path, create.Arguments); Wait(pid); Check(OS.GetProcessExitCode(pid) == 0 && File.Exists(file), "Independent launch and retained child exit status.");
        var survived = System.IO.Path.Combine(dir, "survival.txt"); File.Delete(survived); var parentLaunch = Launch("detach-parent", survived); var parent = OS.CreateProcess(parentLaunch.Path, parentLaunch.Arguments); Wait(parent); using (var parentReport = JsonDocument.Parse(File.ReadAllText(survived + ".parent"))) { var daughter = parentReport.RootElement.GetProperty("Child").GetInt32(); Check(OS.IsProcessRunning(daughter), "Independent child remains alive after its launching application exits."); Wait(daughter); Check(File.ReadAllText(survived) == "survived", "Independent child completes useful work after parent exit."); }
        var again = System.IO.Path.Combine(dir, "instance.json"); var instance = OS.CreateInstance(["--os-process-child", "file", again, "new args", "--", "user value", "++", "tail"]); Wait(instance); using (var report = JsonDocument.Parse(File.ReadAllText(again))) Check(report.RootElement.GetProperty("PID").GetInt32() == instance && report.RootElement.GetProperty("Arguments")[0].GetString() == "new args" && report.RootElement.GetProperty("UserArguments")[0].GetString() == "user value" && report.RootElement.GetProperty("UserArguments")[1].GetString() == "++", "CreateInstance restarts the current application and preserves literal arguments.");
        foreach (var blocking in new[] { true, false })
        {
            var echo = Launch("echo"); using var pipes = OS.ExecuteWithPipe(echo.Path, echo.Arguments, blocking);
            Check(pipes.PID > 0 && OS.IsProcessRunning(pipes.PID) && OS.GetProcessExitCode(pipes.PID) == -1, "Actual live native pipe process.");
            Reject<NotSupportedException>(() => pipes.StandardIO.Seek(0)); Reject<NotSupportedException>(() => _ = pipes.StandardIO.Length); Reject<InvalidOperationException>(() => pipes.StandardError.WriteByte(1));
            if (!blocking) { var watch = Stopwatch.StartNew(); Check(pipes.StandardIO.ReadBytes(32).Length == 0 && !pipes.StandardIO.EOFReached && watch.ElapsedMilliseconds < 200, "Empty nonblocking read returns promptly without claiming EOF."); Reject<IOException>(() => pipes.StandardIO.ReadUInt32()); }
            pipes.StandardIO.WriteString("Привет λ\n"); pipes.StandardIO.Flush();
            if (!blocking) Wait(pipes.PID);
            Check(pipes.StandardIO.ReadLine() == "OUT:Привет λ" && pipes.StandardIO.ReadLine() == "NEXT" && pipes.StandardError.ReadLine() == "ERR:Привет λ", "Bidirectional UTF8 pipes and non-seekable CR pushback.");
            Wait(pipes.PID); Check(OS.GetProcessExitCode(pipes.PID) == 17, "Native child retains actual exit code."); Check(pipes.StandardIO.ReadBytes(1).Length == 0 && pipes.StandardIO.EOFReached, "EOF follows writer closure."); pipes.Dispose(); Check(pipes.StandardIO.IsDisposed && pipes.StandardError.IsDisposed, "Result disposal closes owned streams.");
        }
        var hold = Launch("hold"); using (var pipes = OS.ExecuteWithPipe(hold.Path, hold.Arguments, false))
        {
            var payload = new byte[2 * 1024 * 1024]; try { pipes.StandardIO.WriteBytes(payload); throw new InvalidOperationException("A full nonblocking pipe unexpectedly accepted all input."); } catch (PipeWriteException error) { Check(error.BytesWritten >= 0 && error.BytesWritten < payload.Length, "Partial writes expose the exact consumed prefix."); }
            OS.Kill(pipes.PID); Wait(pipes.PID); Check(!OS.IsProcessRunning(pipes.PID) && OS.GetProcessExitCode(pipes.PID) == (OperatingSystem.IsWindows() ? 0 : 9), "Native pipe child termination preserves signal/exit status.");
        }
        var high = Launch("exit-high"); var highPID = OS.CreateProcess(high.Path, high.Arguments); Wait(highPID); Check(OS.GetProcessExitCode(highPID) == 137, "A high normal exit code stays distinct from signal termination."); Reject<ArgumentException>(() => OS.GetProcessExitCode(int.MaxValue));
        var held = OS.CreateProcess(hold.Path, hold.Arguments); OS.Kill(held); Wait(held); Check(!OS.IsProcessRunning(held) && OS.GetProcessExitCode(held) == (OperatingSystem.IsWindows() ? 0 : 9), "Managed child termination and exit status.");
        var duplexLaunch = Launch("duplex"); using (var pipes = OS.ExecuteWithPipe(duplexLaunch.Path, duplexLaunch.Arguments))
        {
            var payload = new byte[1048576]; new Random(93).NextBytes(payload); var reader = Task.Run(() => pipes.StandardIO.ReadBytes(1048576)); pipes.StandardIO.WriteBytes(payload); Check(reader.GetAwaiter().GetResult().Length == 1048576 && pipes.StandardIO.ReadLine() == Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(payload)), "Blocking duplex read/write directions drain concurrently beyond pipe capacity."); Wait(pipes.PID);
        }
        var transfer = Launch("transfer", "2097152"); using (var pipes = OS.ExecuteWithPipe(transfer.Path, transfer.Arguments, false))
        {
            var payload = new byte[2097152]; new Random(37).NextBytes(payload); var written = 0; var retries = 0; var deadline = Stopwatch.StartNew();
            while (written < payload.Length) { try { pipes.StandardIO.WriteBytes(payload.AsSpan(written)); written = payload.Length; } catch (PipeWriteException error) { written += error.BytesWritten; retries++; if (deadline.Elapsed.TotalSeconds > 8) { OS.Kill(pipes.PID); throw; } Thread.Sleep(1); } }
            Wait(pipes.PID); Check(pipes.StandardIO.ReadLine() == Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(payload)), "Partial-write retries preserve every byte exactly once.");
        }
        var forgotten = Forgotten(hold.Path, hold.Arguments); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); Check(OS.IsProcessRunning(forgotten), "Finalized pipe handles release streams without killing the independent child."); OS.Kill(forgotten); Wait(forgotten);
        Console.WriteLine("OS processes: literal argv, independent/current instances, actual exit/kill, 4MiB capture, blocking/nonblocking native stdin/stdout/stderr, EOF, partial-write state and cleanup passed.");
    }
}
