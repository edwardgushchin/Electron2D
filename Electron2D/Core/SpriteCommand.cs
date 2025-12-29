using System.Numerics;

namespace Electron2D;

public class SpriteCommand
{
    public Texture Texture;
    public Vector2 SrcPx;
    public Vector2 PosWorld;
    public Vector2 SizeWorld;
    public float Rotation;
    public uint Color;
    public ulong SortKey;
}