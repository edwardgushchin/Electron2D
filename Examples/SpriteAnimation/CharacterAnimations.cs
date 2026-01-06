using Electron2D;

namespace SpriteAnimation;

internal static class CharacterAnimations
{
    private static AnimationSet? _cached;

    internal sealed class AnimationSet
    {
        public required AnimationClip Idle { get; init; }
        public required AnimationClip Run { get; init; }
        public required AnimationClip Jump { get; init; }
        public required AnimationClip Fall { get; init; }
        public required AnimationClip Attack { get; init; }
        public required AnimationClip Death { get; init; }
    }

    public static AnimationSet GetOrCreate()
    {
        if (_cached is not null)
            return _cached;

        // Декларативная анимация из Content/character/char_blue.animset.
        var anim = Resources.GetSpriteAnimation(Path.Combine("character", "char_blue.animset"));

        _cached = new AnimationSet
        {
            Idle = anim.GetClip("idle"),
            Run = anim.GetClip("run"),
            Jump = anim.GetClip("jump"),
            Fall = anim.GetClip("fall"),
            Attack = anim.GetClip("attack"),
            Death = anim.GetClip("death")
        };

        return _cached;
    }
}
