# Color

Last updated: 2026-09-23

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Color.cs`](../../src/Core/Math/Color.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public struct Color`

> Represents a color using floating-point red, green, blue, and alpha components.

## Description

Represents a color using floating-point red, green, blue, and alpha components.

`Color` is a mutable 16-byte RGBA value with four sequential `float` fields. It represents ordinary nonlinear sRGB color, linear alpha, overbright/HDR intermediate values, HSV/OKHSL conversions, straight-alpha composition, packed integers, HTML-style text, and exact or approximate comparisons. It owns no resources, identity, handles, callbacks, or managed reference state and does not derive from `ElectronObject`.

Method and property names keep acronyms uppercase: `HTML`, `SRGB`, `HSV`, `OKHSL`, `RGBE`, `ABGR`, `ARGB` and `RGBA`. The `OKHSLH`, `OKHSLS` and `OKHSLL` properties append the hue, saturation or lightness component letter to `OKHSL`.

The zero-initialized value and `new Color()` are transparent black `(0, 0, 0, 0)`. `Colors.Black` is opaque black and `Colors.Transparent` is transparent white. `new Color(r, g, b)` defaults alpha to one. `new Color(existing)` is the RGB-plus-replacement-alpha constructor and therefore also defaults alpha to one; normal assignment copies all four components.

