namespace Electron2D;

public partial class SpinBox
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
        {
            if (property.Name == nameof(ExpEdit)) continue;
            if (property.Name == nameof(Step)) yield return new PropertyDescriptor<SpinBox, double>(nameof(Step), s => s.Step, (s, v) => s.Step = v, _ => 1, stored: true);
            else if (property.Name == nameof(SizeFlagsVertical)) yield return new PropertyDescriptor<SpinBox, SizeFlags>(nameof(SizeFlagsVertical), s => s.SizeFlagsVertical, (s, v) => s.SizeFlagsVertical = v, _ => SizeFlags.Fill, stored: true);
            else yield return property;
        }
        yield return new PropertyDescriptor<SpinBox, HorizontalAlignment>(nameof(Alignment), s => s.Alignment, (s, v) => s.Alignment = v, _ => HorizontalAlignment.Left, stored: true);
        yield return new PropertyDescriptor<SpinBox, bool>(nameof(Editable), s => s.Editable, (s, v) => s.Editable = v, _ => true, stored: true);
        yield return new PropertyDescriptor<SpinBox, bool>(nameof(UpdateOnTextChanged), s => s.UpdateOnTextChanged, (s, v) => s.UpdateOnTextChanged = v, _ => false, stored: true);
        yield return new PropertyDescriptor<SpinBox, string>(nameof(Prefix), s => s.Prefix, (s, v) => s.Prefix = v, _ => "", stored: true);
        yield return new PropertyDescriptor<SpinBox, string>(nameof(Suffix), s => s.Suffix, (s, v) => s.Suffix = v, _ => "", stored: true);
        yield return new PropertyDescriptor<SpinBox, double>(nameof(CustomArrowStep), s => s.CustomArrowStep, (s, v) => s.CustomArrowStep = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<SpinBox, bool>(nameof(CustomArrowRound), s => s.CustomArrowRound, (s, v) => s.CustomArrowRound = v, _ => false, stored: true);
        yield return new PropertyDescriptor<SpinBox, bool>(nameof(SelectAllOnFocus), s => s.SelectAllOnFocus, (s, v) => s.SelectAllOnFocus = v, _ => false, stored: true);
    }
}
