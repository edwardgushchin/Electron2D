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
            if (existing.IsValid) return existing;

            var t = LoadTextureHandleAndSize(id);
            existing.ReplaceHandle(t.Handle, t.W, t.H);
            return existing;
        }

        var loaded = LoadTextureHandleAndSize(id);
        var tex = new Texture(loaded.Handle, loaded.W, loaded.H);
        _textures.Add(id, tex);
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

        SDL.DestroyTexture(tex.Handle);
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
    
    private (nint Handle, int W, int H) LoadTextureHandleAndSize(string id)
    {
        var path = Path.Combine(_contentRoot, id);

        var handle = Image.LoadTexture(_renderer, path);
        if (handle == 0)
            throw new InvalidOperationException($"LoadTexture failed for '{id}'. Path='{path}'. {SDL.GetError()}");

        return !SDL.GetTextureSize(handle, out var w, out var h) ? throw new InvalidOperationException($"SDL.GetTextureSize failed for '{id}'. {SDL.GetError()}") : (handle, (int)w, (int)h);
    }
}
