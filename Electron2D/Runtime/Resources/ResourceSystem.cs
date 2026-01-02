using SDL3;

namespace Electron2D;

/// <summary>
/// Управляет загрузкой/кэшированием текстур по строковому идентификатору.
/// Важно: система не ведёт refcount — выгрузка может быть опасной, если текстура ещё используется где-то в рендере.
/// </summary>
internal sealed class ResourceSystem
{
    #region Constants

    private const string DefaultContentRoot = "Content";

    #endregion

    #region Instance fields

    /// <summary>SDL_Renderer* (handle), необходим для загрузки текстур.</summary>
    private nint _rendererHandle;

    private string _contentRootPath = DefaultContentRoot;

    // string id -> Texture facade
    private readonly Dictionary<string, Texture> _texturesById = new(StringComparer.Ordinal);

    #endregion

    #region Public API

    /// <summary>
    /// Инициализирует систему ресурсов ссылкой на активный рендерер и конфигом движка.
    /// </summary>
    /// <exception cref="InvalidOperationException">Если <paramref name="render"/> не инициализирован.</exception>
    public void Initialize(RenderSystem render, EngineConfig cfg)
    {
        _rendererHandle = render.Handle;
        if (_rendererHandle == 0)
            throw new InvalidOperationException("ResourceSystem.Initialize: RenderSystem is not initialized.");

        _contentRootPath = string.IsNullOrWhiteSpace(cfg.ContentRoot) ? DefaultContentRoot : cfg.ContentRoot;
    }

    /// <summary>
    /// Освобождает все загруженные текстуры и сбрасывает состояние системы.
    /// </summary>
    public void Shutdown()
    {
        foreach (var kvp in _texturesById)
        {
            var texture = kvp.Value;
            if (texture.IsValid)
                SDL.DestroyTexture(texture.Handle);
        }

        _texturesById.Clear();
        _rendererHandle = 0;
    }

    /// <summary>
    /// Возвращает текстуру по идентификатору, загружая её при необходимости.
    /// </summary>
    /// <param name="path">Идентификатор текстуры (без расширения или с расширением; по умолчанию .png).</param>
    /// <exception cref="ArgumentException">Если <paramref name="path"/> пустой/пробельный.</exception>
    /// <exception cref="InvalidOperationException">Если система не инициализирована или загрузка/запрос размера не удались.</exception>
    public Texture GetTexture(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Texture id is empty.", nameof(path));

        ThrowIfNotInitialized();

        if (_texturesById.TryGetValue(path, out var cached) && cached.IsValid)
            return cached;

        var loaded = LoadTextureHandleAndSize(path);

        if (_texturesById.TryGetValue(path, out cached))
        {
            // hot-reload semantics: сохраняем объект, обновляем handle/size
            if (cached.IsValid)
                SDL.DestroyTexture(cached.Handle);

            cached.Reset(loaded.Handle, loaded.W, loaded.H);
            return cached;
        }

        var texture = new Texture(loaded.Handle, loaded.W, loaded.H);
        _texturesById[path] = texture;
        return texture;
    }

    /// <summary>
    /// Пытается получить ранее загруженную и валидную текстуру.
    /// </summary>
    public bool TryGetTexture(string path, out Texture texture)
    {
        texture = default;

        if (string.IsNullOrWhiteSpace(path))
            return false;

        return _texturesById.TryGetValue(path, out texture) && texture.IsValid;
    }

    /// <summary>
    /// Опасная операция без refcount: вызывайте только если уверены, что текстура больше нигде не используется.
    /// </summary>
    public void UnloadTexture(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!_texturesById.TryGetValue(path, out var texture) || !texture.IsValid)
            return;

        SDL.DestroyTexture(texture.Handle);
        texture.Invalidate();

        // либо удалить ключ:
        // _texturesById.Remove(path);
    }

    #endregion

    #region Private helpers

    private void ThrowIfNotInitialized()
    {
        // Ранее этот случай обычно проявлялся как неудачная загрузка (Image.LoadTexture с renderer=0).
        // Явный guard делает ошибку более прозрачной и стабильной.
        if (_rendererHandle == 0)
            throw new InvalidOperationException("ResourceSystem is not initialized. Call Initialize() first.");
    }

    private string ResolveTexturePath(string path)
    {
        var fileName = Path.HasExtension(path) ? path : path + ".png";
        return Path.IsPathRooted(fileName) ? fileName : Path.Combine(_contentRootPath, fileName);
    }

    private (nint Handle, int W, int H) LoadTextureHandleAndSize(string path)
    {
        var resolveTexturePath = ResolveTexturePath(path);

        var handle = Image.LoadTexture(_rendererHandle, resolveTexturePath);
        if (handle == 0)
            throw new InvalidOperationException($"LoadTexture failed for '{path}'. Path='{resolveTexturePath}'. {SDL.GetError()}");

        return !SDL.GetTextureSize(handle, out var w, out var h) ? throw new InvalidOperationException($"SDL.GetTextureSize failed for '{path}'. {SDL.GetError()}") : (handle, (int)w, (int)h);
    }

    #endregion
}
