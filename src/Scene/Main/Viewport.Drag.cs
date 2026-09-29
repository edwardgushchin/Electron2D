namespace Electron2D;

public abstract partial class Viewport
{
    private static readonly PropertyDescriptor[] ViewportDragProperties =
    [
        new PropertyDescriptor<Viewport, int>(nameof(GUIDragThreshold), viewport => viewport.GUIDragThreshold,
            (viewport, value) => viewport.GUIDragThreshold = value, _ => 10, stored: true)
    ];

    private int _guiDragThreshold;
    private string _guiDragDescription = string.Empty;

    /// <summary>Gets or sets the pointer travel required before an automatic GUI drag is attempted.</summary>
    /// <value>Ten logical pixels initially, sampled from the active project setting at construction.</value>
    /// <remarks>A travel length must exceed the signed threshold. A negative value attempts on the first motion.</remarks>
    public int GUIDragThreshold
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _guiDragThreshold; }
        set { EnsureMutable(); _guiDragThreshold = value; }
    }

    /// <summary>Gets the active borrowed drag payload, or null.</summary>
    /// <returns>The payload while dragging; the caller never owns it through this query.</returns>
    public DragPayload? GetGUIDragData()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        return Tree?.GetGUIDragData(this);
    }

    /// <summary>Reports whether this root viewport is preparing or carrying a GUI drag.</summary>
    /// <returns>True during an active drag or while its source callback prepares one.</returns>
    public bool IsGUIDragging()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        return Tree?.IsGUIDragging(this) ?? false;
    }

    /// <summary>Reports whether the most recent GUI drag completed on an accepting target.</summary>
    /// <returns>False before a drag or after cancellation and failed drops.</returns>
    public bool IsGUIDragSuccessful()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        return Tree?.IsGUIDragSuccessful(this) ?? false;
    }

    /// <summary>Gets a description of the active drag for a host accessibility layer.</summary>
    /// <returns>The explicit description, or a localized generic drag label when empty.</returns>
    public string GetGUIDragDescription()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        return _guiDragDescription.Length == 0 ? Tr("Drag-and-drop data") : _guiDragDescription;
    }

    /// <summary>Sets an active drag's host-facing description.</summary>
    /// <param name="description">The nonnull description to retain until drag completion.</param>
    public void SetGUIDragDescription(string description)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(description); _guiDragDescription = description;
    }

    /// <summary>Cancels this root viewport's active drag, destroying its preview.</summary>
    public void CancelGUIDrag()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        Tree?.CancelGUIDrag(this);
    }

    internal void ClearGUIDragDescription() => _guiDragDescription = string.Empty;
}
