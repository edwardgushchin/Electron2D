using SDL3;

namespace Electron2D.Resources;

public class Texture : IResource, IDisposable
{
    internal IntPtr Handle;
    
    private readonly Action<Texture> _onNoReferences;
    
    private BlendMode _blendMode = BlendMode.Blend;
    private ScaleMode _scaleMode = ScaleMode.Linear;

    internal Texture(IntPtr handle, Action<Texture> onNoReferences)
    {
        Handle = handle;
        SDL.GetTextureSize(Handle, out var texWidth, out var texHeight);
        Height = texHeight;
        Width = texWidth;
        _onNoReferences = onNoReferences;
    }
    
    public void SetColorMod(byte r, byte g, byte b)
    {
        if (Handle != IntPtr.Zero)
            SDL.SetTextureColorMod(Handle, r, g, b);
    }

    public void SetAlphaMod(byte a)
    {
        if (Handle != IntPtr.Zero)
            SDL.SetTextureAlphaMod(Handle, a);
    }
    
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
    
    public float Height { get; }
    
    public float Width { get; }
    
    public BlendMode BlendMode
    {
        get => _blendMode;
        set
        {
            if (_blendMode == value || Handle == IntPtr.Zero) return;
            _blendMode = value;
            SDL.SetTextureBlendMode(Handle, (SDL.BlendMode)value);
        }
    }
    
    public ScaleMode ScaleMode
    {
        get => _scaleMode;
        set
        {
            if (_scaleMode == value || Handle == IntPtr.Zero) return;
            _scaleMode = value;
            SDL.SetTextureScaleMode(Handle, (SDL.ScaleMode)value);
        }
    }
    
    internal int RefCount { get; private set; }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            SDL.DestroyTexture(Handle);
            Handle = IntPtr.Zero;
        }
    }
}