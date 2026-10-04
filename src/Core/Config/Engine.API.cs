namespace Electron2D;

public sealed partial class Engine
{
    /// <summary>Gets or sets the maximum process cadence used by Run.</summary>
    /// <value>Zero, meaning unlimited, by default; otherwise a positive number of frames per second.</value>
    /// <remarks>May change from any thread. Manual AdvanceFrame calls do not wait. Waiting uses monotonic,
    /// unscaled time and continues to pump window events at intervals of at most ten milliseconds.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The limit is negative.</exception>
    public static int MaxFPS
    {
        get => Service.MaxFPSCore;
        set => Service.MaxFPSCore = value;
    }

    /// <summary>Runs a root window and its scene on the calling main thread until quit or failure.</summary>
    /// <param name="window">A live, detached, parentless window, not queued for deletion.</param>
    /// <returns>The exit code supplied by SceneTree.Quit, or zero for an automatically accepted close.</returns>
    /// <remarks>The runtime owns the window and children after validation and successful reservation of the idle
    /// engine, including failed native startup or scene activation. It opens the native window before ready, pumps
    /// events before frames, limits cadence with MaxFPS, finalizes and disposes the scene, then releases native resources.
    /// A new window may be run after successful cleanup. Native services opened directly through DisplayServer must
    /// finish before teardown; pending asynchronous dialogs can reject native disposal and the error is reported. Manual Start/AdvanceFrame/Stop cannot interfere with this run.
    /// Canvas frames are submitted after each successful process step. Project locale and pseudolocalization settings are sampled
    /// before native window and scene activation. It does not install process-wide console or termination handlers.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="window"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The supplied window is disposed.</exception>
    /// <exception cref="InvalidOperationException">The window is not detached, another lifecycle is active, the caller is not the native main thread, or startup fails.</exception>
    /// <exception cref="AggregateException">Several callbacks or cleanup operations fail; all owned cleanup is attempted.</exception>
    /// <exception cref="Exception">A callback or platform operation fails. All owned cleanup stages are attempted before the error escapes.</exception>
    public static int Run(Window window) => Service.RunCore(window);

    /// <summary>Gets or sets the fixed-step callback frequency.</summary>
    /// <value>The number of physics callback opportunities per unscaled second. The default is <c>60</c>.</value>
    /// <remarks>
    /// Higher values improve fixed-step precision while increasing processor cost. The fixed callback delta is
    /// <c>TimeScale / PhysicsTicksPerSecond</c>. The value is sampled once at the start of each frame. Changing it
    /// writes <see cref="ProjectSettings.PhysicsTicksPerSecond"/>, re-baselines fixed-step history on the next frame,
    /// and discards any fractional interval from the old frequency.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is less than or equal to zero.</exception>
    public static int PhysicsTicksPerSecond
    {
        get => Service.PhysicsTicksPerSecondCore;
        set => Service.PhysicsTicksPerSecondCore = value;
    }

    /// <summary>Gets or sets the maximum number of fixed-step callbacks run during one process frame.</summary>
    /// <value>A positive callback limit. The default is <c>8</c>.</value>
    /// <remarks>
    /// Limiting catch-up avoids an unbounded spiral after a long host stall. Excess whole fixed steps are discarded;
    /// the remaining fractional time is preserved for interpolation. Assignment writes
    /// <see cref="ProjectSettings.MaxPhysicsStepsPerFrame"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is less than or equal to zero.</exception>
    public static int MaxPhysicsStepsPerFrame
    {
        get => Service.MaxPhysicsStepsPerFrameCore;
        set => Service.MaxPhysicsStepsPerFrameCore = value;
    }

    /// <summary>Gets or sets the tolerance used to smooth fixed-step boundaries against variable frame timing.</summary>
    /// <value>A finite non-negative multiple of one fixed step. The default is <c>0.5</c>.</value>
    /// <remarks>
    /// A value of zero disables tolerance-based clock adjustment. Values above <c>2</c> are accepted but can make
    /// timing noticeably less responsive. Custom interpolation commonly uses zero. Assignment writes
    /// <see cref="ProjectSettings.PhysicsJitterFix"/>; negative input is clamped to zero before storage.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is NaN or infinite.</exception>
    public static double PhysicsJitterFix
    {
        get => Service.PhysicsJitterFixCore;
        set => Service.PhysicsJitterFixCore = value;
    }

