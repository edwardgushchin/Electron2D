# Font

Last updated: 2026-10-07

**Inherits:** [Resource](Resource.md) · **Source:** [Font.cs](../../src/Scene/Resources/Font.cs) · **Component:** [Text](../components/text.md)

## Description

Abstract shared font contract implemented by [FontFile](FontFile.md). Fonts shape Unicode text, measure scalar clusters and draw at a baseline through a currently recording CanvasItem. Text is UTF-16 at the public boundary and Unicode scalar indices internally. The fallback array is copied and borrows its members; null slots and aliases are retained, cycles and excessive depth are rejected. A resource can be measured from another managed thread, but canvas operations still require its owner thread and draw scope.

Single-line and multiline layout caches default to 64 and 16 entries. Changed font data or fallback revisions invalidate dependent layouts, including a missed public change callback. Active reads lease immutable native source snapshots and retry changed dependencies up to 64 times. Continually changing callbacks fail with InvalidOperationException; nested queries use independent reusable layout storage. Disposal invalidates the public font and releases native faces after their last reader, while already recorded glyph pixels remain usable through retained canvas commands. Fallback resources remain caller-owned. The base OpenType-feature query is empty by contract, distinct from FontFile shaping overrides. Coordinate and size queries preserve fractional advances until the specified final ceiling/rounding stages.

## Example

```csharp
using var font = ResourceLoader.Load<FontFile>("res://fonts/interface.woff2");
Vector2 size = font.GetStringSize("Hello العربية");
// Inside CanvasItem.OnDraw, while this font remains alive:
// DrawString(font, new Vector2(8, 24), "Hello", fontSize: 16);
```

## API summary

