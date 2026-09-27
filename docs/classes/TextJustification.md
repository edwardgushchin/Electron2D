# TextJustification

Last updated: 2026-09-27

**Visibility:** internal · **Source:** [TextJustification.cs](../../src/Servers/Text/TextJustification.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Finds Arabic elongation opportunities from the pinned joining-group/type data and shaped context. It implements the joining rules used by text layout without adding a runtime ICU dependency. Independent C/ICU oracle checks cover 3,342,336 scalar/context cases; opportunity finding alone does not prove every consumer fill policy.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
