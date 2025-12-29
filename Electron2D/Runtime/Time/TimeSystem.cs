using System.Diagnostics;

namespace Electron2D;

internal sealed class TimeSystem
{
    // Stopwatch
    private readonly double _invFreq = 1.0 / Stopwatch.Frequency;
    private long _lastTs;
    private long _frameStartTs;

    // Delta
    private float _timeScale = 1f;
    private float _delta;         // scaled
    private float _unscaledDelta; // raw

    // Fixed-step
    private bool _useFixedStep = true;
    private float _fixedDelta = 1f / 60f;
    private int _maxFixedStepsPerFrame = 8;
    private double _accumulator;
    private int _fixedStepsThisFrame;

    // Clamps
    private float _maxFrameDelta = 0.25f;

    // Frame cap (only when vsync disabled)
    private VSyncMode _vsync = VSyncMode.Enabled;
    private int _maxFps; // 0 => off

    private long _nextFrameTs;
    private long _lastTargetFrameTicks;
    private long _targetFrameTicksThisFrame;

    // Public read API
    public float DeltaTime => _delta;
    public float UnscaledDeltaTime => _unscaledDelta;
    public bool UseFixedStep => _useFixedStep;
    public float FixedDelta => _fixedDelta;

    public void Initialize(EngineConfig opt)
    {
        _timeScale = opt.TimeScale;

        _useFixedStep = opt.UseFixedStep;
        _fixedDelta = opt.Physics.FixedDelta;
        _maxFixedStepsPerFrame = opt.MaxFixedStepsPerFrame;
        _maxFrameDelta = opt.MaxFrameDeltaSeconds;

        _vsync = opt.Window.VSync;
        _maxFps = opt.MaxFps;

        _lastTs = Stopwatch.GetTimestamp();
        _nextFrameTs = _lastTs;
        _lastTargetFrameTicks = 0;

        _accumulator = 0.0;
        _fixedStepsThisFrame = 0;
    }

    // Вызывай при live-изменениях (engine runtime settings)
    public void Apply(
        bool useFixedStep,
        float fixedDeltaSeconds,
        int maxFixedStepsPerFrame,
        float timeScale,
        float maxFrameDeltaSeconds,
        VSyncMode vsync,
        int maxFps)
    {
        _useFixedStep = useFixedStep;
        _fixedDelta = fixedDeltaSeconds;
        _maxFixedStepsPerFrame = maxFixedStepsPerFrame;

        _timeScale = timeScale;
        _maxFrameDelta = maxFrameDeltaSeconds;

        _vsync = vsync;
        _maxFps = maxFps;

        if (!_useFixedStep)
            _accumulator = 0.0;
    }

    public void BeginFrame()
    {
        _frameStartTs = Stopwatch.GetTimestamp();

        // dt
        var dt = (_frameStartTs - _lastTs) * _invFreq;
        _lastTs = _frameStartTs;

        if (dt < 0) dt = 0;
        if (dt > _maxFrameDelta) dt = _maxFrameDelta;

        _unscaledDelta = (float)dt;
        _delta = _unscaledDelta * _timeScale;

        // fixed-step accumulator
        _fixedStepsThisFrame = 0;
        if (_useFixedStep)
        {
            _accumulator += _delta;

            // optional: общий лимит backlog, чтобы не расти бесконечно
            var maxBacklog = (double)_fixedDelta * _maxFixedStepsPerFrame;
            if (_accumulator > maxBacklog)
                _accumulator = _accumulator % _fixedDelta; // сохраняем дробный остаток
        }
        else
        {
            _accumulator = 0.0;
        }

        // plan frame cap for EndFrame()
        _targetFrameTicksThisFrame =
            (_vsync == VSyncMode.Disabled && _maxFps > 0)
                ? (long)(Stopwatch.Frequency / (double)_maxFps)
                : 0;

        if (_targetFrameTicksThisFrame <= 0)
        {
            _lastTargetFrameTicks = 0;
            _nextFrameTs = _frameStartTs; // база = начало кадра
            return;
        }

        if (_targetFrameTicksThisFrame != _lastTargetFrameTicks)
        {
            _lastTargetFrameTicks = _targetFrameTicksThisFrame;
            _nextFrameTs = _frameStartTs; // ВАЖНО: база = начало кадра, не "после работы"
        }

        _nextFrameTs += _targetFrameTicksThisFrame;
    }

    // Engine вызывает это в fixed-step цикле:
    public bool TryConsumeFixedStep(out float fixedDt)
    {
        fixedDt = 0f;

        if (!_useFixedStep)
            return false;

        if (_accumulator < _fixedDelta)
            return false;

        if (_fixedStepsThisFrame >= _maxFixedStepsPerFrame)
        {
            // backlog больше, чем разрешено обработать за кадр:
            // выбрасываем хвост, но оставляем дробный остаток
            _accumulator = _accumulator % _fixedDelta;
            return false;
        }

        _accumulator -= _fixedDelta;
        _fixedStepsThisFrame++;
        fixedDt = _fixedDelta;
        return true;
    }

    // Engine вызывает в конце кадра (если не requested stop)
    public void EndFrame()
    {
        var target = _targetFrameTicksThisFrame;
        if (target <= 0)
            return;

        var afterWork = Stopwatch.GetTimestamp();
        if (afterWork > _nextFrameTs)
        {
            var behind = afterWork - _nextFrameTs;
            var skip = behind / target + 1; // +1 гарантирует, что уйдём в будущее
            _nextFrameTs += skip * target;
        }

        WaitUntil(_nextFrameTs);
    }

    // --- Wait policy ---
    // Подбери под себя; ниже — безопасные дефолты (~2ms sleep, ~1ms yield)
    private static readonly long SleepThresholdTicks = Stopwatch.Frequency / 500; // ~2ms
    private static readonly long YieldThresholdTicks  = Stopwatch.Frequency / 1000; // ~1ms

    private static void WaitUntil(long targetTimestamp)
    {
        while (true)
        {
            var now = Stopwatch.GetTimestamp();
            var remaining = targetTimestamp - now;
            if (remaining <= 0) return;

            if (remaining > SleepThresholdTicks)
            {
                var sleepTicks = remaining - YieldThresholdTicks; // запас ~1ms
                var sleepMs = (int)(sleepTicks * 1000 / Stopwatch.Frequency);
                if (sleepMs > 0) Thread.Sleep(sleepMs);
                continue;
            }

            if (remaining > YieldThresholdTicks)
            {
                Thread.Yield();
                continue;
            }

            Thread.SpinWait(64);
        }
    }
}
