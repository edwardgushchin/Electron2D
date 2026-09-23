namespace Electron2D;

/// <summary>Provides the root window's client rectangle and scene input boundary.</summary>
/// <remarks>Only a root <see cref="Window"/> is currently supported. Offscreen render targets, content scaling,
/// and embedded viewports are not implemented. Canvas transforms, sampling and pixel-snapping policies apply to the root renderer. Window input is localized through the inverse final transform.</remarks>
public abstract partial class Viewport : Node
{
    private protected Viewport() { }

    /// <summary>Returns the client rectangle in viewport coordinates.</summary>
    /// <returns>A zero-origin rectangle in client units, independent of desktop and node position.</returns>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public abstract Rect GetVisibleRect();

    /// <summary>Occurs after the client size changes, before subsequent frame callbacks.</summary>
    /// <remarks>Subscribers run synchronously on the scene owner thread. Desktop movement does not notify.</remarks>
    public event Action? SizeChanged;

    /// <summary>Marks the scene input event currently being dispatched as handled.</summary>
    /// <remarks>Stops later scene input callbacks without changing the global polling state.</remarks>
    /// <exception cref="InvalidOperationException">The viewport is detached, no input is being dispatched, or the caller is not the owner.</exception>
    /// <exception cref="ObjectDisposedException">The viewport or scene tree is disposed.</exception>
    public void SetInputAsHandled() => GetInputTree().SetInputAsHandled();

    /// <summary>Reports whether the current scene input event has been handled.</summary>
    /// <returns>The active event's handled state; the state resets for each event.</returns>
    /// <exception cref="InvalidOperationException">The viewport is detached, no input is being dispatched, or the caller is not the owner.</exception>
    /// <exception cref="ObjectDisposedException">The viewport or scene tree is disposed.</exception>
    public bool IsInputHandled() => GetInputTree().IsInputHandled();

    /// <summary>Delivers a borrowed input event directly to this viewport's scene.</summary>
    /// <param name="inputEvent">A live event, retained and disposed by the caller.</param>
    /// <param name="inLocalCoordinates">True for viewport coordinates; false (default) removes the final transform from window-client coordinates.</param>
    /// <remarks>Does not update global Input state or emulate pointer devices. For window coordinates, positional events
    /// are copied and disposed after synchronous dispatch; mouse GlobalPosition becomes its localized Position.
    /// Local input and non-positional events remain borrowed. Nested dispatch is rejected. CanvasTransform is removed
    /// separately by CanvasItem.MakeInputLocal when translating to an item's local coordinates.</remarks>
    /// <exception cref="ArgumentNullException">The event is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Transformed event coordinates overflow finite values.</exception>
    /// <exception cref="InvalidOperationException">The viewport is detached, accessed off-thread, cannot accept input or needs a singular inverse transform.</exception>
    /// <exception cref="ObjectDisposedException">The event, viewport, or tree is disposed.</exception>
    /// <exception cref="AggregateException">Scene callbacks fail after dispatch.</exception>
    public void PushInput(InputEvent inputEvent, bool inLocalCoordinates = false)
    {
        var tree = GetInputTree(); tree.DispatchViewportInput(this, inputEvent, inLocalCoordinates);
    }

    internal void NotifySizeChanged() => SizeChanged?.Invoke();

    private SceneTree GetInputTree()
    {
        ThrowIfDisposed();
        return Tree ?? throw new InvalidOperationException("Input requires an active viewport.");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            SizeChanged = null;
        base.Dispose(disposing);
    }
}
