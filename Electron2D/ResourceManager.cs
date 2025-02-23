using SDL3;

namespace Electron2D;

internal static class ResourceManager
{
    private static Renderer? _renderer;

    internal static void Initialize(Renderer renderer)
    {
        _renderer = renderer;
    }

    public static Texture LoadTexture(string path)
    {
        if (_renderer == null)
            throw new ElectronException("Renderer is not initialized. Cannot load texture.");
        
        var rendererHandle = _renderer.GetRendererHandle();
        var textureHandle = Image.LoadTexture(rendererHandle, path);
        
        if (textureHandle == IntPtr.Zero)
            throw new ElectronException($"Failed to load texture: {SDL.GetError()}");
        
        return new Texture(textureHandle);
    }
}