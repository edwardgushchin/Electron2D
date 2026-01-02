using System.Numerics;

namespace Electron2D;

/// <summary>
/// Снимок параметров спрайта, используемый для кэширования/сравнения без привязки к жизненному циклу исходного объекта.
/// </summary>
internal readonly struct SpriteSnapshot : IEquatable<SpriteSnapshot>
{
    #region Instance fields

    /// <summary>Режим отражения/поворота.</summary>
    public readonly FlipMode Flip;

    /// <summary>Прямоугольник источника в текстуре (в texel-координатах).</summary>
    public readonly Rect TextureRect;

    /// <summary>Плотность пикселей на мировую единицу (PPU).</summary>
    public readonly float PixelsPerUnit;

    /// <summary>Пивот спрайта.</summary>
    public readonly Vector2 Pivot;

    /// <summary>
    /// Идентификатор текстуры для late-load/подмены ресурсов.
    /// Если в проекте TextureId не строка — заменить тип на фактический.
    /// </summary>
    public readonly string? TextureId;

    /// <summary>Нативный handle текстуры; 0 если текстура невалидна.</summary>
    public readonly nint TextureHandle;

    #endregion

    #region Constructors

    private SpriteSnapshot(
        FlipMode flip,
        Rect textureRect,
        float pixelsPerUnit,
        Vector2 pivot,
        string? textureId,
        nint textureHandle)
    {
        Flip = flip;
        TextureRect = textureRect;
        PixelsPerUnit = pixelsPerUnit;
        Pivot = pivot;
        TextureId = textureId;
        TextureHandle = textureHandle;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Создаёт снимок из текущего состояния <paramref name="sprite"/>.
    /// </summary>
    /// <remarks>
    /// Логика привязки:
    /// - Если текстура уже валидна — привязываемся к <see cref="TextureHandle"/>, чтобы кэш обновлялся при смене объекта текстуры.
    /// - Если текстура невалидна — привязываемся к <see cref="TextureId"/>, чтобы кэш пересобрался при появлении/подмене ресурса.
    /// </remarks>
    public static SpriteSnapshot From(Sprite sprite)
    {
        var texture = sprite.Texture;
        nint handle = texture.IsValid ? texture.Handle : 0;

        return new SpriteSnapshot(
            flip: sprite.FlipMode,
            textureRect: sprite.TextureRect,
            pixelsPerUnit: sprite.PixelsPerUnit,
            pivot: sprite.Pivot,
            textureId: sprite.TextureId,
            textureHandle: handle);
    }

    #endregion

    #region Equality

    /// <summary>
    /// Сравнение снимков на эквивалентность (используется для проверки валидности кэша).
    /// </summary>
    public bool Equals(SpriteSnapshot other)
    {
        // Rect/Vector2 — структуры: предполагается корректная реализация Equals().
        // Если она не гарантирована в вашем проекте — сравнить явно по полям.
        return Flip == other.Flip
               && TextureRect.Equals(other.TextureRect)
               && PixelsPerUnit.Equals(other.PixelsPerUnit)
               && Pivot.Equals(other.Pivot)
               && string.Equals(TextureId, other.TextureId, StringComparison.Ordinal)
               && TextureHandle == other.TextureHandle;
    }

    public override bool Equals(object? obj) => obj is SpriteSnapshot other && Equals(other);

    public override int GetHashCode()
    {
        // Важно: хэш должен соответствовать Equals (Ordinal для TextureId).
        int textureIdHash = TextureId is null ? 0 : StringComparer.Ordinal.GetHashCode(TextureId);

        return HashCode.Combine(
            Flip,
            TextureRect,
            PixelsPerUnit,
            Pivot,
            textureIdHash,
            TextureHandle);
    }

    public static bool operator ==(SpriteSnapshot left, SpriteSnapshot right) => left.Equals(right);

    public static bool operator !=(SpriteSnapshot left, SpriteSnapshot right) => !left.Equals(right);

    #endregion
}
