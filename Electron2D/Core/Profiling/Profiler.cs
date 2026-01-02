namespace Electron2D;

/// <summary>
/// Глобальный фасад профайлера (как Resources/Input).
/// Работает только когда <see cref="Engine"/> привязал <see cref="ProfilerSystem"/>.
/// </summary>
public static class Profiler
{
    private static ProfilerSystem? _system;
    private static bool _enabled = true;

    /// <summary>
    /// Включает/выключает профилирование. Если система ещё не привязана, значение будет применено при <c>Bind</c>.
    /// </summary>
    public static bool Enabled
    {
        get => _system?.Enabled ?? _enabled;
        set
        {
            _enabled = value;
            if (_system is not null)
                _system.Enabled = value;
        }
    }

    /// <summary>
    /// Снимок последнего завершённого кадра профайлера.
    /// </summary>
    public static ProfilerFrame LastFrame => _system?.LastFrame ?? default;

    /// <summary>
    /// Текущий курсор в кольцевом буфере истории (для графиков/оверлея).
    /// </summary>
    public static int HistoryCursor => _system?.HistoryCursor ?? 0;

    /// <summary>
    /// История кадров профайлера (кольцевой буфер).
    /// </summary>
    /// <remarks>
    /// Требует привязанной системы; иначе выбрасывает исключение, как и раньше (за счёт null-forgiving).
    /// </remarks>
    public static ReadOnlySpan<ProfilerFrame> HistoryRaw => _system!.HistoryRaw;

    #region Public API
    public static ProfilerScope Sample(ProfilerSampleId id)
    {
        var system = _system;
        if (system is null || !system.Enabled)
            return default;

        system.BeginSample(id);
        return new ProfilerScope(system, id);
    }

    public static void AddCounter(ProfilerCounterId id, long delta = 1)
    {
        var system = _system;
        if (system is null || !system.Enabled)
            return;

        system.AddCounter(id, delta);
    }

    public static void SetCounter(ProfilerCounterId id, long value)
    {
        var system = _system;
        if (system is null || !system.Enabled)
            return;

        system.SetCounter(id, value);
    }
    #endregion

    #region Internal API
    internal static void Bind(ProfilerSystem system)
    {
        _system = system;
        system.Enabled = _enabled;
    }

    internal static void Unbind()
    {
        _system = null;
    }
    #endregion
}
