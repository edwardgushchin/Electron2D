namespace Electron2D;

public sealed class WindowConfig
{
    public string Title { get; set; } = "Electron2D";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
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
    
    public WindowMode Mode { get; set; } = WindowMode.Windowed;
    public WindowState State { get; set; } = WindowState.Normal;
}