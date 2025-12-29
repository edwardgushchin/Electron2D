// FILE: Electron2D/Core/Rendering/Texture.cs
using SDL3;

namespace Electron2D;

public sealed class Texture
{
    internal nint Handle { get; private set; } // SDL_Texture*
    internal bool IsValid => Handle != 0;

    private int _width;
    private int _height;

    internal int Width
    {
        get
        {
            EnsureSize(); 
            return _width;
        }
    }

    internal int Height
    {
        get
        {
            EnsureSize(); 
            return _height;
        }
    }

    internal Texture(nint handle)
    {
        Handle = handle;
        _width = 0;
        _height = 0;
    }

    internal void ReplaceHandle(nint newHandle)
    {
        Handle = newHandle;
        _width = 0;
        _height = 0;
    }

    internal void Invalidate()
    {
        Handle = 0;
        _width = 0;
        _height = 0;
    }

    private void EnsureSize()
    {
        if (_width != 0 && _height != 0) return;
        if (Handle == 0) return;

        // SDL3: SDL_GetTextureSize возвращает float-ы
        if (!SDL.GetTextureSize(Handle, out var w, out var h))
            return;

        // Приводим к пиксельным int (без аллокаций)
        var wi = (int)w;
        var hi = (int)h;

        if (wi <= 0 || hi <= 0) return;

        _width = wi;
        _height = hi;
    }
}