namespace Electron2D;

/// <summary>Moves a viewport's default canvas to follow a spatial scene node.</summary>
/// <remarks>One camera is current per viewport. The first enabled camera becomes current on entry. Tracking
/// uses internal process/physics notifications and the inherited pause policy. Camera state does not change
/// node transforms. Finite values are required; rendering overflow fails before changing the viewport.
/// Editor overlays, physics interpolation and independent offscreen viewports are not implemented.</remarks>
public partial class Camera : Entity
{
    /// <summary>Chooses the point of the visible area anchored to the tracked position.</summary>
    public enum AnchorModeEnum
    {
        /// <summary>Anchors the top-left corner at the tracked position.</summary>
        FixedTopLeft = 0,
        /// <summary>Centers the visible area and applies drag margins and offsets.</summary>
        DragCenter = 1,
    }

    /// <summary>Selects the internal frame lane that updates the camera.</summary>
    public enum CameraProcessCallback
    {
        /// <summary>Updates on physics frames.</summary>
        Physics = 0,
        /// <summary>Updates on process frames.</summary>
        Idle = 1,
    }

    private Vector2 _offset = Vector2.Zero;
    private Vector2 _zoom = Vector2.One;
    private AnchorModeEnum _anchorMode = AnchorModeEnum.DragCenter;
    private bool _ignoreRotation = true;
    private bool _enabled = true;
    private CameraProcessCallback _processCallback = CameraProcessCallback.Idle;
    private bool _limitEnabled = true;
    private bool _limitSmoothed = false;
    private bool _positionSmoothingEnabled = false;
    private float _positionSmoothingSpeed = 5f;
    private bool _rotationSmoothingEnabled = false;
    private float _rotationSmoothingSpeed = 5f;
    private bool _dragHorizontalEnabled = false;
    private bool _dragVerticalEnabled = false;
    private float _dragHorizontalOffset = 0f;
    private float _dragVerticalOffset = 0f;
    private readonly int[] _limits = [-10_000_000, -10_000_000, 10_000_000, 10_000_000];
    private readonly float[] _dragMargins = [0.2f, 0.2f, 0.2f, 0.2f];
    private Viewport? _viewport, _customViewport;
    private Vector2 _targetPosition, _smoothedPosition, _screenCenter;
    private float _screenRotation;
    private bool _first = true, _horizontalOffsetChanged, _verticalOffsetChanged;

    /// <summary>Creates an enabled, centered camera with unit zoom, ignored rotation, default limits and no smoothing.</summary>
    /// <remarks>Enables inherited transform notifications. The camera remains detached until added to a viewport scene.</remarks>
    public Camera() => NotifyTransformChanges = true;

    /// <summary>Gets or sets the additional canvas offset.</summary>
    /// <value>Vector2.Zero initially.</value>
    /// <remarks>Finite offset in canvas units, applied after limits. Equal writes do nothing.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or outside the documented contract.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public Vector2 Offset
    {
        get { CheckQuery(); return _offset; }
        set { EnsureMutable(); Finite(value); if (_offset == value) return; _offset = value; UpdateScrollPreservingPosition(); }
    }

    /// <summary>Gets or sets the canvas magnification.</summary>
    /// <value>Vector2.One initially.</value>
    /// <remarks>Finite, nonzero zoom; negative axes mirror the view. Approximately zero axes are rejected.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or outside the documented contract.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public Vector2 Zoom
    {
        get { CheckQuery(); return _zoom; }
        set { EnsureMutable(); Finite(value); if (Mathf.IsZeroApprox(value.X) || Mathf.IsZeroApprox(value.Y)) throw new ArgumentOutOfRangeException(nameof(value), "Zoom axes must be nonzero."); if (_zoom == value) return; _zoom = value; UpdateScrollPreservingPosition(); }
    }

    /// <summary>Gets or sets the camera anchor mode.</summary>
    /// <value>AnchorModeEnum.DragCenter initially.</value>
    /// <remarks>Selects centered tracking or a fixed top-left anchor. Changes update the current view.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or outside the documented contract.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public AnchorModeEnum AnchorMode
    {
        get { CheckQuery(); return _anchorMode; }
        set { EnsureMutable(); if (value is not (AnchorModeEnum.FixedTopLeft or AnchorModeEnum.DragCenter)) throw new ArgumentOutOfRangeException(nameof(value)); if (_anchorMode == value) return; _anchorMode = value; UpdateScroll(); }
    }

