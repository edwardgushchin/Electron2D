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
        ArgumentNullException.ThrowIfNull(window);
        ObjectDisposedException.ThrowIf(window.IsDisposed, window);
        Node.EnsureSceneFactoryComplete();
        window.EnsureSceneActivationAvailable();
        if (window.Parent is not null || window.Tree is not null || window.IsQueuedForDeletion)
            throw new InvalidOperationException("Run requires a detached window that is not queued for deletion.");
        if (Interlocked.CompareExchange(ref _runtimeState, RuntimeStarting, RuntimeIdle) != RuntimeIdle)
            throw new InvalidOperationException("The engine already has an active MainLoop lifecycle.");

        Volatile.Write(ref _applicationRun, 1);
        Volatile.Write(ref _runtimeOwnerThreadId, Environment.CurrentManagedThreadId);
        SceneTree? tree = null;
        List<Exception> failures = [];
        try
        {
            TranslationServer.LoadProjectLocalization();
            Input.IgnoreJoypadOnUnfocusedApplication = ProjectSettings.GetWithOverride(ProjectSettings.IgnoreJoypadOnUnfocusedApplication);
            window.OpenNative();
            tree = new SceneTree(window, attachToEngine: true);
            Volatile.Write(ref _mainLoop, tree);
            tree.StartForEngine();
            ResetRunState();
            Volatile.Write(ref _runtimeState, RuntimeRunning);
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
                    if (limit == 0)
                        break;
                    var remaining = 1d / limit - Stopwatch.GetElapsedTime(lastFrame).TotalSeconds;
                    if (remaining <= 0d)
                        break;
                    Thread.Sleep(Math.Clamp((int)Math.Ceiling(remaining * 1000d), 1, 10));
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
            Volatile.Write(ref _mainLoop, null);
            Volatile.Write(ref _runtimeOwnerThreadId, 0);
            Volatile.Write(ref _applicationRun, 0);
            Volatile.Write(ref _runtimeState, RuntimeIdle);
        }

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException("Window run or cleanup failed.", failures);
        return tree?.ExitCode ?? 0;
    }
}
