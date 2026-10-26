namespace Electron2D;

public partial class Control
{
    private string _accessibilityName = "", _accessibilityDescription = "", _resolvedAccessibilityName = "", _resolvedAccessibilityDescription = "";
    private static readonly PropertyDescriptor[] AccessibilityProperties =
    [
        new PropertyDescriptor<Control, string>(nameof(AccessibilityName), c => c.AccessibilityName, (c,v) => c.AccessibilityName=v, _ => "", stored: true),
        new PropertyDescriptor<Control, string>(nameof(AccessibilityDescription), c => c.AccessibilityDescription, (c,v) => c.AccessibilityDescription=v, _ => "", stored: true),
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
    /// <inheritdoc />
    protected override string[] OnGetAccessibilityConfigurationWarnings()
    {
        var warnings = base.OnGetAccessibilityConfigurationWarnings();
        return (this is BaseButton or Container { AccessibilityRegion: true }) && AccessibleCaption.Length == 0
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
