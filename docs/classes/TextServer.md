# TextServer

Last updated: 2026-10-19

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [TextServer.cs](../../src/Servers/Text/TextServer.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class Electron2D.TextServer`

## Description

A retained implementation object discovered through TextServerManager. Each shaped buffer belongs to its creating thread. CreateShapedText/duplicate IDs are strongly retained until FreeRID; TextLine and TextParagraph IDs remain borrowed and cannot be freed here. Text, ordered Font lists and OpenType selectors are copied; caller fonts, keys and typed metadata remain borrowed. Prepared queries and drawing reuse storage; authoring and returned snapshots are cold. Width/size are ceiled display metrics; ascent/descent remain fractional. Object rectangles and drawing are baseline-relative. Active work rejects reentry and mutation. See the complete [buffer contract](../components/text-server.md) for source limits, paragraph line detachment, range/index units, readiness and remaining family dependencies.

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

## Protected integration

ValidateDisposal rejects disposal of the retained object. Native source ownership, font-cache/extension gaps and verification limits are described in [the component contract](../components/text-server.md).
