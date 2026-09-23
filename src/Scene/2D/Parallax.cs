namespace Electron2D;

/// <summary>Scrolls and repeats a canvas subtree relative to the current camera.</summary>
/// <remarks>Camera movement updates ScreenOffset while this node is attached. The inherited Position is
/// calculated from scroll settings and is not stored in packed scenes. Repetition affects drawing, not
/// logical child transforms. Like other scene nodes, attached mutation belongs to the tree owner thread.</remarks>
public class Parallax : Entity
{
    private Vector2 _autoscroll;
    private bool _followViewport = true;
    private bool _ignoreCameraScroll;
    private Vector2 _limitBegin = new(-10_000_000, -10_000_000);
    private Vector2 _limitEnd = new(10_000_000, 10_000_000);
    private Vector2 _repeatSize;
    private int _repeatTimes = 1;
    private Vector2 _screenOffset;
    private Vector2 _scrollOffset;
    private Vector2 _scrollScale = Vector2.One;
    private Viewport? _viewport;

    /// <summary>Creates a detached parallax canvas with unit scroll scale and no repetition.</summary>
    public Parallax() { }

    /// <summary>Gets or sets automatic scroll velocity in canvas units per process second.</summary>
    /// <value>Zero initially. Automatic motion runs only while at least one repeat axis is enabled.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned vector is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner or the scroll calculation overflows.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector2 Autoscroll
    {
        get { CheckQuery(); return _autoscroll; }
        set { EnsureMutable(); Finite(value); if (_autoscroll == value) return; _autoscroll = value; UpdateProcessing(); UpdateScroll(); }
    }

    /// <summary>Gets or sets whether the calculated offset includes camera position.</summary>
    /// <value>True initially. A changed policy applies on the next scroll update.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool FollowViewport
    {
        get { CheckQuery(); return _followViewport; }
        set { EnsureMutable(); _followViewport = value; }
    }

    /// <summary>Gets or sets whether camera movement updates ScreenOffset.</summary>
    /// <value>False initially. The current ScreenOffset is retained when enabled.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool IgnoreCameraScroll
    {
        get { CheckQuery(); return _ignoreCameraScroll; }
        set { EnsureMutable(); _ignoreCameraScroll = value; }
    }

    /// <summary>Gets or sets the top-left camera scroll limit.</summary>
    /// <value>(-10000000, -10000000) initially; applies when a scroll update runs.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned vector is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector2 LimitBegin
    {
        get { CheckQuery(); return _limitBegin; }
        set { EnsureMutable(); Finite(value); _limitBegin = value; }
    }

    /// <summary>Gets or sets the bottom-right camera scroll limit.</summary>
    /// <value>(10000000, 10000000) initially; applies when a scroll update runs.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned vector is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector2 LimitEnd
    {
        get { CheckQuery(); return _limitEnd; }
        set { EnsureMutable(); Finite(value); _limitEnd = value; }
    }

    /// <summary>Gets or sets the local spacing between repeated copies of the canvas subtree.</summary>
    /// <value>Zero initially. Negative components clamp to zero; a zero axis does not repeat.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned vector is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner or the scroll calculation overflows.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector2 RepeatSize
    {
        get { CheckQuery(); return _repeatSize; }
        set
        {
            EnsureMutable(); Finite(value);
            if (_repeatSize == value) return;
            _repeatSize = new(MathF.Max(0, value.X), MathF.Max(0, value.Y));
            UpdateProcessing(); UpdateScroll();
        }
    }

    /// <summary>Gets or sets the number of additional copies along each enabled repeat axis.</summary>
    /// <value>One initially. Values below one clamp to one.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public int RepeatTimes
    {
        get { CheckQuery(); return _repeatTimes; }
        set { EnsureMutable(); _repeatTimes = Math.Max(1, value); }
    }

    /// <summary>Gets or sets the camera-derived scroll origin, or a manual origin when camera scroll is ignored.</summary>
    /// <value>Zero initially; camera updates overwrite it unless IgnoreCameraScroll is true.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned vector is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner or the scroll calculation overflows.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector2 ScreenOffset
    {
        get { CheckQuery(); return _screenOffset; }
        set { EnsureMutable(); Finite(value); if (_screenOffset == value) return; _screenOffset = value; UpdateScroll(); }
    }

    /// <summary>Gets or sets the manual offset added to camera-relative scrolling.</summary>
    /// <value>Zero initially. Automatic motion wraps enabled repeat axes.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned vector is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner or the scroll calculation overflows.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector2 ScrollOffset
    {
        get { CheckQuery(); return _scrollOffset; }
        set { EnsureMutable(); Finite(value); if (_scrollOffset == value) return; _scrollOffset = value; UpdateScroll(); }
    }

    /// <summary>Gets or sets the camera scroll multiplier on each axis.</summary>
    /// <value>One on each axis initially; applies on the next scroll update.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned vector is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector2 ScrollScale
    {
        get { CheckQuery(); return _scrollScale; }
        set { EnsureMutable(); Finite(value); _scrollScale = value; }
    }

