namespace Electron2D;

public sealed partial class TreeItem
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<TreeItem, bool>(nameof(Collapsed), item => item.Collapsed, (item, value) => item.Collapsed = value, _ => false);
        yield return new PropertyDescriptor<TreeItem, bool>(nameof(Visible), item => item.Visible, (item, value) => item.Visible = value, _ => true);
        yield return new PropertyDescriptor<TreeItem, bool>(nameof(DisableFolding), item => item.DisableFolding, (item, value) => item.DisableFolding = value, _ => false);
        yield return new PropertyDescriptor<TreeItem, int>(nameof(CustomMinimumHeight), item => item.CustomMinimumHeight, (item, value) => item.CustomMinimumHeight = value, _ => 0);
    }
}
