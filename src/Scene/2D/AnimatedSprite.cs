namespace Electron2D;

/// <summary>Plays named texture-frame animations using the scene's idle process clock.</summary>
/// <remarks>SpriteFrames and textures are borrowed. Internal processing is independent of ProcessEnabled and
/// follows tree pause/process mode and scaled delta. Mutations require the owner thread while attached.
/// Events are synchronous; exceptions retain committed state and stop the current operation. Detached playback
/// retains its state but does not advance. Frame-resource worker notifications are reconciled on the owner thread.
/// Exact frame boundaries transition on the next positive tick. A transition iteration consumes time at the previous
/// frame rate; following iterations use the new duration. Ping-pong retains the endpoint and reverses custom speed.
/// Each process call consumes at most frame-count plus one iterations, discarding any excess delta.</remarks>
public class AnimatedSprite : Entity
{
    private static readonly PropertyDescriptor[] AnimationProperties =
    [
        new PropertyDescriptor<AnimatedSprite, SpriteFrames?>(nameof(SpriteFrames), s => s.SpriteFrames, (s, v) => s.SpriteFrames = v, _ => null, stored: true),
        new PropertyDescriptor<AnimatedSprite, string>(nameof(Animation), s => s.Animation, (s, v) => s.Animation = v, _ => "default", stored: true),
        new PropertyDescriptor<AnimatedSprite, string>(nameof(Autoplay), s => s.Autoplay, (s, v) => s.Autoplay = v, _ => "", stored: true),
        new PropertyDescriptor<AnimatedSprite, int>(nameof(Frame), s => s.Frame, (s, v) => s.Frame = v, _ => 0, stored: true),
        new PropertyDescriptor<AnimatedSprite, float>(nameof(FrameProgress), s => s.FrameProgress, (s, v) => s.FrameProgress = v, _ => 0, stored: true),
        new PropertyDescriptor<AnimatedSprite, float>(nameof(SpeedScale), s => s.SpeedScale, (s, v) => s.SpeedScale = v, _ => 1, stored: true),
        new PropertyDescriptor<AnimatedSprite, bool>(nameof(Centered), s => s.Centered, (s, v) => s.Centered = v, _ => true, stored: true),
        new PropertyDescriptor<AnimatedSprite, Vector2>(nameof(Offset), s => s.Offset, (s, v) => s.Offset = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<AnimatedSprite, bool>(nameof(FlipH), s => s.FlipH, (s, v) => s.FlipH = v, _ => false, stored: true),
        new PropertyDescriptor<AnimatedSprite, bool>(nameof(FlipV), s => s.FlipV, (s, v) => s.FlipV = v, _ => false, stored: true),
    ];

    private static readonly PropertyDescriptor PlayingFrame = new PropertyDescriptor<AnimatedSprite, int>(nameof(Frame), s => s.Frame);

    private SpriteFrames? _frames;
    private string _animation = "default", _autoplay = "";
    private int _frame, _resourceChangePending;
    private float _frameProgress, _speedScale = 1, _customSpeed = 1, _frameSpeed = 1;
    private bool _playing, _centered = true, _flipH, _flipV;
    private Vector2 _offset;

    /// <summary>Creates a centered, stopped sprite with no frame library and the default animation selected.</summary>
    public AnimatedSprite() { }

    /// <summary>Gets or sets the borrowed frame library.</summary>
    /// <value>Null by default.</value>
    /// <remarks>Replacement selects the first inserted animation when the current one is absent, clears invalid
    /// autoplay, stops playback, notifies the property list, requests redraw and emits SpriteFramesChanged.
    /// Equal assignment is a no-op. Assigning null retains the selected name and frame/progress.</remarks>
    /// <exception cref="ObjectDisposedException">The node or new library is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable on this thread or during capture.</exception>
    /// <exception cref="Exception">A callback fails after commitment.</exception>
    public SpriteFrames? SpriteFrames
    {
        get { ThrowIfDisposed(); return _frames; }
        set
        {
            EnsureMutable();
            if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
            if (ReferenceEquals(_frames, value)) return;
            if (_frames is not null) _frames.Changed -= FramesChanged;
            _frames = value; Interlocked.Exchange(ref _resourceChangePending, 0);
            if (_frames is not null)
            {
                _frames.Changed += FramesChanged;
                if (!_frames.HasAnimation(_animation)) Animation = _frames.FirstAnimation;
                if (IsDisposed) return;
                if (_frames is not null && !_frames.HasAnimation(_autoplay)) _autoplay = "";
            }
            Stop(); if (IsDisposed) return;
            NotifyPropertyListChanged(); if (IsDisposed) return;
            InvalidateCanvas(); SpriteFramesChanged?.Invoke();
        }
    }

    /// <summary>Gets or sets the selected animation.</summary>
    /// <value>The default name initially.</value>
    /// <remarks>A changed name emits AnimationChanged before resetting frame/progress. Selection preserves
    /// playing state for a nonempty animation and chooses the end when the current playing speed is negative.
    /// Empty animations stop. Invalid selection commits the name/event before stopping and reporting the error.</remarks>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The selected nonempty name is absent from the library.</exception>
    /// <exception cref="InvalidOperationException">No library exists, or mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node or library is disposed.</exception>
    /// <exception cref="Exception">An event handler fails after commitment.</exception>
    public string Animation
    {
        get { ThrowIfDisposed(); return _animation; }
        set
        {
            EnsureMutable(); ArgumentNullException.ThrowIfNull(value);
            if (_animation == value) return;
            _animation = value; AnimationChanged?.Invoke(); if (IsDisposed) return;
            if (_frames is null) { _animation = ""; Stop(); throw new InvalidOperationException("No frame library is assigned."); }
            if (_animation == "") { Stop(); return; }
            if (!_frames.TryRead(_animation, _frame, out var count, out _, out _, out _, out _))
            { Stop(); throw new ArgumentException("The animation does not exist.", nameof(value)); }
            if (count == 0) { Stop(); return; }
            var backwards = float.IsNegative(GetPlayingSpeed());
            SetFrameAndProgress(backwards ? count - 1 : 0, backwards ? 1 : 0); if (IsDisposed) return;
            NotifyPropertyListChanged(); if (!IsDisposed) InvalidateCanvas();
        }
    }

    /// <summary>Gets or sets the animation played after ready handling.</summary>
    /// <value>Empty by default. An absent name has no ready-time effect.</value>
    /// <remarks>Setting this after ready does not immediately start playback. RequestReady permits a later ready cycle.</remarks>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public string Autoplay { get { ThrowIfDisposed(); return _autoplay; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); _autoplay = value; } }

    /// <summary>Gets or sets the displayed frame index.</summary>
    /// <value>Zero initially.</value>
    /// <remarks>Uses SetFrameAndProgress, resetting progress to one for negative playing speed and zero otherwise.
    /// An assignment without a library does nothing. Worker library changes are reconciled before an owner-thread read.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node or library is disposed.</exception>
    /// <exception cref="Exception">A frame or property-list subscriber fails after commitment.</exception>
    public int Frame { get { ThrowIfDisposed(); RefreshFramesIfOwner(); return _frame; } set => SetFrameAndProgress(value, float.IsNegative(GetPlayingSpeed()) ? 1 : 0); }

    /// <summary>Gets or sets progress through the current frame.</summary>
    /// <value>Zero initially; ordinary playback moves between zero and one.</value>
    /// <remarks>Any finite value is retained without clamping, redraw or an event.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Progress is not finite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public float FrameProgress { get { ThrowIfDisposed(); RefreshFramesIfOwner(); return _frameProgress; } set { EnsureMutable(); ValidateFinite(value, nameof(value)); _frameProgress = value; } }

    /// <summary>Gets or sets the signed playback multiplier.</summary>
    /// <value>One initially. Zero freezes advancement while IsPlaying stays true.</value>
    /// <exception cref="ArgumentOutOfRangeException">The multiplier or its product with custom speed is not finite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public float SpeedScale
    {
        get { ThrowIfDisposed(); return _speedScale; }
        set { EnsureMutable(); ValidateFinite(value, nameof(value)); ValidateFinite(value * _customSpeed, nameof(value)); _speedScale = value; }
    }

    /// <summary>Gets or sets whether the texture is centered around Offset.</summary>
    /// <value>True initially.</value>
    /// <remarks>Changes request redraw and emit ItemRectChanged.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    /// <exception cref="Exception">An ItemRectChanged subscriber fails after commitment.</exception>
    public bool Centered { get { ThrowIfDisposed(); return _centered; } set { EnsureMutable(); if (_centered == value) return; _centered = value; InvalidateCanvas(); NotifyItemRectChanged(); } }

    /// <summary>Gets or sets the finite local drawing offset.</summary>
    /// <value>Zero initially.</value>
    /// <remarks>Changes request redraw and emit ItemRectChanged.</remarks>
    /// <exception cref="ArgumentException">The offset is not finite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    /// <exception cref="Exception">An ItemRectChanged subscriber fails after commitment.</exception>
    public Vector2 Offset
    {
        get { ThrowIfDisposed(); return _offset; }
        set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("The offset must be finite.", nameof(value)); if (_offset == value) return; _offset = value; InvalidateCanvas(); NotifyItemRectChanged(); }
    }