| Signature | Contract |
| --- | --- |
| `protected Font()` | [Font](#font): Initializes empty fallback lists and caches for sixty-four lines and sixteen paragraphs. |
| `public Font?[] Fallbacks { get; set; }` | [Fallbacks](#fallbacks): Gets or sets the ordered borrowed fallback fonts, retaining null slots and repeated identities. |
| `public float GetAscent(int fontSize = 16)` | [GetAscent](#getascent): Returns the maximum ascent across this font and its fallbacks. |
| `public float GetDescent(int fontSize = 16)` | [GetDescent](#getdescent): Returns the maximum descent across this font and its fallbacks. |
| `public float GetHeight(int fontSize = 16)` | [GetHeight](#getheight): Returns the maximum face ascent-plus-descent, including top and bottom spacing. |
| `public float GetUnderlinePosition(int fontSize = 16)` | [GetUnderlinePosition](#getunderlineposition): Returns the maximum underline position below the baseline, including top spacing. |
| `public float GetUnderlineThickness(int fontSize = 16)` | [GetUnderlineThickness](#getunderlinethickness): Returns the maximum underline thickness across this font and its fallbacks. |
| `public string GetFontName()` | [GetFontName](#getfontname): Returns the primary face's family name, or an empty name without data. |
| `public string GetFontStyleName()` | [GetFontStyleName](#getfontstylename): Returns the primary face's style name. |
| `public FontStyle GetFontStyle()` | [GetFontStyle](#getfontstyle): Returns the primary face's intrinsic style flags. |
| `public int GetFontWeight()` | [GetFontWeight](#getfontweight): Returns the primary face's weight class. |
| `public int GetFontStretch()` | [GetFontStretch](#getfontstretch): Returns the primary face's width as a percentage of normal width. |
| `public int GetFaceCount()` | [GetFaceCount](#getfacecount): Returns the number of faces in the primary font collection. |
| `public virtual int GetSpacing(TextSpacingType spacing)` | [GetSpacing](#getspacing): Returns additional spacing supplied by this font. |
| `public virtual Dictionary<string, int> GetOpenTypeFeatures()` | [GetOpenTypeFeatures](#getopentypefeatures): Returns this font's span-level OpenType feature overrides. |
| `public bool HasChar(int character)` | [HasChar](#haschar): Tests whether this font or a fallback contains a Unicode scalar. |
| `public string GetSupportedChars()` | [GetSupportedChars](#getsupportedchars): Returns all supported Unicode characters, preserving face order and removing duplicates. |
| `public Vector2 GetCharSize(int character, int fontSize)` | [GetCharSize](#getcharsize): Measures one character without kerning or contextual shaping. |
| `public void SetCacheCapacity(int singleLine, int multiLine)` | [SetCacheCapacity](#setcachecapacity): Sets the bounded least-recently-used layout cache capacities. |
| `public Vector2 GetStringSize(string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida \| TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)` | [GetStringSize](#getstringsize): Measures a shaped single line, including kerning, bidi order and optional width trimming. |
| `public Vector2 GetMultilineStringSize(string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int maxLines = -1, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory \| TextLineBreakFlags.WordBound, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida \| TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)` | [GetMultilineStringSize](#getmultilinestringsize): Measures shaped text wrapped into lines according to Unicode boundaries. |
| `public float DrawChar(CanvasItem canvasItem, Vector2 position, int character, int fontSize, Color? modulate = null, float oversampling = 0)` | [DrawChar](#drawchar): Draws one unshaped Unicode character at a baseline position. |
| `public float DrawCharOutline(CanvasItem canvasItem, Vector2 position, int character, int fontSize, int size = -1, Color? modulate = null, float oversampling = 0)` | [DrawCharOutline](#drawcharoutline): Draws the outline of one unshaped character at a baseline position. |
| `public void DrawString(CanvasItem canvasItem, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, Color? modulate = null, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida \| TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)` | [DrawString](#drawstring): Draws a shaped single line at a baseline position. |
| `public void DrawStringOutline(CanvasItem canvasItem, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int size = 1, Color? modulate = null, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida \| TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)` | [DrawStringOutline](#drawstringoutline): Draws the outline of a shaped single line at a baseline position. |
| `public void DrawMultilineString(CanvasItem canvasItem, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int maxLines = -1, Color? modulate = null, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory \| TextLineBreakFlags.WordBound, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida \| TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)` | [DrawMultilineString](#drawmultilinestring): Draws a Unicode-wrapped paragraph with the first line at the supplied baseline. |
| `public void DrawMultilineStringOutline(CanvasItem canvasItem, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int maxLines = -1, int size = 1, Color? modulate = null, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory \| TextLineBreakFlags.WordBound, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida \| TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)` | [DrawMultilineStringOutline](#drawmultilinestringoutline): Draws the outline of a Unicode-wrapped paragraph with its first line at the supplied baseline. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | [GetPropertyDescriptors](#getpropertydescriptors): Inherited resource graph hook. |
| `protected override void OnResetState()` | [OnResetState](#onresetstate): Inherited resource graph hook. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | [CopyCustomStateTo](#copycustomstateto): Inherited resource graph hook. |
| `protected override void Dispose(bool disposing)` | [Dispose](#dispose): Inherited resource graph hook. |

## Font instances and metadata

<a id="findvariation"></a>
`public FontVariation FindVariation(Dictionary<uint, float>? variationCoordinates = null, int faceIndex = 0, float strength = 0, Transform? transform = null, int spacingTop = 0, int spacingBottom = 0, int spacingSpace = 0, int spacingGlyph = 0, float baselineOffset = 0, int paletteIndex = 0, Color[]? customColors = null)` creates a caller-owned [FontVariation](FontVariation.md) borrowing this Font. Configuration uses that resource's validation, independent cache identity and lazy realization. Dispose the returned instance. Invalid configuration fails while constructing the resource; an unavailable face fails when it is realized. Native handles remain internal.

| Signature | Contract |
| --- | --- |
| `public Dictionary<uint, FontVariationAxis> GetSupportedVariationList()` | Copies primary-face axis tags and validated minimum/maximum/default design bounds. Empty without axes. |
| `public int GetPaletteCount()` | Predefined primary-face palette count; zero without CPAL data. |
| `public Color[] GetPaletteColors(int index)` | Copies predefined colors independently of instance overrides; invalid index fails. |
| `public string GetPaletteName(int index)` | Optional SFNT palette name; empty when unnamed; invalid index fails. |

All metadata queries reject disposed resources and realize the selected primary face lazily. Snapshots are independent cold allocations. Font.GetOpenTypeFeatures returns empty span settings for FontFile; FontVariation returns its copied span overrides. File-level feature defaults remain separate.

## Member descriptions

<a id="font"></a>
### Font

`protected Font()`

Initializes empty fallback lists and caches for sixty-four lines and sixteen paragraphs.


<a id="fallbacks"></a>
### Fallbacks

`public Font?[] Fallbacks { get; set; }`

Gets or sets the ordered borrowed fallback fonts, retaining null slots and repeated identities.

An independent array snapshot; initially empty. Every successful assignment emits Changed.

Errors: `ArgumentNullException` — The array is null.; `ArgumentException` — The fallback graph would contain a cycle or exceed sixty-four levels.; `ObjectDisposedException` — This font or a supplied nonnull fallback is disposed.

<a id="getascent"></a>
### GetAscent

`public float GetAscent(int fontSize = 16)`

Returns the maximum ascent across this font and its fallbacks.

Pixels above the baseline, including top spacing.

`fontSize`: Positive font size in logical pixels.

Errors: `ArgumentOutOfRangeException` — The font size is not positive.; `ObjectDisposedException` — The font or a required fallback is disposed.

<a id="getdescent"></a>
### GetDescent

`public float GetDescent(int fontSize = 16)`

Returns the maximum descent across this font and its fallbacks.

Pixels below the baseline, including bottom spacing.

`fontSize`: Positive font size in logical pixels.

Errors: `ArgumentOutOfRangeException` — The font size is not positive.; `ObjectDisposedException` — The font or a required fallback is disposed.

<a id="getheight"></a>
### GetHeight

`public float GetHeight(int fontSize = 16)`

Returns the maximum face ascent-plus-descent, including top and bottom spacing.

The line height in logical pixels.

`fontSize`: Positive font size in logical pixels.

Errors: `ArgumentOutOfRangeException` — The font size is not positive.; `ObjectDisposedException` — The font or a required fallback is disposed.

<a id="getunderlineposition"></a>
### GetUnderlinePosition

`public float GetUnderlinePosition(int fontSize = 16)`

Returns the maximum underline position below the baseline, including top spacing.

Underline offset in pixels.

`fontSize`: Positive font size in logical pixels.

Errors: `ArgumentOutOfRangeException` — The font size is not positive.; `ObjectDisposedException` — The font or a required fallback is disposed.

<a id="getunderlinethickness"></a>
### GetUnderlineThickness

`public float GetUnderlineThickness(int fontSize = 16)`

Returns the maximum underline thickness across this font and its fallbacks.

Underline thickness in pixels.

`fontSize`: Positive font size in logical pixels.

Errors: `ArgumentOutOfRangeException` — The font size is not positive.; `ObjectDisposedException` — The font or a required fallback is disposed.

<a id="getfontname"></a>
### GetFontName

`public string GetFontName()`

Returns the primary face's family name, or an empty name without data.

The family name.

Errors: `ObjectDisposedException` — The font is disposed.

<a id="getfontstylename"></a>
### GetFontStyleName

`public string GetFontStyleName()`

Returns the primary face's style name.

The style name, or an empty name without data.

Errors: `ObjectDisposedException` — The font is disposed.

<a id="getfontstyle"></a>
### GetFontStyle

`public FontStyle GetFontStyle()`

Returns the primary face's intrinsic style flags.

The style flags.

Errors: `ObjectDisposedException` — The font is disposed.

<a id="getfontweight"></a>
### GetFontWeight

`public int GetFontWeight()`

Returns the primary face's weight class.

The weight, with four hundred representing normal weight.

Errors: `ObjectDisposedException` — The font is disposed.

<a id="getfontstretch"></a>
### GetFontStretch

`public int GetFontStretch()`

Returns the primary face's width as a percentage of normal width.

The stretch percentage.

Errors: `ObjectDisposedException` — The font is disposed.

<a id="getfacecount"></a>
### GetFaceCount

`public int GetFaceCount()`

Returns the number of faces in the primary font collection.

The collection face count, or zero without data.

Errors: `ObjectDisposedException` — The font is disposed.

<a id="getspacing"></a>
### GetSpacing

`public virtual int GetSpacing(TextSpacingType spacing)`

Returns additional spacing supplied by this font.

Zero for an unmodified font.

`spacing`: The spacing category.

Errors: `ArgumentOutOfRangeException` — The spacing category is invalid.; `ObjectDisposedException` — The font is disposed.

<a id="getopentypefeatures"></a>
### GetOpenTypeFeatures

`public virtual Dictionary<string, int> GetOpenTypeFeatures()`

Returns this font's span-level OpenType feature overrides.

An independent empty dictionary for a scalable font; file-level feature defaults are stored separately.

Errors: `ObjectDisposedException` — The font is disposed.

<a id="haschar"></a>
### HasChar

`public bool HasChar(int character)`

Tests whether this font or a fallback contains a Unicode scalar.

Whether a glyph is available.

`character`: A Unicode scalar value.

Errors: `ArgumentOutOfRangeException` — The character is not a Unicode scalar.; `ObjectDisposedException` — The font or a required fallback is disposed.

<a id="getsupportedchars"></a>
### GetSupportedChars

`public string GetSupportedChars()`

Returns all supported Unicode characters, preserving face order and removing duplicates.

A string containing the distinct supported scalars.

Errors: `ObjectDisposedException` — The font or a required fallback is disposed.

<a id="getcharsize"></a>
### GetCharSize

`public Vector2 GetCharSize(int character, int fontSize)`

Measures one character without kerning or contextual shaping.

Its advance and the font height, or zero when no face contains it.

`character`: A Unicode scalar value.; `fontSize`: Positive font size.

Errors: `ArgumentOutOfRangeException` — The character or size is invalid.; `ObjectDisposedException` — The font or a required fallback is disposed.

<a id="setcachecapacity"></a>
### SetCacheCapacity

`public void SetCacheCapacity(int singleLine, int multiLine)`

Sets the bounded least-recently-used layout cache capacities.

`singleLine`: Number of single-line layouts retained; zero disables retention.; `multiLine`: Number of paragraph layouts retained; zero disables retention.

Errors: `ArgumentOutOfRangeException` — A capacity is negative.; `ObjectDisposedException` — The font is disposed.

<a id="getstringsize"></a>
### GetStringSize

`public Vector2 GetStringSize(string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)`

Measures a shaped single line, including kerning, bidi order and optional width trimming.

The shaped line size in logical pixels.

`text`: The text to shape.; `alignment`: Alignment along the advance axis.; `width`: Finite available width, or a negative value for unconstrained text.; `fontSize`: Positive logical font size.; `justificationFlags`: Fill-spacing rules.; `direction`: Paragraph direction.; `orientation`: Glyph advance axis.

Errors: `ArgumentNullException` — The text is null.; `ArgumentOutOfRangeException` — Size, width or an enum value is invalid.; `ObjectDisposedException` — The font or a required fallback is disposed.

<a id="getmultilinestringsize"></a>
### GetMultilineStringSize

`public Vector2 GetMultilineStringSize(string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int maxLines = -1, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)`

Measures shaped text wrapped into lines according to Unicode boundaries.

The visible paragraph size.

`text`: The text to shape.; `alignment`: Alignment along the advance axis.; `width`: Finite wrap width, or a negative value for unconstrained text.; `fontSize`: Positive logical font size.; `maxLines`: Maximum visible lines, or a negative value for all lines.; `breakFlags`: Line-break rules.; `justificationFlags`: Fill-spacing rules.; `direction`: Paragraph direction.; `orientation`: Glyph advance axis.

Errors: `ArgumentNullException` — The text is null.; `ArgumentOutOfRangeException` — Size, width or an enum value is invalid.; `ObjectDisposedException` — The font or a required fallback is disposed.

<a id="drawchar"></a>
### DrawChar

`public float DrawChar(CanvasItem canvasItem, Vector2 position, int character, int fontSize, Color? modulate = null, float oversampling = 0)`

Draws one unshaped Unicode character at a baseline position.

The character advance, or zero when missing.

`canvasItem`: The currently recording canvas item.; `position`: The finite baseline position.; `character`: A Unicode scalar.; `fontSize`: Positive logical font size.; `modulate`: Finite modulation, or null for white.; `oversampling`: Positive raster scale, or a nonpositive value for automatic scale.

Errors: `ArgumentNullException` — The canvas is null.; `ArgumentException` — Geometry or color is nonfinite.; `ArgumentOutOfRangeException` — Character, font size or raster scale is invalid.; `InvalidOperationException` — The canvas is not recording on its owner thread.; `ObjectDisposedException` — A required resource or canvas item is disposed.

<a id="drawcharoutline"></a>
### DrawCharOutline

`public float DrawCharOutline(CanvasItem canvasItem, Vector2 position, int character, int fontSize, int size = -1, Color? modulate = null, float oversampling = 0)`

Draws the outline of one unshaped character at a baseline position.

The character advance.

`canvasItem`: The currently recording canvas item.; `position`: The finite baseline position.; `character`: A Unicode scalar.; `fontSize`: Positive logical font size.; `size`: Outline radius; nonpositive values draw the unexpanded glyph.; `modulate`: Finite modulation, or null for white.; `oversampling`: Positive raster scale, or a nonpositive value for automatic scale.

Errors: `ArgumentNullException` — The canvas is null.; `ArgumentException` — Geometry or color is nonfinite.; `ArgumentOutOfRangeException` — Character, font size or raster scale is invalid.; `InvalidOperationException` — The canvas is not recording on its owner thread.; `ObjectDisposedException` — A required resource or canvas item is disposed.

<a id="drawstring"></a>
### DrawString

`public void DrawString(CanvasItem canvasItem, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, Color? modulate = null, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)`

Draws a shaped single line at a baseline position.

`canvasItem`: The currently recording canvas item.; `position`: The finite baseline position.; `text`: Text to shape.; `alignment`: Advance-axis alignment.; `width`: Finite available width, or a negative value for no limit.; `fontSize`: Positive logical font size.; `modulate`: Finite modulation, or null for white.; `justificationFlags`: Fill-spacing rules.; `direction`: Paragraph direction.; `orientation`: Advance axis.; `oversampling`: Positive raster scale, or a nonpositive value for automatic scale.

Errors: `ArgumentNullException` — Text or canvas is null.; `ArgumentException` — Geometry or color is nonfinite.; `ArgumentOutOfRangeException` — Size, width, raster scale or an enum value is invalid.; `InvalidOperationException` — The canvas is not recording on its owner thread.; `ObjectDisposedException` — A required resource or canvas item is disposed.

<a id="drawstringoutline"></a>
### DrawStringOutline

`public void DrawStringOutline(CanvasItem canvasItem, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int size = 1, Color? modulate = null, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)`

Draws the outline of a shaped single line at a baseline position.

`canvasItem`: The currently recording canvas item.; `position`: The finite baseline position.; `text`: Text to shape.; `alignment`: Advance-axis alignment.; `width`: Finite available width, or a negative value for no limit.; `fontSize`: Positive logical font size.; `modulate`: Finite modulation, or null for white.; `justificationFlags`: Fill-spacing rules.; `direction`: Paragraph direction.; `orientation`: Advance axis.; `oversampling`: Positive raster scale, or a nonpositive value for automatic scale.; `size`: Outline radius; nonpositive values draw the unexpanded glyph.

Errors: `ArgumentNullException` — Text or canvas is null.; `ArgumentException` — Geometry or color is nonfinite.; `ArgumentOutOfRangeException` — Size, width, raster scale or an enum value is invalid.; `InvalidOperationException` — The canvas is not recording on its owner thread.; `ObjectDisposedException` — A required resource or canvas item is disposed.

<a id="drawmultilinestring"></a>
### DrawMultilineString

`public void DrawMultilineString(CanvasItem canvasItem, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int maxLines = -1, Color? modulate = null, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)`

Draws a Unicode-wrapped paragraph with the first line at the supplied baseline.

`canvasItem`: The currently recording canvas item.; `position`: The finite baseline position.; `text`: Text to shape.; `alignment`: Advance-axis alignment.; `width`: Finite available width, or a negative value for no limit.; `fontSize`: Positive logical font size.; `modulate`: Finite modulation, or null for white.; `justificationFlags`: Fill-spacing rules.; `direction`: Paragraph direction.; `orientation`: Advance axis.; `oversampling`: Positive raster scale, or a nonpositive value for automatic scale.; `maxLines`: Maximum visible lines, or a negative value for all lines.; `breakFlags`: Line-break rules.

Errors: `ArgumentNullException` — Text or canvas is null.; `ArgumentException` — Geometry or color is nonfinite.; `ArgumentOutOfRangeException` — Size, width, raster scale or an enum value is invalid.; `InvalidOperationException` — The canvas is not recording on its owner thread.; `ObjectDisposedException` — A required resource or canvas item is disposed.

<a id="drawmultilinestringoutline"></a>
### DrawMultilineStringOutline

`public void DrawMultilineStringOutline(CanvasItem canvasItem, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16, int maxLines = -1, int size = 1, Color? modulate = null, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)`

Draws the outline of a Unicode-wrapped paragraph with its first line at the supplied baseline.

`canvasItem`: The currently recording canvas item.; `position`: The finite baseline position.; `text`: Text to shape.; `alignment`: Advance-axis alignment.; `width`: Finite available width, or a negative value for no limit.; `fontSize`: Positive logical font size.; `modulate`: Finite modulation, or null for white.; `justificationFlags`: Fill-spacing rules.; `direction`: Paragraph direction.; `orientation`: Advance axis.; `oversampling`: Positive raster scale, or a nonpositive value for automatic scale.; `maxLines`: Maximum visible lines, or a negative value for all lines.; `breakFlags`: Line-break rules.; `size`: Outline radius; nonpositive values draw the unexpanded glyph.

Errors: `ArgumentNullException` — Text or canvas is null.; `ArgumentException` — Geometry or color is nonfinite.; `ArgumentOutOfRangeException` — Size, width, raster scale or an enum value is invalid.; `InvalidOperationException` — The canvas is not recording on its owner thread.; `ObjectDisposedException` — A required resource or canvas item is disposed.

<a id="getpropertydescriptors"></a>
### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Uses the inherited [Resource graph/lifecycle contract](Resource.md) for the concrete resource state described above.

<a id="onresetstate"></a>
### OnResetState

`protected override void OnResetState()`

Uses the inherited [Resource graph/lifecycle contract](Resource.md) for the concrete resource state described above.

<a id="copycustomstateto"></a>
### CopyCustomStateTo

`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`

Uses the inherited [Resource graph/lifecycle contract](Resource.md) for the concrete resource state described above.

<a id="dispose"></a>
### Dispose

`protected override void Dispose(bool disposing)`

Uses the inherited [Resource graph/lifecycle contract](Resource.md) for the concrete resource state described above.

## Verification and limits

[FontTests](../../tests/Electron2D.Tests/FontTests.cs) exercises real metrics, ligatures, BiDi, fallbacks, wrapping, justification, character bounds, all six draw entrypoints, native-thread dispatch, graph copying, callback failure and warmed reuse. [FontRenderingTests](../../tests/Electron2D.Tests/FontRenderingTests.cs) supplies the native pixel gate. Empty text, missing glyphs, invalid scalars and disposed resources have explicit checks. Cache misses, public snapshots and first glyph rasterization may allocate; prepared measured paths are checked separately. Public TextServer RID services, font discovery, bitmap/cache authoring and unsupported platforms remain exact [coverage](../coverage/classes/Font.md) gaps. Native allocator counts and owner acceptance are not inferred from managed tests.

## System font integration

[System font matching](../components/system-fonts.md) adds installed families/styles/logical collection faces and owned automatic text fallback over the shared native owner and canvas path. FontFile.AllowSystemFallback defaults to true; explicit resources retain precedence and explicit support queries remain distinct from automatic rendered coverage. Active parent readers retain retired fallback faces through policy changes. SystemFont archives store preferences and rematch the host. The current Linux catalog and both canvas consumers are exercised; CoreText/DirectWrite, extra raster/MSDF, native allocator and foreign acceptance gates remain explicit.
