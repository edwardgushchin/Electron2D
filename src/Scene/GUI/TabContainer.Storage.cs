namespace Electron2D;

public partial class TabContainer
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<TabContainer, bool>(nameof(ClipTabs), m => m.ClipTabs, (m, v) => m.ClipTabs = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TabContainer, bool>(nameof(DeselectEnabled), m => m.DeselectEnabled, (m, v) => m.DeselectEnabled = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TabContainer, bool>(nameof(DragToRearrangeEnabled), m => m.DragToRearrangeEnabled, (m, v) => m.DragToRearrangeEnabled = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TabContainer, bool>(nameof(SwitchOnDragHover), m => m.SwitchOnDragHover, (m, v) => m.SwitchOnDragHover = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TabContainer, TabBar.AlignmentMode>(nameof(TabAlignment), m => m.TabAlignment, (m, v) => m.TabAlignment = v, _ => TabBar.AlignmentMode.Left, stored: true);
        yield return new PropertyDescriptor<TabContainer, FocusMode>(nameof(TabFocusMode), m => m.TabFocusMode, (m, v) => m.TabFocusMode = v, _ => FocusMode.All, stored: true);
        yield return new PropertyDescriptor<TabContainer, TabPosition>(nameof(TabsPosition), m => m.TabsPosition, (m, v) => m.TabsPosition = v, _ => TabPosition.Top, stored: true);
        yield return new PropertyDescriptor<TabContainer, int>(nameof(TabsRearrangeGroup), m => m.TabsRearrangeGroup, (m, v) => m.TabsRearrangeGroup = v, _ => -1, stored: true);
        yield return new PropertyDescriptor<TabContainer, bool>(nameof(TabsVisible), m => m.TabsVisible, (m, v) => m.TabsVisible = v, _ => true, stored: true);
        yield return new PropertyDescriptor<TabContainer, bool>(nameof(UseHiddenTabsForMinSize), m => m.UseHiddenTabsForMinSize, (m, v) => m.UseHiddenTabsForMinSize = v, _ => false, stored: true);
        yield return new PropertyDescriptor<TabContainer, int>("_tab_schema_count", m => Math.Max(m._pages.Count, m._pending.Count), (m, v) => { if ((uint)v > 65536) throw new ArgumentOutOfRangeException(nameof(v)); while (m._pending.Count < v) m._pending.Add(new()); }, _ => 0, stored: true);
        for (var index = 0; index < Math.Max(_pages.Count, _pending.Count); index++)
        {
            var i = index;
            yield return new PropertyDescriptor<TabContainer, string>($"tab_{i}/title", m => i < m._pages.Count ? m.GetTabTitle(i) : m._pending[i].Title ?? "", (m, v) => m.SetTabTitle(i, v), m => i < m._pages.Count ? m._pages[i].Control.Name : "", stored: true);
            yield return new PropertyDescriptor<TabContainer, Texture?>($"tab_{i}/icon", m => i < m._pages.Count ? m.GetTabIcon(i) : m._pending[i].Icon, (m, v) => m.SetTabIcon(i, v), _ => null, stored: true);
            yield return new PropertyDescriptor<TabContainer, bool>($"tab_{i}/disabled", m => i < m._pages.Count ? m.IsTabDisabled(i) : m._pending[i].Disabled, (m, v) => m.SetTabDisabled(i, v), _ => false, stored: true);
            yield return new PropertyDescriptor<TabContainer, bool>($"tab_{i}/hidden", m => i < m._pages.Count ? m.IsTabHidden(i) : m._pending[i].Hidden, (m, v) => m.SetTabHidden(i, v), _ => false, stored: true);
        }
        yield return new PropertyDescriptor<TabContainer, int>(nameof(CurrentTab), m => m._setupCurrent >= -1 ? m._setupCurrent : m.CurrentTab, (m, v) => m.CurrentTab = v, _ => -1, stored: true);
    }
}
