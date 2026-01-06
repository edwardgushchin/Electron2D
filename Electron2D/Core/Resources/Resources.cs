using System;

namespace Electron2D;

/// <summary>
/// Глобальный фасад доступа к ресурсам (как Input/Profiler).
/// Доступен только пока <see cref="Engine"/> привязал <see cref="ResourceSystem"/>.
/// </summary>
public static class Resources
{
    private static ResourceSystem? _system;

    #region Internal API
    internal static void Bind(ResourceSystem system) => _system = system;

    internal static void Unbind() => _system = null;
    #endregion

    #region Public API
    /// <summary>
    /// Возвращает текстуру по идентификатору. Бросает исключение, если ресурсы не доступны
    /// или текстура не найдена/не загружена (в зависимости от поведения <see cref="ResourceSystem"/>).
    /// </summary>
    public static Texture GetTexture(string path) => System.GetTexture(path);

    /// <summary>
    /// Пытается получить текстуру по идентификатору.
    /// </summary>
    public static bool TryGetTexture(string path, out Texture texture) => System.TryGetTexture(path, out texture);

    /// <summary>
    /// Выгружает текстуру по идентификатору (если поддерживается системой ресурсов).
    /// </summary>
    public static void UnloadTexture(string path) => System.UnloadTexture(path);

    /// <summary>
    /// Возвращает дефолтные импорт-настройки спрайта, привязанные к текстуре (берутся из *.png.meta).
    /// </summary>
    public static SpriteImportDefaults GetSpriteImportDefaults(string texturePath)
        => System.GetSpriteImportDefaults(texturePath);

    /// <summary>
    /// Возвращает набор клипов спрайт-анимации, описанный в *.animset (JSON).
    /// </summary>
    public static Animation GetSpriteAnimation(string path) => System.GetSpriteAnimation(path);

    /// <summary>
    /// Перезагружает *.animset и обновляет уже загруженный объект (если он был в кеше).
    /// </summary>
    public static Animation ReloadSpriteAnimation(string path) => System.ReloadSpriteAnimation(path);

    public static bool TryGetSpriteAnimation(string path, out Animation anim)
        => System.TryGetSpriteAnimation(path, out anim);

    public static void UnloadSpriteAnimation(string path) => System.UnloadSpriteAnimation(path);
    #endregion

    private static ResourceSystem System =>
        _system ?? throw new InvalidOperationException(
            "Resources are not available (Engine is not running or already disposed).");
}