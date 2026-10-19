# TextBIDI

Last updated: 2026-10-20

**Visibility:** internal · **Source:** [TextBIDI.cs](../../src/Servers/Text/TextBIDI.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Unicode paragraph-level resolver over the internal Avalonia BiDi algorithm. It scans embedding/isolate controls conservatively rather than trusting incomplete input hints, and reconciles the preceding-bracket NSM level annotation at the engine boundary. Vendored algorithms remain unchanged. Buffers are reused; exposed levels are per Unicode scalar.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.

Neutral retained buffers use the first authored span language (or tool locale for no authored span) to select their base direction. Detached context slices preserve that choice; TextServerFormattingTests exercises a neutral Arabic-language source.
