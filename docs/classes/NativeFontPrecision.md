# NativeFontPrecision

Last updated: 2026-09-27

- Declaration: `internal sealed unsafe partial class NativeFontPrecision : IDisposable`
- Sources: [NativeFontPrecision.cs](../../src/Servers/Text/NativeFontPrecision.cs), [NativeFontPrecision.Raster.cs](../../src/Servers/Text/NativeFontPrecision.Raster.cs), [OpenTypeFeatureTags.cs](../../src/Servers/Text/OpenTypeFeatureTags.cs)
- Component: [canvas rendering](../components/canvas-rendering.md)
- Visibility: internal; native handles and ABI records are unavailable to engine consumers.

## Description

Owns a copied native font source, a public FreeType face, and HarfBuzz face/font/buffer handles on one text-worker thread. FontData serializes access from public Font resources. SFNT tables are obtained through FT_Load_Sfnt_Table, allowing the same path to shape compressed WOFF2 and ordinary TTF/OTF/TTC data without reading private library structures. HarfBuzz OT functions supply shaping, while explicit FreeType callbacks preserve unhinted advances, glyph extents and vertical origins.

## Internal member summary

| Member | Contract |
| --- | --- |
| `NativeFontPrecision(byte[] immutableData, int faceIndex = 0)` | Copies bytes into owned native storage, validates the face/tables and initializes size 16. |
| `FamilyName`, `StyleName`, `FaceCount`, `GlyphCount`, `UnitsPerEm`, `FaceFlags`, `FaceStyleFlags` | Copied face metadata. |
| `Ascent`, `Descent`, `Height`, `LineHeight`, `UnderlinePosition`, `UnderlineThickness` | Metrics for the current size; Height is ascent plus descent. |
| `SetSize(float pixels)` | Finite positive size converted to 26.6; updates FreeType and HarfBuzz scales together. |
| `Shape(ReadOnlySpan<uint>, NativeTextDirection, uint scriptTag = 0, string language = "", ReadOnlySpan<NativeFontFeature> = default)` | Shapes the whole scalar span and returns a reusable glyph view. |
| `Shape(ReadOnlySpan<uint>, int start, int count, NativeTextDirection, ...)` | Shapes a run while preserving the full paragraph's pre/post context and absolute cluster indices. |
| `GetGlyphIndex(uint scalar, uint variationSelector = 0)` | Nominal or selector-specific glyph lookup; missing glyph returns zero. |
| `GetGlyphAdvance(uint glyph, bool vertical = false)` | Signed 26.6 unhinted advance. |
| `GetGlyphBounds(uint glyph)` | Unhinted outline bounds relative to the baseline, with Y downward. |
| `GetRasterBounds(uint glyph, bool noHinting = false)` | LIGHT raster metrics by default; optional unhinted metrics. |
| `GetRasterBounds(uint glyph, FontHinting hinting)` | None/Light/Normal raster metric selection. |
| `GetSFNTTable(uint tag)` | Independent byte array; missing optional table returns empty. |
| `GetSupportedChars()` | Ordered valid Unicode scalars enumerated through FreeType's character map. |
| `Rasterize(uint glyph, FontHinting hinting, int phase26Dot6 = 0, int outlineRadius26Dot6 = 0)` | Owned RGBA raster, dimensions and exact baseline bearings. |
| `Dispose()` | Destroys buffer/fonts/face before FreeType and source bytes; repeated disposal is harmless. |

## Shaping and raster ownership

<a id="nativetextdirection"></a><a id="nativefontfeature"></a><a id="nativeshapedglyph"></a>
NativeTextDirection uses LTR=4, RTL=5, TTB=6 and BTT=7. NativeFontFeature contains Tag, Value, Start and End; its default range is the complete scalar domain. NativeShapedGlyph contains GlyphIndex, Cluster, signed 26.6 XAdvance/YAdvance/XOffset/YOffset and public HarfBuzz Flags. Unsafe-break/concat and safe-tatweel bits are preserved; the latter lets layout select valid Arabic elongation boundaries.

Shape validates Unicode scalars and run bounds, retains paragraph context, and sets beginning/end flags only at actual supplied paragraph boundaries. Its returned ReadOnlySpan is invalidated by the next Shape call. FontData consumes/copies it synchronously on the worker. The native buffer and managed output capacity are reused after warmup. Managed exceptions are contained inside native callbacks and reported after native shaping returns.

<a id="nativerasterglyph"></a>
NativeRasterGlyph contains Pixels, Width, Height, Left, Top and Colored. Pixels are caller-owned straight RGBA: monochrome coverage uses white RGB, while native premultiplied BGRA color is converted. Left/Top are integer bitmap bearings, with positive Top above the baseline. The font drawing path positions the result using `(Left, -Top)` and the active raster scale.

Rasterize applies a horizontal 0..63 phase in 26.6 to the outline before rasterization. Normal, LIGHT and unhinted raster policies are separate from shaping metrics. Positive outline radii use a contour stroke with butt caps and round joins. Detached glyph/stroker objects and the face transform are restored on success and failure. Whitespace may return a valid empty bitmap. Bitmap dimensions are bounded to 16384 per axis and pitch/pixel format are validated before copying. Glyph rasterization allocates output bytes on cache misses; FontData owns phase-aware texture caching.

Sequential ABI records model only documented public FreeType record fields. CLong/CULong preserve Windows C-long width and Unix native-long width. Public FreeType/HarfBuzz C entry points are called with Cdecl; no SDL_ttf private state is read. FontFile enforces the public 64 MiB source limit before constructing this backend.

macOS resolves private FreeType 2.13.3 with statically linked Brotli/PNG/zlib and HarfBuzz auto-hinting support. Its producer checks the engine's bundled WOFF2 directly before packaging. The first upstream macOS binary failed WOFF2 decoding; [native delivery](../native-packaging.md) distinguishes that failure, the replacement producer and pending full runtime execution.

## Feature tag conversion

<a id="opentypefeaturetags"></a>
OpenTypeFeatureTags maps 127 fixed readable aliases plus 99 character-variant and 20 stylistic-set aliases. Matching is ordinal and case-sensitive. Unknown keys remove all custom_ segments, replace non-ASCII scalars with spaces, stop at NUL, truncate to four bytes and space-pad shorter tags; the empty key maps to zero. FontFile retains original keys and signed values, compiling only nonnegative values into NativeFontFeature records.

## Verification and limits

[NativeFontPrecisionTests](../../tests/Electron2D.Tests/NativeFontPrecisionTests.cs) verifies independent FreeType C metrics, WOFF2 loading, metadata, table ownership, real cmap enumeration, ligature features, combining clusters, Arabic shaping/run context/tatweel flags, vertical origins, corrupt data, owner-thread guards and lifetime. Twelve independent C raster profiles verify complete pixel hashes and bearings across four quarter-pixel phases, all three hinting modes and multiple stroke radii; tests also verify transform restoration and empty/invalid glyph paths.

Sixty-four measured shaping/size/metric/raster-bound-query cycles after sixty-four warmup cycles allocate zero managed bytes. This excludes cache-miss Rasterize output allocations. ABI and native execution are verified on Linux x64; other platforms, all color-font formats, native allocator counts and whole-application performance remain separate gates. [FontFileTests](../../tests/Electron2D.Tests/FontFileTests.cs) verifies the public resource integration. See [ADR 0046](../decisions/rendering.md#adr-0046).