Components usually range from `0` to `1`, but values outside that range are retained for
overbright and high-dynamic-range calculations. RGB components are normally nonlinear sRGB values;
alpha is always linear. The zero-initialized value is transparent black.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var tint = new Color(0.2f, 0.6f, 1f, 1f);
var html = tint.ToHTML();
```

## Constructors

| Member | Description |
| --- | --- |
| [`public Color(float r, float g, float b, float a = 1f)`](#m-electron2d-color-ctor-system-single-system-single-system-single-system-single) | Initializes an RGBA color from floating-point components. |
| [`public Color(Color color, float alpha = 1f)`](#m-electron2d-color-ctor-electron2d-color-system-single) | Initializes a color from another color's RGB components and a replacement alpha value. |
| [`public Color(uint rgba)`](#m-electron2d-color-ctor-system-uint32) | Initializes a color from packed `0xRRGGBBAA` bytes. |
| [`public Color(ulong rgba)`](#m-electron2d-color-ctor-system-uint64) | Initializes a color from packed `0xRRRRGGGGBBBBAAAA` words. |
| [`public Color(string code)`](#m-electron2d-color-ctor-system-string) | Initializes a color from an HTML hexadecimal code or a standard color name. |
| [`public Color(string code, float alpha)`](#m-electron2d-color-ctor-system-string-system-single) | Initializes a color from an HTML hexadecimal code or standard name and replaces its alpha. |

## Properties

| Member | Description |
| --- | --- |
| [`public int R8 { get; set; }`](#p-electron2d-color-r8) | Gets or sets [`Color.R`](Color.md#f-electron2d-color-r) on an integer scale where `255` represents `1`. |
| [`public int G8 { get; set; }`](#p-electron2d-color-g8) | Gets or sets [`Color.G`](Color.md#f-electron2d-color-g) on an integer scale where `255` represents `1`. |
| [`public int B8 { get; set; }`](#p-electron2d-color-b8) | Gets or sets [`Color.B`](Color.md#f-electron2d-color-b) on an integer scale where `255` represents `1`. |
| [`public int A8 { get; set; }`](#p-electron2d-color-a8) | Gets or sets [`Color.A`](Color.md#f-electron2d-color-a) on an integer scale where `255` represents `1`. |
| [`public float H { get; set; }`](#p-electron2d-color-h) | Gets or sets the HSV hue, typically from `0` to `1`. |
| [`public float S { get; set; }`](#p-electron2d-color-s) | Gets or sets the HSV saturation, typically from `0` to `1`. |
| [`public float V { get; set; }`](#p-electron2d-color-v) | Gets or sets the HSV value component, typically from `0` to `1`. |
| [`public float OKHSLH { get; set; }`](#p-electron2d-color-okhslh) | Gets or sets the perceptual OKHSL hue, from `0` to `1`. |
| [`public float OKHSLS { get; set; }`](#p-electron2d-color-okhsls) | Gets or sets the perceptual OKHSL saturation, from `0` to `1`. |
| [`public float OKHSLL { get; set; }`](#p-electron2d-color-okhsll) | Gets or sets the perceptual OKHSL lightness, from `0` to `1`. |
| [`public float Luminance { get; }`](#p-electron2d-color-luminance) | Gets the relative light intensity of a linear-space RGB color. |
| [`public float this[int index] { get; set; }`](#p-electron2d-color-item-system-int32) | Gets or sets a component by RGBA index. |

## Methods

| Member | Description |
| --- | --- |
| [`public Color Blend(Color over)`](#m-electron2d-color-blend-electron2d-color) | Blends `over` as a foreground color over this background color. |
| [`public Color Clamp(Color? min = null, Color? max = null)`](#m-electron2d-color-clamp-system-nullable-electron2d-color-system-nullable-electron2d-color) | Clamps each component between the corresponding components of two colors. |
| [`public Color Darkened(float amount)`](#m-electron2d-color-darkened-system-single) | Darkens the RGB components by a ratio while preserving alpha. |
| [`public Color Inverted()`](#m-electron2d-color-inverted) | Inverts the RGB components while preserving alpha. |
| [`public Color Lightened(float amount)`](#m-electron2d-color-lightened-system-single) | Lightens the RGB components toward one by a ratio while preserving alpha. |
| [`public Color Lerp(Color to, float weight)`](#m-electron2d-color-lerp-electron2d-color-system-single) | Linearly interpolates every component toward another color. |
| [`public Color LinearToSRGB()`](#m-electron2d-color-lineartosrgb) | Converts linear RGB components to nonlinear sRGB while preserving alpha. |
| [`public Color SRGBToLinear()`](#m-electron2d-color-srgbtolinear) | Converts nonlinear sRGB components to linear RGB while preserving alpha. |
| [`public uint ToABGR32()`](#m-electron2d-color-toabgr32) | Packs the color into `0xAABBGGRR`. |
| [`public ulong ToABGR64()`](#m-electron2d-color-toabgr64) | Packs the color into `0xAAAABBBBGGGGRRRR`. |
| [`public uint ToARGB32()`](#m-electron2d-color-toargb32) | Packs the color into `0xAARRGGBB`. |
| [`public ulong ToARGB64()`](#m-electron2d-color-toargb64) | Packs the color into `0xAAAARRRRGGGGBBBB`. |
| [`public uint ToRGBA32()`](#m-electron2d-color-torgba32) | Packs the color into `0xRRGGBBAA`. |
| [`public ulong ToRGBA64()`](#m-electron2d-color-torgba64) | Packs the color into `0xRRRRGGGGBBBBAAAA`. |
| [`public string ToHTML(bool includeAlpha = true)`](#m-electron2d-color-tohtml-system-boolean) | Formats the color as lowercase hexadecimal RGBA or RGB without a leading hash sign. |
| [`public static Color FromHTML(ReadOnlySpan<char> rgba)`](#m-electron2d-color-fromhtml-system-readonlyspan-system-char) | Parses a color from HTML-style hexadecimal RGB or RGBA text. |
| [`public static Color Color8(byte r8, byte g8, byte b8, byte a8 = 255)`](#m-electron2d-color-color8-system-byte-system-byte-system-byte-system-byte) | Constructs a color from 8-bit integer components. |
| [`public static Color FromHSV(float hue, float saturation, float value, float alpha = 1f)`](#m-electron2d-color-fromhsv-system-single-system-single-system-single-system-single) | Constructs a color from HSV components. |
| [`public void ToHSV(out float hue, out float saturation, out float value)`](#m-electron2d-color-tohsv-system-single-byref-system-single-byref-system-single-byref) | Computes this color's HSV components in one pass. |
| [`public static Color FromOKHSL(float hue, float saturation, float lightness, float alpha = 1f)`](#m-electron2d-color-fromokhsl-system-single-system-single-system-single-system-single) | Constructs a color from perceptually uniform OKHSL components. |
| [`public static Color FromRGBE9995(uint rgbe)`](#m-electron2d-color-fromrgbe9995-system-uint32) | Decodes a shared-exponent RGBE9995 value. |
| [`public static Color FromString(string text, Color defaultColor)`](#m-electron2d-color-fromstring-system-string-electron2d-color) | Parses hexadecimal text or a standard name, returning a fallback on failure. |
| [`public static bool HTMLIsValid(ReadOnlySpan<char> color)`](#m-electron2d-color-htmlisvalid-system-readonlyspan-system-char) | Tests whether a span is valid HTML-style hexadecimal color text. |
| [`public override bool Equals(object obj)`](#m-electron2d-color-equals-system-object) | Tests whether another object is an exactly equal color. |
| [`public bool Equals(Color other)`](#m-electron2d-color-equals-electron2d-color) | Tests all components for exact floating-point equality. |
| [`public bool IsEqualApprox(Color other)`](#m-electron2d-color-isequalapprox-electron2d-color) | Tests all components for scale-aware approximate equality. |
| [`public override int GetHashCode()`](#m-electron2d-color-gethashcode) | Returns a hash code based on all four components. |
| [`public override string ToString()`](#m-electron2d-color-tostring) | Formats all four components using invariant culture. |
| [`public string ToString(string format)`](#m-electron2d-color-tostring-system-string) | Formats all four components using a numeric format and invariant culture. |

## Fields

| Member | Description |
| --- | --- |
| [`public float R`](#f-electron2d-color-r) | Gets or sets the red component, typically from `0` to `1`. |
| [`public float G`](#f-electron2d-color-g) | Gets or sets the green component, typically from `0` to `1`. |
| [`public float B`](#f-electron2d-color-b) | Gets or sets the blue component, typically from `0` to `1`. |
| [`public float A`](#f-electron2d-color-a) | Gets or sets the linear alpha component, where `0` is transparent and `1` is opaque. |

## Operators

| Member | Description |
| --- | --- |
| [`public static Color operator +(Color left, Color right)`](#m-electron2d-color-op-addition-electron2d-color-electron2d-color) | Adds matching components. |
| [`public static Color operator +(Color color)`](#m-electron2d-color-op-unaryplus-electron2d-color) | Returns a color unchanged. |
| [`public static Color operator -(Color left, Color right)`](#m-electron2d-color-op-subtraction-electron2d-color-electron2d-color) | Subtracts matching components. |
| [`public static Color operator -(Color color)`](#m-electron2d-color-op-unarynegation-electron2d-color) | Complements all four components. |
| [`public static Color operator *(Color color, float scale)`](#m-electron2d-color-op-multiply-electron2d-color-system-single) | Multiplies every component by a scalar. |
| [`public static Color operator *(float scale, Color color)`](#m-electron2d-color-op-multiply-system-single-electron2d-color) | Multiplies every component by a scalar. |
| [`public static Color operator *(Color left, Color right)`](#m-electron2d-color-op-multiply-electron2d-color-electron2d-color) | Multiplies matching components. |
| [`public static Color operator /(Color color, float scale)`](#m-electron2d-color-op-division-electron2d-color-system-single) | Divides every component by a scalar using IEEE 754 floating-point semantics. |
| [`public static Color operator /(Color left, Color right)`](#m-electron2d-color-op-division-electron2d-color-electron2d-color) | Divides matching components using IEEE 754 floating-point semantics. |
| [`public static bool operator ==(Color left, Color right)`](#m-electron2d-color-op-equality-electron2d-color-electron2d-color) | Tests all components for exact floating-point equality. |
| [`public static bool operator !=(Color left, Color right)`](#m-electron2d-color-op-inequality-electron2d-color-electron2d-color) | Tests whether any component differs under exact floating-point equality. |
| [`public static bool operator <(Color left, Color right)`](#m-electron2d-color-op-lessthan-electron2d-color-electron2d-color) | Compares colors lexicographically in red, green, blue, alpha order. |
| [`public static bool operator >(Color left, Color right)`](#m-electron2d-color-op-greaterthan-electron2d-color-electron2d-color) | Compares colors lexicographically in red, green, blue, alpha order. |
| [`public static bool operator <=(Color left, Color right)`](#m-electron2d-color-op-lessthanorequal-electron2d-color-electron2d-color) | Compares colors lexicographically in red, green, blue, alpha order. |
| [`public static bool operator >=(Color left, Color right)`](#m-electron2d-color-op-greaterthanorequal-electron2d-color-electron2d-color) | Compares colors lexicographically in red, green, blue, alpha order. |

## Constructor Descriptions

<a id="m-electron2d-color-ctor-system-single-system-single-system-single-system-single"></a>
### `public Color(float r, float g, float b, float a = 1f)`

Initializes an RGBA color from floating-point components.

**Parameters**

- `r`: The red component, typically from `0` to `1`.
- `g`: The green component, typically from `0` to `1`.
- `b`: The blue component, typically from `0` to `1`.
- `a`: The linear alpha component, where `0` is transparent and `1` is opaque.

**Remarks:** Values are stored unchanged and are not clamped.

<a id="m-electron2d-color-ctor-electron2d-color-system-single"></a>
### `public Color(Color color, float alpha = 1f)`

Initializes a color from another color's RGB components and a replacement alpha value.

**Parameters**

- `color`: The color whose RGB components are copied.
- `alpha`: The replacement alpha component.

**Remarks:** Omitting `alpha` produces opaque output; ordinary assignment copies all four components.

<a id="m-electron2d-color-ctor-system-uint32"></a>
### `public Color(uint rgba)`

Initializes a color from packed `0xRRGGBBAA` bytes.

**Parameters**

- `rgba`: The packed 32-bit RGBA value.

<a id="m-electron2d-color-ctor-system-uint64"></a>
### `public Color(ulong rgba)`

Initializes a color from packed `0xRRRRGGGGBBBBAAAA` words.

**Parameters**

- `rgba`: The packed 64-bit RGBA value.

<a id="m-electron2d-color-ctor-system-string"></a>
### `public Color(string code)`

Initializes a color from an HTML hexadecimal code or a standard color name.

**Parameters**

- `code`: A 3-, 4-, 6-, or 8-digit hexadecimal code with optional `#`, or a name from [`Colors`](Colors.md).

