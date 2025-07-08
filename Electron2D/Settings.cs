namespace Electron2D;

public class Settings
{
    public int Width { get; set; } = 800;

    public int Height { get; set; } = 600;
    
    public bool Resizable { get; set; }
    
    public FullscreenMode Fullscreen { get; set; }
    
    public VSyncMode VSync { get; set; }

    /*public LogLevel LogLevel
    {
        get => Logger.Level;
        set => Logger.Level = value;
    }*/


    public static Settings LoadFromFile(string filename)
    {
        return new Settings();
    }
}