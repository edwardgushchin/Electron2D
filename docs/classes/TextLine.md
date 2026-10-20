# TextLine

Last updated: 2026-10-20

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [TextLine.cs](../../src/Servers/Text/TextLine.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class TextLine : ElectronObject`

## Description

A retained standalone single line built from borrowed font/size/language segments and identity-keyed inline objects. It owns its shape/display workspaces and a stable borrowed buffer RID; it owns no caller font, token, metadata payload or canvas. The creating thread owns all access. Native/layout work rejects mutation, disposal and reentry. Font generations invalidate coherent shape state.

Default policies are Left/Auto/Horizontal, width minus one, ellipsis character, Kashida plus WordBound flags, TrimEllipsis, PreserveInvalid true and PreserveControl false. Nonpositive finite widths are unlimited. Invalid typed inputs throw before authoring; empty text succeeds without adding a span, duplicate/missing object keys return false. Source expansion is bounded to 1,048,576 Unicode scalars and 64 MiB UTF-16.

Display width/size use ceiled trimmed metrics; ascent/descent are fractional. Full-buffer carets and inline rectangles survive display trimming. Object rectangles include display alignment; hit coordinates are baseline advance-axis offsets. Supplementary characters count as one scalar. One object may represent several replacement scalars with one geometric advance. Empty and inline-only buffers need no caller font. Clear removes source/objects/BIDI ranges while retaining identity, tabs and policies; Duplicate owns a distinct buffer and copies all authored/policy state while sharing borrowed font/key/payload values.

## Example

```csharp
using var line = new TextLine();
line.AddString("Hello ", font, 22, "en", 23);
var icon = new TextObjectKey();
line.AddObject(icon, new(18, 20));
line.AddString("world", font, 30);
// In this canvas item's drawing callback:
line.Draw(canvas, new(12, 14));
```

## Properties

| Member | Description |
| --- | --- |
| Alignment | Left/Center/Right/Fill on the advance axis. |
| Direction | Automatic, LTR, RTL or inherited automatic policy. |
| Orientation | Horizontal or Vertical. |
| EllipsisChar | First Unicode scalar of custom text; empty uses the default. |
| Flags | Defined justification flag bits. |
| PreserveControl | Displays otherwise nonprinting controls. |
| PreserveInvalid | Displays missing-glyph boxes. |
| TextOverrunBehavior | Display trimming/ellipsis policy. |
| Width | Finite available advance width; nonpositive is unlimited. |
| SpanCount | Nonempty text-segment count, excluding inline objects. |

## Methods

| Member | Description |
| --- | --- |
| TextLine() | Empty retained buffer. |
| AddString / AddString with typed metadata | Append copied text and borrowed font/style/data. |
| TryGetSpanMetadata | Read the exact original metadata type by text-segment index. |
| AddObject / ResizeObject | Author unique token geometry, replacement length and signed baseline. |
| HasObject / GetObjects / GetObjectRect | Complete authored objects and caller-owned key arrays. |
| SetBIDIOverride | Copy contiguous, nonoverlapping ranges covering current source; clear before appending. |
| TabAlign | Copy repeating positive interval lengths. |
| GetRID | Stable borrowed shape/display identity, consumed by drawing. |
| GetInferredDirection | First-paragraph inferred direction. |
| GetLineAscent / GetLineDescent | Complete fractional cross-axis metrics. |
| GetLineWidth / GetSize | Ceiled trimmed display dimensions. |
| GetLineUnderlinePosition / GetLineUnderlineThickness | Maximum authored-font underline metrics. |
| HitTest | Complete-buffer Unicode scalar caret on either axis. |
| Draw / DrawOutline | Top-left drawing into a recording CanvasItem, with real oversampling/advance clipping. |
| Duplicate / Clear | Independent buffer copy and content reset. |

## Protected integration

GetPropertyDescriptors exposes live nonstored policy descriptors. ValidateMutation/ValidateDisposal protect the owner and active native work; Dispose unregisters the identity and retires owned neutral-font/layout state. Runtime lines are reconstructed from authored source/font data by scene consumers. See [text objects](../components/text-objects.md) and [ADR 0107](../decisions/text-objects.md#adr-0107).

Related paragraph preparation shares the typed span builder. All captured font read scopes must remain current before the shape/display pair is published; cross-font replacement retries preparation within the existing 64-attempt bound. [TextParagraph](TextParagraph.md) supplies wrapping and independent line/dropcap consumers.

## Verification

TextLineTests completes outstanding collections and drains finalizers after fixture preparation, before the unchanged 128 warmup and 128 measured metric/caret cycles. AllocationWarmupCheck exercises that same preparation helper with a controlled second finalization pass. The interval retains all eight queries and its strict zero-byte owner-thread assertion; failures report the observed delta. Collection setup stays outside the interval and production hosts retain their ordinary GC policy. See the [allocation measurement contract](../maintaining.md#native-enet-source-and-interoperability-checks).

The borrowed identities interoperate with [TextServer](TextServer.md) through the same owner, scalar source and native layouts. The interface cannot free them. [The buffer contract](../components/text-server.md) defines source edits, line-view detachment and reflow retirement.

[Owned TextServer substrings](../components/text-server.md#line-wrapping-and-owned-substrings) retain complete original clusters and shared source-version leases independently of parent retirement. The line owner uses a private reference-counted source snapshot; the layout caches logical glyph order and shares BIDI run resolution for actual substring drawing. Language-specific native break opportunities preserve adjacent equal-language context.

The [TextServer formatting family](../components/text-server.md#fitting-spacing-and-overrun) adjusts retained raw advances and independent display trimming through this same owner. Fitting/trim/ellipsis preserve substring context and parent identity; changed spacing/punctuation detaches authored data. Repeated Arabic/word fitting, vertical/RTL trimming and borrowed paragraph line metrics are covered by the formatting profiles.

Span font replacement keeps source/language/metadata and publishes a new ordered source adapter before reshaping; unused adapter leases retire while caller fonts remain borrowed. Duplicate/source-version leases preserve prior consumers. Successful replacement detaches substrings and updates paragraph line consumers through the existing owner contract. See [span/word integration](../components/text-server.md#span-font-replacement-and-shaped-word-ranges).

## Glyph and run source mapping

TextServer queries use an internal reusable GlyphSpan map of interleaved text/object spans. Substring source leases retain original indexes and global scalar coordinates; authored detachment rebuilds the local map. Copied glyphs retain concrete resource/fallback/linked source identities. Runtime scalar run queries reuse scratch storage; returned glyph arrays and run text remain cold snapshots. See [TextGlyph](TextGlyph.md) and [TextServer](TextServer.md).