    /// <summary>Gets or sets whether view rotation ignores the node rotation.</summary>
    /// <value>true initially.</value>
    /// <remarks>True keeps the view unrotated. Enabling resets the cached angle to zero.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public bool IgnoreRotation
    {
        get { CheckQuery(); return _ignoreRotation; }
        set { EnsureMutable(); if (_ignoreRotation == value) return; _ignoreRotation = value; if (value) _screenRotation = 0; UpdateScrollPreservingPosition(); }
    }

    /// <summary>Gets or sets whether this camera can become current.</summary>
    /// <value>true initially.</value>
    /// <remarks>Controls eligibility for the viewport current camera. Disabling the current camera selects the first enabled camera in tree order.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public bool Enabled
    {
        get { CheckQuery(); return _enabled; }
        set { EnsureMutable(); if (_enabled == value) return; _enabled = value; if (_viewport is { } viewport) { if (value && viewport.GetCamera() is null) MakeCurrent(); else if (!value && ReferenceEquals(viewport.GetCamera(), this)) viewport.ReleaseCamera(this); } }
    }

    /// <summary>Gets or sets the internal frame lane for tracking.</summary>
    /// <value>CameraProcessCallback.Idle initially.</value>
    /// <remarks>Selects the internal process lane, independently of the public processing flags.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or outside the documented contract.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public CameraProcessCallback ProcessCallback
    {
        get { CheckQuery(); return _processCallback; }
        set { EnsureMutable(); if (value is not (CameraProcessCallback.Physics or CameraProcessCallback.Idle)) throw new ArgumentOutOfRangeException(nameof(value)); if (_processCallback == value) return; _processCallback = value; UpdateProcessing(); }
    }

    /// <summary>Gets or sets whether scroll limits apply.</summary>
    /// <value>true initially.</value>
    /// <remarks>Enables all four limits. Offset may move the view beyond them.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public bool LimitEnabled
    {
        get { CheckQuery(); return _limitEnabled; }
        set { EnsureMutable(); if (_limitEnabled == value) return; _limitEnabled = value; UpdateScroll(); }
    }

    /// <summary>Gets or sets whether limits constrain the target before smoothing.</summary>
    /// <value>false initially.</value>
    /// <remarks>Limits the target before position smoothing. Otherwise limits constrain the rendered origin.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public bool LimitSmoothed
    {
        get { CheckQuery(); return _limitSmoothed; }
        set { EnsureMutable(); if (_limitSmoothed == value) return; _limitSmoothed = value; UpdateScroll(); }
    }

    /// <summary>Gets or sets whether position updates interpolate toward the target.</summary>
    /// <value>false initially.</value>
    /// <remarks>Uses the position smoothing speed on subsequent updates; enabling does not immediately reset the cached position.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public bool PositionSmoothingEnabled
    {
        get { CheckQuery(); return _positionSmoothingEnabled; }
        set { EnsureMutable(); if (_positionSmoothingEnabled == value) return; _positionSmoothingEnabled = value; }
    }

    /// <summary>Gets or sets the position smoothing coefficient.</summary>
    /// <value>5f initially.</value>
    /// <remarks>Nonnegative position smoothing coefficient. Negative finite assignments clamp to zero; each update uses speed times the last selected frame delta.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or outside the documented contract.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public float PositionSmoothingSpeed
    {
        get { CheckQuery(); return _positionSmoothingSpeed; }
        set { EnsureMutable(); Finite(value); value = Mathf.Max(0, value); if (_positionSmoothingSpeed == value) return; _positionSmoothingSpeed = value; }
    }

    /// <summary>Gets or sets whether view rotation interpolates toward the node angle.</summary>
    /// <value>false initially.</value>
    /// <remarks>Interpolates along the shortest angle on subsequent updates when IgnoreRotation is false.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public bool RotationSmoothingEnabled
    {
        get { CheckQuery(); return _rotationSmoothingEnabled; }
        set { EnsureMutable(); if (_rotationSmoothingEnabled == value) return; _rotationSmoothingEnabled = value; }
    }

