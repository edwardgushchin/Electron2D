namespace Electron2D;

public partial class MenuButton
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
        {
            var name = property.Name;
            if (name == nameof(ShortcutInputEnabled)) yield return new PropertyDescriptor<MenuButton, bool>(name, b => b.ShortcutInputEnabled, (b, v) => b.ShortcutInputEnabled = v, _ => true, stored: true);
            else if (name == nameof(Flat)) yield return new PropertyDescriptor<MenuButton, bool>(name, b => b.Flat, (b, v) => b.Flat = v, _ => true, stored: true);
            else if (name == nameof(ToggleMode)) yield return new PropertyDescriptor<MenuButton, bool>(name, b => b.ToggleMode, (b, v) => b.ToggleMode = v, _ => true, stored: true);
            else if (name == nameof(ActionMode)) yield return new PropertyDescriptor<MenuButton, ButtonActionMode>(name, b => b.ActionMode, (b, v) => b.ActionMode = v, _ => ButtonActionMode.ButtonPress, stored: true);
            else if (name == nameof(FocusMode)) yield return new PropertyDescriptor<MenuButton, FocusMode>(name, b => b.FocusMode, (b, v) => b.FocusMode = v, _ => FocusMode.Accessibility, stored: true);
            else yield return property;
        }
        yield return new PropertyDescriptor<MenuButton, bool>(nameof(SwitchOnHover), b => b.SwitchOnHover, (b, v) => b.SwitchOnHover = v, _ => false, stored: true);
        yield return new PropertyDescriptor<MenuButton, int>(nameof(ItemCount), b => b.ItemCount, (b, v) => b.ItemCount = v, _ => 0, stored: true);
        for (var index = 0; index < _popup.ItemCount; index++)
        {
            var i = index;
            yield return new PropertyDescriptor<MenuButton, string>($"popup/item_{i}/text", b => b._popup.GetItemText(i), (b, v) => b._popup.SetItemText(i, v), _ => "", stored: true);
            yield return new PropertyDescriptor<MenuButton, Texture?>($"popup/item_{i}/icon", b => b._popup.GetItemIcon(i), (b, v) => b._popup.SetItemIcon(i, v), _ => null, stored: true);
            yield return new PropertyDescriptor<MenuButton, int>($"popup/item_{i}/id", b => b._popup.GetItemID(i), (b, v) => b._popup.SetItemID(i, v), _ => 0, stored: true);
            yield return new PropertyDescriptor<MenuButton, bool>($"popup/item_{i}/disabled", b => b._popup.IsItemDisabled(i), (b, v) => b._popup.SetItemDisabled(i, v), _ => false, stored: true);
            yield return new PropertyDescriptor<MenuButton, bool>($"popup/item_{i}/separator", b => b._popup.IsItemSeparator(i), (b, v) => b._popup.SetItemAsSeparator(i, v), _ => false, stored: true);
            yield return new PropertyDescriptor<MenuButton, bool>($"popup/item_{i}/checked", b => b._popup.IsItemChecked(i), (b, v) => b._popup.SetItemChecked(i, v), _ => false, stored: true);
            yield return new PropertyDescriptor<MenuButton, int>($"popup/item_{i}/checkable", b => b._popup.IsItemRadioCheckable(i) ? 2 : b._popup.IsItemCheckable(i) ? 1 : 0, (b, v) => { if (v is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(v)); if (v == 2) b._popup.SetItemAsRadioCheckable(i, true); else b._popup.SetItemAsCheckable(i, v == 1); }, _ => 0, stored: true);
        }
    }
}
