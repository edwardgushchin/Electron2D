# Colors

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Colors.cs`](../../src/Core/Math/Colors.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public static class Colors`

> Provides the standard named color catalog used by [`Color`](Color.md) string conversion.

## Description

Provides the standard named color catalog used by [`Color`](Color.md) string conversion.

`Colors` is the compile-time catalog used by [`Color`](Color.md) named parsing. Every public property returns a `Color` value and owns no mutable instance, resource, native handle, or lifecycle. The internal lookup is created once as a frozen ordinal dictionary and is thereafter read-only and safe for concurrent lookup.

`Transparent` is transparent white `(1, 1, 1, 0)`; it is deliberately different from `default(Color)`, which is transparent black. `Aqua`/`Cyan`, `Fuchsia`/`Magenta`, and `Green`/`Lime` are equal aliases. Historical X11 `Gray`, `Green`, `Maroon`, and `Purple` values remain separate from the four `Web*` values.

Properties return values and own no resources. Several historical names are aliases with identical channel values.
[`Colors.Transparent`](Colors.md#p-electron2d-colors-transparent) is transparent white rather than the zero-initialized transparent black value.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
Color accent = Colors.CornflowerBlue;
Color parsed = Color.FromString("cornflowerblue", Colors.Transparent);
```

## Properties

| Member | Description |
| --- | --- |
| [`public static Color AliceBlue { get; }`](#p-electron2d-colors-aliceblue) | Gets the standard AliceBlue color. |
| [`public static Color AntiqueWhite { get; }`](#p-electron2d-colors-antiquewhite) | Gets the standard AntiqueWhite color. |
| [`public static Color Aqua { get; }`](#p-electron2d-colors-aqua) | Gets the standard Aqua color. |
| [`public static Color Aquamarine { get; }`](#p-electron2d-colors-aquamarine) | Gets the standard Aquamarine color. |
| [`public static Color Azure { get; }`](#p-electron2d-colors-azure) | Gets the standard Azure color. |
| [`public static Color Beige { get; }`](#p-electron2d-colors-beige) | Gets the standard Beige color. |
| [`public static Color Bisque { get; }`](#p-electron2d-colors-bisque) | Gets the standard Bisque color. |
| [`public static Color Black { get; }`](#p-electron2d-colors-black) | Gets the standard Black color. |
| [`public static Color BlanchedAlmond { get; }`](#p-electron2d-colors-blanchedalmond) | Gets the standard BlanchedAlmond color. |
| [`public static Color Blue { get; }`](#p-electron2d-colors-blue) | Gets the standard Blue color. |
| [`public static Color BlueViolet { get; }`](#p-electron2d-colors-blueviolet) | Gets the standard BlueViolet color. |
| [`public static Color Brown { get; }`](#p-electron2d-colors-brown) | Gets the standard Brown color. |
| [`public static Color Burlywood { get; }`](#p-electron2d-colors-burlywood) | Gets the standard Burlywood color. |
| [`public static Color CadetBlue { get; }`](#p-electron2d-colors-cadetblue) | Gets the standard CadetBlue color. |
| [`public static Color Chartreuse { get; }`](#p-electron2d-colors-chartreuse) | Gets the standard Chartreuse color. |
| [`public static Color Chocolate { get; }`](#p-electron2d-colors-chocolate) | Gets the standard Chocolate color. |
| [`public static Color Coral { get; }`](#p-electron2d-colors-coral) | Gets the standard Coral color. |
| [`public static Color CornflowerBlue { get; }`](#p-electron2d-colors-cornflowerblue) | Gets the standard CornflowerBlue color. |
| [`public static Color Cornsilk { get; }`](#p-electron2d-colors-cornsilk) | Gets the standard Cornsilk color. |
| [`public static Color Crimson { get; }`](#p-electron2d-colors-crimson) | Gets the standard Crimson color. |
| [`public static Color Cyan { get; }`](#p-electron2d-colors-cyan) | Gets the standard Cyan color. |
| [`public static Color DarkBlue { get; }`](#p-electron2d-colors-darkblue) | Gets the standard DarkBlue color. |
| [`public static Color DarkCyan { get; }`](#p-electron2d-colors-darkcyan) | Gets the standard DarkCyan color. |
| [`public static Color DarkGoldenrod { get; }`](#p-electron2d-colors-darkgoldenrod) | Gets the standard DarkGoldenrod color. |
| [`public static Color DarkGray { get; }`](#p-electron2d-colors-darkgray) | Gets the standard DarkGray color. |
| [`public static Color DarkGreen { get; }`](#p-electron2d-colors-darkgreen) | Gets the standard DarkGreen color. |
| [`public static Color DarkKhaki { get; }`](#p-electron2d-colors-darkkhaki) | Gets the standard DarkKhaki color. |
| [`public static Color DarkMagenta { get; }`](#p-electron2d-colors-darkmagenta) | Gets the standard DarkMagenta color. |
| [`public static Color DarkOliveGreen { get; }`](#p-electron2d-colors-darkolivegreen) | Gets the standard DarkOliveGreen color. |
| [`public static Color DarkOrange { get; }`](#p-electron2d-colors-darkorange) | Gets the standard DarkOrange color. |
| [`public static Color DarkOrchid { get; }`](#p-electron2d-colors-darkorchid) | Gets the standard DarkOrchid color. |
| [`public static Color DarkRed { get; }`](#p-electron2d-colors-darkred) | Gets the standard DarkRed color. |
| [`public static Color DarkSalmon { get; }`](#p-electron2d-colors-darksalmon) | Gets the standard DarkSalmon color. |
| [`public static Color DarkSeaGreen { get; }`](#p-electron2d-colors-darkseagreen) | Gets the standard DarkSeaGreen color. |
| [`public static Color DarkSlateBlue { get; }`](#p-electron2d-colors-darkslateblue) | Gets the standard DarkSlateBlue color. |
| [`public static Color DarkSlateGray { get; }`](#p-electron2d-colors-darkslategray) | Gets the standard DarkSlateGray color. |
| [`public static Color DarkTurquoise { get; }`](#p-electron2d-colors-darkturquoise) | Gets the standard DarkTurquoise color. |
| [`public static Color DarkViolet { get; }`](#p-electron2d-colors-darkviolet) | Gets the standard DarkViolet color. |
| [`public static Color DeepPink { get; }`](#p-electron2d-colors-deeppink) | Gets the standard DeepPink color. |
| [`public static Color DeepSkyBlue { get; }`](#p-electron2d-colors-deepskyblue) | Gets the standard DeepSkyBlue color. |
| [`public static Color DimGray { get; }`](#p-electron2d-colors-dimgray) | Gets the standard DimGray color. |
| [`public static Color DodgerBlue { get; }`](#p-electron2d-colors-dodgerblue) | Gets the standard DodgerBlue color. |
| [`public static Color Firebrick { get; }`](#p-electron2d-colors-firebrick) | Gets the standard Firebrick color. |
| [`public static Color FloralWhite { get; }`](#p-electron2d-colors-floralwhite) | Gets the standard FloralWhite color. |
| [`public static Color ForestGreen { get; }`](#p-electron2d-colors-forestgreen) | Gets the standard ForestGreen color. |
| [`public static Color Fuchsia { get; }`](#p-electron2d-colors-fuchsia) | Gets the standard Fuchsia color. |
| [`public static Color Gainsboro { get; }`](#p-electron2d-colors-gainsboro) | Gets the standard Gainsboro color. |
| [`public static Color GhostWhite { get; }`](#p-electron2d-colors-ghostwhite) | Gets the standard GhostWhite color. |
| [`public static Color Gold { get; }`](#p-electron2d-colors-gold) | Gets the standard Gold color. |
| [`public static Color Goldenrod { get; }`](#p-electron2d-colors-goldenrod) | Gets the standard Goldenrod color. |
| [`public static Color Gray { get; }`](#p-electron2d-colors-gray) | Gets the standard Gray color. |
| [`public static Color Green { get; }`](#p-electron2d-colors-green) | Gets the standard Green color. |
| [`public static Color GreenYellow { get; }`](#p-electron2d-colors-greenyellow) | Gets the standard GreenYellow color. |
| [`public static Color Honeydew { get; }`](#p-electron2d-colors-honeydew) | Gets the standard Honeydew color. |
| [`public static Color HotPink { get; }`](#p-electron2d-colors-hotpink) | Gets the standard HotPink color. |
| [`public static Color IndianRed { get; }`](#p-electron2d-colors-indianred) | Gets the standard IndianRed color. |
| [`public static Color Indigo { get; }`](#p-electron2d-colors-indigo) | Gets the standard Indigo color. |
| [`public static Color Ivory { get; }`](#p-electron2d-colors-ivory) | Gets the standard Ivory color. |
| [`public static Color Khaki { get; }`](#p-electron2d-colors-khaki) | Gets the standard Khaki color. |
| [`public static Color Lavender { get; }`](#p-electron2d-colors-lavender) | Gets the standard Lavender color. |
| [`public static Color LavenderBlush { get; }`](#p-electron2d-colors-lavenderblush) | Gets the standard LavenderBlush color. |
| [`public static Color LawnGreen { get; }`](#p-electron2d-colors-lawngreen) | Gets the standard LawnGreen color. |
| [`public static Color LemonChiffon { get; }`](#p-electron2d-colors-lemonchiffon) | Gets the standard LemonChiffon color. |
| [`public static Color LightBlue { get; }`](#p-electron2d-colors-lightblue) | Gets the standard LightBlue color. |
| [`public static Color LightCoral { get; }`](#p-electron2d-colors-lightcoral) | Gets the standard LightCoral color. |
| [`public static Color LightCyan { get; }`](#p-electron2d-colors-lightcyan) | Gets the standard LightCyan color. |
| [`public static Color LightGoldenrod { get; }`](#p-electron2d-colors-lightgoldenrod) | Gets the standard LightGoldenrod color. |
| [`public static Color LightGray { get; }`](#p-electron2d-colors-lightgray) | Gets the standard LightGray color. |
| [`public static Color LightGreen { get; }`](#p-electron2d-colors-lightgreen) | Gets the standard LightGreen color. |
| [`public static Color LightPink { get; }`](#p-electron2d-colors-lightpink) | Gets the standard LightPink color. |
| [`public static Color LightSalmon { get; }`](#p-electron2d-colors-lightsalmon) | Gets the standard LightSalmon color. |
| [`public static Color LightSeaGreen { get; }`](#p-electron2d-colors-lightseagreen) | Gets the standard LightSeaGreen color. |
| [`public static Color LightSkyBlue { get; }`](#p-electron2d-colors-lightskyblue) | Gets the standard LightSkyBlue color. |
| [`public static Color LightSlateGray { get; }`](#p-electron2d-colors-lightslategray) | Gets the standard LightSlateGray color. |
| [`public static Color LightSteelBlue { get; }`](#p-electron2d-colors-lightsteelblue) | Gets the standard LightSteelBlue color. |
| [`public static Color LightYellow { get; }`](#p-electron2d-colors-lightyellow) | Gets the standard LightYellow color. |
| [`public static Color Lime { get; }`](#p-electron2d-colors-lime) | Gets the standard Lime color. |
| [`public static Color LimeGreen { get; }`](#p-electron2d-colors-limegreen) | Gets the standard LimeGreen color. |
| [`public static Color Linen { get; }`](#p-electron2d-colors-linen) | Gets the standard Linen color. |
| [`public static Color Magenta { get; }`](#p-electron2d-colors-magenta) | Gets the standard Magenta color. |
| [`public static Color Maroon { get; }`](#p-electron2d-colors-maroon) | Gets the standard Maroon color. |
| [`public static Color MediumAquamarine { get; }`](#p-electron2d-colors-mediumaquamarine) | Gets the standard MediumAquamarine color. |
| [`public static Color MediumBlue { get; }`](#p-electron2d-colors-mediumblue) | Gets the standard MediumBlue color. |
| [`public static Color MediumOrchid { get; }`](#p-electron2d-colors-mediumorchid) | Gets the standard MediumOrchid color. |
| [`public static Color MediumPurple { get; }`](#p-electron2d-colors-mediumpurple) | Gets the standard MediumPurple color. |
| [`public static Color MediumSeaGreen { get; }`](#p-electron2d-colors-mediumseagreen) | Gets the standard MediumSeaGreen color. |
| [`public static Color MediumSlateBlue { get; }`](#p-electron2d-colors-mediumslateblue) | Gets the standard MediumSlateBlue color. |
| [`public static Color MediumSpringGreen { get; }`](#p-electron2d-colors-mediumspringgreen) | Gets the standard MediumSpringGreen color. |
| [`public static Color MediumTurquoise { get; }`](#p-electron2d-colors-mediumturquoise) | Gets the standard MediumTurquoise color. |
| [`public static Color MediumVioletRed { get; }`](#p-electron2d-colors-mediumvioletred) | Gets the standard MediumVioletRed color. |
| [`public static Color MidnightBlue { get; }`](#p-electron2d-colors-midnightblue) | Gets the standard MidnightBlue color. |
| [`public static Color MintCream { get; }`](#p-electron2d-colors-mintcream) | Gets the standard MintCream color. |
| [`public static Color MistyRose { get; }`](#p-electron2d-colors-mistyrose) | Gets the standard MistyRose color. |
| [`public static Color Moccasin { get; }`](#p-electron2d-colors-moccasin) | Gets the standard Moccasin color. |
| [`public static Color NavajoWhite { get; }`](#p-electron2d-colors-navajowhite) | Gets the standard NavajoWhite color. |
| [`public static Color NavyBlue { get; }`](#p-electron2d-colors-navyblue) | Gets the standard NavyBlue color. |
| [`public static Color OldLace { get; }`](#p-electron2d-colors-oldlace) | Gets the standard OldLace color. |
| [`public static Color Olive { get; }`](#p-electron2d-colors-olive) | Gets the standard Olive color. |
| [`public static Color OliveDrab { get; }`](#p-electron2d-colors-olivedrab) | Gets the standard OliveDrab color. |
| [`public static Color Orange { get; }`](#p-electron2d-colors-orange) | Gets the standard Orange color. |
| [`public static Color OrangeRed { get; }`](#p-electron2d-colors-orangered) | Gets the standard OrangeRed color. |
| [`public static Color Orchid { get; }`](#p-electron2d-colors-orchid) | Gets the standard Orchid color. |
| [`public static Color PaleGoldenrod { get; }`](#p-electron2d-colors-palegoldenrod) | Gets the standard PaleGoldenrod color. |
| [`public static Color PaleGreen { get; }`](#p-electron2d-colors-palegreen) | Gets the standard PaleGreen color. |
| [`public static Color PaleTurquoise { get; }`](#p-electron2d-colors-paleturquoise) | Gets the standard PaleTurquoise color. |
| [`public static Color PaleVioletRed { get; }`](#p-electron2d-colors-palevioletred) | Gets the standard PaleVioletRed color. |
| [`public static Color PapayaWhip { get; }`](#p-electron2d-colors-papayawhip) | Gets the standard PapayaWhip color. |
| [`public static Color PeachPuff { get; }`](#p-electron2d-colors-peachpuff) | Gets the standard PeachPuff color. |
| [`public static Color Peru { get; }`](#p-electron2d-colors-peru) | Gets the standard Peru color. |
| [`public static Color Pink { get; }`](#p-electron2d-colors-pink) | Gets the standard Pink color. |
| [`public static Color Plum { get; }`](#p-electron2d-colors-plum) | Gets the standard Plum color. |
| [`public static Color PowderBlue { get; }`](#p-electron2d-colors-powderblue) | Gets the standard PowderBlue color. |
| [`public static Color Purple { get; }`](#p-electron2d-colors-purple) | Gets the standard Purple color. |
| [`public static Color RebeccaPurple { get; }`](#p-electron2d-colors-rebeccapurple) | Gets the standard RebeccaPurple color. |
| [`public static Color Red { get; }`](#p-electron2d-colors-red) | Gets the standard Red color. |
| [`public static Color RosyBrown { get; }`](#p-electron2d-colors-rosybrown) | Gets the standard RosyBrown color. |
| [`public static Color RoyalBlue { get; }`](#p-electron2d-colors-royalblue) | Gets the standard RoyalBlue color. |
| [`public static Color SaddleBrown { get; }`](#p-electron2d-colors-saddlebrown) | Gets the standard SaddleBrown color. |
| [`public static Color Salmon { get; }`](#p-electron2d-colors-salmon) | Gets the standard Salmon color. |
| [`public static Color SandyBrown { get; }`](#p-electron2d-colors-sandybrown) | Gets the standard SandyBrown color. |
| [`public static Color SeaGreen { get; }`](#p-electron2d-colors-seagreen) | Gets the standard SeaGreen color. |
| [`public static Color Seashell { get; }`](#p-electron2d-colors-seashell) | Gets the standard Seashell color. |
| [`public static Color Sienna { get; }`](#p-electron2d-colors-sienna) | Gets the standard Sienna color. |
| [`public static Color Silver { get; }`](#p-electron2d-colors-silver) | Gets the standard Silver color. |
| [`public static Color SkyBlue { get; }`](#p-electron2d-colors-skyblue) | Gets the standard SkyBlue color. |
| [`public static Color SlateBlue { get; }`](#p-electron2d-colors-slateblue) | Gets the standard SlateBlue color. |
| [`public static Color SlateGray { get; }`](#p-electron2d-colors-slategray) | Gets the standard SlateGray color. |
| [`public static Color Snow { get; }`](#p-electron2d-colors-snow) | Gets the standard Snow color. |
| [`public static Color SpringGreen { get; }`](#p-electron2d-colors-springgreen) | Gets the standard SpringGreen color. |
| [`public static Color SteelBlue { get; }`](#p-electron2d-colors-steelblue) | Gets the standard SteelBlue color. |
| [`public static Color Tan { get; }`](#p-electron2d-colors-tan) | Gets the standard Tan color. |
| [`public static Color Teal { get; }`](#p-electron2d-colors-teal) | Gets the standard Teal color. |
| [`public static Color Thistle { get; }`](#p-electron2d-colors-thistle) | Gets the standard Thistle color. |
| [`public static Color Tomato { get; }`](#p-electron2d-colors-tomato) | Gets the standard Tomato color. |
| [`public static Color Transparent { get; }`](#p-electron2d-colors-transparent) | Gets the standard Transparent color. |
| [`public static Color Turquoise { get; }`](#p-electron2d-colors-turquoise) | Gets the standard Turquoise color. |
| [`public static Color Violet { get; }`](#p-electron2d-colors-violet) | Gets the standard Violet color. |
| [`public static Color WebGray { get; }`](#p-electron2d-colors-webgray) | Gets the standard WebGray color. |
| [`public static Color WebGreen { get; }`](#p-electron2d-colors-webgreen) | Gets the standard WebGreen color. |
| [`public static Color WebMaroon { get; }`](#p-electron2d-colors-webmaroon) | Gets the standard WebMaroon color. |
| [`public static Color WebPurple { get; }`](#p-electron2d-colors-webpurple) | Gets the standard WebPurple color. |
| [`public static Color Wheat { get; }`](#p-electron2d-colors-wheat) | Gets the standard Wheat color. |
| [`public static Color White { get; }`](#p-electron2d-colors-white) | Gets the standard White color. |
| [`public static Color WhiteSmoke { get; }`](#p-electron2d-colors-whitesmoke) | Gets the standard WhiteSmoke color. |
| [`public static Color Yellow { get; }`](#p-electron2d-colors-yellow) | Gets the standard Yellow color. |
| [`public static Color YellowGreen { get; }`](#p-electron2d-colors-yellowgreen) | Gets the standard YellowGreen color. |

## Property Descriptions

<a id="p-electron2d-colors-aliceblue"></a>
### `public static Color AliceBlue { get; }`

Gets the standard AliceBlue color.

**Value:** `#f0f8ffff`.

<a id="p-electron2d-colors-antiquewhite"></a>
### `public static Color AntiqueWhite { get; }`

Gets the standard AntiqueWhite color.

**Value:** `#faebd7ff`.

<a id="p-electron2d-colors-aqua"></a>
### `public static Color Aqua { get; }`

Gets the standard Aqua color.

**Value:** `#00ffffff`.

<a id="p-electron2d-colors-aquamarine"></a>
### `public static Color Aquamarine { get; }`

Gets the standard Aquamarine color.

**Value:** `#7fffd4ff`.

<a id="p-electron2d-colors-azure"></a>
### `public static Color Azure { get; }`

Gets the standard Azure color.

**Value:** `#f0ffffff`.

<a id="p-electron2d-colors-beige"></a>
### `public static Color Beige { get; }`

Gets the standard Beige color.

**Value:** `#f5f5dcff`.

<a id="p-electron2d-colors-bisque"></a>
### `public static Color Bisque { get; }`

Gets the standard Bisque color.

**Value:** `#ffe4c4ff`.

<a id="p-electron2d-colors-black"></a>
### `public static Color Black { get; }`

Gets the standard Black color.

**Value:** `#000000ff`.

<a id="p-electron2d-colors-blanchedalmond"></a>
### `public static Color BlanchedAlmond { get; }`

Gets the standard BlanchedAlmond color.

**Value:** `#ffebcdff`.

<a id="p-electron2d-colors-blue"></a>
### `public static Color Blue { get; }`

Gets the standard Blue color.

**Value:** `#0000ffff`.

<a id="p-electron2d-colors-blueviolet"></a>
### `public static Color BlueViolet { get; }`

Gets the standard BlueViolet color.

**Value:** `#8a2be2ff`.

<a id="p-electron2d-colors-brown"></a>
### `public static Color Brown { get; }`

Gets the standard Brown color.

**Value:** `#a52a2aff`.

<a id="p-electron2d-colors-burlywood"></a>
### `public static Color Burlywood { get; }`

Gets the standard Burlywood color.

**Value:** `#deb887ff`.

<a id="p-electron2d-colors-cadetblue"></a>
### `public static Color CadetBlue { get; }`

Gets the standard CadetBlue color.

**Value:** `#5f9ea0ff`.

<a id="p-electron2d-colors-chartreuse"></a>
### `public static Color Chartreuse { get; }`

Gets the standard Chartreuse color.

**Value:** `#7fff00ff`.

<a id="p-electron2d-colors-chocolate"></a>
### `public static Color Chocolate { get; }`

Gets the standard Chocolate color.

**Value:** `#d2691eff`.

<a id="p-electron2d-colors-coral"></a>
### `public static Color Coral { get; }`

Gets the standard Coral color.

**Value:** `#ff7f50ff`.

<a id="p-electron2d-colors-cornflowerblue"></a>
### `public static Color CornflowerBlue { get; }`

Gets the standard CornflowerBlue color.

**Value:** `#6495edff`.

<a id="p-electron2d-colors-cornsilk"></a>
### `public static Color Cornsilk { get; }`

Gets the standard Cornsilk color.

**Value:** `#fff8dcff`.

<a id="p-electron2d-colors-crimson"></a>
### `public static Color Crimson { get; }`

Gets the standard Crimson color.

**Value:** `#dc143cff`.

<a id="p-electron2d-colors-cyan"></a>
### `public static Color Cyan { get; }`

Gets the standard Cyan color.

**Value:** `#00ffffff`.

<a id="p-electron2d-colors-darkblue"></a>
### `public static Color DarkBlue { get; }`

Gets the standard DarkBlue color.

**Value:** `#00008bff`.

<a id="p-electron2d-colors-darkcyan"></a>
### `public static Color DarkCyan { get; }`

Gets the standard DarkCyan color.

**Value:** `#008b8bff`.

<a id="p-electron2d-colors-darkgoldenrod"></a>
### `public static Color DarkGoldenrod { get; }`

Gets the standard DarkGoldenrod color.

**Value:** `#b8860bff`.

<a id="p-electron2d-colors-darkgray"></a>
### `public static Color DarkGray { get; }`

Gets the standard DarkGray color.

**Value:** `#a9a9a9ff`.

<a id="p-electron2d-colors-darkgreen"></a>
### `public static Color DarkGreen { get; }`

Gets the standard DarkGreen color.

**Value:** `#006400ff`.

<a id="p-electron2d-colors-darkkhaki"></a>
### `public static Color DarkKhaki { get; }`

Gets the standard DarkKhaki color.

**Value:** `#bdb76bff`.

<a id="p-electron2d-colors-darkmagenta"></a>
### `public static Color DarkMagenta { get; }`

Gets the standard DarkMagenta color.

**Value:** `#8b008bff`.

<a id="p-electron2d-colors-darkolivegreen"></a>
### `public static Color DarkOliveGreen { get; }`

Gets the standard DarkOliveGreen color.

**Value:** `#556b2fff`.

<a id="p-electron2d-colors-darkorange"></a>
### `public static Color DarkOrange { get; }`

Gets the standard DarkOrange color.

**Value:** `#ff8c00ff`.

<a id="p-electron2d-colors-darkorchid"></a>
### `public static Color DarkOrchid { get; }`

Gets the standard DarkOrchid color.

**Value:** `#9932ccff`.

<a id="p-electron2d-colors-darkred"></a>
### `public static Color DarkRed { get; }`

Gets the standard DarkRed color.

**Value:** `#8b0000ff`.

<a id="p-electron2d-colors-darksalmon"></a>
### `public static Color DarkSalmon { get; }`

Gets the standard DarkSalmon color.

**Value:** `#e9967aff`.

<a id="p-electron2d-colors-darkseagreen"></a>
### `public static Color DarkSeaGreen { get; }`

Gets the standard DarkSeaGreen color.

**Value:** `#8fbc8fff`.

<a id="p-electron2d-colors-darkslateblue"></a>
### `public static Color DarkSlateBlue { get; }`

Gets the standard DarkSlateBlue color.

**Value:** `#483d8bff`.

<a id="p-electron2d-colors-darkslategray"></a>
### `public static Color DarkSlateGray { get; }`

Gets the standard DarkSlateGray color.

**Value:** `#2f4f4fff`.

<a id="p-electron2d-colors-darkturquoise"></a>
### `public static Color DarkTurquoise { get; }`

Gets the standard DarkTurquoise color.

**Value:** `#00ced1ff`.

<a id="p-electron2d-colors-darkviolet"></a>
### `public static Color DarkViolet { get; }`

Gets the standard DarkViolet color.

**Value:** `#9400d3ff`.

<a id="p-electron2d-colors-deeppink"></a>
### `public static Color DeepPink { get; }`

Gets the standard DeepPink color.

**Value:** `#ff1493ff`.

<a id="p-electron2d-colors-deepskyblue"></a>
### `public static Color DeepSkyBlue { get; }`

Gets the standard DeepSkyBlue color.

**Value:** `#00bfffff`.

<a id="p-electron2d-colors-dimgray"></a>
### `public static Color DimGray { get; }`

Gets the standard DimGray color.

**Value:** `#696969ff`.

<a id="p-electron2d-colors-dodgerblue"></a>
### `public static Color DodgerBlue { get; }`

Gets the standard DodgerBlue color.

**Value:** `#1e90ffff`.

<a id="p-electron2d-colors-firebrick"></a>
### `public static Color Firebrick { get; }`

Gets the standard Firebrick color.

**Value:** `#b22222ff`.

<a id="p-electron2d-colors-floralwhite"></a>
### `public static Color FloralWhite { get; }`

Gets the standard FloralWhite color.

**Value:** `#fffaf0ff`.

<a id="p-electron2d-colors-forestgreen"></a>
### `public static Color ForestGreen { get; }`

Gets the standard ForestGreen color.

**Value:** `#228b22ff`.

<a id="p-electron2d-colors-fuchsia"></a>
### `public static Color Fuchsia { get; }`

Gets the standard Fuchsia color.

**Value:** `#ff00ffff`.

<a id="p-electron2d-colors-gainsboro"></a>
### `public static Color Gainsboro { get; }`

Gets the standard Gainsboro color.

**Value:** `#dcdcdcff`.

<a id="p-electron2d-colors-ghostwhite"></a>
### `public static Color GhostWhite { get; }`

Gets the standard GhostWhite color.

**Value:** `#f8f8ffff`.

<a id="p-electron2d-colors-gold"></a>
### `public static Color Gold { get; }`

Gets the standard Gold color.

**Value:** `#ffd700ff`.

<a id="p-electron2d-colors-goldenrod"></a>
### `public static Color Goldenrod { get; }`

Gets the standard Goldenrod color.

**Value:** `#daa520ff`.

<a id="p-electron2d-colors-gray"></a>
### `public static Color Gray { get; }`

Gets the standard Gray color.

**Value:** `#bebebeff`.

<a id="p-electron2d-colors-green"></a>
### `public static Color Green { get; }`

Gets the standard Green color.

**Value:** `#00ff00ff`.

<a id="p-electron2d-colors-greenyellow"></a>
### `public static Color GreenYellow { get; }`

Gets the standard GreenYellow color.

**Value:** `#adff2fff`.

<a id="p-electron2d-colors-honeydew"></a>
### `public static Color Honeydew { get; }`

Gets the standard Honeydew color.

**Value:** `#f0fff0ff`.

<a id="p-electron2d-colors-hotpink"></a>
### `public static Color HotPink { get; }`

Gets the standard HotPink color.

**Value:** `#ff69b4ff`.

<a id="p-electron2d-colors-indianred"></a>
### `public static Color IndianRed { get; }`

Gets the standard IndianRed color.

**Value:** `#cd5c5cff`.

<a id="p-electron2d-colors-indigo"></a>
### `public static Color Indigo { get; }`

Gets the standard Indigo color.

**Value:** `#4b0082ff`.

<a id="p-electron2d-colors-ivory"></a>
### `public static Color Ivory { get; }`

Gets the standard Ivory color.

**Value:** `#fffff0ff`.

<a id="p-electron2d-colors-khaki"></a>
### `public static Color Khaki { get; }`

Gets the standard Khaki color.

**Value:** `#f0e68cff`.

<a id="p-electron2d-colors-lavender"></a>
### `public static Color Lavender { get; }`

Gets the standard Lavender color.

**Value:** `#e6e6faff`.

<a id="p-electron2d-colors-lavenderblush"></a>
### `public static Color LavenderBlush { get; }`

Gets the standard LavenderBlush color.

**Value:** `#fff0f5ff`.

<a id="p-electron2d-colors-lawngreen"></a>
### `public static Color LawnGreen { get; }`

Gets the standard LawnGreen color.

**Value:** `#7cfc00ff`.

<a id="p-electron2d-colors-lemonchiffon"></a>
### `public static Color LemonChiffon { get; }`

Gets the standard LemonChiffon color.

**Value:** `#fffacdff`.

<a id="p-electron2d-colors-lightblue"></a>
### `public static Color LightBlue { get; }`

Gets the standard LightBlue color.

**Value:** `#add8e6ff`.

<a id="p-electron2d-colors-lightcoral"></a>
### `public static Color LightCoral { get; }`

Gets the standard LightCoral color.

**Value:** `#f08080ff`.

<a id="p-electron2d-colors-lightcyan"></a>
### `public static Color LightCyan { get; }`

Gets the standard LightCyan color.

**Value:** `#e0ffffff`.

<a id="p-electron2d-colors-lightgoldenrod"></a>
### `public static Color LightGoldenrod { get; }`

Gets the standard LightGoldenrod color.

**Value:** `#fafad2ff`.

<a id="p-electron2d-colors-lightgray"></a>
### `public static Color LightGray { get; }`

Gets the standard LightGray color.

**Value:** `#d3d3d3ff`.

<a id="p-electron2d-colors-lightgreen"></a>
### `public static Color LightGreen { get; }`

Gets the standard LightGreen color.

**Value:** `#90ee90ff`.

<a id="p-electron2d-colors-lightpink"></a>
### `public static Color LightPink { get; }`

Gets the standard LightPink color.

**Value:** `#ffb6c1ff`.

<a id="p-electron2d-colors-lightsalmon"></a>
### `public static Color LightSalmon { get; }`

Gets the standard LightSalmon color.

**Value:** `#ffa07aff`.

<a id="p-electron2d-colors-lightseagreen"></a>
### `public static Color LightSeaGreen { get; }`

Gets the standard LightSeaGreen color.

**Value:** `#20b2aaff`.

<a id="p-electron2d-colors-lightskyblue"></a>
### `public static Color LightSkyBlue { get; }`

Gets the standard LightSkyBlue color.

**Value:** `#87cefaff`.

<a id="p-electron2d-colors-lightslategray"></a>
### `public static Color LightSlateGray { get; }`

Gets the standard LightSlateGray color.

**Value:** `#778899ff`.

<a id="p-electron2d-colors-lightsteelblue"></a>
### `public static Color LightSteelBlue { get; }`

Gets the standard LightSteelBlue color.

**Value:** `#b0c4deff`.

<a id="p-electron2d-colors-lightyellow"></a>
### `public static Color LightYellow { get; }`

Gets the standard LightYellow color.

**Value:** `#ffffe0ff`.

<a id="p-electron2d-colors-lime"></a>
### `public static Color Lime { get; }`

Gets the standard Lime color.

**Value:** `#00ff00ff`.

<a id="p-electron2d-colors-limegreen"></a>
### `public static Color LimeGreen { get; }`

Gets the standard LimeGreen color.

**Value:** `#32cd32ff`.

<a id="p-electron2d-colors-linen"></a>
### `public static Color Linen { get; }`

Gets the standard Linen color.

**Value:** `#faf0e6ff`.

<a id="p-electron2d-colors-magenta"></a>
### `public static Color Magenta { get; }`

Gets the standard Magenta color.

**Value:** `#ff00ffff`.

<a id="p-electron2d-colors-maroon"></a>
### `public static Color Maroon { get; }`

Gets the standard Maroon color.

**Value:** `#b03060ff`.

<a id="p-electron2d-colors-mediumaquamarine"></a>
### `public static Color MediumAquamarine { get; }`

Gets the standard MediumAquamarine color.

**Value:** `#66cdaaff`.

<a id="p-electron2d-colors-mediumblue"></a>
### `public static Color MediumBlue { get; }`

Gets the standard MediumBlue color.

**Value:** `#0000cdff`.

<a id="p-electron2d-colors-mediumorchid"></a>
### `public static Color MediumOrchid { get; }`

Gets the standard MediumOrchid color.

**Value:** `#ba55d3ff`.

<a id="p-electron2d-colors-mediumpurple"></a>
### `public static Color MediumPurple { get; }`

Gets the standard MediumPurple color.

**Value:** `#9370dbff`.

<a id="p-electron2d-colors-mediumseagreen"></a>
### `public static Color MediumSeaGreen { get; }`

Gets the standard MediumSeaGreen color.

**Value:** `#3cb371ff`.

<a id="p-electron2d-colors-mediumslateblue"></a>
### `public static Color MediumSlateBlue { get; }`

Gets the standard MediumSlateBlue color.

**Value:** `#7b68eeff`.

<a id="p-electron2d-colors-mediumspringgreen"></a>
### `public static Color MediumSpringGreen { get; }`

Gets the standard MediumSpringGreen color.

**Value:** `#00fa9aff`.

<a id="p-electron2d-colors-mediumturquoise"></a>
### `public static Color MediumTurquoise { get; }`

Gets the standard MediumTurquoise color.

**Value:** `#48d1ccff`.

<a id="p-electron2d-colors-mediumvioletred"></a>
### `public static Color MediumVioletRed { get; }`

Gets the standard MediumVioletRed color.

**Value:** `#c71585ff`.

<a id="p-electron2d-colors-midnightblue"></a>
### `public static Color MidnightBlue { get; }`

Gets the standard MidnightBlue color.

**Value:** `#191970ff`.

<a id="p-electron2d-colors-mintcream"></a>
### `public static Color MintCream { get; }`

Gets the standard MintCream color.

**Value:** `#f5fffaff`.

<a id="p-electron2d-colors-mistyrose"></a>
### `public static Color MistyRose { get; }`

Gets the standard MistyRose color.

**Value:** `#ffe4e1ff`.

<a id="p-electron2d-colors-moccasin"></a>
### `public static Color Moccasin { get; }`

Gets the standard Moccasin color.

**Value:** `#ffe4b5ff`.

<a id="p-electron2d-colors-navajowhite"></a>
### `public static Color NavajoWhite { get; }`

Gets the standard NavajoWhite color.

**Value:** `#ffdeadff`.

<a id="p-electron2d-colors-navyblue"></a>
### `public static Color NavyBlue { get; }`

Gets the standard NavyBlue color.

**Value:** `#000080ff`.

<a id="p-electron2d-colors-oldlace"></a>
### `public static Color OldLace { get; }`

Gets the standard OldLace color.

**Value:** `#fdf5e6ff`.

<a id="p-electron2d-colors-olive"></a>
### `public static Color Olive { get; }`

Gets the standard Olive color.

**Value:** `#808000ff`.

<a id="p-electron2d-colors-olivedrab"></a>
### `public static Color OliveDrab { get; }`

Gets the standard OliveDrab color.

**Value:** `#6b8e23ff`.

<a id="p-electron2d-colors-orange"></a>
### `public static Color Orange { get; }`

Gets the standard Orange color.

**Value:** `#ffa500ff`.

<a id="p-electron2d-colors-orangered"></a>
### `public static Color OrangeRed { get; }`

Gets the standard OrangeRed color.

**Value:** `#ff4500ff`.

<a id="p-electron2d-colors-orchid"></a>
### `public static Color Orchid { get; }`

Gets the standard Orchid color.

**Value:** `#da70d6ff`.

<a id="p-electron2d-colors-palegoldenrod"></a>
### `public static Color PaleGoldenrod { get; }`

Gets the standard PaleGoldenrod color.

**Value:** `#eee8aaff`.

<a id="p-electron2d-colors-palegreen"></a>
### `public static Color PaleGreen { get; }`

Gets the standard PaleGreen color.

**Value:** `#98fb98ff`.

<a id="p-electron2d-colors-paleturquoise"></a>
### `public static Color PaleTurquoise { get; }`

Gets the standard PaleTurquoise color.

**Value:** `#afeeeeff`.

<a id="p-electron2d-colors-palevioletred"></a>
### `public static Color PaleVioletRed { get; }`

Gets the standard PaleVioletRed color.

**Value:** `#db7093ff`.

<a id="p-electron2d-colors-papayawhip"></a>
### `public static Color PapayaWhip { get; }`

Gets the standard PapayaWhip color.

**Value:** `#ffefd5ff`.

<a id="p-electron2d-colors-peachpuff"></a>
### `public static Color PeachPuff { get; }`

Gets the standard PeachPuff color.

**Value:** `#ffdab9ff`.

<a id="p-electron2d-colors-peru"></a>
### `public static Color Peru { get; }`

Gets the standard Peru color.

**Value:** `#cd853fff`.

<a id="p-electron2d-colors-pink"></a>
### `public static Color Pink { get; }`

Gets the standard Pink color.

**Value:** `#ffc0cbff`.

<a id="p-electron2d-colors-plum"></a>
### `public static Color Plum { get; }`

Gets the standard Plum color.

**Value:** `#dda0ddff`.

<a id="p-electron2d-colors-powderblue"></a>
### `public static Color PowderBlue { get; }`

Gets the standard PowderBlue color.

**Value:** `#b0e0e6ff`.

<a id="p-electron2d-colors-purple"></a>
### `public static Color Purple { get; }`

Gets the standard Purple color.

**Value:** `#a020f0ff`.

<a id="p-electron2d-colors-rebeccapurple"></a>
### `public static Color RebeccaPurple { get; }`

Gets the standard RebeccaPurple color.

**Value:** `#663399ff`.

<a id="p-electron2d-colors-red"></a>
### `public static Color Red { get; }`

Gets the standard Red color.

**Value:** `#ff0000ff`.

<a id="p-electron2d-colors-rosybrown"></a>
### `public static Color RosyBrown { get; }`

Gets the standard RosyBrown color.

**Value:** `#bc8f8fff`.

<a id="p-electron2d-colors-royalblue"></a>
### `public static Color RoyalBlue { get; }`

Gets the standard RoyalBlue color.

**Value:** `#4169e1ff`.

<a id="p-electron2d-colors-saddlebrown"></a>
### `public static Color SaddleBrown { get; }`

Gets the standard SaddleBrown color.

**Value:** `#8b4513ff`.

<a id="p-electron2d-colors-salmon"></a>
### `public static Color Salmon { get; }`

Gets the standard Salmon color.

**Value:** `#fa8072ff`.

<a id="p-electron2d-colors-sandybrown"></a>
### `public static Color SandyBrown { get; }`

Gets the standard SandyBrown color.

**Value:** `#f4a460ff`.

<a id="p-electron2d-colors-seagreen"></a>
### `public static Color SeaGreen { get; }`

Gets the standard SeaGreen color.

**Value:** `#2e8b57ff`.

<a id="p-electron2d-colors-seashell"></a>
### `public static Color Seashell { get; }`

Gets the standard Seashell color.

**Value:** `#fff5eeff`.

<a id="p-electron2d-colors-sienna"></a>
### `public static Color Sienna { get; }`

Gets the standard Sienna color.

**Value:** `#a0522dff`.

<a id="p-electron2d-colors-silver"></a>
### `public static Color Silver { get; }`

Gets the standard Silver color.

**Value:** `#c0c0c0ff`.

<a id="p-electron2d-colors-skyblue"></a>
### `public static Color SkyBlue { get; }`

Gets the standard SkyBlue color.

**Value:** `#87ceebff`.

<a id="p-electron2d-colors-slateblue"></a>
### `public static Color SlateBlue { get; }`

Gets the standard SlateBlue color.

**Value:** `#6a5acdff`.

<a id="p-electron2d-colors-slategray"></a>
### `public static Color SlateGray { get; }`

Gets the standard SlateGray color.

**Value:** `#708090ff`.

<a id="p-electron2d-colors-snow"></a>
### `public static Color Snow { get; }`

Gets the standard Snow color.

**Value:** `#fffafaff`.

<a id="p-electron2d-colors-springgreen"></a>
### `public static Color SpringGreen { get; }`

Gets the standard SpringGreen color.

**Value:** `#00ff7fff`.

<a id="p-electron2d-colors-steelblue"></a>
### `public static Color SteelBlue { get; }`

Gets the standard SteelBlue color.

**Value:** `#4682b4ff`.

<a id="p-electron2d-colors-tan"></a>
### `public static Color Tan { get; }`

Gets the standard Tan color.

**Value:** `#d2b48cff`.

<a id="p-electron2d-colors-teal"></a>
### `public static Color Teal { get; }`

Gets the standard Teal color.

**Value:** `#008080ff`.

<a id="p-electron2d-colors-thistle"></a>
### `public static Color Thistle { get; }`

Gets the standard Thistle color.

**Value:** `#d8bfd8ff`.

<a id="p-electron2d-colors-tomato"></a>
### `public static Color Tomato { get; }`

Gets the standard Tomato color.

**Value:** `#ff6347ff`.

<a id="p-electron2d-colors-transparent"></a>
### `public static Color Transparent { get; }`

Gets the standard Transparent color.

**Value:** `#ffffff00`.

<a id="p-electron2d-colors-turquoise"></a>
### `public static Color Turquoise { get; }`

Gets the standard Turquoise color.

**Value:** `#40e0d0ff`.

<a id="p-electron2d-colors-violet"></a>
### `public static Color Violet { get; }`

Gets the standard Violet color.

**Value:** `#ee82eeff`.

<a id="p-electron2d-colors-webgray"></a>
### `public static Color WebGray { get; }`

Gets the standard WebGray color.

**Value:** `#808080ff`.

<a id="p-electron2d-colors-webgreen"></a>
### `public static Color WebGreen { get; }`

Gets the standard WebGreen color.

**Value:** `#008000ff`.

<a id="p-electron2d-colors-webmaroon"></a>
### `public static Color WebMaroon { get; }`

Gets the standard WebMaroon color.

**Value:** `#800000ff`.

<a id="p-electron2d-colors-webpurple"></a>
### `public static Color WebPurple { get; }`

Gets the standard WebPurple color.

**Value:** `#800080ff`.

<a id="p-electron2d-colors-wheat"></a>
### `public static Color Wheat { get; }`

Gets the standard Wheat color.

**Value:** `#f5deb3ff`.

<a id="p-electron2d-colors-white"></a>
### `public static Color White { get; }`

Gets the standard White color.

**Value:** `#ffffffff`.

<a id="p-electron2d-colors-whitesmoke"></a>
### `public static Color WhiteSmoke { get; }`

Gets the standard WhiteSmoke color.

**Value:** `#f5f5f5ff`.

<a id="p-electron2d-colors-yellow"></a>
### `public static Color Yellow { get; }`

Gets the standard Yellow color.

**Value:** `#ffff00ff`.

<a id="p-electron2d-colors-yellowgreen"></a>
### `public static Color YellowGreen { get; }`

Gets the standard YellowGreen color.

**Value:** `#9acd32ff`.

## Lookup behavior

The internal lookup backs `Color(string)` and `Color.FromString`. Lookup removes spaces, hyphens, underscores, apostrophes, and periods, then applies invariant uppercase. It is therefore case-insensitive and separator-insensitive, while the public property names remain exact PascalCase identifiers. Unknown names are handled by the calling `Color` API rather than by a public `Colors` method.

## Lifecycle, threading, and errors

The class has no caller-visible lifecycle or mutation. CLR type initialization constructs the private frozen table once. Successful property access cannot throw under ordinary runtime conditions. Concurrent property access and named lookup are safe. Property access performs only value construction and allocates no managed memory; normalized string lookup may allocate its normalized key.

## Dependencies and interactions

The class depends on `Color`, `System.Collections.Frozen`, and ordinal/invariant string operations. It has no dependency on rendering, SDL, Scene, resources, configuration, input, audio, physics, scripting, an editor, or `System.Drawing`.

## Verification and known limitations

The executable harness reflects the public static properties and requires exactly 146 `Color` properties, verifies that every property round-trips through named parsing, checks aliases and historically distinct web values, and performs parallel lookup. This proves catalog/API consistency, not visual appearance on a future renderer or color-managed display.

## Decisions

- [0024: Typed color values and portable quantization](../decisions/core-math.md#adr-0024)
