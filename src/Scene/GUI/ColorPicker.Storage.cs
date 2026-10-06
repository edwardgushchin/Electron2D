namespace Electron2D;

public partial class ColorPicker
{
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<ColorPicker, Color>(nameof(Color), p => p.Color, (p, v) => p.Color = v, _ => Colors.White, stored: true);
        yield return new PropertyDescriptor<ColorPicker, ColorModeType>(nameof(ColorMode), p => p.ColorMode, (p, v) => p.ColorMode = v, _ => ColorModeType.RGB, stored: true);
        yield return new PropertyDescriptor<ColorPicker, PickerShapeType>(nameof(PickerShape), p => p.PickerShape, (p, v) => p.PickerShape = v, _ => PickerShapeType.HSVRectangle, stored: true);
        yield return new PropertyDescriptor<ColorPicker, bool>(nameof(EditAlpha), p => p.EditAlpha, (p, v) => p.EditAlpha = v, _ => true, stored: true);
        yield return new PropertyDescriptor<ColorPicker, bool>(nameof(EditIntensity), p => p.EditIntensity, (p, v) => p.EditIntensity = v, _ => true, stored: true);
        yield return new PropertyDescriptor<ColorPicker, bool>(nameof(DeferredMode), p => p.DeferredMode, (p, v) => p.DeferredMode = v, _ => false, stored: true);
        yield return new PropertyDescriptor<ColorPicker, bool>(nameof(ColorModesVisible), p => p.ColorModesVisible, (p, v) => p.ColorModesVisible = v, _ => true, stored: true);
        yield return new PropertyDescriptor<ColorPicker, bool>(nameof(SlidersVisible), p => p.SlidersVisible, (p, v) => p.SlidersVisible = v, _ => true, stored: true);
        yield return new PropertyDescriptor<ColorPicker, bool>(nameof(HexVisible), p => p.HexVisible, (p, v) => p.HexVisible = v, _ => true, stored: true);
        yield return new PropertyDescriptor<ColorPicker, bool>(nameof(SamplerVisible), p => p.SamplerVisible, (p, v) => p.SamplerVisible = v, _ => true, stored: true);
        yield return new PropertyDescriptor<ColorPicker, bool>(nameof(PresetsVisible), p => p.PresetsVisible, (p, v) => p.PresetsVisible = v, _ => true, stored: true);
        yield return new PropertyDescriptor<ColorPicker, bool>(nameof(CanAddSwatches), p => p.CanAddSwatches, (p, v) => p.CanAddSwatches = v, _ => true, stored: true);
    }
}
