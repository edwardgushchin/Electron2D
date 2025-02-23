namespace Electron2D;

internal static class SpriteRenderer
{
    private static readonly List<Sprite> Sprites = [];
    private static bool _needsSorting;

    public static void RegisterSprite(Sprite sprite)
    {
        Sprites.Add(sprite);
        _needsSorting = true;
    }

    public static void UnregisterSprite(Sprite sprite)
    {
        Sprites.Remove(sprite);
    }

    public static void MarkForSorting()
    {
        _needsSorting = true;
    }

    public static void Render(IRenderContext context)
    {
        if (_needsSorting)
        {
            Sprites.Sort((a, b) => a.Position.Z.CompareTo(b.Position.Z));
            _needsSorting = false;
        }

        foreach (var sprite in Sprites)
        {
            sprite.Draw(context);
        }
    }
}