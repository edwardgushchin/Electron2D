using Electron2D;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Electron2D.HostExample;

/// <summary>Runs one scene and main window on the calling application thread.</summary>
/// <remarks>The host owns the supplied root from <see cref="Run"/> until teardown, including failed startup.
/// It pumps native input before each frame, uses a monotonic clock for unscaled elapsed time, and never renders.</remarks>
internal sealed class ApplicationHost
{
    private readonly string _title;
    private readonly Vector2I _size;
    private readonly int _maxFramesPerSecond;
    private int _running;
    private int _exitRequested;

    /// <summary>Configures a windowed application host.</summary>
    /// <param name="title">Initial window title.</param>
    /// <param name="size">Positive initial window dimensions.</param>
    /// <param name="maxFramesPerSecond">Maximum process frames per second; zero disables waiting.</param>
    /// <exception cref="ArgumentNullException"><paramref name="title"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The size is nonpositive or the frame limit is negative.</exception>
    public ApplicationHost(string title, Vector2I size, int maxFramesPerSecond = 60)
    {
        ArgumentNullException.ThrowIfNull(title);
        if (size.X <= 0 || size.Y <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Window dimensions must be positive.");
        if (maxFramesPerSecond < 0)
            throw new ArgumentOutOfRangeException(nameof(maxFramesPerSecond));

        _title = title;
        _size = size;
        _maxFramesPerSecond = maxFramesPerSecond;
    }

    /// <summary>Requests that the active run stop after the current event pump or frame.</summary>
    /// <remarks>This method may be called from any thread. It does not interrupt a running user callback.
    /// A request made before <see cref="Run"/> starts is cleared by that run.</remarks>
    public void RequestExit() => Volatile.Write(ref _exitRequested, 1);

    /// <summary>Runs the supplied root until an exit request or a loop stop request.</summary>
    /// <param name="root">The scene root, transferred to this host even if startup fails.</param>
    /// <remarks>Startup, events, frames, finalization, and native disposal run on the calling main thread.
    /// Cleanup failures are combined with the original failure. A host instance may run again with a new root.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Another run is active, the display rejects startup, or the caller is not its main thread.</exception>
    /// <exception cref="AggregateException">Several callback or cleanup operations fail.</exception>
    public void Run(Node root)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            throw new InvalidOperationException("This application host is already running.");

        Volatile.Write(ref _exitRequested, 0);
        ConsoleCancelEventHandler cancel = (_, args) =>
        {
            args.Cancel = true;
            RequestExit();
        };
        Console.CancelKeyPress += cancel;
        DisplayServer? display = null;
        SceneTree? tree = null;
        var started = false;
        List<Exception> failures = [];

        try
        {
            display = DisplayServer.Open(_title, _size);
            display.CloseRequested += RequestExit;
            display.QuitRequested += RequestExit;
            tree = new SceneTree(root);
            Engine.Instance.Start(tree);
            started = true;

            var lastFrame = Stopwatch.GetTimestamp();
            var minimumFrameSeconds = _maxFramesPerSecond == 0 ? 0d : 1d / _maxFramesPerSecond;

            while (Volatile.Read(ref _exitRequested) == 0)
            {
                display.ProcessEvents();
                if (Volatile.Read(ref _exitRequested) != 0)
                    break;

                var now = Stopwatch.GetTimestamp();
                var elapsed = Stopwatch.GetElapsedTime(lastFrame, now).TotalSeconds;
                lastFrame = now;
                if (Engine.Instance.AdvanceFrame(elapsed))
                    break;

                while (minimumFrameSeconds > 0d && Volatile.Read(ref _exitRequested) == 0)
                {
                    var remaining = minimumFrameSeconds - Stopwatch.GetElapsedTime(lastFrame).TotalSeconds;
                    if (remaining <= 0d)
                        break;
                    Thread.Sleep(Math.Clamp((int)Math.Ceiling(remaining * 1000d), 1, 10));
                    display.ProcessEvents();
                }
            }
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (started)
                TryCleanup(Engine.Instance.Stop, failures);
            if (tree is not null)
                TryCleanup(tree.Dispose, failures);
            else if (!root.IsDisposed)
                TryCleanup(root.Dispose, failures);
            if (display is not null)
            {
                display.CloseRequested -= RequestExit;
                display.QuitRequested -= RequestExit;
                TryCleanup(display.Dispose, failures);
            }
            Console.CancelKeyPress -= cancel;
            Volatile.Write(ref _running, 0);
        }

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException("Application host or cleanup failed.", failures);
    }

    private static void TryCleanup(Action cleanup, List<Exception> failures)
    {
        try { cleanup(); }
        catch (Exception error) { failures.Add(error); }
    }
}
