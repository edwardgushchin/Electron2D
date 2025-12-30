namespace Electron2D;

public sealed class EngineConfig
{
    public WindowConfig Window { get; init; } = new();
    public PhysicsConfig Physics { get; init; } = new();
    
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

    public bool UseFixedStep { get; set; } = true;
    public int MaxFixedStepsPerFrame { get; init; } = 8;

    public int MaxFps { get; set; } = 0;
    public float TimeScale { get; set; } = 1f;
    
    public int EngineEventsPerFrame { get; init; } = 16;
    public int WindowEventsPerFrame { get; init; } = 256;
    public int InputEventsPerFrame  { get; init; } = 512;

    public int RenderQueueCapacity { get; init; } = 8192;

    /// <summary>
    /// Ёмкость очереди deferred-free (сколько Node может быть поставлено в QueueFree за кадр без аллокаций).
    /// </summary>
    public int DeferredFreeQueueCapacity { get; init; } = 4096;

    /// <summary>
    /// Корневая папка контента (простая модель dev/offline-first заготовки).
    /// </summary>
    public string ContentRoot { get; init; } = "Content";
    
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
    public Color DebugGridLineColor { get; set; } = new Color(71, 71, 84);
}