    /// <summary>Gets or sets the rate at which game time advances relative to unscaled host time.</summary>
    /// <value>A finite non-negative multiplier. The default is <c>1</c>; zero freezes callback deltas.</value>
    /// <remarks>
    /// This multiplier changes the deltas supplied to process and fixed-step callbacks. It does not change how often
    /// those callbacks are scheduled. Extremely large values reduce temporal precision and should be avoided.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is negative, NaN, or infinite.</exception>
    public static double TimeScale
    {
        get => Service.TimeScaleCore;
        set => Service.TimeScaleCore = value;
    }

    /// <summary>Gets the number of process callbacks completed since the process-wide engine was created.</summary>
    /// <value>A monotonically increasing process-lifetime count. A callback that throws is not counted.</value>
    public static ulong ProcessFrames
    {
        get => Service.ProcessFramesCore;
    }

    /// <summary>Gets the number of fixed-step callbacks started since the process-wide engine was created.</summary>
    /// <value>A monotonically increasing process-lifetime count, including a callback that throws.</value>
    public static ulong PhysicsFrames
    {
        get => Service.PhysicsFramesCore;
    }

    /// <summary>Gets the most recently measured process-frame rate.</summary>
    /// <value>
    /// Completed process frames per unscaled host second, updated after each accumulated second. The value is zero
    /// until the first measurement window completes and is reset by <see cref="Start"/>.
    /// </value>
    public static double FramesPerSecond
    {
        get => Service.FramesPerSecondCore;
    }

    /// <summary>Gets the fraction of the current fixed interval remaining after the latest scheduling decision.</summary>
    /// <value>A value from <c>0</c> through <c>1</c>, where zero is exactly on a fixed-step boundary.</value>
    /// <remarks>The active 2D renderer uses this fraction for eligible canvas and camera presentation when
    /// <see cref="SceneTree.PhysicsInterpolation"/> is enabled; logical transforms remain current.</remarks>
    public static double PhysicsInterpolationFraction
    {
        get => Service.PhysicsInterpolationFractionCore;
    }

    /// <summary>Gets whether the current thread is executing a fixed-step callback.</summary>
    /// <value><see langword="true"/> only during a call to <see cref="MainLoop.PhysicsProcess"/>.</value>
    public static bool IsInPhysicsFrame
    {
        get => Service.IsInPhysicsFrameCore;
    }

    /// <summary>Gets the currently attached application loop.</summary>
    /// <value>The loop visible during startup, frames, and shutdown; otherwise <see langword="null"/>.</value>
    public static MainLoop? MainLoop
    {
        get => Service.MainLoopCore;
    }

    /// <summary>Gets the architecture targeted by the current Electron2D process.</summary>
    /// <value>A stable lowercase architecture name such as <c>x86_64</c>, <c>x86_32</c>, <c>arm64</c>, or <c>arm32</c>.</value>
    public static string ArchitectureName
    {
        get => Service.ArchitectureNameCore;
    }

    /// <summary>Gets immutable version information for the loaded Electron2D assembly.</summary>
    /// <value>The process-wide version descriptor.</value>
    public static EngineVersionInfo VersionInfo
    {
        get => Service.VersionInfoCore;
    }

    /// <summary>Attaches and, when necessary, initializes one application loop.</summary>
    /// <param name="mainLoop">The live loop to own until <see cref="Stop"/> completes.</param>
    /// <remarks>
    /// The calling thread becomes the runtime owner. An uninitialized loop is initialized; an already running loop,
    /// including a newly constructed <see cref="SceneTree"/>, is attached without a second initialization. The loop
    /// is not disposed by the engine. During its initialization, <see cref="MainLoop"/> already returns
    /// <paramref name="mainLoop"/>. Active project locale and pseudolocalization settings are sampled before loop attachment;
    /// a caller-constructed SceneTree has already completed its initial node-ready callbacks at that point.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="mainLoop"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A runtime is already starting, running, iterating, or stopping; the loop is in an incompatible state; or the caller does not own the loop.</exception>
    /// <exception cref="ObjectDisposedException">The loop is disposing or disposed.</exception>
    /// <exception cref="Exception">Loop initialization throws. The engine returns to its idle state.</exception>
    public static void Start(MainLoop mainLoop) => Service.StartCore(mainLoop);

