# ColorPicker

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.ColorPicker`. **Base:** `Electron2D.VBoxContainer`. **Source:** [source](../../src/Scene/GUI/ColorPicker.cs). **Component:** [Color authoring](../components/color-authoring.md).

A color editor connecting six spatial shapes, shared SpinBox/slider channels, intensity, numeric/hex text, local swatches, palette files and completed application-viewport sampling. Programmatic color writes are silent. The component defines input, lifecycle, theme and verification limits.


## Constructors

| Complete declaration | Contract |
| --- | --- |
| `public ColorPicker()` | Creates a white RGB editor with all sections enabled and an HSV rectangle. |

## Properties

| Complete declaration | Contract |
| --- | --- |
| `public System.Boolean CanAddSwatches { get; set; }` | Gets or sets user addition/removal of swatches. True initially; programmatic preset methods remain available. |
| `public Electron2D.Color Color { get; set; }` | Gets or sets the selected finite color without publishing ColorChanged. Opaque white initially; signed and HDR channels are retained. |
| `public Electron2D.ColorPicker.ColorModeType ColorMode { get; set; }` | Gets or sets the numeric channel mode without changing the color. RGB initially. |
| `public System.Boolean ColorModesVisible { get; set; }` | Gets or sets visibility of the mode controls. True initially. |
| `public System.Boolean DeferredMode { get; set; }` | Gets or sets publishing slider and surface edits only at the end of a gesture. False initially; programmatic writes remain silent. |
| `public System.Boolean EditAlpha { get; set; }` | Gets or sets alpha channel visibility. True initially; hidden alpha is preserved. |
| `public System.Boolean EditIntensity { get; set; }` | Gets or sets linear exposure editing. True initially; exposure multiplies linear RGB by two to the intensity power. |
| `public System.Boolean HexVisible { get; set; }` | Gets or sets visibility of color text controls. True initially. |
| `public Electron2D.ColorPicker.PickerShapeType PickerShape { get; set; }` | Gets or sets the spatial editor shape without changing the color. HSVRectangle initially. |
| `public System.Boolean PresetsVisible { get; set; }` | Gets or sets visibility of local and recent swatches. True initially. |
| `public System.Boolean SamplerVisible { get; set; }` | Gets or sets visibility of the sample, sampler and shape menu. True initially. |
| `public System.Boolean SlidersVisible { get; set; }` | Gets or sets visibility of channel and intensity controls. True initially. |

## Events

| Complete declaration | Contract |
| --- | --- |
| `public event System.Action<Electron2D.Color> ColorChanged` | Occurs after a user color edit has committed and refreshed the editor. |
| `public event System.Action<Electron2D.Color> PresetAdded` | Occurs after the user adds the current color to local swatches. |
| `public event System.Action<Electron2D.Color> PresetRemoved` | Occurs after the user removes a local swatch. |

## Members and lifecycle

| Complete declaration | Contract |
| --- | --- |
| `public System.Void AddPreset(Electron2D.Color color)` | Adds a local swatch, moving an existing exact color to the end; no user event is emitted. Finite color, including alpha and HDR channels. |
| `public System.Void AddRecentPreset(Electron2D.Color color)` | Adds a recent swatch unless already present; at most nine colors are retained, oldest first. Finite color. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Extends the inherited typed lifecycle contract. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Extends the inherited typed lifecycle contract. |
| `public System.Void ErasePreset(Electron2D.Color color)` | Removes an exact local swatch, if present, without a user event. Color to remove. |
| `public System.Void EraseRecentPreset(Electron2D.Color color)` | Removes an exact recent swatch, if present. Color to remove. |
| `public Electron2D.Color[] GetPresets()` | Returns a caller-owned copy of local swatches in insertion order. An independent array. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Extends the inherited typed lifecycle contract. |
| `public Electron2D.Color[] GetRecentPresets()` | Returns a caller-owned copy of recent swatches, oldest first. An independent array containing at most nine colors. |
| `protected override System.Void OnNotification(System.Int32 what)` | Extends the inherited typed lifecycle contract. |
