namespace Electron2D;

/// <summary>
/// Конфигурация движка (параметры и лимиты подсистем на момент инициализации).
/// </summary>
/// <remarks>
/// Часть параметров имеет <c>init</c>-сеттеры и предполагает установку только до запуска движка.
/// </remarks>
public sealed class EngineConfig
{
    #region Core systems
    public WindowConfig Window { get; init; } = new();
    public PhysicsConfig Physics { get; init; } = new();
    
    public RenderPresentationConfig Presentation { get; init; } = new();
    #endregion

    #region Timing
    public VSyncMode VSync { get; set; } = VSyncMode.Disabled;

    /// <summary>
    /// Интервал VSync для рендерера (1 = каждый vblank, 2 = каждый второй vblank и т.д.).
    /// Используется только при <see cref="VSync"/> == <see cref="VSyncMode.Enabled"/>.
    /// По умолчанию 1.
    /// </summary>
    /// <remarks>
    /// Драйвер/бэкенд может не поддерживать все значения, поэтому RenderSystem обязан
    /// проверять успешность установки и при необходимости делать fallback.
    /// </remarks>
    public int VSyncInterval { get; set; } = 1;

    /// <summary>
    /// Использовать фиксированный шаг для физики и <c>PhysicsProcess</c>.
    /// </summary>
    public bool UseFixedStep { get; set; } = true;

    /// <summary>
    /// Максимальное число фиксированных шагов за кадр (защита от спирали смерти при просадках).
    /// </summary>
    public int MaxFixedStepsPerFrame { get; init; } = 8;

    /// <summary>
    /// Ограничение FPS, если VSync выключен. 0 или меньше — без ограничения.
    /// </summary>
    public int MaxFps { get; set; } = 0;

    /// <summary>
    /// Масштаб времени (1.0 = реальное время).
    /// </summary>
    public float TimeScale { get; set; } = 1f;
    #endregion

    #region Events
    public int EngineEventsPerFrame { get; init; } = 16;
    public int WindowEventsPerFrame { get; init; } = 256;
    public int KeyboardEventsPerFrame { get; init; } = 512;
    public int MouseEventsPerFrame { get; init; } = 512;
    #endregion

    #region Rendering
    public int RenderQueueCapacity { get; init; } = 8192;

    /// <summary>
    /// Ёмкость очереди deferred-free (сколько Node может быть поставлено в QueueFree за кадр без аллокаций).
    /// </summary>
    public int DeferredFreeQueueCapacity { get; init; } = 4096;
    
    public FilterMode TextureFilter { get; set; } = FilterMode.Linear;
    #endregion

    #region Content
    /// <summary>
    /// Корневая папка контента (простая модель dev/offline-first заготовки).
    /// </summary>
    public string ContentRoot { get; init; } = "Content";
    #endregion

    #region Debug
    /// <summary>
    /// Рисовать отладочную сетку (1 unit) на фоне (world-space).
    /// </summary>
    public bool DebugGridEnabled { get; set; } = false;

    /// <summary>
    /// Цвет фона при включенной DebugGrid (заменяет SceneTree.ClearColor только на уровне рендера).
    /// Формат: 0xRRGGBBAA.
    /// </summary>
    public Color DebugGridColor { get; set; } = new(47, 47, 56);

    /// <summary>
    /// Цвет линий сетки (шаг 1 unit). Оси будут автоматически чуть ярче.
    /// Формат: 0xRRGGBBAA.
    /// </summary>
    public Color DebugGridLineColor { get; set; } = new(71, 71, 84);
    #endregion
}
