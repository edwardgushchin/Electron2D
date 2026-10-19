# TextServer

Last updated: 2026-10-20

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [TextServer.cs](../../src/Servers/Text/TextServer.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class Electron2D.TextServer`

## Description

A retained implementation object discovered through TextServerManager. Each shaped buffer belongs to its creating thread. CreateShapedText/duplicate/substring IDs are strongly retained until FreeRID; TextLine and TextParagraph IDs remain borrowed and cannot be freed here. Text, ordered Font lists and OpenType selectors are copied; caller fonts, keys and typed metadata remain borrowed. Prepared queries and drawing reuse storage; authoring and returned snapshots are cold. Width/size are ceiled display metrics; ascent/descent remain fractional. Object rectangles and drawing are baseline-relative. Active work rejects reentry and mutation. See the complete [buffer contract](../components/text-server.md) for source limits, paragraph line detachment, range/index units, readiness and remaining family dependencies.

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
| `public Electron2D.RID CreateShapedText(Electron2D.TextDirection direction = Auto, Electron2D.TextOrientation orientation = Horizontal)` | Creates an empty owned text buffer. |
| `public System.Void FreeRID(Electron2D.RID rid)` | Releases an owned buffer, preserving borrowed text-object lifetimes. |
| `public abstract System.String GetName()` | Returns the stable name of this text implementation. |
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

ValidateDisposal rejects disposal of the retained object. Native source ownership, font-cache/extension gaps and verification limits are described in [the component contract](../components/text-server.md).

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
