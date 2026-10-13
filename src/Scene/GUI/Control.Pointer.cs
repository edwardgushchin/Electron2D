namespace Electron2D;

public partial class Control
{
    /// <summary>Transfers an existing pointer-button capture to this control at the deferred boundary.</summary>
    /// <remarks>The last request in this viewport wins. No active capture means no change. The previous
    /// control receives internal releases for held left/right/middle buttons; this control receives corresponding
    /// internal presses in the next deferred batch. Keyboard focus and global input state are unaffected.</remarks>
    /// <exception cref="InvalidOperationException">The control is detached or accessed off its scene owner.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public void GrabClickFocus()
    {
        EnsureMutable();
        var viewport = GetViewport();
        if (!IsInsideTree || viewport is null) throw new InvalidOperationException("Click capture requires an active viewport.");
        Tree!.GrabGUIClickFocus(viewport, this);
    }

    /// <summary>Returns the desktop position of this control's local origin.</summary>
    /// <returns>The origin of the complete canvas, viewport and window screen transform.</returns>
    /// <exception cref="NotSupportedException">The native backend cannot report desktop window placement.</exception>
    /// <exception cref="InvalidOperationException">There is no active viewport or the caller is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public Vector2 GetScreenPosition() => GetScreenTransform().Origin;

    /// <summary>Requests a native pointer move to a finite control-local position.</summary>
    /// <param name="position">The position transformed through the canvas and containing viewport.</param>
    /// <exception cref="ArgumentException">The input/transformed position is nonfinite or outside native limits.</exception>
    /// <exception cref="InvalidOperationException">No active containing window exists or the caller is off-owner.</exception>
    /// <exception cref="NotSupportedException">The native backend does not support pointer warping.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public void WarpMouse(Vector2 position)
    {
        EnsureMutable();
        if (!position.IsFinite()) throw new ArgumentException("Pointer coordinates must be finite.", nameof(position));
        if (!IsInsideTree || GetViewport() is not { } viewport) throw new InvalidOperationException("Pointer warping requires an active viewport.");
        viewport.WarpMouse(GetGlobalTransformWithCanvas() * position);
    }
}
