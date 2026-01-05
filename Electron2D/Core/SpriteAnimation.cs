using System.Diagnostics;

namespace Electron2D;

/// <summary>
/// Контейнер клипов (аналог Godot SpriteFrames): имя -> clip.
/// Все аллокации происходят при конфигурации, в кадре — только чтение.
/// </summary>
public sealed class SpriteAnimation
{
    private readonly Dictionary<string, int> _clipIds = new(StringComparer.Ordinal);
    private SpriteAnimationClip[] _clips = [];
    private int _count;

    public int ClipCount => _count;

    public bool TryGetClip(string name, out SpriteAnimationClip clip)
    {
        if (_clipIds.TryGetValue(name, out var id))
        {
            clip = _clips[id];
            return true;
        }

        clip = null!;
        return false;
    }

    public SpriteAnimationClip GetClip(string name)
        => TryGetClip(name, out var clip) ? clip : throw new KeyNotFoundException($"Clip not found: {name}");

    public int GetClipId(string name)
        => _clipIds.TryGetValue(name, out var id) ? id : throw new KeyNotFoundException($"Clip not found: {name}");

    public SpriteAnimationClip GetClip(int id)
    {
        if ((uint)id >= (uint)_count)
            throw new ArgumentOutOfRangeException(nameof(id), id, "Invalid clip id.");

        return _clips[id];
    }

    public void AddClip(SpriteAnimationClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);

        ValidateClip(clip);

        if (_clipIds.ContainsKey(clip.Name))
            throw new InvalidOperationException($"Clip already exists: {clip.Name}");

        EnsureCapacity(_count + 1);
        _clips[_count] = clip;
        _clipIds.Add(clip.Name, _count);
        _count++;
    }

    public void AddOrReplaceClip(SpriteAnimationClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);

        ValidateClip(clip);

        if (_clipIds.TryGetValue(clip.Name, out var id))
        {
            _clips[id] = clip;
            return;
        }

        EnsureCapacity(_count + 1);
        _clips[_count] = clip;
        _clipIds.Add(clip.Name, _count);
        _count++;
    }

    private void EnsureCapacity(int desired)
    {
        if (_clips.Length >= desired)
            return;

        var newCap = _clips.Length == 0 ? 8 : _clips.Length * 2;
        if (newCap < desired) newCap = desired;

        Array.Resize(ref _clips, newCap);
    }

    private static void ValidateClip(SpriteAnimationClip clip)
    {
        if (string.IsNullOrWhiteSpace(clip.Name))
            throw new ArgumentException("Clip.Name must be non-empty.", nameof(clip));

        var frames = clip.Frames;
        if (frames is null || frames.Length == 0)
            throw new ArgumentException("Clip.Frames must be non-empty.", nameof(clip));

        for (var i = 0; i < frames.Length; i++)
        {
            var f = frames[i];
            if (f is null)
                throw new ArgumentException($"Clip frame {i}: Sprite is null.", nameof(clip));
        }
    }
}
