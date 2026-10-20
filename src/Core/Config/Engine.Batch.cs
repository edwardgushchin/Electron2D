namespace Electron2D;

public sealed partial class Engine
{
    private static Action<Window, ulong> RequireFrameCallback(Action<Window, ulong> callback)
    { ArgumentNullException.ThrowIfNull(callback); return callback; }

    /// <summary>Runs a native root with a host input boundary before each scheduler step.</summary>
    /// <param name="beforeFrame">Owner-thread callback receiving the root and the one-based next process-frame index.</param>
    /// <param name="window">A detached root whose ownership follows Run(Window).</param>
    /// <returns>The scene quit code after all owned cleanup.</returns>
    /// <remarks>The callback runs after native event pumping and complete scene readiness, outside scene callbacks
    /// and before physics/process. Input.ParseInputEvent commits and delivers before the first step. A quit request
    /// skips the pending step. Callback failures follow ordinary owned cleanup. Manual lifecycle reentry rejects.
    /// The callback adds no hidden frame, thread or wait; real-time elapsed input includes its duration.</remarks>
    /// <exception cref="ArgumentNullException">The callback or window is null.</exception>
    /// <exception cref="Exception">A Run precondition, callback, native operation or cleanup fails.</exception>
    public static int Run(Action<Window, ulong> beforeFrame, Window window) => Service.RunCore(window, RequireFrameCallback(beforeFrame));

    /// <summary>Runs an asynchronous native root with a host input boundary before each scheduler step.</summary>
    /// <param name="beforeFrame">Owner-thread callback receiving the root and one-based next process-frame index.</param>
    /// <param name="window">A detached root whose ownership follows RunAsync(Window).</param>
    /// <returns>The scene quit code after all owned cleanup.</returns>
    /// <remarks>Shares the synchronous host callback ordering and RunAsync's platform synchronization requirements.
    /// Android retains the callback through pending startup and releases it on cancellation or terminal cleanup.</remarks>
    /// <exception cref="ArgumentNullException">The callback or window is null.</exception>
    /// <exception cref="Exception">A RunAsync precondition, callback, native operation or cleanup fails.</exception>
    public static Task<int> RunAsync(Action<Window, ulong> beforeFrame, Window window) => Service.RunAsyncCore(window, RequireFrameCallback(beforeFrame));

    /// <summary>Records fixed-duration frames with host input committed before each scheduler step.</summary>
    /// <param name="beforeFrame">Owner-thread callback receiving the root and one-based next process-frame index.</param>
    /// <param name="window">A detached root consumed by the recording host.</param>
    /// <param name="moviePath">Output accepted by a registered MovieWriter.</param>
    /// <param name="fps">Positive fixed recording frames per second, initially 60.</param>
    /// <returns>The scene quit code after writer, scene and native cleanup.</returns>
    /// <remarks>Uses the ordinary recording Run lifecycle and fixed cadence. The callback runs after native input
    /// pumping and before physics/process, including the first frame; its wall time does not change fixed deltas.</remarks>
    /// <exception cref="ArgumentNullException">The callback or window is null.</exception>
    /// <exception cref="Exception">A recording precondition, callback, writer or cleanup fails.</exception>
    public static int Run(Action<Window, ulong> beforeFrame, Window window, string moviePath, int fps = 60) => RunRecordingCore(window, moviePath, fps, RequireFrameCallback(beforeFrame));

    /// <summary>Records asynchronous fixed-duration frames with a host input boundary.</summary>
    /// <param name="beforeFrame">Owner-thread callback receiving the root and one-based next process-frame index.</param>
    /// <param name="window">A detached root consumed by the recording host.</param>
    /// <param name="moviePath">Output accepted by a registered MovieWriter.</param>
    /// <param name="fps">Positive fixed recording frames per second, initially 60.</param>
    /// <returns>The scene quit code after all owned cleanup.</returns>
    /// <remarks>Shares recording and asynchronous platform requirements; no callback executes before scene readiness.</remarks>
    /// <exception cref="ArgumentNullException">The callback or window is null.</exception>
    /// <exception cref="Exception">A recording precondition, callback, writer or cleanup fails.</exception>
    public static Task<int> RunAsync(Action<Window, ulong> beforeFrame, Window window, string moviePath, int fps = 60) => RunRecordingAsyncCore(window, moviePath, fps, RequireFrameCallback(beforeFrame));

    /// <summary>Records the configured movie with a host input boundary before every scheduler step.</summary>
    /// <param name="beforeFrame">Owner-thread callback receiving the root and one-based next process-frame index.</param>
    /// <param name="window">A detached root consumed by the recording host.</param>
    /// <returns>The scene quit code after all owned cleanup.</returns>
    /// <remarks>Samples the same path and FPS settings as RunMovie(Window), retaining the callback ordering above.</remarks>
    /// <exception cref="ArgumentNullException">The callback or window is null.</exception>
    /// <exception cref="Exception">Configuration, recording, callbacks or cleanup fail.</exception>
    public static int RunMovie(Action<Window, ulong> beforeFrame, Window window) => Run(beforeFrame, window, ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterFile), ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterFPS));

    /// <summary>Records the configured asynchronous movie with a host input boundary.</summary>
    /// <param name="beforeFrame">Owner-thread callback receiving the root and one-based next process-frame index.</param>
    /// <param name="window">A detached root consumed by the recording host.</param>
    /// <returns>The scene quit code after all owned cleanup.</returns>
    /// <remarks>Samples RunMovieAsync(Window)'s configuration and retains the asynchronous recording contract.</remarks>
    /// <exception cref="ArgumentNullException">The callback or window is null.</exception>
    /// <exception cref="Exception">Configuration, recording, callbacks or cleanup fail.</exception>
    public static Task<int> RunMovieAsync(Action<Window, ulong> beforeFrame, Window window) => RunAsync(beforeFrame, window, ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterFile), ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterFPS));
}
