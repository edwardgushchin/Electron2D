namespace Electron2D;

public sealed class EngineConfig
{
    public WindowConfig Window { get; init; } = new();
    public PhysicsConfig Physics { get; init; } = new();

    public bool UseFixedStep { get; set; } = true;
    public float FixedDeltaSeconds { get; set; } = 1.0f / 60.0f;
    public int MaxFixedStepsPerFrame { get; init; } = 8;

    public int MaxFps { get; set; } = 0;
    public float TimeScale { get; set; } = 1f;

    // P0: TimeSystem ожидает это свойство
    public float MaxFrameDeltaSeconds { get; set; } = 0.25f;

    public float PixelPerUnit { get; set; } = 128f;

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
}