    /// <summary>Gets or sets horizontal flipping without moving the drawing origin.</summary>
    /// <value>False initially.</value>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool FlipH { get { ThrowIfDisposed(); return _flipH; } set { EnsureMutable(); if (_flipH == value) return; _flipH = value; InvalidateCanvas(); } }

    /// <summary>Gets or sets vertical flipping without moving the drawing origin.</summary>
    /// <value>False initially.</value>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool FlipV { get { ThrowIfDisposed(); return _flipV; } set { EnsureMutable(); if (_flipV == value) return; _flipV = value; InvalidateCanvas(); } }

    /// <summary>Occurs when the selected animation name changes.</summary>
    public event Action? AnimationChanged;
    /// <summary>Occurs after non-looping playback pauses at an endpoint.</summary>
    public event Action? AnimationFinished;
    /// <summary>Occurs at a wrap or direction reversal, before the following FrameChanged event.</summary>
    public event Action? AnimationLooped;
    /// <summary>Occurs on a frame transition or an explicit requested index different from the prior index.</summary>
    /// <remarks>A clamped assignment may emit even if the resulting index stays equal. Automatic transitions do not emit ItemRectChanged.</remarks>
    public event Action? FrameChanged;
    /// <summary>Occurs after library replacement and its stop/property-list/redraw stages.</summary>
    public event Action? SpriteFramesChanged;

