namespace Electron2D;

public partial class OptionButton
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
        {
            if (property.Name is nameof(Text) or nameof(Icon)) continue;
            if (property.Name == nameof(Alignment)) yield return new PropertyDescriptor<OptionButton, HorizontalAlignment>(nameof(Alignment), b => b.Alignment, (b, v) => b.Alignment = v, _ => HorizontalAlignment.Left, stored: true);
            else if (property.Name == nameof(ToggleMode)) yield return new PropertyDescriptor<OptionButton, bool>(nameof(ToggleMode), b => b.ToggleMode, (b, v) => b.ToggleMode = v, _ => true, stored: true);
            else if (property.Name == nameof(ActionMode)) yield return new PropertyDescriptor<OptionButton, ButtonActionMode>(nameof(ActionMode), b => b.ActionMode, (b, v) => b.ActionMode = v, _ => ButtonActionMode.ButtonPress, stored: true);
            else yield return property;
        }
        yield return new PropertyDescriptor<OptionButton, bool>(nameof(AllowReselect), b => b.AllowReselect, (b, v) => b.AllowReselect = v, _ => false, stored: true);
        yield return new PropertyDescriptor<OptionButton, bool>(nameof(FitToLongestItem), b => b.FitToLongestItem, (b, v) => b.FitToLongestItem = v, _ => true, stored: true);
        yield return new PropertyDescriptor<OptionButton, bool>(nameof(SearchBarEnabled), b => b.SearchBarEnabled, (b, v) => b.SearchBarEnabled = v, _ => false, stored: true);
        yield return new PropertyDescriptor<OptionButton, bool>(nameof(SearchBarFuzzySearchEnabled), b => b.SearchBarFuzzySearchEnabled, (b, v) => b.SearchBarFuzzySearchEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<OptionButton, int>(nameof(SearchBarFuzzySearchMaxMisses), b => b.SearchBarFuzzySearchMaxMisses, (b, v) => b.SearchBarFuzzySearchMaxMisses = v, _ => 2, stored: true);
        yield return new PropertyDescriptor<OptionButton, int>(nameof(SearchBarMinItemCount), b => b.SearchBarMinItemCount, (b, v) => b.SearchBarMinItemCount = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<OptionButton, int>(nameof(ItemCount), b => b.ItemCount, (b, v) => b.ItemCount = v, _ => 0, stored: true);
        for (var index = 0; index < _popup.ItemCount; index++)
        {
            var i = index;
            yield return new PropertyDescriptor<OptionButton, string>($"popup/item_{i}/text", b => b.GetItemText(i), (b, v) => b.SetItemText(i, v), _ => "", stored: true);
            yield return new PropertyDescriptor<OptionButton, Texture?>($"popup/item_{i}/icon", b => b.GetItemIcon(i), (b, v) => b.SetItemIcon(i, v), _ => null, stored: true);
            yield return new PropertyDescriptor<OptionButton, int>($"popup/item_{i}/id", b => b.GetItemID(i), (b, v) => b.SetItemID(i, v), _ => 0, stored: true);
            yield return new PropertyDescriptor<OptionButton, bool>($"popup/item_{i}/disabled", b => b.IsItemDisabled(i), (b, v) => b.SetItemDisabled(i, v), _ => false, stored: true);
            yield return new PropertyDescriptor<OptionButton, bool>($"popup/item_{i}/separator", b => b.IsItemSeparator(i), (b, v) => b._popup.SetItemAsSeparator(i, v), _ => false, stored: true);
        }
        yield return new PropertyDescriptor<OptionButton, int>(nameof(Selected), b => b.Selected, (b, v) => b.Select(v), _ => -1, stored: true);
    }
}
