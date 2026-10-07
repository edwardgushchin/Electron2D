namespace Electron2D;

public partial class Tree
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
        {
            if (property.Name == nameof(FocusMode)) yield return new PropertyDescriptor<Tree, FocusMode>(nameof(FocusMode), t => t.FocusMode, (t, v) => t.FocusMode = v, _ => FocusMode.All, stored: true);
            else if (property.Name == nameof(ClipContents)) yield return new PropertyDescriptor<Tree, bool>(nameof(ClipContents), t => t.ClipContents, (t, v) => t.ClipContents = v, _ => true, stored: true);
            else yield return property;
        }
        yield return new PropertyDescriptor<Tree, bool>(nameof(AllowReselect), t => t.AllowReselect, (t, v) => t.AllowReselect = v, _ => false, stored: true);
        yield return new PropertyDescriptor<Tree, bool>(nameof(AllowRMBSelect), t => t.AllowRMBSelect, (t, v) => t.AllowRMBSelect = v, _ => false, stored: true);
        yield return new PropertyDescriptor<Tree, bool>(nameof(AllowSearch), t => t.AllowSearch, (t, v) => t.AllowSearch = v, _ => true, stored: true);
        yield return new PropertyDescriptor<Tree, bool>(nameof(AutoTooltip), t => t.AutoTooltip, (t, v) => t.AutoTooltip = v, _ => true, stored: true);
        yield return new PropertyDescriptor<Tree, bool>(nameof(ColumnTitlesVisible), t => t.ColumnTitlesVisible, (t, v) => t.ColumnTitlesVisible = v, _ => false, stored: true);
        yield return new PropertyDescriptor<Tree, int>(nameof(Columns), t => t.Columns, (t, v) => t.Columns = v, _ => 1, stored: true);
        yield return new PropertyDescriptor<Tree, TreeDropModeFlags>(nameof(DropModeFlags), t => t.DropModeFlags, (t, v) => t.DropModeFlags = v, _ => TreeDropModeFlags.Disabled, stored: true);
        yield return new PropertyDescriptor<Tree, bool>(nameof(EnableDragUnfolding), t => t.EnableDragUnfolding, (t, v) => t.EnableDragUnfolding = v, _ => true, stored: true);
        yield return new PropertyDescriptor<Tree, bool>(nameof(EnableRecursiveFolding), t => t.EnableRecursiveFolding, (t, v) => t.EnableRecursiveFolding = v, _ => true, stored: true);
        yield return new PropertyDescriptor<Tree, bool>(nameof(HideFolding), t => t.HideFolding, (t, v) => t.HideFolding = v, _ => false, stored: true);
        yield return new PropertyDescriptor<Tree, bool>(nameof(HideRoot), t => t.HideRoot, (t, v) => t.HideRoot = v, _ => false, stored: true);
        yield return new PropertyDescriptor<Tree, VerticalScrollHintMode>(nameof(HintMode), t => t.HintMode, (t, v) => t.HintMode = v, _ => VerticalScrollHintMode.Disabled, stored: true);
        yield return new PropertyDescriptor<Tree, bool>(nameof(ScrollHorizontalEnabled), t => t.ScrollHorizontalEnabled, (t, v) => t.ScrollHorizontalEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<Tree, bool>(nameof(ScrollVerticalEnabled), t => t.ScrollVerticalEnabled, (t, v) => t.ScrollVerticalEnabled = v, _ => true, stored: true);
        yield return new PropertyDescriptor<Tree, SelectMode>(nameof(SelectionMode), t => t.SelectionMode, (t, v) => t.SelectionMode = v, _ => SelectMode.Single, stored: true);
        yield return new PropertyDescriptor<Tree, bool>(nameof(TileScrollHint), t => t.TileScrollHint, (t, v) => t.TileScrollHint = v, _ => false, stored: true);
        for (var i = 0; i < _columns.Count; i++)
        {
            var index = i; var prefix = "column/" + i + "/";
            yield return new PropertyDescriptor<Tree, int>(prefix + "minimum_width", t => t.AtColumn(index).Minimum, (t, v) => t.SetColumnCustomMinimumWidth(index, v), _ => 0, stored: true);
            yield return new PropertyDescriptor<Tree, int>(prefix + "expand_ratio", t => t.GetColumnExpandRatio(index), (t, v) => t.SetColumnExpandRatio(index, v), _ => 1, stored: true);
            yield return new PropertyDescriptor<Tree, bool>(prefix + "expand", t => t.IsColumnExpanding(index), (t, v) => t.SetColumnExpand(index, v), _ => true, stored: true);
            yield return new PropertyDescriptor<Tree, bool>(prefix + "clip", t => t.IsColumnClippingContent(index), (t, v) => t.SetColumnClipContent(index, v), _ => false, stored: true);
            yield return new PropertyDescriptor<Tree, string>(prefix + "title", t => t.GetColumnTitle(index), (t, v) => t.SetColumnTitle(index, v), _ => "", stored: true);
            yield return new PropertyDescriptor<Tree, string>(prefix + "tooltip", t => t.GetColumnTitleTooltipText(index), (t, v) => t.SetColumnTitleTooltipText(index, v), _ => "", stored: true);
            yield return new PropertyDescriptor<Tree, string>(prefix + "language", t => t.GetColumnTitleLanguage(index), (t, v) => t.SetColumnTitleLanguage(index, v), _ => "", stored: true);
            yield return new PropertyDescriptor<Tree, HorizontalAlignment>(prefix + "alignment", t => t.GetColumnTitleAlignment(index), (t, v) => t.SetColumnTitleAlignment(index, v), _ => HorizontalAlignment.Center, stored: true);
            yield return new PropertyDescriptor<Tree, TextDirection>(prefix + "direction", t => t.GetColumnTitleDirection(index), (t, v) => t.SetColumnTitleDirection(index, v), _ => TextDirection.Inherited, stored: true);
        }
    }
}
