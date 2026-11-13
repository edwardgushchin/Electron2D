using System.Runtime.InteropServices;
using Electron2D;

if (args is ["--hold"])
{
    Native.ShowWindow(Native.GetConsoleWindow(), 0); Thread.Sleep(30000); return 0;
}
if (args is ["--exit"]) { Native.ShowWindow(Native.GetConsoleWindow(), 0); return 0; }
if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows adoption fixture.");
var stages = args is ["--raw-only"] ? new[] { 0 } : new[] { 0, 1, 2, 3 };
var path = Environment.ProcessPath!;
void Complete(int pid)
{
    var time = System.Diagnostics.Stopwatch.StartNew();
    while (OS.IsProcessRunning(pid)) { if (time.Elapsed.TotalSeconds > 8) throw new TimeoutException("Fixture child did not retire."); Thread.Sleep(1); }
}
void Retry()
{
    var pid = OS.CreateProcess(path, ["--hold"]); if (!OS.IsProcessRunning(pid)) throw new InvalidOperationException("Independent retry child did not survive successful adoption.");
    OS.Kill(pid); Complete(pid);
}
for (var i = 0; i < 3; i++) Retry();
foreach (var console in new[] { false, true })
    if (OS.Execute(path, ["--exit"], new List<string>(), true, console) != 0) throw new InvalidOperationException("Capture warmup failed.");
_ = ProcessAdoptionFault.Failure;
foreach (var capture in new[] { false, true })
    foreach (var console in new[] { false, true })
        foreach (var stage in stages)
        {
            Native.GetProcessHandleCount(-1, out var before);
            ProcessAdoptionFault.Stage = stage; var rejected = false;
            try
            {
                if (capture) OS.Execute(path, ["--hold"], new List<string>(), true, console);
                else OS.CreateProcess(path, ["--hold"], console);
            }
            catch (OutOfMemoryException error) when (ReferenceEquals(error, ProcessAdoptionFault.Failure)) { rejected = true; }
            finally { ProcessAdoptionFault.Stage = -1; }
            var processOpen = Native.GetHandleInformation(ProcessAdoptionFault.Process, out _);
            var threadOpen = Native.GetHandleInformation(ProcessAdoptionFault.Thread, out _);
            var alive = ProcessAdoptionFault.Observer == 0 || Native.WaitForSingleObject(ProcessAdoptionFault.Observer, 0) != 0;
            // The independent fixture also cleans up a deliberately failing preceding-version control.
            if (alive && ProcessAdoptionFault.Observer != 0) { Native.TerminateProcess(ProcessAdoptionFault.Observer, 0); Native.WaitForSingleObject(ProcessAdoptionFault.Observer, 8000); }
            if (processOpen) Native.CloseHandle(ProcessAdoptionFault.Process);
            if (threadOpen) Native.CloseHandle(ProcessAdoptionFault.Thread);
            if (ProcessAdoptionFault.Observer != 0) Native.CloseHandle(ProcessAdoptionFault.Observer);
            Native.GetProcessHandleCount(-1, out var after);
            Console.WriteLine($"stage={stage} capture={capture} console={console} rejected={rejected} processOpen={processOpen} threadOpen={threadOpen} childAlive={alive} handles={before}->{after}");
            if (!rejected || processOpen || threadOpen || alive || before != after) return 1;
            Retry();
        }
Console.WriteLine("Post-create adoption failure: native handles closed, unpublished children retired and independent retries passed.");
return 0;

internal static partial class Native
{
    [LibraryImport("kernel32.dll")] internal static partial nint GetConsoleWindow();
    [LibraryImport("user32.dll")] internal static partial int ShowWindow(nint window, int command);
    [LibraryImport("kernel32.dll")] internal static partial uint WaitForSingleObject(nint process, uint milliseconds);
    [LibraryImport("kernel32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static partial bool GetHandleInformation(nint handle, out uint flags);
    [LibraryImport("kernel32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static partial bool GetProcessHandleCount(nint process, out uint count);
    [LibraryImport("kernel32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static partial bool TerminateProcess(nint process, uint code);
    [LibraryImport("kernel32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static partial bool CloseHandle(nint handle);
}
