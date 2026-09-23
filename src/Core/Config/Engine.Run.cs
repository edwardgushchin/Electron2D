using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class Engine
{
    private int _applicationRun;
    private int _maxFps;

    internal bool OwnsWindowRun => Volatile.Read(ref _applicationRun) != 0 && Volatile.Read(ref _runtimeState) != RuntimeStopping;

    internal void AttachConstructingTree(SceneTree tree) => Volatile.Write(ref _mainLoop, tree);

    /// <summary>Gets or sets the maximum process cadence used by Run.</summary>
    /// <value>Zero, meaning unlimited, by default; otherwise a positive number of frames per second.</value>
    /// <remarks>May change from any thread. Manual AdvanceFrame calls do not wait. Waiting uses monotonic,
    /// unscaled time and continues to pump window events at intervals of at most ten milliseconds.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The limit is negative.</exception>
    public int MaxFPS
    {
        get => Volatile.Read(ref _maxFps);
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); Volatile.Write(ref _maxFps, value); }
    }

    /// <summary>Runs a root window and its scene on the calling main thread until quit or failure.</summary>
    /// <param name="window">A live, detached, parentless window, not queued for deletion.</param>
    /// <returns>The exit code supplied by SceneTree.Quit, or zero for an automatically accepted close.</returns>
    /// <remarks>The runtime owns the window and children after validation and successful reservation of the idle
    /// engine, including failed native startup or scene activation. It opens the native window before ready, pumps
    /// events before frames, limits cadence with MaxFPS, finalizes and disposes the scene, then releases native resources.
    /// A new window may be run after successful cleanup. Native services opened directly through DisplayServer must
    /// finish before teardown; pending asynchronous dialogs can reject native disposal and the error is reported. Manual Start/AdvanceFrame/Stop cannot interfere with this run.
    /// Canvas frames are submitted after each successful process step. It does not install process-wide console or termination handlers.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="window"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The supplied window is disposed.</exception>
    /// <exception cref="InvalidOperationException">The window is not detached, another lifecycle is active, the caller is not the native main thread, or startup fails.</exception>
    /// <exception cref="AggregateException">Several callbacks or cleanup operations fail; all owned cleanup is attempted.</exception>
    /// <exception cref="Exception">A callback or platform operation fails. All owned cleanup stages are attempted before the error escapes.</exception>
    public int Run(Window window)
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
                    var limit = MaxFPS;
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
