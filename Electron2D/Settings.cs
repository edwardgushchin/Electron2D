namespace Electron2D;

public struct Settings
{
    public int Width { get; set; }
    
    public int Height { get; set; }
    
    public bool Resizable { get; set; }
    
    public FullscreenMode Fullscreen { get; set; }
    
    public VSyncMode VSync { get; set; }


    public static Settings LoadFromFile(string filename)
    {
        return new Settings();
    }
}