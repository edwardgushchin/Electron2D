namespace Electron2D;

public partial class Control
{
    private Func<Vector2, DragPayload?>? _forwardGetDragData;
    private Func<Vector2, DragPayload, bool>? _forwardCanDropData;
    private Action<Vector2, DragPayload>? _forwardDropData;

    /// <summary>Sets typed delegates that replace the three virtual drag hooks when supplied.</summary>
    /// <param name="getData">Produces a payload from a local pointer position, or null to use the virtual hook.</param>
    /// <param name="canDrop">Tests an incoming payload, or null to use the virtual hook.</param>
    /// <param name="drop">Consumes an accepted payload, or null to use the virtual hook.</param>
    /// <remarks>Delegates are borrowed and cleared on disposal. A supplied delegate returning null or false
    /// does not fall back to the corresponding virtual hook.</remarks>
    public void SetDragForwarding(Func<Vector2, DragPayload?>? getData,
        Func<Vector2, DragPayload, bool>? canDrop, Action<Vector2, DragPayload>? drop)
    {
        EnsureMutable(); _forwardGetDragData = getData; _forwardCanDropData = canDrop; _forwardDropData = drop;
    }

    /// <summary>Produces a typed payload for a drag beginning at a control-local position.</summary>
    /// <param name="atPosition">Finite local pointer coordinates.</param>
    /// <returns>A borrowed payload, or null when no drag can begin.</returns>
    public DragPayload? GetDragData(Vector2 atPosition)
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        if (!atPosition.IsFinite()) throw new ArgumentException("Drag position must be finite.", nameof(atPosition));
        return _forwardGetDragData is { } forward ? forward(atPosition) : OnGetDragData(atPosition);
    }

    /// <summary>Tests whether this control accepts a typed payload at a local position.</summary>
    /// <param name="atPosition">Finite local pointer coordinates.</param>
    /// <param name="payload">The live borrowed drag payload.</param>
    /// <returns>Whether this control accepts the payload.</returns>
    public bool CanDropData(Vector2 atPosition, DragPayload payload)
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread(); ArgumentNullException.ThrowIfNull(payload);
        if (!atPosition.IsFinite()) throw new ArgumentException("Drop position must be finite.", nameof(atPosition));
        return _forwardCanDropData is { } forward ? forward(atPosition, payload) : OnCanDropData(atPosition, payload);
    }

    /// <summary>Delivers an accepted payload to this control.</summary>
    /// <param name="atPosition">Finite local pointer coordinates.</param>
    /// <param name="payload">The live borrowed drag payload.</param>
    public void DropData(Vector2 atPosition, DragPayload payload)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(payload);
        if (!atPosition.IsFinite()) throw new ArgumentException("Drop position must be finite.", nameof(atPosition));
        if (_forwardDropData is { } forward) forward(atPosition, payload);
        else OnDropData(atPosition, payload);
    }

    /// <summary>Produces drag data for an automatic pointer drag; null rejects the attempt.</summary>
    /// <param name="atPosition">The original left-press position in local coordinates.</param>
    /// <returns>A typed payload, or null.</returns>
    protected virtual DragPayload? OnGetDragData(Vector2 atPosition) => null;

    /// <summary>Tests a potential drop target; false rejects it.</summary>
    /// <param name="atPosition">The current local pointer position.</param>
    /// <param name="payload">The borrowed typed payload.</param>
    /// <returns>Whether the target accepts the payload.</returns>
    protected virtual bool OnCanDropData(Vector2 atPosition, DragPayload payload) => false;

    /// <summary>Consumes an accepted drop after the target test succeeds.</summary>
    /// <param name="atPosition">The current local pointer position.</param>
    /// <param name="payload">The borrowed typed payload.</param>
    protected virtual void OnDropData(Vector2 atPosition, DragPayload payload) { }

    /// <summary>Starts a drag immediately, optionally using a detached preview control.</summary>
    /// <param name="payload">A nonnull typed payload borrowed until the drag ends.</param>
    /// <param name="preview">A live parentless control transferred to the scene tree and disposed at drag end.</param>
    public void ForceDrag(DragPayload payload, Control? preview = null)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(payload);
        (Tree ?? throw new InvalidOperationException("Dragging requires an attached control.")).ForceGUIDrag(this, payload, preview);
    }

    /// <summary>Replaces the visual preview of the active or preparing drag.</summary>
    /// <param name="preview">A live parentless control transferred to the tree and disposed at drag end.</param>
    public void SetDragPreview(Control preview)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(preview);
        (Tree ?? throw new InvalidOperationException("Dragging requires an attached control.")).SetGUIDragPreview(this, preview);
    }

    /// <summary>Reports the result of the most recently completed GUI drag in this viewport.</summary>
    /// <returns>True when an accepted target received the most recent drop.</returns>
    public bool IsDragSuccessful()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        return GetViewport()?.IsGUIDragSuccessful() ?? false;
    }
}