    /// <summary>Advances fixed-step callbacks followed by one process callback.</summary>
    /// <param name="elapsedSeconds">Finite non-negative unscaled host time elapsed since the previous call.</param>
    /// <returns><see langword="true"/> if either callback lane asks the host to stop; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// Fixed steps run before the process callback. If a fixed callback requests a stop, remaining fixed callbacks are
    /// skipped but the process callback still runs. A callback exception propagates, restores the engine to its running
    /// state, and does not implicitly finalize the loop. This method performs no waiting, rendering, input pumping,
    /// audio work, or collision simulation. After a successful process callback and metric update, the method flushes
    /// one pending <see cref="ProjectSettings.SettingsChanged"/> event before returning.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="elapsedSeconds"/> is negative, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">The runtime is not running, the caller is not its owner thread, frame execution is re-entered, or the current time scale would produce a non-finite callback delta.</exception>
    /// <exception cref="Exception">A loop callback or project-settings event handler throws.</exception>
    public static bool AdvanceFrame(double elapsedSeconds) => Service.AdvanceFrameCore(elapsedSeconds);

    /// <summary>Finalizes and detaches the current application loop.</summary>
    /// <remarks>
    /// The loop becomes unavailable through <see cref="MainLoop"/> after finalization returns or throws. The engine
    /// returns to its idle state and may start a different loop. Native audio output and player voice slots are closed even
    /// after finalization fails. The detached loop and borrowed streams are not disposed.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The runtime is not running, the caller is not its owner thread, or the call occurs during a frame or lifecycle transition.</exception>
    /// <exception cref="Exception">Loop finalization or audio cleanup throws. Detachment still completes.</exception>
    /// <exception cref="AggregateException">Several finalization or cleanup operations fail.</exception>
    public static void Stop() => Service.StopCore();

    /// <summary>Registers a named, non-owned engine singleton.</summary>
    /// <param name="name">The nonblank case-sensitive name.</param>
    /// <param name="instance">The live object to expose.</param>
    /// <remarks>
    /// Registration retains a managed reference but does not transfer disposal ownership. Disposing an object does not
    /// remove its registration; the registering component must unregister it during teardown. The name <c>Engine</c>,
    /// <c>ProjectSettings</c>, <c>Input</c>, <c>InputMap</c>, and <c>AudioServer</c> are already occupied by built-in process singletons.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="instance"/> is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The name is already registered.</exception>
    public static void RegisterSingleton(string name, ElectronObject instance) => Service.RegisterSingletonCore(name, instance);

    /// <summary>Unregisters a named engine singleton without disposing it.</summary>
    /// <param name="name">The nonblank case-sensitive registered name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="KeyNotFoundException">No singleton has the supplied name.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="name"/> identifies a built-in singleton.</exception>
    public static void UnregisterSingleton(string name) => Service.UnregisterSingletonCore(name);

    /// <summary>Gets a named engine singleton.</summary>
    /// <param name="name">The nonblank case-sensitive registered name.</param>
    /// <returns>The registered object. Ownership remains with the registering component.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="KeyNotFoundException">No singleton has the supplied name.</exception>
    public static ElectronObject GetSingleton(string name) => Service.GetSingletonCore(name);

    /// <summary>Gets a named engine singleton and validates its type.</summary>
    /// <typeparam name="T">The required <see cref="ElectronObject"/> type.</typeparam>
    /// <param name="name">The nonblank case-sensitive registered name.</param>
    /// <returns>The registered object cast to <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="KeyNotFoundException">No singleton has the supplied name.</exception>
    /// <exception cref="InvalidCastException">The registered object is not assignable to <typeparamref name="T"/>.</exception>
    public static T GetSingleton<T>(string name) where T : ElectronObject => Service.GetSingletonCore<T>(name);

    /// <summary>Reports whether a named engine singleton is registered.</summary>
    /// <param name="name">The nonblank case-sensitive name.</param>
    /// <returns><see langword="true"/> when the name is registered; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or consists only of whitespace.</exception>
    public static bool HasSingleton(string name) => Service.HasSingletonCore(name);

    /// <summary>Gets the current engine-singleton names in registration order.</summary>
    /// <returns>An immutable snapshot using ordinal, case-sensitive names.</returns>
    public static IReadOnlyList<string> GetSingletonList() => Service.GetSingletonListCore();

}
