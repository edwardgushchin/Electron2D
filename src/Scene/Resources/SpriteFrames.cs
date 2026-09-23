namespace Electron2D;

/// <summary>A library of named animations containing borrowed textures and relative frame durations.</summary>
/// <remarks>Starts with an empty default animation at five frames per second and linear looping. Frame edits
/// emit Changed; animation-name, speed and loop edits do not. Sources are never disposed by the library.
/// State access is serialized; notifications run after mutation outside the lock. Copies have independent containers.</remarks>
public sealed class SpriteFrames : Resource
{
    /// <summary>Controls how playback behaves at an animation endpoint.</summary>
    public enum LoopMode
    {
        /// <summary>Play once and pause at the endpoint.</summary>
        None = 0,
        /// <summary>Wrap to the opposite endpoint.</summary>
        Linear = 1,
        /// <summary>Reverse direction at each endpoint.</summary>
        PingPong = 2,
    }

    private readonly object _gate = new();
    private readonly Dictionary<string, (double Speed, LoopMode Loop, List<(Texture? Texture, float Duration)> Frames)> _animations = new(StringComparer.Ordinal);
    private readonly List<string> _order = [];

    /// <summary>Creates the empty default animation.</summary>
    public SpriteFrames() { AddAnimation("default"); }

    /// <summary>Adds an empty animation at five frames per second with linear looping, without emitting Changed.</summary>
    /// <param name="animation">The exact case-sensitive name; an empty name is allowed.</param>
    /// <exception cref="ArgumentException">The name already exists.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public void AddAnimation(string animation)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(animation);
            if (!_animations.TryAdd(animation, (5, LoopMode.Linear, []))) throw new ArgumentException("The animation already exists.", nameof(animation));
            _order.Add(animation);
        }
    }

    /// <summary>Tests whether an exact animation name exists.</summary>
    /// <param name="animation">The non-null name.</param>
    /// <returns>Whether the animation exists.</returns>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public bool HasAnimation(string animation) { lock (_gate) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(animation); return _animations.ContainsKey(animation); } }

    /// <summary>Returns a new array of animation names sorted using ordinal order.</summary>
    /// <returns>An independent array, possibly empty.</returns>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public string[] GetAnimationNames() { lock (_gate) { ThrowIfDisposed(); var names = _order.ToArray(); Array.Sort(names, StringComparer.Ordinal); return names; } }

    /// <summary>Copies an animation to a new name, sharing its textures but not its frame collection.</summary>
    /// <param name="from">The existing animation.</param>
    /// <param name="to">The unused destination name.</param>
    /// <remarks>Does not emit Changed.</remarks>
    /// <exception cref="ArgumentException">The source is absent or the destination exists.</exception>
    /// <exception cref="ArgumentNullException">A name is null.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public void DuplicateAnimation(string from, string to)
    {
        lock (_gate)
        {
            var source = Require(from); ArgumentNullException.ThrowIfNull(to);
            if (_animations.ContainsKey(to)) throw new ArgumentException("The destination animation already exists.", nameof(to));
            _animations.Add(to, (source.Speed, source.Loop, new(source.Frames))); _order.Add(to);
        }
    }

    /// <summary>Removes an animation if present, without emitting Changed.</summary>
    /// <param name="animation">The non-null name.</param>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public void RemoveAnimation(string animation) { lock (_gate) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(animation); if (_animations.Remove(animation)) _order.Remove(animation); } }

    /// <summary>Renames an animation, placing it last in insertion order, without emitting Changed.</summary>
    /// <param name="animation">The existing name.</param>
    /// <param name="newName">The unused new name, including an empty name.</param>
    /// <exception cref="ArgumentException">The source is absent or the new name exists, including the original name.</exception>
    /// <exception cref="ArgumentNullException">A name is null.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public void RenameAnimation(string animation, string newName)
    {
        lock (_gate)
        {
            var value = Require(animation); ArgumentNullException.ThrowIfNull(newName);
            if (_animations.ContainsKey(newName)) throw new ArgumentException("The new animation name already exists.", nameof(newName));
            _animations.Remove(animation); _order.Remove(animation); _animations.Add(newName, value); _order.Add(newName);
        }
    }

    /// <summary>Sets an animation's frames per second without emitting Changed.</summary>
    /// <param name="animation">The existing name.</param>
    /// <param name="FPS">A finite nonnegative rate. Zero freezes advancement without pausing playback.</param>
    /// <exception cref="ArgumentOutOfRangeException">The rate is negative or nonfinite.</exception>
    /// <exception cref="ArgumentException">The name does not exist.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public void SetAnimationSpeed(string animation, double FPS)
    {
        lock (_gate)
        {
            var value = Require(animation);
            if (!double.IsFinite(FPS) || FPS < 0) throw new ArgumentOutOfRangeException(nameof(FPS));
            value.Speed = FPS; _animations[animation] = value;
        }
    }

    /// <summary>Returns an animation's frames per second.</summary>
    /// <param name="animation">The existing name.</param>
    /// <returns>The finite nonnegative rate, initially five.</returns>
    /// <exception cref="ArgumentException">The name does not exist.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public double GetAnimationSpeed(string animation) { lock (_gate) return Require(animation).Speed; }

    /// <summary>Sets the endpoint behavior without emitting Changed.</summary>
    /// <param name="animation">The existing name.</param>
    /// <param name="loopMode">A defined loop mode.</param>
    /// <exception cref="ArgumentOutOfRangeException">The loop mode is undefined.</exception>
    /// <exception cref="ArgumentException">The name does not exist.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public void SetAnimationLoopMode(string animation, LoopMode loopMode)
    {
        lock (_gate)
        {
            var value = Require(animation);
            if (loopMode is < LoopMode.None or > LoopMode.PingPong) throw new ArgumentOutOfRangeException(nameof(loopMode));
            value.Loop = loopMode; _animations[animation] = value;
        }
    }

    /// <summary>Returns the animation's endpoint behavior.</summary>
    /// <param name="animation">The existing name.</param>
    /// <returns>The loop mode, initially Linear.</returns>
    /// <exception cref="ArgumentException">The name does not exist.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public LoopMode GetAnimationLoopMode(string animation) { lock (_gate) return Require(animation).Loop; }

    /// <summary>Sets Linear for true or None for false.</summary>
    /// <param name="animation">The existing name.</param>
    /// <param name="loop">Whether to use linear repetition.</param>
    /// <remarks>Uses SetAnimationLoopMode validation and emits no Changed event.</remarks>
    [Obsolete("Use SetAnimationLoopMode instead.")]
    public void SetAnimationLoop(string animation, bool loop) => SetAnimationLoopMode(animation, loop ? LoopMode.Linear : LoopMode.None);

    /// <summary>Tests whether the loop mode is Linear; PingPong returns false.</summary>
    /// <param name="animation">The existing name.</param>
    /// <returns>Whether the animation loops linearly.</returns>
    /// <remarks>Uses GetAnimationLoopMode validation.</remarks>
    [Obsolete("Use GetAnimationLoopMode instead.")]
    public bool GetAnimationLoop(string animation) => GetAnimationLoopMode(animation) == LoopMode.Linear;

    /// <summary>Adds a borrowed frame and emits Changed.</summary>
    /// <param name="animation">The existing name.</param>
    /// <param name="texture">A live borrowed texture, or null for an empty frame.</param>
    /// <param name="duration">Finite relative duration, clamped to at least 0.01.</param>
    /// <param name="atPosition">Insert before an existing index; any negative index or index at/beyond the end appends.</param>
    /// <exception cref="ArgumentException">The animation does not exist.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Duration is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The library or supplied texture is disposed.</exception>
    /// <exception cref="Exception">A change handler throws after commitment.</exception>
    public void AddFrame(string animation, Texture? texture, float duration = 1f, int atPosition = -1)
    {
        lock (_gate)
        {
            var frames = Require(animation).Frames; ValidateFrame(texture, duration);
            frames.Insert(atPosition >= 0 && atPosition < frames.Count ? atPosition : frames.Count, (texture, Mathf.Max(0.01f, duration)));
        }
        EmitChanged();
    }

    /// <summary>Replaces a frame and emits Changed even when its values stay equal.</summary>
    /// <param name="animation">The existing name.</param>
    /// <param name="index">Nonnegative frame index. At/beyond the end is a no-op.</param>
    /// <param name="texture">A live borrowed texture or null.</param>
    /// <param name="duration">Finite relative duration, clamped to at least 0.01.</param>
    /// <remarks>A past-end index returns before validating texture/duration.</remarks>
    /// <exception cref="ArgumentException">The animation does not exist.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is negative or the applied duration is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The library or applied texture is disposed.</exception>
    /// <exception cref="Exception">A change handler throws after commitment.</exception>
    public void SetFrame(string animation, int index, Texture? texture, float duration = 1f)
    {
        lock (_gate)
        {
            var frames = Require(animation).Frames; ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (index >= frames.Count) return;
            ValidateFrame(texture, duration); frames[index] = (texture, Mathf.Max(0.01f, duration));
        }
        EmitChanged();
    }

    /// <summary>Removes an existing frame and emits Changed.</summary>
    /// <param name="animation">The existing name.</param>
    /// <param name="index">An existing frame index.</param>
    /// <exception cref="ArgumentException">The animation does not exist.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the frame list.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    /// <exception cref="Exception">A change handler throws after commitment.</exception>
    public void RemoveFrame(string animation, int index) { lock (_gate) Require(animation).Frames.RemoveAt(index); EmitChanged(); }

    /// <summary>Returns the frame count of an existing animation.</summary>
    /// <param name="animation">The existing name.</param>
    /// <returns>The nonnegative frame count.</returns>
    /// <exception cref="ArgumentException">The animation does not exist.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public int GetFrameCount(string animation) { lock (_gate) return Require(animation).Frames.Count; }

    /// <summary>Returns the borrowed texture for a frame.</summary>
    /// <param name="animation">The existing name.</param>
    /// <param name="index">Nonnegative frame index.</param>
    /// <returns>The borrowed texture, or null for an empty frame or past-end index.</returns>
    /// <exception cref="ArgumentException">The animation does not exist.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is negative.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public Texture? GetFrameTexture(string animation, int index) { lock (_gate) { var frames = Require(animation).Frames; ArgumentOutOfRangeException.ThrowIfNegative(index); return index < frames.Count ? frames[index].Texture : null; } }

    /// <summary>Returns the relative duration of a frame.</summary>
    /// <param name="animation">The existing name.</param>
    /// <param name="index">Nonnegative frame index.</param>
    /// <returns>The stored relative duration, or one for a past-end index. Seconds equal duration divided by FPS and absolute playing speed.</returns>
    /// <exception cref="ArgumentException">The animation does not exist.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is negative.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public float GetFrameDuration(string animation, int index) { lock (_gate) { var frames = Require(animation).Frames; ArgumentOutOfRangeException.ThrowIfNegative(index); return index < frames.Count ? frames[index].Duration : 1; } }

    /// <summary>Removes every frame from an existing animation and emits Changed, even when already empty.</summary>
    /// <param name="animation">The existing name.</param>
    /// <exception cref="ArgumentException">The animation does not exist.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    /// <exception cref="Exception">A change handler throws after commitment.</exception>
    public void Clear(string animation) { lock (_gate) Require(animation).Frames.Clear(); EmitChanged(); }

    /// <summary>Removes every animation and recreates the empty default animation without emitting Changed.</summary>
    /// <exception cref="ObjectDisposedException">The library is disposed.</exception>
    public void ClearAll() { lock (_gate) { ThrowIfDisposed(); _animations.Clear(); _order.Clear(); AddAnimation("default"); } }

    private (double Speed, LoopMode Loop, List<(Texture? Texture, float Duration)> Frames) Require(string animation)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(animation);
        return _animations.TryGetValue(animation, out var result) ? result : throw new ArgumentException("The animation does not exist.", nameof(animation));
    }

    private static void ValidateFrame(Texture? texture, float duration)
    {
        if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(texture));
        if (!float.IsFinite(duration)) throw new ArgumentOutOfRangeException(nameof(duration));
    }

    internal string FirstAnimation { get { lock (_gate) { ThrowIfDisposed(); return _order.Count == 0 ? "" : _order[0]; } } }

    internal bool TryRead(string animation, int index, out int count, out double FPS, out LoopMode loop, out Texture? texture, out float duration)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); count = 0; FPS = 0; loop = LoopMode.None; texture = null; duration = 1;
            if (!_animations.TryGetValue(animation, out var value)) return false;
            count = value.Frames.Count; FPS = value.Speed; loop = value.Loop;
            if ((uint)index < (uint)count) (texture, duration) = value.Frames[index];
            return true;
        }
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new SpriteFrames();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        KeyValuePair<string, (double Speed, LoopMode Loop, List<(Texture? Texture, float Duration)> Frames)>[] snapshot;
        lock (_gate)
        {
            ThrowIfDisposed();
            snapshot = _order.Select(name => new KeyValuePair<string, (double, LoopMode, List<(Texture?, float)>)>(name,
                (_animations[name].Speed, _animations[name].Loop, new(_animations[name].Frames)))).ToArray();
        }
        if (deep)
            foreach (var animation in snapshot)
                for (var i = 0; i < animation.Value.Frames.Count; i++)
                {
                    var frame = animation.Value.Frames[i];
                    animation.Value.Frames[i] = ((Texture?)duplicateSubresource(frame.Texture), frame.Duration);
                }
        var copy = (SpriteFrames)target;
        lock (copy._gate)
        {
            copy.ThrowIfDisposed(); copy._animations.Clear(); copy._order.Clear();
            foreach (var animation in snapshot) { copy._animations.Add(animation.Key, animation.Value); copy._order.Add(animation.Key); }
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) { _animations.Clear(); _order.Clear(); }
        base.Dispose(disposing);
    }
}
