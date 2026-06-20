namespace Electron2D;

public sealed class AnimationClip
{
    public string Name { get; }
    public Sprite[] Frames { get; }
    public float Fps { get; }
    public bool Loop { get; }

    public float FrameDurationSeconds => 1f / Fps;

    public AnimationClip(string name, Sprite[] frames, float fps = 12f, bool loop = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Clip name must be non-empty.", nameof(name));

        ArgumentNullException.ThrowIfNull(frames);

        if (frames.Length == 0)
            throw new ArgumentException("Clip frames must be non-empty.", nameof(frames));

        if (!(fps > 0f) || float.IsNaN(fps) || float.IsInfinity(fps))
            throw new ArgumentOutOfRangeException(nameof(fps), fps, "FPS must be finite and > 0.");

        for (var i = 0; i < frames.Length; i++)
        {
            if (frames[i] is null)
                throw new ArgumentException($"Frame {i}: Sprite is null.", nameof(frames));
        }

        Name = name;
        Frames = frames;
        Fps = fps;
        Loop = loop;
    }
}