    /// <summary>Tests whether playback is enabled, including when its speed is zero or the node is detached.</summary>
    /// <returns>The playback flag.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool IsPlaying() { ThrowIfDisposed(); return _playing; }

    /// <summary>Returns the signed product of SpeedScale and the custom speed passed to Play.</summary>
    /// <returns>Zero when paused; otherwise the multiplier, excluding animation FPS and relative frame duration.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public float GetPlayingSpeed() { ThrowIfDisposed(); return _playing ? _speedScale * _customSpeed : 0; }

    /// <summary>Starts or resumes a named animation.</summary>
    /// <param name="name">The exact name, or empty to use the current selection.</param>
    /// <param name="customSpeed">A finite signed multiplier; zero still enables playback.</param>
    /// <param name="fromEnd">Start a newly selected animation at its last frame/progress one.</param>
    /// <remarks>An empty animation is a no-op. The same animation resumes, except at the corresponding completed
    /// endpoint, where forward Play or reverse Play with fromEnd restarts. New selection emits FrameChanged before
    /// AnimationChanged. Processing, property-list notification and redraw follow the selection events.
    /// Internal scheduling follows the final playback flag even when an observer throws or reenters.</remarks>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The animation is absent.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Custom speed or its scaled product is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">No library exists or mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node or library is disposed.</exception>
    /// <exception cref="Exception">A callback fails after commitment.</exception>
    public void Play(string name = "", float customSpeed = 1, bool fromEnd = false)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(name); ValidateFinite(customSpeed, nameof(customSpeed)); ValidateFinite(customSpeed * _speedScale, nameof(customSpeed));
        RefreshFramesIfOwner();
        name = name == "" ? _animation : name;
        if (_frames is null) throw new InvalidOperationException("No frame library is assigned.");
        if (!_frames.TryRead(name, _frame, out var count, out _, out _, out _, out _)) throw new ArgumentException("The animation does not exist.", nameof(name));
        if (count == 0) return;
        _playing = true; _customSpeed = customSpeed;
        try
        {
            if (name != _animation)
            {
                _animation = name; SetFrameAndProgress(fromEnd ? count - 1 : 0, fromEnd ? 1 : 0); if (IsDisposed) return;
                AnimationChanged?.Invoke(); if (IsDisposed) return;
            }
            else
            {
                var backwards = float.IsNegative(_speedScale * _customSpeed);
                if (fromEnd && backwards && _frame == 0 && _frameProgress <= 0) SetFrameAndProgress(count - 1, 1);
                else if (!fromEnd && !backwards && _frame == count - 1 && _frameProgress >= 1) SetFrameAndProgress(0, 0);
                if (IsDisposed) return;
            }
            SetInternalProcessing(_playing, false); NotifyPropertyListChanged(); if (!IsDisposed) InvalidateCanvas();
        }
        finally { if (!IsDisposed) SetInternalProcessing(_playing, false); }
    }

    /// <summary>Starts or resumes reverse playback using Play(name, -1, true).</summary>
    /// <param name="name">The animation name, or empty for the current selection.</param>
    /// <remarks>Shares all Play validation, events and lifetime rules.</remarks>
    public void PlayBackwards(string name = "") => Play(name, -1, true);

    /// <summary>Pauses playback while retaining frame, progress and custom speed.</summary>
    /// <remarks>Notifies the property list before disabling internal processing.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    /// <exception cref="Exception">A property-list subscriber fails after commitment.</exception>
    public void Pause() { EnsureMutable(); StopInternal(false); }

    /// <summary>Stops playback, resets custom speed to one and resets frame/progress when a library is assigned.</summary>
    /// <remarks>Notifies the property list before disabling internal processing.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node or library is disposed.</exception>
    /// <exception cref="Exception">A subscriber fails after commitment.</exception>
    public void Stop() { EnsureMutable(); StopInternal(true); }

    /// <summary>Sets the frame index and its progress together.</summary>
    /// <param name="frame">Requested index; negatives clamp to zero and valid animation indices clamp to the last frame.</param>
    /// <param name="progress">Any finite progress, without clamping.</param>
    /// <remarks>Without a library this is a no-op. If the selected animation is absent, positive indices are retained.
    /// FrameChanged compares the request with the old index, before clamping. Progress-only changes do not redraw.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Progress is not finite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node or library is disposed.</exception>
    /// <exception cref="Exception">A frame subscriber fails after commitment.</exception>
    public void SetFrameAndProgress(int frame, float progress)
    {
        EnsureMutable(); ValidateFinite(progress, nameof(progress));
        if (_frames is null) return;
        var has = _frames.TryRead(_animation, frame, out var count, out _, out _, out _, out _);
        var changed = _frame != frame;
        _frame = Math.Max(0, has ? Math.Min(frame, Math.Max(0, count - 1)) : frame);
        CalculateFrameSpeed(); _frameProgress = progress;
        if (changed) { InvalidateCanvas(); FrameChanged?.Invoke(); }
    }

    private void StopInternal(bool reset)
    {
        _playing = false;
        try
        {
            if (reset) { _customSpeed = 1; SetFrameAndProgress(0, 0); if (IsDisposed) return; }
            NotifyPropertyListChanged();
        }
        finally { if (!IsDisposed) SetInternalProcessing(_playing, false); }
    }

    private void CalculateFrameSpeed()
    {
        var duration = 1f;
        _frames?.TryRead(_animation, _frame, out _, out _, out _, out _, out duration);
        _frameSpeed = 1f / duration;
    }

    private void FramesChanged(Resource source)
    {
        if (IsDisposed || !ReferenceEquals(source, _frames)) return;
        Interlocked.Exchange(ref _resourceChangePending, 1); InvalidateCanvas(); RefreshFramesIfOwner();
    }

    private void RefreshFramesIfOwner()
    {
        if (Tree is { IsOwnerThread: false } || Interlocked.Exchange(ref _resourceChangePending, 0) == 0) return;
        SetFrameAndProgress(_frame, _frameProgress);
        if (!IsDisposed) NotifyPropertyListChanged();
    }

    /// <inheritdoc />
    /// <remarks>Handles resource reconciliation, ready-time autoplay and internal idle playback after inherited handling.</remarks>
    protected override void OnNotification(int what)
    {
        base.OnNotification(what); if (IsDisposed) return;
        if (what is NotificationEnterTree or NotificationReady or NotificationInternalProcess or NotificationDraw) RefreshFramesIfOwner();
        if (IsDisposed) return;
        if (what == NotificationReady && _frames?.HasAnimation(_autoplay) == true) Play(_autoplay);
        else if (what == NotificationInternalProcess && _playing) Advance(ProcessDeltaTime);
    }

    private void Advance(double remaining)
    {
        var iteration = 0;
        while (remaining > 0 && !IsDisposed)
        {
            if (_frames?.TryRead(_animation, _frame, out var count, out var FPS, out var loop, out _, out _) != true || count == 0) return;
            var speed = FPS * _speedScale * _customSpeed * _frameSpeed;
            if (!double.IsFinite(speed)) throw new InvalidOperationException("Animation playback rate overflowed.");
            if (speed == 0) return;
            var backwards = double.IsNegative(speed); var absoluteSpeed = Math.Abs(speed);
            if (backwards ? _frameProgress <= 0 : _frameProgress >= 1)
            {
                if (backwards ? _frame <= 0 : _frame >= count - 1)
                {
                    _frame = backwards ? 0 : count - 1;
                    if (loop == SpriteFrames.LoopMode.None) { Pause(); if (!IsDisposed) AnimationFinished?.Invoke(); return; }
                    if (loop == SpriteFrames.LoopMode.PingPong) _customSpeed = -_customSpeed;
                    else _frame = backwards ? count - 1 : 0;
                    AnimationLooped?.Invoke(); if (IsDisposed || _frames is null) return;
                }
                else _frame += backwards ? -1 : 1;
                CalculateFrameSpeed(); _frameProgress = backwards ? 1 : 0;
                InvalidateCanvas(); FrameChanged?.Invoke(); if (IsDisposed || _frames is null) return;
            }
            var consumed = Math.Min((backwards ? _frameProgress : 1d - _frameProgress) / absoluteSpeed, remaining);
            _frameProgress = (float)(_frameProgress + consumed * absoluteSpeed * (backwards ? -1 : 1));
            remaining -= consumed;
            if (++iteration > count) return;
        }
    }

    /// <inheritdoc />
    /// <remarks>Draws the current non-null frame through its texture's virtual region hook. Calls to base.OnDraw
    /// preserve the animation image in derived classes. Transform snapping rounds only the local drawing offset.</remarks>
    protected override void OnDraw()
    {
        if (_frames?.TryRead(_animation, _frame, out _, out _, out _, out var texture, out _) != true || texture is null) return;
        var size = texture.Size;
        if (!size.IsFinite() || size.X < 0 || size.Y < 0) throw new InvalidOperationException("Animation texture dimensions must be finite and nonnegative.");
        var offset = _centered ? _offset - size / 2 : _offset;
        if (IsInsideTree && GetViewport()?.SnapTransformsToPixel == true) offset = CanvasGeometry.Snap(offset);
        DrawTextureRectRegion(texture, new Rect(offset, new Vector2(_flipH ? -size.X : size.X, _flipV ? -size.Y : size.Y)), new Rect(Vector2.Zero, size));
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(AnimationProperties);
    /// <inheritdoc />
    /// <remarks>During playback, tooling exposes Frame as read-only and does not store its transient value.</remarks>
    protected override PropertyDescriptor? ValidateProperty(PropertyDescriptor property) => _playing && property.Name == nameof(Frame)
        ? PlayingFrame : base.ValidateProperty(property);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(AnimatedSprite) ? CreateAnimatedSprite : base.CreateSceneInstanceFactory();

    private static Node CreateAnimatedSprite() => new AnimatedSprite();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_frames is not null) _frames.Changed -= FramesChanged;
            _frames = null; _playing = false;
            AnimationChanged = AnimationFinished = AnimationLooped = FrameChanged = SpriteFramesChanged = null;
        }
        base.Dispose(disposing);
    }

    private static void ValidateFinite(float value, string parameter) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(parameter); }
}
