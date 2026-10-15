# TextLine

Last updated: 2026-10-15

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
