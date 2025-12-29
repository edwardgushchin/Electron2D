using System.Diagnostics;

namespace Electron2D;

internal sealed class TimeSystem
{
    private long _lastTicks;
    private double _invFreq;

    private float _timeScale = 1f;

    private float _delta;
    private float _unscaledDelta;

    private float _accum;
    private float _fixedDelta = 1f / 60f;
    private int _maxFixedStepsPerFrame = 8;

    public float DeltaTime => _delta;
    public float UnscaledDeltaTime => _unscaledDelta;
    public float FixedDelta => _fixedDelta;

    public void Initialize(EngineConfig cfg)
    {
        _timeScale = cfg.TimeScale;
        _fixedDelta = cfg.Physics.FixedDelta;
        _maxFixedStepsPerFrame = cfg.MaxFixedStepsPerFrame;

        _invFreq = 1.0 / Stopwatch.Frequency;
        _lastTicks = Stopwatch.GetTimestamp();

        _accum = 0f;
        _delta = 0f;
        _unscaledDelta = 0f;
    }

    public void BeginFrame()
    {
        var now = Stopwatch.GetTimestamp();
        var dt = (now - _lastTicks) * _invFreq;
        _lastTicks = now;

        // Защита от гигантских прыжков (alt-tab, отладчик)
        if (dt < 0) dt = 0;
        if (dt > 0.25) dt = 0.25;

        _unscaledDelta = (float)dt;
        _delta = _unscaledDelta * _timeScale;

        _accum += _delta;

        // Clamp backlog, чтобы не уйти в “spiral of death”
        var maxBacklog = _fixedDelta * _maxFixedStepsPerFrame;
        if (_accum > maxBacklog) _accum = maxBacklog;
    }

    public bool TryConsumeFixedStep(out float fixedDt)
    {
        if (_accum >= _fixedDelta)
        {
            _accum -= _fixedDelta;
            fixedDt = _fixedDelta;
            return true;
        }

        fixedDt = 0f;
        return false;
    }

    public void SetTimeScale(float value) => _timeScale = value;
}