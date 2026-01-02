using System.Diagnostics;

namespace Electron2D;

/// <summary>
/// Система времени движка: вычисляет delta-time, поддерживает fixed-step и (опционально) cap FPS при отключённом VSync.
/// </summary>
internal sealed class TimeSystem
{
    #region Static fields

    private static readonly double InvStopwatchFrequency = 1.0 / Stopwatch.Frequency;

    // --- Wait policy ---
    // Безопасные дефолты: ~2ms sleep, ~1ms yield (остальное — spin).
    private static readonly long SleepThresholdTicks = Stopwatch.Frequency / 500;  // ~2ms
    private static readonly long YieldThresholdTicks = Stopwatch.Frequency / 1000; // ~1ms

    #endregion

    #region Instance fields

    // Stopwatch timestamps (ticks)
    private long _lastTimestamp;
    private long _frameStartTimestamp;

    // Delta
    private float _timeScale = 1f;
    private float _deltaTimeScaled;
    private float _deltaTimeUnscaled;

    // Fixed-step
    private bool _useFixedStep = true;
    private float _fixedDeltaSeconds = 1f / 60f;
    private int _maxFixedStepsPerFrame = 8;

    private double _fixedAccumulatorSeconds;
    private int _fixedStepsThisFrame;

    // Frame cap (только когда VSync выключен)
    private VSyncMode _vsyncMode = VSyncMode.Enabled;
    private int _maxFps; // 0 => off

    private long _nextFrameTimestamp;
    private long _lastTargetFrameTicks;
    private long _targetFrameTicksThisFrame;

    #endregion

    #region Properties

    /// <summary>DeltaTime, масштабированное <see cref="_timeScale"/>.</summary>
    public float DeltaTime => _deltaTimeScaled;

    /// <summary>DeltaTime без масштабирования.</summary>
    public float UnscaledDeltaTime => _deltaTimeUnscaled;

    public bool UseFixedStep => _useFixedStep;

    public float FixedDelta => _fixedDeltaSeconds;

    #endregion

    #region Public API

    public void Initialize(EngineConfig config)
    {
        _timeScale = config.TimeScale;

        _useFixedStep = config.UseFixedStep;
        _fixedDeltaSeconds = config.Physics.FixedDelta;
        _maxFixedStepsPerFrame = config.MaxFixedStepsPerFrame;

        _vsyncMode = config.VSync;
        _maxFps = config.MaxFps;

        ValidateFixedStepSettings();

        _lastTimestamp = Stopwatch.GetTimestamp();
        _nextFrameTimestamp = _lastTimestamp;
        _lastTargetFrameTicks = 0;

        _fixedAccumulatorSeconds = 0.0;
        _fixedStepsThisFrame = 0;

        _deltaTimeUnscaled = 0f;
        _deltaTimeScaled = 0f;
    }

    /// <summary>
    /// Применяет настройки во время работы (runtime/live settings).
    /// </summary>
    public void Apply(
        bool useFixedStep,
        float fixedDeltaSeconds,
        int maxFixedStepsPerFrame,
        float timeScale,
        VSyncMode vsync,
        int maxFps)
    {
        _useFixedStep = useFixedStep;
        _fixedDeltaSeconds = fixedDeltaSeconds;
        _maxFixedStepsPerFrame = maxFixedStepsPerFrame;

        _timeScale = timeScale;

        _vsyncMode = vsync;
        _maxFps = maxFps;

        ValidateFixedStepSettings();

        if (!_useFixedStep)
            _fixedAccumulatorSeconds = 0.0;
    }

