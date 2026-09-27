namespace Electron2D;

public partial class Control
{
    private ThemeOwner? _themeOwner;
    internal ThemeOwner ThemeOwner => _themeOwner ??= new ThemeOwner(this, CheckThemeQuery, EnsureMutable);
    private static readonly PropertyDescriptor[] ThemeProperties =
    [
        new PropertyDescriptor<Control, Theme?>(nameof(Theme), node => node.Theme, (node, value) => node.Theme = value, _ => null, stored: true),
        new PropertyDescriptor<Control, string>(nameof(ThemeTypeVariation), node => node.ThemeTypeVariation, (node, value) => node.ThemeTypeVariation = value, _ => "", stored: true)
    ];
    /// <summary>Identifies a theme refresh before cached values are invalidated and dependent state is updated.</summary>
    public const int NotificationThemeChanged = 45;
    /// <summary>Occurs during a theme refresh, before the previous lookup cache is cleared.</summary>
    /// <remarks>Resource changes are deferred to the scene owner. Local overrides refresh synchronously unless
    /// bulk editing is active. Required invalidation continues after a subscriber fails.</remarks>
    public event Action? ThemeChanged;
    /// <summary>Gets or sets the borrowed theme supplying this branch's typed appearance values.</summary>
    /// <value>Null initially; lookup continues through consecutive Control/Window ancestors and built-in defaults.</value>
    /// <exception cref="ObjectDisposedException">The node or assigned theme is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation is capture-owned or access is off the scene owner.</exception>
    public Theme? Theme { get => ThemeOwner.Theme; set => ThemeOwner.Theme = value; }
    /// <summary>Gets or sets the variation name used before this node's native type hierarchy.</summary>
    /// <value>Empty initially. The nearest theme defining its base supplies the variation chain.</value>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The name contains a null character.</exception>
    /// <exception cref="InvalidOperationException">Mutation is capture-owned or access is off the scene owner.</exception>
    public string ThemeTypeVariation { get => ThemeOwner.Variation; set => ThemeOwner.Variation = value; }
    private void CheckThemeQuery() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    internal int InheritedThemeConstant(string name) => ThemeOwner.Get(ThemeOwner.Constants, name, "", useOverrides: false);
    /// <summary>Gets a typed theme color using local override, branch themes, native defaults and universal fallback.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="themeType">An explicit type, or empty for this node and its variation.</param>
    /// <returns>The resolved value; resources are borrowed.</returns>
    /// <exception cref="ArgumentException">A key is null or contains a null character.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or a variation chain cycles.</exception>
    /// <exception cref="ObjectDisposedException">The node or queried service is disposed.</exception>
    public Color GetThemeColor(string name, string themeType = "") => ThemeOwner.Get(ThemeOwner.Colors, name, themeType);
    /// <summary>Tests whether an override or a branch/default theme provides a color, excluding universal fallback.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="themeType">An explicit type, or empty for this node and its variation.</param>
    /// <returns>True when lookup finds a defined value.</returns>
    public bool HasThemeColor(string name, string themeType = "") => ThemeOwner.Has(ThemeOwner.Colors, name, themeType);
    /// <summary>Tests for a local color override, without inherited lookup.</summary>
    /// <param name="name">The item key.</param>
    /// <returns>True when an override slot exists.</returns>
    public bool HasThemeColorOverride(string name) => ThemeOwner.HasOverride(ThemeOwner.Colors, name);
    /// <summary>Stores a local color override and refreshes this node's theme state.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="color">The typed override value.</param>
    /// <remarks>Equal assignments still refresh. Overrides apply only to implicit, own-class or own-variation queries.</remarks>
    /// <exception cref="ArgumentException">A key is invalid; color overrides must be finite and resource overrides must be nonnull.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The node or assigned resource is disposed.</exception>
    public void AddThemeColorOverride(string name, Color color)
    {
        ThemeOwner.SetOverride(ThemeOwner.Colors, name, color);
    }
    /// <summary>Removes a local color override and refreshes even when the key was absent.</summary>
    /// <param name="name">The item key.</param>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    public void RemoveThemeColorOverride(string name) => ThemeOwner.RemoveOverride(ThemeOwner.Colors, name);
    /// <summary>Gets a typed theme constant using local override, branch themes, native defaults and universal fallback.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="themeType">An explicit type, or empty for this node and its variation.</param>
    /// <returns>The resolved value; resources are borrowed.</returns>
    /// <exception cref="ArgumentException">A key is null or contains a null character.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or a variation chain cycles.</exception>
    /// <exception cref="ObjectDisposedException">The node or queried service is disposed.</exception>
    public int GetThemeConstant(string name, string themeType = "") => ThemeOwner.Get(ThemeOwner.Constants, name, themeType);
    /// <summary>Tests whether an override or a branch/default theme provides a constant, excluding universal fallback.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="themeType">An explicit type, or empty for this node and its variation.</param>
    /// <returns>True when lookup finds a defined value.</returns>
    public bool HasThemeConstant(string name, string themeType = "") => ThemeOwner.Has(ThemeOwner.Constants, name, themeType);
    /// <summary>Tests for a local constant override, without inherited lookup.</summary>
    /// <param name="name">The item key.</param>
    /// <returns>True when an override slot exists.</returns>
    public bool HasThemeConstantOverride(string name) => ThemeOwner.HasOverride(ThemeOwner.Constants, name);
    /// <summary>Stores a local constant override and refreshes this node's theme state.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="constant">The typed override value.</param>
    /// <remarks>Equal assignments still refresh. Overrides apply only to implicit, own-class or own-variation queries.</remarks>
    /// <exception cref="ArgumentException">A key is invalid; color overrides must be finite and resource overrides must be nonnull.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The node or assigned resource is disposed.</exception>
    public void AddThemeConstantOverride(string name, int constant)
    {
        ThemeOwner.SetOverride(ThemeOwner.Constants, name, constant);
    }
    /// <summary>Removes a local constant override and refreshes even when the key was absent.</summary>
    /// <param name="name">The item key.</param>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    public void RemoveThemeConstantOverride(string name) => ThemeOwner.RemoveOverride(ThemeOwner.Constants, name);
    /// <summary>Gets a typed theme font size using local override, branch themes, native defaults and universal fallback.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="themeType">An explicit type, or empty for this node and its variation.</param>
    /// <returns>The resolved value; resources are borrowed.</returns>
    /// <exception cref="ArgumentException">A key is null or contains a null character.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or a variation chain cycles.</exception>
    /// <exception cref="ObjectDisposedException">The node or queried service is disposed.</exception>
    public int GetThemeFontSize(string name, string themeType = "") => ThemeOwner.Get(ThemeOwner.FontSizes, name, themeType);
    /// <summary>Tests whether an override or a branch/default theme provides a font size, excluding universal fallback.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="themeType">An explicit type, or empty for this node and its variation.</param>
    /// <returns>True when lookup finds a defined value.</returns>
    public bool HasThemeFontSize(string name, string themeType = "") => ThemeOwner.Has(ThemeOwner.FontSizes, name, themeType);
    /// <summary>Tests for a local font size override, without inherited lookup.</summary>
    /// <param name="name">The item key.</param>
    /// <returns>True when an override slot exists.</returns>
    public bool HasThemeFontSizeOverride(string name) => ThemeOwner.HasOverride(ThemeOwner.FontSizes, name);
    /// <summary>Stores a local font size override and refreshes this node's theme state.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="fontSize">The typed override value.</param>
    /// <remarks>Equal assignments still refresh. Overrides apply only to implicit, own-class or own-variation queries.</remarks>
    /// <exception cref="ArgumentException">A key is invalid; color overrides must be finite and resource overrides must be nonnull.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The node or assigned resource is disposed.</exception>
    public void AddThemeFontSizeOverride(string name, int fontSize)
    {
        ThemeOwner.SetOverride(ThemeOwner.FontSizes, name, fontSize);
    }
    /// <summary>Removes a local font size override and refreshes even when the key was absent.</summary>
    /// <param name="name">The item key.</param>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    public void RemoveThemeFontSizeOverride(string name) => ThemeOwner.RemoveOverride(ThemeOwner.FontSizes, name);
    /// <summary>Gets a typed theme icon using local override, branch themes, native defaults and universal fallback.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="themeType">An explicit type, or empty for this node and its variation.</param>
    /// <returns>The resolved value; resources are borrowed.</returns>
    /// <exception cref="ArgumentException">A key is null or contains a null character.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or a variation chain cycles.</exception>
    /// <exception cref="ObjectDisposedException">The node or queried service is disposed.</exception>
    public Texture? GetThemeIcon(string name, string themeType = "") => ThemeOwner.Get(ThemeOwner.Icons, name, themeType);
    /// <summary>Tests whether an override or a branch/default theme provides a icon, excluding universal fallback.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="themeType">An explicit type, or empty for this node and its variation.</param>
    /// <returns>True when lookup finds a defined value.</returns>
    public bool HasThemeIcon(string name, string themeType = "") => ThemeOwner.Has(ThemeOwner.Icons, name, themeType);
    /// <summary>Tests for a local icon override, without inherited lookup.</summary>
    /// <param name="name">The item key.</param>
    /// <returns>True when an override slot exists.</returns>
    public bool HasThemeIconOverride(string name) => ThemeOwner.HasOverride(ThemeOwner.Icons, name);
    /// <summary>Stores a local icon override and refreshes this node's theme state.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="texture">The typed override value.</param>
    /// <remarks>Equal assignments still refresh. Overrides apply only to implicit, own-class or own-variation queries.</remarks>
    /// <exception cref="ArgumentException">A key is invalid; color overrides must be finite and resource overrides must be nonnull.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The node or assigned resource is disposed.</exception>
    public void AddThemeIconOverride(string name, Texture texture)
    {
        ThemeOwner.SetOverride(ThemeOwner.Icons, name, texture);
    }
    /// <summary>Removes a local icon override and refreshes even when the key was absent.</summary>
    /// <param name="name">The item key.</param>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    public void RemoveThemeIconOverride(string name) => ThemeOwner.RemoveOverride(ThemeOwner.Icons, name);
    /// <summary>Gets a typed theme style box using local override, branch themes, native defaults and universal fallback.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="themeType">An explicit type, or empty for this node and its variation.</param>
    /// <returns>The resolved value; resources are borrowed.</returns>
    /// <exception cref="ArgumentException">A key is null or contains a null character.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or a variation chain cycles.</exception>
    /// <exception cref="ObjectDisposedException">The node or queried service is disposed.</exception>
    public StyleBox? GetThemeStyleBox(string name, string themeType = "") => ThemeOwner.Get(ThemeOwner.Styles, name, themeType);
    /// <summary>Tests whether an override or a branch/default theme provides a style box, excluding universal fallback.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="themeType">An explicit type, or empty for this node and its variation.</param>
    /// <returns>True when lookup finds a defined value.</returns>
    public bool HasThemeStyleBox(string name, string themeType = "") => ThemeOwner.Has(ThemeOwner.Styles, name, themeType);
    /// <summary>Tests for a local style box override, without inherited lookup.</summary>
    /// <param name="name">The item key.</param>
    /// <returns>True when an override slot exists.</returns>
    public bool HasThemeStyleBoxOverride(string name) => ThemeOwner.HasOverride(ThemeOwner.Styles, name);
    /// <summary>Stores a local style box override and refreshes this node's theme state.</summary>
    /// <param name="name">The item key.</param>
    /// <param name="styleBox">The live borrowed style.</param>
    /// <remarks>Equal assignments still refresh. Overrides apply only to implicit, own-class or own-variation queries.</remarks>
    /// <exception cref="ArgumentException">A key is invalid; color overrides must be finite and resource overrides must be nonnull.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The node or assigned resource is disposed.</exception>
    public void AddThemeStyleBoxOverride(string name, StyleBox styleBox)
    {
        ThemeOwner.SetOverride(ThemeOwner.Styles, name, styleBox);
    }
    /// <summary>Removes a local style box override and refreshes even when the key was absent.</summary>
    /// <param name="name">The item key.</param>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    public void RemoveThemeStyleBoxOverride(string name) => ThemeOwner.RemoveOverride(ThemeOwner.Styles, name);
    /// <summary>Gets the first positive branch/default theme scale, or the universal scale fallback.</summary>
    /// <returns>The resolved scale.</returns>
    public float GetThemeDefaultBaseScale() => ThemeOwner.DefaultBaseScale();
    /// <summary>Gets the first positive branch/default font-size value, or the universal integer fallback.</summary>
    /// <returns>The resolved size; no Font resource or text renderer is implied.</returns>
    public int GetThemeDefaultFontSize() => ThemeOwner.DefaultFontSize();
    /// <summary>Suppresses local override notifications until EndBulkThemeOverride. Repeated begins do not nest.</summary>
    public void BeginBulkThemeOverride() => ThemeOwner.BeginBulk();
    /// <summary>Ends local override batching and emits one refresh while attached, even when no values changed.</summary>
    /// <exception cref="InvalidOperationException">No batch is active, or mutation is off-owner/capture-owned.</exception>
    public void EndBulkThemeOverride() => ThemeOwner.EndBulk();
    private void ProcessThemeNotification()
    {
        List<Exception>? errors = null;
        try { ThemeChanged?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        ThemeOwner.Invalidate();
        if (!IsDisposed)
        {
            try { QueueRedraw(); } catch (Exception error) { CollectException(ref errors, error); }
            try { UpdateMinimumSize(); } catch (Exception error) { CollectException(ref errors, error); }
            try { Reflow(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Theme notification callbacks failed.", errors);
    }
    internal override void OnTreeMembershipChanged(bool entering)
    {
        ThemeOwner.Membership(entering);
        base.OnTreeMembershipChanged(entering);
    }
}
