using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using SDL3;

namespace Electron2D;

public sealed partial class OS
{
    private readonly object _processGate = new();
    private readonly Dictionary<int, OSProcess> _processes = [];
    private static readonly UTF8Encoding ProcessUTF8 = new(false, true);
    /// <summary>Starts an independent executable with literal ordered arguments.</summary>
    /// <param name="path">An executable path or platform-search name; virtual paths resolve through ProjectSettings.</param>
    /// <param name="arguments">Arguments passed separately, without shell interpretation.</param>
    /// <param name="openConsole">Requests a console for a Windows console application; other platforms ignore this flag.</param>
    /// <returns>The child process ID. The child survives renderer and application shutdown.</returns>
    /// <exception cref="ArgumentException">The executable or an argument contains a null character, or the path is empty.</exception>
    /// <exception cref="PlatformNotSupportedException">The host has no independent process service.</exception>
    /// <exception cref="System.ComponentModel.Win32Exception">The executable cannot be started.</exception>
    /// <exception cref="IOException">Native process creation fails.</exception>
    public static int CreateProcess(string path, ReadOnlySpan<string> arguments, bool openConsole = false) => Service.StartProcess(path, arguments, openConsole);
    /// <summary>Starts another instance of the current application with literal ordered arguments.</summary>
    /// <param name="arguments">New application arguments; current arguments are not copied.</param>
    /// <returns>The independent child process ID.</returns>
    /// <remarks>A framework-dependent host includes the current entry assembly before the supplied arguments.</remarks>
    /// <exception cref="PlatformNotSupportedException">The current application has no launchable executable/entry assembly.</exception>
    /// <remarks>An application executable relaunches directly. A dotnet host uses its named entry DLL in the application base directory and rejects a missing file before launch.</remarks>
    public static int CreateInstance(ReadOnlySpan<string> arguments)
    {
        DesktopProcesses(); var path = Environment.ProcessPath ?? throw new PlatformNotSupportedException("This host has no executable path.");
        if (!string.Equals(System.IO.Path.GetFileNameWithoutExtension(path), "dotnet", StringComparison.OrdinalIgnoreCase)) return CreateProcess(path, arguments);
        var assembly = Assembly.GetEntryAssembly()?.GetName().Name is { Length: > 0 } entry ? System.IO.Path.Combine(AppContext.BaseDirectory, entry + ".dll") : null; if (string.IsNullOrEmpty(assembly) || !File.Exists(assembly)) throw new PlatformNotSupportedException("This managed host has no launchable entry assembly.");
        var values = new string[arguments.Length + 1]; values[0] = assembly; arguments.CopyTo(values.AsSpan(1)); return CreateProcess(path, values);
    }
    /// <summary>Runs an executable, waits for exit and optionally appends one complete UTF-8 output string.</summary>
    /// <param name="path">An executable path or platform-search name.</param>
    /// <param name="arguments">Literal ordered arguments, without shell interpretation.</param>
    /// <param name="output">Receives one string after completion; existing items remain intact. Null discards captured output.</param>
    /// <param name="readStderr">Includes standard error in captured output; otherwise it is drained and discarded.</param>
    /// <param name="openConsole">Requests a Windows console; other platforms ignore this flag.</param>
    /// <returns>The executable's actual exit code, including nonzero exit codes.</returns>
    /// <remarks>Blocks the caller and allocates captured output. Capture drains both streams concurrently to avoid pipe deadlock;
    /// POSIX capture merges stdout/stderr on one native pipe in write order. Use ExecuteWithPipe for independent channels.</remarks>
    /// <exception cref="ArgumentException">The launch input is invalid.</exception>
    /// <exception cref="System.ComponentModel.Win32Exception">The executable cannot be started.</exception>
    /// <exception cref="IOException">The native process or pipe operation fails.</exception>
    /// <exception cref="PlatformNotSupportedException">A required host or shared-output adapter is absent.</exception>
    public static int Execute(string path, ReadOnlySpan<string> arguments, ICollection<string>? output = null, bool readStderr = false, bool openConsole = false)
    {
        if (!OperatingSystem.IsWindows()) return Service.ExecuteCaptured(path, arguments, output, readStderr);
        // shortcut: enable merged Windows capture after its shared-output and new-console launch adapter executes.
        if (readStderr && OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Merged console capture requires the Windows shared-output process adapter.");
        var start = ProcessStart(path, arguments, openConsole); start.RedirectStandardOutput = true; start.RedirectStandardError = true;
        start.StandardOutputEncoding = Encoding.UTF8; if (start.RedirectStandardError) start.StandardErrorEncoding = Encoding.UTF8;
        using var process = Process.Start(start) ?? throw new IOException("The executable did not start.");
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.BaseStream.CopyToAsync(Stream.Null);
        process.WaitForExit(); var text = stdout.GetAwaiter().GetResult(); stderr.GetAwaiter().GetResult();
        if (output is not null) output.Add(text); return process.ExitCode;
    }
    /// <summary>Starts an independent executable with owned stdin/stdout and stderr pipe access.</summary>
    /// <param name="path">An executable path or platform-search name.</param>
    /// <param name="arguments">Literal ordered arguments, without shell interpretation.</param>
    /// <param name="blocking">Whether empty reads and full writes wait on the calling thread.</param>
    /// <returns>An owned typed result containing the process ID and two FileAccess streams.</returns>
    /// <remarks>Closing pipes never kills the child. Nonblocking ReadBytes returns available bytes and distinguishes temporary
    /// emptiness from EOF. A partial nonblocking write throws PipeWriteException with the consumed byte count.</remarks>
    /// <exception cref="IOException">Native process creation or pipe setup fails.</exception>
    /// <exception cref="PlatformNotSupportedException">The host lacks the process/pipe backend.</exception>
    public static ProcessPipes ExecuteWithPipe(string path, ReadOnlySpan<string> arguments, bool blocking = true) => Service.StartPipes(path, arguments, blocking);
    /// <summary>Returns the current native executable path.</summary>
    /// <returns>The executable hosting this application; framework-dependent applications return their runtime host.</returns>
    /// <exception cref="PlatformNotSupportedException">The host cannot supply an executable path.</exception>
    public static string GetExecutablePath() => Environment.ProcessPath ?? throw new PlatformNotSupportedException("This host has no executable path.");
    /// <summary>Copies the application's command-line arguments, excluding the executable/entry assembly.</summary>
    /// <returns>Ordered literal arguments, including any user-argument separator.</returns>
    public static string[] GetCommandLineArgs() => Environment.GetCommandLineArgs()[1..];
    /// <summary>Copies application arguments after the first -- or ++ separator.</summary>
    /// <returns>Only user arguments, or an empty array when no separator was supplied.</returns>
    public static string[] GetCommandLineUserArgs()
    {
        var arguments = GetCommandLineArgs(); var index = Array.FindIndex(arguments, value => value is "--" or "++"); return index < 0 ? [] : arguments[(index + 1)..];
    }
    /// <summary>Returns the current application's process ID.</summary>
    /// <returns>The current native process ID.</returns>
    public static int GetProcessID() => Environment.ProcessId;
    /// <summary>Reports whether a positive native process ID is still running.</summary>
    /// <param name="pid">The process ID to inspect, including processes created elsewhere.</param>
    /// <returns>False when the process no longer exists or is a completed owned child.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The ID is not positive.</exception>
    public static bool IsProcessRunning(int pid) => Service.ProcessRunning(pid);
    /// <summary>Returns a captured exit code for a process created by this service.</summary>
    /// <param name="pid">A positive process ID.</param>
    /// <returns>The actual exit code, or -1 while the owned child is running.</returns>
    /// <remarks>Exit history belongs to the most recently created child with that ID. POSIX abnormal termination retains
    /// native signal/core wait status, distinct from normal exit codes. Native PID reuse is possible.</remarks>
    /// <exception cref="ArgumentException">The positive ID is not an owned child.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The ID is not positive.</exception>
    public static int GetProcessExitCode(int pid) => Service.ProcessExitCode(pid);
    /// <summary>Terminates one native process without terminating its descendants.</summary>
    /// <param name="pid">A positive process ID.</param>
    /// <exception cref="ArgumentException">The process no longer exists.</exception>
    /// <exception cref="System.ComponentModel.Win32Exception">The operating system denies termination.</exception>
    /// <exception cref="IOException">The native child cannot be terminated.</exception>
    public static void Kill(int pid) => Service.KillProcess(pid);
    private static void DesktopProcesses()
    {
        if (!(OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid() || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())) throw new PlatformNotSupportedException("Independent process services require a desktop process host; mobile launch adapters are unavailable.");
    }
    private static ProcessStartInfo ProcessStart(string path, ReadOnlySpan<string> arguments, bool console)
    {
        DesktopProcesses(); ValidateProcessInput(path, arguments); if (path.Contains("://", StringComparison.Ordinal)) path = ProjectSettings.GlobalizePath(path);
        var info = new ProcessStartInfo(path) { UseShellExecute = false, CreateNoWindow = !console };
        foreach (var argument in arguments) info.ArgumentList.Add(argument); return info;
    }
    private static void ValidateProcessInput(string path, ReadOnlySpan<string> arguments)
    {
        ArgumentException.ThrowIfNullOrEmpty(path); ProcessUTF8.GetByteCount(path); if (path.Contains('\0')) throw new ArgumentException("The executable contains a null character.", nameof(path));
        foreach (var argument in arguments) { ArgumentNullException.ThrowIfNull(argument); ProcessUTF8.GetByteCount(argument); if (argument.Contains('\0')) throw new ArgumentException("An argument contains a null character.", nameof(arguments)); }
    }
    private int StartProcess(string path, ReadOnlySpan<string> arguments, bool console)
    {
        if (!OperatingSystem.IsWindows()) { var child = StartNative(path, arguments, false); try { lock (_processGate) { ReapProcesses(); _processes[child.PID] = child; } return child.PID; } catch { child.Abandon(); throw; } }
        var start = ProcessStart(path, arguments, console); var process = Process.Start(start) ?? throw new IOException("The executable did not start.");
        try { var pid = process.Id; lock (_processGate) { ReapProcesses(); _processes[pid] = new(process); } return pid; }
        catch { process.Dispose(); throw; }
    }
    private int ExecuteCaptured(string path, ReadOnlySpan<string> arguments, ICollection<string>? output, bool readError)
    {
        using var pipes = StartPipes(path, arguments, true, mergeError: readError, capture: true);
        using var bytes = new MemoryStream();
        while (true) { var data = pipes.StandardIO.ReadBytes(65536); bytes.Write(data); if (pipes.StandardIO.EOFReached) break; }
        while (ProcessRunning(pipes.PID)) Thread.Sleep(1);
        var code = ProcessExitCode(pipes.PID); output?.Add(Encoding.UTF8.GetString(bytes.GetBuffer(), 0, checked((int)bytes.Length))); return code;
    }
    private unsafe OSProcess StartNative(string path, ReadOnlySpan<string> arguments, bool pipes, bool capture = false, bool mergeError = false)
    {
        DesktopProcesses(); ValidateProcessInput(path, arguments); if (path.Contains("://", StringComparison.Ordinal)) path = ProjectSettings.GlobalizePath(path);
        var pointers = new nint[checked(arguments.Length + 2)]; uint properties = 0; nint native = 0;
        try
        {
            pointers[0] = Marshal.StringToCoTaskMemUTF8(path); for (var i = 0; i < arguments.Length; i++) pointers[i + 1] = Marshal.StringToCoTaskMemUTF8(arguments[i]);
            properties = SDL.CreateProperties(); if (properties == 0) throw new IOException(SDL.GetError());
            fixed (nint* argv = pointers)
            {
                var input = pipes && !capture ? SDL.ProcessIO.App : SDL.ProcessIO.Inherited;
                var output = pipes ? SDL.ProcessIO.App : SDL.ProcessIO.Inherited;
                var error = pipes ? capture ? SDL.ProcessIO.Null : SDL.ProcessIO.App : SDL.ProcessIO.Inherited;
                if (!SDL.SetPointerProperty(properties, SDL.Props.ProcessCreateArgsPointer, (nint)argv) || !SDL.SetNumberProperty(properties, SDL.Props.ProcessCreateSTDInNumber, (long)input) || !SDL.SetNumberProperty(properties, SDL.Props.ProcessCreateSTDOutNumber, (long)output) || !(mergeError ? SDL.SetBooleanProperty(properties, SDL.Props.ProcessCreateSTDErrToSTDOutBoolean, true) : SDL.SetNumberProperty(properties, SDL.Props.ProcessCreateSTDErrNumber, (long)error))) throw new IOException(SDL.GetError());
                lock (_environmentGate) native = SDL.CreateProcessWithProperties(properties); if (native == 0) throw new IOException("Cannot start executable: " + SDL.GetError());
            }
            var owner = new OSProcess(native); native = 0; return owner;
        }
        finally { if (native != 0) { SDL.KillProcess(native, true); SDL.WaitProcess(native, true, out _); SDL.DestroyProcess(native); } if (properties != 0) SDL.DestroyProperties(properties); foreach (var pointer in pointers) if (pointer != 0) Marshal.FreeCoTaskMem(pointer); }
    }
    private ProcessPipes StartPipes(string path, ReadOnlySpan<string> arguments, bool blocking, bool mergeError = false, bool capture = false)
    {
        var owner = StartNative(path, arguments, true, capture, mergeError); var pid = owner.PID; ProcessPipeStream? io = null; FileAccess? stdio = null, stderr = null;
        try
        {
            io = new(owner, SDL.GetProcessOutput(owner.Native), capture ? 0 : SDL.GetProcessInput(owner.Native), blocking);
            stdio = FileAccess.FromProcessPipe($"pipe://{pid}/stdio", io, capture ? FileAccessModeFlags.Read : FileAccessModeFlags.ReadWrite);
            stderr = capture ? FileAccess.FromProcessPipe($"pipe://{pid}/stderr", new MemoryStream(), FileAccessModeFlags.Read) : FileAccess.FromProcessPipe($"pipe://{pid}/stderr", new ProcessPipeStream(owner, SDL.GetPointerProperty(SDL.GetProcessProperties(owner.Native), SDL.Props.ProcessSTDErrPointer, 0), 0, blocking), FileAccessModeFlags.Read);
            var result = new ProcessPipes(pid, stdio, stderr); lock (_processGate) { ReapProcesses(); _processes[pid] = owner; }
            return result;
        }
        catch { try { stdio?.Dispose(); } finally { try { stderr?.Dispose(); } finally { io?.Dispose(); owner.Abandon(); } } throw; }
    }
    private void ReapProcesses() { foreach (var process in _processes.Values) process.Poll(); }
    private bool ProcessRunning(int pid)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pid); lock (_processGate) if (_processes.TryGetValue(pid, out var child) && !child.Poll()) return true;
        try { using var process = Process.GetProcessById(pid); return !process.HasExited; } catch (ArgumentException) { return false; }
    }
    private int ProcessExitCode(int pid) { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pid); lock (_processGate) { if (!_processes.TryGetValue(pid, out var process)) throw new ArgumentException("The process was not created by this service.", nameof(pid)); process.Poll(); return process.ExitCode ?? -1; } }
    private void KillProcess(int pid)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pid); lock (_processGate) if (_processes.TryGetValue(pid, out var child) && !child.Poll()) { child.Kill(); return; }
        using var process = Process.GetProcessById(pid); ProcessPlatform.Kill(process);
    }
}
