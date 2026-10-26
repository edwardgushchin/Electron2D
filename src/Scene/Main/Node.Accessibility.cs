namespace Electron2D;

public partial class Node
{
    /// <summary>Requests the node's current native semantic metadata.</summary>
    public const int NotificationAccessibilityUpdate = 3000;
    /// <summary>Indicates retirement of this node's transient accessibility element.</summary>
    public const int NotificationAccessibilityInvalidate = 3001;
    private RID _accessibilityElement;
    internal bool AccessibilityQueued = true;
    internal RID AccessibilityIdentity => _accessibilityElement;
    /// <summary>Gets this node's borrowed native semantic element while automatic scene output is active.</summary>
    /// <returns>A live element RID, or an empty identity outside a native accessible scene.</returns>
    /// <remarks>Identity is transient, owner-affine and never copied or serialized.</remarks>
    /// <exception cref="ObjectDisposedException">This node has been disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached query is off the scene owner.</exception>
    public RID GetAccessibilityElement()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        if (AccessibilityServer.TryGetSource(_accessibilityElement, this) is not null) return _accessibilityElement;
        return IsDepartingTree ? default : Tree?.GetAccessibilityElement(this) ?? default;
    }
    /// <summary>Queues a semantic update notification during the next scene accessibility pass.</summary>
    /// <remarks>Repeated requests coalesce; detached requests are retained until a later accessible tree entry.</remarks>
    /// <exception cref="ObjectDisposedException">This node has been disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is outside the owner or current processing group.</exception>
    public void QueueAccessibilityUpdate() { EnsureProcessingAccess(); AccessibilityQueued = true; }
    /// <summary>Returns accessibility-specific authoring warnings.</summary><returns>Ordered warning strings; empty by default.</returns>
    protected virtual string[] OnGetAccessibilityConfigurationWarnings() => [];
    /// <summary>Selects the semantic element focused when this node owns GUI focus.</summary>
    /// <returns>The node's own element by default; a composite may return one of its live sub-elements.</returns>
    protected virtual RID OnGetFocusedAccessibilityElement() => GetAccessibilityElement();
    internal void DispatchAccessibilityUpdate() => DispatchNotification(NotificationAccessibilityUpdate);
    internal RID FocusedAccessibilityElement() => OnGetFocusedAccessibilityElement();
    internal void SetAccessibilityElement(RID id) => _accessibilityElement = id;
    internal void AppendAccessibilityNodes(List<Node> output)
    {
        output.Add(this); foreach (var child in _children) child.AppendAccessibilityNodes(output);
    }
    internal void DetachAccessibility()
    {
        var id = _accessibilityElement; if (!id.IsValid()) return;
        try { DispatchNotification(NotificationAccessibilityInvalidate); }
        finally { try { AccessibilityServer.DetachSource(id, this); } finally { _accessibilityElement = default; AccessibilityQueued = true; } }
    }
}
