using SDL3;

namespace Electron2D;

internal sealed class ResourceSystem
{
    private nint _renderer; // SDL_Renderer*
    private string _contentRoot = "Content";

    // string-id -> Texture facade
    private readonly Dictionary<string, Texture> _textures = new(StringComparer.Ordinal);

    public void Initialize(RenderSystem render, EngineConfig cfg)
    {
        _renderer = render.Handle;
        if (_renderer == 0)
            throw new InvalidOperationException("ResourceSystem.Initialize: RenderSystem is not initialized.");

        _contentRoot = string.IsNullOrWhiteSpace(cfg.ContentRoot) ? "Content" : cfg.ContentRoot;
    }

    public void Shutdown()
    {
        foreach (var kv in _textures)
        {
            var tex = kv.Value;
            if (tex.Handle != 0)
                SDL.DestroyTexture(tex.Handle);
        }

        _textures.Clear();
        _renderer = 0;
    }

    internal Texture GetTexture(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Texture id cannot be empty.", nameof(id));

        if (_textures.TryGetValue(id, out var cached))
            return cached;

        var path = ResolveTexturePath(id);
        var handle = Image.LoadTexture(_renderer, path);
        if (handle == 0)
            throw new InvalidOperationException($"Failed to load texture '{id}' from '{path}'. SDL error: {SDL.GetError()}");

        var tex = new Texture(handle);
        _textures.Add(id, tex);
        return tex;
    }

    internal bool TryGetTexture(string id, out Texture texture)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            texture = null!;
            return false;
        }

        if (_textures.TryGetValue(id, out texture!))
            return true;

        try
        {
            texture = GetTexture(id);
            return true;
        }
        catch
        {
            texture = null!;
            return false;
        }
    }

    internal void UnloadTexture(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        if (!_textures.Remove(id, out var tex)) return;
        if (tex.Handle != 0) SDL.DestroyTexture(tex.Handle);
    }

    private string ResolveTexturePath(string id)
    {
        // Если расширение не задано — считаем, что это png.
        var file = Path.HasExtension(id) ? id : id + ".png";

        // Если id уже абсолютный/содержит директорию — позволяем.
        return Path.IsPathRooted(file) ? file : Path.Combine(_contentRoot, file);
    }
}
