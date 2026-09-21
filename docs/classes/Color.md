# Color

Last updated: 2026-09-21

## Declaration

- Source: [`Color.cs`](../../src/Core/Math/Color.cs)
- Namespace: `Electron2D`
- Declaration: `[Serializable] [StructLayout(LayoutKind.Sequential)] public struct Color : IEquatable<Color>`
- Domain: [Core](../domains/core.md)
- Component: [Color values](../components/color-values.md)

## Responsibility and ownership

`Color` is a mutable 16-byte RGBA value with four sequential `float` fields. It represents ordinary nonlinear sRGB color, linear alpha, overbright/HDR intermediate values, HSV/OKHSL conversions, straight-alpha composition, packed integers, HTML-style text, and exact or approximate comparisons. It owns no resources, identity, handles, callbacks, or managed reference state and does not derive from `ElectronObject`.

The zero-initialized value and `new Color()` are transparent black `(0, 0, 0, 0)`. `Colors.Black` is opaque black and `Colors.Transparent` is transparent white. `new Color(r, g, b)` defaults alpha to one. `new Color(existing)` is the RGB-plus-replacement-alpha constructor and therefore also defaults alpha to one; normal assignment copies all four components.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `R`, `G`, `B`, `A` | Mutable floating-point RGBA fields; ordinary constructors do not clamp or reject HDR/non-finite values |
| `R8`, `G8`, `B8`, `A8` | Mutable integer-scale views; getter rounds `component * 255`, maps NaN to zero and overflow to an `int` endpoint; setter divides by 255 without byte-range clamping |
| `H`, `S`, `V` | Mutable HSV views; setters rebuild RGB and preserve alpha |
| `OkHslH`, `OkHslS`, `OkHslL` | Mutable perceptual OKHSL views; getters return finite values clamped to `0..1`, setters rebuild RGB and preserve then clamp alpha |
| `Luminance` | Linear-space relative luminance `0.2126R + 0.7152G + 0.0722B` |
| `this[int]` | RGBA indexing at `0..3`; other indices throw `ArgumentOutOfRangeException` |
| `Color(float, float, float, float = 1)` | Stores components unchanged |
| `Color(Color, float = 1)` | Copies RGB and uses replacement alpha |
| `Color(uint)` | Decodes `0xRRGGBBAA` bytes |
| `Color(ulong)` | Decodes `0xRRRRGGGGBBBBAAAA` words |
| `Color(string)` | Parses valid HTML-style hexadecimal text first, otherwise a normalized standard name |
| `Color(string, float)` | Parses text/name then replaces alpha |
| `Blend(Color)` | Straight-alpha source-over composition with the argument as foreground; zero output alpha returns transparent black |
| `Clamp(Color? = null, Color? = null)` | Componentwise `Math.Clamp`, defaulting to transparent black and opaque white; reversed bounds throw `ArgumentException` |
| `Darkened(float)`, `Lightened(float)` | Unbounded RGB adjustment that preserves alpha |
| `Inverted()` | Complements RGB and preserves alpha |
| `Lerp(Color, float)` | Unbounded componentwise interpolation including alpha |
| `LinearToSrgb()`, `SrgbToLinear()` | Standard piecewise RGB transfer functions; alpha is unchanged |
| `ToAbgr32/64`, `ToArgb32/64`, `ToRgba32/64` | Clamp components to `0..1`, convert NaN to zero, round midpoint-to-even, and pack in the named order |
| `ToHtml(bool = true)` | Lowercase six/eight-digit text without `#`; applies the same stable clamped byte quantization |
| `FromHtml(ReadOnlySpan<char>)` | Parses optional-`#` 3/4/6/8-digit text; empty input retains the typed compatibility result of opaque black; other invalid input throws |
| `HtmlIsValid(ReadOnlySpan<char>)` | Validates exactly the nonempty hexadecimal forms accepted by normal string construction |
| `Color8(byte, byte, byte, byte = 255)` | Divides byte channels by 255 |
| `FromHsv(...)`, `ToHsv(out ...)` | Converts HSV without clamping ordinary inputs; achromatic hue/saturation are zero |
| `FromOkHsl(...)` | Converts published OKHSL coordinates to sRGB, then clamps RGBA to `0..1` |
| `FromRgbe9995(uint)` | Decodes three 9-bit mantissas and one 5-bit shared exponent; alpha is one |
| `FromString(string, Color)` | Parses hexadecimal/name text or returns the fallback for an unknown name |
| Binary `+`, `-`, `*`, `/` | Componentwise arithmetic including alpha; division follows IEEE 754 |
| Scalar `*`, `/`; unary `+`, `-` | Componentwise scale, identity, and four-channel complement |
| `==`, `!=`, `Equals` | Exact component equality; NaN is unequal |
| `<`, `>`, `<=`, `>=` | RGBA lexicographic comparisons; comparisons involving NaN are unordered |
| `IsEqualApprox(Color)` | Per-component scale-aware [`Mathf.Epsilon`](Mathf.md) (`1e-6f`), with exact equality first so equal infinities pass |
| `GetHashCode()` | Hashes all four components |
| `ToString()`, `ToString(string?)` | Invariant-culture `(R, G, B, A)` formatting |

## Parsing and named colors

HTML-style input permits exactly 3, 4, 6, or 8 ASCII hexadecimal digits and at most one leading `#`; it does not trim whitespace. Three/four-digit forms divide each nibble by 15, six/eight-digit forms divide bytes by 255, and missing alpha is one. `HtmlIsValid("")` is false while direct `FromHtml` on an empty span returns opaque black as a retained C# compatibility edge case.

Named parsing is case-insensitive and removes spaces, hyphens, underscores, apostrophes, and periods before lookup. [`Colors`](Colors.md) owns all 146 public names. String constructors reject null or unknown input; `FromString` rejects null but returns its fallback for an unknown name.

## Numeric invariants and error behavior

- Ordinary float construction and arithmetic preserve out-of-range, infinity, and NaN values.
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

The public type depends on canonical scalar [`Mathf`](Mathf.md) plus .NET globalization and interop metadata. `OkColor.cs` is an internal managed OKHSL implementation with its license retained in source. [`ConfigFile`](ConfigFile.md) serializes finite colors through a strict `{"R", "G", "B", "A"}` JSON object. Stored typed property descriptors and [`PackedScene`](PackedScene.md) preserve `Color` directly as a reference-free value.

There is no dependency on Scene, rendering, SDL, input, audio, physics, resources, scripting, or an editor. There is no boolean conversion operator because C# has no appropriate implicit color truth-value contract. No `System.Drawing` dependency or implicit conversion is provided; consumers that import another `Color` type use a normal C# alias.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies layout/defaults, constructors, every component view and index failure, HSV and independent OKHSL primary anchors/interior round trips/saturated-boundary fixture, luminance, blend, clamping failures, unbounded adjustment/interpolation, transfer functions, exhaustive byte round trips, packing order and edge quantization, RGBE9995, all HTML forms and failures, normalized named lookup, all 146 properties, concurrent lookup, every operator family, NaN/infinity behavior, hashing, invariant formatting, strict `ConfigFile` serialization, `PackedScene` storage, and zero warmed numeric allocation.

Execution is currently verified on Linux/.NET 8. Native backend conversion and rendering output cannot be verified until those domains exist. The API intentionally follows the current typed C# surface; language-specific boolean evaluation and native-only named-color-index helpers are permanently excluded, while image/renderer format interactions remain dependency-blocked rather than stubbed.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0024: Typed color values and portable quantization](../decisions/core-math.md#adr-0024)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
