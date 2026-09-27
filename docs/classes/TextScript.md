# TextScript

Last updated: 2026-09-27

**Visibility:** internal · **Source:** [TextScript.cs](../../src/Servers/Text/TextScript.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Maps Unicode script identities and Script_Extensions to ISO 15924 tags used by HarfBuzz. Common and inherited characters adopt a compatible run script only when their explicit extension data permits it. The pinned Unicode corpus and multilingual shaping tests validate the boundary.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
