using System.Numerics;

namespace Electron2D;

public class TextureAtlas
{
    private readonly Dictionary<string, TextureRegion> _regions = new(StringComparer.Ordinal);

    public Texture Texture { get; set; }

    public TextureAtlas() { }

    public TextureAtlas(Texture texture)
    {
        Texture = texture;
    }

    public bool TryGetRegion(string name, out TextureRegion region)
        => _regions.TryGetValue(name, out region!);

    public TextureRegion GetRegion(string name)
        => _regions[name];

    public void AddRegion(string name, int x, int y, int width, int height)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Region name must be non-empty.", nameof(name));

        _regions.Add(name, new TextureRegion(x, y, width, height));
    }


    public bool RemoveRegion(string name) => _regions.Remove(name);

    public void Clear() => _regions.Clear();

    /// <summary>
    /// Создаёт Sprite по региону атласа.
    /// </summary>
    public Sprite CreateSprite(
        string regionName,
        float pixelsPerUnit = 100f,
        Vector2? pivot = null,
        FilterMode filterMode = FilterMode.Inherit,
        FlipMode flipMode = FlipMode.None)
    {
        if (Texture is null)
            throw new InvalidOperationException("TextureAtlas.Texture is not set.");

        if (!Texture.IsValid)
            throw new InvalidOperationException("TextureAtlas.Texture is not valid.");

        if (!TryGetRegion(regionName, out var region))
            throw new KeyNotFoundException($"Atlas region not found: {regionName}");

        var r = region.SourceRectangle;

        return new Sprite(
            texture: Texture,
            pixelsPerUnit: pixelsPerUnit,
            pivot: pivot,
            rect: r,
            textureRect: r,
            filterMode: filterMode,
            flipMode: flipMode);
    }

    /// <summary>
    /// Создаёт клип из списка регионов атласа (фиксированная длительность кадра).
    /// </summary>
    public SpriteAnimationClip CreateClip(
        string clipName,
        string[] regionNames,
        float fps,
        bool loop = true,
        float pixelsPerUnit = 100f,
        Vector2? pivot = null,
        FlipMode flipMode = FlipMode.None)
    {
        if (string.IsNullOrWhiteSpace(clipName))
            throw new ArgumentException("clipName must be non-empty.", nameof(clipName));

        if (regionNames is null || regionNames.Length == 0)
            throw new ArgumentException("regionNames must be non-empty.", nameof(regionNames));

        if (!(fps > 0f) || float.IsNaN(fps) || float.IsInfinity(fps))
            throw new ArgumentOutOfRangeException(nameof(fps), fps, "Fps must be finite and > 0.");

        var sprites = new Sprite[regionNames.Length];
        for (var i = 0; i < regionNames.Length; i++)
            sprites[i] = CreateSprite(regionNames[i], pixelsPerUnit, pivot, flipMode: flipMode);

        return new SpriteAnimationClip(clipName, sprites, fps, loop);
    }


    /// <summary>
    /// Создаёт клип по числовой последовательности: prefix + index (например run_0..run_5).
    /// </summary>
    public SpriteAnimationClip CreateClip(
        string clipName,
        string prefix,
        int firstIndex,
        int count,
        float fps,
        bool loop = true,
        float pixelsPerUnit = 100f,
        Vector2? pivot = null,
        FlipMode flipMode = FlipMode.None)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be > 0.");

        var names = new string[count];
        for (var i = 0; i < count; i++)
            names[i] = prefix + (firstIndex + i);

        return CreateClip(clipName, names, fps, loop, pixelsPerUnit, pivot, flipMode);
    }

}
