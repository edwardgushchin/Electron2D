using System.Diagnostics;

internal static class OpenSSLOracle
{
    internal static Process StartOracle(string[] args, out Task<string> error)
    {
        var start = new ProcessStartInfo("openssl") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in args) start.ArgumentList.Add(argument);
        var process = Process.Start(start) ?? throw new IOException("OpenSSL oracle did not start.");
        error = process.StandardError.ReadToEndAsync(); return process;
    }
    internal static void CheckOracle(Process process, Task<string> error)
    {
        if (process.HasExited && process.ExitCode != 0) throw new IOException("OpenSSL oracle failed: " + error.GetAwaiter().GetResult());
    }
    internal static void StopOracle(Process process)
    {
        if (!process.HasExited) process.Kill(true);
        if (!process.WaitForExit(3000)) throw new TimeoutException("Independent OpenSSL oracle did not exit.");
    }
}