    /// <summary>Gets or sets the rotation smoothing coefficient.</summary>
    /// <value>5f initially.</value>
    /// <remarks>Nonnegative angular smoothing coefficient. Negative finite assignments clamp to zero; weights are not capped at one.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or outside the documented contract.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public float RotationSmoothingSpeed
    {
        get { CheckQuery(); return _rotationSmoothingSpeed; }
        set { EnsureMutable(); Finite(value); value = Mathf.Max(0, value); if (_rotationSmoothingSpeed == value) return; _rotationSmoothingSpeed = value; }
    }

    /// <summary>Gets or sets whether horizontal movement waits for a drag margin.</summary>
    /// <value>false initially.</value>
    /// <remarks>Keeps the current target inside the horizontal drag margins on subsequent updates.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public bool DragHorizontalEnabled
    {
        get { CheckQuery(); return _dragHorizontalEnabled; }
        set { EnsureMutable(); if (_dragHorizontalEnabled == value) return; _dragHorizontalEnabled = value; }
    }

    /// <summary>Gets or sets whether vertical movement waits for a drag margin.</summary>
    /// <value>false initially.</value>
    /// <remarks>Keeps the current target inside the vertical drag margins on subsequent updates.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public bool DragVerticalEnabled
    {
        get { CheckQuery(); return _dragVerticalEnabled; }
        set { EnsureMutable(); if (_dragVerticalEnabled == value) return; _dragVerticalEnabled = value; }
    }

    /// <summary>Gets or sets the configured horizontal drag bias.</summary>
    /// <value>0f initially.</value>
    /// <remarks>Finite relative horizontal bias; negative uses the right margin, positive the left. Values outside minus one to one are accepted.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or outside the documented contract.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public float DragHorizontalOffset
    {
        get { CheckQuery(); return _dragHorizontalOffset; }
        set { EnsureMutable(); Finite(value); if (_dragHorizontalOffset == value) return; _dragHorizontalOffset = value; _horizontalOffsetChanged = true; UpdateScrollPreservingPosition(); }
    }

    /// <summary>Gets or sets the configured vertical drag bias.</summary>
    /// <value>0f initially.</value>
    /// <remarks>Finite relative vertical bias; negative uses the bottom margin, positive the top. Values outside minus one to one are accepted.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or outside the documented contract.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, during capture, or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public float DragVerticalOffset
    {
        get { CheckQuery(); return _dragVerticalOffset; }
        set { EnsureMutable(); Finite(value); if (_dragVerticalOffset == value) return; _dragVerticalOffset = value; _verticalOffsetChanged = true; UpdateScrollPreservingPosition(); }
    }

    /// <summary>Gets or sets the left scroll limit in canvas units.</summary>
    /// <value>-10,000,000 initially. Inverted limits are allowed.</value>
    /// <remarks>Offset is applied after limiting. Delegates to GetLimit and SetLimit.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is unavailable or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public int LimitLeft { get => GetLimit(Side.Left); set => SetLimit(Side.Left, value); }

    /// <summary>Gets or sets the left drag margin as a fraction of the half-screen extent.</summary>
    /// <value>0.2 initially. Any finite value is accepted; margins are applied on the next tracking update.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public float DragLeftMargin { get => GetDragMargin(Side.Left); set => SetDragMargin(Side.Left, value); }

    /// <summary>Gets or sets the top scroll limit in canvas units.</summary>
    /// <value>-10,000,000 initially. Inverted limits are allowed.</value>
    /// <remarks>Offset is applied after limiting. Delegates to GetLimit and SetLimit.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is unavailable or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public int LimitTop { get => GetLimit(Side.Top); set => SetLimit(Side.Top, value); }

    /// <summary>Gets or sets the top drag margin as a fraction of the half-screen extent.</summary>
    /// <value>0.2 initially. Any finite value is accepted; margins are applied on the next tracking update.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public float DragTopMargin { get => GetDragMargin(Side.Top); set => SetDragMargin(Side.Top, value); }

    /// <summary>Gets or sets the right scroll limit in canvas units.</summary>
    /// <value>10,000,000 initially. Inverted limits are allowed.</value>
    /// <remarks>Offset is applied after limiting. Delegates to GetLimit and SetLimit.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is unavailable or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public int LimitRight { get => GetLimit(Side.Right); set => SetLimit(Side.Right, value); }

