# FontThread

Last updated: 2026-09-27

**Visibility:** internal · **Source:** [FontThread.cs](../../src/Servers/Text/FontThread.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Serial synchronous worker for native font operations. One reusable request slot avoids per-call jobs; exceptions return to the caller with their captured stack. Native ownership never crosses the worker thread. Completed work clears captured delegates before waiting, so the worker does not intentionally retain a font. This process service is not a public scheduler.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
