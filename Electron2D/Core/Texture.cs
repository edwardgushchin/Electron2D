namespace Electron2D;

public class Texture
{
    internal readonly nint Handle; // SDL_Texture*
    internal Texture(nint handle) => Handle = handle;
}