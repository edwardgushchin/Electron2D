namespace Electron2D;

/// <summary>Draws the current theme's panel decoration without imposing content margins on children.</summary>
/// <remarks>The panel style is borrowed through inherited theme lookup. Theme changes invalidate retained
/// drawing; the style does not contribute an intrinsic minimum size. Pointer input stops by default.</remarks>
public class Panel : Control
{
    /// <summary>Creates a panel with stopping pointer input and the inherited theme's panel style.</summary>
    public Panel() => MouseFilter = MouseFilter.Stop;
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Theme lookup supplies no panel style.</exception>
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationDraw) DrawStyleBox(GetThemeStyleBox("panel") ?? throw new InvalidOperationException("Panel drawing requires a panel style."), new(Vector2.Zero, Size));
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Panel) ? CreatePanel : base.CreateSceneInstanceFactory();
    private static Node CreatePanel() => new Panel();
}

/// <summary>Draws a themed panel and fits all eligible child controls into its shared content rectangle.</summary>
/// <remarks>Content margins contribute to the minimum size and reduce propagated maximum bounds.
/// Locally visible children contribute minimums even under a hidden ancestor; arrangement uses tree visibility.
/// A missing style contributes zero margins to layout but fails explicitly when drawing.</remarks>
public class PanelContainer : Container
{
    private bool _arranging;
    private readonly List<Control> _children = [];
    private static readonly PropertyDescriptor[] PanelProperties =
    [
        new PropertyDescriptor<PanelContainer, MouseFilter>(nameof(MouseFilter), node => node.MouseFilter, (node, value) => node.MouseFilter = value, _ => MouseFilter.Stop, stored: true)
    ];
    /// <summary>Creates a panel container with stopping pointer input and inherited maximum propagation.</summary>
    public PanelContainer() => MouseFilter = MouseFilter.Stop;
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize()
    {
        var margins = GetThemeStyleBox("panel")?.GetMinimumSize() ?? Vector2.Zero;
        var maximum = GetChildMaximum(margins); var minimum = Vector2.Zero;
        for (var index = 0; index < GetChildCount(includeInternal: true); index++)
            if (Sortable(GetChild(index, includeInternal: true), true) && GetChild(index, includeInternal: true) is Control child)
            {
                child.ContainerMaximum = maximum;
                minimum = minimum.Max(child.GetBoundMinimumSize());
            }
        return minimum + margins;
    }
    private Vector2? GetChildMaximum(Vector2 margins)
    {
        if (!PropagateMaximumSize) return null;
        var maximum = GetCombinedMaximumSize();
        var inner = new Vector2(maximum.X < 0 ? -1 : MathF.Max(0, maximum.X - margins.X), maximum.Y < 0 ? -1 : MathF.Max(0, maximum.Y - margins.Y));
        if (!inner.IsFinite()) throw new InvalidOperationException("Panel content bounds exceeded finite geometry.");
        return inner;
    }
    private void Arrange()
    {
        if (!IsInsideTree || !IsVisibleInTree) return;
        if (_arranging) { QueueSort(); return; }
        _arranging = true; var expectedTree = Tree;
        try
        {
            for (var index = 0; index < GetChildCount(includeInternal: true); index++)
                if (Sortable(GetChild(index, includeInternal: true)) && GetChild(index, includeInternal: true) is Control child) _children.Add(child);
            var style = GetThemeStyleBox("panel");
            var margins = style?.GetMinimumSize() ?? Vector2.Zero; var offset = style?.GetOffset() ?? Vector2.Zero;
            var size = (Size - margins).Max(Vector2.Zero); var maximum = GetChildMaximum(margins);
            List<Exception>? errors = null;
            foreach (var child in _children)
            {
                if (IsDisposed || !IsInsideTree || !ReferenceEquals(Tree, expectedTree)) break;
                if (child.IsDisposed || !ReferenceEquals(child.Parent, this) || !Sortable(child)) continue;
                child.ContainerMaximum = maximum;
                try { FitChildInRect(child, new(offset, size)); }
                catch (Exception error) { CollectException(ref errors, error); }
            }
            ThrowCollected("Panel child layout callbacks failed.", errors);
        }
        finally { _children.Clear(); _arranging = false; }
    }
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Theme lookup supplies no panel style during drawing.</exception>
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationSortChildren) Arrange();
        else if (what == NotificationDraw) DrawStyleBox(GetThemeStyleBox("panel") ?? throw new InvalidOperationException("Panel container drawing requires a panel style."), new(Vector2.Zero, Size));
    }
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsHorizontal() => [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd];
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsVertical() => [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Where(property => property.Name != nameof(MouseFilter)).Concat(PanelProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(PanelContainer) ? CreatePanel : base.CreateSceneInstanceFactory();
    private static Node CreatePanel() => new PanelContainer();
}