    /// <summary>Gets or sets the right drag margin as a fraction of the half-screen extent.</summary>
    /// <value>0.2 initially. Any finite value is accepted; margins are applied on the next tracking update.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public float DragRightMargin { get => GetDragMargin(Side.Right); set => SetDragMargin(Side.Right, value); }

    /// <summary>Gets or sets the bottom scroll limit in canvas units.</summary>
    /// <value>10,000,000 initially. Inverted limits are allowed.</value>
    /// <remarks>Offset is applied after limiting. Delegates to GetLimit and SetLimit.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is unavailable or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public int LimitBottom { get => GetLimit(Side.Bottom); set => SetLimit(Side.Bottom, value); }

    /// <summary>Gets or sets the bottom drag margin as a fraction of the half-screen extent.</summary>
    /// <value>0.2 initially. Any finite value is accepted; margins are applied on the next tracking update.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public float DragBottomMargin { get => GetDragMargin(Side.Bottom); set => SetDragMargin(Side.Bottom, value); }

    /// <summary>Gets or sets the borrowed viewport used instead of the containing viewport.</summary>
    /// <value>Null initially. Null or a non-viewport Node selects the containing viewport.</value>
    /// <remarks>Runtime-only state, not stored by PackedScene. The target must be active in this tree when
    /// the camera is attached; another tree's or detached viewport is unsupported. Changing the target releases
    /// this camera from the old viewport and selects it on the new viewport only when no other camera is current.</remarks>
    /// <exception cref="NotSupportedException">The selected viewport is not active in this camera's tree.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable, or no containing viewport exists.</exception>
    /// <exception cref="ObjectDisposedException">The camera or assigned node is disposed.</exception>
    public Node? CustomViewport
    {
        get { CheckQuery(); return _customViewport; }
        set
        {
            EnsureMutable(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value));
            var custom = value as Viewport; if (ReferenceEquals(custom, _customViewport)) return;
            var target = Tree is null ? null : ResolveViewport(custom);
            var old = _viewport; _customViewport = custom;
            if (ReferenceEquals(old, target)) return;
            _viewport = null; old?.ReleaseCamera(this); _viewport = target; _first = true;
            if (_enabled && target?.GetCamera() is null && target is not null) MakeCurrent();
        }
    }

    /// <summary>Returns the specified scroll limit.</summary>
    /// <param name="side">Left, Top, Right or Bottom.</param>
    /// <returns>The configured integer limit, in canvas units.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined.</exception>
    /// <exception cref="InvalidOperationException">The attached camera is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public int GetLimit(Side side) { CheckQuery(); return _limits[SideIndex(side)]; }

    /// <summary>Changes a scroll limit and refreshes the current view without advancing stored position smoothing.</summary>
    /// <param name="side">Left, Top, Right or Bottom.</param>
    /// <param name="limit">Canvas units; inverted and undersized bounds are accepted and centered by tracking.</param>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public void SetLimit(Side side, int limit)
    {
        EnsureMutable(); var index = SideIndex(side); if (_limits[index] == limit) return;
        _limits[index] = limit; UpdateScrollPreservingPosition();
    }

    /// <summary>Returns the specified drag margin.</summary>
    /// <param name="side">Left, Top, Right or Bottom.</param>
    /// <returns>The fraction of the corresponding half-screen extent.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined.</exception>
    /// <exception cref="InvalidOperationException">The attached camera is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public float GetDragMargin(Side side) { CheckQuery(); return _dragMargins[SideIndex(side)]; }

    /// <summary>Changes a drag margin for subsequent tracking updates.</summary>
    /// <param name="side">Left, Top, Right or Bottom.</param>
    /// <param name="dragMargin">Any finite fraction; the zero-to-one editor range is not a runtime clamp.</param>
    /// <exception cref="ArgumentOutOfRangeException">The side is undefined or the value nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public void SetDragMargin(Side side, float dragMargin) { EnsureMutable(); var index = SideIndex(side); Finite(dragMargin, nameof(dragMargin)); _dragMargins[index] = dragMargin; }

    /// <summary>Reports whether this is the viewport's current camera.</summary>
    /// <returns>False while detached or when another camera is current.</returns>
    /// <exception cref="InvalidOperationException">The attached camera is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public bool IsCurrent() { CheckQuery(); return _viewport is { } viewport && ReferenceEquals(viewport.GetCamera(), this); }

    /// <summary>Selects this enabled camera and immediately updates the viewport canvas.</summary>
    /// <exception cref="InvalidOperationException">The camera is disabled, detached, off-owner, captured or tracking overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public void MakeCurrent()
    {
        EnsureMutable(); if (!_enabled || _viewport is null) throw new InvalidOperationException("An enabled camera in an active viewport is required.");
        _viewport.SetCurrentCamera(this); UpdateScroll();
    }

    /// <summary>Forces an immediate tracking update for the current camera.</summary>
    /// <remarks>Detached or noncurrent cameras do nothing. Uses the last selected frame delta, so repeated calls can advance smoothing again.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is unavailable or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public void ForceUpdateScroll() { EnsureMutable(); UpdateScroll(); }

    /// <summary>Updates scrolling, then resets stored position smoothing to its current destination.</summary>
    /// <remarks>The next scroll update uses the reset position. Rotation smoothing is unchanged.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is unavailable or tracking arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public void ResetSmoothing() { EnsureMutable(); UpdateScroll(); _smoothedPosition = _targetPosition; }

    /// <summary>Realigns the drag target to the global node position and configured drag offsets, then updates scrolling.</summary>
    /// <remarks>Requires viewport membership. Smoothing still applies; the node's own transform stays unchanged.
    /// An overflowing target or failed tracking update preserves the previous target and view.</remarks>
    /// <exception cref="InvalidOperationException">No viewport is active, mutation is unavailable or arithmetic overflows.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public void Align()
    {
        EnsureMutable(); if (_viewport is null) throw new InvalidOperationException("Alignment requires an active viewport.");
        var halfSize = _viewport.GetVisibleRect().Size * 0.5f;
        var target = GlobalPosition + (_anchorMode == AnchorModeEnum.DragCenter ? DragOffset(halfSize) : Vector2.Zero);
        if (!target.IsFinite()) throw new InvalidOperationException("Camera alignment overflowed finite coordinates.");
        var previous = _targetPosition; _targetPosition = target;
        try { UpdateScroll(); }
        catch { _targetPosition = previous; throw; }
    }

    /// <summary>Returns the cached target position before position smoothing and the final view offset.</summary>
    /// <returns>Canvas coordinates; zero before the first tracking update.</returns>
    /// <exception cref="InvalidOperationException">The attached camera is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public Vector2 GetTargetPosition() { CheckQuery(); return _targetPosition; }
    /// <summary>Returns the cached center of the visible screen in canvas coordinates.</summary>
    /// <returns>The last successfully calculated center, including limits, smoothing, zoom, rotation and offset.</returns>
    /// <exception cref="InvalidOperationException">The attached camera is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public Vector2 GetScreenCenterPosition() { CheckQuery(); return _screenCenter; }
    /// <summary>Returns the cached view rotation in radians.</summary>
    /// <returns>Zero while rotation is ignored; otherwise the last updated, possibly smoothed rotation.</returns>
    /// <exception cref="InvalidOperationException">The attached camera is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The camera is disposed.</exception>
    public float GetScreenRotation() { CheckQuery(); return _screenRotation; }

    /// <inheritdoc />
    /// <remarks>Appends stored camera configuration and the non-stored custom viewport reference.</remarks>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(CameraProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Camera) ? CreateCamera : base.CreateSceneInstanceFactory();
    /// <inheritdoc />
    /// <remarks>Inherited disposal releases viewport selection and owned children; finally drops borrowed viewport references.</remarks>
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally { if (disposing) { _viewport = null; _customViewport = null; } }
    }

    private static Node CreateCamera() => new Camera();
    private void CheckQuery() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private static int SideIndex(Side side) => side is >= Side.Left and <= Side.Bottom ? (int)side : throw new ArgumentOutOfRangeException(nameof(side));
    private static void Finite(float value, string name = "value") { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(name); }
    private static void Finite(Vector2 value) { if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); }

    private static readonly PropertyDescriptor[] CameraProperties =
    [
        new PropertyDescriptor<Camera, Vector2>(nameof(Offset), c => c.Offset, (c, v) => c.Offset = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Camera, Vector2>(nameof(Zoom), c => c.Zoom, (c, v) => c.Zoom = v, _ => Vector2.One, stored: true),
        new PropertyDescriptor<Camera, AnchorModeEnum>(nameof(AnchorMode), c => c.AnchorMode, (c, v) => c.AnchorMode = v, _ => AnchorModeEnum.DragCenter, stored: true),
        new PropertyDescriptor<Camera, bool>(nameof(IgnoreRotation), c => c.IgnoreRotation, (c, v) => c.IgnoreRotation = v, _ => true, stored: true),
        new PropertyDescriptor<Camera, bool>(nameof(Enabled), c => c.Enabled, (c, v) => c.Enabled = v, _ => true, stored: true),
        new PropertyDescriptor<Camera, CameraProcessCallback>(nameof(ProcessCallback), c => c.ProcessCallback, (c, v) => c.ProcessCallback = v, _ => CameraProcessCallback.Idle, stored: true),
        new PropertyDescriptor<Camera, bool>(nameof(LimitEnabled), c => c.LimitEnabled, (c, v) => c.LimitEnabled = v, _ => true, stored: true),
        new PropertyDescriptor<Camera, bool>(nameof(LimitSmoothed), c => c.LimitSmoothed, (c, v) => c.LimitSmoothed = v, _ => false, stored: true),
        new PropertyDescriptor<Camera, bool>(nameof(PositionSmoothingEnabled), c => c.PositionSmoothingEnabled, (c, v) => c.PositionSmoothingEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<Camera, float>(nameof(PositionSmoothingSpeed), c => c.PositionSmoothingSpeed, (c, v) => c.PositionSmoothingSpeed = v, _ => 5f, stored: true),
        new PropertyDescriptor<Camera, bool>(nameof(RotationSmoothingEnabled), c => c.RotationSmoothingEnabled, (c, v) => c.RotationSmoothingEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<Camera, float>(nameof(RotationSmoothingSpeed), c => c.RotationSmoothingSpeed, (c, v) => c.RotationSmoothingSpeed = v, _ => 5f, stored: true),
        new PropertyDescriptor<Camera, bool>(nameof(DragHorizontalEnabled), c => c.DragHorizontalEnabled, (c, v) => c.DragHorizontalEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<Camera, bool>(nameof(DragVerticalEnabled), c => c.DragVerticalEnabled, (c, v) => c.DragVerticalEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<Camera, float>(nameof(DragHorizontalOffset), c => c.DragHorizontalOffset, (c, v) => c.DragHorizontalOffset = v, _ => 0f, stored: true),
        new PropertyDescriptor<Camera, float>(nameof(DragVerticalOffset), c => c.DragVerticalOffset, (c, v) => c.DragVerticalOffset = v, _ => 0f, stored: true),
        new PropertyDescriptor<Camera, int>(nameof(LimitLeft), c => c.LimitLeft, (c, v) => c.LimitLeft = v, _ => -10_000_000, stored: true),
        new PropertyDescriptor<Camera, float>(nameof(DragLeftMargin), c => c.DragLeftMargin, (c, v) => c.DragLeftMargin = v, _ => 0.2f, stored: true),
        new PropertyDescriptor<Camera, int>(nameof(LimitTop), c => c.LimitTop, (c, v) => c.LimitTop = v, _ => -10_000_000, stored: true),
        new PropertyDescriptor<Camera, float>(nameof(DragTopMargin), c => c.DragTopMargin, (c, v) => c.DragTopMargin = v, _ => 0.2f, stored: true),
        new PropertyDescriptor<Camera, int>(nameof(LimitRight), c => c.LimitRight, (c, v) => c.LimitRight = v, _ => 10_000_000, stored: true),
        new PropertyDescriptor<Camera, float>(nameof(DragRightMargin), c => c.DragRightMargin, (c, v) => c.DragRightMargin = v, _ => 0.2f, stored: true),
        new PropertyDescriptor<Camera, int>(nameof(LimitBottom), c => c.LimitBottom, (c, v) => c.LimitBottom = v, _ => 10_000_000, stored: true),
        new PropertyDescriptor<Camera, float>(nameof(DragBottomMargin), c => c.DragBottomMargin, (c, v) => c.DragBottomMargin = v, _ => 0.2f, stored: true),
        new PropertyDescriptor<Camera, Node?>(nameof(CustomViewport), c => c.CustomViewport, (c, v) => c.CustomViewport = v, _ => null),
    ];
}
