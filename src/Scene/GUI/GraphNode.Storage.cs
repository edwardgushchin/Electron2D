namespace Electron2D;

public partial class GraphNode
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var p in base.GetPropertyDescriptors())
        {
            if (p.Name == nameof(MouseFilter)) yield return new PropertyDescriptor<GraphNode, MouseFilter>(p.Name, n => n.MouseFilter, (n, v) => n.MouseFilter = v, _ => MouseFilter.Stop, stored: true);
            else if (p.Name == nameof(FocusMode)) yield return new PropertyDescriptor<GraphNode, FocusMode>(p.Name, n => n.FocusMode, (n, v) => n.FocusMode = v, _ => FocusMode.Accessibility, stored: true);
            else yield return p;
        }
        yield return new PropertyDescriptor<GraphNode, string>(nameof(Title), n => n.Title, (n, v) => n.Title = v, _ => "", stored: true);
        yield return new PropertyDescriptor<GraphNode, bool>(nameof(IgnoreInvalidConnectionType), n => n.IgnoreInvalidConnectionType, (n, v) => n.IgnoreInvalidConnectionType = v, _ => false, stored: true);
        yield return new PropertyDescriptor<GraphNode, FocusMode>(nameof(SlotsFocusMode), n => n.SlotsFocusMode, (n, v) => n.SlotsFocusMode = v, _ => FocusMode.Accessibility, stored: true);
        yield return new PropertyDescriptor<GraphNode, int>("slot_count", n => n._storedSlots, (n, v) => { if (v < 0 || v > 65536) throw new ArgumentOutOfRangeException(nameof(v)); n._storedSlots = v; n.NotifyPropertyListChanged(); }, _ => 0, stored: true);
        for (var i = 0; i < _storedSlots; i++)
        {
            var index = i; var prefix = "slot/" + i + "/";
            yield return new PropertyDescriptor<GraphNode, bool>(prefix + "left_enabled", n => n.IsSlotEnabledLeft(index), (n, v) => n.SetSlotEnabledLeft(index, v), _ => false, stored: true);
            yield return new PropertyDescriptor<GraphNode, int>(prefix + "left_type", n => n.GetSlotTypeLeft(index), (n, v) => n.SetSlotTypeLeft(index, v), _ => 0, stored: true);
            yield return new PropertyDescriptor<GraphNode, Color>(prefix + "left_color", n => n.GetSlotColorLeft(index), (n, v) => n.SetSlotColorLeft(index, v), _ => Colors.White, stored: true);
            yield return new PropertyDescriptor<GraphNode, Texture?>(prefix + "left_customicon", n => n.GetSlotCustomIconLeft(index), (n, v) => n.SetSlotCustomIconLeft(index, v), _ => null, stored: true);
            yield return new PropertyDescriptor<GraphNode, bool>(prefix + "right_enabled", n => n.IsSlotEnabledRight(index), (n, v) => n.SetSlotEnabledRight(index, v), _ => false, stored: true);
            yield return new PropertyDescriptor<GraphNode, int>(prefix + "right_type", n => n.GetSlotTypeRight(index), (n, v) => n.SetSlotTypeRight(index, v), _ => 0, stored: true);
            yield return new PropertyDescriptor<GraphNode, Color>(prefix + "right_color", n => n.GetSlotColorRight(index), (n, v) => n.SetSlotColorRight(index, v), _ => Colors.White, stored: true);
            yield return new PropertyDescriptor<GraphNode, Texture?>(prefix + "right_customicon", n => n.GetSlotCustomIconRight(index), (n, v) => n.SetSlotCustomIconRight(index, v), _ => null, stored: true);
            yield return new PropertyDescriptor<GraphNode, bool>(prefix + "draw_stylebox", n => n.IsSlotDrawStylebox(index), (n, v) => n.SetSlotDrawStylebox(index, v), _ => true, stored: true);
        }
    }
}