**Exceptions**

- `ArgumentNullException`: `code` is `null`.
- `ArgumentOutOfRangeException`: `code` is neither a valid hexadecimal code nor a known name.

<a id="m-electron2d-color-ctor-system-string-system-single"></a>
### `public Color(string code, float alpha)`

Initializes a color from an HTML hexadecimal code or standard name and replaces its alpha.

**Parameters**

- `code`: A hexadecimal code or a name from [`Colors`](Colors.md).
- `alpha`: The replacement alpha component.

**Exceptions**

- `ArgumentNullException`: `code` is `null`.
- `ArgumentOutOfRangeException`: `code` is neither a valid hexadecimal code nor a known name.

## Property Descriptions

<a id="p-electron2d-color-r8"></a>
### `public int R8 { get; set; }`

Gets or sets [`Color.R`](Color.md#f-electron2d-color-r) on an integer scale where `255` represents `1`.

**Value:** The rounded value of [`Color.R`](Color.md#f-electron2d-color-r) multiplied by 255, or a value divided by 255 when set.

**Remarks:** Finite values are not clamped to byte range; overflow saturates to an `Int32` endpoint and NaN becomes zero.

<a id="p-electron2d-color-g8"></a>
### `public int G8 { get; set; }`

Gets or sets [`Color.G`](Color.md#f-electron2d-color-g) on an integer scale where `255` represents `1`.

**Value:** The rounded value of [`Color.G`](Color.md#f-electron2d-color-g) multiplied by 255, or a value divided by 255 when set.

**Remarks:** Finite values are not clamped to byte range; overflow saturates to an `Int32` endpoint and NaN becomes zero.

<a id="p-electron2d-color-b8"></a>
### `public int B8 { get; set; }`

Gets or sets [`Color.B`](Color.md#f-electron2d-color-b) on an integer scale where `255` represents `1`.

**Value:** The rounded value of [`Color.B`](Color.md#f-electron2d-color-b) multiplied by 255, or a value divided by 255 when set.

**Remarks:** Finite values are not clamped to byte range; overflow saturates to an `Int32` endpoint and NaN becomes zero.

<a id="p-electron2d-color-a8"></a>
### `public int A8 { get; set; }`

Gets or sets [`Color.A`](Color.md#f-electron2d-color-a) on an integer scale where `255` represents `1`.

**Value:** The rounded value of [`Color.A`](Color.md#f-electron2d-color-a) multiplied by 255, or a value divided by 255 when set.

**Remarks:** Finite values are not clamped to byte range; overflow saturates to an `Int32` endpoint and NaN becomes zero.

<a id="p-electron2d-color-h"></a>
### `public float H { get; set; }`

Gets or sets the HSV hue, typically from `0` to `1`.

**Value:** The hue of this color; achromatic colors report `0`.

**Remarks:** Setting the value reconstructs RGB through [`Color.FromHSV(Single,Single,Single,Single)`](Color.md#m-electron2d-color-fromhsv-system-single-system-single-system-single-system-single) and preserves alpha.

<a id="p-electron2d-color-s"></a>
### `public float S { get; set; }`

Gets or sets the HSV saturation, typically from `0` to `1`.

**Value:** The ratio of RGB chroma to the greatest RGB component, or `0` when the greatest component is zero.

**Remarks:** Setting the value reconstructs RGB through [`Color.FromHSV(Single,Single,Single,Single)`](Color.md#m-electron2d-color-fromhsv-system-single-system-single-system-single-system-single) and preserves alpha.

<a id="p-electron2d-color-v"></a>
### `public float V { get; set; }`

Gets or sets the HSV value component, typically from `0` to `1`.

**Value:** The greatest RGB component.

**Remarks:** Setting the value reconstructs RGB through [`Color.FromHSV(Single,Single,Single,Single)`](Color.md#m-electron2d-color-fromhsv-system-single-system-single-system-single-system-single) and preserves alpha.

<a id="p-electron2d-color-okhslh"></a>
### `public float OKHSLH { get; set; }`

Gets or sets the perceptual OKHSL hue, from `0` to `1`.

**Value:** The normalized perceptual hue; achromatic colors report `0`.

**Remarks:** Setting the value reconstructs RGB through [`Color.FromOKHSL(Single,Single,Single,Single)`](Color.md#m-electron2d-color-fromokhsl-system-single-system-single-system-single-system-single) and preserves alpha.

<a id="p-electron2d-color-okhsls"></a>
### `public float OKHSLS { get; set; }`

Gets or sets the perceptual OKHSL saturation, from `0` to `1`.

**Value:** The normalized perceptual saturation; achromatic colors report `0`.

**Remarks:** Setting the value reconstructs RGB through [`Color.FromOKHSL(Single,Single,Single,Single)`](Color.md#m-electron2d-color-fromokhsl-system-single-system-single-system-single-system-single) and preserves alpha.

<a id="p-electron2d-color-okhsll"></a>
### `public float OKHSLL { get; set; }`

Gets or sets the perceptual OKHSL lightness, from `0` to `1`.

**Value:** The normalized perceptual lightness.

**Remarks:** Setting the value reconstructs RGB through [`Color.FromOKHSL(Single,Single,Single,Single)`](Color.md#m-electron2d-color-fromokhsl-system-single-system-single-system-single-system-single) and preserves alpha.

<a id="p-electron2d-color-luminance"></a>
### `public float Luminance { get; }`

Gets the relative light intensity of a linear-space RGB color.

**Value:** `0.2126 × R + 0.7152 × G + 0.0722 × B`; alpha is ignored.

**Remarks:** Call [`Color.SRGBToLinear`](Color.md#m-electron2d-color-srgbtolinear) first when the stored RGB components are sRGB encoded.

<a id="p-electron2d-color-item-system-int32"></a>
### `public float this[int index] { get; set; }`

Gets or sets a component by RGBA index.

**Parameters**

- `index`: `0` for red, `1` for green, `2` for blue, or `3` for alpha.

**Value:** The selected floating-point component.

**Exceptions**

- `ArgumentOutOfRangeException`: `index` is outside the range `0..3`.

## Method Descriptions

<a id="m-electron2d-color-blend-electron2d-color"></a>
### `public Color Blend(Color over)`

Blends `over` as a foreground color over this background color.

**Parameters**

- `over`: The straight-alpha foreground color.

**Returns:** The source-over composite. An exactly zero output alpha produces transparent black.

<a id="m-electron2d-color-clamp-system-nullable-electron2d-color-system-nullable-electron2d-color"></a>
### `public Color Clamp(Color? min = null, Color? max = null)`

Clamps each component between the corresponding components of two colors.

**Parameters**

- `min`: The componentwise minimum, or transparent black when omitted.
- `max`: The componentwise maximum, or opaque white when omitted.

**Returns:** A componentwise-clamped color.

**Exceptions**

- `ArgumentException`: A minimum component is greater than its matching maximum component.

<a id="m-electron2d-color-darkened-system-single"></a>
### `public Color Darkened(float amount)`

Darkens the RGB components by a ratio while preserving alpha.

**Parameters**

- `amount`: The darkening ratio, normally from `0` to `1`.

**Returns:** A color whose RGB components are multiplied by `1 - amount`.

**Remarks:** The ratio is not clamped and therefore supports extrapolation.

<a id="m-electron2d-color-inverted"></a>
### `public Color Inverted()`

Inverts the RGB components while preserving alpha.

**Returns:** `(1 - R, 1 - G, 1 - B, A)`.

<a id="m-electron2d-color-lightened-system-single"></a>
### `public Color Lightened(float amount)`

Lightens the RGB components toward one by a ratio while preserving alpha.

**Parameters**

- `amount`: The lightening ratio, normally from `0` to `1`.

**Returns:** A color linearly moved toward white in RGB space.

**Remarks:** The ratio is not clamped and therefore supports extrapolation.

<a id="m-electron2d-color-lerp-electron2d-color-system-single"></a>
### `public Color Lerp(Color to, float weight)`

Linearly interpolates every component toward another color.

**Parameters**

- `to`: The destination color.
- `weight`: The interpolation weight, normally from `0` to `1`.

**Returns:** The componentwise interpolation.

**Remarks:** The weight is not clamped and therefore supports extrapolation.

<a id="m-electron2d-color-lineartosrgb"></a>
### `public Color LinearToSRGB()`

Converts linear RGB components to nonlinear sRGB while preserving alpha.

**Returns:** The sRGB-encoded color.

<a id="m-electron2d-color-srgbtolinear"></a>
### `public Color SRGBToLinear()`

Converts nonlinear sRGB components to linear RGB while preserving alpha.

**Returns:** The linear-space color.

<a id="m-electron2d-color-toabgr32"></a>
### `public uint ToABGR32()`

Packs the color into `0xAABBGGRR`.

**Returns:** An unsigned 32-bit ABGR value with one rounded byte per component.

**Remarks:** Components are clamped to `0..1`; NaN becomes zero.

<a id="m-electron2d-color-toabgr64"></a>
### `public ulong ToABGR64()`

Packs the color into `0xAAAABBBBGGGGRRRR`.

**Returns:** An unsigned 64-bit ABGR value with one rounded word per component.

**Remarks:** Components are clamped to `0..1`; NaN becomes zero.

<a id="m-electron2d-color-toargb32"></a>
### `public uint ToARGB32()`

Packs the color into `0xAARRGGBB`.

**Returns:** An unsigned 32-bit ARGB value with one rounded byte per component.

**Remarks:** Components are clamped to `0..1`; NaN becomes zero.

<a id="m-electron2d-color-toargb64"></a>
### `public ulong ToARGB64()`

Packs the color into `0xAAAARRRRGGGGBBBB`.

**Returns:** An unsigned 64-bit ARGB value with one rounded word per component.

**Remarks:** Components are clamped to `0..1`; NaN becomes zero.

<a id="m-electron2d-color-torgba32"></a>
### `public uint ToRGBA32()`

Packs the color into `0xRRGGBBAA`.

**Returns:** An unsigned 32-bit RGBA value with one rounded byte per component.

**Remarks:** Components are clamped to `0..1`; NaN becomes zero.

<a id="m-electron2d-color-torgba64"></a>
### `public ulong ToRGBA64()`

Packs the color into `0xRRRRGGGGBBBBAAAA`.

**Returns:** An unsigned 64-bit RGBA value with one rounded word per component.

**Remarks:** Components are clamped to `0..1`; NaN becomes zero.

<a id="m-electron2d-color-tohtml-system-boolean"></a>
### `public string ToHTML(bool includeAlpha = true)`

Formats the color as lowercase hexadecimal RGBA or RGB without a leading hash sign.

**Parameters**

- `includeAlpha`: Whether to append the alpha byte.

**Returns:** Six or eight hexadecimal digits. Each component is clamped to `0..1` and rounded to a byte.

<a id="m-electron2d-color-fromhtml-system-readonlyspan-system-char"></a>
### `public static Color FromHTML(ReadOnlySpan<char> rgba)`

Parses a color from HTML-style hexadecimal RGB or RGBA text.

**Parameters**

- `rgba`: Three, four, six, or eight hexadecimal digits, optionally prefixed by one `#`.

**Returns:** The parsed color; formats without alpha produce an alpha value of `1`.

**Exceptions**

- `ArgumentOutOfRangeException`: The length or a character is invalid.

**Remarks:** An empty span returns opaque black for parity with the typed API contract; [`Color.HTMLIsValid(ReadOnlySpan{Char})`](Color.md#m-electron2d-color-htmlisvalid-system-readonlyspan-system-char) still reports it as invalid.

<a id="m-electron2d-color-color8-system-byte-system-byte-system-byte-system-byte"></a>
### `public static Color Color8(byte r8, byte g8, byte b8, byte a8 = 255)`

Constructs a color from 8-bit integer components.

**Parameters**

- `r8`: The red byte.
- `g8`: The green byte.
- `b8`: The blue byte.
- `a8`: The alpha byte.

**Returns:** The components divided by 255.

<a id="m-electron2d-color-fromhsv-system-single-system-single-system-single-system-single"></a>
### `public static Color FromHSV(float hue, float saturation, float value, float alpha = 1f)`

Constructs a color from HSV components.

**Parameters**

- `hue`: The hue, typically from `0` to `1`.
- `saturation`: The saturation, typically from `0` to `1`.
- `value`: The value or brightness, typically from `0` to `1`.
- `alpha`: The alpha component, typically from `0` to `1`.

**Returns:** The equivalent RGBA color.

**Remarks:** Inputs are not clamped. Hue is periodic for ordinary nonnegative values.

<a id="m-electron2d-color-tohsv-system-single-byref-system-single-byref-system-single-byref"></a>
### `public void ToHSV(out float hue, out float saturation, out float value)`

Computes this color's HSV components in one pass.

**Parameters**

- `hue`: Receives the hue, or `0` for an achromatic color.
- `saturation`: Receives the saturation.
- `value`: Receives the greatest RGB component.

<a id="m-electron2d-color-fromokhsl-system-single-system-single-system-single-system-single"></a>
### `public static Color FromOKHSL(float hue, float saturation, float lightness, float alpha = 1f)`

Constructs a color from perceptually uniform OKHSL components.

**Parameters**

- `hue`: The perceptual hue, typically from `0` to `1`.
- `saturation`: The perceptual saturation, typically from `0` to `1`.
- `lightness`: The perceptual lightness, typically from `0` to `1`.
- `alpha`: The alpha component, typically from `0` to `1`.

**Returns:** The equivalent sRGB color, componentwise clamped to `0..1`, including alpha.

<a id="m-electron2d-color-fromrgbe9995-system-uint32"></a>
### `public static Color FromRGBE9995(uint rgbe)`

Decodes a shared-exponent RGBE9995 value.

**Parameters**

- `rgbe`: Nine-bit red, green, and blue mantissas followed by a five-bit exponent.

**Returns:** The decoded linear RGB color with opaque alpha.

<a id="m-electron2d-color-fromstring-system-string-electron2d-color"></a>
### `public static Color FromString(string text, Color defaultColor)`

Parses hexadecimal text or a standard name, returning a fallback on failure.

**Parameters**

- `text`: The candidate hexadecimal code or color name.
- `defaultColor`: The value returned when no color matches.

**Returns:** The parsed color or `defaultColor`.

**Exceptions**

- `ArgumentNullException`: `text` is `null`.

<a id="m-electron2d-color-htmlisvalid-system-readonlyspan-system-char"></a>
### `public static bool HTMLIsValid(ReadOnlySpan<char> color)`

Tests whether a span is valid HTML-style hexadecimal color text.

**Parameters**

- `color`: The candidate text.

**Returns:** `true` for 3, 4, 6, or 8 hexadecimal digits with an optional leading `#`; otherwise `false`.

**Remarks:** Whitespace and multiple hash signs are not accepted.

<a id="m-electron2d-color-equals-system-object"></a>
### `public override bool Equals(object obj)`

Tests whether another object is an exactly equal color.

**Parameters**

- `obj`: The object to compare.

**Returns:** `true` when `obj` is a color with exactly equal components.

<a id="m-electron2d-color-equals-electron2d-color"></a>
### `public bool Equals(Color other)`

Tests all components for exact floating-point equality.

**Parameters**

- `other`: The other color.

**Returns:** `true` when every component is exactly equal.

<a id="m-electron2d-color-isequalapprox-electron2d-color"></a>
### `public bool IsEqualApprox(Color other)`

Tests all components for scale-aware approximate equality.

**Parameters**

- `other`: The other color.

**Returns:** `true` when every component is within the scale-aware the internal `1e-6f` threshold tolerance.

<a id="m-electron2d-color-gethashcode"></a>
### `public override int GetHashCode()`

Returns a hash code based on all four components.

**Returns:** The component hash code.

<a id="m-electron2d-color-tostring"></a>
### `public override string ToString()`

Formats all four components using invariant culture.

**Returns:** A string in the form `(R, G, B, A)`.

<a id="m-electron2d-color-tostring-system-string"></a>
### `public string ToString(string format)`

Formats all four components using a numeric format and invariant culture.

**Parameters**

- `format`: A standard or custom numeric format string, or `null` for the default format.

**Returns:** A string in the form `(R, G, B, A)`.

**Exceptions**

- `FormatException`: `format` is invalid.

## Field Descriptions

<a id="f-electron2d-color-r"></a>
### `public float R`

Gets or sets the red component, typically from `0` to `1`.

<a id="f-electron2d-color-g"></a>
### `public float G`

Gets or sets the green component, typically from `0` to `1`.

<a id="f-electron2d-color-b"></a>
### `public float B`

Gets or sets the blue component, typically from `0` to `1`.

<a id="f-electron2d-color-a"></a>
### `public float A`

Gets or sets the linear alpha component, where `0` is transparent and `1` is opaque.

## Operator Descriptions

<a id="m-electron2d-color-op-addition-electron2d-color-electron2d-color"></a>
### `public static Color operator +(Color left, Color right)`

Adds matching components.

**Parameters**

- `left`: The first color.
- `right`: The second color.

**Returns:** The componentwise sum.

<a id="m-electron2d-color-op-unaryplus-electron2d-color"></a>
### `public static Color operator +(Color color)`

Returns a color unchanged.

**Parameters**

- `color`: The color.

**Returns:** `color`.

<a id="m-electron2d-color-op-subtraction-electron2d-color-electron2d-color"></a>
### `public static Color operator -(Color left, Color right)`

Subtracts matching components.

**Parameters**

- `left`: The minuend.
- `right`: The subtrahend.

**Returns:** The componentwise difference.

<a id="m-electron2d-color-op-unarynegation-electron2d-color"></a>
### `public static Color operator -(Color color)`

Complements all four components.

**Parameters**

- `color`: The color to complement.

**Returns:** `(1 - R, 1 - G, 1 - B, 1 - A)`.

<a id="m-electron2d-color-op-multiply-electron2d-color-system-single"></a>
### `public static Color operator *(Color color, float scale)`

Multiplies every component by a scalar.

**Parameters**

- `color`: The color.
- `scale`: The scalar multiplier.

**Returns:** The scaled color.

<a id="m-electron2d-color-op-multiply-system-single-electron2d-color"></a>
### `public static Color operator *(float scale, Color color)`

Multiplies every component by a scalar.

**Parameters**

- `scale`: The scalar multiplier.
- `color`: The color.

**Returns:** The scaled color.

<a id="m-electron2d-color-op-multiply-electron2d-color-electron2d-color"></a>
### `public static Color operator *(Color left, Color right)`

Multiplies matching components.

**Parameters**

- `left`: The first color.
- `right`: The second color.

**Returns:** The componentwise product.

<a id="m-electron2d-color-op-division-electron2d-color-system-single"></a>
### `public static Color operator /(Color color, float scale)`

Divides every component by a scalar using IEEE 754 floating-point semantics.

**Parameters**

- `color`: The dividend color.
- `scale`: The scalar divisor.

**Returns:** The scaled quotient; zero divisors can produce infinities or NaN.

<a id="m-electron2d-color-op-division-electron2d-color-electron2d-color"></a>
### `public static Color operator /(Color left, Color right)`

Divides matching components using IEEE 754 floating-point semantics.

**Parameters**

- `left`: The dividend color.
- `right`: The divisor color.

**Returns:** The componentwise quotient; zero divisors can produce infinities or NaN.

<a id="m-electron2d-color-op-equality-electron2d-color-electron2d-color"></a>
### `public static bool operator ==(Color left, Color right)`

Tests all components for exact floating-point equality.

**Parameters**

- `left`: The first color.
- `right`: The second color.

**Returns:** `true` when every component is exactly equal.

<a id="m-electron2d-color-op-inequality-electron2d-color-electron2d-color"></a>
### `public static bool operator !=(Color left, Color right)`

Tests whether any component differs under exact floating-point equality.

**Parameters**

- `left`: The first color.
- `right`: The second color.

**Returns:** `true` when at least one component differs.

<a id="m-electron2d-color-op-lessthan-electron2d-color-electron2d-color"></a>
### `public static bool operator <(Color left, Color right)`

Compares colors lexicographically in red, green, blue, alpha order.

**Parameters**

- `left`: The first color.
- `right`: The second color.

**Returns:** `true` when `left` sorts before `right`.

<a id="m-electron2d-color-op-greaterthan-electron2d-color-electron2d-color"></a>
### `public static bool operator >(Color left, Color right)`

Compares colors lexicographically in red, green, blue, alpha order.

**Parameters**

- `left`: The first color.
- `right`: The second color.

**Returns:** `true` when `left` sorts after `right`.

<a id="m-electron2d-color-op-lessthanorequal-electron2d-color-electron2d-color"></a>
### `public static bool operator <=(Color left, Color right)`

Compares colors lexicographically in red, green, blue, alpha order.

**Parameters**

- `left`: The first color.
- `right`: The second color.

**Returns:** `true` when `left` does not sort after `right`.

<a id="m-electron2d-color-op-greaterthanorequal-electron2d-color-electron2d-color"></a>
### `public static bool operator >=(Color left, Color right)`

Compares colors lexicographically in red, green, blue, alpha order.

**Parameters**

- `left`: The first color.
- `right`: The second color.

**Returns:** `true` when `left` does not sort before `right`.

## Parsing and named colors

HTML-style input permits exactly 3, 4, 6, or 8 ASCII hexadecimal digits and at most one leading `#`; it does not trim whitespace. Three/four-digit forms divide each nibble by 15, six/eight-digit forms divide bytes by 255, and missing alpha is one. `HTMLIsValid("")` is false while direct `FromHTML` on an empty span returns opaque black as a retained C# compatibility edge case.

Named parsing is case-insensitive and removes spaces, hyphens, underscores, apostrophes, and periods before lookup. [`Colors`](Colors.md) owns all 146 public names. String constructors reject null or unknown input; `FromString` rejects null but returns its fallback for an unknown name.

## Numeric invariants and error behavior

- Ordinary float construction and arithmetic preserve out-of-range, infinity, and NaN values.
- `Color8` accepts byte channels, matching the accepted typed C# binding; the reference language's integer-channel overload also accepts values outside `0..255`. Call the float constructor for those colors. Integer scalar multiplication and division compile through the existing `float` operators.
- `R8/G8/B8/A8` deliberately retain the C# integer-scale behavior instead of clamping to byte range; NaN maps to zero and overflow saturates at an `int` endpoint for cross-platform determinism.
- HTML and packed integer output use Electron2D's portable quantization contract: clamp finite values to `0..1`, map NaN to zero, map infinities to an endpoint, then midpoint-to-even rounding.
- `Clamp` follows `Math.Clamp`; if any minimum exceeds its corresponding maximum, the operation throws before returning a result.
- HSV inputs are documented for `0..1` and are not generally normalized or validated outside that range.
- OKHSL conversion uses the bundled MIT-licensed managed algorithm; constructed output, including alpha, is clamped, and NaN output becomes zero. Achromatic getters explicitly report zero hue/saturation.
- OKHSL getters clamp their published coordinates to `0..1`. At a narrow saturated gamut boundary the raw reference saturation can slightly exceed one, so reconstructing only from the public clamped coordinates can be intentionally lossy; this is covered by an independent dark-blue fixture.
- Exact equality and ordering are floating-point operations rather than a total-order key. Use `IsEqualApprox` for calculated ordinary colors.

## Lifecycle, threading, and allocation

There is no lifecycle or state transition beyond normal value assignment. Copies are independent. Numeric constructors, accessors, conversions, composition, arithmetic, packing, and comparisons use no mutable shared state and are safe across threads when each caller owns its value. Concurrent writes to the same storage location remain an ordinary unsynchronized C# data race. Named lookup reads an immutable process-wide table and is thread-safe.

Warmed numeric operations allocate no managed memory. String formatting/parsing and named normalization may allocate. The type does not own renderer or SDL state, and its 16-byte sequential layout is not a promise that it is ABI-identical to a native backend color structure; backend conversion must remain explicit.

## Dependencies and integration

The public type depends on canonical scalar [`MathF`](MathF.md) plus .NET globalization and interop metadata. `OkColor.cs` is an internal managed OKHSL implementation with its license retained in source. [`ConfigFile`](ConfigFile.md) serializes finite colors through a strict `{"R", "G", "B", "A"}` JSON object. Stored typed property descriptors and [`PackedScene`](PackedScene.md) preserve `Color` directly as a reference-free value.

There is no dependency on Scene, rendering, SDL, input, audio, physics, resources, scripting, or an editor. There is no boolean conversion operator because C# has no appropriate implicit color truth-value contract. No `System.Drawing` dependency or implicit conversion is provided; consumers that import another `Color` type use a normal C# alias.

## Verification and known limitations

The pinned reference audit covers all 206 members and the type row. All 146 named values were compared with the pinned XML; three previously unmapped signatures are available through the explicit-alpha constructor and C# integer-to-float scalar conversion. `tests/Electron2D.Tests/Program.cs` verifies layout/defaults, constructors, every component view and index failure, HSV and independent OKHSL primary anchors/interior round trips/saturated-boundary fixture, luminance, blend, clamping failures, unbounded adjustment/interpolation, transfer functions, exhaustive byte round trips, packing order and edge quantization, RGBE9995, all HTML forms and failures, normalized named lookup, all 146 properties, concurrent lookup, integer scalar calls and every operator family, NaN/infinity behavior, hashing, invariant formatting, strict `ConfigFile` serialization, `PackedScene` storage, and zero warmed numeric allocation.

Execution of the color-value contract is verified on Linux/.NET 8. Rendering tests verify canvas and material pixels separately; this audit does not establish native color ABI or other-platform behavior. The API follows the accepted typed C# surface; language-specific boolean evaluation and native-only named-color-index helpers are permanently excluded, while remaining image/renderer format interactions retain their own coverage rows.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0024: Typed color values and portable quantization](../decisions/core-math.md#adr-0024)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
