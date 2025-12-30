// FILE: Electron2D/Core/Rendering/Texture.cs
namespace Electron2D;

/// <summary>
/// Лёгкий фасад текстуры. Не владеет ресурсом.
/// Владение и уничтожение SDL_Texture выполняет ResourceSystem.
/// </summary>
public readonly struct Texture
{
    internal nint Handle { get; }
    public bool IsValid => Handle != 0;

    public int Width  { get; }
    public int Height { get; }

    internal Texture(nint handle, int width, int height)
    {
        Handle = handle;
        Width = width;
        Height = height;
    }
}