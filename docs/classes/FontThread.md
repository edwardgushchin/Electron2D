# FontThread

Last updated: 2026-10-05

**Visibility:** internal · **Source:** [FontThread.cs](../../src/Servers/Text/FontThread.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Serial synchronous owner for native font operations. Threaded runtimes use one worker and reusable request slot; exceptions return with their captured stack and completed work releases captured delegates before waiting. The nonthreaded browser profile captures its execution thread and invokes directly without creating a worker or blocking the event loop. A different owner rejects. Native ownership never crosses that selected thread. This process service is not a public scheduler.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.
