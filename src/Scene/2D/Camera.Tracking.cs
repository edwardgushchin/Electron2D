namespace Electron2D;

public partial class Camera
{
    internal bool CanTrack(Viewport viewport) => _enabled && ReferenceEquals(_viewport, viewport) && !IsDisposed;

    internal override void OnTreeMembershipChanged(bool entering)
    {
        if (!entering)
        {
            var old = _viewport; _viewport = null;
            try { old?.ReleaseCamera(this); }
            finally { base.OnTreeMembershipChanged(false); }
            return;
        }
        base.OnTreeMembershipChanged(true);
        _viewport = ResolveViewport(_customViewport); _first = true; UpdateProcessing();
        if (_enabled && _viewport.GetCamera() is null) MakeCurrent();
    }

    private Viewport ResolveViewport(Viewport? custom)
    {
        var target = custom ?? GetViewport() ?? throw new InvalidOperationException("A camera requires an active viewport.");
        if (target.IsDisposed) throw new ObjectDisposedException(nameof(CustomViewport));
        if (!ReferenceEquals(target.Tree, Tree)) throw new NotSupportedException("A camera target must be active in the same scene tree.");
        return target;
    }

    /// <inheritdoc />
    /// <remarks>Updates the current view on the selected internal frame lane and unsmoothed transform notifications.
    /// Actual membership, not manually delivered enter/exit notifications, owns viewport registration.</remarks>
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (IsDisposed || _viewport is null) return;
        if (what == NotificationInternalProcess && _processCallback == CameraProcessCallback.Idle ||
            what == NotificationInternalPhysicsProcess && _processCallback == CameraProcessCallback.Physics ||
            what == NotificationTransformChanged && !_positionSmoothingEnabled)
            UpdateScroll();
    }

    private void UpdateProcessing() => SetInternalProcessing(_processCallback == CameraProcessCallback.Idle, _processCallback == CameraProcessCallback.Physics);
    private void UpdateScrollPreservingPosition()
    {
        var previous = _smoothedPosition;
        try { UpdateScroll(); }
        finally { _smoothedPosition = previous; }
    }

    private Vector2 DragOffset(Vector2 halfSize) => new(
        halfSize.X * _dragHorizontalOffset * _dragMargins[_dragHorizontalOffset < 0 ? (int)Side.Right : (int)Side.Left],
        halfSize.Y * _dragVerticalOffset * _dragMargins[_dragVerticalOffset < 0 ? (int)Side.Bottom : (int)Side.Top]);

    private void UpdateScroll()
    {
        var viewport = _viewport;
        if (viewport is null || !ReferenceEquals(viewport.GetCamera(), this)) return;
        var size = viewport.GetVisibleRect().Size; var halfSize = size * 0.5f; var scale = Vector2.One / _zoom;
        var position = GlobalPosition; var target = _targetPosition; var smoothed = _smoothedPosition;
        var angle = _screenRotation; var delta = (float)(_processCallback == CameraProcessCallback.Physics ? PhysicsProcessDeltaTime : ProcessDeltaTime);
        var horizontalChanged = _horizontalOffsetChanged; var verticalChanged = _verticalOffsetChanged;
        if (_first) target = smoothed = position;
        else
        {
            if (_anchorMode == AnchorModeEnum.FixedTopLeft) target = position;
            else
            {
                var bias = DragOffset(halfSize);
                target.X = _dragHorizontalEnabled && !horizontalChanged
                    ? MathF.Max(MathF.Min(target.X, position.X + halfSize.X * scale.X * _dragMargins[0]), position.X - halfSize.X * scale.X * _dragMargins[2])
                    : position.X + bias.X;
                target.Y = _dragVerticalEnabled && !verticalChanged
                    ? MathF.Max(MathF.Min(target.Y, position.Y + halfSize.Y * scale.Y * _dragMargins[1]), position.Y - halfSize.Y * scale.Y * _dragMargins[3])
                    : position.Y + bias.Y;
                horizontalChanged = verticalChanged = false;
            }
            if (_limitEnabled && _limitSmoothed)
            {
                var anchor = _anchorMode == AnchorModeEnum.DragCenter ? halfSize * scale : Vector2.Zero;
                target = LimitOrigin(target - anchor, size * scale) + anchor;
            }
            smoothed = _positionSmoothingEnabled ? smoothed + (target - smoothed) * (_positionSmoothingSpeed * delta) : target;
        }
        var screenOffset = _anchorMode == AnchorModeEnum.DragCenter ? halfSize * scale : Vector2.Zero;
        if (!_ignoreRotation)
        {
            angle = _rotationSmoothingEnabled ? Mathf.LerpAngle(angle, GlobalRotation, _rotationSmoothingSpeed * delta) : GlobalRotation;
            screenOffset = screenOffset.Rotated(angle);
        }
        var origin = smoothed - screenOffset;
        if (_limitEnabled && (!_positionSmoothingEnabled || !_limitSmoothed)) origin = LimitOrigin(origin, screenOffset * 2);
        origin += _offset;
        var pose = new Transform(0, scale, 0, origin);
        if (!_ignoreRotation) pose = new Transform(angle, pose.Scale, 0, origin);
        var center = pose * halfSize;
        if (!target.IsFinite() || !smoothed.IsFinite() || !float.IsFinite(angle) || !pose.IsFinite() || !center.IsFinite())
            throw new InvalidOperationException("Camera tracking overflowed finite coordinates.");
        var canvas = pose.AffineInverse();
        if (!canvas.IsFinite()) throw new InvalidOperationException("Camera inverse transform overflowed finite coordinates.");
        viewport.CanvasTransform = canvas;
        _targetPosition = target; _smoothedPosition = smoothed; _screenRotation = angle; _screenCenter = center;
        _horizontalOffsetChanged = horizontalChanged; _verticalOffsetChanged = verticalChanged; _first = false;
    }

    private Vector2 LimitOrigin(Vector2 origin, Vector2 extent) => new(
        LimitAxis(origin.X, extent.X, _limits[0], _limits[2]), LimitAxis(origin.Y, extent.Y, _limits[1], _limits[3]));

    private static float LimitAxis(float origin, float extent, int lower, int upper)
    {
        if (lower > (double)upper - extent) return (float)(((double)lower + upper - extent) / 2);
        if (origin < lower) return lower;
        if ((double)origin + extent > upper) return upper - extent;
        return origin;
    }
}
