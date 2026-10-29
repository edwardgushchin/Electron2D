# TextServer

Last updated: 2026-10-29

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** [TextServerExtension](TextServerExtension.md)

- **Source:** [TextServer.cs](../../src/Servers/Text/TextServer.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class Electron2D.TextServer`

## Description

A retained implementation object discovered or selected through TextServerManager. Public operations delegate to protected On-prefixed callbacks while retaining engine lifetime and identity admission. Public owned shaped buffers belong to their creating thread; private engine consumer buffers use coordinated admission. CreateShapedText/duplicate/substring IDs are strongly retained until FreeRID; TextLine and TextParagraph IDs remain borrowed and cannot be freed here. Text, ordered Font lists and OpenType selectors are copied; caller fonts, keys and typed metadata remain borrowed. Prepared queries and drawing reuse storage; authoring and returned snapshots are cold. Width/size are ceiled display metrics; ascent/descent remain fractional. Object rectangles and drawing are baseline-relative. Active work rejects reentry and mutation. See the complete [buffer contract](../components/text-server.md) for source limits, paragraph line detachment, range/index units, readiness and remaining family dependencies.

## Example

Partial snippet; `font` is a live Font and `canvas` is a recording CanvasItem.

```csharp
var text = TextServerManager.GetPrimaryInterface();
var buffer = text.CreateShapedText();
try
{
    text.ShapedTextAddString(buffer, "Hello", [font], 24);
    // During the canvas item's Draw callback:
    text.ShapedTextDraw(buffer, canvas, new(12, 40));
}
finally { text.FreeRID(buffer); }
```

## Methods

| Signature | Purpose |
| --- | --- |
| `public Electron2D.Vector2 GetHexCodeBoxSize(System.Int32 size, System.Int32 index)` | Measures the font-free hexadecimal replacement, including trailing advance. |
| `public System.Void DrawHexCodeBox(Electron2D.CanvasItem canvas, System.Int32 size, Electron2D.Vector2 position, System.Int32 index, Electron2D.Color color)` | Draws hexadecimal frame/digits at a baseline in the current canvas recording. |
| `public Electron2D.RID CreateShapedText(Electron2D.TextDirection direction = Auto, Electron2D.TextOrientation orientation = Horizontal)` | Creates an empty owned text buffer. |
| `public System.Void FreeRID(Electron2D.RID rid)` | Releases an owned buffer, preserving borrowed text-object lifetimes. |
| `public System.String GetName()` | Returns the stable name of this text implementation. |
| `public System.Boolean Has(Electron2D.RID rid)` | Reports whether an identity resolves to a live shaped buffer on this interface. |
| `public System.Int32 ShapedGetSpanCount(Electron2D.RID shaped)` | Returns the number of source text and inline-object spans intersecting this buffer. |
| `public Electron2D.TextObjectKey ShapedGetSpanEmbeddedObject(Electron2D.RID shaped, System.Int32 index)` | Returns a span's embedded inline-object key. |
| `public Electron2D.TextObjectKey ShapedGetSpanObject(Electron2D.RID shaped, System.Int32 index)` | Returns a span's inline-object key. |
| `public System.String ShapedGetSpanText(Electron2D.RID shaped, System.Int32 index)` | Returns the source text of an authored span. |
| `public System.String ShapedGetText(Electron2D.RID shaped)` | Returns complete underlying source text, including replacement scalars and the surrounding source of a line view. |
| `public System.Boolean ShapedTextAddObject(Electron2D.RID shaped, Electron2D.TextObjectKey key, Electron2D.Vector2 size, Electron2D.InlineAlignment inlineAlign = Center, System.Int32 length = 1, System.Single baseline = 0f)` | Appends one identity-keyed inline object. |
| `public System.Boolean ShapedTextAddString(Electron2D.RID shaped, System.String text, System.ReadOnlySpan<Electron2D.Font> fonts, System.Int32 size, System.Collections.Generic.IReadOnlyDictionary<System.String, System.Int32> openTypeFeatures = null, System.String language = "")` | Appends copied text with ordered borrowed fonts and copied feature overrides. |
| `public System.Boolean ShapedTextAddString<T>(Electron2D.RID shaped, System.String text, System.ReadOnlySpan<Electron2D.Font> fonts, System.Int32 size, System.Collections.Generic.IReadOnlyDictionary<System.String, System.Int32> openTypeFeatures, System.String language, T metadata)` | Appends copied text with ordered borrowed fonts and copied feature overrides. |
| `public System.Void ShapedTextClear(Electron2D.RID shaped)` | Clears authored text, objects and BIDI overrides while retaining policies and identity. |
| `public System.Void ShapedTextDraw(Electron2D.RID shaped, Electron2D.CanvasItem canvas, Electron2D.Vector2 position, System.Single clipL = -1f, System.Single clipR = -1f, System.Nullable<Electron2D.Color> color = null, System.Single oversampling = 0f)` | Records the current shaped display at a baseline origin. |
| `public System.Void ShapedTextDrawOutline(Electron2D.RID shaped, Electron2D.CanvasItem canvas, Electron2D.Vector2 position, System.Single clipL = -1f, System.Single clipR = -1f, System.Int32 outlineSize = 1, System.Nullable<Electron2D.Color> color = null, System.Single oversampling = 0f)` | Records a shaped glyph outline at a baseline origin. |
| `public Electron2D.RID ShapedTextDuplicate(Electron2D.RID shaped)` | Copies a complete buffer into an independent owned identity. |
| `public System.Single ShapedTextGetAscent(Electron2D.RID shaped)` | Returns fractional ascent above the baseline. |
| `public System.Single ShapedTextGetDescent(Electron2D.RID shaped)` | Returns fractional descent below the baseline. |
| `public Electron2D.TextDirection ShapedTextGetDirection(Electron2D.RID shaped)` | Returns the authored base direction. |
| `public System.Int32 ShapedTextGetGlyphCount(Electron2D.RID shaped)` | Returns the number of glyph records in visual order. |
| `public Electron2D.TextDirection ShapedTextGetInferredDirection(Electron2D.RID shaped)` | Returns the first paragraph's inferred direction. |
| `public Electron2D.Vector2i ShapedTextGetObjectRange(Electron2D.RID shaped, Electron2D.TextObjectKey key)` | Returns an object's half-open scalar range. |
| `public Electron2D.Rect2 ShapedTextGetObjectRect(Electron2D.RID shaped, Electron2D.TextObjectKey key)` | Returns an inline rectangle relative to the buffer baseline. |
| `public Electron2D.TextObjectKey[] ShapedTextGetObjects(Electron2D.RID shaped)` | Returns inline tokens in source order. |
| `public Electron2D.TextOrientation ShapedTextGetOrientation(Electron2D.RID shaped)` | Returns the advance orientation. |
| `public System.Boolean ShapedTextGetPreserveControl(Electron2D.RID shaped)` | Returns the nonprinting-control display policy. |
| `public System.Boolean ShapedTextGetPreserveInvalid(Electron2D.RID shaped)` | Returns the missing-character display policy. |
| `public Electron2D.Vector2i ShapedTextGetRange(Electron2D.RID shaped)` | Returns the buffer's half-open Unicode scalar range. |
| `public Electron2D.Vector2 ShapedTextGetSize(Electron2D.RID shaped)` | Returns ceiled advance and cross-axis dimensions. |
| `public System.Single ShapedTextGetUnderlinePosition(Electron2D.RID shaped)` | Returns underline offset for the buffer's authored fonts. |
| `public System.Single ShapedTextGetUnderlineThickness(Electron2D.RID shaped)` | Returns maximum authored underline thickness. |
| `public System.Single ShapedTextGetWidth(Electron2D.RID shaped)` | Returns the ceiled advance-axis width. |
| `public System.Boolean ShapedTextHasObject(Electron2D.RID shaped, Electron2D.TextObjectKey key)` | Tests for an inline token in the buffer. |
| `public System.Int32 ShapedTextHitTestPosition(Electron2D.RID shaped, System.Single coords)` | Finds the closest scalar caret, interpolating inside shaped ligatures. |
| `public System.Boolean ShapedTextIsReady(Electron2D.RID shaped)` | Reports whether a prepared buffer remains current without forcing a rebuild. |
| `public System.Boolean ShapedTextResizeObject(Electron2D.RID shaped, Electron2D.TextObjectKey key, Electron2D.Vector2 size, Electron2D.InlineAlignment inlineAlign = Center, System.Single baseline = 0f)` | Updates an existing inline object. |
| `public System.Void ShapedTextSetBIDIOverride(Electron2D.RID shaped, System.ReadOnlySpan<Electron2D.TextBIDIRange> ranges)` | Replaces contiguous scalar BIDI override ranges. |
| `public System.Void ShapedTextSetDirection(Electron2D.RID shaped, Electron2D.TextDirection direction = Auto)` | Replaces the base direction. |
| `public System.Void ShapedTextSetOrientation(Electron2D.RID shaped, Electron2D.TextOrientation orientation = Horizontal)` | Replaces the advance orientation. |
| `public System.Void ShapedTextSetPreserveControl(Electron2D.RID shaped, System.Boolean enabled)` | Replaces the nonprinting-control display policy. |
| `public System.Void ShapedTextSetPreserveInvalid(Electron2D.RID shaped, System.Boolean enabled)` | Replaces the missing-character display policy. |
| `public System.Boolean ShapedTextShape(Electron2D.RID shaped)` | Prepares the real native layout and metrics. |
| `public System.Single ShapedTextTabAlign(Electron2D.RID shaped, System.ReadOnlySpan<System.Single> tabStops)` | Aligns tabs to repeating positive interval lengths. |
| `public System.Boolean TryGetSpanMetadata<T>(Electron2D.RID shaped, System.Int32 index, out T metadata)` | Reads span metadata under its exact original type. |

## Buffer editing

Caret results use [TextCaretInfo](TextCaretInfo.md); Unicode composite-character and native shaping-cluster navigation remain separate. Selection and caret geometry are baseline-relative and line-local; paragraph positions retain global scalar origins. See [editing behavior](../components/text-server.md#caret-selection-and-navigation).

| Signature | Contract |
| --- | --- |
| `public Electron2D.TextCaretInfo ShapedTextGetCarets(Electron2D.RID shaped, System.Int32 position)` | Returns the two baseline-relative caret shapes at a Unicode scalar position. |
| `public Electron2D.TextDirection ShapedTextGetDominantDirectionInRange(Electron2D.RID shaped, System.Int32 start, System.Int32 end)` | Returns the direction represented by more overlapping shaped clusters. |
| `public Electron2D.Vector2 ShapedTextGetGraphemeBounds(Electron2D.RID shaped, System.Int32 position)` | Returns the first visual shaped-cluster interval containing a scalar position, including its end. |
| `public Electron2D.Vector2[] ShapedTextGetSelection(Electron2D.RID shaped, System.Int32 start, System.Int32 end)` | Returns ordered visual advance intervals for a logical scalar selection. |
| `public System.Boolean ShapedTextHasVisibleChars(Electron2D.RID shaped)` | Reports whether the buffer has a nonzero, nonvirtual glyph index. |
| `public System.Int32 ShapedTextClosestCharacterPos(Electron2D.RID shaped, System.Int32 position)` | Finds the closest composite-character boundary, choosing the preceding one on a tie. |
| `public System.Int32 ShapedTextHitTestGrapheme(Electron2D.RID shaped, System.Single coords)` | Finds the visual glyph record intersected by an advance coordinate. |
| `public System.Int32 ShapedTextNextCharacterPos(Electron2D.RID shaped, System.Int32 position)` | Finds the first composite-character end strictly after a position. |
| `public System.Int32 ShapedTextNextGraphemePos(Electron2D.RID shaped, System.Int32 position)` | Returns the end of the shaped cluster containing a scalar position. |
| `public System.Int32 ShapedTextPrevCharacterPos(Electron2D.RID shaped, System.Int32 position)` | Finds the composite-character boundary strictly before a position. |
| `public System.Int32 ShapedTextPrevGraphemePos(Electron2D.RID shaped, System.Int32 position)` | Returns the start of the shaped cluster immediately before a scalar position. |
| `public System.Int32[] ShapedTextGetCharacterBreaks(Electron2D.RID shaped)` | Returns composite-character end boundaries, including the buffer end. |

## Wrapping and substrings

The complete [wrapping/ownership contract](../components/text-server.md#line-wrapping-and-owned-substrings) specifies global scalar positions, whole native clusters, source versions and parent retirement.

| Signature | Contract |
| --- | --- |
| `int[] ShapedTextGetLineBreaks(RID shaped, float width, int start = 0, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory \| TextLineBreakFlags.WordBound)` | Single-width line ranges. |
| `int[] ShapedTextGetLineBreaksAdv(RID shaped, ReadOnlySpan<float> width, int start = 0, bool once = true, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory \| TextLineBreakFlags.WordBound)` | Successive-width ranges, optionally one cycle. |
| `RID ShapedTextSubstr(RID shaped, int start, int length)` | Owned complete-cluster slice with retained source context. |
| `RID ShapedTextGetParent(RID shaped)` | Original parent RID, possibly retired, or empty after detachment. |

### ShapedTextGetLineBreaks

Returns an independent alternating start/end array in global Unicode scalar units. Width is finite; nonpositive values disable width wrapping. Start is clamped. Undefined flags throw ArgumentOutOfRangeException. Empty/exhausted ranges return their own origin twice. Word, cluster, adaptive, mandatory and trimming policies follow the linked contract. The query prepares native shaping and may allocate its result/workspace.

### ShapedTextGetLineBreaksAdv

Accepts 1–4096 finite widths synchronously; invalid length throws ArgumentException and nonfinite entries throw ArgumentOutOfRangeException. With once=true it stops after the first width cycle or mandatory separator. Otherwise widths repeat and mandatory separators restart the column sequence. The same break flags apply as the single-width operation. Array/range units and owner-thread/error rules are identical.

### ShapedTextSubstr

Start/length identify a nonnegative range inside the original source; invalid or overflowing ranges throw ArgumentOutOfRangeException before creating an identity. A nested slice addresses its original parent range. Release the returned identity with FreeRID. Whole shaping clusters, inline keys and typed metadata are preserved. Cutting a ligature/object does not create new glyphs. Shared source-version leases survive parent retirement while caller fonts remain borrowed. Parent mutation does not rewrite children. First successful child authoring mutation detaches; failed/no-op mutation leaves the relationship intact. Fitting, trimming and ellipsis configuration retain the original relation.

### ShapedTextGetParent

Returns the original root RID, or empty for independent/duplicated/detached buffers. An unedited borrowed paragraph line reports the paragraph's full buffer. The identity may be retired after source disposal or reflow; Has can test it. Querying a retired child still fails under the usual live-buffer checks.

Example with a live borrowed `Font font` and a recording `CanvasItem canvas`:

```csharp
var server = TextServerManager.GetPrimaryInterface();
var source = server.CreateShapedText();
server.ShapedTextAddString(source, "First column, then another line", [font], 24);
try
{
    var ranges = server.ShapedTextGetLineBreaksAdv(source, [120, 180], once: false);
    for (var i = 0; i < ranges.Length; i += 2)
    {
        var line = server.ShapedTextSubstr(source, ranges[i], ranges[i + 1] - ranges[i]);
        try { server.ShapedTextDraw(line, canvas, new Vector2(0, 30 + i * 20)); }
        finally { server.FreeRID(line); }
    }
}
finally { server.FreeRID(source); }
```

## Protected integration

Registered/selected implementations reject disposal; release owned identities before retiring a caller-owned extension. Public operations retain admission and delegate to these typed hooks. Inherited hooks execute native behavior, with native recursion suppression limited to that operation. The complete [provider contract](../components/text-server.md#external-implementations) describes ownership, results, callbacks and verification limits.

| Signature | Purpose |
| --- | --- |
| `protected Electron2D.RID AllocateFontRID(System.Func<Electron2D.RID, Electron2D.RID> duplicate)` | Allocates an implementation-owned identity for an opaque external font source. |
| `protected Electron2D.RID AllocateShapedTextRID()` | Allocates an implementation-owned identity for an opaque external shaped buffer. |
| `protected System.Void NotifyFontChanged(Electron2D.RID fontRID)` | Publishes a font-source change made outside ordinary service setters. |
| `protected virtual System.Void OnCleanup()` | Releases implementation caches immediately before registry removal. |
| `protected virtual Electron2D.RID OnCreateFont()` | Creates an owned empty font source and cache. |
| `protected virtual Electron2D.RID OnCreateFontLinkedVariation(Electron2D.RID fontRID)` | Creates a linked font sharing the original source, configuration and cache. |
| `protected virtual Electron2D.RID OnCreateShapedText(Electron2D.TextDirection direction = Auto, Electron2D.TextOrientation orientation = Horizontal)` | Creates an empty owned text buffer. |
| `protected virtual System.Void OnDrawHexCodeBox(Electron2D.CanvasItem canvas, System.Int32 size, Electron2D.Vector2 position, System.Int32 index, Electron2D.Color color)` | Records a frame and hexadecimal digits at a text baseline without loading a font. |
| `protected virtual System.Void OnFontClearGlyphs(Electron2D.RID fontRID, Electron2D.Vector2i size)` | Queries or authors the shared clear glyphs cache. |
| `protected virtual System.Void OnFontClearKerningMap(Electron2D.RID fontRID, System.Int32 size)` | Queries or authors the shared clear kerning map cache. |
| `protected virtual System.Void OnFontClearSizeCache(Electron2D.RID fontRID)` | Queries or authors the shared clear size cache cache. |
| `protected virtual System.Void OnFontClearSystemFallbackCache()` | Clears realized automatic fallback caches and invalidates all live font consumers. |
| `protected virtual System.Void OnFontClearTextures(Electron2D.RID fontRID, Electron2D.Vector2i size)` | Queries or authors the shared clear textures cache. |
| `protected virtual System.Void OnFontDrawGlyph(Electron2D.RID fontRID, Electron2D.CanvasItem canvas, System.Int32 size, Electron2D.Vector2 position, System.Int32 index, System.Nullable<Electron2D.Color> color = null, System.Single oversampling = 0f)` | Records a primary glyph at a baseline using the actual shared raster cache. |
| `protected virtual System.Void OnFontDrawGlyphOutline(Electron2D.RID fontRID, Electron2D.CanvasItem canvas, System.Int32 size, System.Int32 outlineSize, Electron2D.Vector2 position, System.Int32 index, System.Nullable<Electron2D.Color> color = null, System.Single oversampling = 0f)` | Records a primary glyph stroke at a baseline using the actual shared raster cache. |
| `protected virtual Electron2D.FontAntialiasing OnFontGetAntialiasing(Electron2D.RID fontRID)` | Reads the shared font antialiasing setting. |
| `protected virtual System.Single OnFontGetAscent(Electron2D.RID fontRID, System.Int32 size)` | Reads the size-cache ascent. |
| `protected virtual System.Single OnFontGetBaselineOffset(Electron2D.RID fontRID)` | Reads this font's independent baseline-height fraction. |
| `protected virtual System.Int32 OnFontGetCharFromGlyphIndex(Electron2D.RID fontRID, System.Int32 size, System.Int32 glyphIndex)` | Returns the first primary character mapped to a glyph, or zero. |
| `protected virtual System.Single OnFontGetDescent(Electron2D.RID fontRID, System.Int32 size)` | Reads the size-cache descent. |
| `protected virtual System.Boolean OnFontGetDisableEmbeddedBitmaps(Electron2D.RID fontRID)` | Reads the shared font disable embedded bitmaps setting. |
| `protected virtual System.Single OnFontGetEmbolden(Electron2D.RID fontRID)` | Reads the shared font embolden. |
| `protected virtual System.Int32 OnFontGetFaceCount(Electron2D.RID fontRID)` | Copies or reads the primary font face count. |
| `protected virtual System.Int32 OnFontGetFaceIndex(Electron2D.RID fontRID)` | Reads the shared font face index. |
| `protected virtual System.Int32 OnFontGetFixedSize(Electron2D.RID fontRID)` | Reads the shared font fixed size setting. |
| `protected virtual Electron2D.FixedSizeScaleMode OnFontGetFixedSizeScaleMode(Electron2D.RID fontRID)` | Reads the shared font fixed size scale mode setting. |
| `protected virtual System.Boolean OnFontGetGenerateMIPMaps(Electron2D.RID fontRID)` | Reads the shared font generate mipmaps setting. |
| `protected virtual Electron2D.Vector2 OnFontGetGlyphAdvance(Electron2D.RID fontRID, System.Int32 size, System.Int32 glyph)` | Queries or authors the shared glyph advance cache. |
| `protected virtual Electron2D.FontGlyphContours OnFontGetGlyphContours(Electron2D.RID fontRID, System.Int32 size, System.Int32 index)` | Copies baseline-relative native Bézier points, contour ends and fill orientation. |
| `protected virtual System.Int32 OnFontGetGlyphIndex(Electron2D.RID fontRID, System.Int32 size, System.Int32 character, System.Int32 variationSelector)` | Resolves a Unicode scalar and optional variation selector to the primary glyph index. |
| `protected virtual System.Int32[] OnFontGetGlyphList(Electron2D.RID fontRID, Electron2D.Vector2i size)` | Queries or authors the shared glyph list cache. |
| `protected virtual Electron2D.Vector2 OnFontGetGlyphOffset(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Reads the shared glyph offset. |
| `protected virtual Electron2D.Vector2 OnFontGetGlyphSize(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Reads the shared glyph size. |
| `protected virtual System.Int32 OnFontGetGlyphTextureIndex(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Reads the shared glyph texture idx. |
| `protected virtual Electron2D.RID OnFontGetGlyphTextureRID(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Returns the real borrowed renderer atlas texture identity, or an empty identity. |
| `protected virtual Electron2D.Vector2 OnFontGetGlyphTextureSize(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Returns actual renderer atlas dimensions, or zero. |
| `protected virtual Electron2D.Rect2 OnFontGetGlyphUVRect(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Reads the shared glyph uv rect. |
| `protected virtual Electron2D.FontHinting OnFontGetHinting(Electron2D.RID fontRID)` | Reads the shared font hinting setting. |
| `protected virtual System.Boolean OnFontGetKeepRoundingRemainders(Electron2D.RID fontRID)` | Reads the shared font keep rounding remainders setting. |
| `protected virtual Electron2D.Vector2 OnFontGetKerning(Electron2D.RID fontRID, System.Int32 size, Electron2D.Vector2i glyphPair)` | Queries or authors the shared kerning cache. |
| `protected virtual Electron2D.Vector2i[] OnFontGetKerningList(Electron2D.RID fontRID, System.Int32 size)` | Queries or authors the shared kerning list cache. |
| `protected virtual System.Boolean OnFontGetLanguageSupportOverride(Electron2D.RID fontRID, System.String language)` | Queries or authors the shared language support policy. |
| `protected virtual System.String[] OnFontGetLanguageSupportOverrides(Electron2D.RID fontRID)` | Queries or authors the shared language support policy. |
| `protected virtual System.Int32 OnFontGetMSDFPixelRange(Electron2D.RID fontRID)` | Reads the shared font msdf pixel range setting. |
| `protected virtual System.Int32 OnFontGetMSDFSize(Electron2D.RID fontRID)` | Reads the shared font msdf size setting. |
| `protected virtual System.String OnFontGetName(Electron2D.RID fontRID)` | Reads the shared font name setting. |
| `protected virtual System.Collections.Generic.Dictionary<System.String, System.Collections.Generic.Dictionary<System.String, System.String>> OnFontGetOTNameStrings(Electron2D.RID fontRID)` | Copies or reads the primary font ot name strings. |
| `protected virtual System.Collections.Generic.Dictionary<System.String, System.Int32> OnFontGetOpenTypeFeatureOverrides(Electron2D.RID fontRID)` | Reads the shared font opentype feature overrides setting. |
| `protected virtual System.Single OnFontGetOversampling(Electron2D.RID fontRID)` | Reads the shared font oversampling setting. |
| `protected virtual Electron2D.Color[] OnFontGetPaletteColors(Electron2D.RID fontRID, System.Int32 index)` | Copies or reads the primary font palette colors. |
| `protected virtual System.Int32 OnFontGetPaletteCount(Electron2D.RID fontRID)` | Copies or reads the primary font palette count. |
| `protected virtual Electron2D.Color[] OnFontGetPaletteCustomColors(Electron2D.RID fontRID)` | Reads the shared font palette custom colors setting. |
| `protected virtual System.String OnFontGetPaletteName(Electron2D.RID fontRID, System.Int32 index)` | Copies or reads the primary font palette name. |
| `protected virtual System.Single OnFontGetScale(Electron2D.RID fontRID, System.Int32 size)` | Reads the size-cache scale. |
| `protected virtual System.Boolean OnFontGetScriptSupportOverride(Electron2D.RID fontRID, System.String script)` | Queries or authors the shared script support policy. |
| `protected virtual System.String[] OnFontGetScriptSupportOverrides(Electron2D.RID fontRID)` | Queries or authors the shared script support policy. |
| `protected virtual Electron2D.FontSizeCacheInfo[] OnFontGetSizeCacheInfo(Electron2D.RID fontRID)` | Copies actual nominal size-cache counts, atlas storage estimates and realized raster scales. |
| `protected virtual Electron2D.Vector2i[] OnFontGetSizeCacheList(Electron2D.RID fontRID)` | Queries or authors the shared size cache list cache. |
| `protected virtual System.Int32 OnFontGetSpacing(Electron2D.RID fontRID, Electron2D.TextSpacingType spacing)` | Reads this root or linked font's independent signed spacing. |
| `protected virtual System.Int32 OnFontGetStretch(Electron2D.RID fontRID)` | Reads the shared font stretch setting. |
| `protected virtual Electron2D.FontStyle OnFontGetStyle(Electron2D.RID fontRID)` | Reads the shared font style setting. |
| `protected virtual System.String OnFontGetStyleName(Electron2D.RID fontRID)` | Reads the shared font style name setting. |
| `protected virtual Electron2D.FontSubpixelPositioning OnFontGetSubpixelPositioning(Electron2D.RID fontRID)` | Reads the shared font subpixel positioning setting. |
| `protected virtual System.String OnFontGetSupportedChars(Electron2D.RID fontRID)` | Copies or reads the primary font supported chars. |
| `protected virtual System.Int32[] OnFontGetSupportedGlyphs(Electron2D.RID fontRID)` | Copies or reads the primary font supported glyphs. |
| `protected virtual System.Int32 OnFontGetTextureCount(Electron2D.RID fontRID, Electron2D.Vector2i size)` | Queries or authors the shared texture count cache. |
| `protected virtual Electron2D.Image OnFontGetTextureImage(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 textureIndex)` | Queries or authors the shared texture image cache. |
| `protected virtual System.Int32[] OnFontGetTextureOffsets(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 textureIndex)` | Queries or authors the shared texture offsets cache. |
| `protected virtual Electron2D.Transform OnFontGetTransform(Electron2D.RID fontRID)` | Reads the shared font transform. |
| `protected virtual System.Single OnFontGetUnderlinePosition(Electron2D.RID fontRID, System.Int32 size)` | Reads the size-cache underline position. |
| `protected virtual System.Single OnFontGetUnderlineThickness(Electron2D.RID fontRID, System.Int32 size)` | Reads the size-cache underline thickness. |
| `protected virtual System.Int32 OnFontGetUsedPalette(Electron2D.RID fontRID)` | Reads the shared font used palette setting. |
| `protected virtual System.Collections.Generic.Dictionary<System.UInt32, System.Single> OnFontGetVariationCoordinates(Electron2D.RID fontRID)` | Reads the shared font variation coordinates. |
| `protected virtual System.Int32 OnFontGetWeight(Electron2D.RID fontRID)` | Reads the shared font weight setting. |
| `protected virtual System.Boolean OnFontHasChar(Electron2D.RID fontRID, System.Int32 character)` | Tests the primary source character map without automatic fallback. |
| `protected virtual System.Boolean OnFontIsAllowSystemFallback(Electron2D.RID fontRID)` | Reads the shared font allow system fallback setting. |
| `protected virtual System.Boolean OnFontIsForceAutohinter(Electron2D.RID fontRID)` | Reads the shared font force autohinter setting. |
| `protected virtual System.Boolean OnFontIsLanguageSupported(Electron2D.RID fontRID, System.String language)` | Queries or authors the shared language support policy. |
| `protected virtual System.Boolean OnFontIsModulateColorGlyphs(Electron2D.RID fontRID)` | Reads the shared font modulate color glyphs setting. |
| `protected virtual System.Boolean OnFontIsMultichannelSignedDistanceField(Electron2D.RID fontRID)` | Reads the shared font multichannel signed distance field setting. |
| `protected virtual System.Boolean OnFontIsScriptSupported(Electron2D.RID fontRID, System.String script)` | Queries or authors the shared script support policy. |
| `protected virtual System.Void OnFontRemoveGlyph(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Queries or authors the shared remove glyph cache. |
| `protected virtual System.Void OnFontRemoveKerning(Electron2D.RID fontRID, System.Int32 size, Electron2D.Vector2i glyphPair)` | Queries or authors the shared remove kerning cache. |
| `protected virtual System.Void OnFontRemoveLanguageSupportOverride(Electron2D.RID fontRID, System.String language)` | Queries or authors the shared language support policy. |
| `protected virtual System.Void OnFontRemoveScriptSupportOverride(Electron2D.RID fontRID, System.String script)` | Queries or authors the shared script support policy. |
| `protected virtual System.Void OnFontRemoveSizeCache(Electron2D.RID fontRID, Electron2D.Vector2i size)` | Queries or authors the shared remove size cache cache. |
| `protected virtual System.Void OnFontRemoveTexture(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 textureIndex)` | Queries or authors the shared remove texture cache. |
| `protected virtual System.Void OnFontRenderGlyph(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 index)` | Queries or authors the shared render glyph cache. |
| `protected virtual System.Void OnFontRenderRange(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 start, System.Int32 end)` | Queries or authors the shared render range cache. |
| `protected virtual System.Void OnFontSetAllowSystemFallback(Electron2D.RID fontRID, System.Boolean value)` | Sets the shared font allow system fallback setting. |
| `protected virtual System.Void OnFontSetAntialiasing(Electron2D.RID fontRID, Electron2D.FontAntialiasing value)` | Sets the shared font antialiasing setting. |
| `protected virtual System.Void OnFontSetAscent(Electron2D.RID fontRID, System.Int32 size, System.Single value)` | Authors the size-cache ascent. |
| `protected virtual System.Void OnFontSetBaselineOffset(Electron2D.RID fontRID, System.Single value)` | Sets this font's independent finite baseline-height fraction. |
| `protected virtual System.Void OnFontSetData(Electron2D.RID fontRID, System.Byte[] data)` | Copies and validates source bytes before replacing the shared font data. |
| `protected virtual System.Void OnFontSetDescent(Electron2D.RID fontRID, System.Int32 size, System.Single value)` | Authors the size-cache descent. |
| `protected virtual System.Void OnFontSetDisableEmbeddedBitmaps(Electron2D.RID fontRID, System.Boolean value)` | Sets the shared font disable embedded bitmaps setting. |
| `protected virtual System.Void OnFontSetEmbolden(Electron2D.RID fontRID, System.Single value)` | Authors the shared font embolden. |
| `protected virtual System.Void OnFontSetFaceIndex(Electron2D.RID fontRID, System.Int32 value)` | Authors the shared font face index. |
| `protected virtual System.Void OnFontSetFixedSize(Electron2D.RID fontRID, System.Int32 value)` | Sets the shared font fixed size setting. |
| `protected virtual System.Void OnFontSetFixedSizeScaleMode(Electron2D.RID fontRID, Electron2D.FixedSizeScaleMode value)` | Sets the shared font fixed size scale mode setting. |
| `protected virtual System.Void OnFontSetForceAutohinter(Electron2D.RID fontRID, System.Boolean value)` | Sets the shared font force autohinter setting. |
| `protected virtual System.Void OnFontSetGenerateMIPMaps(Electron2D.RID fontRID, System.Boolean value)` | Sets the shared font generate mipmaps setting. |
| `protected virtual System.Void OnFontSetGlyphAdvance(Electron2D.RID fontRID, System.Int32 size, System.Int32 glyph, Electron2D.Vector2 value)` | Queries or authors the shared glyph advance cache. |
| `protected virtual System.Void OnFontSetGlyphOffset(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph, Electron2D.Vector2 value)` | Authors the shared glyph offset. |
| `protected virtual System.Void OnFontSetGlyphSize(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph, Electron2D.Vector2 value)` | Authors the shared glyph size. |
| `protected virtual System.Void OnFontSetGlyphTextureIndex(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph, System.Int32 textureIndex)` | Authors the shared glyph texture idx. |
| `protected virtual System.Void OnFontSetGlyphUVRect(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph, Electron2D.Rect2 value)` | Authors the shared glyph uv rect. |
| `protected virtual System.Void OnFontSetHinting(Electron2D.RID fontRID, Electron2D.FontHinting value)` | Sets the shared font hinting setting. |
| `protected virtual System.Void OnFontSetKeepRoundingRemainders(Electron2D.RID fontRID, System.Boolean value)` | Sets the shared font keep rounding remainders setting. |
| `protected virtual System.Void OnFontSetKerning(Electron2D.RID fontRID, System.Int32 size, Electron2D.Vector2i glyphPair, Electron2D.Vector2 value)` | Queries or authors the shared kerning cache. |
| `protected virtual System.Void OnFontSetLanguageSupportOverride(Electron2D.RID fontRID, System.String language, System.Boolean supported)` | Queries or authors the shared language support policy. |
| `protected virtual System.Void OnFontSetMSDFPixelRange(Electron2D.RID fontRID, System.Int32 value)` | Sets the shared font msdf pixel range setting. |
| `protected virtual System.Void OnFontSetMSDFSize(Electron2D.RID fontRID, System.Int32 value)` | Sets the shared font msdf size setting. |
| `protected virtual System.Void OnFontSetModulateColorGlyphs(Electron2D.RID fontRID, System.Boolean value)` | Sets the shared font modulate color glyphs setting. |
| `protected virtual System.Void OnFontSetMultichannelSignedDistanceField(Electron2D.RID fontRID, System.Boolean value)` | Sets the shared font multichannel signed distance field setting. |
| `protected virtual System.Void OnFontSetName(Electron2D.RID fontRID, System.String value)` | Sets the shared font name setting. |
| `protected virtual System.Void OnFontSetOpenTypeFeatureOverrides(Electron2D.RID fontRID, System.Collections.Generic.Dictionary<System.String, System.Int32> value)` | Sets the shared font opentype feature overrides setting. |
| `protected virtual System.Void OnFontSetOversampling(Electron2D.RID fontRID, System.Single value)` | Sets the shared font oversampling setting. |
| `protected virtual System.Void OnFontSetPaletteCustomColors(Electron2D.RID fontRID, Electron2D.Color[] value)` | Sets the shared font palette custom colors setting. |
| `protected virtual System.Void OnFontSetScale(Electron2D.RID fontRID, System.Int32 size, System.Single value)` | Authors the size-cache scale. |
| `protected virtual System.Void OnFontSetScriptSupportOverride(Electron2D.RID fontRID, System.String script, System.Boolean supported)` | Queries or authors the shared script support policy. |
| `protected virtual System.Void OnFontSetSpacing(Electron2D.RID fontRID, Electron2D.TextSpacingType spacing, System.Int32 value)` | Sets this root or linked font's independent signed spacing. |
| `protected virtual System.Void OnFontSetStretch(Electron2D.RID fontRID, System.Int32 value)` | Sets the shared font stretch setting. |
| `protected virtual System.Void OnFontSetStyle(Electron2D.RID fontRID, Electron2D.FontStyle value)` | Sets the shared font style setting. |
| `protected virtual System.Void OnFontSetStyleName(Electron2D.RID fontRID, System.String value)` | Sets the shared font style name setting. |
| `protected virtual System.Void OnFontSetSubpixelPositioning(Electron2D.RID fontRID, Electron2D.FontSubpixelPositioning value)` | Sets the shared font subpixel positioning setting. |
| `protected virtual System.Void OnFontSetTextureImage(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 textureIndex, Electron2D.Image image)` | Queries or authors the shared texture image cache. |
| `protected virtual System.Void OnFontSetTextureOffsets(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 textureIndex, System.Int32[] offsets)` | Queries or authors the shared texture offsets cache. |
| `protected virtual System.Void OnFontSetTransform(Electron2D.RID fontRID, Electron2D.Transform value)` | Authors the shared font transform. |
| `protected virtual System.Void OnFontSetUnderlinePosition(Electron2D.RID fontRID, System.Int32 size, System.Single value)` | Authors the size-cache underline position. |
| `protected virtual System.Void OnFontSetUnderlineThickness(Electron2D.RID fontRID, System.Int32 size, System.Single value)` | Authors the size-cache underline thickness. |
| `protected virtual System.Void OnFontSetUsedPalette(Electron2D.RID fontRID, System.Int32 value)` | Sets the shared font used palette setting. |
| `protected virtual System.Void OnFontSetVariationCoordinates(Electron2D.RID fontRID, System.Collections.Generic.Dictionary<System.UInt32, System.Single> value)` | Authors the shared font variation coordinates. |
| `protected virtual System.Void OnFontSetWeight(Electron2D.RID fontRID, System.Int32 value)` | Sets the shared font weight setting. |
| `protected virtual System.Collections.Generic.Dictionary<System.UInt32, Electron2D.FontFeatureInfo> OnFontSupportedFeatureList(Electron2D.RID fontRID)` | Copies supported OpenType feature metadata. |
| `protected virtual System.Collections.Generic.Dictionary<System.UInt32, Electron2D.FontVariationAxis> OnFontSupportedVariationList(Electron2D.RID fontRID)` | Copies supported OpenType variation-axis metadata. |
| `protected virtual System.Void OnFreeRID(Electron2D.RID rid)` | Releases an owned font or buffer, preserving borrowed text-object lifetimes. |
| `protected virtual Electron2D.TextServer.Feature OnGetFeatures()` | Supplies the implementation's capability mask. |
| `protected virtual Electron2D.Vector2 OnGetHexCodeBoxSize(System.Int32 size, System.Int32 index)` | Measures a hexadecimal replacement box without loading a font. |
| `protected abstract System.String OnGetName()` | Returns the stable name of this text implementation. |
| `protected virtual System.Byte[] OnGetSupportData()` | Returns an independent copy of the embedded native text database. |
| `protected virtual System.String OnGetSupportDataFilename()` | Returns the default database export filename. |
| `protected virtual System.String OnGetSupportDataInfo()` | Describes the embedded native text database. |
| `protected virtual System.Boolean OnHas(Electron2D.RID rid)` | Reports whether an identity resolves to a live font entry or shaped buffer. |
| `protected virtual System.Boolean OnHasFeature(Electron2D.TextServer.Feature feature)` | Tests one text capability against the current implementation mask. |
| `protected virtual System.Int32 OnIsConfusable(System.String text, System.ReadOnlySpan<System.String> dictionary)` | Finds the first dictionary entry with the same Unicode confusable skeleton. |
| `protected virtual System.Boolean OnIsLocaleRightToLeft(System.String locale)` | Tests the language component against the supported right-to-left language policy. |
| `protected virtual System.Boolean OnIsLocaleUsingSupportData(System.String locale)` | Tests whether a locale uses dictionary support for word or line breaking. |
| `protected virtual System.Boolean OnIsValidIdentifier(System.String text)` | Tests an NFC Unicode identifier in recommended scripts, with contextual join controls. |
| `protected virtual System.Boolean OnIsValidLetter(System.Int32 unicode)` | Tests whether a Unicode scalar has a letter general category. |
| `protected virtual System.Boolean OnLoadSupportData(System.String filename)` | Initializes the immutable embedded native text database. |
| `protected virtual System.UInt32 OnNameToTag(System.String name)` | Resolves a registered feature or variation name, or a literal four-byte tag. |
| `protected virtual System.ReadOnlySpan<Electron2D.TextBIDIRange> OnParseStructuredText(Electron2D.StructuredTextParser parser, System.Collections.Generic.IReadOnlyList<System.String> options, System.String text)` | Supplies structured text contexts through the native built-in parser. |
| `protected virtual System.Void OnReferenceOversamplingLevel(System.Single oversampling)` | Retains one viewport or canvas-item reference to a quantized raster scale other than one. |
| `protected virtual System.Boolean OnSaveSupportData(System.String filename)` | Exports the embedded text database through the engine file service. |
| `protected virtual System.Int32 OnShapedGetRunCount(Electron2D.RID shaped)` | Returns the number of contiguous visual runs with one span, source, size and direction. |
| `protected virtual Electron2D.TextDirection OnShapedGetRunDirection(Electron2D.RID shaped, System.Int32 index)` | Returns a visual run's resolved writing direction. |
| `protected virtual Electron2D.RID OnShapedGetRunFontRID(Electron2D.RID shaped, System.Int32 index)` | Returns the actual source identity used by a visual run. |
| `protected virtual System.Int32 OnShapedGetRunFontSize(Electron2D.RID shaped, System.Int32 index)` | Returns the logical font size used by a visual run. |
| `protected virtual Electron2D.Vector2i OnShapedGetRunGlyphRange(Electron2D.RID shaped, System.Int32 index)` | Returns the first and last cluster-leader indices belonging to a visual run. |
| `protected virtual System.String OnShapedGetRunLanguage(Electron2D.RID shaped, System.Int32 index)` | Returns a visual run's authored language tag. |
| `protected virtual Electron2D.TextObjectKey OnShapedGetRunObject(Electron2D.RID shaped, System.Int32 index)` | Returns a visual run's borrowed inline-object identity. |
| `protected virtual Electron2D.Vector2i OnShapedGetRunRange(Electron2D.RID shaped, System.Int32 index)` | Returns one run's global scalar range, with an exclusive end. |
| `protected virtual System.String OnShapedGetRunText(Electron2D.RID shaped, System.Int32 index)` | Returns one visual run's original source text. |
| `protected virtual System.Int32 OnShapedGetSpanCount(Electron2D.RID shaped)` | Returns the number of source text and inline-object spans intersecting this buffer. |
| `protected virtual Electron2D.TextObjectKey OnShapedGetSpanEmbeddedObject(Electron2D.RID shaped, System.Int32 index)` | Returns a span's embedded inline-object key. |
| `protected virtual Electron2D.TextObjectKey OnShapedGetSpanObject(Electron2D.RID shaped, System.Int32 index)` | Returns a span's inline-object key. |
| `protected virtual System.String OnShapedGetSpanText(Electron2D.RID shaped, System.Int32 index)` | Returns the source text of an authored span. |
| `protected virtual System.String OnShapedGetText(Electron2D.RID shaped)` | Returns complete underlying source text, including replacement scalars and the surrounding source of a line view. |
| `protected virtual System.Void OnShapedSetSpanUpdateFont(Electron2D.RID shaped, System.Int32 index, System.ReadOnlySpan<Electron2D.Font> fonts, System.Int32 size, System.Collections.Generic.IReadOnlyDictionary<System.String, System.Int32> openTypeFeatures = null)` | Replaces a span's ordered fonts, size and features without changing its text, language or metadata. |
| `protected virtual System.Boolean OnShapedTextAddObject(Electron2D.RID shaped, Electron2D.TextObjectKey key, Electron2D.Vector2 size, Electron2D.InlineAlignment inlineAlign = Center, System.Int32 length = 1, System.Single baseline = 0f)` | Appends one identity-keyed inline object. |
| `protected virtual System.Boolean OnShapedTextAddString(Electron2D.RID shaped, System.String text, System.ReadOnlySpan<Electron2D.Font> fonts, System.Int32 size, System.Collections.Generic.IReadOnlyDictionary<System.String, System.Int32> openTypeFeatures = null, System.String language = "")` | Appends text retaining metadata under its exact supplied type. |
| `protected virtual System.Boolean OnShapedTextAddString<T>(Electron2D.RID shaped, System.String text, System.ReadOnlySpan<Electron2D.Font> fonts, System.Int32 size, System.Collections.Generic.IReadOnlyDictionary<System.String, System.Int32> openTypeFeatures, System.String language, T metadata)` | Appends text retaining metadata under its exact supplied type. |
| `protected virtual System.Void OnShapedTextClear(Electron2D.RID shaped)` | Clears authored text, objects and BIDI overrides while retaining policies and identity. |
| `protected virtual System.Int32 OnShapedTextClosestCharacterPos(Electron2D.RID shaped, System.Int32 position)` | Finds the closest composite-character boundary, choosing the preceding one on a tie. |
| `protected virtual System.Void OnShapedTextDraw(Electron2D.RID shaped, Electron2D.CanvasItem canvas, Electron2D.Vector2 position, System.Single clipL = -1f, System.Single clipR = -1f, System.Nullable<Electron2D.Color> color = null, System.Single oversampling = 0f)` | Records the current shaped display at a baseline origin. |
| `protected virtual System.Void OnShapedTextDrawOutline(Electron2D.RID shaped, Electron2D.CanvasItem canvas, Electron2D.Vector2 position, System.Single clipL = -1f, System.Single clipR = -1f, System.Int32 outlineSize = 1, System.Nullable<Electron2D.Color> color = null, System.Single oversampling = 0f)` | Records a shaped glyph outline at a baseline origin. |
| `protected virtual Electron2D.RID OnShapedTextDuplicate(Electron2D.RID shaped)` | Copies a complete buffer into an independent owned identity. |
| `protected virtual System.Single OnShapedTextFitToWidth(Electron2D.RID shaped, System.Single width, Electron2D.TextJustificationFlags justificationFlags = Kashida, WordBound)` | Adjusts current shaped advances using available justification opportunities. |
| `protected virtual System.Single OnShapedTextGetAscent(Electron2D.RID shaped)` | Returns fractional ascent above the baseline. |
| `protected virtual Electron2D.TextCaretInfo OnShapedTextGetCarets(Electron2D.RID shaped, System.Int32 position)` | Returns the two baseline-relative caret shapes at a Unicode scalar position. |
| `protected virtual System.Int32[] OnShapedTextGetCharacterBreaks(Electron2D.RID shaped)` | Returns composite-character end boundaries, including the buffer end. |
| `protected virtual System.Int32 OnShapedTextGetCustomEllipsis(Electron2D.RID shaped)` | Returns the configured ellipsis Unicode scalar. |
| `protected virtual System.String OnShapedTextGetCustomPunctuation(Electron2D.RID shaped)` | Returns the custom punctuation list. |
| `protected virtual System.Single OnShapedTextGetDescent(Electron2D.RID shaped)` | Returns fractional descent below the baseline. |
| `protected virtual Electron2D.TextDirection OnShapedTextGetDirection(Electron2D.RID shaped)` | Returns the authored base direction. |
| `protected virtual Electron2D.TextDirection OnShapedTextGetDominantDirectionInRange(Electron2D.RID shaped, System.Int32 start, System.Int32 end)` | Returns the direction represented by more overlapping shaped clusters. |
| `protected virtual System.Int32 OnShapedTextGetEllipsisGlyphCount(Electron2D.RID shaped)` | Returns the number of current display ellipsis records, excluding repeat expansion. |
| `protected virtual System.ReadOnlySpan<Electron2D.TextGlyph> OnShapedTextGetEllipsisGlyphs(Electron2D.RID shaped)` | Copies only the current display ellipsis records. |
| `protected virtual System.Int32 OnShapedTextGetEllipsisPos(Electron2D.RID shaped)` | Returns the current visual glyph-record ellipsis boundary. |
| `protected virtual System.Int32 OnShapedTextGetGlyphCount(Electron2D.RID shaped)` | Returns the number of glyph records in visual order. |
| `protected virtual System.ReadOnlySpan<Electron2D.TextGlyph> OnShapedTextGetGlyphs(Electron2D.RID shaped)` | Copies the complete visual glyph records, independently of display trimming. |
| `protected virtual Electron2D.Vector2 OnShapedTextGetGraphemeBounds(Electron2D.RID shaped, System.Int32 position)` | Returns the first visual shaped-cluster interval containing a scalar position, including its end. |
| `protected virtual Electron2D.TextDirection OnShapedTextGetInferredDirection(Electron2D.RID shaped)` | Returns the first paragraph's inferred direction. |
| `protected virtual System.Int32[] OnShapedTextGetLineBreaks(Electron2D.RID shaped, System.Single width, System.Int32 start = 0, Electron2D.TextLineBreakFlags breakFlags = Mandatory, WordBound)` | Splits a shaped buffer into half-open Unicode scalar line ranges. |
| `protected virtual System.Int32[] OnShapedTextGetLineBreaksAdv(Electron2D.RID shaped, System.ReadOnlySpan<System.Single> width, System.Int32 start = 0, System.Boolean once = true, Electron2D.TextLineBreakFlags breakFlags = Mandatory, WordBound)` | Splits a shaped buffer using successive column widths. |
| `protected virtual System.Int32 OnShapedTextGetObjectGlyph(Electron2D.RID shaped, Electron2D.TextObjectKey key)` | Finds the visual glyph record for an inline object. |
| `protected virtual Electron2D.Vector2i OnShapedTextGetObjectRange(Electron2D.RID shaped, Electron2D.TextObjectKey key)` | Returns an object's half-open scalar range. |
| `protected virtual Electron2D.Rect2 OnShapedTextGetObjectRect(Electron2D.RID shaped, Electron2D.TextObjectKey key)` | Returns an inline rectangle relative to the buffer baseline. |
| `protected virtual Electron2D.TextObjectKey[] OnShapedTextGetObjects(Electron2D.RID shaped)` | Returns inline tokens in source order. |
| `protected virtual Electron2D.TextOrientation OnShapedTextGetOrientation(Electron2D.RID shaped)` | Returns the advance orientation. |
| `protected virtual Electron2D.RID OnShapedTextGetParent(Electron2D.RID shaped)` | Returns the original substring parent identity. |
| `protected virtual System.Boolean OnShapedTextGetPreserveControl(Electron2D.RID shaped)` | Returns the nonprinting-control display policy. |
| `protected virtual System.Boolean OnShapedTextGetPreserveInvalid(Electron2D.RID shaped)` | Returns the missing-character display policy. |
| `protected virtual Electron2D.Vector2i OnShapedTextGetRange(Electron2D.RID shaped)` | Returns the buffer's half-open Unicode scalar range. |
| `protected virtual System.ReadOnlySpan<Electron2D.Vector2> OnShapedTextGetSelection(Electron2D.RID shaped, System.Int32 start, System.Int32 end)` | Returns ordered visual advance intervals for a logical scalar selection. |
| `protected virtual Electron2D.Vector2 OnShapedTextGetSize(Electron2D.RID shaped)` | Returns ceiled advance and cross-axis dimensions. |
| `protected virtual System.Int32 OnShapedTextGetSpacing(Electron2D.RID shaped, Electron2D.TextSpacingType spacing)` | Returns extra buffer spacing. |
| `protected virtual System.Int32 OnShapedTextGetTrimPos(Electron2D.RID shaped)` | Returns the current visual glyph-record trim boundary. |
| `protected virtual System.Single OnShapedTextGetUnderlinePosition(Electron2D.RID shaped)` | Returns underline offset for the buffer's authored fonts. |
| `protected virtual System.Single OnShapedTextGetUnderlineThickness(Electron2D.RID shaped)` | Returns maximum authored underline thickness. |
| `protected virtual System.Single OnShapedTextGetWidth(Electron2D.RID shaped)` | Returns the ceiled advance-axis width. |
| `protected virtual System.Int32[] OnShapedTextGetWordBreaks(Electron2D.RID shaped, Electron2D.TextGraphemeFlags graphemeFlags = Space, Punctuation, Electron2D.TextGraphemeFlags skipGraphemeFlags = Virtual)` | Returns word ranges delimited by selected shaped-cluster properties. |
| `protected virtual System.Boolean OnShapedTextHasObject(Electron2D.RID shaped, Electron2D.TextObjectKey key)` | Tests for an inline token in the buffer. |
| `protected virtual System.Boolean OnShapedTextHasVisibleChars(Electron2D.RID shaped)` | Reports whether the buffer has a nonzero, nonvirtual glyph index. |
| `protected virtual System.Int32 OnShapedTextHitTestGrapheme(Electron2D.RID shaped, System.Single coords)` | Finds the visual glyph record intersected by an advance coordinate. |
| `protected virtual System.Int32 OnShapedTextHitTestPosition(Electron2D.RID shaped, System.Single coords)` | Finds the closest scalar caret, interpolating inside shaped ligatures. |
| `protected virtual System.Boolean OnShapedTextIsReady(Electron2D.RID shaped)` | Reports whether a prepared buffer remains current without forcing a rebuild. |
| `protected virtual System.Int32 OnShapedTextNextCharacterPos(Electron2D.RID shaped, System.Int32 position)` | Finds the first composite-character end strictly after a position. |
| `protected virtual System.Int32 OnShapedTextNextGraphemePos(Electron2D.RID shaped, System.Int32 position)` | Returns the end of the shaped cluster containing a scalar position. |
| `protected virtual System.Void OnShapedTextOverrunTrimToWidth(Electron2D.RID shaped, System.Single width = 0f, Electron2D.TextOverrunFlags overrunTrimFlags = NoTrim)` | Chooses an independent visible glyph range and optional ellipsis without removing original clusters. |
| `protected virtual System.Int32 OnShapedTextPrevCharacterPos(Electron2D.RID shaped, System.Int32 position)` | Finds the composite-character boundary strictly before a position. |
| `protected virtual System.Int32 OnShapedTextPrevGraphemePos(Electron2D.RID shaped, System.Int32 position)` | Returns the start of the shaped cluster immediately before a scalar position. |
| `protected virtual System.Boolean OnShapedTextResizeObject(Electron2D.RID shaped, Electron2D.TextObjectKey key, Electron2D.Vector2 size, Electron2D.InlineAlignment inlineAlign = Center, System.Single baseline = 0f)` | Updates an existing inline object. |
| `protected virtual System.Void OnShapedTextSetBIDIOverride(Electron2D.RID shaped, System.ReadOnlySpan<Electron2D.TextBIDIRange> ranges)` | Replaces contiguous scalar BIDI override ranges. |
| `protected virtual System.Void OnShapedTextSetCustomEllipsis(Electron2D.RID shaped, System.Int32 character)` | Sets the Unicode scalar used by subsequent explicit trimming. |
| `protected virtual System.Void OnShapedTextSetCustomPunctuation(Electron2D.RID shaped, System.String punctuation)` | Replaces the punctuation scalar list used when classifying justification opportunities. |
| `protected virtual System.Void OnShapedTextSetDirection(Electron2D.RID shaped, Electron2D.TextDirection direction = Auto)` | Replaces the base direction. |
| `protected virtual System.Void OnShapedTextSetOrientation(Electron2D.RID shaped, Electron2D.TextOrientation orientation = Horizontal)` | Replaces the advance orientation. |
| `protected virtual System.Void OnShapedTextSetPreserveControl(Electron2D.RID shaped, System.Boolean enabled)` | Replaces the nonprinting-control display policy. |
| `protected virtual System.Void OnShapedTextSetPreserveInvalid(Electron2D.RID shaped, System.Boolean enabled)` | Replaces the missing-character display policy. |
| `protected virtual System.Void OnShapedTextSetSpacing(Electron2D.RID shaped, Electron2D.TextSpacingType spacing, System.Int32 value)` | Sets extra signed buffer spacing independently of font-source spacing. |
| `protected virtual System.Boolean OnShapedTextShape(Electron2D.RID shaped)` | Prepares the real native layout and metrics. |
| `protected virtual System.ReadOnlySpan<Electron2D.TextGlyph> OnShapedTextSortLogical(Electron2D.RID shaped)` | Copies complete glyph records in stable logical scalar order. |
| `protected virtual Electron2D.RID OnShapedTextSubstr(Electron2D.RID shaped, System.Int32 start, System.Int32 length)` | Creates an owned substring retaining complete original shaped clusters and source context. |
| `protected virtual System.Single OnShapedTextTabAlign(Electron2D.RID shaped, System.ReadOnlySpan<System.Single> tabStops)` | Aligns tabs to repeating positive interval lengths. |
| `protected virtual System.Boolean OnShapedTextUpdateBreaks(Electron2D.RID shaped)` | Prepares shaped break opportunities for the default line-breaking operations. |
| `protected virtual System.Boolean OnShapedTextUpdateJustificationOps(Electron2D.RID shaped)` | Prepares shaped justification opportunities for default fitting and word-boundary operations. |
| `protected virtual System.Boolean OnSpoofCheck(System.String text)` | Checks Unicode security restrictions, invisible sequences, mixed numbers and allowed identifier characters. |
| `protected virtual System.Int32[] OnStringGetCharacterBreaks(System.String text, System.String language = "")` | Returns composite character end boundaries using the locale-aware Unicode iterator. |
| `protected virtual System.Int32[] OnStringGetWordBreaks(System.String text, System.String language = "", System.Int32 charsPerLine = 0)` | Returns word ranges, or scalar-width line ranges when a positive limit is supplied. |
| `protected virtual System.String OnStringToLower(System.String text, System.String language = "")` | Converts text to lowercase with full Unicode and locale-sensitive rules. |
| `protected virtual System.String OnStringToTitle(System.String text, System.String language = "")` | Titlecases Unicode words, lowercasing their remaining characters. |
| `protected virtual System.String OnStringToUpper(System.String text, System.String language = "")` | Converts text to uppercase with full Unicode expansions and locale tailoring. |
| `protected virtual System.String OnStripDiacritics(System.String text)` | Applies compatibility decomposition and removes nonzero canonical combining classes. |
| `protected virtual System.String OnTagToName(System.UInt32 tag)` | Returns a registered tag name or its custom_ four-byte spelling. |
| `protected virtual System.Boolean OnTryGetSpanMetadata<T>(Electron2D.RID shaped, System.Int32 index, out T metadata)` | Reads span metadata under its exact original type. |
| `protected virtual System.Void OnUnreferenceOversamplingLevel(System.Single oversampling)` | Releases one raster-scale reference; the last release permits retiring that scale's font caches. |

## Buffer formatting

See [fitting, spacing and overrun](../components/text-server.md#fitting-spacing-and-overrun) and [TextOverrunFlags](TextOverrunFlags.md). Operations retain the shared owner/thread/font-generation contract.

| Signature | Behavior |
| --- | --- |
| `float ShapedTextFitToWidth(RID shaped, float width, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida \| TextJustificationFlags.WordBound)` | Adjusts actual raw advances; returns the ceiled achievable width. Finite targets and defined flags are required. Existing display cuts remain independent; ConstrainEllipsis fits only the retained visible range. |
| `void ShapedTextOverrunTrimToWidth(RID shaped, float width = 0, TextOverrunFlags overrunTrimFlags = TextOverrunFlags.NoTrim)` | Chooses complete visible clusters and optional ellipsis. NoTrim/nonpositive width restores raw display; original clusters and editing geometry remain complete. Undefined flags and nonfinite widths throw. |
| `int ShapedTextGetTrimPos(RID shaped)` | Returns the current buffer-local visual glyph-record trim boundary, or -1 when untrimmed/unprepared. This is not a scalar position. |
| `int ShapedTextGetEllipsisPos(RID shaped)` | Returns the current buffer-local visual glyph-record ellipsis boundary, or -1 without an active ellipsis. |
| `void ShapedTextSetCustomEllipsis(RID shaped, int character)` | Sets a valid Unicode scalar for subsequent explicit trims, preserving current display and parent relation. Unsupported glyphs use three periods when available. |
| `int ShapedTextGetCustomEllipsis(RID shaped)` | Returns the configured Unicode scalar, initially U+2026. Borrowed paragraph lines inherit the paragraph symbol; overrides retain it until explicitly changed. |
| `void ShapedTextSetCustomPunctuation(RID shaped, string punctuation)` | Copies punctuation classification, at most 64 MiB UTF-16. Null throws; nonempty text replaces default punctuation. A changed value invalidates shaping and detaches substring authoring. |
| `string ShapedTextGetCustomPunctuation(RID shaped)` | Returns the source punctuation string, initially empty. The paragraph property shares this same owner. |
| `void ShapedTextSetSpacing(RID shaped, TextSpacingType spacing, int value)` | Sets signed extra logical-pixel spacing for Glyph, Space, Top or Bottom; Max/undefined categories throw. Changes invalidate shaping and detach authored substrings; equal values are no-ops. |
| `int ShapedTextGetSpacing(RID shaped, TextSpacingType spacing)` | Returns signed extra spacing, initially zero; it survives clear and duplicate. Only the four selectable categories are accepted. |

<a id="shapedtextfittowidth"></a>
### ShapedTextFitToWidth

`float ShapedTextFitToWidth(RID shaped, float width, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound)`

Adjusts actual raw advances; returns the ceiled achievable width. Finite targets and defined flags are required. Existing display cuts remain independent; ConstrainEllipsis fits only the retained visible range.

<a id="shapedtextoverruntrimtowidth"></a>
### ShapedTextOverrunTrimToWidth

`void ShapedTextOverrunTrimToWidth(RID shaped, float width = 0, TextOverrunFlags overrunTrimFlags = TextOverrunFlags.NoTrim)`

Chooses complete visible clusters and optional ellipsis. NoTrim/nonpositive width restores raw display; original clusters and editing geometry remain complete. Undefined flags and nonfinite widths throw.

<a id="shapedtextgettrimpos"></a>
### ShapedTextGetTrimPos

`int ShapedTextGetTrimPos(RID shaped)`

Returns the current buffer-local visual glyph-record trim boundary, or -1 when untrimmed/unprepared. This is not a scalar position.

<a id="shapedtextgetellipsispos"></a>
### ShapedTextGetEllipsisPos

`int ShapedTextGetEllipsisPos(RID shaped)`

Returns the current buffer-local visual glyph-record ellipsis boundary, or -1 without an active ellipsis.

<a id="shapedtextsetcustomellipsis"></a>
### ShapedTextSetCustomEllipsis

`void ShapedTextSetCustomEllipsis(RID shaped, int character)`

Sets a valid Unicode scalar for subsequent explicit trims, preserving current display and parent relation. Unsupported glyphs use three periods when available.

<a id="shapedtextgetcustomellipsis"></a>
### ShapedTextGetCustomEllipsis

`int ShapedTextGetCustomEllipsis(RID shaped)`

Returns the configured Unicode scalar, initially U+2026. Borrowed paragraph lines inherit the paragraph symbol; overrides retain it until explicitly changed.

<a id="shapedtextsetcustompunctuation"></a>
### ShapedTextSetCustomPunctuation

`void ShapedTextSetCustomPunctuation(RID shaped, string punctuation)`

Copies punctuation classification, at most 64 MiB UTF-16. Null throws; nonempty text replaces default punctuation. A changed value invalidates shaping and detaches substring authoring.

<a id="shapedtextgetcustompunctuation"></a>
### ShapedTextGetCustomPunctuation

`string ShapedTextGetCustomPunctuation(RID shaped)`

Returns the source punctuation string, initially empty. The paragraph property shares this same owner.

<a id="shapedtextsetspacing"></a>
### ShapedTextSetSpacing

`void ShapedTextSetSpacing(RID shaped, TextSpacingType spacing, int value)`

Sets signed extra logical-pixel spacing for Glyph, Space, Top or Bottom; Max/undefined categories throw. Changes invalidate shaping and detach authored substrings; equal values are no-ops.

<a id="shapedtextgetspacing"></a>
### ShapedTextGetSpacing

`int ShapedTextGetSpacing(RID shaped, TextSpacingType spacing)`

Returns signed extra spacing, initially zero; it survives clear and duplicate. Only the four selectable categories are accepted.

## Unicode and locale utilities

See the [standalone Unicode contract](../components/text-server.md#standalone-unicode-utilities) for exact range units, normalization, join-control rules, bounds and verification limits. These operations are stateless and callable across threads; returned arrays are independent cold snapshots. Null string arguments throw ArgumentNullException and strings beyond 64 MiB UTF-16 throw ArgumentException. Empty language samples the current tool locale.

| Signature | Behavior |
| --- | --- |
| `bool IsLocaleRightToLeft(string locale)` | Tests the case-sensitive ar/dv/he/fa/ff/ku/ur language policy; accepts underscore/hyphen separators, empty is false. |
| `bool IsValidIdentifier(string text)` | NFC/recommended-script XID syntax with initial underscore and contextual join controls; malformed/empty is false. Does not detect confusables. |
| `bool IsValidLetter(int unicode)` | Unicode letter category; invalid scalars and surrogate values return false. |
| `uint NameToTag(string name)` | Registered alias or custom big-endian four-byte OpenType tag. |
| `string TagToName(uint tag)` | Canonical registered name or custom_ Latin-1 bytes, stopped at NUL. |
| `int[] StringGetCharacterBreaks(string text, string language = "")` | Composite-character scalar ends, including nonempty source end. |
| `int[] StringGetWordBreaks(string text, string language = "", int charsPerLine = 0)` | Alternating scalar starts/ends for words, or scalar-width lines when positive. |
| `string StringToLower(string text, string language = "")` | Full locale-sensitive lowercase. |
| `string StringToTitle(string text, string language = "")` | Locale-sensitive word titlecase with remaining letters lowercased. |
| `string StringToUpper(string text, string language = "")` | Shared managed full uppercase and locale tailoring. |
| `string StripDiacritics(string text)` | NFKD compatibility decomposition, removing nonzero canonical combining classes. |

Transformations preserve embedded NUL and unpaired UTF-16. Positive character-width word ranges can split composites; use composite ends when that cut is unwanted. Hard separators do not occur in returned word/width ranges. Native lower/title/strip reject an oversized expanded result before output allocation. Scalar/known-tag queries reuse warmed state; transformations and arrays may allocate.

## Span font replacement and shaped words

| Signature | Behavior |
| --- | --- |
| `void ShapedSetSpanUpdateFont(RID shaped, int index, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures = null)` | Copies ordered fonts/features, replaces a slice-local text-or-object span and invalidates native geometry without changing source, language or typed metadata. |
| `int[] ShapedTextGetWordBreaks(RID shaped, TextGraphemeFlags graphemeFlags = TextGraphemeFlags.Space \| TextGraphemeFlags.Punctuation, TextGraphemeFlags skipGraphemeFlags = TextGraphemeFlags.Virtual)` | Returns independent global scalar word ranges from logical shaped clusters and selected property masks. |

Font sources require one to sixty-four live entries, a positive size and at most 4096 features. Null removes prior feature overrides. Invalid input does not detach a substring or publish a borrowed line; every successful update is authoring, including an equal setting. Replaced internal adapters retire only when their last source/snapshot/duplicate lease ends. Caller font resources stay borrowed. Explicit inline geometry remains independent of fonts. A replacement can recover a buffer whose previous caller font retired. A borrowed paragraph line publishes its own override, reaching the paragraph consumer until reflow retires the RID.

Word delimiters exclude complete source clusters; virtual opportunities mark boundaries without consuming source characters. No delimiters returns the complete nonempty shaped range, while an empty/unshaped partial-cluster slice has no ranges. Custom punctuation replaces default punctuation; underscore has its own flag. Direction uses actual resolved run metadata; both orientations use scalar units. Undefined flag bits throw ArgumentOutOfRangeException. Queries prepare shape and return cold snapshots; prepared values remain reusable. See the [complete span/word contract](../components/text-server.md#span-font-replacement-and-shaped-word-ranges).

GetHexCodeBoxSize/DrawHexCodeBox require no font. Their valid Unicode scalar selects two/four/six digits; every signed size rounds to a pixel scale of at least one. Zero keeps measured advance and emits no primitives after normal canvas, geometry and color validation. See [the measurement/drawing contract](../components/text-server.md#font-free-hexadecimal-replacement) for baseline, scale, trailing space, clipping, errors and verification.

## Owned font sources

CreateFont/CreateFontLinkedVariation return stable owned font RIDs. FontGetResource supplies a borrowed runtime Font for the existing typed consumers; FreeRID or projection disposal retires that entry. Links share source/config/cache and retain separate spacing/baseline. The complete FontGet/Set/Render/Draw/Remove/Clear family, real texture identities, typed FontSizeCacheInfo and native FontGlyphContours are specified in [the font-cache contract](../components/text-server.md#owned-font-sources-and-caches). Runtime views are reconstructed from persistent FontFile assets. Ordinary-resource source RIDs, complete glyph/run snapshots and external providers retain separate prerequisites.

## Font-cache method reference

| Signature | Purpose |
| --- | --- |
| `public Electron2D.RID CreateFont()` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.RID CreateFontLinkedVariation(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontClearGlyphs(Electron2D.RID fontRID, Electron2D.Vector2i size)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontClearKerningMap(Electron2D.RID fontRID, System.Int32 size)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontClearSizeCache(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontClearSystemFallbackCache()` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontClearTextures(Electron2D.RID fontRID, Electron2D.Vector2i size)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontDrawGlyph(Electron2D.RID fontRID, Electron2D.CanvasItem canvas, System.Int32 size, Electron2D.Vector2 position, System.Int32 index, System.Nullable<Electron2D.Color> color = null, System.Single oversampling = 0f)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontDrawGlyphOutline(Electron2D.RID fontRID, Electron2D.CanvasItem canvas, System.Int32 size, System.Int32 outlineSize, Electron2D.Vector2 position, System.Int32 index, System.Nullable<Electron2D.Color> color = null, System.Single oversampling = 0f)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.FontAntialiasing FontGetAntialiasing(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Single FontGetAscent(Electron2D.RID fontRID, System.Int32 size)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Single FontGetBaselineOffset(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetCharFromGlyphIndex(Electron2D.RID fontRID, System.Int32 size, System.Int32 glyphIndex)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Single FontGetDescent(Electron2D.RID fontRID, System.Int32 size)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Boolean FontGetDisableEmbeddedBitmaps(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Single FontGetEmbolden(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetFaceCount(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetFaceIndex(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetFixedSize(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.FixedSizeScaleMode FontGetFixedSizeScaleMode(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Boolean FontGetGenerateMIPMaps(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.Vector2 FontGetGlyphAdvance(Electron2D.RID fontRID, System.Int32 size, System.Int32 glyph)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.FontGlyphContours FontGetGlyphContours(Electron2D.RID fontRID, System.Int32 size, System.Int32 index)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetGlyphIndex(Electron2D.RID fontRID, System.Int32 size, System.Int32 character, System.Int32 variationSelector)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32[] FontGetGlyphList(Electron2D.RID fontRID, Electron2D.Vector2i size)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.Vector2 FontGetGlyphOffset(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.Vector2 FontGetGlyphSize(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetGlyphTextureIndex(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.RID FontGetGlyphTextureRID(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.Vector2 FontGetGlyphTextureSize(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.Rect2 FontGetGlyphUVRect(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.FontHinting FontGetHinting(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Boolean FontGetKeepRoundingRemainders(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.Vector2 FontGetKerning(Electron2D.RID fontRID, System.Int32 size, Electron2D.Vector2i glyphPair)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.Vector2i[] FontGetKerningList(Electron2D.RID fontRID, System.Int32 size)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Boolean FontGetLanguageSupportOverride(Electron2D.RID fontRID, System.String language)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.String[] FontGetLanguageSupportOverrides(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetMSDFPixelRange(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetMSDFSize(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.String FontGetName(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Collections.Generic.Dictionary<System.String, System.Collections.Generic.Dictionary<System.String, System.String>> FontGetOTNameStrings(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Collections.Generic.Dictionary<System.String, System.Int32> FontGetOpenTypeFeatureOverrides(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Single FontGetOversampling(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.Color[] FontGetPaletteColors(Electron2D.RID fontRID, System.Int32 index)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetPaletteCount(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.Color[] FontGetPaletteCustomColors(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.String FontGetPaletteName(Electron2D.RID fontRID, System.Int32 index)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.Font FontGetResource(Electron2D.RID fontRID)` | Returns the borrowed typed runtime font. |
| `public System.Single FontGetScale(Electron2D.RID fontRID, System.Int32 size)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Boolean FontGetScriptSupportOverride(Electron2D.RID fontRID, System.String script)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.String[] FontGetScriptSupportOverrides(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.FontSizeCacheInfo[] FontGetSizeCacheInfo(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.Vector2i[] FontGetSizeCacheList(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetSpacing(Electron2D.RID fontRID, Electron2D.TextSpacingType spacing)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetStretch(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.FontStyle FontGetStyle(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.String FontGetStyleName(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.FontSubpixelPositioning FontGetSubpixelPositioning(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.String FontGetSupportedChars(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32[] FontGetSupportedGlyphs(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetTextureCount(Electron2D.RID fontRID, Electron2D.Vector2i size)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.Image FontGetTextureImage(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 textureIndex)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32[] FontGetTextureOffsets(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 textureIndex)` | Executes the shared source/cache operation; see the owning contract. |
| `public Electron2D.Transform FontGetTransform(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Single FontGetUnderlinePosition(Electron2D.RID fontRID, System.Int32 size)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Single FontGetUnderlineThickness(Electron2D.RID fontRID, System.Int32 size)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetUsedPalette(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Collections.Generic.Dictionary<System.UInt32, System.Single> FontGetVariationCoordinates(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Int32 FontGetWeight(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Boolean FontHasChar(Electron2D.RID fontRID, System.Int32 character)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Boolean FontIsAllowSystemFallback(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Boolean FontIsForceAutohinter(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Boolean FontIsLanguageSupported(Electron2D.RID fontRID, System.String language)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Boolean FontIsModulateColorGlyphs(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Boolean FontIsMultichannelSignedDistanceField(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Boolean FontIsScriptSupported(Electron2D.RID fontRID, System.String script)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontRemoveGlyph(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontRemoveKerning(Electron2D.RID fontRID, System.Int32 size, Electron2D.Vector2i glyphPair)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontRemoveLanguageSupportOverride(Electron2D.RID fontRID, System.String language)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontRemoveScriptSupportOverride(Electron2D.RID fontRID, System.String script)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontRemoveSizeCache(Electron2D.RID fontRID, Electron2D.Vector2i size)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontRemoveTexture(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 textureIndex)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontRenderGlyph(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 index)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontRenderRange(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 start, System.Int32 end)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetAllowSystemFallback(Electron2D.RID fontRID, System.Boolean value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetAntialiasing(Electron2D.RID fontRID, Electron2D.FontAntialiasing value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetAscent(Electron2D.RID fontRID, System.Int32 size, System.Single value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetBaselineOffset(Electron2D.RID fontRID, System.Single value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetData(Electron2D.RID fontRID, System.Byte[] data)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetDescent(Electron2D.RID fontRID, System.Int32 size, System.Single value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetDisableEmbeddedBitmaps(Electron2D.RID fontRID, System.Boolean value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetEmbolden(Electron2D.RID fontRID, System.Single value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetFaceIndex(Electron2D.RID fontRID, System.Int32 value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetFixedSize(Electron2D.RID fontRID, System.Int32 value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetFixedSizeScaleMode(Electron2D.RID fontRID, Electron2D.FixedSizeScaleMode value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetForceAutohinter(Electron2D.RID fontRID, System.Boolean value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetGenerateMIPMaps(Electron2D.RID fontRID, System.Boolean value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetGlyphAdvance(Electron2D.RID fontRID, System.Int32 size, System.Int32 glyph, Electron2D.Vector2 value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetGlyphOffset(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph, Electron2D.Vector2 value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetGlyphSize(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph, Electron2D.Vector2 value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetGlyphTextureIndex(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph, System.Int32 textureIndex)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetGlyphUVRect(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 glyph, Electron2D.Rect2 value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetHinting(Electron2D.RID fontRID, Electron2D.FontHinting value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetKeepRoundingRemainders(Electron2D.RID fontRID, System.Boolean value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetKerning(Electron2D.RID fontRID, System.Int32 size, Electron2D.Vector2i glyphPair, Electron2D.Vector2 value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetLanguageSupportOverride(Electron2D.RID fontRID, System.String language, System.Boolean supported)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetMSDFPixelRange(Electron2D.RID fontRID, System.Int32 value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetMSDFSize(Electron2D.RID fontRID, System.Int32 value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetModulateColorGlyphs(Electron2D.RID fontRID, System.Boolean value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetMultichannelSignedDistanceField(Electron2D.RID fontRID, System.Boolean value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetName(Electron2D.RID fontRID, System.String value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetOpenTypeFeatureOverrides(Electron2D.RID fontRID, System.Collections.Generic.Dictionary<System.String, System.Int32> value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetOversampling(Electron2D.RID fontRID, System.Single value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetPaletteCustomColors(Electron2D.RID fontRID, Electron2D.Color[] value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetScale(Electron2D.RID fontRID, System.Int32 size, System.Single value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetScriptSupportOverride(Electron2D.RID fontRID, System.String script, System.Boolean supported)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetSpacing(Electron2D.RID fontRID, Electron2D.TextSpacingType spacing, System.Int32 value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetStretch(Electron2D.RID fontRID, System.Int32 value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetStyle(Electron2D.RID fontRID, Electron2D.FontStyle value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetStyleName(Electron2D.RID fontRID, System.String value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetSubpixelPositioning(Electron2D.RID fontRID, Electron2D.FontSubpixelPositioning value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetTextureImage(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 textureIndex, Electron2D.Image image)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetTextureOffsets(Electron2D.RID fontRID, Electron2D.Vector2i size, System.Int32 textureIndex, System.Int32[] offsets)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetTransform(Electron2D.RID fontRID, Electron2D.Transform value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetUnderlinePosition(Electron2D.RID fontRID, System.Int32 size, System.Single value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetUnderlineThickness(Electron2D.RID fontRID, System.Int32 size, System.Single value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetUsedPalette(Electron2D.RID fontRID, System.Int32 value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetVariationCoordinates(Electron2D.RID fontRID, System.Collections.Generic.Dictionary<System.UInt32, System.Single> value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Void FontSetWeight(Electron2D.RID fontRID, System.Int32 value)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Collections.Generic.Dictionary<System.UInt32, Electron2D.FontFeatureInfo> FontSupportedFeatureList(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |
| `public System.Collections.Generic.Dictionary<System.UInt32, Electron2D.FontVariationAxis> FontSupportedVariationList(Electron2D.RID fontRID)` | Executes the shared source/cache operation; see the owning contract. |

## Resource glyphs and visual runs

FontGetResource accepts actual Font.GetRID/GetRIDs and automatic fallback identities as borrowed sources. Disposing a resource projection preserves its source; FreeRID rejects it. Service-created/link projections retain their own owned lifetime. All existing font cache/query/draw methods operate on the actual resource backend. See [source ownership](../components/text-server.md#resource-sources-glyph-snapshots-and-visual-runs).

| Member | Result |
| --- | --- |
| `TextGlyph[] ShapedTextGetGlyphs(RID shaped)` | Independent complete visual raw glyph snapshot, unaffected by trimming. |
| `TextGlyph[] ShapedTextSortLogical(RID shaped)` | Independent stable logical-order snapshot, preserving visual storage. |
| `TextGlyph[] ShapedTextGetEllipsisGlyphs(RID shaped)` | Current display ellipsis only, with -1 scalar/span sentinels. |
| `int ShapedTextGetEllipsisGlyphCount(RID shaped)` | Current ellipsis record count; repeats are not expanded. |
| `int ShapedTextGetObjectGlyph(RID shaped, TextObjectKey key)` | Buffer-local visual index, or -1 for missing/partial objects. |
| `int ShapedGetRunCount(RID shaped)` | Visual run count; zero for an empty buffer. |
| `string ShapedGetRunText(RID shaped, int index)` | Copied original run text in logical order. |
| `Vector2i ShapedGetRunRange(RID shaped, int index)` | Half-open global scalar range. |
| `Vector2i ShapedGetRunGlyphRange(RID shaped, int index)` | Inclusive first/last cluster-leader indices. Last Count covers continuations. |
| `RID ShapedGetRunFontRID(RID shaped, int index)` | Actual borrowed font source, or empty for an object/missing replacement. |
| `int ShapedGetRunFontSize(RID shaped, int index)` | Actual logical size; zero for objects. |
| `string ShapedGetRunLanguage(RID shaped, int index)` | Authored span language, empty for objects. |
| `TextDirection ShapedGetRunDirection(RID shaped, int index)` | Resolved LTR or RTL. |
| `TextObjectKey? ShapedGetRunObject(RID shaped, int index)` | Borrowed inline token, null for ordinary text. |

Run getters reject absent indices. Adjacent visual leaders join only when their span/source/size/direction agree. Substrings retain original span/global coordinates through their source lease; paragraph line views use their current raw/display workspace. Scalar/value run queries reuse storage. Arrays and copied run strings may allocate. TextGlyph source identities stay borrowed and may retire after a query.

## Unicode security and support database

See [Unicode security and support data](../components/text-server.md#unicode-security-and-embedded-support-data) for script policy, limits, ownership and verification.

| Signature | Contract |
| --- | --- |
| `int IsConfusable(string text, ReadOnlySpan<string> dictionary)` | First equal case-sensitive Unicode skeleton index, or -1. |
| `bool SpoofCheck(string text)` | True when Unicode security checks flag the text. |
| `byte[] GetSupportData()` | Independent exact copy of the embedded database. |
| `string GetSupportDataFilename()` | TextBreak78.dat export name. |
| `string GetSupportDataInfo()` | ICU/Unicode versions and database roles. |
| `bool LoadSupportData(string filename)` | Initializes the immutable embedded database; external path is unused in this profile. |
| `bool SaveSupportData(string filename)` | Writes and flushes exact database bytes; file access failure returns false. |
| `bool IsLocaleUsingSupportData(string locale)` | Whether word/line breaking uses dictionary support for this locale. |

Internal scalar rectangles and copied font-run metadata also feed [native accessibility text](AccessibilityServer.md#native-text). This integration retains the existing buffer/font ownership and Unicode scalar contract; it adds no public text-server API.


## Capability and structured-text queries

| Signature | Purpose |
| --- | --- |
| `public Electron2D.TextServer.Feature GetFeatures()` | Returns the implementation's supported capability mask. |
| `public System.Boolean HasFeature(Electron2D.TextServer.Feature feature)` | Tests one defined text capability. |
| `public Electron2D.TextBIDIRange[] ParseStructuredText(Electron2D.StructuredTextParser parser, System.Collections.Generic.IReadOnlyList<System.String> options, System.String text)` | Parses independently directed logical contexts for structured text. |
