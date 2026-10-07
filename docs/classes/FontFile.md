# FontFile

Last updated: 2026-10-07

**Inherits:** [Font](Font.md), [Resource](Resource.md), ElectronObject · **Inherited By:** —

**Declaration:** `public class FontFile : Font` · **Source:** [FontFile.cs](../../src/Scene/Resources/FontFile.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description and example

Owns scalable font source bytes and independent native metric, shaping and glyph caches. Replacement retains retired native faces only until active readers finish, then explicitly releases their handles and encoded bytes. Already recorded immutable glyph images remain valid; their native renderer payload is released after the first unused frame. Use Data for copied in-memory data or LoadDynamicFont for an operating-system, res:// or user:// file. TTF, OTF, TTC, WOFF and WOFF2 use the SFNT loading path; Type 1 and bitmap-font authoring require separate format integration. Font provides measurement, drawing and ordered borrowed fallbacks.

```csharp
using var font = new FontFile();
font.LoadDynamicFont("res://fonts/body.woff2");
font.OpenTypeFeatureOverrides = new() { ["standard_ligatures"] = 1 };
Vector2 size = font.GetStringSize("Hello", fontSize: 20);
// During a CanvasItem drawing callback:
// DrawString(font, new Vector2(10, 30), "Hello", fontSize: 20);
```

## API summary

| Signature | Contract/default |
| --- | --- |
| `public FontFile()` | Empty bytes, no native initialization. |
| `public byte[] Data { get; set; }` | Copied array; empty initially; maximum 64 MiB. |
| `public string FontName { get; set; }` | Intrinsic family name after loading, empty initially; silent descriptive override. |
| `public string StyleName { get; set; }` | Intrinsic style name after loading, empty initially; silent descriptive override. |
| `public FontStyle FontStyle { get; set; }` | Intrinsic flags after loading, None initially; numeric bits retained. |
| `public int FontWeight { get; set; }` | Intrinsic weight after loading, 400 initially; writes clamp to 100..999. |
| `public int FontStretch { get; set; }` | Intrinsic width percentage after loading, 100 initially; writes clamp to 50..200. |
| `public FontHinting Hinting { get; set; }` | [Light](FontHinting.md) initially. |
| `public FontSubpixelPositioning SubpixelPositioning { get; set; }` | [Auto](FontSubpixelPositioning.md) initially; Disabled after successful file loading/reset. |
| `public bool KeepRoundingRemainders { get; set; }` | True; preserves whole-pixel rounding residuals during shaping. |
| `public float Oversampling { get; set; }` | Finite signed value, zero initially. |
| `public bool ModulateColorGlyphs { get; set; }` | False; controls requested RGB modulation of intrinsic-color glyphs. |
| `public Dictionary<string, int> OpenTypeFeatureOverrides { get; set; }` | Independent dictionary snapshot, initially empty. |
| `public void LoadDynamicFont(string path)` | Validates a new face before committing bytes and reset settings. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Inherited properties plus all twelve typed stored properties. |
| `protected override Resource CreateDuplicateInstance()` | Creates an exact FontFile; unhandled derived factories are rejected. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies bytes, configuration, metadata and fallback graph policy. |
| `protected override void OnResetState()` | Clears source/native state and resets raster settings; preserves features/fallbacks. |
| `protected override void Dispose(bool disposing)` | Releases owned font caches and inherited subscriptions. |

## Data and loading

<a id="data"></a><a id="loaddynamicfont"></a>
Data copies its input and returns copied snapshots. Empty data clears the primary face. A nonempty assignment eagerly validates an independent replacement, refreshes intrinsic metadata, preserves current shaping/raster configuration, and emits Changed after committing and invalidating layout. Equal byte contents still perform replacement. Null is rejected; malformed/unsupported data and input above 64 MiB raise InvalidDataException. Failures preserve previous bytes, native face, settings and metrics.

LoadDynamicFont bounds the file before reading and rejects empty, oversized, unreadable or concurrently resized input. On success it resets Hinting to Light, SubpixelPositioning to Disabled, KeepRoundingRemainders to true, Oversampling to zero and ModulateColorGlyphs to false. Fallbacks and OpenTypeFeatureOverrides survive. Failed reads/decodes do not run that reset. Exceptions replace status-code-only failure reporting, matching the existing Image loading contract.

New state and layout generation are published together under the font lock. Changed runs after releasing the lock, so handlers may read or replace the font. An observer failure does not undo a committed replacement; old native state is still released, and cleanup failures are combined with observer failures when both occur.

An internal constructor stores embedded default font bytes for deferred realization. Byte/configuration queries and resource duplication preserve deferred state; PrimaryData, metadata and actual text queries initialize it on the text worker. This allows non-text GUI defaults to exist without loading text libraries. A failed deferred realization preserves bytes for later replacement.

## Metadata and configuration

<a id="fontname"></a><a id="stylename"></a><a id="fontstyle"></a><a id="fontweight"></a><a id="fontstretch"></a>
Metadata overrides describe the current face; they do not synthesize outlines, change width, or emit Changed. Null names fail. Replacing source data refreshes intrinsic metadata; duplication preserves the current overridden values exactly.

<a id="hinting"></a><a id="subpixelpositioning"></a><a id="keeproundingremainders"></a><a id="oversampling"></a><a id="modulatecolorglyphs"></a>
Changed scalar settings invalidate text before emitting Changed; equal writes are silent. Raw hinting/positioning values remain stored: unrecognized hinting uses normal raster fitting, unrecognized positioning uses whole pixels. Rounding remainders concern advance accumulation, while half/quarter positioning uses real raster phases. Raster hinting does not replace the separate unhinted shaping metric policy.

An explicit positive per-draw oversampling value takes precedence. A positive FontFile override supplies automatic draw requests; nonpositive values retain the default policy. Nonfinite values are rejected. Color glyphs always receive requested alpha modulation; ModulateColorGlyphs controls their RGB modulation during ordinary glyph drawing. Outline drawing preserves intrinsic RGB regardless of that setting.

<a id="opentypefeatureoverrides"></a>
Feature dictionaries are copied on assignment and query. Registered readable names such as standard_ligatures map to tags such as liga; all 246 registered names include character_variant_01..99, stylistic_set_01..20 and the registered variation aliases. Unknown names have every custom_ segment removed, are converted to ASCII with non-ASCII scalars replaced by spaces, terminate at NUL, and use at most four bytes padded with spaces. An empty name maps to tag zero. Case is significant. Raw unknown tags are retained and passed to shaping.

Nonnegative integer values are passed to HarfBuzz; zero disables a boolean feature. Negative values remain stored but are omitted from shaping. Dictionary insertion order is retained when aliases resolve to the same tag. Assignment invalidates text and emits Changed, including equal dictionaries, so cached output changes immediately. Inherited Font.GetOpenTypeFeatures remains the base query contract and is distinct from these per-file overrides.

## Copying, storage and verification

<a id="getpropertydescriptors"></a><a id="createduplicateinstance"></a><a id="copycustomstateto"></a><a id="onresetstate"></a><a id="dispose"></a>
All twelve properties have typed stored descriptors. Data precedes metadata/settings during restoration. The positioning revert value is the constructor default Auto. Copies own independent native caches and byte views; feature dictionaries are independent, while fallbacks follow Resource's shallow/deep graph policy and preserve repeated aliases. ResetState clears bytes/native state and applies file-load reset settings without discarding fallbacks/features. Disposal releases owned data and does not dispose borrowed fallback resources.

[FontFileTests](../../tests/Electron2D.Tests/FontFileTests.cs) passes defaults, readable/raw feature equivalence, native glyph effects, negative-feature omission, deferred realization, data/file rollback, 64 MiB limits, metadata, callback ordering/failures, exact copies, graph aliases and typed descriptors. Sixty-four measured scalar configuration cycles after sixty-four warmup cycles allocate zero managed bytes. [NativeFontPrecisionTests](../../tests/Electron2D.Tests/NativeFontPrecisionTests.cs) adds exact metric and raster oracles. These focused results are Linux x64 checks; they do not establish other native platforms, all font/color formats or owner visual acceptance.

Indexed bitmap/glyph/size/variation/palette cache authoring, system font discovery/fallback and MSDF retain their own executable integration dependencies. Typed FontVariation instances and copied primary-face axis/palette metadata execute separately. See [coverage](../coverage/classes/FontFile.md), [ADR 0046](../decisions/rendering.md#adr-0046) and [NativeFontPrecision](NativeFontPrecision.md).

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

Font now exposes copied axis ranges and predefined palette metadata. [FontVariation](FontVariation.md) and Font.FindVariation borrow the encoded source to create independent coordinates, collection face, outline/baseline and palette instances. These resource instances do not implement FontFile mutable indexed cache authoring.
