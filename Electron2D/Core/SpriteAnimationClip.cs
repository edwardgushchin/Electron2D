namespace Electron2D;

public sealed class SpriteAnimationClip
{
    public string Name { get; }
    public SpriteAnimationFrame[] Frames { get; }
    public bool Loop { get; }

    public SpriteAnimationClip(string name, SpriteAnimationFrame[] frames, bool loop = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Clip name must be non-empty.", nameof(name));

        ArgumentNullException.ThrowIfNull(frames);

        if (frames.Length == 0)
            throw new ArgumentException("Clip frames must be non-empty.", nameof(frames));

        for (var i = 0; i < frames.Length; i++)
        {
            if (frames[i].Sprite is null)
                throw new ArgumentException($"Frame {i}: Sprite is null.", nameof(frames));

            var d = frames[i].DurationSeconds;
            if (!(d > 0f) || float.IsNaN(d) || float.IsInfinity(d))
                throw new ArgumentException($"Frame {i}: DurationSeconds must be finite and > 0.", nameof(frames));
        }

        Name = name;
        Frames = frames;
        Loop = loop;
    }
}

public readonly record struct SpriteAnimationFrame(Sprite Sprite, float DurationSeconds);