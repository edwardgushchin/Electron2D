# TextGlyph

Last updated: 2026-10-20

**Declaration:** `public readonly record struct TextGlyph`

**Source:** [TextGlyph.cs](../../src/Servers/Text/TextGlyph.cs)

A caller-owned value snapshot from TextServer's full visual/logical or display ellipsis glyph queries. It owns no native memory or font. Borrowed font identities may change configuration or retire after the query.

| Member | Meaning |
| --- | --- |
| `int Start`, `int End` | Inclusive start/exclusive end in global Unicode scalar units; -1 for ellipsis. |
| `int Repeat` | Draw repetitions at Advance; zero retains an inactive justification opportunity. |
| `int Count` | Number of records in a cluster, set only on its leading glyph; continuations use zero. |
| `TextGraphemeFlags Flags` | Full leader classification and native joining flags. Continuations retain their own joining flags. |
| `Vector2 Offset` | Baseline-relative logical pixels with positive Y down. |
| `float Advance` | Advance per repeat on the buffer's horizontal or vertical axis. |
| `RID FontRID` | Actual borrowed resource/fallback/linked source; empty for objects, missing boxes and synthetic advance-only records. |
| `int FontSize` | Logical font size; zero for objects. |
| `int Index` | Native glyph index, or Unicode scalar for a missing-glyph box. |
| `int SpanIndex` | Original interleaved authored text/object span index; -1 for ellipsis. |

The positional constructor, standard value equality/operators and Deconstruct copy these eleven existing result roles. ShapedTextGetGlyphs retains raw geometry after trim. ShapedTextSortLogical creates a separate ordered snapshot; leader counts and repeated/virtual records are preserved. Ellipsis glyphs use the display geometry. See [TextServer](TextServer.md) for run boundaries and [the component contract](../components/text-server.md#resource-sources-glyph-snapshots-and-visual-runs) for lifetime and ordering.
