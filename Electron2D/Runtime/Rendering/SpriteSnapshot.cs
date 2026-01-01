using System.Numerics;

namespace Electron2D;

internal readonly struct SpriteSnapshot
{
    public readonly FlipMode Flip;
    public readonly Rect TextureRect;
    public readonly float PixelsPerUnit;
    public readonly Vector2 Pivot;

    // Для late-load/подмены текстур: учитываем «источник»
    public readonly string? TextureId; // если у тебя TextureId не object — поменяй тип на фактический
    public readonly nint TextureHandle; // 0 если невалидна

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

    public static SpriteSnapshot From(Sprite s)
    {
        // Если texture уже валидна — привязываемся к handle, чтобы кэш обновился при смене texture объекта.
        // Если не валидна — привязываемся к TextureId, чтобы кэш пересобрался когда ресурс появится/сменится.
        var tex = s.Texture;
        var handle = tex.IsValid ? tex.Handle : 0;

        return new SpriteSnapshot(
            flip: s.FlipMode,
            textureRect: s.TextureRect,
            pixelsPerUnit: s.PixelsPerUnit,
            pivot: s.Pivot,
            textureId: s.TextureId,
            textureHandle: handle
        );
    }

    public bool Equals(SpriteSnapshot other)
    {
        // Rect/Vector2 — структуры, сравнение по полям; если у них нет корректного Equals — сравни явно.
        return Flip == other.Flip
               && TextureRect.Equals(other.TextureRect)
               && PixelsPerUnit.Equals(other.PixelsPerUnit)
               && Pivot.Equals(other.Pivot)
               && string.Equals(TextureId, other.TextureId, StringComparison.Ordinal)
               && TextureHandle == other.TextureHandle;
    }
}