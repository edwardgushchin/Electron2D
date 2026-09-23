using System.Diagnostics;

namespace Electron2D;

/// <summary>Plays a sequence of borrowed two-dimensional textures anywhere a Texture can be drawn.</summary>
/// <remarks>Playback advances on rendered frames using monotonic, unscaled time. Frame textures remain caller-owned.
/// With no active frame texture the logical size is one pixel; an empty current frame has no readable image.
/// Larger frames crop to the smallest active dimensions. AtlasTexture frames are unsupported.</remarks>
public sealed class AnimatedTexture : Texture
{
    /// <summary>The number of available frame slots.</summary>
    public const int MaxFrames = 256;

    // ponytail: One graph lock serializes dependency checks; split it only if resource contention is measured.
    private static readonly object GraphGate = new();
    private static readonly List<WeakReference<AnimatedTexture>> Instances = [];
    private readonly Frame[] _frames = new Frame[MaxFrames];
    private int _frameCount = 1, _currentFrame;
    private bool _pause, _oneShot;
    private float _speedScale = 1;
    private double _elapsed;
    private long _lastTicks;
    private RenderingServer? _lastServer;
    private int _minWidth = 1, _minHeight = 1;
    private bool _sizeDirty = true;

    private struct Frame
    {
        internal Texture? Texture;
        internal float Duration;
    }

    /// <summary>Creates one frame with a one-second duration.</summary>
    public AnimatedTexture()
    {
        for (var i = 0; i < MaxFrames; i++) _frames[i].Duration = 1;
        lock (GraphGate) Instances.Add(new(this));
    }

