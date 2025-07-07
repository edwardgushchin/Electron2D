using SDL3;

namespace Electron2D.Resources;

public class Texture : IResource, IDisposable
{
    internal IntPtr Handle;
    
    private readonly Action<Texture> _onNoReferences;

    internal Texture(IntPtr handle, Action<Texture> onNoReferences)
    {
        Handle = handle;
        SDL.GetTextureSize(Handle, out var texWidth, out var texHeight);
        Height = texHeight;
        Width = texWidth;
        _onNoReferences = onNoReferences;
    }

    public float Height { get; }
    
    public float Width { get; }
    
    internal int RefCount { get; private set; }

    internal void AddReference()
    {
        RefCount++;
    }

    internal void RemoveReference()
    {
        RefCount--;
        if (RefCount <= 0)
        {
            _onNoReferences?.Invoke(this);
        }
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            SDL.DestroyTexture(Handle);
            Handle = IntPtr.Zero;
        }
    }
}