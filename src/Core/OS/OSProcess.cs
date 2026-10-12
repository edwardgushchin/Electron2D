using System.Diagnostics;
using SDL3;

namespace Electron2D;

internal sealed class OSProcess
{
    internal readonly object Gate = new();
    internal nint Native;
    internal readonly int PID;
    private Process? _managed;
    private int _pipes;
    internal int? ExitCode;
    internal OSProcess(Process process) { _managed = process; PID = process.Id; }
    internal OSProcess(nint native) { Native = native; PID = checked((int)SDL.GetNumberProperty(SDL.GetProcessProperties(native), SDL.Props.ProcessPIDNumber, 0)); if (PID <= 0) throw new IOException("The native child has no process ID."); }
    internal bool Poll()
    {
        lock (Gate)
        {
            if (ExitCode is null)
            {
                if (_managed is { HasExited: true } process) { ExitCode = process.ExitCode; process.Dispose(); _managed = null; }
                else if (Native != 0)
                {
                    if (OperatingSystem.IsWindows()) { if (SDL.WaitProcess(Native, false, out var code)) ExitCode = code; }
                    else if (ProcessPlatform.ObserveExit(PID, out var status)) { if (!SDL.WaitProcess(Native, false, out _)) throw new IOException("Cannot reap owned child: " + SDL.GetError()); ExitCode = status; }
                }
            }
            ReleaseCompleted(); return ExitCode is not null;
        }
    }
    internal void Kill()
    {
        lock (Gate) { if (Poll()) throw new ArgumentException("The child process has exited."); if (OperatingSystem.IsWindows()) { if (_managed is not null) ProcessPlatform.Kill(_managed); else { using var process = Process.GetProcessById(PID); ProcessPlatform.Kill(process); } } else if (!SDL.KillProcess(Native, true)) throw new IOException(SDL.GetError()); }
    }
    internal void AcquirePipe() { lock (Gate) _pipes++; }
    internal void ReleasePipe() { lock (Gate) { _pipes--; Poll(); } }
    private void ReleaseCompleted() { if (ExitCode is not null && _pipes == 0 && Native != 0) { SDL.DestroyProcess(Native); Native = 0; } }
    internal void Abandon() { lock (Gate) { if (Native != 0) { SDL.KillProcess(Native, true); SDL.WaitProcess(Native, true, out _); SDL.DestroyProcess(Native); Native = 0; } _managed?.Dispose(); _managed = null; } }
}
