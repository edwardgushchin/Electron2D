# ColorPicker.ColorModeType

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.ColorPicker.ColorModeType`. **Base:** `System.Enum`. **Source:** [source](../../src/Scene/GUI/ColorPicker.cs). **Component:** [Color authoring](../components/color-authoring.md).

Typed numeric channel encoding; RGB=0, HSV=1, Linear=2, OKHSL=3. Deprecated modes are omitted.


## Members and lifecycle

| Complete declaration | Contract |
| --- | --- |
| `public const Electron2D.ColorPicker.ColorModeType HSV = 1` | Hue in degrees and saturation/value in percent. |
| `public const Electron2D.ColorPicker.ColorModeType Linear = 2` | Linear RGB channels in the unit range. |
| `public const Electron2D.ColorPicker.ColorModeType OKHSL = 3` | Perceptual OKHSL hue in degrees and saturation/lightness in percent. |
| `public const Electron2D.ColorPicker.ColorModeType RGB = 0` | Nonlinear sRGB channels in the range zero through 255. |
