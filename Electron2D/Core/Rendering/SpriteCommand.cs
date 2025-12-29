using System.Numerics;

namespace Electron2D;

public readonly struct SpriteCommand(
    Texture tex,
    Vector2 srcPx,
    Vector2 pos,
    Vector2 size,
    float rot,
    uint color,
    ulong sortKey)
{
    public readonly Texture Texture = tex; // ссылочный тип допускается, но лучше TextureId (int)
    public readonly Vector2 SrcPx = srcPx;
    public readonly Vector2 PosWorld = pos;
    public readonly Vector2 SizeWorld = size;
    public readonly float Rotation = rot;
    public readonly uint Color = color;
    public readonly ulong SortKey = sortKey;
}
