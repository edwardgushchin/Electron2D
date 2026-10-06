using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class Engine
{
    private int _applicationRun;
    private int _maxFps;

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
        if (Interlocked.CompareExchange(ref _runtimeState, RuntimeStarting, RuntimeIdle) != RuntimeIdle)
            throw new InvalidOperationException("The engine already has an active MainLoop lifecycle.");
        Volatile.Write(ref _applicationRun, 1);
    }

    internal int RunReservedWindowCore(Window window) =>
        RunWindowCore(window, asynchronous: false, reserved: true).GetAwaiter().GetResult();

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
        Volatile.Write(ref _mainLoop, null);
        Volatile.Write(ref _runtimeOwnerThreadId, 0);
        Volatile.Write(ref _applicationRun, 0);
        Volatile.Write(ref _runtimeState, RuntimeIdle);
    }

    private async Task<int> RunWindowCore(Window window, bool asynchronous, bool reserved = false)
    {
        if (!reserved)
        {
            ValidateRunWindow(window);
            ReserveWindowRun();
        }
        if (Interlocked.CompareExchange(ref _runtimeOwnerThreadId, Environment.CurrentManagedThreadId, 0) != 0)
            throw new InvalidOperationException("The window run already has an owner thread.");
        SceneTree? tree = null;
        List<Exception> failures = [];
        try
        {
            if (reserved) ValidateRunWindow(window);
            if (OperatingSystem.IsBrowser() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
                SDL3.SDL.SetMainReady();
            TranslationServer.LoadProjectLocalization();
            AudioServer.LoadDefaultLayout();
            Input.IgnoreJoypadOnUnfocusedApplication = ProjectSettings.GetWithOverride(ProjectSettings.IgnoreJoypadOnUnfocusedApplication);
            window.OpenNative();
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
                var elapsed = Stopwatch.GetElapsedTime(lastFrame, now).TotalSeconds;
                lastFrame = now;
                if (AdvanceFrameCore(elapsed, out var renderStep))
                    break;
                window.Render(tree, renderStep);
                while (!tree.QuitRequested)
                {
                    var limit = MaxFPSCore;
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
            failures.Add(error);
        }
        finally
        {
            // Keep the engine reserved until user teardown callbacks and native release have finished.
            Volatile.Write(ref _runtimeState, RuntimeStopping);
            try { if (tree is not null) tree.Dispose(); else window.Dispose(); }
            catch (Exception error) { failures.Add(error); }
            try { AudioServer.CloseForEngine(); }
            catch (Exception error) { failures.Add(error); }
            try { window.CloseNative(); }
            catch (Exception error) { failures.Add(error); }
            ReleaseWindowRun();
        }

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException("Window run or cleanup failed.", failures);
        return tree?.ExitCode ?? 0;
    }
}
