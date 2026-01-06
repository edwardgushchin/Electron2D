using System.Numerics;

namespace Electron2D;

/// <summary>
/// DTO для ассета "*.animset" (JSON) — декларативное описание клипов спрайт-анимации.
/// </summary>
/// <remarks>
/// Пример:
/// <code>
/// {
///   "texture": "character/char_blue.png",
///   "grid": { "cellW": 56, "cellH": 56, "margin": 0, "spacing": 0 },
///   "defaults": { "ppu": 100, "pivot": [0.5, 0.0], "filter": "inherit" },
///   "clips": {
///     "idle":   { "fps": 8,  "loop": true,  "frames": "0:0-5" },
///     "run":    { "fps": 12, "loop": true,  "frames": "2:0-7" },
///     "death":  { "fps": 12, "loop": false, "frames": "5:2-7;6:0-3" }
///   }
/// }
/// </code>
/// </remarks>
internal sealed class SpriteAnimSetAsset
{
    public string? Texture { get; set; }
    public SpriteAnimGridAsset? Grid { get; set; }
    public SpriteAnimDefaultsAsset? Defaults { get; set; }

    // Имя клипа — ключ словаря.
    public Dictionary<string, SpriteAnimClipAsset>? Clips { get; set; }
}

internal sealed class SpriteAnimGridAsset
{
    public int CellW { get; set; }
    public int CellH { get; set; }
    public int Margin { get; set; }
    public int Spacing { get; set; }
}

internal sealed class SpriteAnimDefaultsAsset
{
    public float? Ppu { get; set; }
    public Vector2? Pivot { get; set; }
    public string? Filter { get; set; }
}

internal sealed class SpriteAnimClipAsset
{
    public float? Fps { get; set; }
    public bool? Loop { get; set; }
    public string? Frames { get; set; }
}