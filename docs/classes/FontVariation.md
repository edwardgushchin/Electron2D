# FontVariation

Last updated: 2026-10-07

**Inherits:** [Font](Font.md), [Resource](Resource.md), ElectronObject · **Inherited By:** —

**Declaration:** `public partial class FontVariation : Font` · **Source:** [FontVariation.cs](../../src/Scene/Resources/FontVariation.cs) · **Component:** [Text](../components/text.md)

## Description and example

Borrows a base Font and creates an independent native face, shaping font and glyph cache. OpenType design coordinates, collection face, synthetic bold/thinning, outline transform, baseline and color palette affect actual glyphs. Spacing affects shared Font layout. [Font.FindVariation](Font.md#findvariation) creates a caller-owned instance with the same configuration.

```csharp
using var source = new FontFile();
source.LoadDynamicFont("res://fonts/variable.ttf");
using var heading = new FontVariation { BaseFont = source, SpacingGlyph = 1 };
heading.SetVariationOpenType(new() { ["weight"] = 700 });
Vector2 measured = heading.GetStringSize("Heading", fontSize: 28);
// Inside a CanvasItem drawing callback:
// DrawString(heading, new Vector2(16, 40), "Heading", fontSize: 28);
```

## API summary

| Signature | Contract/default |
| --- | --- |
| `public FontVariation()` | Lazy instance; no native initialization until a font query. |
| `public Font? BaseFont { get; set; }` | Borrowed base; null selects ThemeDB.FallbackFont. |
| `public Dictionary<uint, float> VariationOpenType { get; set; }` | Copied design coordinates by numeric OpenType tag; empty initially. |
| `public void SetVariationOpenType(Dictionary<string, float> coordinates)` | Replaces coordinates from registered aliases or custom tag names. |
| `public Dictionary<string, int> OpenTypeFeatures { get; set; }` | Copied span features; empty initially. |
| `public override Dictionary<string, int> GetOpenTypeFeatures()` | Independent feature snapshot. |
| `public float VariationEmbolden { get; set; }` | Finite outline strength; zero initially; negative thins. |
| `public Transform VariationTransform { get; set; }` | Finite outline basis; Identity initially; translation stored but ignored. |
| `public int VariationFaceIndex { get; set; }` | Nonnegative collection index; zero initially. |
| `public float BaselineOffset { get; set; }` | Finite fraction of ascent plus descent; zero initially. |
| `public int PaletteIndex { get; set; }` | Zero initially; realization clamps to available palettes. |
| `public Color[] PaletteCustomColors { get; set; }` | Copied finite colors; empty initially. |
| `public void SetSpacing(TextSpacingType spacing, int value)` | Assigns signed pixel spacing for Glyph/Space/Top/Bottom. |
| `public override int GetSpacing(TextSpacingType spacing)` | Reads the selected extra spacing. |
| `public int SpacingGlyph { get; set; }` | Extra glyph width; zero initially. |
| `public int SpacingSpace { get; set; }` | Extra space width; zero initially. |
| `public int SpacingTop { get; set; }` | Extra top pixels; zero initially. |
| `public int SpacingBottom { get; set; }` | Extra bottom pixels; zero initially. |
| `protected override Resource CreateDuplicateInstance()` | Exact factory; derived resources must supply their own factory. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies configuration and follows resource graph duplication policy. |
| `protected override void OnResetState()` | Resets instance configuration; inherited fallback graph remains. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Inherited descriptors and all thirteen stored variation properties. |
| `protected override void Dispose(bool disposing)` | Retires owned native data and detaches borrowed/static subscriptions. |

## Coordinates and shaping

Numeric keys are four-byte OpenType tags, for example `0x77676874` for wght. GetSupportedVariationList returns the same numeric keys with [FontVariationAxis](FontVariationAxis.md) bounds. SetVariationOpenType accepts weight, width and the registered aliases already used by FontFile feature names. Custom names remove custom_ segments, use four ASCII scalars, pad short tags with spaces and terminate at NUL. Aliases that resolve to the same tag use the last dictionary entry. Coordinates are finite; at most 65,536 entries are accepted. Missing axes use defaults; known coordinates clamp to bounds; unknown axes remain stored and are ignored by the native face. Assigning/querying dictionaries copies them.

OpenTypeFeatures follows FontFile's registered/custom feature naming and negative-value omission rules. Span features override nonnegative file-level defaults with the same tag on every selected face, including fallbacks. Nested FontVariation configuration selects a new instance of the underlying source; coordinates, outline strength and spacing do not compose with the inner instance. Its inherited fallback selection still follows the borrowed base chain.

## Outlines, baseline and palettes

VariationEmbolden changes outlines before rasterization and outline stroke. Horizontal advances receive strength × logical font size / 64; it does not scale with drawing oversampling. VariationTransform uses the native outline matrix convention: `new Transform(new(1, slant), new(0, 1), default)` slants glyphs. Basis entries are bounded to ±32,767 for portable 16.16 conversion. Translation never moves the baseline. Bitmap/color strikes retain their native raster path; outline transforms apply only to outline glyphs. Transformed raster extents retain the 16,384-pixel texture bound.

BaselineOffset shifts horizontal glyphs downward, or vertical glyphs horizontally, by its fraction of native ascent plus descent. It changes shaped offsets without changing line metrics. Signed spacing changes shared layout and line metrics; it does not create a new native face. The terminal advancing glyph receives no extra glyph/space spacing. A zero space setting uses glyph spacing. Synthetic advances enter the existing rounding/residual stage before rounding.

PaletteIndex clamps only when a face has predefined palettes; the assigned value remains stored. PaletteCustomColors overrides entries up to the palette length; transparent black preserves that predefined entry. Finite channels clamp to [0, 1] and quantize to bytes. GetPaletteColors always returns the predefined palette, independently of selected/custom instance colors. GetPaletteName returns an optional SFNT palette name. Unsupported palettes/axes report empty metadata through Font queries.

## Ownership, events, storage and errors

BaseFont and Fallbacks are borrowed. Empty own Fallbacks inherits the base's effective fallback list; a nonempty own list replaces it, including retained null slots. Base and fallback graphs reject cycles and chains beyond sixty-four levels. Source replacement/configuration and default-theme changes invalidate layouts and retire the instance face; active font readers keep retired native data until they finish. Already recorded glyph images retain the existing independent canvas snapshot contract. A disposed required source causes ObjectDisposedException. Borrowed source and static theme subscriptions use weak targets and are removed during deterministic disposal.

Equal configuration assignments are silent; changed configurations emit Changed after committing coherent state. BaseFont changes also notify the property list. Public font state is synchronized, native work stays on the existing serialized font owner, and drawing keeps the canvas owner/recording restrictions. New configurations and public snapshots are cold operations; cached layouts and prepared glyph rendering reuse storage.

Negative face indices, invalid spacing categories, nonfinite coordinates/colors/strength/baseline and out-of-range transform bases are rejected. An unavailable collection face raises the native load exception on realization; correcting the index permits retry. Malformed font metadata raises InvalidDataException. Oversized transformed rasters fail before native bitmap allocation. Resource duplication copies all instance settings and uses inherited borrowed/deep graph policy; source bytes are not duplicated by shallow copies. Registered resource factories and typed numeric-coordinate codecs support storage and fresh-process loading.

[FontVariationTests](../../tests/Electron2D.Tests/FontVariationTests.cs) exercises native metadata, clamping, actual shaping/raster changes, palettes, collection faces, retired readers, cycles, borrowed ownership, rich markup, duplication and fresh-process storage. Native and prepared-allocation evidence is recorded in [Text](../components/text.md); it establishes only the exercised local backend/platform.

## Bitmap/indexed font integration

[Bitmap font authoring](../components/bitmap-fonts.md) connects FontFile indexed image/glyph/kerning/metric records and matching configured FontVariation resources to the existing HarfBuzz and common canvas/control path. Copied pixel UV regions preserve clipping and recorded image snapshots; authored publication retires native data after active readers finish. Text/binary v3 import, typed archive/fresh-process restoration and current Linux GPU/compatibility prepared output are exercised. Source policies and other platform/native-allocator gates remain explicit.

## System font integration

[System font matching](../components/system-fonts.md) adds installed families/styles/logical collection faces and owned automatic text fallback over the shared native owner and canvas path. FontFile.AllowSystemFallback defaults to true; explicit resources retain precedence and explicit support queries remain distinct from automatic rendered coverage. Active parent readers retain retired fallback faces through policy changes. SystemFont archives store preferences and rematch the host. The current Linux catalog and both canvas consumers are exercised; CoreText/DirectWrite, extra raster/MSDF, native allocator and foreign acceptance gates remain explicit.
