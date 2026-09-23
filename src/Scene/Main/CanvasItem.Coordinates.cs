namespace Electron2D;

public abstract partial class CanvasItem
{
    /// <summary>Returns the containing viewport's visible rectangle.</summary>
    /// <returns>A rectangle in viewport coordinates, independent of canvas and node transforms.</returns>
    /// <exception cref="InvalidOperationException">The item has no active viewport or is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Rect GetViewportRect() => RequireCanvasViewport().GetVisibleRect();

    /// <summary>Returns the transform from this item's canvas to viewport coordinates.</summary>
    /// <returns>The containing viewport's CanvasTransform.</returns>
    /// <remarks>Requires tree membership. Independent canvas layers are not implemented yet.</remarks>
    /// <exception cref="InvalidOperationException">The item has no active viewport or is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Transform GetCanvasTransform() => RequireCanvasViewport().CanvasTransform;

    /// <summary>Returns this item's local-to-viewport transform, including its canvas.</summary>
    /// <returns>CanvasTransform times GetGlobalTransform while attached; otherwise the logical global transform.</returns>
    /// <remarks>GlobalCanvasTransform and pixel snapping are not included. Neutral parents and TopLevel still break only the node-transform chain.</remarks>
    /// <exception cref="InvalidOperationException">An attached item is queried off-owner or has no viewport.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Transform GetGlobalTransformWithCanvas()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        return IsInsideTree ? GetCanvasTransform() * GetGlobalTransform() : GetGlobalTransform();
    }

    /// <summary>Returns the transform from this item's canvas to the containing window's client coordinates.</summary>
    /// <returns>The viewport final transform times its CanvasTransform.</returns>
    /// <remarks>Does not include this item's own transform, native desktop placement or framebuffer pixel density.</remarks>
    /// <exception cref="InvalidOperationException">The item has no active viewport or is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Transform GetViewportTransform() { var viewport = RequireCanvasViewport(); return viewport.GetFinalTransform() * GetCanvasTransform(); }

    /// <summary>Returns this item's local-to-screen transform, including native window placement.</summary>
    /// <returns>The native client origin, viewport final transform and local-to-canvas transform composed in order.</returns>
    /// <remarks>Native desktop placement is unavailable on Wayland and fails explicitly. No fabricated screen origin is returned.</remarks>
    /// <exception cref="NotSupportedException">The native backend cannot report the window's desktop position.</exception>
    /// <exception cref="InvalidOperationException">The item has no active viewport or is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Transform GetScreenTransform()
    {
        var viewport = RequireCanvasViewport();
        var origin = viewport is Window window ? (Vector2)window.Position : Vector2.Zero;
        return new Transform(0, origin) * viewport.GetScreenTransform() * GetGlobalTransformWithCanvas();
    }

    /// <summary>Converts a point in viewport coordinates to this item's local coordinates.</summary>
    /// <param name="viewportPoint">Finite viewport coordinates, such as a localized input position.</param>
    /// <returns>The inverse canvas-and-node transform applied to the point.</returns>
    /// <remarks>Incoming coordinates must already exclude the viewport final transform. Snapping does not change logical queries.</remarks>
    /// <exception cref="ArgumentException">The supplied point is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The item has no active viewport, is queried off-owner or the composed transform is singular.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Vector2 MakeCanvasPositionLocal(Vector2 viewportPoint)
    {
        _ = RequireCanvasViewport();
        if (!viewportPoint.IsFinite()) throw new ArgumentException("Viewport coordinates must be finite.", nameof(viewportPoint));
        return GetGlobalTransformWithCanvas().AffineInverse() * viewportPoint;
    }

    /// <summary>Converts a viewport input event to this item's local coordinates.</summary>
    /// <param name="inputEvent">A live borrowed event already in viewport coordinates.</param>
    /// <returns>A caller-owned copy for positional events, or the same borrowed object for non-positional events.</returns>
    /// <remarks>Uses InputEvent.XformedBy. Mouse GlobalPosition and screen-space motion stay unchanged. Dispose the result
    /// only if it differs from the input; the item does not take ownership of either event.</remarks>
    /// <exception cref="ArgumentNullException">The event is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Transformed event coordinates overflow finite values.</exception>
    /// <exception cref="InvalidOperationException">The item has no active viewport, is queried off-owner or its transform is singular.</exception>
    /// <exception cref="ObjectDisposedException">The item or event is disposed.</exception>
    public InputEvent MakeInputLocal(InputEvent inputEvent)
    {
        _ = RequireCanvasViewport(); ArgumentNullException.ThrowIfNull(inputEvent); inputEvent.EnsureUsable();
        return inputEvent.XformedBy(GetGlobalTransformWithCanvas().AffineInverse());
    }

    /// <summary>Returns the current pointer in this item's canvas coordinates.</summary>
    /// <returns>The viewport pointer transformed by the inverse canvas transform.</returns>
    /// <exception cref="InvalidOperationException">There is no active native viewport, the caller is off-owner or the canvas transform is singular.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Vector2 GetGlobalMousePosition() => GetCanvasTransform().AffineInverse() * RequireCanvasViewport().GetMousePosition();

    /// <summary>Returns the current pointer in this item's local coordinates.</summary>
    /// <returns>The inverse logical global transform applied to GetGlobalMousePosition.</returns>
    /// <exception cref="InvalidOperationException">There is no active native viewport, the caller is off-owner or an inverse transform is singular.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Vector2 GetLocalMousePosition() => GetGlobalTransform().AffineInverse() * GetGlobalMousePosition();

    private Viewport RequireCanvasViewport()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        return IsInsideTree && GetViewport() is { } viewport ? viewport : throw new InvalidOperationException("Canvas coordinates require an active viewport.");
    }
}
