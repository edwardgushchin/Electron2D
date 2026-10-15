using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class Engine
{
    private int _applicationRun;
    private int _maxFps;
    private string _writeMoviePath = "";
    internal bool IsRuntimeIdle => Volatile.Read(ref _runtimeState) == RuntimeIdle;

    internal bool OwnsWindowRun => Volatile.Read(ref _applicationRun) != 0 && Volatile.Read(ref _runtimeState) != RuntimeStopping;

    internal void AttachConstructingTree(SceneTree tree) => Volatile.Write(ref _mainLoop, tree);

    internal int MaxFPSCore
    {
        get => Volatile.Read(ref _maxFps);
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); Volatile.Write(ref _maxFps, value); }
    }

    internal int RunCore(Window window)
    {
        if (OperatingSystem.IsBrowser() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
            throw new NotSupportedException("This platform requires Engine.RunAsync so its application event loop remains available.");
        return RunWindowCore(window, asynchronous: false).GetAwaiter().GetResult();
    }

    internal Task<int> RunAsyncCore(Window window)
    {
        ValidateRunWindow(window);
#if ANDROID
        if (OperatingSystem.IsAndroid())
            return AndroidWindowHost.RunAsync(window);
#endif
        if (!OperatingSystem.IsBrowser() && SynchronizationContext.Current is null)
            throw new InvalidOperationException("RunAsync requires an application synchronization context on the native main thread.");
        return RunWindowCore(window, asynchronous: true);
    }

    private static void ValidateRunWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        ObjectDisposedException.ThrowIf(window.IsDisposed, window);
        Node.EnsureSceneFactoryComplete();
        window.EnsureSceneActivationAvailable();
        if (window.Parent is not null || window.Tree is not null || window.IsQueuedForDeletion)
            throw new InvalidOperationException("Run requires a detached window that is not queued for deletion.");
    }

    internal void ReserveWindowRun()
    {
        MovieWriter.EnsureStartupAllowed();
        if (Interlocked.CompareExchange(ref _runtimeState, RuntimeStarting, RuntimeIdle) != RuntimeIdle)
            throw new InvalidOperationException("The engine already has an active MainLoop lifecycle.");
        Volatile.Write(ref _applicationRun, 1);
    }

    internal int RunReservedWindowCore(Window window, string? moviePath = null, int movieFPS = 60) =>
        RunWindowCore(window, asynchronous: false, reserved: true, moviePath: moviePath, movieFPS: movieFPS).GetAwaiter().GetResult();

    internal void CancelReservedWindowCore(Window window)
    {
        if (Volatile.Read(ref _runtimeOwnerThreadId) != 0 ||
            Interlocked.CompareExchange(ref _runtimeState, RuntimeStopping, RuntimeStarting) != RuntimeStarting)
            throw new InvalidOperationException("Only an unstarted window run can be cancelled.");
        try { window.Dispose(); }
        finally { ReleaseWindowRun(); }
    }

    private void ReleaseWindowRun()
    {
        Volatile.Write(ref _writeMoviePath, "");
        Volatile.Write(ref _mainLoop, null);
        Volatile.Write(ref _runtimeOwnerThreadId, 0);
        Volatile.Write(ref _applicationRun, 0);
        Volatile.Write(ref _runtimeState, RuntimeIdle);
    }

    private async Task<int> RunWindowCore(Window window, bool asynchronous, bool reserved = false, string? moviePath = null, int movieFPS = 60)
    {
        if (!reserved)
        {
            ValidateRunWindow(window);
            ReserveWindowRun();
        }
        if (Interlocked.CompareExchange(ref _runtimeOwnerThreadId, Environment.CurrentManagedThreadId, 0) != 0)
            throw new InvalidOperationException("The window run already has an owner thread.");
        Volatile.Write(ref _writeMoviePath, moviePath ?? "");
        SceneTree? tree = null;
        MovieRecording? movie = null;
        List<Exception> failures = [];
        try
        {
            if (reserved) ValidateRunWindow(window);
            if (OperatingSystem.IsBrowser() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
                SDL3.SDL.SetMainReady();
            TranslationServer.LoadProjectLocalization();
            ThemeDB.LoadProjectTheme();
            AudioServer.LoadDefaultLayout();
            Input.IgnoreJoypadOnUnfocusedApplication = ProjectSettings.GetWithOverride(ProjectSettings.IgnoreJoypadOnUnfocusedApplication);
            window.OpenNative();
            window.ApplyProjectMSAA();
            if (moviePath is not null)
            {
                movie = new MovieRecording(window.TextureDimensions, movieFPS, moviePath);
            }
            tree = new SceneTree(window, attachToEngine: true);
            Volatile.Write(ref _mainLoop, tree);
            tree.StartForEngine();
            ResetRunState();
            Volatile.Write(ref _runtimeState, RuntimeRunning);
            using var frameTimer = asynchronous ? new PeriodicTimer(TimeSpan.FromMilliseconds(1)) : null;
            var lastFrame = Stopwatch.GetTimestamp();
            while (!tree.QuitRequested)
            {
                window.PumpEvents();
                if (tree.QuitRequested)
                    break;
                var now = Stopwatch.GetTimestamp();
                var elapsed = movie is null ? Stopwatch.GetElapsedTime(lastFrame, now).TotalSeconds : 1d / movieFPS;
                lastFrame = now;
                if (AdvanceFrameCore(elapsed, out var renderStep, fixedFPS: movie is null ? 0 : movieFPS))
                    break;
                window.Render(tree, renderStep);
                movie?.Write(window);
                while (!tree.QuitRequested)
                {
                    var limit = movie is null ? MaxFPSCore : 0;
                    if (limit == 0 && !asynchronous)
                        break;
                    var remaining = limit == 0 ? 0d : 1d / limit - Stopwatch.GetElapsedTime(lastFrame).TotalSeconds;
                    if (remaining <= 0d && !asynchronous)
                        break;
                    var delay = Math.Clamp((int)Math.Ceiling(remaining * 1000d), 1, 10);
                    if (asynchronous)
                        await frameTimer!.WaitForNextTickAsync();
                    else
                        Thread.Sleep(delay);
                    if (Environment.CurrentManagedThreadId != Volatile.Read(ref _runtimeOwnerThreadId))
                        throw new InvalidOperationException("The application synchronization context must retain the native window thread.");
                    if (remaining <= 0d)
                        break;
                    window.PumpEvents();
                }
            }
        }
        catch (Exception error)
        {
            RuntimeDiagnostics.Instance.ReportException(error);
            failures.Add(error);
        }
        finally
        {
            // Keep the engine reserved until user teardown callbacks and native release have finished.
            Volatile.Write(ref _runtimeState, RuntimeStopping);
            try { movie?.Dispose(); }
            catch (Exception error) { RuntimeDiagnostics.Instance.ReportException(error); failures.Add(error); }
            try { if (tree is not null) tree.Dispose(); else window.Dispose(); }
            catch (Exception error) { RuntimeDiagnostics.Instance.ReportException(error); failures.Add(error); }
            try { AudioServer.CloseForEngine(); }
            catch (Exception error) { RuntimeDiagnostics.Instance.ReportException(error); failures.Add(error); }
            try { window.CloseNative(); }
            catch (Exception error) { RuntimeDiagnostics.Instance.ReportException(error); failures.Add(error); }
            ReleaseWindowRun();
        }

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException("Window run or cleanup failed.", failures);
        return tree?.ExitCode ?? 0;
    }
}
