namespace Electron2D;

public partial class Control
{
    private string _accessibilityName = "", _accessibilityDescription = "", _resolvedAccessibilityName = "", _resolvedAccessibilityDescription = "";
    private string[] _accessibilityControlsNodes = [], _accessibilityDescribedByNodes = [], _accessibilityLabeledByNodes = [], _accessibilityFlowToNodes = [];
    internal ulong AccessibilityRelationRevision { get; private set; }
    internal ReadOnlySpan<string> AccessibilityControlsPaths => _accessibilityControlsNodes;
    internal ReadOnlySpan<string> AccessibilityDescribedByPaths => _accessibilityDescribedByNodes;
    internal ReadOnlySpan<string> AccessibilityLabeledByPaths => _accessibilityLabeledByNodes;
    internal ReadOnlySpan<string> AccessibilityFlowToPaths => _accessibilityFlowToNodes;
    private static readonly PropertyDescriptor[] AccessibilityProperties =
    [
        new PropertyDescriptor<Control, string>(nameof(AccessibilityName), c => c.AccessibilityName, (c,v) => c.AccessibilityName=v, _ => "", stored: true),
        new PropertyDescriptor<Control, string>(nameof(AccessibilityDescription), c => c.AccessibilityDescription, (c,v) => c.AccessibilityDescription=v, _ => "", stored: true),
        new PropertyDescriptor<Control, string[]>(nameof(AccessibilityControlsNodes), c => c.AccessibilityControlsNodes, (c,v) => c.AccessibilityControlsNodes=v, _ => [], stored: true),
        new PropertyDescriptor<Control, string[]>(nameof(AccessibilityDescribedByNodes), c => c.AccessibilityDescribedByNodes, (c,v) => c.AccessibilityDescribedByNodes=v, _ => [], stored: true),
        new PropertyDescriptor<Control, string[]>(nameof(AccessibilityLabeledByNodes), c => c.AccessibilityLabeledByNodes, (c,v) => c.AccessibilityLabeledByNodes=v, _ => [], stored: true),
        new PropertyDescriptor<Control, string[]>(nameof(AccessibilityFlowToNodes), c => c.AccessibilityFlowToNodes, (c,v) => c.AccessibilityFlowToNodes=v, _ => [], stored: true),
    ];
    /// <summary>Gets or sets the authored accessible name, overriding this control's displayed caption.</summary>
    /// <value>Empty by default, using the control's ordinary semantic caption.</value>
    /// <remarks>Stored in scenes and automatically translated using this control's translation context.</remarks>
    /// <exception cref="ArgumentNullException">The new name is null.</exception>
    /// <exception cref="ObjectDisposedException">This control is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    public string AccessibilityName
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _accessibilityName; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); var resolved = Atr(value); if (_accessibilityName == value && _resolvedAccessibilityName == resolved) return; _accessibilityName = value; _resolvedAccessibilityName = resolved; QueueAccessibilityUpdate(); UpdateConfigurationWarnings(); }
    }
    /// <summary>Gets or sets the authored description exposed to native assistive clients.</summary>
    /// <value>Empty by default. Stored and automatically translated with the control.</value>
    /// <exception cref="ArgumentNullException">The new description is null.</exception>
    /// <exception cref="ObjectDisposedException">This control is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    public string AccessibilityDescription
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _accessibilityDescription; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); var resolved = Atr(value); if (_accessibilityDescription == value && _resolvedAccessibilityDescription == resolved) return; _accessibilityDescription = value; _resolvedAccessibilityDescription = resolved; QueueAccessibilityUpdate(); }
    }
    /// <summary>Gets or sets authored paths to the nodes controlled by this control.</summary>
    /// <value>An empty array initially. Arrays are copied in and out and stored in scenes.</value>
    /// <remarks>Empty and unresolved paths are ignored until resolvable. Targets in another native window are ignored.
    /// Topology and assignment changes refresh native relations without retaining scene element RIDs.</remarks>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ArgumentException">An array item is null or contains malformed node-path syntax.</exception>
    /// <exception cref="ObjectDisposedException">This control is disposed.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned.</exception>
    public string[] AccessibilityControlsNodes
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return (string[])_accessibilityControlsNodes.Clone(); }
        set => SetAccessibilityPaths(ref _accessibilityControlsNodes, value);
    }

    /// <summary>Gets or sets authored paths to the nodes describing this control.</summary>
    /// <value>An empty array initially. Arrays are copied in and out and stored in scenes.</value>
    /// <remarks>Empty and unresolved paths are ignored until resolvable. Targets in another native window are ignored.
    /// Topology and assignment changes refresh native relations without retaining scene element RIDs.</remarks>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ArgumentException">An array item is null or contains malformed node-path syntax.</exception>
    /// <exception cref="ObjectDisposedException">This control is disposed.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned.</exception>
    public string[] AccessibilityDescribedByNodes
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return (string[])_accessibilityDescribedByNodes.Clone(); }
        set => SetAccessibilityPaths(ref _accessibilityDescribedByNodes, value);
    }

    /// <summary>Gets or sets authored paths to the nodes labelling this control.</summary>
    /// <value>An empty array initially. Arrays are copied in and out and stored in scenes.</value>
    /// <remarks>Empty and unresolved paths are ignored until resolvable. Targets in another native window are ignored.
    /// Topology and assignment changes refresh native relations without retaining scene element RIDs.</remarks>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ArgumentException">An array item is null or contains malformed node-path syntax.</exception>
    /// <exception cref="ObjectDisposedException">This control is disposed.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned.</exception>
    public string[] AccessibilityLabeledByNodes
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return (string[])_accessibilityLabeledByNodes.Clone(); }
        set => SetAccessibilityPaths(ref _accessibilityLabeledByNodes, value);
    }

    /// <summary>Gets or sets authored paths to the nodes following this control in semantic reading flow.</summary>
    /// <value>An empty array initially. Arrays are copied in and out and stored in scenes.</value>
    /// <remarks>Empty and unresolved paths are ignored until resolvable. Targets in another native window are ignored.
    /// Topology and assignment changes refresh native relations without retaining scene element RIDs.</remarks>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ArgumentException">An array item is null or contains malformed node-path syntax.</exception>
    /// <exception cref="ObjectDisposedException">This control is disposed.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation is capture-owned.</exception>
    public string[] AccessibilityFlowToNodes
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return (string[])_accessibilityFlowToNodes.Clone(); }
        set => SetAccessibilityPaths(ref _accessibilityFlowToNodes, value);
    }
    private void SetAccessibilityPaths(ref string[] storage, string[] value)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(value);
        var copy = (string[])value.Clone();
        foreach (var path in copy)
        {
            if (path is null) throw new ArgumentException("A related-node path cannot be null.", nameof(value));
            _ = GetNodeOrNull(path);
        }
        if (storage.AsSpan().SequenceEqual(copy)) return;
        storage = copy; AccessibilityRelationRevision++; QueueAccessibilityUpdate(); UpdateConfigurationWarnings();
    }
    private bool HasAuthoredAccessibilityLabel()
    {
        foreach (var path in _accessibilityLabeledByNodes)
            if (path.Length != 0 && GetNodeOrNull(path) is Control { IsDisposed: false } label && label != this && label.AccessibleCaption.Length != 0 && SceneAccessibility.NativeWindow(label) == SceneAccessibility.NativeWindow(this)) return true;
        return false;
    }
    /// <inheritdoc />
    protected override string[] OnGetAccessibilityConfigurationWarnings()
    {
        var warnings = base.OnGetAccessibilityConfigurationWarnings();
        return (this is BaseButton or Container { AccessibilityRegion: true }) && AccessibleCaption.Length == 0 && !HasAuthoredAccessibilityLabel()
            ? [.. warnings, "Provide an accessible name for this action or landmark region."] : warnings;
    }
    internal void GrabAccessibilityFocus() => GrabFocus();
    internal void RefreshAccessibilityText() { _resolvedAccessibilityName = Atr(_accessibilityName); _resolvedAccessibilityDescription = Atr(_accessibilityDescription); }
    internal string AccessibleCaption => _resolvedAccessibilityName.Length != 0 ? _resolvedAccessibilityName : this switch
    {
        Button button => button.AccessibleButtonText,
        LinkButton link => link.AccessibleLinkText,
        Label label => label.AccessibleLabelText,
        _ => "",
    };
    internal string AccessibleDescription => _resolvedAccessibilityDescription;
}
