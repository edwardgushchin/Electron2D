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
            if (tex.IsValid)
                SDL.DestroyTexture(tex.Handle);
        }

        _textures.Clear();
        _renderer = 0;
    }

    public Texture GetTexture(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Texture id is empty.", nameof(id));

        if (_textures.TryGetValue(id, out var tex) && tex.IsValid)
            return tex;

        var loaded = LoadTextureHandleAndSize(id);
        tex = new Texture(loaded.Handle, loaded.W, loaded.H);

        // ВАЖНО: struct => нужно перезаписать значение в словаре
        _textures[id] = tex;
        return tex;
    }

    public bool TryGetTexture(string id, out Texture texture)
    {
        texture = default;
        if (string.IsNullOrWhiteSpace(id)) return false;

        return _textures.TryGetValue(id, out texture) && texture.IsValid;
    }

    /// <summary>
    /// Опасная операция без refcount: вызывайте только если уверены, что текстура больше нигде не используется.
    /// </summary>
    public void UnloadTexture(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        if (!_textures.TryGetValue(id, out var tex) || !tex.IsValid)
            return;

        SDL.DestroyTexture(tex.Handle);

        // Оставляем ключ, но инвалидируем значение (TryGetTexture вернёт false)
        _textures[id] = default;
    }

    private string ResolveTexturePath(string id)
    {
        var file = Path.HasExtension(id) ? id : id + ".png";
        return Path.IsPathRooted(file) ? file : Path.Combine(_contentRoot, file);
    }
    
    private (nint Handle, int W, int H) LoadTextureHandleAndSize(string id)
    {
        var path = ResolveTexturePath(id);

        var handle = Image.LoadTexture(_renderer, path);
        if (handle == 0)
            throw new InvalidOperationException($"LoadTexture failed for '{id}'. Path='{path}'. {SDL.GetError()}");

        if (!SDL.GetTextureSize(handle, out var w, out var h))
            throw new InvalidOperationException($"SDL.GetTextureSize failed for '{id}'. {SDL.GetError()}");

        return (handle, (int)w, (int)h);
    }
}
