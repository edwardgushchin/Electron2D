using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Electron2D;
using Path = System.IO.Path;
using static TextureArrayTests;

internal static class OSStandardInputTests
{
    [DllImport("libc", EntryPoint = "close")]
    private static extern int CloseDescriptor(int descriptor);

    internal static void Child(string[] args)
    {
        var mode = args[1];
        if (mode == "closed")
        {
            Check(OS.ReadBufferFromSTDIN(1).Length == 0 && OS.ReadStringFromSTDIN(2) == "", "Initialize the borrowed reader at EOF before closing its descriptor.");
            Check(CloseDescriptor(0) == 0, "Close the test child's input only.");
        }
        var input = OS.GetSTDINType(); var output = OS.GetSTDOUTType(); var error = OS.GetSTDERRType();
        byte[] data = []; string text = ""; bool rejected = false; bool eof = false;
        switch (mode)
        {
            case "fixed": data = OS.ReadBufferFromSTDIN(4); break;
            case "fragments": Console.WriteLine("READY"); Console.Out.Flush(); data = OS.ReadBufferFromSTDIN(4); eof = OS.ReadBufferFromSTDIN(1).Length == 0; break;
            case "binary": data = OS.ReadBufferFromSTDIN(1024); eof = OS.ReadBufferFromSTDIN(1).Length == 0; break;
            case "terminal": data = OS.ReadBufferFromSTDIN(1024); break;
            case "nul": text = OS.ReadStringFromSTDIN(16); break;
            case "mixed": text = OS.ReadStringFromSTDIN(16); data = OS.ReadBufferFromSTDIN(2); eof = OS.ReadStringFromSTDIN() == ""; break;
            case "bounded": text = OS.ReadStringFromSTDIN(3); data = OS.ReadBufferFromSTDIN(3); eof = OS.ReadBufferFromSTDIN(1).Length == 0; break;
            case "bad":
            case "split": try { OS.ReadStringFromSTDIN(mode == "split" ? 2 : 16); } catch (DecoderFallbackException) { rejected = true; } data = OS.ReadBufferFromSTDIN(1); break;
            case "invalid":
                foreach (var size in new[] { -1, 0, int.MaxValue }) Reject<ArgumentOutOfRangeException>(() => OS.ReadBufferFromSTDIN(size));
                foreach (var size in new[] { -1, 0, 1, int.MaxValue }) Reject<ArgumentOutOfRangeException>(() => OS.ReadStringFromSTDIN(size));
                data = OS.ReadBufferFromSTDIN(1); rejected = true; break;
            case "closed": Reject<IOException>(() => OS.ReadBufferFromSTDIN(1)); Reject<IOException>(() => OS.ReadStringFromSTDIN()); rejected = true; break;
        }
        Console.WriteLine(JsonSerializer.Serialize(new { Input = input, Output = output, Error = error, Data = Convert.ToHexString(data), Text = text, Rejected = rejected, EOF = eof }));
    }