    /// <summary>Gets or sets how many of the 256 frame slots participate in playback.</summary>
    /// <value>One by default; the allowed range is 1 through MaxFrames.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is outside the allowed range.</exception>
    public int Frames
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _frameCount; } }
        set
        {
            if (value is < 1 or > MaxFrames) throw new ArgumentOutOfRangeException(nameof(value));
            lock (GraphGate)
            {
                ThrowIfDisposed();
                if (_frameCount == value) return;
                _frameCount = value;
                _sizeDirty = true;
                if (_currentFrame >= value) { _currentFrame = value - 1; _elapsed = 0; }
            }
            EmitChanged();
        }
    }

    /// <summary>Gets or selects the visible frame, restarting its full duration on every assignment.</summary>
    /// <value>Zero by default; less than Frames.</value>
    /// <exception cref="ArgumentOutOfRangeException">The selected frame is not active.</exception>
    public int CurrentFrame
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _currentFrame; } }
        set
        {
            lock (GraphGate)
            {
                ThrowIfDisposed();
                if ((uint)value >= (uint)_frameCount) throw new ArgumentOutOfRangeException(nameof(value));
                _currentFrame = value; _elapsed = 0;
            }
            EmitChanged();
        }
    }

    /// <summary>Gets or sets whether playback stops at its first or last frame instead of wrapping.</summary>
    public bool OneShot
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _oneShot; } }
        set { lock (GraphGate) { ThrowIfDisposed(); _oneShot = value; } EmitChanged(); }
    }

    /// <summary>Gets or sets whether playback holds its current frame and remaining duration.</summary>
    public bool Pause
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _pause; } }
        set { lock (GraphGate) { ThrowIfDisposed(); _pause = value; } EmitChanged(); }
    }

    /// <summary>Gets or sets playback speed; zero stops advancement and negative values play backward.</summary>
    /// <value>One by default; finite values from -1000 inclusive to 1000 exclusive are accepted.</value>
    /// <exception cref="ArgumentOutOfRangeException">The speed is nonfinite or outside the supported range.</exception>
    public float SpeedScale
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _speedScale; } }
        set
        {
            if (!float.IsFinite(value) || value < -1000 || value >= 1000) throw new ArgumentOutOfRangeException(nameof(value));
            lock (GraphGate) { ThrowIfDisposed(); _speedScale = value; }
            EmitChanged();
        }
    }

    /// <summary>Returns a borrowed texture from any frame slot, including an inactive one.</summary>
    /// <param name="frame">A slot from zero through MaxFrames minus one.</param>
    /// <returns>The assigned texture, or null.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The slot is outside the supported range.</exception>
    public Texture? GetFrameTexture(int frame)
    {
        lock (GraphGate) { ThrowIfDisposed(); CheckSlot(frame); return _frames[frame].Texture; }
    }

    /// <summary>Assigns a borrowed texture to any frame slot.</summary>
    /// <param name="frame">A slot from zero through MaxFrames minus one.</param>
    /// <param name="texture">A live texture or null. Atlas views and dependency cycles are rejected.</param>
    /// <exception cref="ArgumentOutOfRangeException">The slot is outside the supported range.</exception>
    /// <exception cref="ObjectDisposedException">The texture has been disposed.</exception>
    /// <exception cref="ArgumentException">The texture is an atlas view or would create a cycle.</exception>
    public void SetFrameTexture(int frame, Texture? texture)
    {
        lock (GraphGate)
        {
            ThrowIfDisposed(); CheckSlot(frame);
            if (texture?.IsDisposed == true) throw new ObjectDisposedException(nameof(texture));
            if (texture is AtlasTexture || Reaches(texture, this, new HashSet<AnimatedTexture>()))
                throw new ArgumentException("Animated texture frames cannot use atlas views or create cycles.", nameof(texture));
            var previous = _frames[frame].Texture;
            if (ReferenceEquals(previous, texture)) return;
            _frames[frame].Texture = texture;
            _sizeDirty = true;
            if (previous is not null && !Uses(previous)) previous.Changed -= SourceChanged;
            if (texture is not null && !UsesElsewhere(texture, frame)) texture.Changed += SourceChanged;
        }
        EmitChanged();
    }

    /// <summary>Returns the duration in seconds of any frame slot.</summary>
    /// <param name="frame">A slot from zero through MaxFrames minus one.</param>
    /// <returns>One second by default.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The slot is outside the supported range.</exception>
    public float GetFrameDuration(int frame)
    {
        lock (GraphGate) { ThrowIfDisposed(); CheckSlot(frame); return _frames[frame].Duration; }
    }

    /// <summary>Sets a slot's duration; zero skips that frame during playback.</summary>
    /// <param name="frame">A slot from zero through MaxFrames minus one.</param>
    /// <param name="duration">A finite nonnegative number of seconds.</param>
    /// <exception cref="ArgumentOutOfRangeException">The slot or duration is invalid.</exception>
    public void SetFrameDuration(int frame, float duration)
    {
        if (!float.IsFinite(duration) || duration < 0) throw new ArgumentOutOfRangeException(nameof(duration));
        lock (GraphGate) { ThrowIfDisposed(); CheckSlot(frame); _frames[frame].Duration = duration; }
        EmitChanged();
    }

    /// <inheritdoc />
    public override int GetWidth() { lock (GraphGate) { ThrowIfDisposed(); UpdateMinimumSize(); return _minWidth; } }
    /// <inheritdoc />
    public override int GetHeight() { lock (GraphGate) { ThrowIfDisposed(); UpdateMinimumSize(); return _minHeight; } }
    /// <inheritdoc />
    public override bool HasAlpha => CapturePixels() is { } pixels && Image.TextureHasAlpha(pixels.Source.Format);
    /// <inheritdoc />
    public override Image.Format PixelFormat => CapturePixels()?.Source.Format ?? Image.Format.L8;
    /// <inheritdoc />
    public override bool HasMipmaps => CapturePixels()?.Source.HasMipmaps ?? false;
    /// <inheritdoc />
    public override int MipmapCount => (CapturePixels()?.Levels ?? 1) - 1;
    /// <inheritdoc />
    public override Image? GetImage()
    {
        lock (GraphGate)
        {
            ThrowIfDisposed();
            UpdateMinimumSize();
            var image = CurrentTexture?.GetImage();
            try
            {
                if (image is not null && (image.Width != _minWidth || image.Height != _minHeight))
                    image.Crop(_minWidth, _minHeight);
                return image;
            }
            catch { image?.Dispose(); throw; }
        }
    }
    /// <inheritdoc />
    public override bool IsPixelOpaque(int x, int y)
    {
        lock (GraphGate) { ThrowIfDisposed(); UpdateMinimumSize(); return SampleOpacity(CapturePixels(), new(_minWidth, _minHeight), x, y); }
    }
    internal override TexturePixels? CapturePixels()
    {
        lock (GraphGate)
        {
            ThrowIfDisposed();
            UpdateMinimumSize();
            var pixels = CurrentTexture?.CapturePixels();
            return pixels is null || pixels.Source.Width == _minWidth && pixels.Source.Height == _minHeight
                ? pixels : base.CapturePixels();
        }
    }

    private Texture? CurrentTexture => _frames[_currentFrame].Texture;

    private void UpdateMinimumSize()
    {
        if (!_sizeDirty) return;
        var width = int.MaxValue; var height = int.MaxValue;
        for (var i = 0; i < _frameCount; i++)
            if (_frames[i].Texture is { } texture)
            {
                width = Math.Min(width, texture.GetWidth());
                height = Math.Min(height, texture.GetHeight());
            }
        _minWidth = width == int.MaxValue ? 1 : width;
        _minHeight = height == int.MaxValue ? 1 : height;
        _sizeDirty = false;
    }

    internal static void AdvanceAll(RenderingServer server, long ticks, List<AnimatedTexture> changed)
    {
        lock (GraphGate)
        {
            for (var i = Instances.Count - 1; i >= 0; i--)
            {
                if (!Instances[i].TryGetTarget(out var texture) || texture.IsDisposed) { Instances.RemoveAt(i); continue; }
                if (texture.Advance(server, ticks)) changed.Add(texture);
            }
        }
        try { foreach (var texture in changed) if (!texture.IsDisposed) texture.EmitChanged(); }
        finally { changed.Clear(); }
    }

    private bool Advance(RenderingServer server, long ticks)
    {
        if (!ReferenceEquals(_lastServer, server)) { _lastServer = server; _lastTicks = ticks; return false; }
        var seconds = Stopwatch.GetElapsedTime(_lastTicks, ticks).TotalSeconds;
        _lastTicks = ticks;
        return AdvanceTime(seconds);
    }

    internal bool AdvanceTime(double seconds)
    {
        lock (GraphGate)
        {
            ThrowIfDisposed();
            if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (_pause || _speedScale == 0 || _frameCount == 1 || seconds <= 0) return false;
            _elapsed += seconds;
            var speed = Math.Abs((double)_speedScale);
            if (!_oneShot)
            {
                double cycle = 0;
                for (var i = 0; i < _frameCount; i++) cycle += _frames[i].Duration;
                if (cycle <= 0) { _elapsed = 0; return false; }
                cycle /= speed;
                if (_elapsed >= cycle) _elapsed %= cycle;
            }
            var changed = false;
            for (var i = 0; i < _frameCount && _elapsed >= _frames[_currentFrame].Duration / speed; i++)
            {
                var duration = _frames[_currentFrame].Duration / speed;
                var next = _currentFrame + (_speedScale > 0 ? 1 : -1);
                if (next >= _frameCount || next < 0)
                {
                    if (_oneShot) { _elapsed = 0; break; }
                    next = next < 0 ? _frameCount - 1 : 0;
                }
                _elapsed -= duration;
                _currentFrame = next; changed = true;
            }
            return changed;
        }
    }

    private static bool Reaches(Texture? source, AnimatedTexture target, HashSet<AnimatedTexture> seen)
    {
        if (source is not AnimatedTexture animation) return false;
        if (ReferenceEquals(animation, target)) return true;
        if (!seen.Add(animation)) return false;
        foreach (var frame in animation._frames)
            if (Reaches(frame.Texture, target, seen)) return true;
        return false;
    }

    private static void CheckSlot(int frame)
    {
        if ((uint)frame >= MaxFrames) throw new ArgumentOutOfRangeException(nameof(frame));
    }

    private bool Uses(Texture texture)
    {
        foreach (var frame in _frames) if (ReferenceEquals(frame.Texture, texture)) return true;
        return false;
    }

    private bool UsesElsewhere(Texture texture, int except)
    {
        for (var i = 0; i < MaxFrames; i++) if (i != except && ReferenceEquals(_frames[i].Texture, texture)) return true;
        return false;
    }

    private void SourceChanged(Resource _)
    {
        lock (GraphGate) _sizeDirty = true;
        EmitChanged();
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimatedTexture();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (AnimatedTexture)target;
        lock (GraphGate)
        {
            ThrowIfDisposed();
            copy._frameCount = _frameCount; copy._currentFrame = _currentFrame; copy._pause = _pause;
            copy._oneShot = _oneShot; copy._speedScale = _speedScale;
            copy._sizeDirty = true;
            for (var i = 0; i < MaxFrames; i++)
            {
                copy._frames[i].Duration = _frames[i].Duration;
                copy.SetFrameTexture(i, deep ? (Texture?)duplicateSubresource(_frames[i].Texture) : _frames[i].Texture);
            }
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<AnimatedTexture, int>(nameof(Frames), t => t.Frames, (t, v) => t.Frames = v, _ => 1, stored: true),
        new PropertyDescriptor<AnimatedTexture, int>(nameof(CurrentFrame), t => t.CurrentFrame, (t, v) => t.CurrentFrame = v, _ => 0),
        new PropertyDescriptor<AnimatedTexture, bool>(nameof(OneShot), t => t.OneShot, (t, v) => t.OneShot = v, _ => false, stored: true),
        new PropertyDescriptor<AnimatedTexture, bool>(nameof(Pause), t => t.Pause, (t, v) => t.Pause = v, _ => false, stored: true),
        new PropertyDescriptor<AnimatedTexture, float>(nameof(SpeedScale), t => t.SpeedScale, (t, v) => t.SpeedScale = v, _ => 1, stored: true),
    ]);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (GraphGate)
            {
                for (var i = 0; i < MaxFrames; i++)
                {
                    if (_frames[i].Texture is { } texture && !UsesElsewhere(texture, i)) texture.Changed -= SourceChanged;
                    _frames[i].Texture = null;
                }
            }
        }
        base.Dispose(disposing);
    }
}
