namespace Electron2D;

public sealed class EngineConfig
{
    public WindowConfig Window { get; init; } = new();
    public PhysicsConfig Physics { get; init; } = new();
    
    /// <summary>
    /// Включает использование фиксированного шага симуляции.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Если <c>true</c>, движок может выполнять один или несколько «fixed» шагов в пределах одного кадра,
    /// чтобы симуляция оставалась стабильной при просадках FPS. Длительность шага задаётся
    /// <see cref="FixedDeltaSeconds"/>, а верхняя граница количества шагов — <see cref="MaxFixedStepsPerFrame"/>.
    /// </para>
    /// <para>
    /// Если <c>false</c>, движок может обновлять симуляцию только по переменному <c>deltaTime</c> кадра.
    /// </para>
    /// </remarks>
    public bool UseFixedStep { get; set; } = true;
    
    /// <summary>
    /// Длительность одного фиксированного шага симуляции в секундах.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Типичное значение для 60 Гц: <c>1.0 / 60.0</c>.
    /// Используется только если <see cref="UseFixedStep"/> включён.
    /// </para>
    /// </remarks>
    public double FixedDeltaSeconds { get; set; } = 1.0 / 60.0;
    
    /// <summary>
    /// Максимально допустимое количество фиксированных шагов симуляции за один кадр.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ограничение защищает от «спирали смерти» (death spiral), когда при большом лаге движок пытается
    /// догнать симуляцию бесконечным числом fixed-шагов, усугубляя отставание.
    /// </para>
    /// </remarks>
    public int MaxFixedStepsPerFrame { get; init; } = 8;
    
    /// <summary>
    /// Максимально допустимое значение <c>deltaTime</c> кадра в секундах.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Используется для отсечения слишком больших временных скачков (например, после паузы, сворачивания окна,
    /// отладки под брейкпоинтом). Как правило, движок «клампит» входной <c>deltaTime</c> этим значением.
    /// </para>
    /// </remarks>
    public double MaxFrameDeltaSeconds { get; set; } = 0.25;
    
    /// <summary>
    /// Целевой FPS (кадров в секунду), если используется ограничение частоты кадров.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Конкретный механизм ограничения (sleep/spin-wait, учёт VSync и т.п.) определяется реализацией игрового цикла.
    /// Значение <c>0</c> или отрицательное трактуется как отсутствие целевого ограничения
    /// </para>
    /// </remarks>
    public int MaxFps { get; set; } = 0;

    public float TimeScale { get; set; } = 1f;

    /// <summary>Pixels Per Unit (PPU). Конверсия World(Unit)->Screen(px).</summary>
    public float PixelPerUnit { get; set; } = 128f;

    public int MaxDeferredFreePerFrame { get; init; } = 1024;

    public int EngineEventsPerFrame { get; init; } = 16;
    public int WindowEventsPerFrame { get; init; } = 256;
    public int InputEventsPerFrame { get; init; } = 512;

    public int RenderQueueCapacity { get; init; } = 8192;

    
    
    
}