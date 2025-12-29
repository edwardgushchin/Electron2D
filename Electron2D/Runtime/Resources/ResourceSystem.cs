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

    public Texture GetTexture(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Texture id is empty.", nameof(id));

        if (_textures.TryGetValue(id, out var existing))
        {
            if (existing.IsValid)
                return existing;

            // Было unloaded — перезагружаем в тот же объект (важно для кэшей)
            var newHandle = LoadTextureHandle(id);
            existing.ReplaceHandle(newHandle);
            return existing;
        }

        var handle = LoadTextureHandle(id);
        var tex = new Texture(handle);
        _textures[id] = tex;
        return tex;
    }

    public bool TryGetTexture(string id, out Texture texture)
    {
        texture = null!;
        if (string.IsNullOrWhiteSpace(id)) return false;

        if (!_textures.TryGetValue(id, out var tex) || !tex.IsValid) return false;
        texture = tex;
        return true;

        // “Try” — не грузим с диска автоматически
    }

    public void UnloadTexture(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        if (!_textures.TryGetValue(id, out var tex))
            return;

        if (!tex.IsValid)
            return;

        SDL3.SDL.DestroyTexture(tex.Handle);
        tex.Invalidate();
    }

    private nint LoadTextureHandle(string id)
    {
        var path = ResolveTexturePath(id);

        // Image.LoadTexture требует валидный renderer
        var handle = Image.LoadTexture(_renderer, path);
        return handle == 0 ? throw new InvalidOperationException($"LoadTexture failed for '{id}'. Path='{path}'. {SDL3.SDL.GetError()}") : handle;
    }


    private string ResolveTexturePath(string id)
    {
        // Если расширение не задано — считаем, что это png.
        var file = Path.HasExtension(id) ? id : id + ".png";

        // Если id уже абсолютный/содержит директорию — позволяем.
        return Path.IsPathRooted(file) ? file : Path.Combine(_contentRoot, file);
    }
}
