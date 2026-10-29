# TextParagraph

Last updated: 2026-10-29

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [TextParagraph.cs](../../src/Servers/Text/TextParagraph.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class TextParagraph : ElectronObject`

## Description

A retained standalone paragraph built from borrowed Font/size/language spans, exact typed metadata and reference-identity TextObjectKey values. It shares the FreeType/HarfBuzz/TextLayout backend and authored span builder with [TextLine](TextLine.md); no additional renderer or universal payload dispatch is involved. It owns full/display multiline workspaces and an independent dropcap. Fonts, tokens, metadata and canvases remain caller-owned. The creating thread owns every access. Active native work rejects query, mutation and disposal reentry; failure releases read scopes so a later operation can retry.

Full-string and dropcap RIDs remain stable until disposal. Each prepared line has a borrowed RID tied to that reflow generation; subsequent content, policy or font changes retire it. Indexed queries and DrawLine include hidden lines. MaxLinesVisible controls paragraph size/drawing and the wrapped visible tail's overrun; negative values mean all lines, zero hides every ordinary line. An empty paragraph has one zero-sized line. Character ranges are half-open Unicode scalar ranges; CRLF consumes two scalars and supplementary characters consume one. Mandatory separators remain in a line range unless end trimming removes them. The full buffer size ignores wrapping and dropcap.

Line width and size are ceiled; ascent, descent, underline metrics and baseline spacing retain fractional values. An inline object may cover several replacement scalars with one geometry/advance. GetLineObjectRect returns its complete, untrimmed rectangle in paragraph coordinates, including preceding lines, paragraph alignment and dropcap flow. GetLineObjects returns a fresh array of borrowed keys. HitTest uses line-local advance coordinates and paragraph cross coordinates, independently of paragraph alignment/dropcap shifts; it retains hidden-line carets. Negative cross coordinates return the range start, cross gaps/beyond the paragraph return the range end. Vertical orientation swaps advance/cross axes.

Draw/DrawOutline apply paragraph alignment, dropcap flow, visibility and clipping. DrawLine/DrawLineOutline start at the requested line's own top-left, without paragraph offsets, clipping or visibility restriction. Dropcap-only drawing uses the same physical leading-margin origin as the paragraph draw. Signed finite margins and spacing are retained; do not assume non-overlapping lines with negative spacing. A dropcap reserves its ceiled size plus leading and trailing margins. GetDropcapLines counts complete covered line extents; the next partial line still flows around it. GetDropcapSize includes margins even after Clear; ClearDropcap resets them.

Defaults are Left, Auto, Horizontal, Width -1, mandatory word breaks, zero line spacing, unlimited visible lines, NoTrimming, ellipsis `…`, empty custom punctuation, PreserveInvalid true and PreserveControl false. JustificationFlags initially contains Kashida, WordBound, SkipLastLine and DoNotSkipSingleLine (163). CustomPunctuation replaces the Unicode punctuation classification used for justification. Nonpositive width disables width-dependent wrapping/trimming. Invalid typed inputs throw; duplicate/missing object edits return false; empty AddString succeeds without a span. Source expansion is bounded to 1,048,576 scalars and 64 MiB UTF-16. Clear removes content/objects/BIDI/dropcap while retaining policies/tabs/margins; Duplicate copies every setting into independent buffers and shares borrowed values.

Wrapping and line drawing share the selected text implementation, including opaque sources, producer line breaks and independent formatted line buffers. Reflow and disposal retire private buffers. See [external implementations](../components/text-server.md#external-implementations).

## Example

```csharp
using var paragraph = new TextParagraph { Width = 320, LineSpacing = 4 };
paragraph.SetDropcap("T", font, 56, new(new(0, 0), new(8, 0)));
paragraph.AddString("ext in several wrapped lines.", font, 22, "en", 47);
var icon = new TextObjectKey();
paragraph.AddObject(icon, new(18, 18));
// In the supplied canvas item's Draw callback:
paragraph.Draw(canvas, new(12, 12));
```

The example requires a live borrowed `font` and a recording `canvas`. Store text/font resources in scenes and reconstruct this transient object after loading; paragraph buffers and delegates are not serialized.


## Constructors

| Signature | Contract |
| --- | --- |
| [`public TextParagraph()`](#textparagraph) | Creates an empty thread-owned paragraph with mandatory word wrapping and unlimited visible lines. |

## Properties

| Signature | Contract |
| --- | --- |
| [`public HorizontalAlignment Alignment`](#alignment) | Gets or sets Advance-axis paragraph alignment; initially Left. |
| [`public TextLineBreakFlags BreakFlags`](#breakflags) | Gets or sets Mandatory and width-dependent breaks; initially Mandatory and WordBound. |
| [`public string CustomPunctuation`](#custompunctuation) | Gets or sets Replacement punctuation classification for justification; empty uses Unicode punctuation. |
| [`public string EllipsisChar`](#ellipsischar) | Gets or sets First Unicode scalar used for ellipsis; empty uses the standard ellipsis. |
| [`public TextJustificationFlags JustificationFlags`](#justificationflags) | Gets or sets Line justification policies; initially Kashida, WordBound, SkipLastLine and DoNotSkipSingleLine. |
| [`public float LineSpacing`](#linespacing) | Gets or sets Finite signed pixels between lines; initially zero. |
| [`public int MaxLinesVisible`](#maxlinesvisible) | Gets or sets Maximum visible lines; any negative value means all lines. |
| [`public TextOverrunBehavior TextOverrunBehavior`](#textoverrunbehavior) | Gets or sets Display trimming policy; initially NoTrimming. |
| [`public float Width`](#width) | Gets or sets Finite advance-axis width; nonpositive disables width-dependent wrapping and trimming. |
| [`public TextDirection Direction`](#direction) | Gets or sets text direction for text and dropcap. |
| [`public TextOrientation Orientation`](#orientation) | Gets or sets text orientation for text and dropcap. |
| [`public bool PreserveControl`](#preservecontrol) | Gets or sets control-box display for text and dropcap. |
| [`public bool PreserveInvalid`](#preserveinvalid) | Gets or sets invalid-glyph display for text and dropcap. |
| [`public int SpanCount`](#spancount) | Gets authored text-span count, excluding inline objects and empty additions. |

## Methods

| Signature | Contract |
| --- | --- |
| [`public bool AddString(string text, Font font, int fontSize, string language = "")`](#addstring) | Appends a copied text segment with a borrowed font. |
| [`public bool AddString<T>(string text, Font font, int fontSize, string language, T metadata)`](#addstring-metadata) | Appends a segment retaining exact typed metadata. |
| [`public bool TryGetSpanMetadata<T>(int spanIndex, out T metadata)`](#trygetspanmetadata-metadata) | Reads metadata under its exact original type. |
| [`public bool AddObject(TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, int length = 1, float baseline = 0)`](#addobject) | Appends one inline object with reference identity and one advance. |
| [`public bool ResizeObject(TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, float baseline = 0)`](#resizeobject) | Changes existing object geometry without changing its scalar range. |
| [`public bool HasObject(TextObjectKey key)`](#hasobject) | Tests authored inline identity, including hidden lines. |
| [`public void SetBIDIOverride(ReadOnlySpan<TextBIDIRange> ranges)`](#setbidioverride) | Copies explicit contiguous BIDI ranges over the current scalar buffer; empty clears them. |
| [`public void TabAlign(ReadOnlySpan<float> tabStops)`](#tabalign) | Copies positive repeating tab intervals. |
| [`public bool SetDropcap(string text, Font font, int fontSize, Rect2 dropcapMargins = default, string language = "")`](#setdropcap) | Replaces the independent dropcap text without changing its RID. |
| [`public void ClearDropcap()`](#cleardropcap) | Clears dropcap content and margins, preserving its RID and paragraph settings. |
| [`public void Clear()`](#clear) | Clears authored text, objects, BIDI and dropcap content; policies, tabs, margins and full/dropcap RIDs remain. |
| [`public TextParagraph Duplicate()`](#duplicate) | Copies source and every paragraph policy into independent owned buffers. |
| [`public RID GetRID()`](#getrid) | Returns the stable borrowed full-string buffer identity. |
| [`public RID GetDropcapRID()`](#getdropcaprid) | Returns the stable borrowed independent dropcap identity. |
| [`public RID GetLineRID(int line)`](#getlinerid) | Returns a borrowed shaped-line sub-buffer identity. |
| [`public Vector2i GetRange()`](#getrange) | Returns the complete half-open Unicode scalar range. |
| [`public Vector2i GetLineRange(int line)`](#getlinerange) | Returns a line's half-open scalar range, including mandatory separators unless trimmed. |
| [`public int GetLineCount()`](#getlinecount) | Returns total shaped line count independently of visibility. |
| [`public TextDirection GetInferredDirection()`](#getinferreddirection) | Returns inferred full-string writing direction. |
| [`public Vector2 GetNonWrappedSize()`](#getnonwrappedsize) | Returns the full buffer size without line breaks, visibility or dropcap. |
| [`public Vector2 GetSize()`](#getsize) | Returns the visible paragraph bounding size, including spacing and dropcap reservation. |
| [`public Vector2 GetLineSize(int line)`](#getlinesize) | Returns one display line's ceiled bounding size. |
| [`public float GetLineWidth(int line)`](#getlinewidth) | Returns ceiled display advance width, or height in vertical orientation. |
| [`public float GetLineAscent(int line)`](#getlineascent) | Returns fractional line ascent. |
| [`public float GetLineDescent(int line)`](#getlinedescent) | Returns fractional line descent. |
| [`public float GetLineUnderlinePosition(int line)`](#getlineunderlineposition) | Returns maximum underline offset across a line's authored fonts. |
| [`public float GetLineUnderlineThickness(int line)`](#getlineunderlinethickness) | Returns maximum underline thickness across a line's authored fonts. |
| [`public Vector2 GetDropcapSize()`](#getdropcapsize) | Returns the dropcap bounding size plus leading and trailing margins. |
| [`public int GetDropcapLines()`](#getdropcaplines) | Returns the number of complete line extents covered by dropcap reservation. |
| [`public TextObjectKey[] GetLineObjects(int line)`](#getlineobjects) | Returns borrowed inline tokens in one complete line as a caller-owned snapshot. |
| [`public Rect2 GetLineObjectRect(int line, TextObjectKey key)`](#getlineobjectrect) | Returns full inline geometry in paragraph-top-left coordinates, including alignment and preceding lines. |
| [`public int HitTest(Vector2 coords)`](#hittest) | Returns a full-buffer scalar caret near line-local advance and paragraph cross coordinates. |
| [`public void Draw(CanvasItem canvas, Vector2 position, Color? color = null, Color? dropcapColor = null, float oversampling = 0)`](#draw) | Draws visible lines and dropcap from the paragraph top-left. |
| [`public void DrawOutline(CanvasItem canvas, Vector2 position, int outlineSize = 1, Color? color = null, Color? dropcapColor = null, float oversampling = 0)`](#drawoutline) | Draws visible line and dropcap outlines. |
| [`public void DrawLine(CanvasItem canvas, Vector2 position, int line, Color? color = null, float oversampling = 0)`](#drawline) | Draws an indexed line from its own top-left, without paragraph offsets or visibility restriction. |
| [`public void DrawLineOutline(CanvasItem canvas, Vector2 position, int line, int outlineSize = 1, Color? color = null, float oversampling = 0)`](#drawlineoutline) | Draws one line outline from its own top-left. |
| [`public void DrawDropcap(CanvasItem canvas, Vector2 position, Color? color = null, float oversampling = 0)`](#drawdropcap) | Draws the independent dropcap, including its leading margins. |
| [`public void DrawDropcapOutline(CanvasItem canvas, Vector2 position, int outlineSize = 1, Color? color = null, float oversampling = 0)`](#drawdropcapoutline) | Draws only the dropcap outline. |

## Constructor Descriptions

<a id="textparagraph"></a>
### TextParagraph

`public TextParagraph()`

Creates an empty thread-owned paragraph with mandatory word wrapping and unlimited visible lines.

## Property Descriptions

<a id="alignment"></a>
### Alignment

`public HorizontalAlignment Alignment`

Gets or sets Advance-axis paragraph alignment; initially Left.

Value: The current policy.

<a id="breakflags"></a>
### BreakFlags

`public TextLineBreakFlags BreakFlags`

Gets or sets Mandatory and width-dependent breaks; initially Mandatory and WordBound.

Value: The current policy.

<a id="custompunctuation"></a>
### CustomPunctuation

`public string CustomPunctuation`

Gets or sets Replacement punctuation classification for justification; empty uses Unicode punctuation.

Value: The current policy.

<a id="ellipsischar"></a>
### EllipsisChar

`public string EllipsisChar`

Gets or sets First Unicode scalar used for ellipsis; empty uses the standard ellipsis.

Value: The current policy.

<a id="justificationflags"></a>
### JustificationFlags

`public TextJustificationFlags JustificationFlags`

Gets or sets Line justification policies; initially Kashida, WordBound, SkipLastLine and DoNotSkipSingleLine.

Value: The current policy.

<a id="linespacing"></a>
### LineSpacing

`public float LineSpacing`

Gets or sets Finite signed pixels between lines; initially zero.

Value: The current policy.

<a id="maxlinesvisible"></a>
### MaxLinesVisible

`public int MaxLinesVisible`

Gets or sets Maximum visible lines; any negative value means all lines.

Value: The current policy.

<a id="textoverrunbehavior"></a>
### TextOverrunBehavior

`public TextOverrunBehavior TextOverrunBehavior`

Gets or sets Display trimming policy; initially NoTrimming.

Value: The current policy.

<a id="width"></a>
### Width

`public float Width`

Gets or sets Finite advance-axis width; nonpositive disables width-dependent wrapping and trimming.

Value: The current policy.

<a id="direction"></a>
### Direction

`public TextDirection Direction`

Gets or sets text direction for text and dropcap.

Value: Initially TextDirection.Auto.

<a id="orientation"></a>
### Orientation

`public TextOrientation Orientation`

Gets or sets text orientation for text and dropcap.

Value: Initially TextOrientation.Horizontal.

<a id="preservecontrol"></a>
### PreserveControl

`public bool PreserveControl`

Gets or sets control-box display for text and dropcap.

Value: Initially false.

<a id="preserveinvalid"></a>
### PreserveInvalid

`public bool PreserveInvalid`

Gets or sets invalid-glyph display for text and dropcap.

Value: Initially true.

<a id="spancount"></a>
### SpanCount

`public int SpanCount`

Gets authored text-span count, excluding inline objects and empty additions.

Value: Nonnegative count.

## Method Descriptions

<a id="addstring"></a>
### AddString

`public bool AddString(string text, Font font, int fontSize, string language = "")`

Appends a copied text segment with a borrowed font.

`text`: Copied text; empty succeeds without a segment.; `font`: Live borrowed font.; `fontSize`: Positive logical size.; `language`: Language tag, or empty..

Returns: True on successful authoring.

<a id="addstring-metadata"></a>
### AddString

`public bool AddString<T>(string text, Font font, int fontSize, string language, T metadata)`

Appends a segment retaining exact typed metadata.

`T`: Borrowed metadata type.; `text`: Copied text.; `font`: Borrowed font.; `fontSize`: Positive logical size.; `language`: Language tag.; `metadata`: Borrowed value..

Returns: True on success.

<a id="trygetspanmetadata-metadata"></a>
### TryGetSpanMetadata

`public bool TryGetSpanMetadata<T>(int spanIndex, out T metadata)`

Reads metadata under its exact original type.

`T`: Original type.; `spanIndex`: Text span index.; `metadata`: Borrowed result or default..

Returns: Whether the type matches.

<a id="addobject"></a>
### AddObject

`public bool AddObject(TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, int length = 1, float baseline = 0)`

Appends one inline object with reference identity and one advance.

`key`: Unique borrowed token.; `size`: Finite nonnegative dimensions.; `inlineAlign`: Two-axis alignment.; `length`: Positive replacement-scalar count.; `baseline`: Finite signed baseline..

Returns: False for a duplicate token.

<a id="resizeobject"></a>
### ResizeObject

`public bool ResizeObject(TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, float baseline = 0)`

Changes existing object geometry without changing its scalar range.

`key`: Borrowed token.; `size`: Finite nonnegative size.; `inlineAlign`: Two-axis alignment.; `baseline`: Finite signed baseline..

Returns: False for a missing token.

<a id="hasobject"></a>
### HasObject

`public bool HasObject(TextObjectKey key)`

Tests authored inline identity, including hidden lines.

`key`: Borrowed token..

Returns: Whether present.

<a id="setbidioverride"></a>
### SetBIDIOverride

`public void SetBIDIOverride(ReadOnlySpan<TextBIDIRange> ranges)`

Copies explicit contiguous BIDI ranges over the current scalar buffer; empty clears them.

`ranges`: Full nonoverlapping ranges..

<a id="tabalign"></a>
### TabAlign

`public void TabAlign(ReadOnlySpan<float> tabStops)`

Copies positive repeating tab intervals.

`tabStops`: Finite positive intervals; empty restores default tabs..

<a id="setdropcap"></a>
### SetDropcap

`public bool SetDropcap(string text, Font font, int fontSize, Rect2 dropcapMargins = default, string language = "")`

Replaces the independent dropcap text without changing its RID.

`text`: Copied text, bounded like ordinary spans.; `font`: Live borrowed font.; `fontSize`: Positive logical size.; `dropcapMargins`: Finite signed leading and trailing margins.; `language`: Language tag..

Returns: True after successful authoring.

<a id="cleardropcap"></a>
### ClearDropcap

`public void ClearDropcap()`

Clears dropcap content and margins, preserving its RID and paragraph settings.

<a id="clear"></a>
### Clear

`public void Clear()`

Clears authored text, objects, BIDI and dropcap content; policies, tabs, margins and full/dropcap RIDs remain.

<a id="duplicate"></a>
### Duplicate

`public TextParagraph Duplicate()`

Copies source and every paragraph policy into independent owned buffers.

Returns: A caller-owned duplicate sharing borrowed fonts/tokens/metadata.

<a id="getrid"></a>
### GetRID

`public RID GetRID()`

Returns the stable borrowed full-string buffer identity.

Returns: Valid until disposal.

<a id="getdropcaprid"></a>
### GetDropcapRID

`public RID GetDropcapRID()`

Returns the stable borrowed independent dropcap identity.

Returns: Valid even when empty, until disposal.

<a id="getlinerid"></a>
### GetLineRID

`public RID GetLineRID(int line)`

Returns a borrowed shaped-line sub-buffer identity.

`line`: Index including hidden lines..

Returns: Valid until reflow or disposal.

<a id="getrange"></a>
### GetRange

`public Vector2i GetRange()`

Returns the complete half-open Unicode scalar range.

Returns: Zero to authored scalar count.

<a id="getlinerange"></a>
### GetLineRange

`public Vector2i GetLineRange(int line)`

Returns a line's half-open scalar range, including mandatory separators unless trimmed.

`line`: Index including hidden lines..

Returns: Scalar start and end.

<a id="getlinecount"></a>
### GetLineCount

`public int GetLineCount()`

Returns total shaped line count independently of visibility.

Returns: One zero-sized line for empty text.

<a id="getinferreddirection"></a>
### GetInferredDirection

`public TextDirection GetInferredDirection()`

Returns inferred full-string writing direction.

Returns: LTR or RTL.

<a id="getnonwrappedsize"></a>
### GetNonWrappedSize

`public Vector2 GetNonWrappedSize()`

Returns the full buffer size without line breaks, visibility or dropcap.

Returns: Ceiled dimensions.

<a id="getsize"></a>
### GetSize

`public Vector2 GetSize()`

Returns the visible paragraph bounding size, including spacing and dropcap reservation.

Returns: Finite dimensions; signed spacing/margins may reduce them.

<a id="getlinesize"></a>
### GetLineSize

`public Vector2 GetLineSize(int line)`

Returns one display line's ceiled bounding size.

`line`: Index including hidden lines..

Returns: Ceiled dimensions with the selected orientation.

<a id="getlinewidth"></a>
### GetLineWidth

`public float GetLineWidth(int line)`

Returns ceiled display advance width, or height in vertical orientation.

`line`: Index including hidden lines..

Returns: Advance pixels.

<a id="getlineascent"></a>
### GetLineAscent

`public float GetLineAscent(int line)`

Returns fractional line ascent.

`line`: Index including hidden lines..

Returns: Pixels before the baseline.

<a id="getlinedescent"></a>
### GetLineDescent

`public float GetLineDescent(int line)`

Returns fractional line descent.

`line`: Index including hidden lines..

Returns: Pixels after the baseline.

<a id="getlineunderlineposition"></a>
### GetLineUnderlinePosition

`public float GetLineUnderlinePosition(int line)`

Returns maximum underline offset across a line's authored fonts.

`line`: Index including hidden lines..

Returns: Fractional pixels.

<a id="getlineunderlinethickness"></a>
### GetLineUnderlineThickness

`public float GetLineUnderlineThickness(int line)`

Returns maximum underline thickness across a line's authored fonts.

`line`: Index including hidden lines..

Returns: Fractional pixels.

<a id="getdropcapsize"></a>
### GetDropcapSize

`public Vector2 GetDropcapSize()`

Returns the dropcap bounding size plus leading and trailing margins.

Returns: Finite signed dimensions.

<a id="getdropcaplines"></a>
### GetDropcapLines

`public int GetDropcapLines()`

Returns the number of complete line extents covered by dropcap reservation.

Returns: Zero when no line is completely covered.

<a id="getlineobjects"></a>
### GetLineObjects

`public TextObjectKey[] GetLineObjects(int line)`

Returns borrowed inline tokens in one complete line as a caller-owned snapshot.

`line`: Index including hidden lines..

Returns: Tokens in authored order, including trimmed objects.

<a id="getlineobjectrect"></a>
### GetLineObjectRect

`public Rect2 GetLineObjectRect(int line, TextObjectKey key)`

Returns full inline geometry in paragraph-top-left coordinates, including alignment and preceding lines.

`line`: Index including hidden lines.; `key`: Borrowed token..

Returns: Zero for an object absent from this line.

<a id="hittest"></a>
### HitTest

`public int HitTest(Vector2 coords)`

Returns a full-buffer scalar caret near line-local advance and paragraph cross coordinates.

`coords`: Finite coordinates; advance ignores paragraph alignment/dropcap offset..

Returns: Scalar position; cross gaps/beyond the paragraph return the range end.

<a id="draw"></a>
### Draw

`public void Draw(CanvasItem canvas, Vector2 position, Color? color = null, Color? dropcapColor = null, float oversampling = 0)`

Draws visible lines and dropcap from the paragraph top-left.

`canvas`: Recording canvas.; `position`: Finite origin.; `color`: Text modulation, or white.; `dropcapColor`: Dropcap modulation, or white.; `oversampling`: Raster scale, nonpositive automatic..

<a id="drawoutline"></a>
### DrawOutline

`public void DrawOutline(CanvasItem canvas, Vector2 position, int outlineSize = 1, Color? color = null, Color? dropcapColor = null, float oversampling = 0)`

Draws visible line and dropcap outlines.

`canvas`: Recording canvas.; `position`: Finite origin.; `outlineSize`: Nonpositive uses the unexpanded outline pass.; `color`: Text modulation.; `dropcapColor`: Dropcap modulation.; `oversampling`: Raster scale, nonpositive automatic..

<a id="drawline"></a>
### DrawLine

`public void DrawLine(CanvasItem canvas, Vector2 position, int line, Color? color = null, float oversampling = 0)`

Draws an indexed line from its own top-left, without paragraph offsets or visibility restriction.

`canvas`: Recording canvas.; `position`: Line-local origin.; `line`: Any valid index.; `color`: Modulation.; `oversampling`: Raster scale..

<a id="drawlineoutline"></a>
### DrawLineOutline

`public void DrawLineOutline(CanvasItem canvas, Vector2 position, int line, int outlineSize = 1, Color? color = null, float oversampling = 0)`

Draws one line outline from its own top-left.

`canvas`: Recording canvas.; `position`: Line-local origin.; `line`: Any valid index.; `outlineSize`: Outline pixels.; `color`: Modulation.; `oversampling`: Raster scale..

<a id="drawdropcap"></a>
### DrawDropcap

`public void DrawDropcap(CanvasItem canvas, Vector2 position, Color? color = null, float oversampling = 0)`

Draws the independent dropcap, including its leading margins.

`canvas`: Recording canvas.; `position`: Paragraph top-left.; `color`: Modulation.; `oversampling`: Raster scale..

<a id="drawdropcapoutline"></a>
### DrawDropcapOutline

`public void DrawDropcapOutline(CanvasItem canvas, Vector2 position, int outlineSize = 1, Color? color = null, float oversampling = 0)`

Draws only the dropcap outline.

`canvas`: Recording canvas.; `position`: Paragraph top-left.; `outlineSize`: Outline pixels.; `color`: Modulation.; `oversampling`: Raster scale..

## Lifecycle, errors and protected extension points

Dispose releases only owned buffers and registered identities, never borrowed fonts/tokens/metadata. Repeated disposal follows ElectronObject. The protected GetPropertyDescriptors override supplies live nonstored typed settings; ValidateMutation and ValidateDisposal enforce owner/reentry guards, and Dispose(bool) releases paragraph-owned state. Numeric notifications and typed tooling inherit ElectronObject. Invalid line/span indices throw ArgumentOutOfRangeException; null text/font/key/language throws ArgumentNullException; invalid flags, sizes, geometry and nonfinite input throw typed argument exceptions. Unrepresentable derived geometry throws OverflowException. Disposed fonts fail explicitly. Rendering requires a live recording CanvasItem under the ordinary Font drawing contract.

## Dependencies and verification

The borrowed-font invalidation fixture uses `AAAA AAAA`, preserving its two four-glyph words and separator while selecting glyphs actually present in the variable test font. Its spacing increase therefore measures the borrowed font's generation, independently of system fallback. The strict width increase and 128 warmup/128 measured zero-byte query cycles remain unchanged. The macOS workflow also selects this fixture separately before its complete suite; target acceptance still requires those actual logs.

[Text objects](../components/text-objects.md) and [ADR 0107](../decisions/text-objects.md#adr-0107) own the typed buffer, lifecycle and coordinate contract. TextParagraphTests exercises the complete query/authoring family and native preparation, including Unicode ranges, wrapping/visibility, inline/dropcap, duplicates, both axes, font generations and failure/reentry. TextParagraphRenderingTests exercises actual paragraph/line/dropcap draw variants, outline, vertical text, clipping, live reflow and the warmed full-frame allocation gate on each selected backend. These local gates do not establish foreign platform execution, native allocator totals, large-input performance or owner acceptance. Independent package/fresh-process scene reconstruction has its own evidence.

The borrowed identities interoperate with [TextServer](TextServer.md) through the same owner, scalar source and native layouts. The interface cannot free them. [The buffer contract](../components/text-server.md) defines source edits, line-view detachment and reflow retirement.

The [shaped-buffer editing](../components/text-server.md#caret-selection-and-navigation) path retains line-local caret/selection geometry and global scalar origins. Reflow refreshes font dependencies after retiring detached adapters before the next coherence check; character-end caches reset with layout preparation.

[Owned TextServer substrings](../components/text-server.md#line-wrapping-and-owned-substrings) retain complete original clusters and shared source-version leases independently of parent retirement. The line owner uses a private reference-counted source snapshot; the layout caches logical glyph order and shares BIDI run resolution for actual substring drawing. Language-specific native break opportunities preserve adjacent equal-language context.

The [TextServer formatting family](../components/text-server.md#fitting-spacing-and-overrun) adjusts retained raw advances and independent display trimming through this same owner. Fitting/trim/ellipsis preserve substring context and parent identity; changed spacing/punctuation detaches authored data. Repeated Arabic/word fitting, vertical/RTL trimming and borrowed paragraph line metrics are covered by the formatting profiles.

## Glyph metadata for borrowed lines

TextServer glyph/run queries use each borrowed line's raw geometry and global authored span coordinates. Ellipsis queries select its current display, including a line-specific imperative formatting override. Retired line identities fail through the ordinary owner checks; partial objects/clusters have no fabricated glyph metadata.
