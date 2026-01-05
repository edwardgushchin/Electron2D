using System.Numerics;
using Electron2D;

namespace SpriteAnimation;

internal static class CharacterAnimations
{
    private const int CellW = 56;
    private const int CellH = 56;

    private const float Ppu = 100f;
    // В Electron2D pivot.Y = 0 означает "низ спрайта" (world Y-up).
    // Поэтому чтобы WorldPosition.Y воспринимать как "пол" — используем (0.5, 0.0).
    private static readonly Vector2 Pivot = new(0.5f, 0.0f);

    private static AnimationSet? _cached;

    internal sealed class AnimationSet
    {
        public required SpriteAnimationClip Idle { get; init; }
        public required SpriteAnimationClip Run { get; init; }
        public required SpriteAnimationClip Jump { get; init; }
        public required SpriteAnimationClip Fall { get; init; }
        public required SpriteAnimationClip Attack { get; init; }
        public required SpriteAnimationClip Death { get; init; }
    }

    public static AnimationSet GetOrCreate()
    {
        if (_cached is not null)
            return _cached;

        var tex = Resources.GetTexture(Path.Combine("character", "char_blue.png"));

        Sprite Cell(int row, int col)
        {
            var r = new Rect(col * CellW, row * CellH, CellW, CellH);
            return new Sprite(
                texture: tex,
                pixelsPerUnit: Ppu,
                pivot: Pivot,
                rect: r,
                textureRect: r,
                filterMode: FilterMode.Pixelart);
        }

        SpriteAnimationClip Loop(string name, float fps, params Sprite[] sprites) =>
            new(name, sprites, fps: fps, loop: true);

        SpriteAnimationClip OneShot(string name, float fps, params Sprite[] sprites) =>
            new(name, sprites, fps: fps, loop: false);

        var idle = Loop("idle", 8,
            Cell(0,0), Cell(0,1), Cell(0,2), Cell(0,3), Cell(0,4), Cell(0,5));

        var run = Loop("run", fps: 12,
            Cell(2,0), Cell(2,1), Cell(2,2), Cell(2,3), Cell(2,4), Cell(2,5), Cell(2,6), Cell(2,7));

        var attack = OneShot("attack", fps: 16,
            Cell(1,0), Cell(1,1), Cell(1,2), Cell(1,3), Cell(1,4), Cell(1,5));

        var death = OneShot("death", fps: 12,
            Cell(5,2), Cell(5,3), Cell(5,4), Cell(5,5), Cell(5,6), Cell(5,7),
            Cell(6,0), Cell(6,1), Cell(6,2), Cell(6,3));

        var jump = Loop("jump", fps: 1f, Cell(3,6));
        var fall = Loop("fall", fps: 1f, Cell(3,7));

        _cached = new AnimationSet
        {
            Idle = idle,
            Run = run,
            Jump = jump,
            Fall = fall,
            Attack = attack,
            Death = death
        };

        return _cached;
    }
}
