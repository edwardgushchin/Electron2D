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
    /// <param name="id">Идентификатор текстуры (без расширения или с расширением; по умолчанию .png).</param>
    /// <exception cref="ArgumentException">Если <paramref name="id"/> пустой/пробельный.</exception>
    /// <exception cref="InvalidOperationException">Если система не инициализирована или загрузка/запрос размера не удались.</exception>
    public Texture GetTexture(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Texture id is empty.", nameof(id));

        ThrowIfNotInitialized();

        if (_texturesById.TryGetValue(id, out var cached) && cached.IsValid)
            return cached;

        var loaded = LoadTextureHandleAndSize(id);
        var texture = new Texture(loaded.Handle, loaded.W, loaded.H);

        // ВАЖНО: Texture — struct, поэтому при обновлении нужно перезаписать значение в словаре.
        _texturesById[id] = texture;

        return texture;
    }

    /// <summary>
    /// Пытается получить ранее загруженную и валидную текстуру.
    /// </summary>
    public bool TryGetTexture(string id, out Texture texture)
    {
        texture = default;

        if (string.IsNullOrWhiteSpace(id))
            return false;

        return _texturesById.TryGetValue(id, out texture) && texture.IsValid;
    }

    /// <summary>
    /// Опасная операция без refcount: вызывайте только если уверены, что текстура больше нигде не используется.
    /// </summary>
    public void UnloadTexture(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (!_texturesById.TryGetValue(id, out var texture) || !texture.IsValid)
            return;

        SDL.DestroyTexture(texture.Handle);

        // Оставляем ключ, но инвалидируем значение (TryGetTexture вернёт false).
        _texturesById[id] = default;
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

    private string ResolveTexturePath(string id)
    {
        var fileName = Path.HasExtension(id) ? id : id + ".png";
        return Path.IsPathRooted(fileName) ? fileName : Path.Combine(_contentRootPath, fileName);
    }

    private (nint Handle, int W, int H) LoadTextureHandleAndSize(string id)
    {
        var path = ResolveTexturePath(id);

        var handle = Image.LoadTexture(_rendererHandle, path);
        if (handle == 0)
            throw new InvalidOperationException($"LoadTexture failed for '{id}'. Path='{path}'. {SDL.GetError()}");

        return !SDL.GetTextureSize(handle, out var w, out var h) ? throw new InvalidOperationException($"SDL.GetTextureSize failed for '{id}'. {SDL.GetError()}") : (handle, (int)w, (int)h);
    }

    #endregion
}
