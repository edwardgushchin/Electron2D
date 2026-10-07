# FontFile

Last updated: 2026-10-07

**Inherits:** [Font](Font.md), [Resource](Resource.md), ElectronObject · **Inherited By:** —

**Declaration:** `public class FontFile : Font` · **Source:** [FontFile.cs](../../src/Scene/Resources/FontFile.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description and example

Owns scalable font source bytes and independent native metric, shaping and glyph caches. Replacement retains retired native faces only until active readers finish, then explicitly releases their handles and encoded bytes. Already recorded immutable glyph images remain valid; their native renderer payload is released after the first unused frame. Use Data for copied in-memory data or LoadDynamicFont for an operating-system, res:// or user:// file. TTF, OTF, TTC, WOFF and WOFF2 use the SFNT loading path; Type 1 requires separate non-SFNT integration; bitmap font import and indexed authoring execute through the common path below. Font provides measurement, drawing and ordered borrowed fallbacks.

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
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Inherited properties plus all fifteen typed stored properties and the internal cache snapshot. |
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
All fifteen scalar/source properties plus the internal typed cache snapshot have typed stored descriptors. Data precedes metadata/settings during restoration. The positioning revert value is the constructor default Auto. Copies own independent native caches and byte views; feature dictionaries are independent, while fallbacks follow Resource's shallow/deep graph policy and preserve repeated aliases. ResetState clears bytes/native state and applies file-load reset settings without discarding fallbacks/features. Disposal releases owned data and does not dispose borrowed fallback resources.

[FontFileTests](../../tests/Electron2D.Tests/FontFileTests.cs) passes defaults, readable/raw feature equivalence, native glyph effects, negative-feature omission, deferred realization, data/file rollback, 64 MiB limits, metadata, callback ordering/failures, exact copies, graph aliases and typed descriptors. Sixty-four measured scalar configuration cycles after sixty-four warmup cycles allocate zero managed bytes. [NativeFontPrecisionTests](../../tests/Electron2D.Tests/NativeFontPrecisionTests.cs) adds exact metric and raster oracles. These focused results are Linux x64 checks; they do not establish other native platforms, all font/color formats or owner visual acceptance.

Bitmap loading and indexed glyph/size/texture/kerning/instance authoring now execute as described below. language/script policies, additional native raster modes and MSDF retain their exact coverage dependencies. Typed FontVariation instances and copied primary-face axis/palette metadata execute separately. See [coverage](../coverage/classes/FontFile.md), [ADR 0046](../decisions/rendering.md#adr-0046) and [NativeFontPrecision](NativeFontPrecision.md).

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

Font now exposes copied axis ranges and predefined palette metadata. [FontVariation](FontVariation.md) and Font.FindVariation borrow the encoded source to create independent coordinates, collection face, outline/baseline and palette instances. Configured FontVariation resources consume an independently copied matching indexed cache when instance coordinates and spacing agree.

## Bitmap and indexed cache authoring

This API implements [bitmap font authoring](../components/bitmap-fonts.md). A fresh resource has zero indexed entries. Reading a primary face or a cache creates needed entries through index 4095; ClearCache removes them while retaining encoded scalable bytes. Cache indices compact after RemoveCache. Size keys are `(positive pixels, nonnegative outline thickness)` bounded to 16384. Source replacement clears authored entries. FixedSize and FixedSizeScaleMode queries remain passive.

Bitmap fonts map supported Unicode scalars directly to glyph indices. Scalable fonts retain source glyph indices and optional variation selectors. RenderGlyph/RenderRange populate actual native glyph pixels and atlas metadata; a character range spans at most 65537 scalars and skips surrogates. Dynamic records generated at the base phase remain observations of native rendering; explicit glyph/page edits become authored replacements. Native variants at other phases or oversampling factors retain their own caches.

The cache owns copied RGBA8 page pixels. GetTextureImage returns a new caller-owned Image. SetTextureImage copies synchronously and keeps caller ownership; empty, mipmapped, oversized or disposed input fails before publication. Texture indices are bounded to 65535. Removing/clearing textures leaves glyph records; removing/clearing glyphs leaves page pixels. RemoveTexture compacts pages and retains the original glyph page indices, so callers must repair affected associations. Out-of-range positive page references draw no image, while logical advances remain usable.

Glyph offsets, dimensions and UV rectangles use logical/pixel coordinates, never normalized UVs. Dimensions must be nonnegative and finite; signed offsets/advances/metric overrides remain valid. Texture rectangles feed the same clipping and recorded CPU image snapshots as ordinary text. A manually authored color image follows ModulateColorGlyphs when imported as an intrinsic-color font.

Packing offsets are copied `x, y, remaining width, shelf height` tuples with finite-page bounds and positive heights. Subsequent native preparation consumes those shelves. Missing page images fail; bad tuple counts or geometry preserve previous metadata. Rendered pages use a one-pixel extruded glyph border and minimum 256-pixel square pages, rather than guaranteeing a particular atlas layout. Atlas slots and packing coordinates are observations and authoring data.

Cache metric overrides supply the same common Font measurement/layout. Kerning overrides use finite horizontal/vertical pixel adjustments. GetKerning returns an override or native FreeType kerning converted from 26.6 to logical pixels; GetKerningList enumerates overrides only. Bitmap pairs affect real HarfBuzz glyph advances. Scalable advance overrides replace a shaped glyph's logical advance; they do not introduce new contextual substitution tables. Scalar spacing follows the inherited common layout's terminal-glyph and space policy.

Each entry has independent face/coordinates/outline transform/embolden/baseline/spacing configuration. A native configuration change validates a replacement face and clears obsolete dynamic size records. Matching Font.FindVariation instances borrow this file and copy the corresponding authored cache; later file edits retire and rebuild retained variants. Bitmap transforms/embolden apply to native outlines only and do not transform authored page pixels.

FixedSize selects a bitmap source size. Disable=0 preserves original pixels/metrics; IntegerOnly=1 rounds the ratio away from zero at a half boundary; Enabled=2 uses the exact ratio. Zero disables fixed-size selection. These policies affect bitmap glyph geometry, advances, kerning and line metrics together; scalable source sizing remains native.

Authored edits copy cache state, validate/realize it, invalidate layout and publish before Changed outside the resource lock. Observer failure does not roll back publication; retired readers finish before native cleanup. Explicit authoring/import/archive operations allocate outside prepared frame processing. The hidden `_font_cache` typed byte-array descriptor persists bounded cache records and copied pixels, not native handles or texture identities; its versioned binary payload is capped at 64 MiB. Duplication and fresh-process restoration rebuild independent ownership.

### API summary

| Signature | Contract |
| --- | --- |
| `public System.Void ClearCache()` | See the member description below. |
| `public System.Void ClearGlyphs(System.Int32 cacheIndex, Electron2D.Vector2i size)` | See the member description below. |
| `public System.Void ClearKerningMap(System.Int32 cacheIndex, System.Int32 size)` | See the member description below. |
| `public System.Void ClearSizeCache(System.Int32 cacheIndex)` | See the member description below. |
| `public System.Void ClearTextures(System.Int32 cacheIndex, Electron2D.Vector2i size)` | See the member description below. |
| `public System.Single GetCacheAscent(System.Int32 cacheIndex, System.Int32 size)` | See the member description below. |
| `public System.Int32 GetCacheCount()` | See the member description below. |
| `public System.Single GetCacheDescent(System.Int32 cacheIndex, System.Int32 size)` | See the member description below. |
| `public System.Single GetCacheScale(System.Int32 cacheIndex, System.Int32 size)` | See the member description below. |
| `public System.Single GetCacheUnderlinePosition(System.Int32 cacheIndex, System.Int32 size)` | See the member description below. |
| `public System.Single GetCacheUnderlineThickness(System.Int32 cacheIndex, System.Int32 size)` | See the member description below. |
| `public System.Int32 GetCharFromGlyphIndex(System.Int32 size, System.Int32 glyph)` | See the member description below. |
| `public System.Single GetEmbolden(System.Int32 cacheIndex)` | See the member description below. |
| `public System.Single GetExtraBaselineOffset(System.Int32 cacheIndex)` | See the member description below. |
| `public System.Int32 GetExtraSpacing(System.Int32 cacheIndex, Electron2D.TextSpacingType spacing)` | See the member description below. |
| `public System.Int32 GetFaceIndex(System.Int32 cacheIndex)` | See the member description below. |
| `public Electron2D.Vector2 GetGlyphAdvance(System.Int32 cacheIndex, System.Int32 size, System.Int32 glyph)` | See the member description below. |
| `public System.Int32 GetGlyphIndex(System.Int32 size, System.Int32 character, System.Int32 variationSelector)` | See the member description below. |
| `public System.Int32[] GetGlyphList(System.Int32 cacheIndex, Electron2D.Vector2i size)` | See the member description below. |
| `public Electron2D.Vector2 GetGlyphOffset(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph)` | See the member description below. |
| `public Electron2D.Vector2 GetGlyphSize(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph)` | See the member description below. |
| `public System.Int32 GetGlyphTextureIndex(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph)` | See the member description below. |
| `public Electron2D.Rect2 GetGlyphUVRect(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph)` | See the member description below. |
| `public Electron2D.Vector2 GetKerning(System.Int32 cacheIndex, System.Int32 size, Electron2D.Vector2i glyphPair)` | See the member description below. |
| `public Electron2D.Vector2i[] GetKerningList(System.Int32 cacheIndex, System.Int32 size)` | See the member description below. |
| `public Electron2D.Vector2i[] GetSizeCacheList(System.Int32 cacheIndex)` | See the member description below. |
| `public override System.Int32 GetSpacing(Electron2D.TextSpacingType spacing)` | See the member description below. |
| `public System.Int32 GetTextureCount(System.Int32 cacheIndex, Electron2D.Vector2i size)` | See the member description below. |
| `public Electron2D.Image GetTextureImage(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 textureIndex)` | See the member description below. |
| `public System.Int32[] GetTextureOffsets(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 textureIndex)` | See the member description below. |
| `public Electron2D.Transform GetTransform(System.Int32 cacheIndex)` | See the member description below. |
| `public System.Collections.Generic.Dictionary<System.UInt32, System.Single> GetVariationCoordinates(System.Int32 cacheIndex)` | See the member description below. |
| `public System.Void LoadBitmapFont(System.String path)` | See the member description below. |
| `public System.Void RemoveCache(System.Int32 cacheIndex)` | See the member description below. |
| `public System.Void RemoveGlyph(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph)` | See the member description below. |
| `public System.Void RemoveKerning(System.Int32 cacheIndex, System.Int32 size, Electron2D.Vector2i glyphPair)` | See the member description below. |
| `public System.Void RemoveSizeCache(System.Int32 cacheIndex, Electron2D.Vector2i size)` | See the member description below. |
| `public System.Void RemoveTexture(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 textureIndex)` | See the member description below. |
| `public System.Void RenderGlyph(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph)` | See the member description below. |
| `public System.Void RenderRange(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 start, System.Int32 end)` | See the member description below. |
| `public System.Void SetCacheAscent(System.Int32 cacheIndex, System.Int32 size, System.Single value)` | See the member description below. |
| `public System.Void SetCacheDescent(System.Int32 cacheIndex, System.Int32 size, System.Single value)` | See the member description below. |
| `public System.Void SetCacheScale(System.Int32 cacheIndex, System.Int32 size, System.Single value)` | See the member description below. |
| `public System.Void SetCacheUnderlinePosition(System.Int32 cacheIndex, System.Int32 size, System.Single value)` | See the member description below. |
| `public System.Void SetCacheUnderlineThickness(System.Int32 cacheIndex, System.Int32 size, System.Single value)` | See the member description below. |
| `public System.Void SetEmbolden(System.Int32 cacheIndex, System.Single value)` | See the member description below. |
| `public System.Void SetExtraBaselineOffset(System.Int32 cacheIndex, System.Single value)` | See the member description below. |
| `public System.Void SetExtraSpacing(System.Int32 cacheIndex, Electron2D.TextSpacingType spacing, System.Int32 value)` | See the member description below. |
| `public System.Void SetFaceIndex(System.Int32 cacheIndex, System.Int32 value)` | See the member description below. |
| `public System.Void SetGlyphAdvance(System.Int32 cacheIndex, System.Int32 size, System.Int32 glyph, Electron2D.Vector2 advance)` | See the member description below. |
| `public System.Void SetGlyphOffset(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph, Electron2D.Vector2 offset)` | See the member description below. |
| `public System.Void SetGlyphSize(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph, Electron2D.Vector2 glyphSize)` | See the member description below. |
| `public System.Void SetGlyphTextureIndex(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph, System.Int32 textureIndex)` | See the member description below. |
| `public System.Void SetGlyphUVRect(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph, Electron2D.Rect2 uvRect)` | See the member description below. |
| `public System.Void SetKerning(System.Int32 cacheIndex, System.Int32 size, Electron2D.Vector2i glyphPair, Electron2D.Vector2 value)` | See the member description below. |
| `public System.Void SetTextureImage(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 textureIndex, Electron2D.Image image)` | See the member description below. |
| `public System.Void SetTextureOffsets(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 textureIndex, System.Int32[] offsets)` | See the member description below. |
| `public System.Void SetTransform(System.Int32 cacheIndex, Electron2D.Transform value)` | See the member description below. |
| `public System.Void SetVariationCoordinates(System.Int32 cacheIndex, System.Collections.Generic.Dictionary<System.UInt32, System.Single> value)` | See the member description below. |
| `public System.Int32 FixedSize { get; set; }` | Bitmap-size policy. |
| `public Electron2D.FixedSizeScaleMode FixedSizeScaleMode { get; set; }` | Bitmap-size policy. |

### Member descriptions

<a id="clearcache"></a>
`public System.Void ClearCache()`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="clearglyphs"></a>
`public System.Void ClearGlyphs(System.Int32 cacheIndex, Electron2D.Vector2i size)`

Reads a copied typed instance setting or validates and publishes an independently realized native instance. Failed face/coordinate realization preserves previous records and rendering. Disposed resources reject access.

<a id="clearkerningmap"></a>
`public System.Void ClearKerningMap(System.Int32 cacheIndex, System.Int32 size)`

Queries, stores or removes glyph-pair adjustments. Lists contain explicit overrides; unmodified native queries return converted FreeType kerning. Disposed resources reject access.

<a id="clearsizecache"></a>
`public System.Void ClearSizeCache(System.Int32 cacheIndex)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="cleartextures"></a>
`public System.Void ClearTextures(System.Int32 cacheIndex, Electron2D.Vector2i size)`

Queries or changes owned image-page slots. Images are copied and caller-owned query results must be disposed; glyph metadata remains independent. Disposed resources reject access.

<a id="getcacheascent"></a>
`public System.Single GetCacheAscent(System.Int32 cacheIndex, System.Int32 size)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="getcachecount"></a>
`public System.Int32 GetCacheCount()`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="getcachedescent"></a>
`public System.Single GetCacheDescent(System.Int32 cacheIndex, System.Int32 size)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="getcachescale"></a>
`public System.Single GetCacheScale(System.Int32 cacheIndex, System.Int32 size)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="getcacheunderlineposition"></a>
`public System.Single GetCacheUnderlinePosition(System.Int32 cacheIndex, System.Int32 size)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="getcacheunderlinethickness"></a>
`public System.Single GetCacheUnderlineThickness(System.Int32 cacheIndex, System.Int32 size)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="getcharfromglyphindex"></a>
`public System.Int32 GetCharFromGlyphIndex(System.Int32 size, System.Int32 glyph)`

Returns the first supported Unicode scalar mapped to the source glyph, or zero when no scalar maps to it. Scalable reverse enumeration is a cold query. Disposed resources reject access.

<a id="getembolden"></a>
`public System.Single GetEmbolden(System.Int32 cacheIndex)`

Reads a copied typed instance setting or validates and publishes an independently realized native instance. Failed face/coordinate realization preserves previous records and rendering. Disposed resources reject access.

<a id="getextrabaselineoffset"></a>
`public System.Single GetExtraBaselineOffset(System.Int32 cacheIndex)`

Reads a copied typed instance setting or validates and publishes an independently realized native instance. Failed face/coordinate realization preserves previous records and rendering. Disposed resources reject access.

<a id="getextraspacing"></a>
`public System.Int32 GetExtraSpacing(System.Int32 cacheIndex, Electron2D.TextSpacingType spacing)`

Uses a valid TextSpacingType and signed pixels. Primary spacing feeds common layout; matching indexed variation spacing is retained without another dispatcher. Disposed resources reject access.

<a id="getfaceindex"></a>
`public System.Int32 GetFaceIndex(System.Int32 cacheIndex)`

Reads a copied typed instance setting or validates and publishes an independently realized native instance. Failed face/coordinate realization preserves previous records and rendering. Disposed resources reject access.

<a id="getglyphadvance"></a>
`public Electron2D.Vector2 GetGlyphAdvance(System.Int32 cacheIndex, System.Int32 size, System.Int32 glyph)`

Reads source glyph mapping or prepared glyph geometry; collections are copied. Cache/size/outline and glyph indices retain their distinct roles. Disposed resources reject access.

<a id="getglyphindex"></a>
`public System.Int32 GetGlyphIndex(System.Int32 size, System.Int32 character, System.Int32 variationSelector)`

Reads source glyph mapping or prepared glyph geometry; collections are copied. Cache/size/outline and glyph indices retain their distinct roles. Disposed resources reject access.

<a id="getglyphlist"></a>
`public System.Int32[] GetGlyphList(System.Int32 cacheIndex, Electron2D.Vector2i size)`

Reads source glyph mapping or prepared glyph geometry; collections are copied. Cache/size/outline and glyph indices retain their distinct roles. Disposed resources reject access.

<a id="getglyphoffset"></a>
`public Electron2D.Vector2 GetGlyphOffset(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph)`

Reads source glyph mapping or prepared glyph geometry; collections are copied. Cache/size/outline and glyph indices retain their distinct roles. Disposed resources reject access.

<a id="getglyphsize"></a>
`public Electron2D.Vector2 GetGlyphSize(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph)`

Reads source glyph mapping or prepared glyph geometry; collections are copied. Cache/size/outline and glyph indices retain their distinct roles. Disposed resources reject access.

<a id="getglyphtextureindex"></a>
`public System.Int32 GetGlyphTextureIndex(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph)`

Reads source glyph mapping or prepared glyph geometry; collections are copied. Cache/size/outline and glyph indices retain their distinct roles. Disposed resources reject access.

<a id="getglyphuvrect"></a>
`public Electron2D.Rect2 GetGlyphUVRect(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph)`

Reads source glyph mapping or prepared glyph geometry; collections are copied. Cache/size/outline and glyph indices retain their distinct roles. Disposed resources reject access.

<a id="getkerning"></a>
`public Electron2D.Vector2 GetKerning(System.Int32 cacheIndex, System.Int32 size, Electron2D.Vector2i glyphPair)`

Queries, stores or removes glyph-pair adjustments. Lists contain explicit overrides; unmodified native queries return converted FreeType kerning. Disposed resources reject access.

<a id="getkerninglist"></a>
`public Electron2D.Vector2i[] GetKerningList(System.Int32 cacheIndex, System.Int32 size)`

Queries, stores or removes glyph-pair adjustments. Lists contain explicit overrides; unmodified native queries return converted FreeType kerning. Disposed resources reject access.

<a id="getsizecachelist"></a>
`public Electron2D.Vector2i[] GetSizeCacheList(System.Int32 cacheIndex)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="getspacing"></a>
`public override System.Int32 GetSpacing(Electron2D.TextSpacingType spacing)`

Uses a valid TextSpacingType and signed pixels. Primary spacing feeds common layout; matching indexed variation spacing is retained without another dispatcher. Disposed resources reject access.

<a id="gettexturecount"></a>
`public System.Int32 GetTextureCount(System.Int32 cacheIndex, Electron2D.Vector2i size)`

Queries or changes owned image-page slots. Images are copied and caller-owned query results must be disposed; glyph metadata remains independent. Disposed resources reject access.

<a id="gettextureimage"></a>
`public Electron2D.Image GetTextureImage(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 textureIndex)`

Queries or changes owned image-page slots. Images are copied and caller-owned query results must be disposed; glyph metadata remains independent. Disposed resources reject access.

<a id="gettextureoffsets"></a>
`public System.Int32[] GetTextureOffsets(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 textureIndex)`

Queries or replaces copied shelf-packing tuples. Invalid or unbounded shelves fail before publication; later preparation uses the supplied free regions. Disposed resources reject access.

<a id="gettransform"></a>
`public Electron2D.Transform GetTransform(System.Int32 cacheIndex)`

Reads a copied typed instance setting or validates and publishes an independently realized native instance. Failed face/coordinate realization preserves previous records and rendering. Disposed resources reject access.

<a id="getvariationcoordinates"></a>
`public System.Collections.Generic.Dictionary<System.UInt32, System.Single> GetVariationCoordinates(System.Int32 cacheIndex)`

Reads a copied typed instance setting or validates and publishes an independently realized native instance. Failed face/coordinate realization preserves previous records and rendering. Disposed resources reject access.

<a id="loadbitmapfont"></a>
`public System.Void LoadBitmapFont(System.String path)`

Loads text or binary v3 BMFont, copied page pixels, metrics, Unicode/OEM mapping, packed channels, outline records and kerning atomically. Filesystem/decode failures preserve the old font. Fallbacks and feature defaults survive; encoded scalable bytes are cleared. Disposed resources reject access.

<a id="removecache"></a>
`public System.Void RemoveCache(System.Int32 cacheIndex)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="removeglyph"></a>
`public System.Void RemoveGlyph(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph)`

Reads a copied typed instance setting or validates and publishes an independently realized native instance. Failed face/coordinate realization preserves previous records and rendering. Disposed resources reject access.

<a id="removekerning"></a>
`public System.Void RemoveKerning(System.Int32 cacheIndex, System.Int32 size, Electron2D.Vector2i glyphPair)`

Queries, stores or removes glyph-pair adjustments. Lists contain explicit overrides; unmodified native queries return converted FreeType kerning. Disposed resources reject access.

<a id="removesizecache"></a>
`public System.Void RemoveSizeCache(System.Int32 cacheIndex, Electron2D.Vector2i size)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="removetexture"></a>
`public System.Void RemoveTexture(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 textureIndex)`

Queries or changes owned image-page slots. Images are copied and caller-owned query results must be disposed; glyph metadata remains independent. Disposed resources reject access.

<a id="renderglyph"></a>
`public System.Void RenderGlyph(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph)`

Prepares native pixels and updates real glyph/page/shelf observations; already authored glyphs retain their replacement. Input bounds are validated before a bounded range executes. Disposed resources reject access.

<a id="renderrange"></a>
`public System.Void RenderRange(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 start, System.Int32 end)`

Prepares native pixels and updates real glyph/page/shelf observations; already authored glyphs retain their replacement. Input bounds are validated before a bounded range executes. Disposed resources reject access.

<a id="setcacheascent"></a>
`public System.Void SetCacheAscent(System.Int32 cacheIndex, System.Int32 size, System.Single value)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="setcachedescent"></a>
`public System.Void SetCacheDescent(System.Int32 cacheIndex, System.Int32 size, System.Single value)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="setcachescale"></a>
`public System.Void SetCacheScale(System.Int32 cacheIndex, System.Int32 size, System.Single value)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="setcacheunderlineposition"></a>
`public System.Void SetCacheUnderlinePosition(System.Int32 cacheIndex, System.Int32 size, System.Single value)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="setcacheunderlinethickness"></a>
`public System.Void SetCacheUnderlineThickness(System.Int32 cacheIndex, System.Int32 size, System.Single value)`

Queries, clears or changes one indexed entry or size record. Metrics use logical pixels, scales must be positive, and collections are copied; native source data remains separate. Disposed resources reject access.

<a id="setembolden"></a>
`public System.Void SetEmbolden(System.Int32 cacheIndex, System.Single value)`

Reads a copied typed instance setting or validates and publishes an independently realized native instance. Failed face/coordinate realization preserves previous records and rendering. Disposed resources reject access.

<a id="setextrabaselineoffset"></a>
`public System.Void SetExtraBaselineOffset(System.Int32 cacheIndex, System.Single value)`

Reads a copied typed instance setting or validates and publishes an independently realized native instance. Failed face/coordinate realization preserves previous records and rendering. Disposed resources reject access.

<a id="setextraspacing"></a>
`public System.Void SetExtraSpacing(System.Int32 cacheIndex, Electron2D.TextSpacingType spacing, System.Int32 value)`

Uses a valid TextSpacingType and signed pixels. Primary spacing feeds common layout; matching indexed variation spacing is retained without another dispatcher. Disposed resources reject access.

<a id="setfaceindex"></a>
`public System.Void SetFaceIndex(System.Int32 cacheIndex, System.Int32 value)`

Reads a copied typed instance setting or validates and publishes an independently realized native instance. Failed face/coordinate realization preserves previous records and rendering. Disposed resources reject access.

<a id="setglyphadvance"></a>
`public System.Void SetGlyphAdvance(System.Int32 cacheIndex, System.Int32 size, System.Int32 glyph, Electron2D.Vector2 advance)`

Publishes a finite glyph geometry, advance or page association. The change immediately invalidates consumers and does not remove unrelated glyphs/pages. Disposed resources reject access.

<a id="setglyphoffset"></a>
`public System.Void SetGlyphOffset(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph, Electron2D.Vector2 offset)`

Publishes a finite glyph geometry, advance or page association. The change immediately invalidates consumers and does not remove unrelated glyphs/pages. Disposed resources reject access.

<a id="setglyphsize"></a>
`public System.Void SetGlyphSize(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph, Electron2D.Vector2 glyphSize)`

Publishes a finite glyph geometry, advance or page association. The change immediately invalidates consumers and does not remove unrelated glyphs/pages. Disposed resources reject access.

<a id="setglyphtextureindex"></a>
`public System.Void SetGlyphTextureIndex(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph, System.Int32 textureIndex)`

Publishes a finite glyph geometry, advance or page association. The change immediately invalidates consumers and does not remove unrelated glyphs/pages. Disposed resources reject access.

<a id="setglyphuvrect"></a>
`public System.Void SetGlyphUVRect(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 glyph, Electron2D.Rect2 uvRect)`

Publishes a finite glyph geometry, advance or page association. The change immediately invalidates consumers and does not remove unrelated glyphs/pages. Disposed resources reject access.

<a id="setkerning"></a>
`public System.Void SetKerning(System.Int32 cacheIndex, System.Int32 size, Electron2D.Vector2i glyphPair, Electron2D.Vector2 value)`

Queries, stores or removes glyph-pair adjustments. Lists contain explicit overrides; unmodified native queries return converted FreeType kerning. Disposed resources reject access.

<a id="settextureimage"></a>
`public System.Void SetTextureImage(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 textureIndex, Electron2D.Image image)`

Queries or changes owned image-page slots. Images are copied and caller-owned query results must be disposed; glyph metadata remains independent. Disposed resources reject access.

<a id="settextureoffsets"></a>
`public System.Void SetTextureOffsets(System.Int32 cacheIndex, Electron2D.Vector2i size, System.Int32 textureIndex, System.Int32[] offsets)`

Queries or replaces copied shelf-packing tuples. Invalid or unbounded shelves fail before publication; later preparation uses the supplied free regions. Disposed resources reject access.

<a id="settransform"></a>
`public System.Void SetTransform(System.Int32 cacheIndex, Electron2D.Transform value)`

Reads a copied typed instance setting or validates and publishes an independently realized native instance. Failed face/coordinate realization preserves previous records and rendering. Disposed resources reject access.

<a id="setvariationcoordinates"></a>
`public System.Void SetVariationCoordinates(System.Int32 cacheIndex, System.Collections.Generic.Dictionary<System.UInt32, System.Single> value)`

Reads a copied typed instance setting or validates and publishes an independently realized native instance. Failed face/coordinate realization preserves previous records and rendering. Disposed resources reject access.

<a id="fixedsize"></a>
`public System.Int32 FixedSize { get; set; }`

Uses the fixed bitmap-size and numeric scaling policy above. Equal property writes are silent; invalid sizes/modes fail before publication. Disposed resources reject access.

<a id="fixedsizescalemode"></a>
`public Electron2D.FixedSizeScaleMode FixedSizeScaleMode { get; set; }`

Uses the fixed bitmap-size and numeric scaling policy above. Equal property writes are silent; invalid sizes/modes fail before publication. Disposed resources reject access.

Type 1 PFB/PFM dynamic sources remain unsupported by the SFNT-backed native source profile; their nominal/kerning callbacks and format fixtures are tracked as a separate source capability in coverage. BMFont loading uses the executable bitmap path above.

## System font integration

[System font matching](../components/system-fonts.md) adds installed families/styles/logical collection faces and owned automatic text fallback over the shared native owner and canvas path. FontFile.AllowSystemFallback defaults to true; explicit resources retain precedence and explicit support queries remain distinct from automatic rendered coverage. Active parent readers retain retired fallback faces through policy changes. SystemFont archives store preferences and rematch the host. The current Linux catalog and both canvas consumers are exercised; CoreText/DirectWrite, extra raster/MSDF, native allocator and foreign acceptance gates remain explicit.

<a id="allowsystemfallback"></a>
`public bool AllowSystemFallback { get; set; }`

True initially and after file/bitmap reset. Missing shaped/drawn glyphs can load bounded owned platform sources after explicit fallbacks. Equal writes are silent; changing the policy invalidates layout and retains old fallback faces until active readers finish. Discovery is cold; prepared lookups reuse native faces. Missing or unsupported catalogs retain hexadecimal missing output. Source paths are not persisted with the permission.
