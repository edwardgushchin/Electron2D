namespace Electron2D;

public partial class TabBar
{
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<TabBar, int>(nameof(TabCount), b => b.TabCount, (b, v) => b.TabCount = v, _ => 0, stored: true),
        new PropertyDescriptor<TabBar, bool>(nameof(ClipTabs), b => b.ClipTabs, (b, v) => b.ClipTabs = v, _ => true, stored: true),
        new PropertyDescriptor<TabBar, bool>(nameof(CloseWithMiddleMouse), b => b.CloseWithMiddleMouse, (b, v) => b.CloseWithMiddleMouse = v, _ => true, stored: true),
        new PropertyDescriptor<TabBar, bool>(nameof(DragToRearrangeEnabled), b => b.DragToRearrangeEnabled, (b, v) => b.DragToRearrangeEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<TabBar, bool>(nameof(ScrollToSelected), b => b.ScrollToSelected, (b, v) => b.ScrollToSelected = v, _ => true, stored: true),
        new PropertyDescriptor<TabBar, bool>(nameof(ScrollingEnabled), b => b.ScrollingEnabled, (b, v) => b.ScrollingEnabled = v, _ => true, stored: true),
        new PropertyDescriptor<TabBar, bool>(nameof(SelectWithRMB), b => b.SelectWithRMB, (b, v) => b.SelectWithRMB = v, _ => false, stored: true),
        new PropertyDescriptor<TabBar, bool>(nameof(SwitchOnDragHover), b => b.SwitchOnDragHover, (b, v) => b.SwitchOnDragHover = v, _ => true, stored: true),
        new PropertyDescriptor<TabBar, int>(nameof(TabsRearrangeGroup), b => b.TabsRearrangeGroup, (b, v) => b.TabsRearrangeGroup = v, _ => -1, stored: true),
        new PropertyDescriptor<TabBar, bool>(nameof(DeselectEnabled), b => b.DeselectEnabled, (b, v) => b.DeselectEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<TabBar, int>(nameof(MaxTabWidth), b => b.MaxTabWidth, (b, v) => b.MaxTabWidth = v, _ => 0, stored: true),
        new PropertyDescriptor<TabBar, AlignmentMode>(nameof(TabAlignment), b => b.TabAlignment, (b, v) => b.TabAlignment = v, _ => AlignmentMode.Left, stored: true),
        new PropertyDescriptor<TabBar, CloseButtonDisplayPolicy>(nameof(TabCloseDisplayPolicy), b => b.TabCloseDisplayPolicy, (b, v) => b.TabCloseDisplayPolicy = v, _ => CloseButtonDisplayPolicy.ShowNever, stored: true),
        new PropertyDescriptor<TabBar, FocusMode>(nameof(FocusMode), b => b.FocusMode, (b, v) => b.FocusMode = v, _ => FocusMode.All, stored: true),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) if (property.Name != nameof(FocusMode)) yield return property;
        foreach (var property in Properties) yield return property;
        for (var i = 0; i < _tabs.Count; i++)
        {
            var index = i;
            yield return new PropertyDescriptor<TabBar, string>($"tab_{index}/title", b => b.GetTabTitle(index), (b, v) => b.SetTabTitle(index, v), _ => "", stored: true);
            yield return new PropertyDescriptor<TabBar, string>($"tab_{index}/tooltip", b => b.GetTabTooltip(index), (b, v) => b.SetTabTooltip(index, v), _ => "", stored: true);
            yield return new PropertyDescriptor<TabBar, Texture?>($"tab_{index}/icon", b => b.GetTabIcon(index), (b, v) => b.SetTabIcon(index, v), _ => null, stored: true);
            yield return new PropertyDescriptor<TabBar, bool>($"tab_{index}/disabled", b => b.IsTabDisabled(index), (b, v) => b.SetTabDisabled(index, v), _ => false, stored: true);
        }
        yield return new PropertyDescriptor<TabBar, int>(nameof(CurrentTab), b => b.CurrentTab, (b, v) => b.CurrentTab = v, b => b.DeselectEnabled || b.TabCount == 0 ? -1 : 0, stored: true);
    }
}
