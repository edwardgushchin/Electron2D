# Colors

Last updated: 2026-09-21

## Declaration

- Source: [`Colors.cs`](../../src/Core/Math/Colors.cs)
- Namespace: `Electron2D`
- Declaration: `public static class Colors`
- Domain: [Core](../domains/core.md)
- Component: [Color values](../components/color-values.md)

## Responsibility and ownership

`Colors` is the compile-time catalog used by [`Color`](Color.md) named parsing. Every public property returns a `Color` value and owns no mutable instance, resource, native handle, or lifecycle. The internal lookup is created once as a frozen ordinal dictionary and is thereafter read-only and safe for concurrent lookup.

`Transparent` is transparent white `(1, 1, 1, 0)`; it is deliberately different from `default(Color)`, which is transparent black. `Aqua`/`Cyan`, `Fuchsia`/`Magenta`, and `Green`/`Lime` are equal aliases. Historical X11 `Gray`, `Green`, `Maroon`, and `Purple` values remain separate from the four `Web*` values.

## Complete public API

The complete surface is 146 get-only static properties:

| Property | RGBA hexadecimal value |
| --- | --- |
| `AliceBlue` | `#f0f8ffff` |
| `AntiqueWhite` | `#faebd7ff` |
| `Aqua` | `#00ffffff` |
| `Aquamarine` | `#7fffd4ff` |
| `Azure` | `#f0ffffff` |
| `Beige` | `#f5f5dcff` |
| `Bisque` | `#ffe4c4ff` |
| `Black` | `#000000ff` |
| `BlanchedAlmond` | `#ffebcdff` |
| `Blue` | `#0000ffff` |
| `BlueViolet` | `#8a2be2ff` |
| `Brown` | `#a52a2aff` |
| `Burlywood` | `#deb887ff` |
| `CadetBlue` | `#5f9ea0ff` |
| `Chartreuse` | `#7fff00ff` |
| `Chocolate` | `#d2691eff` |
| `Coral` | `#ff7f50ff` |
| `CornflowerBlue` | `#6495edff` |
| `Cornsilk` | `#fff8dcff` |
| `Crimson` | `#dc143cff` |
| `Cyan` | `#00ffffff` |
| `DarkBlue` | `#00008bff` |
| `DarkCyan` | `#008b8bff` |
| `DarkGoldenrod` | `#b8860bff` |
| `DarkGray` | `#a9a9a9ff` |
| `DarkGreen` | `#006400ff` |
| `DarkKhaki` | `#bdb76bff` |
| `DarkMagenta` | `#8b008bff` |
| `DarkOliveGreen` | `#556b2fff` |
| `DarkOrange` | `#ff8c00ff` |
| `DarkOrchid` | `#9932ccff` |
| `DarkRed` | `#8b0000ff` |
| `DarkSalmon` | `#e9967aff` |
| `DarkSeaGreen` | `#8fbc8fff` |
| `DarkSlateBlue` | `#483d8bff` |
| `DarkSlateGray` | `#2f4f4fff` |
| `DarkTurquoise` | `#00ced1ff` |
| `DarkViolet` | `#9400d3ff` |
| `DeepPink` | `#ff1493ff` |
| `DeepSkyBlue` | `#00bfffff` |
| `DimGray` | `#696969ff` |
| `DodgerBlue` | `#1e90ffff` |
| `Firebrick` | `#b22222ff` |
| `FloralWhite` | `#fffaf0ff` |
| `ForestGreen` | `#228b22ff` |
| `Fuchsia` | `#ff00ffff` |
| `Gainsboro` | `#dcdcdcff` |
| `GhostWhite` | `#f8f8ffff` |
| `Gold` | `#ffd700ff` |
| `Goldenrod` | `#daa520ff` |
| `Gray` | `#bebebeff` |
| `Green` | `#00ff00ff` |
| `GreenYellow` | `#adff2fff` |
| `Honeydew` | `#f0fff0ff` |
| `HotPink` | `#ff69b4ff` |
| `IndianRed` | `#cd5c5cff` |
| `Indigo` | `#4b0082ff` |
| `Ivory` | `#fffff0ff` |
| `Khaki` | `#f0e68cff` |
| `Lavender` | `#e6e6faff` |
| `LavenderBlush` | `#fff0f5ff` |
| `LawnGreen` | `#7cfc00ff` |
| `LemonChiffon` | `#fffacdff` |
| `LightBlue` | `#add8e6ff` |
| `LightCoral` | `#f08080ff` |
| `LightCyan` | `#e0ffffff` |
| `LightGoldenrod` | `#fafad2ff` |
| `LightGray` | `#d3d3d3ff` |
| `LightGreen` | `#90ee90ff` |
| `LightPink` | `#ffb6c1ff` |
| `LightSalmon` | `#ffa07aff` |
| `LightSeaGreen` | `#20b2aaff` |
| `LightSkyBlue` | `#87cefaff` |
| `LightSlateGray` | `#778899ff` |
| `LightSteelBlue` | `#b0c4deff` |
| `LightYellow` | `#ffffe0ff` |
| `Lime` | `#00ff00ff` |
| `LimeGreen` | `#32cd32ff` |
| `Linen` | `#faf0e6ff` |
| `Magenta` | `#ff00ffff` |
| `Maroon` | `#b03060ff` |
| `MediumAquamarine` | `#66cdaaff` |
| `MediumBlue` | `#0000cdff` |
| `MediumOrchid` | `#ba55d3ff` |
| `MediumPurple` | `#9370dbff` |
| `MediumSeaGreen` | `#3cb371ff` |
| `MediumSlateBlue` | `#7b68eeff` |
| `MediumSpringGreen` | `#00fa9aff` |
| `MediumTurquoise` | `#48d1ccff` |
| `MediumVioletRed` | `#c71585ff` |
| `MidnightBlue` | `#191970ff` |
| `MintCream` | `#f5fffaff` |
| `MistyRose` | `#ffe4e1ff` |
| `Moccasin` | `#ffe4b5ff` |
| `NavajoWhite` | `#ffdeadff` |
| `NavyBlue` | `#000080ff` |
| `OldLace` | `#fdf5e6ff` |
| `Olive` | `#808000ff` |
| `OliveDrab` | `#6b8e23ff` |
| `Orange` | `#ffa500ff` |
| `OrangeRed` | `#ff4500ff` |
| `Orchid` | `#da70d6ff` |
| `PaleGoldenrod` | `#eee8aaff` |
| `PaleGreen` | `#98fb98ff` |
| `PaleTurquoise` | `#afeeeeff` |
| `PaleVioletRed` | `#db7093ff` |
| `PapayaWhip` | `#ffefd5ff` |
| `PeachPuff` | `#ffdab9ff` |
| `Peru` | `#cd853fff` |
| `Pink` | `#ffc0cbff` |
| `Plum` | `#dda0ddff` |
| `PowderBlue` | `#b0e0e6ff` |
| `Purple` | `#a020f0ff` |
| `RebeccaPurple` | `#663399ff` |
| `Red` | `#ff0000ff` |
| `RosyBrown` | `#bc8f8fff` |
| `RoyalBlue` | `#4169e1ff` |
| `SaddleBrown` | `#8b4513ff` |
| `Salmon` | `#fa8072ff` |
| `SandyBrown` | `#f4a460ff` |
| `SeaGreen` | `#2e8b57ff` |
| `Seashell` | `#fff5eeff` |
| `Sienna` | `#a0522dff` |
| `Silver` | `#c0c0c0ff` |
| `SkyBlue` | `#87ceebff` |
| `SlateBlue` | `#6a5acdff` |
| `SlateGray` | `#708090ff` |
| `Snow` | `#fffafaff` |
| `SpringGreen` | `#00ff7fff` |
| `SteelBlue` | `#4682b4ff` |
| `Tan` | `#d2b48cff` |
| `Teal` | `#008080ff` |
| `Thistle` | `#d8bfd8ff` |
| `Tomato` | `#ff6347ff` |
| `Transparent` | `#ffffff00` |
| `Turquoise` | `#40e0d0ff` |
| `Violet` | `#ee82eeff` |
| `WebGray` | `#808080ff` |
| `WebGreen` | `#008000ff` |
| `WebMaroon` | `#800000ff` |
| `WebPurple` | `#800080ff` |
| `Wheat` | `#f5deb3ff` |
| `White` | `#ffffffff` |
| `WhiteSmoke` | `#f5f5f5ff` |
| `Yellow` | `#ffff00ff` |
| `YellowGreen` | `#9acd32ff` |

There are no public constructors, methods, mutable fields, events, or nested types.

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
