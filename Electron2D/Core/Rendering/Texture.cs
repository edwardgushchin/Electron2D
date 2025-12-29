namespace Electron2D;

public sealed class Texture
{
    internal nint Handle { get; private set; } // SDL_Texture*
    internal bool IsValid => Handle != 0;

    internal Texture(nint handle) => Handle = handle;

    internal void ReplaceHandle(nint newHandle) => Handle = newHandle;

    internal void Invalidate() => Handle = 0;
}