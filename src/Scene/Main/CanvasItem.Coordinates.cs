namespace Electron2D;

public abstract partial class CanvasItem
{
    /// <summary>Returns the containing viewport's visible rectangle.</summary>
    /// <returns>A rectangle in viewport coordinates, independent of canvas and node transforms.</returns>
    /// <exception cref="InvalidOperationException">The item has no active viewport or is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Rect2 GetViewportRect() => RequireCanvasViewport().GetVisibleRect();

    /// <summary>Returns the transform from this item's canvas to viewport coordinates.</summary>
    /// <returns>The associated CanvasLayer.GetFinalTransform, or the containing viewport's CanvasTransform.</returns>
    /// <remarks>Requires tree membership. Neutral nodes and TopLevel preserve the enclosing canvas layer.</remarks>
    /// <exception cref="InvalidOperationException">The item has no active viewport or is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Transform GetCanvasTransform() { var viewport = RequireCanvasViewport(); return _canvasLayer?.GetFinalTransform() ?? viewport.CanvasTransform; }

    /// <summary>Returns the canvas layer associated with this item's actual canvas membership.</summary>
    /// <returns>A borrowed CanvasLayer, or null for the default canvas and detached items.</returns>
    /// <remarks>Search stops at the containing viewport. Neutral parents and TopLevel do not remove layer membership.
    /// Logical membership may be read by the scene owner or an active processing group with caller coordination.</remarks>
    /// <exception cref="InvalidOperationException">The attached item is queried outside the owner or active processing groups.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public CanvasLayer? GetCanvasLayerNode() { ThrowIfDisposed(); Tree?.EnsureProcessingReadAccess(); return _canvasLayer; }

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

    /// <summary>Returns the transform from this item's canvas to its owning viewport's output coordinates.</summary>
    /// <returns>The viewport final transform times this item's canvas transform.</returns>
    /// <remarks>Does not include this item's own transform, ancestor viewport/container placement, native desktop placement or framebuffer pixel density.</remarks>
    /// <exception cref="InvalidOperationException">The item has no active viewport or is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Transform GetViewportTransform() { var viewport = RequireCanvasViewport(); return viewport.GetFinalTransform() * GetCanvasTransform(); }

    /// <summary>Returns this item's local-to-screen transform in its popup host's coordinate space.</summary>
    /// <returns>The viewport popup-base transform followed by this item's canvas and logical global transforms.</returns>
    /// <remarks>A viewport that embeds windows stops the popup-base chain before its own final transform. Other windows
    /// include their position and final transform through the embedder; offscreen containers include their item, shrink
    /// and child final transforms. Native desktop placement is included only when the chain reaches a native window
    /// without an embedding boundary. Wayland rejects that desktop lookup explicitly. Viewport.GetScreenTransform
    /// instead projects through the complete containing-client chain.</remarks>
    /// <exception cref="NotSupportedException">Required native desktop placement is unavailable.</exception>
    /// <exception cref="InvalidOperationException">The item has no active viewport or is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Transform GetScreenTransform()
    {
        return RequireCanvasViewport().GetPopupBaseTransform() * GetGlobalTransformWithCanvas();
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
    /// <remarks>Uses the owning viewport's native, emulated or isolated-section pointer policy.</remarks>
    /// <exception cref="InvalidOperationException">There is no active viewport, the caller is off-owner or the canvas transform is singular.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Vector2 GetGlobalMousePosition() => GetCanvasTransform().AffineInverse() * RequireCanvasViewport().GetMousePosition();

    /// <summary>Returns the current pointer in this item's local coordinates.</summary>
    /// <returns>The inverse logical global transform applied to GetGlobalMousePosition.</returns>
    /// <remarks>Uses the owning viewport's native, emulated or isolated-section pointer policy.</remarks>
    /// <exception cref="InvalidOperationException">There is no active viewport, the caller is off-owner or an inverse transform is singular.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public Vector2 GetLocalMousePosition() => GetGlobalTransform().AffineInverse() * GetGlobalMousePosition();

    private Viewport RequireCanvasViewport()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        return IsInsideTree && GetViewport() is { } viewport ? viewport : throw new InvalidOperationException("Canvas coordinates require an active viewport.");
    }
}
