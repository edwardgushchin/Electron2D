namespace Electron2D;

internal sealed class RenderQueue(int capacity)
{
    private readonly SpriteCommand[] _sprites = new SpriteCommand[capacity];
    private int _count;

    public void Clear() => _count = 0;

    public bool TryPush(in SpriteCommand cmd)
    {
        if ((uint)_count >= (uint)_sprites.Length) return false;
        _sprites[_count++] = cmd;
        return true;
    }

    public ReadOnlySpan<SpriteCommand> Sprites => _sprites.AsSpan(0, _count);
}