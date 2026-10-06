# ColorPicker.PickerShapeType

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.ColorPicker.PickerShapeType`. **Base:** `System.Enum`. **Source:** [source](../../src/Scene/GUI/ColorPicker.cs). **Component:** [Color authoring](../components/color-authoring.md).

Spatial editing identities, retaining the None=4 gap before perceptual rectangles=5/6. None hides the surface.


## Members and lifecycle

| Complete declaration | Contract |
| --- | --- |
| `public const Electron2D.ColorPicker.PickerShapeType HSVRectangle = 0` | Saturation/value rectangle and hue bar. |
| `public const Electron2D.ColorPicker.PickerShapeType HSVWheel = 1` | Hue ring surrounding a saturation/value square. |
| `public const Electron2D.ColorPicker.PickerShapeType None = 4` | Hides the surface and its shape selector. |
| `public const Electron2D.ColorPicker.PickerShapeType OKHLRectangle = 6` | Perceptual hue/lightness rectangle and saturation bar. |
| `public const Electron2D.ColorPicker.PickerShapeType OKHSLCircle = 3` | Perceptual hue/saturation circle and lightness bar. |
| `public const Electron2D.ColorPicker.PickerShapeType OKHSRectangle = 5` | Perceptual hue/saturation rectangle and lightness bar. |
| `public const Electron2D.ColorPicker.PickerShapeType VHSCircle = 2` | Hue/saturation circle and value bar. |
