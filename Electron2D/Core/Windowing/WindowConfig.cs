namespace Electron2D;

public sealed class WindowConfig
{
    public string Title { get; set; } = "Electron2D";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public WindowMode Mode { get; set; } = WindowMode.Windowed;
    public WindowState State { get; set; } = WindowState.Normal;
}