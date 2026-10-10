namespace Electron2D;

public partial class PopupMenu
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        yield return new PropertyDescriptor<PopupMenu, string?>("_menu_bar/title", m => m.MenuBarTitle, (m, v) => { m.EnsureMutable(); if (m.Parent is MenuBar owner) owner.ValidatePopupHeaderMutation(); m.MenuBarTitle = v; if (m.Parent is MenuBar bar) bar.PopupHeaderChanged(m); }, _ => null, stored: true);
        yield return new PropertyDescriptor<PopupMenu, string>("_menu_bar/tooltip", m => m.MenuBarTooltip, (m, v) => { m.EnsureMutable(); ArgumentNullException.ThrowIfNull(v); if (m.Parent is MenuBar owner) owner.ValidatePopupHeaderMutation(); m.MenuBarTooltip = v; if (m.Parent is MenuBar bar) bar.PopupHeaderChanged(m); }, _ => "", stored: true);
        yield return new PropertyDescriptor<PopupMenu, bool>("_menu_bar/disabled", m => m.MenuBarDisabled, (m, v) => { m.EnsureMutable(); if (m.Parent is MenuBar owner) owner.ValidatePopupHeaderMutation(); m.MenuBarDisabled = v; if (m.Parent is MenuBar bar) bar.PopupHeaderChanged(m); }, _ => false, stored: true);
        yield return new PropertyDescriptor<PopupMenu, bool>("_menu_bar/hidden", m => m.MenuBarHidden, (m, v) => { m.EnsureMutable(); if (m.Parent is MenuBar owner) owner.ValidatePopupHeaderMutation(); m.MenuBarHidden = v; if (m.Parent is MenuBar bar) bar.PopupHeaderChanged(m); }, _ => false, stored: true);
        foreach (var property in base.GetPropertyDescriptors())
        {
            if (property.Name is nameof(Transparent) or nameof(TransparentBG)) yield return new PropertyDescriptor<PopupMenu, bool>(property.Name, m => property.Name == nameof(Transparent) ? m.Transparent : m.TransparentBG, (m, v) => { if (property.Name == nameof(Transparent)) m.Transparent = v; else m.TransparentBG = v; }, _ => true, stored: true);
            else if (property.Name == nameof(CanvasItemDefaultTextureFilter)) yield return new PropertyDescriptor<PopupMenu, DefaultCanvasItemTextureFilter>(property.Name, m => m.CanvasItemDefaultTextureFilter, (m, v) => m.CanvasItemDefaultTextureFilter = v, _ => DefaultCanvasItemTextureFilter.ParentNode, stored: true);
            else if (property.Name == nameof(CanvasItemDefaultTextureRepeat)) yield return new PropertyDescriptor<PopupMenu, DefaultCanvasItemTextureRepeat>(property.Name, m => m.CanvasItemDefaultTextureRepeat, (m, v) => m.CanvasItemDefaultTextureRepeat = v, _ => DefaultCanvasItemTextureRepeat.ParentNode, stored: true);
            else yield return property;
        }
        yield return new PropertyDescriptor<PopupMenu, bool>(nameof(AllowSearch), m => m.AllowSearch, (m, v) => m.AllowSearch = v, _ => true, stored: true);
        yield return new PropertyDescriptor<PopupMenu, bool>(nameof(HideOnCheckableItemSelection), m => m.HideOnCheckableItemSelection, (m, v) => m.HideOnCheckableItemSelection = v, _ => true, stored: true);
        yield return new PropertyDescriptor<PopupMenu, bool>(nameof(HideOnItemSelection), m => m.HideOnItemSelection, (m, v) => m.HideOnItemSelection = v, _ => true, stored: true);
        yield return new PropertyDescriptor<PopupMenu, bool>(nameof(HideOnStateItemSelection), m => m.HideOnStateItemSelection, (m, v) => m.HideOnStateItemSelection = v, _ => false, stored: true);
        yield return new PropertyDescriptor<PopupMenu, int>(nameof(ItemCount), m => m.ItemCount, (m, v) => m.ItemCount = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<PopupMenu, bool>(nameof(SearchBarEnabled), m => m.SearchBarEnabled, (m, v) => m.SearchBarEnabled = v, _ => false, stored: true);
        yield return new PropertyDescriptor<PopupMenu, bool>(nameof(SearchBarFuzzySearchEnabled), m => m.SearchBarFuzzySearchEnabled, (m, v) => m.SearchBarFuzzySearchEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<PopupMenu, int>(nameof(SearchBarFuzzySearchMaxMisses), m => m.SearchBarFuzzySearchMaxMisses, (m, v) => m.SearchBarFuzzySearchMaxMisses = v, _ => 2, stored: true);
        yield return new PropertyDescriptor<PopupMenu, int>(nameof(SearchBarMinItemCount), m => m.SearchBarMinItemCount, (m, v) => m.SearchBarMinItemCount = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<PopupMenu, bool>(nameof(ShrinkHeight), m => m.ShrinkHeight, (m, v) => m.ShrinkHeight = v, _ => true, stored: true);
        yield return new PropertyDescriptor<PopupMenu, bool>(nameof(ShrinkWidth), m => m.ShrinkWidth, (m, v) => m.ShrinkWidth = v, _ => true, stored: true);
        yield return new PropertyDescriptor<PopupMenu, double>(nameof(SubmenuPopupDelay), m => m.SubmenuPopupDelay, (m, v) => m.SubmenuPopupDelay = v, _ => .2, stored: true);
        for (var index = 0; index < _items.Count; index++)
        {
            var i = index;
            yield return new PropertyDescriptor<PopupMenu, string>($"item_{i}/text", m => m.GetItemText(i), (m, v) => m.SetItemText(i, v), _ => "", stored: true);
            yield return new PropertyDescriptor<PopupMenu, int>($"item_{i}/id", m => m.GetItemID(i), (m, v) => m.SetItemID(i, v), _ => 0, stored: true);
            yield return new PropertyDescriptor<PopupMenu, Texture?>($"item_{i}/icon", m => m.GetItemIcon(i), (m, v) => m.SetItemIcon(i, v), _ => null, stored: true);
            yield return new PropertyDescriptor<PopupMenu, bool>($"item_{i}/checked", m => m.IsItemChecked(i), (m, v) => m.SetItemChecked(i, v), _ => false, stored: true);
            yield return new PropertyDescriptor<PopupMenu, bool>($"item_{i}/disabled", m => m.IsItemDisabled(i), (m, v) => m.SetItemDisabled(i, v), _ => false, stored: true);
            yield return new PropertyDescriptor<PopupMenu, bool>($"item_{i}/separator", m => m.IsItemSeparator(i), (m, v) => m.SetItemAsSeparator(i, v), _ => false, stored: true);
            yield return new PropertyDescriptor<PopupMenu, int>($"item_{i}/checkable", m => m.Get(i).Checkable, (m, v) => { if (v is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(v)); if (v == 2) m.SetItemAsRadioCheckable(i, true); else m.SetItemAsCheckable(i, v == 1); }, _ => 0, stored: true);
        }
    }
}
