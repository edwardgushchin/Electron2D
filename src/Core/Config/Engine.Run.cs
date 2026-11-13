using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class Engine
{
    private int _applicationRun;
    private int _maxFpsOverride = -1;
    private int _windowMaxFps;
    private string _writeMoviePath = "";
    internal bool IsRuntimeIdle => Volatile.Read(ref _runtimeState) == RuntimeIdle;

    internal bool OwnsWindowRun => Volatile.Read(ref _applicationRun) != 0 && Volatile.Read(ref _runtimeState) != RuntimeStopping;

    internal void AttachConstructingTree(SceneTree tree) => Volatile.Write(ref _mainLoop, tree);

    internal int MaxFPSCore
    {
        get
        {
            var explicitLimit = Volatile.Read(ref _maxFpsOverride);
            return explicitLimit >= 0 ? explicitLimit : Volatile.Read(ref _windowMaxFps);
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            Volatile.Write(ref _maxFpsOverride, value);
        }
    }

    internal int RunCore(Window window, Action<Window, ulong>? beforeFrame = null, bool runProject = false)
    {
        if (OperatingSystem.IsBrowser() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
            throw new NotSupportedException("This platform requires Engine.RunAsync so its application event loop remains available.");
        return RunWindowCore(window, asynchronous: false, beforeFrame: beforeFrame, runProject: runProject).GetAwaiter().GetResult();
    }

    internal Task<int> RunAsyncCore(Window window, Action<Window, ulong>? beforeFrame = null, bool runProject = false)
    {
        ValidateRunWindow(window);
#if ANDROID
        if (OperatingSystem.IsAndroid())
            return AndroidWindowHost.RunAsync(window, beforeFrame: beforeFrame, runProject: runProject);
#endif
        if (!OperatingSystem.IsBrowser() && SynchronizationContext.Current is null)
            throw new InvalidOperationException("RunAsync requires an application synchronization context on the native main thread.");
        return RunWindowCore(window, asynchronous: true, beforeFrame: beforeFrame, runProject: runProject);
    }

    private static Node AttachProjectScene(Window window, string path)
    {
        var template = ResourceLoader.LoadSceneTemplate(path, out var retention);
        using (retention)
        {
            var scene = template.Instantiate();
            try
            {
                window.AddChild(scene);
                if (!ReferenceEquals(scene.Parent, window))
                    throw new InvalidOperationException("The project scene was detached during startup.");
                return scene;
            }
            catch (Exception error)
            {
                if (scene.Parent is null && !scene.IsDisposed)
                {
                    try { scene.Dispose(); }
                    catch (Exception cleanupError) { throw new AggregateException(error, cleanupError); }
                }
                throw;
            }
        }
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

    internal int RunReservedWindowCore(Window window, string? moviePath = null, int movieFPS = 60, bool movieDisableVSync = false, Action<Window, ulong>? beforeFrame = null, bool runProject = false) =>
        RunWindowCore(window, asynchronous: false, reserved: true, moviePath: moviePath, movieFPS: movieFPS, movieDisableVSync: movieDisableVSync, beforeFrame: beforeFrame, runProject: runProject).GetAwaiter().GetResult();

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
        RuntimeDiagnostics.Instance.EndRun();
        Volatile.Write(ref _windowMaxFps, 0);
        Volatile.Write(ref _writeMoviePath, "");
        Volatile.Write(ref _mainLoop, null);
        OS.SetMainThreadOwner(0);
        Volatile.Write(ref _runtimeOwnerThreadId, 0);
        Volatile.Write(ref _applicationRun, 0);
        Volatile.Write(ref _runtimeState, RuntimeIdle);
    }

    private async Task<int> RunWindowCore(Window window, bool asynchronous, bool reserved = false, string? moviePath = null, int movieFPS = 60, bool movieDisableVSync = false, Action<Window, ulong>? beforeFrame = null, bool runProject = false)
    {
        if (!reserved)
        {
            ValidateRunWindow(window);
            ReserveWindowRun();
        }
        if (Interlocked.CompareExchange(ref _runtimeOwnerThreadId, Environment.CurrentManagedThreadId, 0) != 0)
            throw new InvalidOperationException("The window run already has an owner thread.");
        OS.SetMainThreadOwner(Environment.CurrentManagedThreadId);
        Volatile.Write(ref _writeMoviePath, moviePath ?? "");
        SceneTree? tree = null;
        MovieRecording? movie = null;
        List<Exception> failures = [];
        try
        {
            if (reserved) ValidateRunWindow(window);
            var frameDelay = ConfigureFrameRateSettings(windowed: true) / 1000d;
            if (moviePath is not null) frameDelay = 0;
            RuntimeDiagnostics.Instance.BeginRun();
            var mainScenePath = runProject ? ProjectSettings.GetWithOverride(ProjectSettings.MainScene) : null;
            if (mainScenePath is { Length: 0 })
                throw new InvalidOperationException("Configure a project main scene before running the project.");
            if (OperatingSystem.IsBrowser() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
                SDL3.SDL.SetMainReady();
            TranslationServer.LoadProjectLocalization();
            ThemeDB.LoadProjectTheme();
            AudioServer.LoadDefaultLayout();
            Input.IgnoreJoypadOnUnfocusedApplication = ProjectSettings.GetWithOverride(ProjectSettings.IgnoreJoypadOnUnfocusedApplication);
            OS.DeltaSmoothing = ProjectSettings.GetWithOverride(ProjectSettings.DeltaSmoothing);
            OS.LowProcessorUsageMode = ProjectSettings.GetWithOverride(ProjectSettings.LowProcessorMode);
            OS.LowProcessorUsageModeSleepUsec = ProjectSettings.GetWithOverride(ProjectSettings.LowProcessorModeSleepUsec);
            window.ApplyProjectHDROutput();
            window.OpenNative(movieDisableVSync);
            window.ApplyProjectMSAA();
            window.ApplyProjectScreenSpaceAA();
            window.ApplyProjectMIPMapBias();
            window.ApplyProjectMeshLODThreshold();
            window.ApplyProjectHDR();
            window.ApplyProjectDebanding();
            if (moviePath is not null)
            {
                movie = new MovieRecording(window.TextureDimensions, movieFPS, moviePath);
            }
            var mainScene = mainScenePath is null ? null : AttachProjectScene(window, mainScenePath);
            tree = new SceneTree(window, attachToEngine: true, initialScene: mainScene);
            Volatile.Write(ref _mainLoop, tree);
            tree.StartForEngine();
            ResetRunState();
            Volatile.Write(ref _runtimeState, RuntimeRunning);
            using var frameTimer = asynchronous ? new PeriodicTimer(TimeSpan.FromMilliseconds(1)) : null;
            var lastFrame = Stopwatch.GetTimestamp();
            ulong nextFrame = 0;
            while (!tree.QuitRequested)
            {
                window.PumpEvents();
                if (tree.QuitRequested)
                    break;
                beforeFrame?.Invoke(window, checked(++nextFrame));
                if (tree.QuitRequested) break;
                var now = Stopwatch.GetTimestamp();
                var elapsed = movie is null ? Stopwatch.GetElapsedTime(lastFrame, now).TotalSeconds : 1d / movieFPS;
                lastFrame = now;
                var simulationElapsed = elapsed;
                if (movie is null && elapsed <= 1d && OS.DeltaSmoothing && DisplayServer.WindowGetVSyncMode() == DisplayServer.VSyncMode.Enabled)
                {
                    var smoothed = _frameDeltaSmoother.Smooth((long)(elapsed * 1_000_000d), true, true, out var applied);
                    if (applied) simulationElapsed = smoothed / 1_000_000d;
                }
                var stopRequested = AdvanceFrameCore(simulationElapsed, out var renderStep, fixedFPS: movie is null ? 0 : movieFPS, deferFrameTiming: true, diagnosticElapsed: elapsed);
                if (stopRequested && movie is null)
                {
                    CompleteFrameTiming(elapsed);
                    break;
                }
                window.Render(tree, renderStep);
                CompleteFrameTiming(elapsed);
                movie?.Write(window);
                if (stopRequested) break;
                var waitStart = Stopwatch.GetTimestamp();
                while (!tree.QuitRequested)
                {
                    var limit = movie is null ? MaxFPSCore : 0;
                    var lowProcessorDelay = movie is null && OS.LowProcessorUsageMode ? OS.LowProcessorUsageModeSleepUsec / 1_000_000d : 0d;
                    var postFrameWait = Math.Max(frameDelay, lowProcessorDelay);
                    if (postFrameWait > 0d) postFrameWait -= Stopwatch.GetElapsedTime(waitStart).TotalSeconds;
                    var remaining = Math.Max(postFrameWait, limit == 0 ? 0d : 1d / limit - Stopwatch.GetElapsedTime(lastFrame).TotalSeconds);
                    if (remaining <= 0d && !asynchronous)
                        break;
                    var delay = (int)Math.Clamp(Math.Ceiling(remaining * 1000d), 1d, 10d);
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
            try { OS.CloseMIDIInputs(); }
            catch (Exception error) { RuntimeDiagnostics.Instance.ReportException(error); failures.Add(error); }
            try { CameraServer.CloseForEngine(); }
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
