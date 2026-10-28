namespace Electron2D;

public partial class Control
{
    /// <summary>Returns contextual keyboard help for the native accessible HelpText property.</summary>
    /// <returns>Contextual help; empty by default.</returns>
    /// <remarks>Runs on queued source updates. Queue an accessibility update after changing custom context.</remarks>
    protected virtual string OnAccessibilityGetContextualInfo() => "";
    /// <summary>Describes the immediate child's position in this accessible container.</summary>
    /// <param name="node">The immediate child represented at this level.</param>
    /// <returns>A name suffix, or the parent Control's description of this container by default.</returns>
    /// <remarks>A non-Control parent ends inheritance. Queue this container's accessibility update after changing its custom description.</remarks>
    /// <exception cref="ArgumentNullException">The child is null.</exception>
    protected virtual string OnGetAccessibilityContainerName(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return GetParentControl()?.AccessibleContainerName(this) ?? "";
    }
    internal string AccessibleContextualInfo() => (ControlCallbacks?.AccessibilityGetContextualInfo is { } query ? query() : OnAccessibilityGetContextualInfo()) ?? throw new InvalidOperationException("Contextual help cannot be null.");
    internal string AccessibleContainerName(Node child) => (ControlCallbacks?.GetAccessibilityContainerName is { } query ? query(child) : OnGetAccessibilityContainerName(child)) ?? throw new InvalidOperationException("A container name cannot be null.");
}
