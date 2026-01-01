namespace Electron2D;

/// <summary>
/// Глобальный фасад (как Resources/Input). Работает только когда Engine привязал ProfilerSystem.
/// </summary>
public static class Profiler
{
    private static ProfilerSystem? _sys;
    private static bool _enabled = true;

    public static bool Enabled
    {
        get => _sys?.Enabled ?? _enabled;
        set
        {
            _enabled = value;
            _sys?.Enabled = value;
        }
    }

    public static ProfilerFrame LastFrame => _sys?.LastFrame ?? default;

    /// <summary>Raw ring buffer (по кругу) и текущий курсор доступны для графиков/оверлея.</summary>
    public static int HistoryCursor => _sys?.HistoryCursor ?? 0;
    public static ReadOnlySpan<ProfilerFrame> HistoryRaw => _sys!.HistoryRaw;

    public static ProfilerScope Sample(ProfilerSampleId id)
    {
        var sys = _sys;
        if (sys is null || !sys.Enabled) return default;
        sys.BeginSample(id);
        return new ProfilerScope(sys, id);
    }

    public static void AddCounter(ProfilerCounterId id, long delta = 1)
    {
        var sys = _sys;
        if (sys is null || !sys.Enabled) return;
        sys.AddCounter(id, delta);
    }

    public static void SetCounter(ProfilerCounterId id, long value)
    {
        var sys = _sys;
        if (sys is null || !sys.Enabled) return;
        sys.SetCounter(id, value);
    }

    internal static void Bind(ProfilerSystem sys)
    {
        _sys = sys;
        sys.Enabled = _enabled;
    }

    internal static void Unbind()
    {
        _sys = null;
    }
}