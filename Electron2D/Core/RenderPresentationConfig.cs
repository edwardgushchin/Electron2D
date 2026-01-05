namespace Electron2D;

public sealed class RenderPresentationConfig
{
    /// <summary>
    /// Базовое (виртуальное) разрешение, в координатах которого работает SDL_Renderer при включённой презентации.
    /// 0 или меньше => использовать текущий размер вывода (фактический размер окна на момент инициализации).
    /// </summary>
    public int VirtualWidth { get; set; } = 0;

    /// <inheritdoc cref="VirtualWidth"/>
    public int VirtualHeight { get; set; } = 0;

    /// <summary>
    /// Режим презентации (SDL_Renderer logical presentation).
    /// По умолчанию — Disabled, чтобы не было неожиданных “полос” и изменения масштаба из коробки.
    /// </summary>
    public PresentationMode Mode { get; set; } = PresentationMode.Disabled;

    // Полезно для пикселя: не давать окну стать меньше базы.
    public bool ClampWindowMinSizeToVirtual { get; set; } = true;
}