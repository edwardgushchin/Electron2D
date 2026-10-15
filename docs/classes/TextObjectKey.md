# TextObjectKey

Last updated: 2026-10-15

**Inherits:** —

- **Source:** [TextLine.cs](../../src/Servers/Text/TextLine.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class TextObjectKey`

## Description

A caller-owned reference-identity token for one inline object. `new TextObjectKey()` creates a distinct token. Reuse the same token in TextLine.AddObject, ResizeObject, HasObject and GetObjectRect; GetObjects returns borrowed tokens in authored order. Duplicate keeps token identities while copying geometry. The token owns no native or scene state and requires no disposal. See [TextLine](TextLine.md) and [ADR 0107](../decisions/text-objects.md#adr-0107).
