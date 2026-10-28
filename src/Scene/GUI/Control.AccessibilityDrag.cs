namespace Electron2D;

public partial class Control
{
    internal static readonly Vector2 AccessibilityDragPosition = new(float.PositiveInfinity, float.PositiveInfinity);
    /// <summary>Starts a drag from this control without pointer input.</summary>
    /// <remarks>Drag hooks receive positive-infinite coordinates to identify a non-positional request. Preview preparation and typed payload lifetime follow ordinary GUI dragging.</remarks>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception><exception cref="InvalidOperationException">The control is detached, access is off-owner, capture owns it or a drag is already active.</exception>
    public void AccessibilityDrag()
    {
        EnsureMutable(); (Tree ?? throw new InvalidOperationException("Accessible dragging requires an attached control.")).BeginAccessibilityDrag(this);
    }
    /// <summary>Attempts the active drag on this control and its eligible ancestors, then ends the transaction.</summary>
    /// <remarks>Drop hooks receive positive-infinite coordinates; built-in text controls use their current caret. Rejected or failing drops still release preview/payload ownership.</remarks>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception><exception cref="InvalidOperationException">The control is detached, access is off-owner, capture owns it or no drag is active in its section.</exception>
    /// <exception cref="AggregateException">Drop or finalization callbacks fail.</exception>
    public void AccessibilityDrop()
    {
        EnsureMutable(); (Tree ?? throw new InvalidOperationException("Accessible dropping requires an attached control.")).CompleteAccessibilityDrag(this);
    }
    internal DragPayload? GetAccessibilityDragData() => GetDragDataCore(AccessibilityDragPosition);
    internal bool CanAccessibilityDrop(DragPayload payload) => CanDropDataCore(AccessibilityDragPosition, payload);
    internal void DropAccessibilityData(DragPayload payload) => DropDataCore(AccessibilityDragPosition, payload);
}
