namespace Electron2D;

public partial class ItemList
{
    private static readonly PropertyDescriptor[] ListProperties =
    [
        new PropertyDescriptor<ItemList, int>(nameof(ItemCount), list => list.ItemCount, (list, value) => list.ItemCount = value, _ => 0, stored: true),
        new PropertyDescriptor<ItemList, SelectMode>(nameof(SelectionMode), list => list.SelectionMode, (list, value) => list.SelectionMode = value, _ => SelectMode.Single, stored: true),
        new PropertyDescriptor<ItemList, IconMode>(nameof(IconDisplayMode), list => list.IconDisplayMode, (list, value) => list.IconDisplayMode = value, _ => IconMode.Left, stored: true),
        new PropertyDescriptor<ItemList, int>(nameof(FixedColumnWidth), list => list.FixedColumnWidth, (list, value) => list.FixedColumnWidth = value, _ => 0, stored: true),
        new PropertyDescriptor<ItemList, int>(nameof(MaxColumns), list => list.MaxColumns, (list, value) => list.MaxColumns = value, _ => 1, stored: true),
        new PropertyDescriptor<ItemList, int>(nameof(MaxTextLines), list => list.MaxTextLines, (list, value) => list.MaxTextLines = value, _ => 1, stored: true),
        new PropertyDescriptor<ItemList, bool>(nameof(SameColumnWidth), list => list.SameColumnWidth, (list, value) => list.SameColumnWidth = value, _ => false, stored: true),
        new PropertyDescriptor<ItemList, bool>(nameof(AutoWidth), list => list.AutoWidth, (list, value) => list.AutoWidth = value, _ => false, stored: true),
        new PropertyDescriptor<ItemList, bool>(nameof(AutoHeight), list => list.AutoHeight, (list, value) => list.AutoHeight = value, _ => false, stored: true),
        new PropertyDescriptor<ItemList, bool>(nameof(WraparoundItems), list => list.WraparoundItems, (list, value) => list.WraparoundItems = value, _ => true, stored: true),
        new PropertyDescriptor<ItemList, Vector2i>(nameof(FixedIconSize), list => list.FixedIconSize, (list, value) => list.FixedIconSize = value, _ => Vector2i.Zero, stored: true),
        new PropertyDescriptor<ItemList, float>(nameof(IconScale), list => list.IconScale, (list, value) => list.IconScale = value, _ => 1, stored: true),
        new PropertyDescriptor<ItemList, TextOverrunBehavior>(nameof(TextOverrunBehavior), list => list.TextOverrunBehavior, (list, value) => list.TextOverrunBehavior = value, _ => global::Electron2D.TextOverrunBehavior.TrimEllipsis, stored: true),
        new PropertyDescriptor<ItemList, bool>(nameof(AllowReselect), list => list.AllowReselect, (list, value) => list.AllowReselect = value, _ => false, stored: true),
        new PropertyDescriptor<ItemList, bool>(nameof(AllowRMBSelect), list => list.AllowRMBSelect, (list, value) => list.AllowRMBSelect = value, _ => false, stored: true),
        new PropertyDescriptor<ItemList, bool>(nameof(AllowSearch), list => list.AllowSearch, (list, value) => list.AllowSearch = value, _ => true, stored: true),
        new PropertyDescriptor<ItemList, ScrollHintMode>(nameof(HintMode), list => list.HintMode, (list, value) => list.HintMode = value, _ => ScrollHintMode.Disabled, stored: true),
        new PropertyDescriptor<ItemList, bool>(nameof(TileScrollHint), list => list.TileScrollHint, (list, value) => list.TileScrollHint = value, _ => false, stored: true),
        new PropertyDescriptor<ItemList, bool>(nameof(ClipContents), list => list.ClipContents, (list, value) => list.ClipContents = value, _ => true, stored: true),
        new PropertyDescriptor<ItemList, FocusMode>(nameof(FocusMode), list => list.FocusMode, (list, value) => list.FocusMode = value, _ => FocusMode.All, stored: true)
    ];

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
            if (property.Name is not (nameof(ClipContents) or nameof(FocusMode))) yield return property;
        foreach (var property in ListProperties) yield return property;
        for (var index = 0; index < _items.Count; index++)
        {
            var itemIndex = index;
            var prefix = $"item_{index}/";
            yield return new PropertyDescriptor<ItemList, string>(prefix + "text", list => list.GetItemText(itemIndex),
                (list, value) => list.SetItemText(itemIndex, value), _ => string.Empty, stored: true);
            yield return new PropertyDescriptor<ItemList, Texture?>(prefix + "icon", list => list.GetItemIcon(itemIndex),
                (list, value) => list.SetItemIcon(itemIndex, value), _ => null, stored: true);
            yield return new PropertyDescriptor<ItemList, bool>(prefix + "selectable", list => list.IsItemSelectable(itemIndex),
                (list, value) => list.SetItemSelectable(itemIndex, value), _ => true, stored: true);
            yield return new PropertyDescriptor<ItemList, bool>(prefix + "disabled", list => list.IsItemDisabled(itemIndex),
                (list, value) => list.SetItemDisabled(itemIndex, value), _ => false, stored: true);
        }
    }

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(ItemList) ? CreateList : base.CreateSceneInstanceFactory();
    private static Node CreateList() => new ItemList();
}
