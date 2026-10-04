namespace Electron2D;

public abstract partial class Viewport
{
    private Transform _canvasTransform = Transform.Identity;
    private Transform _globalCanvasTransform = Transform.Identity;
    private static readonly PropertyDescriptor[] CanvasTransformProperties =
    [
        new PropertyDescriptor<Viewport, Transform>(nameof(CanvasTransform), n => n.CanvasTransform, (n, v) => n.CanvasTransform = v, _ => Transform.Identity),
        new PropertyDescriptor<Viewport, Transform>(nameof(GlobalCanvasTransform), n => n.GlobalCanvasTransform, (n, v) => n.GlobalCanvasTransform = v, _ => Transform.Identity),
    ];

    /// <summary>Gets or sets the transform from the default canvas to viewport coordinates.</summary>
    /// <value>Identity initially. Finite singular transforms are allowed for rendering.</value>
    /// <remarks>Applies before GlobalCanvasTransform. Changes affect retained drawing on the next submission,
    /// without changing node transforms or emitting their notifications. A current physics-interpolated camera may
    /// present a historical canvas transform while this property retains the latest logical value. Runtime state is
    /// not stored by PackedScene.</remarks>
    /// <exception cref="ArgumentException">The transform is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">An attached viewport is accessed off-owner, or changed during capture.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public Transform CanvasTransform
    {
        get { CheckTransformQuery(); return _canvasTransform; }
        set
        {
            EnsureMutable();
            if (!value.IsFinite()) throw new ArgumentException("Canvas transform must be finite.", nameof(value));
            _canvasTransform = value;
            if (Tree is { IsPhysicsInterpolationActive: true, IsInPhysicsFrame: false }) ResetCanvasInterpolationSnapshot();
        }
    }

    /// <summary>Gets or sets the outer transform from viewport coordinates to the window's client coordinates.</summary>
    /// <value>Identity initially. Finite singular transforms are allowed.</value>
    /// <remarks>Applies after CanvasTransform and is removed from incoming window coordinates before input callbacks.
    /// Rendering changes need no redraw; logical node state remains unchanged. Runtime state is not stored by PackedScene.</remarks>
    /// <exception cref="ArgumentException">The transform is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">An attached viewport is accessed off-owner, or changed during capture.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public Transform GlobalCanvasTransform
    {
        get { CheckTransformQuery(); return _globalCanvasTransform; }
        set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Global canvas transform must be finite.", nameof(value)); _globalCanvasTransform = value; }
    }

    /// <summary>Returns the transform from viewport coordinates to the containing client area.</summary>
    /// <returns>StretchTransform followed by GlobalCanvasTransform; native root content stretch is identity.</returns>
    /// <remarks>Does not include CanvasTransform, native desktop placement or framebuffer pixel density.</remarks>
    /// <exception cref="InvalidOperationException">An attached viewport is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public Transform GetFinalTransform() => StretchTransform * GlobalCanvasTransform;

    /// <summary>Returns the transform from viewport coordinates to its containing window.</summary>
    /// <returns>GetFinalTransform for a root/standalone viewport, or the complete embedded container chain to the containing client area.</returns>
    /// <remarks>The native root's desktop position is not part of this query. A standalone offscreen viewport returns its final transform without a native desktop position; embedded transforms include container placement and StretchShrink.</remarks>
    /// <exception cref="InvalidOperationException">An attached viewport is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public Transform GetScreenTransform()
    {
        CheckTransformQuery();
        return Parent is SubViewportContainer container && container.GetViewport() is { } parent
            ? parent.GetScreenTransform() * container.GetGlobalTransformWithCanvas() * container.ViewportScale * GetFinalTransform()
            : GetFinalTransform();
    }

    /// <summary>Returns the current pointer position in viewport coordinates.</summary>
    /// <returns>The native client pointer transformed by the inverse final transform; zero when it is singular.</returns>
    /// <remarks>Requires a containing native Window. Does not alter input polling or use the last event supplied to PushInput.
    /// Fractional client coordinates are retained. CanvasTransform is not removed by this query.</remarks>
    /// <exception cref="InvalidOperationException">No containing native window is active, or the caller is not its owner.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public Vector2 GetMousePosition()
    {
        CheckTransformQuery();
        var window = GetWindow() ?? throw new InvalidOperationException("Pointer queries require an active containing native Window.");
        var position = window.GetClientMousePosition();
        var transform = GetScreenTransform();
        return transform.Determinant() == 0 ? Vector2.Zero : transform.AffineInverse() * position;
    }

    /// <summary>Requests a pointer move to a position in viewport coordinates.</summary>
    /// <param name="position">Finite viewport coordinates, transformed to the window client area before the request.</param>
    /// <remarks>The final transform is applied; CanvasTransform is not. The native request truncates fractional client
    /// coordinates to integer units. Platform input policy may prevent movement even when the request is supported.</remarks>
    /// <exception cref="ArgumentException">The input or transformed position is nonfinite or outside native integer coordinates.</exception>
    /// <exception cref="NotSupportedException">The active backend does not support pointer warping.</exception>
    /// <exception cref="InvalidOperationException">No containing native window is active, or the caller is not its owner.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public void WarpMouse(Vector2 position)
    {
        EnsureMutable();
        if (!position.IsFinite()) throw new ArgumentException("Pointer coordinates must be finite.", nameof(position));
        if (this is not Window window) throw new InvalidOperationException("Pointer warping requires an active native Window.");
        position = GetScreenTransform() * position;
        if (!position.IsFinite() || (double)position.X < int.MinValue || (double)position.X > int.MaxValue || (double)position.Y < int.MinValue || (double)position.Y > int.MaxValue)
            throw new ArgumentException("Transformed pointer coordinates exceed native limits.", nameof(position));
        window.WarpClientMouse(new((int)position.X, (int)position.Y));
    }

    internal InputEvent MakeViewportInputLocal(InputEvent inputEvent)
    {
        var result = inputEvent.XformedBy(GetFinalTransform().AffineInverse());
        try
        {
            if (result is InputEventMouse mouse) mouse.GlobalPosition = mouse.Position;
            return result;
        }
        catch { if (!ReferenceEquals(result, inputEvent)) result.Dispose(); throw; }
    }

    private void CheckTransformQuery() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
}
