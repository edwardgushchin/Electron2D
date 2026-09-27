# OpenTypeFeatureTags

Last updated: 2026-09-27

**Visibility:** internal · **Source:** [OpenTypeFeatureTags.cs](../../src/Servers/Text/OpenTypeFeatureTags.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Converts registered human-readable feature aliases and custom tag spellings into native four-byte OpenType tags. FontFile keeps caller-owned typed snapshots; negative feature values remain stored but do not enable a native feature. FontFileTests checks aliases, unknown/custom spellings and shaping effects.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