    internal void CameraMoved(Vector2 adjustedScreenPosition, bool snapToPixel)
    {
        if (_ignoreCameraScroll) return;
        if (snapToPixel)
        {
            var size = _viewport!.GetVisibleRect().Size;
            adjustedScreenPosition = (adjustedScreenPosition + new Vector2((int)size.X % 2 == 0 ? .5f : 0, (int)size.Y % 2 == 0 ? .5f : 0)).Floor();
        }
        ScreenOffset = adjustedScreenPosition;
    }

    internal override void OnTreeMembershipChanged(bool entering)
    {
        if (!entering)
        {
            try { _viewport?.UnregisterParallax(this); }
            finally { _viewport = null; base.OnTreeMembershipChanged(false); }
            return;
        }
        base.OnTreeMembershipChanged(true);
        _viewport = GetViewport() ?? throw new InvalidOperationException("Parallax requires an active viewport.");
        _viewport.RegisterParallax(this);
        UpdateProcessing(); UpdateScroll();
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what != NotificationInternalProcess || !IsInsideTree) return;
        var offset = _scrollOffset + _autoscroll * (float)ProcessDeltaTime;
        if (!offset.IsFinite()) throw new InvalidOperationException("Parallax automatic scroll overflowed finite coordinates.");
        if (_repeatSize.X != 0) offset.X = Mathf.PosMod(offset.X, _repeatSize.X);
        if (_repeatSize.Y != 0) offset.Y = Mathf.PosMod(offset.Y, _repeatSize.Y);
        _scrollOffset = offset;
        UpdateScroll();
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Where(property => property.Name != nameof(Position)).Concat(ParallaxProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Parallax)
        ? CreateParallax : base.CreateSceneInstanceFactory();

    private static Node CreateParallax() => new Parallax();

    private void UpdateProcessing() => SetInternalProcessing(
        (_repeatSize.X != 0 || _repeatSize.Y != 0) && (_autoscroll.X != 0 || _autoscroll.Y != 0), false);

    private void UpdateScroll()
    {
        if (!IsInsideTree || _viewport is null) return;
        var size = _viewport.GetVisibleRect().Size;
        var scroll = _screenOffset;
        if (_limitBegin.X <= _limitEnd.X - size.X) scroll.X = Math.Clamp(scroll.X, _limitBegin.X, _limitEnd.X - size.X);
        if (_limitBegin.Y <= _limitEnd.Y - size.Y) scroll.Y = Math.Clamp(scroll.Y, _limitBegin.Y, _limitEnd.Y - size.Y);
        scroll *= _scrollScale;
        var scale = Scale;
        var periodX = _repeatSize.X * scale.X;
        var periodY = _repeatSize.Y * scale.Y;
        scroll.X = _repeatSize.X != 0 && periodX != 0
            ? _screenOffset.X - Mathf.PosMod(scroll.X - _scrollOffset.X, periodX)
            : _screenOffset.X + _scrollOffset.X - scroll.X;
        scroll.Y = _repeatSize.Y != 0 && periodY != 0
            ? _screenOffset.Y - Mathf.PosMod(scroll.Y - _scrollOffset.Y, periodY)
            : _screenOffset.Y + _scrollOffset.Y - scroll.Y;
        if (!_followViewport) scroll -= _screenOffset;
        if (!scroll.IsFinite()) throw new InvalidOperationException("Parallax scroll overflowed finite coordinates.");
        Position = scroll;
    }

    private void CheckQuery() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private static void Finite(Vector2 value)
    {
        if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value), "Parallax coordinates must be finite.");
    }

    private static readonly PropertyDescriptor[] ParallaxProperties =
    [
        new PropertyDescriptor<Parallax, Vector2>(nameof(Autoscroll), n => n.Autoscroll, (n, v) => n.Autoscroll = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Parallax, bool>(nameof(FollowViewport), n => n.FollowViewport, (n, v) => n.FollowViewport = v, _ => true, stored: true),
        new PropertyDescriptor<Parallax, bool>(nameof(IgnoreCameraScroll), n => n.IgnoreCameraScroll, (n, v) => n.IgnoreCameraScroll = v, _ => false, stored: true),
        new PropertyDescriptor<Parallax, Vector2>(nameof(LimitBegin), n => n.LimitBegin, (n, v) => n.LimitBegin = v, _ => new(-10_000_000, -10_000_000), stored: true),
        new PropertyDescriptor<Parallax, Vector2>(nameof(LimitEnd), n => n.LimitEnd, (n, v) => n.LimitEnd = v, _ => new(10_000_000, 10_000_000), stored: true),
        new PropertyDescriptor<Parallax, Vector2>(nameof(RepeatSize), n => n.RepeatSize, (n, v) => n.RepeatSize = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Parallax, int>(nameof(RepeatTimes), n => n.RepeatTimes, (n, v) => n.RepeatTimes = v, _ => 1, stored: true),
        new PropertyDescriptor<Parallax, Vector2>(nameof(ScreenOffset), n => n.ScreenOffset, (n, v) => n.ScreenOffset = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Parallax, Vector2>(nameof(ScrollOffset), n => n.ScrollOffset, (n, v) => n.ScrollOffset = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Parallax, Vector2>(nameof(ScrollScale), n => n.ScrollScale, (n, v) => n.ScrollScale = v, _ => Vector2.One, stored: true),
    ];
}