    internal static void Run()
    {
        var binary = new byte[] { 0, 255, 13, 10, 7 };
        using (var report = Redirected("binary", binary)) Verify(report, OS.StdHandleType.Pipe, "00FF0D0A07", eof: true);
        using (var report = Redirected("mixed", Encoding.UTF8.GetBytes("λ\r\nx\n"))) Verify(report, OS.StdHandleType.Pipe, "780A", "λ", eof: true);
        using (var report = Redirected("mixed", Encoding.UTF8.GetBytes("\nx\n"))) Verify(report, OS.StdHandleType.Pipe, "780A", eof: true);
        using (var report = Redirected("nul", Encoding.UTF8.GetBytes("tail\r"))) Verify(report, OS.StdHandleType.Pipe, "", "tail\r");
        using (var report = Redirected("bounded", Encoding.UTF8.GetBytes("abcde"))) Verify(report, OS.StdHandleType.Pipe, "636465", "ab", eof: true);
        using (var report = Redirected("bad", [255, 10, 81])) Verify(report, OS.StdHandleType.Pipe, "51", rejected: true);
        using (var report = Redirected("split", [206, 187])) Verify(report, OS.StdHandleType.Pipe, "BB", rejected: true);
        using (var report = Redirected("invalid", [82])) Verify(report, OS.StdHandleType.Pipe, "52", rejected: true);
        using (var report = Redirected("binary", [])) Verify(report, OS.StdHandleType.Pipe, "", eof: true);
        using (var report = Redirected("nul", [65, 0, 66, 10])) Verify(report, OS.StdHandleType.Pipe, "", "A\0B");
        Fragments();
        var plan = Launch("fixed");
        using (var pipes = OS.ExecuteWithPipe(plan.FileName, plan.ArgumentList.ToArray()))
        {
            pipes.StandardIO.WriteBytes([1, 2, 3, 4]); pipes.StandardIO.Flush();
            using var report = JsonDocument.Parse(pipes.StandardIO.ReadLine());
            Verify(report, OS.StdHandleType.Pipe, "01020304");
            var deadline = Stopwatch.StartNew(); while (OS.IsProcessRunning(pipes.PID) && deadline.Elapsed < TimeSpan.FromSeconds(15)) Thread.Sleep(10);
            if (OS.IsProcessRunning(pipes.PID)) { OS.Kill(pipes.PID); throw new InvalidOperationException("Standard-input pipe child did not exit."); }
            Check(OS.GetProcessExitCode(pipes.PID) == 0, "Existing OS native pipes feed public standard-input APIs.");
        }
        if (OperatingSystem.IsLinux())
        {
            var root = Path.Combine(Path.GetTempPath(), "electron2d-stdio-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            try
            {
                File.WriteAllBytes(Path.Combine(root, "input.bin"), binary);
                using (var report = Hosted("file", root, "binary")) Verify(report, OS.StdHandleType.File, "00FF0D0A07", eof: true, output: OS.StdHandleType.File);
                using (var report = Hosted("null", root, "binary")) Verify(report, OS.StdHandleType.Unknown, "", eof: true);
                using (var report = Hosted("terminal", root, "terminal")) Verify(report, OS.StdHandleType.Console, "7474790A");
                using (var report = Redirected("closed", [])) Verify(report, OS.StdHandleType.Invalid, "", rejected: true);
            }
            finally { Directory.Delete(root, true); }
        }
        Console.WriteLine("OS standard handles, pipe/file/terminal input, UTF8 boundaries, EOF and native pipe integration passed.");
    }

    private static void Verify(JsonDocument report, OS.StdHandleType input, string data, string text = "", bool rejected = false, bool eof = false, OS.StdHandleType output = OS.StdHandleType.Pipe)
    {
        var r = report.RootElement;
        Check(r.GetProperty("Input").GetInt32() == (int)input && r.GetProperty("Output").GetInt32() == (int)output && r.GetProperty("Error").GetInt32() == (int)output, "Actual native handle types.");
        Check(r.GetProperty("Data").GetString() == data && r.GetProperty("Text").GetString() == text && r.GetProperty("Rejected").GetBoolean() == rejected && r.GetProperty("EOF").GetBoolean() == eof, "Actual bytes, text, failure consumption and EOF.");
    }

    private static ProcessStartInfo Launch(string mode)
    {
        var p = new ProcessStartInfo(OS.GetExecutablePath()) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        if (Path.GetFileNameWithoutExtension(p.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) p.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        p.ArgumentList.Add("--os-stdio-child"); p.ArgumentList.Add(mode); return p;
    }

    private static JsonDocument Redirected(string mode, byte[] data)
    {
        using var child = Process.Start(Launch(mode))!;
        try
        {
            child.StandardInput.BaseStream.Write(data); child.StandardInput.Close();
            return Finish(child);
        }
        finally { if (!child.HasExited) { child.Kill(); child.WaitForExit(); } }
    }

    private static JsonDocument Hosted(string host, string root, string mode)
    {
        var childPlan = Launch(mode); var plan = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { Path.Combine(AppContext.BaseDirectory, "Fixtures", "StandardInputHost.py"), host, root, childPlan.FileName }.Concat(childPlan.ArgumentList)) plan.ArgumentList.Add(arg);
        using var child = Process.Start(plan)!;
        try { return Finish(child); }
        finally { if (!child.HasExited) { child.Kill(true); child.WaitForExit(); } }
    }

    private static JsonDocument Finish(Process child)
    {
        var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
        Check(child.WaitForExit(20000), "Standard-input child deadline.");
        Check(child.ExitCode == 0, "Standard-input child: " + error.GetAwaiter().GetResult());
        return JsonDocument.Parse(output.GetAwaiter().GetResult());
    }

    private static void Fragments()
    {
        using var child = Process.Start(Launch("fragments"))!;
        try
        {
            Check(child.StandardOutput.ReadLine() == "READY", "Pipe reader is ready.");
            child.StandardInput.BaseStream.Write([1, 2]); child.StandardInput.BaseStream.Flush();
            var result = child.StandardOutput.ReadLineAsync(); Check(!result.Wait(100), "Pipe input waits for the requested count.");
            child.StandardInput.BaseStream.Write([3, 4]); child.StandardInput.Close();
            Check(result.Wait(15000) && child.WaitForExit(15000) && child.ExitCode == 0, "Fragmented input completes after count and EOF.");
            using var report = JsonDocument.Parse(result.Result!); Verify(report, OS.StdHandleType.Pipe, "01020304", eof: true);
        }
        finally { if (!child.HasExited) { child.Kill(); child.WaitForExit(); } }
    }
}
