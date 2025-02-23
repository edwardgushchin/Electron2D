using SDL3;

namespace Electron2D;

public sealed class Texture
{
    internal IntPtr Handle { get; }

    internal Texture(IntPtr handle)
    {
        Handle = handle;
    }
    
    public static Texture LoadFromFile(string path)
    {
        return ResourceManager.LoadTexture(path);
    }

    public void Destroy()
    {
        SDL.DestroyTexture(Handle);
    }
}