# TextBufferFont

Last updated: 2026-10-20

**Inherits:** [Font](Font.md)

- **Source:** [TextLine.Server.cs](../../src/Servers/Text/TextLine.Server.cs)
- **Declaration:** `internal sealed class TextBufferFont : Font`

An internal adapter for a copied ordered Font list and OpenType overrides. It borrows original native faces, preserves dependency generations, source spacing and fallback shaping features, and owns no independent raster backend. Text buffers share explicit leases across duplicate/sliced authored state; the final lease disposes only the adapter. Compiled per-face features merge current source defaults with copied selectors and retire stale-generation entries. This class is not public font-cache authoring. Construction/lease changes are cold; the prepared layout/draw path uses the existing font read scopes. See [text buffers](../components/text-server.md).

Span replacement installs a new copied source/feature adapter and removes unused old dependencies from the line generation set. The old adapter retires after its last source/snapshot/duplicate lease; replacing it does not dispose caller fonts. Inline geometry owns no font adapter. See [span/word integration](../components/text-server.md#span-font-replacement-and-shaped-word-ranges).
