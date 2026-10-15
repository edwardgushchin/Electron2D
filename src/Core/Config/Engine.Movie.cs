namespace Electron2D;

public sealed partial class Engine
{
    /// <summary>Records a root Window using the configured movie output path and cadence.</summary>
    /// <param name="window">A detached root owned after successful runtime reservation.</param>
    /// <returns>The scene quit code after writer, scene and native cleanup.</returns>
    /// <remarks>Samples MovieWriterFile and MovieWriterFPS with active feature overrides before startup.
    /// Setting a movie path does not enable recording for ordinary Run calls. Explicit recording Run arguments
    /// take precedence over these settings. Shares the recording Run lifecycle and failure contract.</remarks>
    /// <exception cref="ArgumentException">The configured movie file is blank.</exception>
    /// <exception cref="Exception">A recording Run precondition, lifecycle or output operation fails.</exception>
    public static int RunMovie(Window window) => Run(window,
        ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterFile), ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterFPS));

    /// <summary>Records a configured movie while yielding to the application synchronization context.</summary>
    /// <param name="window">A detached root owned after successful runtime reservation.</param>
    /// <returns>The quit code after all owned cleanup.</returns>
    /// <remarks>Samples the same project settings as RunMovie. Shares RunAsync's native-main-thread requirements.</remarks>
    /// <exception cref="ArgumentException">The configured movie file is blank.</exception>
    /// <exception cref="Exception">A recording RunAsync precondition, lifecycle or output operation fails.</exception>
    public static Task<int> RunMovieAsync(Window window) => RunAsync(window,
        ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterFile), ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterFPS));

    /// <summary>Runs and consumes a root Window while recording fixed-duration rendered frames and synchronous audio.</summary>
    /// <param name="window">A live detached root, owned after successful runtime reservation.</param>
    /// <param name="moviePath">Output path accepted by a registered MovieWriter; .png selects PNG/WAV; .avi selects MJPEG/PCM; .ogv selects Theora/Vorbis.</param>
    /// <param name="fps">Positive recording frames per second, initially 60.</param>
    /// <returns>The SceneTree quit code after writer, scene and native cleanup.</returns>
    /// <remarks>Uses the ordinary window lifecycle, process/physics scheduler and actual renderer. Process delta is
    /// 1/fps before TimeScale. Audio mixes synchronously without an output device. MaxFPS waiting is disabled.
    /// MovieWriterDisableVSync is sampled before startup and applied before renderer creation.
    /// Hook failures abort and cleanup is attempted. Readback/encoding may allocate and block. Other Run ownership rules apply.</remarks>
    /// <exception cref="ArgumentException">The output path is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">FPS is not positive.</exception>
    /// <exception cref="NotSupportedException">No writer accepts the path, or the platform needs RunAsync.</exception>
    /// <exception cref="Exception">A lifecycle, writer, rendering or audio operation fails.</exception>
    public static int Run(Window window, string moviePath, int fps = 60)
    {
        ValidateMovieArguments(moviePath, fps);
        if (OperatingSystem.IsBrowser() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS()) throw new NotSupportedException("This platform requires RunAsync.");
        return Service.RunWindowCore(window, asynchronous: false, moviePath: moviePath, movieFPS: fps, movieDisableVSync: ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterDisableVSync)).GetAwaiter().GetResult();
    }
    /// <summary>Records a root Window while yielding to its application synchronization context.</summary>
    /// <param name="window">A detached root owned after startup reservation.</param>
    /// <param name="moviePath">Output accepted by a registered writer.</param><param name="fps">Positive recording cadence.</param>
    /// <returns>The quit code after all owned cleanup.</returns>
    /// <remarks>Shares RunAsync's main-thread requirements and the recording Run overload's frame/audio contract.</remarks>
    /// <exception cref="ArgumentException">The path is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">FPS is not positive.</exception>
    /// <exception cref="InvalidOperationException">No application synchronization context retains the native main thread.</exception>
    /// <exception cref="Exception">A lifecycle, writer, rendering or audio operation fails.</exception>
    public static Task<int> RunAsync(Window window, string moviePath, int fps = 60)
    {
        ValidateMovieArguments(moviePath, fps); ValidateRunWindow(window);
#if ANDROID
        if (OperatingSystem.IsAndroid()) return AndroidWindowHost.RunAsync(window, moviePath, fps, ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterDisableVSync));
#endif
        if (!OperatingSystem.IsBrowser() && SynchronizationContext.Current is null) throw new InvalidOperationException("RunAsync requires an application synchronization context.");
        return Service.RunWindowCore(window, asynchronous: true, moviePath: moviePath, movieFPS: fps, movieDisableVSync: ProjectSettings.GetWithOverride(ProjectSettings.MovieWriterDisableVSync));
    }
    /// <summary>Returns the requested output path during a movie Window lifecycle.</summary>
    /// <returns>The unchanged path, or an empty string outside recording startup/teardown.</returns>
    public static string GetWriteMoviePath() => Volatile.Read(ref Service._writeMoviePath);
    private static void ValidateMovieArguments(string path, int fps)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path); ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fps);
    }
}
