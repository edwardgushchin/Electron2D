using System.Reflection;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Coordinates process-wide frame scheduling, runtime metrics, and named engine singletons.</summary>
/// <remarks>
/// <para>
/// <see cref="Instance"/> is created once for the process and cannot be disposed. Use Run to own a windowed scene lifecycle. For embedding, a host attaches one <see cref="MainLoop"/>, supplies finite elapsed time to
/// <see cref="AdvanceFrame"/>, and finally calls <see cref="Stop"/>.
/// </para>
/// <para>
/// Runtime lifecycle and frame execution have owner-thread affinity. Configuration properties, metric reads, and
/// named-singleton operations are safe from other threads. Fixed-step and time-scale properties use the process-wide
/// <see cref="ProjectSettings"/> registry, including active feature overrides. A frame uses one configuration snapshot.
/// </para>
/// </remarks>
public sealed partial class Engine : ElectronObject
{
    private const int RuntimeIdle = 0;
    private const int RuntimeStarting = 1;
    private const int RuntimeRunning = 2;
    private const int RuntimeIterating = 3;
    private const int RuntimeStopping = 4;

    private static readonly Engine SharedInstance = new();
    private static readonly EngineVersionInfo SharedVersionInfo = CreateVersionInfo();
    private static readonly IReadOnlyList<PropertyDescriptor> EngineProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<Engine, int>(
                nameof(PhysicsTicksPerSecond),
                engine => engine.PhysicsTicksPerSecond,
                (engine, value) => engine.PhysicsTicksPerSecond = value,
                _ => 60),
            new PropertyDescriptor<Engine, int>(
                nameof(MaxPhysicsStepsPerFrame),
                engine => engine.MaxPhysicsStepsPerFrame,
                (engine, value) => engine.MaxPhysicsStepsPerFrame = value,
                _ => 8),
            new PropertyDescriptor<Engine, double>(
                nameof(PhysicsJitterFix),
                engine => engine.PhysicsJitterFix,
                (engine, value) => engine.PhysicsJitterFix = value,
                _ => 0.5d),
            new PropertyDescriptor<Engine, double>(
                nameof(TimeScale),
                engine => engine.TimeScale,
                (engine, value) => engine.TimeScale = value,
                _ => 1d),
            new PropertyDescriptor<Engine, int>(nameof(MaxFPS), engine => engine.MaxFPS, (engine, value) => engine.MaxFPS = value, _ => 0),
            new PropertyDescriptor<Engine, ulong>(nameof(ProcessFrames), engine => engine.ProcessFrames),
            new PropertyDescriptor<Engine, ulong>(nameof(PhysicsFrames), engine => engine.PhysicsFrames),
            new PropertyDescriptor<Engine, double>(nameof(FramesPerSecond), engine => engine.FramesPerSecond),
            new PropertyDescriptor<Engine, double>(
                nameof(PhysicsInterpolationFraction),
                engine => engine.PhysicsInterpolationFraction),
            new PropertyDescriptor<Engine, bool>(nameof(IsInPhysicsFrame), engine => engine.IsInPhysicsFrame),
            new PropertyDescriptor<Engine, string>(nameof(ArchitectureName), engine => engine.ArchitectureName),
            new PropertyDescriptor<Engine, EngineVersionInfo>(nameof(VersionInfo), engine => engine.VersionInfo)
        ]);

    private readonly object _singletonsGate = new();
    private readonly Dictionary<string, ElectronObject> _singletons = new(StringComparer.Ordinal);
    private readonly List<string> _singletonNames = [];
    private readonly FrameSynchronizer _frameSynchronizer = new();

    private double _timeScale = 1d;
    private int _runtimeState;
    private int _runtimeOwnerThreadId;
    private MainLoop? _mainLoop;
    private long _processFrames;
    private long _physicsFrames;
    private double _physicsInterpolationFraction;
    private int _inPhysicsFrame;
    private double _framesPerSecond;
    private double _fpsElapsed;
    private ulong _fpsFrames;
    private int _scheduledPhysicsTicksPerSecond = 60;

    private Engine()
    {
        _singletons.Add(nameof(Engine), this);
        _singletonNames.Add(nameof(Engine));
        _singletons.Add(nameof(ProjectSettings), ProjectSettings.Instance);
        _singletonNames.Add(nameof(ProjectSettings));
        _singletons.Add(nameof(Input), Input.Instance);
        _singletonNames.Add(nameof(Input));
        _singletons.Add(nameof(InputMap), InputMap.Instance);
        _singletonNames.Add(nameof(InputMap));
    }

    /// <summary>Gets the process-wide engine instance.</summary>
    /// <value>The same non-disposable instance for the lifetime of the process.</value>
    public static Engine Instance => SharedInstance;

    /// <summary>Gets or sets the fixed-step callback frequency.</summary>
    /// <value>The number of physics callback opportunities per unscaled second. The default is <c>60</c>.</value>
    /// <remarks>
    /// Higher values improve fixed-step precision while increasing processor cost. The fixed callback delta is
    /// <c>TimeScale / PhysicsTicksPerSecond</c>. The value is sampled once at the start of each frame. Changing it
    /// writes <see cref="ProjectSettings.PhysicsTicksPerSecond"/>, re-baselines fixed-step history on the next frame,
    /// and discards any fractional interval from the old frequency.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is less than or equal to zero.</exception>
    public int PhysicsTicksPerSecond
    {
        get => ProjectSettings.Instance.GetWithOverride(ProjectSettings.PhysicsTicksPerSecond);
        set => ProjectSettings.Instance.Set(ProjectSettings.PhysicsTicksPerSecond, value);
    }

    /// <summary>Gets or sets the maximum number of fixed-step callbacks run during one process frame.</summary>
    /// <value>A positive callback limit. The default is <c>8</c>.</value>
    /// <remarks>
    /// Limiting catch-up avoids an unbounded spiral after a long host stall. Excess whole fixed steps are discarded;
    /// the remaining fractional time is preserved for interpolation. Assignment writes
    /// <see cref="ProjectSettings.MaxPhysicsStepsPerFrame"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is less than or equal to zero.</exception>
    public int MaxPhysicsStepsPerFrame
    {
        get => ProjectSettings.Instance.GetWithOverride(ProjectSettings.MaxPhysicsStepsPerFrame);
        set => ProjectSettings.Instance.Set(ProjectSettings.MaxPhysicsStepsPerFrame, value);
    }

    /// <summary>Gets or sets the tolerance used to smooth fixed-step boundaries against variable frame timing.</summary>
    /// <value>A finite non-negative multiple of one fixed step. The default is <c>0.5</c>.</value>
    /// <remarks>
    /// A value of zero disables tolerance-based clock adjustment. Values above <c>2</c> are accepted but can make
    /// timing noticeably less responsive. Custom interpolation commonly uses zero. Assignment writes
    /// <see cref="ProjectSettings.PhysicsJitterFix"/>; negative input is clamped to zero before storage.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is NaN or infinite.</exception>
    public double PhysicsJitterFix
    {
        get => ProjectSettings.Instance.GetWithOverride(ProjectSettings.PhysicsJitterFix);
        set
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Physics jitter fix must be finite.");

            ProjectSettings.Instance.Set(ProjectSettings.PhysicsJitterFix, Math.Max(0d, value));
        }
    }

    /// <summary>Gets or sets the rate at which game time advances relative to unscaled host time.</summary>
    /// <value>A finite non-negative multiplier. The default is <c>1</c>; zero freezes callback deltas.</value>
    /// <remarks>
    /// This multiplier changes the deltas supplied to process and fixed-step callbacks. It does not change how often
    /// those callbacks are scheduled. Extremely large values reduce temporal precision and should be avoided.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is negative, NaN, or infinite.</exception>
    public double TimeScale
    {
        get => Volatile.Read(ref _timeScale);
        set
        {
            if (!double.IsFinite(value) || value < 0d)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Time scale must be finite and non-negative.");

            Volatile.Write(ref _timeScale, value);
        }
    }

    /// <summary>Gets the number of process callbacks completed since the process-wide engine was created.</summary>
    /// <value>A monotonically increasing process-lifetime count. A callback that throws is not counted.</value>
    public ulong ProcessFrames => unchecked((ulong)Interlocked.Read(ref _processFrames));

    /// <summary>Gets the number of fixed-step callbacks started since the process-wide engine was created.</summary>
    /// <value>A monotonically increasing process-lifetime count, including a callback that throws.</value>
    public ulong PhysicsFrames => unchecked((ulong)Interlocked.Read(ref _physicsFrames));

    /// <summary>Gets the most recently measured process-frame rate.</summary>
    /// <value>
    /// Completed process frames per unscaled host second, updated after each accumulated second. The value is zero
    /// until the first measurement window completes and is reset by <see cref="Start"/>.
    /// </value>
    public double FramesPerSecond => Volatile.Read(ref _framesPerSecond);

    /// <summary>Gets the fraction of the current fixed interval remaining after the latest scheduling decision.</summary>
    /// <value>A value from <c>0</c> through <c>1</c>, where zero is exactly on a fixed-step boundary.</value>
    /// <remarks>The value is intended for visual interpolation between the previous and current fixed states.</remarks>
    public double PhysicsInterpolationFraction => Volatile.Read(ref _physicsInterpolationFraction);

    /// <summary>Gets whether the current thread is executing a fixed-step callback.</summary>
    /// <value><see langword="true"/> only during a call to <see cref="MainLoop.PhysicsProcess"/>.</value>
    public bool IsInPhysicsFrame => Volatile.Read(ref _inPhysicsFrame) != 0;

    /// <summary>Gets the currently attached application loop.</summary>
    /// <value>The loop visible during startup, frames, and shutdown; otherwise <see langword="null"/>.</value>
    public MainLoop? MainLoop => Volatile.Read(ref _mainLoop);

    /// <summary>Gets the architecture targeted by the current Electron2D process.</summary>
    /// <value>A stable lowercase architecture name such as <c>x86_64</c>, <c>x86_32</c>, <c>arm64</c>, or <c>arm32</c>.</value>
    public string ArchitectureName { get; } = GetArchitectureName();

    /// <summary>Gets immutable version information for the loaded Electron2D assembly.</summary>
    /// <value>The process-wide version descriptor.</value>
    public EngineVersionInfo VersionInfo => SharedVersionInfo;

    /// <summary>Attaches and, when necessary, initializes one application loop.</summary>
    /// <param name="mainLoop">The live loop to own until <see cref="Stop"/> completes.</param>
    /// <remarks>
    /// The calling thread becomes the runtime owner. An uninitialized loop is initialized; an already running loop,
    /// including a newly constructed <see cref="SceneTree"/>, is attached without a second initialization. The loop
    /// is not disposed by the engine. During its initialization, <see cref="MainLoop"/> already returns
    /// <paramref name="mainLoop"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="mainLoop"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A runtime is already starting, running, iterating, or stopping; the loop is in an incompatible state; or the caller does not own the loop.</exception>
    /// <exception cref="ObjectDisposedException">The loop is disposing or disposed.</exception>
    /// <exception cref="Exception">Loop initialization throws. The engine returns to its idle state.</exception>
    public void Start(MainLoop mainLoop)
    {
        ArgumentNullException.ThrowIfNull(mainLoop);

        if (Interlocked.CompareExchange(ref _runtimeState, RuntimeStarting, RuntimeIdle) != RuntimeIdle)
            throw new InvalidOperationException("The engine already has an active MainLoop lifecycle.");

        Volatile.Write(ref _runtimeOwnerThreadId, Environment.CurrentManagedThreadId);
        Volatile.Write(ref _mainLoop, mainLoop);

        try
        {
            mainLoop.StartForEngine();
            ResetRunState();
            Volatile.Write(ref _runtimeState, RuntimeRunning);
        }
        catch
        {
            Volatile.Write(ref _mainLoop, null);
            Volatile.Write(ref _runtimeOwnerThreadId, 0);
            Volatile.Write(ref _runtimeState, RuntimeIdle);
            throw;
        }
    }

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
    public bool AdvanceFrame(double elapsedSeconds)
    {
        if (Volatile.Read(ref _applicationRun) != 0)
            throw new InvalidOperationException("Engine.Run owns frame execution until it returns.");
        return AdvanceFrameCore(elapsedSeconds, out _);
    }

    private bool AdvanceFrameCore(double elapsedSeconds, out double renderStep)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0d)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), elapsedSeconds, "Elapsed time must be finite and non-negative.");

        EnsureRuntimeOwnerThread();

        if (Interlocked.CompareExchange(ref _runtimeState, RuntimeIterating, RuntimeRunning) != RuntimeRunning)
            throw new InvalidOperationException("The engine can advance a frame only while its MainLoop is running.");

        try
        {
            var mainLoop = Volatile.Read(ref _mainLoop) ??
                throw new InvalidOperationException("The running engine has no MainLoop.");
            var physicsTicksPerSecond = PhysicsTicksPerSecond;
            var maxPhysicsSteps = MaxPhysicsStepsPerFrame;
            var jitterFix = PhysicsJitterFix;
            var timeScale = TimeScale;
            var physicsStep = 1d / physicsTicksPerSecond;

            if (_scheduledPhysicsTicksPerSecond != physicsTicksPerSecond)
            {
                _frameSynchronizer.Reset();
                _scheduledPhysicsTicksPerSecond = physicsTicksPerSecond;
            }

            var schedulingElapsed = LimitCatchUp(elapsedSeconds, physicsStep, maxPhysicsSteps);
            var timing = _frameSynchronizer.Advance(physicsStep, physicsTicksPerSecond, schedulingElapsed, jitterFix);

            if (timing.PhysicsSteps > maxPhysicsSteps)
            {
                timing = new FrameTiming(
                    Math.Max(0d, timing.ProcessStep - ((timing.PhysicsSteps - maxPhysicsSteps) * physicsStep)),
                    maxPhysicsSteps,
                    timing.InterpolationFraction);
            }

            var scaledPhysicsStep = physicsStep * timeScale;
            var scaledProcessStep = timing.ProcessStep * timeScale;
            renderStep = scaledProcessStep;

            if (!double.IsFinite(scaledPhysicsStep) || !double.IsFinite(scaledProcessStep))
                throw new InvalidOperationException("The current time scale produces a non-finite callback delta.");

            Volatile.Write(ref _physicsInterpolationFraction, Math.Clamp(timing.InterpolationFraction, 0d, 1d));

            var stopRequested = false;

            for (var index = 0; index < timing.PhysicsSteps; index++)
            {
                Interlocked.Increment(ref _physicsFrames);
                Volatile.Write(ref _inPhysicsFrame, 1);

                try
                {
                    if (mainLoop.PhysicsProcessForEngine(scaledPhysicsStep, physicsStep))
                    {
                        stopRequested = true;
                        break;
                    }
                }
                finally
                {
                    Volatile.Write(ref _inPhysicsFrame, 0);
                }
            }

            if (mainLoop.ProcessForEngine(scaledProcessStep, timing.ProcessStep))
                stopRequested = true;

            Interlocked.Increment(ref _processFrames);
            UpdateFramesPerSecond(elapsedSeconds);
            ProjectSettings.Instance.FlushChanges();
            return stopRequested;
        }
        finally
        {
            Volatile.Write(ref _inPhysicsFrame, 0);
            Volatile.Write(ref _runtimeState, RuntimeRunning);
        }
    }

    /// <summary>Finalizes and detaches the current application loop.</summary>
    /// <remarks>
    /// The loop becomes unavailable through <see cref="MainLoop"/> after finalization returns or throws. The engine
    /// returns to its idle state and may start a different loop. The detached loop is not disposed.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The runtime is not running, the caller is not its owner thread, or the call occurs during a frame or lifecycle transition.</exception>
    /// <exception cref="Exception">Loop finalization throws. Detachment still completes.</exception>
    public void Stop()
    {
        if (Volatile.Read(ref _applicationRun) != 0)
            throw new InvalidOperationException("Request SceneTree.Quit to stop Engine.Run.");
        EnsureRuntimeOwnerThread();

        if (Interlocked.CompareExchange(ref _runtimeState, RuntimeStopping, RuntimeRunning) != RuntimeRunning)
            throw new InvalidOperationException("The engine can stop only a running MainLoop outside frame execution.");

        try
        {
            var mainLoop = Volatile.Read(ref _mainLoop) ??
                throw new InvalidOperationException("The running engine has no MainLoop.");
            mainLoop.FinalizeLoop();
        }
        finally
        {
            Volatile.Write(ref _mainLoop, null);
            Volatile.Write(ref _runtimeOwnerThreadId, 0);
            Volatile.Write(ref _runtimeState, RuntimeIdle);
        }
    }

    /// <summary>Registers a named, non-owned engine singleton.</summary>
    /// <param name="name">The nonblank case-sensitive name.</param>
    /// <param name="instance">The live object to expose.</param>
    /// <remarks>
    /// Registration retains a managed reference but does not transfer disposal ownership. Disposing an object does not
    /// remove its registration; the registering component must unregister it during teardown. The name <c>Engine</c>
    /// <c>ProjectSettings</c>, <c>Input</c>, and <c>InputMap</c> are already occupied by built-in process singletons.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="instance"/> is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The name is already registered.</exception>
    public void RegisterSingleton(string name, ElectronObject instance)
    {
        ValidateSingletonName(name);
        ArgumentNullException.ThrowIfNull(instance);
        ObjectDisposedException.ThrowIf(instance.IsDisposed, instance);

        lock (_singletonsGate)
        {
            if (!_singletons.TryAdd(name, instance))
                throw new InvalidOperationException($"An engine singleton named '{name}' is already registered.");

            _singletonNames.Add(name);
        }
    }

    /// <summary>Unregisters a named engine singleton without disposing it.</summary>
    /// <param name="name">The nonblank case-sensitive registered name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="KeyNotFoundException">No singleton has the supplied name.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="name"/> identifies a built-in singleton.</exception>
    public void UnregisterSingleton(string name)
    {
        ValidateSingletonName(name);

        if (string.Equals(name, nameof(Engine), StringComparison.Ordinal) ||
            string.Equals(name, nameof(ProjectSettings), StringComparison.Ordinal) ||
            string.Equals(name, nameof(Input), StringComparison.Ordinal) ||
            string.Equals(name, nameof(InputMap), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The built-in {name} singleton cannot be unregistered.");
        }

        lock (_singletonsGate)
        {
            if (!_singletons.Remove(name))
                throw new KeyNotFoundException($"No engine singleton named '{name}' is registered.");

            _singletonNames.Remove(name);
        }
    }

    /// <summary>Gets a named engine singleton.</summary>
    /// <param name="name">The nonblank case-sensitive registered name.</param>
    /// <returns>The registered object. Ownership remains with the registering component.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="KeyNotFoundException">No singleton has the supplied name.</exception>
    public ElectronObject GetSingleton(string name)
    {
        ValidateSingletonName(name);

        lock (_singletonsGate)
        {
            return _singletons.TryGetValue(name, out var instance)
                ? instance
                : throw new KeyNotFoundException($"No engine singleton named '{name}' is registered.");
        }
    }

    /// <summary>Gets a named engine singleton and validates its type.</summary>
    /// <typeparam name="T">The required <see cref="ElectronObject"/> type.</typeparam>
    /// <param name="name">The nonblank case-sensitive registered name.</param>
    /// <returns>The registered object cast to <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="KeyNotFoundException">No singleton has the supplied name.</exception>
    /// <exception cref="InvalidCastException">The registered object is not assignable to <typeparamref name="T"/>.</exception>
    public T GetSingleton<T>(string name)
        where T : ElectronObject
    {
        var instance = GetSingleton(name);
        return instance as T ?? throw new InvalidCastException(
            $"Engine singleton '{name}' is a {instance.ClassName}, not a {typeof(T).Name}.");
    }

    /// <summary>Reports whether a named engine singleton is registered.</summary>
    /// <param name="name">The nonblank case-sensitive name.</param>
    /// <returns><see langword="true"/> when the name is registered; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or consists only of whitespace.</exception>
    public bool HasSingleton(string name)
    {
        ValidateSingletonName(name);

        lock (_singletonsGate)
            return _singletons.ContainsKey(name);
    }

    /// <summary>Gets the current engine-singleton names in registration order.</summary>
    /// <returns>An immutable snapshot using ordinal, case-sensitive names.</returns>
    public IReadOnlyList<string> GetSingletonList()
    {
        lock (_singletonsGate)
            return Array.AsReadOnly(_singletonNames.ToArray());
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
            yield return property;

        foreach (var property in EngineProperties)
            yield return property;
    }

    /// <inheritdoc />
    /// <remarks>The process-wide engine instance cannot be disposed.</remarks>
    /// <exception cref="InvalidOperationException">Always thrown because the singleton has process lifetime.</exception>
    protected override void ValidateDisposal() =>
        throw new InvalidOperationException("The process-wide Engine instance cannot be disposed.");

    private static EngineVersionInfo CreateVersionInfo()
    {
        var assembly = typeof(Engine).Assembly;
        var assemblyVersion = assembly.GetName().Version ?? new Version(0, 0);
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return new EngineVersionInfo(assemblyVersion, informationalVersion ?? assemblyVersion.ToString());
    }

    private static string GetArchitectureName() => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => "x86_64",
        Architecture.X86 => "x86_32",
        Architecture.Arm64 => "arm64",
        Architecture.Arm => "arm32",
        _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
    };

    private static void ValidateSingletonName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("An engine singleton name cannot be empty or whitespace.", nameof(name));
    }

    private static double LimitCatchUp(double elapsed, double physicsStep, int maxPhysicsSteps)
    {
        var maximumWindow = (maxPhysicsSteps + 1d) * physicsStep;
        return elapsed <= maximumWindow
            ? elapsed
            : (maxPhysicsSteps * physicsStep) + (elapsed % physicsStep);
    }

    private void EnsureRuntimeOwnerThread()
    {
        if (Volatile.Read(ref _runtimeState) == RuntimeIdle)
            throw new InvalidOperationException("The engine has no running MainLoop.");

        if (Environment.CurrentManagedThreadId != Volatile.Read(ref _runtimeOwnerThreadId))
            throw new InvalidOperationException("Engine lifecycle and frame execution must run on the runtime owner thread.");
    }

    private void ResetRunState()
    {
        _frameSynchronizer.Reset();
        Volatile.Write(ref _physicsInterpolationFraction, 0d);
        Volatile.Write(ref _inPhysicsFrame, 0);
        Volatile.Write(ref _framesPerSecond, 0d);
        _fpsElapsed = 0d;
        _fpsFrames = 0;
        _scheduledPhysicsTicksPerSecond = PhysicsTicksPerSecond;
    }

    private void UpdateFramesPerSecond(double elapsedSeconds)
    {
        _fpsElapsed += elapsedSeconds;
        _fpsFrames++;

        if (_fpsElapsed < 1d - 1e-12d)
            return;

        Volatile.Write(ref _framesPerSecond, _fpsFrames / _fpsElapsed);
        _fpsElapsed %= 1d;

        if (_fpsElapsed > 1d - 1e-12d)
            _fpsElapsed = 0d;

        _fpsFrames = 0;
    }

    private readonly struct FrameTiming(double processStep, int physicsSteps, double interpolationFraction)
    {
        public double ProcessStep { get; } = processStep;

        public int PhysicsSteps { get; } = physicsSteps;

        public double InterpolationFraction { get; } = interpolationFraction;
    }

    private sealed class FrameSynchronizer
    {
        private const int ControlSteps = 12;

        private readonly int[] _accumulatedPhysicsSteps = new int[ControlSteps];
        private readonly int[] _typicalPhysicsSteps = new int[ControlSteps];
        private double _timeAccumulator;
        private double _timeDeficit;

        public FrameSynchronizer() => Reset();

        public void Reset()
        {
            _timeAccumulator = 0d;
            _timeDeficit = 0d;

            for (var index = 0; index < ControlSteps; index++)
            {
                _typicalPhysicsSteps[index] = index;
                _accumulatedPhysicsSteps[index] = index;
            }
        }

        public FrameTiming Advance(
            double physicsStep,
            int physicsTicksPerSecond,
            double processStep,
            double jitterFix)
        {
            if (processStep == 0d)
                return new FrameTiming(0d, 0, _timeAccumulator / physicsStep);

            var minimumOutputStep = Math.Max(processStep / 8d, 1e-6d);
            processStep += _timeDeficit;
            var timing = AdvanceCore(physicsStep, physicsTicksPerSecond, processStep, jitterFix);
            var processMinusAccumulator = timing.ProcessStep - _timeAccumulator;
            var adjustedProcessStep = timing.ProcessStep;

            if (GetAveragePhysicsSteps(out var minimumAverage, out var maximumAverage) > 3)
            {
                adjustedProcessStep = Math.Clamp(
                    adjustedProcessStep,
                    minimumAverage * physicsStep,
                    maximumAverage * physicsStep);
            }

            var maximumClockDeviation = jitterFix * physicsStep;
            adjustedProcessStep = Math.Clamp(
                adjustedProcessStep,
                processStep - maximumClockDeviation,
                processStep + maximumClockDeviation);
            adjustedProcessStep = Math.Clamp(
                adjustedProcessStep,
                processMinusAccumulator,
                processMinusAccumulator + physicsStep);
            adjustedProcessStep = Math.Max(adjustedProcessStep, minimumOutputStep);

            _timeAccumulator = adjustedProcessStep - processMinusAccumulator;
            var physicsSteps = timing.PhysicsSteps;

            if (_timeAccumulator > physicsStep)
            {
                var extraPhysicsSteps = FloorToInt(_timeAccumulator * physicsTicksPerSecond);
                _timeAccumulator -= extraPhysicsSteps * physicsStep;
                physicsSteps = SaturatingAdd(physicsSteps, extraPhysicsSteps);
            }

            _timeAccumulator = Math.Clamp(_timeAccumulator, 0d, physicsStep);
            _timeDeficit = processStep - adjustedProcessStep;
            return new FrameTiming(adjustedProcessStep, physicsSteps, _timeAccumulator / physicsStep);
        }

        private FrameTiming AdvanceCore(
            double physicsStep,
            int physicsTicksPerSecond,
            double processStep,
            double jitterFix)
        {
            _timeAccumulator += processStep;
            var physicsSteps = FloorToInt(_timeAccumulator * physicsTicksPerSecond);
            var minimumTypicalSteps = _typicalPhysicsSteps[0];
            var maximumTypicalSteps = minimumTypicalSteps == int.MaxValue ? int.MaxValue : minimumTypicalSteps + 1;
            var updateTypical = false;

            for (var index = 0; index < ControlSteps - 1; index++)
            {
                var stepsLeft = _typicalPhysicsSteps[index + 1] - _accumulatedPhysicsSteps[index];
                var stepsLeftPlusOne = stepsLeft == int.MaxValue ? int.MaxValue : stepsLeft + 1;

                if (stepsLeft > maximumTypicalSteps || stepsLeftPlusOne < minimumTypicalSteps)
                {
                    updateTypical = true;
                    break;
                }

                minimumTypicalSteps = Math.Max(minimumTypicalSteps, stepsLeft);
                maximumTypicalSteps = Math.Min(maximumTypicalSteps, stepsLeftPlusOne);
            }

            if (physicsSteps < minimumTypicalSteps)
            {
                var maximumPossible = FloorToInt((_timeAccumulator * physicsTicksPerSecond) + jitterFix);

                if (maximumPossible < minimumTypicalSteps)
                {
                    physicsSteps = maximumPossible;
                    updateTypical = true;
                }
                else
                {
                    physicsSteps = minimumTypicalSteps;
                }
            }
            else if (physicsSteps > maximumTypicalSteps)
            {
                var minimumPossible = FloorToInt((_timeAccumulator * physicsTicksPerSecond) - jitterFix);

                if (minimumPossible > maximumTypicalSteps)
                {
                    physicsSteps = minimumPossible;
                    updateTypical = true;
                }
                else
                {
                    physicsSteps = maximumTypicalSteps;
                }
            }

            physicsSteps = Math.Max(0, physicsSteps);
            _timeAccumulator -= physicsSteps * physicsStep;

            for (var index = ControlSteps - 2; index >= 0; index--)
                _accumulatedPhysicsSteps[index + 1] = SaturatingAdd(_accumulatedPhysicsSteps[index], physicsSteps);

            _accumulatedPhysicsSteps[0] = physicsSteps;

            if (updateTypical)
            {
                for (var index = ControlSteps - 1; index >= 0; index--)
                {
                    if (_typicalPhysicsSteps[index] > _accumulatedPhysicsSteps[index])
                        _typicalPhysicsSteps[index] = _accumulatedPhysicsSteps[index];
                    else if (_typicalPhysicsSteps[index] < _accumulatedPhysicsSteps[index] - 1)
                        _typicalPhysicsSteps[index] = _accumulatedPhysicsSteps[index] - 1;
                }
            }

            return new FrameTiming(processStep, physicsSteps, 0d);
        }

        private static int FloorToInt(double value)
        {
            if (value >= int.MaxValue)
                return int.MaxValue;

            if (value <= int.MinValue)
                return int.MinValue;

            return (int)Math.Floor(value);
        }

        private static int SaturatingAdd(int left, int right)
        {
            var sum = (long)left + right;
            return sum >= int.MaxValue ? int.MaxValue : sum <= int.MinValue ? int.MinValue : (int)sum;
        }

        private int GetAveragePhysicsSteps(out double minimum, out double maximum)
        {
            minimum = _typicalPhysicsSteps[0];
            maximum = minimum + 1d;

            for (var index = 1; index < ControlSteps; index++)
            {
                var currentMinimum = _typicalPhysicsSteps[index] / (index + 1d);

                if (currentMinimum > maximum)
                    return index;

                minimum = Math.Max(minimum, currentMinimum);
                var currentMaximum = (_typicalPhysicsSteps[index] + 1d) / (index + 1d);

                if (currentMaximum < minimum)
                    return index;

                maximum = Math.Min(maximum, currentMaximum);
            }

            return ControlSteps;
        }
    }
}
