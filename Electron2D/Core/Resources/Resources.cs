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
    #endregion

    private static ResourceSystem System =>
        _system ?? throw new InvalidOperationException(
            "Resources are not available (Engine is not running or already disposed).");
}