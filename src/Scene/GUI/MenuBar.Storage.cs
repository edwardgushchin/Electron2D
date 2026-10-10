namespace Electron2D;

public partial class MenuBar
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
        {
            if (property.Name == nameof(FocusMode)) yield return new PropertyDescriptor<MenuBar, FocusMode>(property.Name, b => b.FocusMode, (b, v) => b.FocusMode = v, _ => FocusMode.Accessibility, stored: true);
            else if (property.Name == nameof(ShortcutInputEnabled)) yield return new PropertyDescriptor<MenuBar, bool>(property.Name, b => b.ShortcutInputEnabled, (b, v) => b.ShortcutInputEnabled = v, _ => true, stored: true);
            else yield return property;
        }
        yield return new PropertyDescriptor<MenuBar, bool>(nameof(Flat), b => b.Flat, (b, v) => b.Flat = v, _ => false, stored: true);
        yield return new PropertyDescriptor<MenuBar, string>(nameof(Language), b => b.Language, (b, v) => b.Language = v, _ => "", stored: true);
        yield return new PropertyDescriptor<MenuBar, TextDirection>(nameof(TextDirection), b => b.TextDirection, (b, v) => b.TextDirection = v, _ => TextDirection.Auto, stored: true);
        yield return new PropertyDescriptor<MenuBar, bool>(nameof(SwitchOnHover), b => b.SwitchOnHover, (b, v) => b.SwitchOnHover = v, _ => true, stored: true);
    }
}
