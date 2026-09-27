namespace Electron2D;

public partial class Control
{
    private WeakReference<Node>? _shortcutContext;
    private string _tooltipText = string.Empty;
    private NodeAutoTranslateMode _tooltipAutoTranslateMode;
    private static readonly PropertyDescriptor[] TooltipProperties =
    [
        new PropertyDescriptor<Control, Node?>(nameof(ShortcutContext), node => node.ShortcutContext, (node, value) => node.ShortcutContext = value, _ => null, stored: true),
        new PropertyDescriptor<Control, string>(nameof(TooltipText), node => node.TooltipText, (node, value) => node.TooltipText = value, _ => string.Empty, stored: true),
        new PropertyDescriptor<Control, NodeAutoTranslateMode>(nameof(TooltipAutoTranslateMode), node => node.TooltipAutoTranslateMode, (node, value) => node.TooltipAutoTranslateMode = value, _ => NodeAutoTranslateMode.Inherit, stored: true)
    ];

    /// <summary>Gets or sets the weak scene context that restricts this control's shortcuts.</summary>
    /// <value>Null initially, meaning global shortcuts. A context permits only focus on itself or its descendants.
    /// A freed context is returned as null but continues to disable shortcuts until explicitly cleared.</value>
    /// <remarks>Packed scenes save the relative node path and resolve it after constructing the complete hierarchy.</remarks>
    /// <exception cref="ObjectDisposedException">This control or an assigned context is disposed.</exception>
    /// <exception cref="InvalidOperationException">Access violates the scene owner thread or capture boundary.</exception>
    public Node? ShortcutContext
    {
        get { CheckTooltipAccess(); return _shortcutContext is not null && _shortcutContext.TryGetTarget(out var node) && !node.IsDisposed ? node : null; }
        set
        {
            EnsureMutable(); if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value);
            if (value is null) _shortcutContext = null;
            else if (_shortcutContext is null) _shortcutContext = new(value);
            else _shortcutContext.SetTarget(value);
        }
    }
    /// <summary>Gets or sets the untranslated tooltip text.</summary>
    /// <value>An empty string initially. Whitespace is retained; the presenter trims its outer edges.</value>
    /// <exception cref="ArgumentNullException">The assigned text is null.</exception>
    /// <exception cref="InvalidOperationException">Access violates the scene owner thread or capture boundary.</exception>
    /// <exception cref="ObjectDisposedException">This control is disposed.</exception>
    public string TooltipText
    {
        get { CheckTooltipAccess(); return _tooltipText; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); _tooltipText = value; }
    }
    /// <summary>Gets or sets automatic translation of the default tooltip label.</summary>
    /// <value>Inherit initially. Always enables translation and Disabled suppresses it.</value>
    /// <remarks>The stored enum identity is retained; a default label validates the mode when shown.</remarks>
    /// <exception cref="InvalidOperationException">Access violates the scene owner thread or capture boundary.</exception>
    /// <exception cref="ObjectDisposedException">This control is disposed.</exception>
    public NodeAutoTranslateMode TooltipAutoTranslateMode
    {
        get { CheckTooltipAccess(); return _tooltipAutoTranslateMode; }
        set { EnsureMutable(); _tooltipAutoTranslateMode = value; }
    }
    /// <summary>Gets the untranslated tooltip for a local position through the overridable tooltip hook.</summary>
    /// <param name="atPosition">The local control position; zero by default.</param>
    /// <returns>The tooltip text, or an empty string to allow eligible parent lookup.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The position is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">An attached control is queried off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This control is disposed.</exception>
    public string GetTooltip(Vector2 atPosition = default)
    {
        CheckTooltipAccess(); ValidateTooltipPosition(atPosition); return OnGetTooltip(atPosition) ?? string.Empty;
    }
    /// <summary>Supplies untranslated tooltip text for a local control position.</summary>
    /// <param name="atPosition">The finite local cursor position.</param>
    /// <returns>TooltipText by default; an empty string permits eligible parent lookup.</returns>
    protected virtual string OnGetTooltip(Vector2 atPosition) => _tooltipText;
    /// <summary>Supplies the default tooltip label's translation policy at a local position.</summary>
    /// <param name="atPosition">The finite local cursor position.</param>
    /// <returns>TooltipAutoTranslateMode by default.</returns>
    protected virtual NodeAutoTranslateMode OnGetTooltipAutoTranslateModeAt(Vector2 atPosition) => _tooltipAutoTranslateMode;
    /// <summary>Creates an optional custom tooltip control.</summary>
    /// <param name="forText">The trimmed untranslated tooltip text, which may be empty.</param>
    /// <returns>A newly owned detached control, or null to use the default label. A hidden returned control suppresses display.</returns>
    /// <remarks>Ownership of a valid detached result transfers to the presenter, which disposes it when the tooltip closes.
    /// Do not return this control, an ancestor, or a control already parented or attached to a scene tree.</remarks>
    protected virtual Control? OnMakeCustomTooltip(string forText) => null;

    internal virtual Control? CreateTooltipControl(string text) => OnMakeCustomTooltip(text);
    internal NodeAutoTranslateMode GetTooltipTranslationMode(Vector2 position)
    {
        CheckTooltipAccess(); ValidateTooltipPosition(position); return OnGetTooltipAutoTranslateModeAt(position);
    }
    internal bool IsFocusOwnerInShortcutContext()
    {
        CheckTooltipAccess();
        if (_shortcutContext is null) return true;
        var context = ShortcutContext; var focused = GetViewport()?.GetGUIFocusOwner();
        return context is not null && focused is not null && (ReferenceEquals(context, focused) || context.IsAncestorOf(focused));
    }
    private void CheckTooltipAccess() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private static void ValidateTooltipPosition(Vector2 position)
    {
        if (!position.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position), "Tooltip positions must be finite.");
    }
}
