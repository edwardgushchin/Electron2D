using System.Numerics;
using System.Text.Json;
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

    // textureId -> parsed meta (null means "meta not found")
    private readonly Dictionary<string, TextureMetaAsset?> _textureMetaById = new(StringComparer.Ordinal);

    // textureId -> computed sprite defaults (from meta + fallbacks)
    private readonly Dictionary<string, SpriteImportDefaults> _spriteDefaultsByTextureId = new(StringComparer.Ordinal);

    // string id -> SpriteAnimation
    private readonly Dictionary<string, SpriteAnimation> _spriteAnimationsById = new(StringComparer.Ordinal);

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
        _textureMetaById.Clear();
        _spriteDefaultsByTextureId.Clear();
        _spriteAnimationsById.Clear();
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

        // Нормализуем ключ: "foo" == "foo.png".
        path = NormalizeTextureId(path);

        if (_texturesById.TryGetValue(path, out var cached) && cached.IsValid)
            return cached;

        var resolveTexturePath = ResolveTexturePath(path);
        var loaded = LoadTextureHandleAndSize(path, resolveTexturePath);

        if (_texturesById.TryGetValue(path, out cached))
        {
            // hot-reload semantics: сохраняем объект, обновляем handle/size
            if (cached.IsValid)
                SDL.DestroyTexture(cached.Handle);

            cached.Reset(loaded.Handle, loaded.W, loaded.H);
            ApplyTextureMeta(path, resolveTexturePath, cached);
            return cached;
        }

        var texture = new Texture(loaded.Handle, loaded.W, loaded.H);
        ApplyTextureMeta(path, resolveTexturePath, texture);
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

        path = NormalizeTextureId(path);

        return _texturesById.TryGetValue(path, out texture) && texture.IsValid;
    }

    /// <summary>
    /// Опасная операция без refcount: вызывайте только если уверены, что текстура больше нигде не используется.
    /// </summary>
    public void UnloadTexture(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        path = NormalizeTextureId(path);

        if (!_texturesById.TryGetValue(path, out var texture) || !texture.IsValid)
            return;

        SDL.DestroyTexture(texture.Handle);
        texture.Invalidate();

        // либо удалить ключ:
        // _texturesById.Remove(path);
    }

    /// <summary>
    /// Возвращает дефолтные импорт-настройки спрайтов, привязанные к текстуре.
    /// </summary>
    public SpriteImportDefaults GetSpriteImportDefaults(string texturePath)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
            throw new ArgumentException("Texture id is empty.", nameof(texturePath));

        ThrowIfNotInitialized();

        var id = NormalizeTextureId(texturePath);
        if (_spriteDefaultsByTextureId.TryGetValue(id, out var cached))
            return cached;

        var resolved = ResolveTexturePath(id);
        var meta = GetOrLoadTextureMeta(id, resolved);

        var defaults = SpriteImportDefaults.Default;
        if (meta?.Sprite is not null)
        {
            if (meta.Sprite.Ppu is { } ppu)
                defaults = defaults with { PixelsPerUnit = ppu };

            if (meta.Sprite.Pivot is { } pivot)
                defaults = defaults with { Pivot = pivot };

            if (!string.IsNullOrWhiteSpace(meta.Sprite.Filter))
            {
                if (!ResourceJson.TryParseFilterMode(meta.Sprite.Filter, out var fm))
                    throw new InvalidOperationException($"Invalid sprite.filter in meta for '{id}': '{meta.Sprite.Filter}'.");

                defaults = defaults with { FilterMode = fm };
            }
        }

        _spriteDefaultsByTextureId[id] = defaults;
        return defaults;
    }

    /// <summary>
    /// Возвращает набор клипов спрайт-анимации из "*.animset" (JSON), загружая при необходимости.
    /// </summary>
    public SpriteAnimation GetSpriteAnimation(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Animation id is empty.", nameof(path));

        ThrowIfNotInitialized();

        var id = NormalizeAnimSetId(path);

        if (_spriteAnimationsById.TryGetValue(id, out var cached))
            return cached;

        var anim = new SpriteAnimation();
        LoadAnimSetInto(id, anim);
        _spriteAnimationsById[id] = anim;
        return anim;
    }

    /// <summary>
    /// Принудительно перечитывает *.animset и обновляет существующий объект (если он уже был загружен).
    /// </summary>
    public SpriteAnimation ReloadSpriteAnimation(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Animation id is empty.", nameof(path));

        ThrowIfNotInitialized();

        var id = NormalizeAnimSetId(path);

        if (!_spriteAnimationsById.TryGetValue(id, out var anim))
        {
            anim = new SpriteAnimation();
            _spriteAnimationsById[id] = anim;
        }

        LoadAnimSetInto(id, anim);
        return anim;
    }

    public bool TryGetSpriteAnimation(string path, out SpriteAnimation anim)
    {
        anim = null!;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        path = NormalizeAnimSetId(path);
        return _spriteAnimationsById.TryGetValue(path, out anim);
    }

    public void UnloadSpriteAnimation(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        path = NormalizeAnimSetId(path);
        _spriteAnimationsById.Remove(path);
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

    private string ResolveAnimSetPath(string animSetId)
    {
        // animSetId уже должен быть нормализован (с расширением)
        return Path.IsPathRooted(animSetId) ? animSetId : Path.Combine(_contentRootPath, animSetId);
    }

    private static string NormalizeTextureId(string path)
    {
        // Разрешаем "character/char_blue" -> "character/char_blue.png".
        // Если расширение задано явно — оставляем как есть.
        path = NormalizeSeparators(path);
        return Path.HasExtension(path) ? path : path + ".png";
    }

    private static string NormalizeAnimSetId(string path)
    {
        path = NormalizeSeparators(path);
        return Path.HasExtension(path) ? path : path + ".animset";
    }

    private static string NormalizeSeparators(string path)
        => path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

    private (nint Handle, int W, int H) LoadTextureHandleAndSize(string path, string resolvedTexturePath)
    {
        var handle = Image.LoadTexture(_rendererHandle, resolvedTexturePath);
        if (handle == 0)
            throw new InvalidOperationException($"LoadTexture failed for '{path}'. Path='{resolvedTexturePath}'. {SDL.GetError()}");

        return !SDL.GetTextureSize(handle, out var w, out var h)
            ? throw new InvalidOperationException($"SDL.GetTextureSize failed for '{path}'. {SDL.GetError()}")
            : (handle, (int)w, (int)h);
    }

    private void ApplyTextureMeta(string textureId, string resolvedTexturePath, Texture texture)
    {
        var meta = GetOrLoadTextureMeta(textureId, resolvedTexturePath);

        // 1) Texture-wide filter
        if (meta?.Texture is not null && !string.IsNullOrWhiteSpace(meta.Texture.Filter))
        {
            if (!ResourceJson.TryParseFilterMode(meta.Texture.Filter, out var textureFilter))
                throw new InvalidOperationException($"Invalid texture.filter in meta for '{textureId}': '{meta.Texture.Filter}'.");

            texture.FilterMode = textureFilter;
        }

        // 2) Sprite defaults (лениво кешируем по запросу GetSpriteImportDefaults)
        // Здесь не вычисляем, чтобы не дублировать логику.
    }

    private TextureMetaAsset? GetOrLoadTextureMeta(string textureId, string resolvedTexturePath)
    {
        if (_textureMetaById.TryGetValue(textureId, out var cached))
            return cached;

        var metaPath = resolvedTexturePath + ".meta";
        if (!File.Exists(metaPath))
        {
            _textureMetaById[textureId] = null;
            return null;
        }

        try
        {
            var json = File.ReadAllText(metaPath);
            var meta = JsonSerializer.Deserialize<TextureMetaAsset>(json, ResourceJson.Options);
            _textureMetaById[textureId] = meta;
            return meta;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Failed to load texture meta: '{metaPath}'.", ex);
        }
    }

    private void LoadAnimSetInto(string animSetId, SpriteAnimation target)
    {
        var animPath = ResolveAnimSetPath(animSetId);
        if (!File.Exists(animPath))
            throw new FileNotFoundException($"AnimSet not found: '{animSetId}'. Path='{animPath}'.", animPath);

        SpriteAnimSetAsset asset;
        try
        {
            var json = File.ReadAllText(animPath);
            asset = JsonSerializer.Deserialize<SpriteAnimSetAsset>(json, ResourceJson.Options)
                    ?? throw new InvalidOperationException($"AnimSet deserialize returned null: '{animPath}'.");
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Failed to load animset: '{animPath}'.", ex);
        }

        if (string.IsNullOrWhiteSpace(asset.Texture))
            throw new InvalidOperationException($"AnimSet '{animSetId}': 'texture' is required.");

        if (asset.Grid is null || asset.Grid.CellW <= 0 || asset.Grid.CellH <= 0)
            throw new InvalidOperationException($"AnimSet '{animSetId}': 'grid' with positive cellW/cellH is required.");

        if (asset.Clips is null || asset.Clips.Count == 0)
            throw new InvalidOperationException($"AnimSet '{animSetId}': 'clips' is required and must be non-empty.");

        // Разрешаем относительные ссылки на текстуру относительно папки animset.
        var dir = Path.GetDirectoryName(animSetId);
        var texId = NormalizeTextureId(ResolveRelativeId(dir, asset.Texture));

        var tex = GetTexture(texId);
        var defaults = GetSpriteImportDefaults(texId);

        if (asset.Defaults is not null)
        {
            if (asset.Defaults.Ppu is { } ppu)
                defaults = defaults with { PixelsPerUnit = ppu };

            if (asset.Defaults.Pivot is { } pivot)
                defaults = defaults with { Pivot = pivot };

            if (!string.IsNullOrWhiteSpace(asset.Defaults.Filter))
            {
                if (!ResourceJson.TryParseFilterMode(asset.Defaults.Filter, out var fm))
                    throw new InvalidOperationException($"AnimSet '{animSetId}': invalid defaults.filter '{asset.Defaults.Filter}'.");

                defaults = defaults with { FilterMode = fm };
            }
        }

        var grid = new SpriteSheetGrid(asset.Grid.CellW, asset.Grid.CellH, asset.Grid.Margin, asset.Grid.Spacing);
        var sheet = SpriteSheet.GridSheet(tex, grid.CellWidth, grid.CellHeight, grid.Margin, grid.Spacing)
            .WithDefaults(defaults);

        target.Clear();

        foreach (var kvp in asset.Clips)
        {
            var clipName = kvp.Key;
            var c = kvp.Value;

            if (string.IsNullOrWhiteSpace(clipName))
                throw new InvalidOperationException($"AnimSet '{animSetId}': clip name is empty.");

            if (c?.Frames is null || string.IsNullOrWhiteSpace(c.Frames))
                throw new InvalidOperationException($"AnimSet '{animSetId}': clip '{clipName}': 'frames' is required.");

            var fps = c.Fps ?? 12f;
            var loop = c.Loop ?? true;

            var clip = sheet.Clip(clipName, fps, loop, c.Frames);
            target.AddOrReplaceClip(clip);
        }
    }

    private static string ResolveRelativeId(string? baseDir, string id)
    {
        id = NormalizeSeparators(id);
        if (Path.IsPathRooted(id))
            return id;

        if (string.IsNullOrWhiteSpace(baseDir))
            return id;

        return Path.Combine(baseDir, id);
    }


    #endregion
}
