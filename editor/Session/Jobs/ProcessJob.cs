using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Electron2D.Editor;

internal static partial class ProcessJob
{
    internal static async Task<int> Run(string file, string[] args, string workingDirectory, string output, string name, int deadline, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        await using var outputLog = OpenLog(IOPath.Combine(output, name + ".stdout.log"));
        await using var errorLog = OpenLog(IOPath.Combine(output, name + ".stderr.log"));
        if (!OperatingSystem.IsLinux())
            throw new JobFailure("JOB_CONTROL_UNAVAILABLE", 7, "Isolated project jobs currently require Linux process groups; this platform has no implemented job owner.");
        var isolated = OperatingSystem.IsLinux();
        var launcher = isolated ? new[] { "/usr/bin/setsid", "/bin/setsid" }.FirstOrDefault(File.Exists) : null;
        if (isolated && launcher is null) throw new JobFailure("JOB_CONTROL_UNAVAILABLE", 7, "Linux project jobs require the platform session launcher.");
        var start = new ProcessStartInfo(launcher ?? throw new PlatformNotSupportedException()) { WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true, UseShellExecute = false };
        start.ArgumentList.Add("--wait"); start.ArgumentList.Add("dotnet");
        start.ArgumentList.Add(typeof(SessionJob).Assembly.Location);
        start.ArgumentList.Add("--session-process-worker"); start.ArgumentList.Add(workingDirectory); start.ArgumentList.Add(file);
        foreach (var arg in args) start.ArgumentList.Add(arg);
        start.Environment["DOTNET_NOLOGO"] = "1";
        start.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation); stop.CancelAfter(deadline);
        using var process = Process.Start(start) ?? throw new JobFailure("PROCESS_FAILED", 4, "The child process could not start.");
        var stdout = Pump(process.StandardOutput.BaseStream, outputLog, stop);
        var stderr = Pump(process.StandardError.BaseStream, errorLog, stop);
        try
        {
            await process.WaitForExitAsync(stop.Token);
            if (isolated) TerminateSession(process.Id);
            await Task.WhenAll(stdout, stderr).WaitAsync(stop.Token);
            return process.ExitCode;
        }
        catch
        {
            process.StandardInput.Close();
            if (isolated) TerminateSession(process.Id);
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) when (process.HasExited) { }
            await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
            try { await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (Exception error) when (stdout.IsFaulted || stderr.IsFaulted)
            { throw new JobFailure("LOG_FAILED", 4, error.Message); }
            if (cancellation.IsCancellationRequested) throw new OperationCanceledException(cancellation);
            if (stop.IsCancellationRequested) throw new JobFailure("DEADLINE_EXCEEDED", 124, $"The {name} process exceeded {deadline} milliseconds and was terminated.");
            throw;
        }
    }
    private static FileStream OpenLog(string path)
    {
        try { return File.Create(path); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { throw new JobFailure("LOG_FAILED", 4, error.Message); }
    }
    [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static partial int KillGroup(int group, int signal);
    internal static void TerminateSession(int processID)
    {
        if (KillGroup(-processID, 9) != 0 && Marshal.GetLastPInvokeError() != 3)
            throw new JobFailure("JOB_CLEANUP_FAILED", 4, "The owned process group could not be terminated.");
    }
    private static async Task Pump(Stream input, Stream output, CancellationTokenSource failure)
    {
        try
        {
            var buffer = new byte[8192]; var saved = 0; var truncated = false; const int limit = 1024 * 1024;
            while (true)
            {
                var count = await input.ReadAsync(buffer); if (count == 0) break;
                var keep = Math.Min(count, limit - saved);
                if (keep > 0) { await output.WriteAsync(buffer.AsMemory(0, keep)); saved += keep; }
                if (keep != count && !truncated) { await output.WriteAsync("\n[log truncated; remaining output is drained]\n"u8.ToArray()); truncated = true; }
            }
        }
        catch { failure.Cancel(); throw; }
    }
}
