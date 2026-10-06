using System.Reflection;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Coordinates process-wide frame scheduling, runtime metrics, and named engine singletons.</summary>
/// <remarks>
/// <para>
/// The retained engine object is created once for the process and cannot be disposed. Public operations are static delegates to that object. Use Run to own a windowed scene lifecycle. For embedding, a host attaches one <see cref="MainLoop"/>, supplies finite elapsed time to
/// <see cref="AdvanceFrame"/>, and finally calls <see cref="Stop"/>.
/// </para>
/// <para>
/// Runtime lifecycle and frame execution have owner-thread affinity. Configuration properties, metric reads, and
/// named-singleton operations are safe from other threads. Floating-point snapshots are atomic on 32-bit hosts too.
/// Fixed-step and time-scale properties use the process-wide
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
                engine => engine.PhysicsTicksPerSecondCore,
                (engine, value) => engine.PhysicsTicksPerSecondCore = value,
                _ => 60),
            new PropertyDescriptor<Engine, int>(
                nameof(MaxPhysicsStepsPerFrame),
                engine => engine.MaxPhysicsStepsPerFrameCore,
                (engine, value) => engine.MaxPhysicsStepsPerFrameCore = value,
                _ => 8),
            new PropertyDescriptor<Engine, double>(
                nameof(PhysicsJitterFix),
                engine => engine.PhysicsJitterFixCore,
                (engine, value) => engine.PhysicsJitterFixCore = value,
                _ => 0.5d),
            new PropertyDescriptor<Engine, double>(
                nameof(TimeScale),
                engine => engine.TimeScaleCore,
                (engine, value) => engine.TimeScaleCore = value,
                _ => 1d),
            new PropertyDescriptor<Engine, int>(nameof(MaxFPS), engine => engine.MaxFPSCore, (engine, value) => engine.MaxFPSCore = value, _ => 0),
            new PropertyDescriptor<Engine, ulong>(nameof(ProcessFrames), engine => engine.ProcessFramesCore),
            new PropertyDescriptor<Engine, ulong>(nameof(PhysicsFrames), engine => engine.PhysicsFramesCore),
            new PropertyDescriptor<Engine, double>(nameof(FramesPerSecond), engine => engine.FramesPerSecondCore),
            new PropertyDescriptor<Engine, double>(
                nameof(PhysicsInterpolationFraction),
                engine => engine.PhysicsInterpolationFractionCore),
            new PropertyDescriptor<Engine, bool>(nameof(IsInPhysicsFrame), engine => engine.IsInPhysicsFrameCore),
            new PropertyDescriptor<Engine, string>(nameof(ArchitectureName), engine => engine.ArchitectureNameCore),
            new PropertyDescriptor<Engine, EngineVersionInfo>(nameof(VersionInfo), engine => engine.VersionInfoCore)
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
        _singletons.Add(nameof(OS), OS.Service);
        _singletonNames.Add(nameof(OS));
        _singletons.Add(nameof(ProjectSettings), ProjectSettings.Service);
        _singletonNames.Add(nameof(ProjectSettings));
        _singletons.Add(nameof(Input), Input.Service);
        _singletonNames.Add(nameof(Input));
        _singletons.Add(nameof(InputMap), InputMap.Service);
        _singletonNames.Add(nameof(InputMap));
        _singletons.Add(nameof(ResourceLoader), ResourceLoader.Runtime);
        _singletonNames.Add(nameof(ResourceLoader));
        _singletons.Add(nameof(ResourceSaver), ResourceSaver.Runtime);
        _singletonNames.Add(nameof(ResourceSaver));
        _singletons.Add(nameof(ResourceUID), ResourceUID.Runtime);
        _singletonNames.Add(nameof(ResourceUID));
        _singletons.Add(nameof(AudioServer), AudioServer.Service);
        _singletonNames.Add(nameof(AudioServer));
    }

    internal static Engine Service => SharedInstance;

    internal int PhysicsTicksPerSecondCore
    {
        get => ProjectSettings.GetWithOverride(ProjectSettings.PhysicsTicksPerSecond);
        set => ProjectSettings.Set(ProjectSettings.PhysicsTicksPerSecond, value);
    }

    internal int MaxPhysicsStepsPerFrameCore
    {
        get => ProjectSettings.GetWithOverride(ProjectSettings.MaxPhysicsStepsPerFrame);
        set => ProjectSettings.Set(ProjectSettings.MaxPhysicsStepsPerFrame, value);
    }

    internal double PhysicsJitterFixCore
    {
        get => ProjectSettings.GetWithOverride(ProjectSettings.PhysicsJitterFix);
        set
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Physics jitter fix must be finite.");

            ProjectSettings.Set(ProjectSettings.PhysicsJitterFix, Math.Max(0d, value));
        }
    }

    internal double TimeScaleCore
    {
        get => AtomicFloatingPoint.Read(ref _timeScale);
        set
        {
            if (!double.IsFinite(value) || value < 0d)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Time scale must be finite and non-negative.");

            AtomicFloatingPoint.Write(ref _timeScale, value);
        }
    }

    internal ulong ProcessFramesCore => unchecked((ulong)Interlocked.Read(ref _processFrames));

    internal ulong PhysicsFramesCore => unchecked((ulong)Interlocked.Read(ref _physicsFrames));

    internal double FramesPerSecondCore => AtomicFloatingPoint.Read(ref _framesPerSecond);

    internal double PhysicsInterpolationFractionCore => AtomicFloatingPoint.Read(ref _physicsInterpolationFraction);

    internal bool IsInPhysicsFrameCore => Volatile.Read(ref _inPhysicsFrame) != 0;

    internal MainLoop? MainLoopCore => Volatile.Read(ref _mainLoop);

    internal string ArchitectureNameCore { get; } = GetArchitectureName();

    internal EngineVersionInfo VersionInfoCore => SharedVersionInfo;

    internal void StartCore(MainLoop mainLoop)
    {
        ArgumentNullException.ThrowIfNull(mainLoop);

        if (Interlocked.CompareExchange(ref _runtimeState, RuntimeStarting, RuntimeIdle) != RuntimeIdle)
            throw new InvalidOperationException("The engine already has an active MainLoop lifecycle.");

        Volatile.Write(ref _runtimeOwnerThreadId, Environment.CurrentManagedThreadId);
        Volatile.Write(ref _mainLoop, mainLoop);

        try
        {
            TranslationServer.LoadProjectLocalization();
            AudioServer.LoadDefaultLayout();
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

    internal bool AdvanceFrameCore(double elapsedSeconds)
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
            var physicsTicksPerSecond = PhysicsTicksPerSecondCore;
            var maxPhysicsSteps = MaxPhysicsStepsPerFrameCore;
            var jitterFix = PhysicsJitterFixCore;
            var timeScale = TimeScaleCore;
            var physicsStep = 1d / physicsTicksPerSecond;

            if (_scheduledPhysicsTicksPerSecond != physicsTicksPerSecond)
            {
                _frameSynchronizer.Reset();
                _scheduledPhysicsTicksPerSecond = physicsTicksPerSecond;
            }

            var schedulingElapsed = LimitCatchUp(elapsedSeconds, physicsStep, maxPhysicsSteps);
            var timing = _frameSynchronizer.Advance(physicsStep, physicsTicksPerSecond, schedulingElapsed, jitterFix);
            var unscaledProcessStep = timing.ProcessStep;

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

            AtomicFloatingPoint.Write(ref _physicsInterpolationFraction, Math.Clamp(timing.InterpolationFraction, 0d, 1d));

            var stopRequested = false;

            for (var index = 0; index < timing.PhysicsSteps; index++)
            {
                Interlocked.Increment(ref _physicsFrames);
                Volatile.Write(ref _inPhysicsFrame, 1);

                try
                {
                    if (mainLoop.PhysicsProcessForEngine(scaledPhysicsStep, physicsStep, unscaledProcessStep))
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

            if (mainLoop.ProcessForEngine(scaledProcessStep, timing.ProcessStep, unscaledProcessStep))
                stopRequested = true;

            Interlocked.Increment(ref _processFrames);
            UpdateFramesPerSecond(elapsedSeconds);
            ProjectSettings.FlushChanges();
            return stopRequested;
        }
        finally
        {
            Volatile.Write(ref _inPhysicsFrame, 0);
            Volatile.Write(ref _runtimeState, RuntimeRunning);
        }
    }

    internal void StopCore()
    {
        if (Volatile.Read(ref _applicationRun) != 0)
            throw new InvalidOperationException("Request SceneTree.Quit to stop Engine.Run.");
        EnsureRuntimeOwnerThread();

        if (Interlocked.CompareExchange(ref _runtimeState, RuntimeStopping, RuntimeRunning) != RuntimeRunning)
            throw new InvalidOperationException("The engine can stop only a running MainLoop outside frame execution.");

        List<Exception>? errors = null;
        try { (Volatile.Read(ref _mainLoop) ?? throw new InvalidOperationException("The running engine has no MainLoop.")).FinalizeLoop(); }
        catch (Exception error) { Node.CollectException(ref errors, error); }
        try { AudioServer.CloseForEngine(); }
        catch (Exception error) { Node.CollectException(ref errors, error); }
        finally
        {
            Volatile.Write(ref _mainLoop, null);
            Volatile.Write(ref _runtimeOwnerThreadId, 0);
            Volatile.Write(ref _runtimeState, RuntimeIdle);
        }
        if (errors is { Count: 1 }) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
        Node.ThrowCollected("Engine shutdown failed.", errors);
    }

    internal void RegisterSingletonCore(string name, ElectronObject instance)
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

    internal void UnregisterSingletonCore(string name)
    {
        ValidateSingletonName(name);

        if (string.Equals(name, nameof(OS), StringComparison.Ordinal) ||
            string.Equals(name, nameof(Engine), StringComparison.Ordinal) ||
            string.Equals(name, nameof(ProjectSettings), StringComparison.Ordinal) ||
            string.Equals(name, nameof(Input), StringComparison.Ordinal) ||
            string.Equals(name, nameof(InputMap), StringComparison.Ordinal) ||
            string.Equals(name, nameof(ResourceLoader), StringComparison.Ordinal) ||
            string.Equals(name, nameof(ResourceSaver), StringComparison.Ordinal) ||
            string.Equals(name, nameof(ResourceUID), StringComparison.Ordinal) ||
            string.Equals(name, nameof(AudioServer), StringComparison.Ordinal))
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

    internal ElectronObject GetSingletonCore(string name)
    {
        ValidateSingletonName(name);

        lock (_singletonsGate)
        {
            return _singletons.TryGetValue(name, out var instance)
                ? instance
                : throw new KeyNotFoundException($"No engine singleton named '{name}' is registered.");
        }
    }

    internal T GetSingletonCore<T>(string name)
    where T : ElectronObject
    {
        var instance = GetSingletonCore(name);
        return instance as T ?? throw new InvalidCastException(
            $"Engine singleton '{name}' is a {instance.ClassName}, not a {typeof(T).Name}.");
    }

    internal bool HasSingletonCore(string name)
    {
        ValidateSingletonName(name);

        lock (_singletonsGate)
            return _singletons.ContainsKey(name);
    }

    internal IReadOnlyList<string> GetSingletonListCore()
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
        AtomicFloatingPoint.Write(ref _physicsInterpolationFraction, 0d);
        Volatile.Write(ref _inPhysicsFrame, 0);
        AtomicFloatingPoint.Write(ref _framesPerSecond, 0d);
        _fpsElapsed = 0d;
        _fpsFrames = 0;
        _scheduledPhysicsTicksPerSecond = PhysicsTicksPerSecondCore;
    }

    private void UpdateFramesPerSecond(double elapsedSeconds)
    {
        _fpsElapsed += elapsedSeconds;
        _fpsFrames++;

        if (_fpsElapsed < 1d - 1e-12d)
            return;

        AtomicFloatingPoint.Write(ref _framesPerSecond, _fpsFrames / _fpsElapsed);
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
