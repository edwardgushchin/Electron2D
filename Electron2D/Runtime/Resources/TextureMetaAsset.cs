using System.Numerics;

namespace Electron2D;

/// <summary>
/// DTO для sidecar-файла метаданных текстуры: "*.png.meta".
/// </summary>
/// <remarks>
/// Формат intentionally простой (JSON):
/// <code>
/// {
///   "texture": { "filter": "pixelart" },
///   "sprite":  { "ppu": 100, "pivot": [0.5, 0.0], "filter": "inherit" }
/// }
/// </code>
/// </remarks>
internal sealed class TextureMetaAsset
{
    public TextureMetaTexture? Texture { get; set; }
    public TextureMetaSprite? Sprite { get; set; }
}

internal sealed class TextureMetaTexture
{
    public string? Filter { get; set; }
}

internal sealed class TextureMetaSprite
{
    public float? Ppu { get; set; }
    public Vector2? Pivot { get; set; }
    public string? Filter { get; set; }
}
