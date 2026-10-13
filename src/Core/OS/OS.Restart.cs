namespace Electron2D;

public sealed partial class OS
{
    private bool _restartOnExit, _restartHookRegistered, _restartShutdown;
    private string[] _restartArguments = [];

    /// <summary>Configures a single independent application restart when this desktop process exits normally.</summary>
    /// <param name="restart">Whether to launch another instance during normal managed process shutdown.</param>
    /// <param name="arguments">Copied literal application arguments; omitted arguments start a fresh instance with no application arguments.</param>
    /// <remarks>The disabled default is process-local. Configuration replaces the previous flag and arguments atomically;
    /// disabling does not discard the supplied argument snapshot. Engine.Run and borrowed loop teardown do not exit
    /// the host process. A standalone host must return or exit normally after its loop finishes. Restart uses
    /// CreateInstance, including the entry assembly for a framework-dependent host. A crash or forced termination
    /// does not guarantee managed shutdown. An attempted request clears its flag and argument list. Launch failure is reported to standard error and preserves the original exit code.</remarks>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">An argument contains NUL.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">An argument is not valid Unicode.</exception>
    /// <exception cref="InvalidOperationException">Normal process shutdown has already captured the request.</exception>
    /// <exception cref="PlatformNotSupportedException">The host has no desktop process service.</exception>
    public static void SetRestartOnExit(bool restart, ReadOnlySpan<string> arguments = default)
    {
        DesktopProcesses();
        var copy = arguments.ToArray(); ValidateProcessInput("restart", copy);
        lock (Service._processGate)
        {
            if (Service._restartShutdown) throw new InvalidOperationException("Application restart configuration is already captured for shutdown.");
            if (!Service._restartHookRegistered)
            {
                AppDomain.CurrentDomain.ProcessExit += Service.RestartApplicationOnExit;
                Service._restartHookRegistered = true;
            }
            Service._restartArguments = copy; Service._restartOnExit = restart;
        }
    }

    /// <summary>Gets whether a desktop application restart is requested for normal process exit.</summary>
    /// <returns>False initially; otherwise the most recently committed restart flag.</returns>
    /// <exception cref="PlatformNotSupportedException">The host has no desktop process service.</exception>
    public static bool IsRestartOnExitSet()
    {
        DesktopProcesses(); lock (Service._processGate) return Service._restartOnExit;
    }

    /// <summary>Copies the literal application arguments retained for restart.</summary>
    /// <returns>A caller-owned array, initially empty; available even while restart is disabled.</returns>
    /// <exception cref="PlatformNotSupportedException">The host has no desktop process service.</exception>
    public static string[] GetRestartOnExitArguments()
    {
        DesktopProcesses(); lock (Service._processGate) return (string[])Service._restartArguments.Clone();
    }

    private void RestartApplicationOnExit(object? sender, EventArgs args)
    {
        string[] arguments;
        lock (_processGate)
        {
            if (_restartShutdown) return;
            _restartShutdown = true;
            if (!_restartOnExit) return;
            arguments = _restartArguments;
        }
        try { CreateInstance(arguments); }
        catch (Exception error)
        {
            try { Console.Error.WriteLine("Application restart failed: " + error.Message); }
            catch { }
        }
        finally
        {
            lock (_processGate) { _restartOnExit = false; _restartArguments = []; }
        }
    }
}
