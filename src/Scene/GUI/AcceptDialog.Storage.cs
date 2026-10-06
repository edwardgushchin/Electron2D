namespace Electron2D;

public partial class AcceptDialog
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
        {
            var name = property.Name;
            if (name == nameof(Visible)) yield return new PropertyDescriptor<AcceptDialog, bool>(name, d => d.Visible, (d, v) => d.Visible = v, _ => false, stored: true);
            else if (name == nameof(Title)) yield return new PropertyDescriptor<AcceptDialog, string>(name, d => d.Title, (d, v) => d.Title = v, _ => "Alert!", stored: true);
            else if (name is nameof(Transient) or nameof(Exclusive) or nameof(WrapControls) or nameof(KeepTitleVisible) or nameof(MinimizeDisabled) or nameof(MaximizeDisabled) or nameof(InputEnabled))
                yield return new PropertyDescriptor<AcceptDialog, bool>(name, d => ReadDefault(d, name), (d, v) => WriteDefault(d, name, v), _ => true, stored: true);
            else yield return property;
        }
        yield return new PropertyDescriptor<AcceptDialog, string>(nameof(DialogText), d => d.DialogText, (d, v) => d.DialogText = v, _ => "", stored: true);
        yield return new PropertyDescriptor<AcceptDialog, string>(nameof(OKButtonText), d => d.OKButtonText, (d, v) => d.OKButtonText = v, _ => "", stored: true);
        yield return new PropertyDescriptor<AcceptDialog, bool>(nameof(DialogAutowrap), d => d.DialogAutowrap, (d, v) => d.DialogAutowrap = v, _ => false, stored: true);
        yield return new PropertyDescriptor<AcceptDialog, bool>(nameof(DialogHideOnOK), d => d.DialogHideOnOK, (d, v) => d.DialogHideOnOK = v, _ => true, stored: true);
        yield return new PropertyDescriptor<AcceptDialog, bool>(nameof(DialogCloseOnEscape), d => d.DialogCloseOnEscape, (d, v) => d.DialogCloseOnEscape = v, _ => true, stored: true);
    }
    private static bool ReadDefault(AcceptDialog d, string name) => name switch { nameof(Transient) => d.Transient, nameof(Exclusive) => d.Exclusive, nameof(WrapControls) => d.WrapControls, nameof(KeepTitleVisible) => d.KeepTitleVisible, nameof(MinimizeDisabled) => d.MinimizeDisabled, nameof(MaximizeDisabled) => d.MaximizeDisabled, _ => d.InputEnabled };
    private static void WriteDefault(AcceptDialog d, string name, bool value) { switch (name) { case nameof(Transient): d.Transient = value; break; case nameof(Exclusive): d.Exclusive = value; break; case nameof(WrapControls): d.WrapControls = value; break; case nameof(KeepTitleVisible): d.KeepTitleVisible = value; break; case nameof(MinimizeDisabled): d.MinimizeDisabled = value; break; case nameof(MaximizeDisabled): d.MaximizeDisabled = value; break; default: d.InputEnabled = value; break; } }
}
