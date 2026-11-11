namespace Electron2D;

public partial class Camera
{
    private bool _editorDrawScreen = true;
    private bool _editorDrawLimits;
    private bool _editorDrawDragMargin;
    private EditorOverlay _editorOverlay;

    /// <summary>Gets or sets whether the edited scene displays this camera's screen outline.</summary>
    /// <value>True initially. Stored in packed scenes.</value>
    /// <remarks>The outline uses the target viewport's logical size and the authored camera pose.</remarks>
    public bool EditorDrawScreen
    {
        get { CheckQuery(); return _editorDrawScreen; }
        set { EnsureMutable(); _editorDrawScreen = value; }
    }

    /// <summary>Gets or sets whether the edited scene displays this camera's enabled scroll limits.</summary>
    /// <value>False initially. Stored in packed scenes.</value>
    public bool EditorDrawLimits
    {
        get { CheckQuery(); return _editorDrawLimits; }
        set { EnsureMutable(); _editorDrawLimits = value; }
    }

    /// <summary>Gets or sets whether the edited scene displays this camera's configured drag margins.</summary>
    /// <value>False initially. Stored in packed scenes.</value>
    /// <remarks>The outline shows all four configured margins independently of the two tracking toggles.</remarks>
    public bool EditorDrawDragMargin
    {
        get { CheckQuery(); return _editorDrawDragMargin; }
        set { EnsureMutable(); _editorDrawDragMargin = value; }
    }

    private readonly record struct EditorOverlay(
        Rect2? Screen, Rect2? Margins, Rect2? Limits, Transform ScreenToLocal, Transform WorldToLocal, float Width);

    internal void RefreshEditorOverlay()
    {
        var overlay = CaptureEditorOverlay();
        if (_editorOverlay == overlay) return;
        _editorOverlay = overlay;
        QueueRedraw();
    }

    private EditorOverlay CaptureEditorOverlay()
    {
        if (!Engine.IsEditorHint || Tree?.IsEditedSceneMember(this) != true ||
            _viewport is not { IsDisposed: false } viewport ||
            !(_editorDrawScreen || _editorDrawDragMargin || _editorDrawLimits && _limitEnabled))
            return default;

        var global = GetGlobalTransform();
        var determinant = global.Determinant();
        if (!global.IsFinite() || !float.IsFinite(determinant) || determinant == 0) return default;
        var worldToLocal = global.AffineInverse();
        if (!worldToLocal.IsFinite()) return default;

        Rect2? limits = null;
        if (_editorDrawLimits && _limitEnabled)
        {
            var first = new Vector2(_limits[0], _limits[1]);
            var last = new Vector2(_limits[2], _limits[3]);
            limits = new Rect2(first, last - first);
        }

        Rect2? screen = null, margins = null;
        var screenToLocal = Transform.Identity;
        var size = viewport.GetVisibleRect().Size;
        if ((_editorDrawScreen || _editorDrawDragMargin) && size.X > 0 && size.Y > 0)
        {
            var half = size * .5f;
            var centered = _anchorMode == AnchorMode.DragCenter;
            var scale = Vector2.One / _zoom;
            var anchor = centered ? half * scale : Vector2.Zero;
            var position = global.Origin;
            if (centered) position += DragOffset(half);
            if (_limitEnabled && _limitSmoothed) position = LimitOrigin(position - anchor, size * scale) + anchor;
            var angle = _ignoreRotation ? 0f : GlobalRotation;
            var pose = CameraPose(position, angle, anchor, scale, _limitEnabled && (!_positionSmoothingEnabled || !_limitSmoothed));
            screenToLocal = worldToLocal * pose;
            if (screenToLocal.IsFinite())
            {
                if (_editorDrawScreen) screen = new Rect2(Vector2.Zero, size);
                if (_editorDrawDragMargin)
                {
                    var first = half - half * new Vector2(_dragMargins[0], _dragMargins[1]);
                    var last = half + half * new Vector2(_dragMargins[2], _dragMargins[3]);
                    var rect = new Rect2(first, last - first);
                    if (rect.IsFinite() && last.IsFinite()) margins = rect;
                }
            }
            else screenToLocal = Transform.Identity;
        }

        return new(screen, margins, limits, screenToLocal, worldToLocal, viewport.IsCurrentCamera(this) ? 3f : -1f);
    }

    private void DrawEditorOverlay()
    {
        var overlay = _editorOverlay;
        if (overlay.Screen is { } screen)
            DrawEditorOutline(screen, overlay.ScreenToLocal, new(1, .4f, 1, .63f), overlay.Width);
        if (overlay.Limits is { } limits)
            DrawEditorOutline(limits, overlay.WorldToLocal, new(1, 1, .25f, .63f), overlay.Width);
        if (overlay.Margins is { } margins)
            DrawEditorOutline(margins, overlay.ScreenToLocal, new(.25f, 1, 1, .63f), overlay.Width);
    }

    private void DrawEditorOutline(Rect2 rect, Transform projection, Color color, float width)
    {
        var start = rect.Position;
        var end = rect.End;
        Span<Vector2> corners =
        [
            projection * start,
            projection * new Vector2(end.X, start.Y),
            projection * end,
            projection * new Vector2(start.X, end.Y)
        ];
        foreach (var corner in corners) if (!corner.IsFinite()) return;
        for (var i = 0; i < corners.Length; i++) DrawLine(corners[i], corners[(i + 1) % corners.Length], color, width);
    }
}