    public void BeginFrame()
    {
        _frameStartTimestamp = Stopwatch.GetTimestamp();

        // dt (в секундах)
        var dtSeconds = (_frameStartTimestamp - _lastTimestamp) * InvStopwatchFrequency;
        _lastTimestamp = _frameStartTimestamp;

        if (dtSeconds < 0.0)
            dtSeconds = 0.0;

        _deltaTimeUnscaled = (float)dtSeconds;
        _deltaTimeScaled = _deltaTimeUnscaled * _timeScale;

        // Fixed-step accumulator.
        // Важно: аккумулятор заполняется scaled delta (timeScale влияет на симуляцию) — сохраняем исходную семантику.
        _fixedStepsThisFrame = 0;

        if (_useFixedStep)
        {
            _fixedAccumulatorSeconds += _deltaTimeScaled;

            // Лимит backlog, чтобы не расти бесконечно (например, при лаге/паузе):
            // ограничиваем максимумом "на кадр" и сохраняем дробный остаток.
            double maxBacklogSeconds = (double)_fixedDeltaSeconds * _maxFixedStepsPerFrame;
            if (_fixedAccumulatorSeconds > maxBacklogSeconds)
                _fixedAccumulatorSeconds %= _fixedDeltaSeconds;
        }
        else
        {
            _fixedAccumulatorSeconds = 0.0;
        }

        // Планируем frame cap для EndFrame().
        _targetFrameTicksThisFrame =
            (_vsyncMode == VSyncMode.Disabled && _maxFps > 0)
                ? (long)(Stopwatch.Frequency / (double)_maxFps)
                : 0;

        if (_targetFrameTicksThisFrame <= 0)
        {
            _lastTargetFrameTicks = 0;
            _nextFrameTimestamp = _frameStartTimestamp; // база = начало кадра
            return;
        }

        if (_targetFrameTicksThisFrame != _lastTargetFrameTicks)
        {
            _lastTargetFrameTicks = _targetFrameTicksThisFrame;
            _nextFrameTimestamp = _frameStartTimestamp; // ВАЖНО: база = начало кадра, не "после работы"
        }

        _nextFrameTimestamp += _targetFrameTicksThisFrame;
    }

    /// <summary>
    /// Пытается “съесть” один fixed-step. Должно вызываться движком в fixed-step цикле.
    /// </summary>
    public bool TryConsumeFixedStep(out float fixedDt)
    {
        fixedDt = 0f;

        if (!_useFixedStep)
            return false;

        if (_fixedAccumulatorSeconds < _fixedDeltaSeconds)
            return false;

        if (_fixedStepsThisFrame >= _maxFixedStepsPerFrame)
        {
            // backlog больше, чем разрешено обработать за кадр:
            // выбрасываем хвост, но оставляем дробный остаток.
            _fixedAccumulatorSeconds %= _fixedDeltaSeconds;
            return false;
        }

        _fixedAccumulatorSeconds -= _fixedDeltaSeconds;
        _fixedStepsThisFrame++;
        fixedDt = _fixedDeltaSeconds;
        return true;
    }

    /// <summary>
    /// Должно вызываться в конце кадра (если не запрошена остановка) для применения FPS cap при VSync Disabled.
    /// </summary>
    public void EndFrame()
    {
        var targetTicks = _targetFrameTicksThisFrame;
        if (targetTicks <= 0)
            return;

        var afterWork = Stopwatch.GetTimestamp();
        if (afterWork > _nextFrameTimestamp)
        {
            var behindTicks = afterWork - _nextFrameTimestamp;
            var skip = behindTicks / targetTicks + 1; // +1 гарантирует, что уйдём в будущее
            _nextFrameTimestamp += skip * targetTicks;
        }

        WaitUntil(_nextFrameTimestamp);
    }

    #endregion

    #region Private helpers

    private void ValidateFixedStepSettings()
    {
        // Эти проверки не меняют поведение для валидных конфигов и предотвращают
        // недетерминированные ошибки (деление на 0/бесконечные циклы) при некорректных настройках.
        if (!_useFixedStep) return;
        if (_fixedDeltaSeconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(_fixedDeltaSeconds), _fixedDeltaSeconds, "FixedDelta must be > 0.");

        if (_maxFixedStepsPerFrame <= 0)
            throw new ArgumentOutOfRangeException(nameof(_maxFixedStepsPerFrame), _maxFixedStepsPerFrame, "MaxFixedStepsPerFrame must be > 0.");
    }

    private static void WaitUntil(long targetTimestamp)
    {
        while (true)
        {
            var now = Stopwatch.GetTimestamp();
            var remainingTicks = targetTimestamp - now;
            if (remainingTicks <= 0)
                return;

            if (remainingTicks > SleepThresholdTicks)
            {
                var sleepTicks = remainingTicks - YieldThresholdTicks; // запас ~1ms
                var sleepMs = (int)(sleepTicks * 1000 / Stopwatch.Frequency);
                if (sleepMs > 0)
                    Thread.Sleep(sleepMs);

                continue;
            }

            if (remainingTicks > YieldThresholdTicks)
            {
                Thread.Yield();
                continue;
            }

            Thread.SpinWait(64);
        }
    }

    #endregion
}
