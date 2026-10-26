# TextLayout

Last updated: 2026-10-26

**Visibility:** internal · **Source:** [TextLayout.cs](../../src/Servers/Text/TextLayout.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Reusable layout buffer owned by Font caches or a text consumer. Converts text to Unicode scalars, determines grapheme/word/line boundaries and scripts, resolves fallback faces, shapes contextual runs, applies BiDi order, line breaks, clipping/ellipsis and permitted justification. Character bounds use shaped scalar clusters rather than ink boxes. Font revisions invalidate cached layout. Build leases every source face, repeats at most 64 times when a callback or concurrent replacement changes dependencies, and isolates nested queries from active cache entries. Drawing revalidates the built generation before recording and holds source leases through the complete draw. Retained canvas commands preserve immutable glyph-image snapshots after source retirement.

Regional and script language tags use stack-backed fallback-priority keys across
all ordinary and rich-style graphemes. [FontSupportPolicy](FontSupportPolicy.md)
retains the existing prefix, separator and ordinal matching rules without creating
one managed string per character.

## Integration and verification

Word and locale-specific line boundaries come from the private ICU dictionary iterators; grapheme boundaries and BiDi processing use the pinned managed algorithms. Decoding also caches exact nonprinting classification, so unsupported graphic characters retain hexadecimal fallback boxes while missing control or unassigned glyphs do not acquire a synthetic advance.

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.

## Editing geometry

[TextLayout.Editing.cs](../../src/Servers/Text/TextLayout.Editing.cs) provides shaped-grapheme/word boundaries, dominant input direction, leading/trailing BiDi carets, nearest-column hit testing and visual selection intervals for [LineEdit](LineEdit.md). Cluster advance is subdivided over scalar columns; zero-width controls keep valid boundary positions. Carets are prepared once per build in retained buffers, so a pointer hit is linear instead of rescanning all glyphs per column. Optional PreserveControl emits visible hexadecimal placeholders; optional local clip rectangles crop glyph textures and missing-glyph rectangles before canvas recording, converting logical glyph coordinates to physical oversampled bitmap UVs. LineEditTests verifies these paths alongside native editing/display.

## Support-aware source selection

Scripts are resolved before fallback selection. Primary sources remain first; explicit fallback sources retain authored order within the three language/script support tiers. Full grapheme coverage is attempted before scalar fallback, and script-unsupported sources remain last resort. Emoji presentation prefers color fonts, including explicit VS16, while VS15 retains ordinary text priority. Policy changes invalidate cached layout and dependent native variations; prepared canvas commands reuse glyph storage. The text component records current native pixel and allocation verification.

The [shaped-buffer editing](../components/text-server.md#caret-selection-and-navigation) path retains line-local caret/selection geometry and global scalar origins. Reflow refreshes font dependencies after retiring detached adapters before the next coherence check; character-end caches reset with layout preparation.

The retained character-end and line-local geometry queries are implemented in [TextLayout.ServerEditing.cs](../../src/Servers/Text/TextLayout.ServerEditing.cs).

Glyph direction is retained from the actual resolved shaping run, including line-ending whitespace resets. Caret, selection, scalar hits and rich glyph flags share that retained metadata across wrapped lines and BIDI override contexts.

[Owned TextServer substrings](../components/text-server.md#line-wrapping-and-owned-substrings) retain complete original clusters and shared source-version leases independently of parent retirement. The line owner uses a private reference-counted source snapshot; the layout caches logical glyph order and shares BIDI run resolution for actual substring drawing. Language-specific native break opportunities preserve adjacent equal-language context.

The [TextServer formatting family](../components/text-server.md#fitting-spacing-and-overrun) adjusts retained raw advances and independent display trimming through this same owner. Fitting/trim/ellipsis preserve substring context and parent identity; changed spacing/punctuation detaches authored data. Repeated Arabic/word fitting, vertical/RTL trimming and borrowed paragraph line metrics are covered by the formatting profiles.

Retained text buffers prepare zero-advance word and Arabic elongation opportunities inside the existing coherent native font-read scope. Shaped word ranges traverse logical complete clusters, use the shared typed glyph-property classifier and treat virtual opportunities as points. The same classifier feeds rich callbacks with pinned punctuation, separate underscores, mandatory/soft breaks and valid inline objects. See [span/word integration](../components/text-server.md#span-font-replacement-and-shaped-word-ranges).

## Copied glyph metadata

Internal glyph records retain the actual source RID and an ellipsis discriminator through fitting, slicing and display copying. Virtual word/Kashida records preserve the same source. The private ServerTextRun scratch list groups visual cluster leaders by authored span, source, size and direction without changing layout order. Snapshot arrays expose TextGlyph values, while scalar run queries reuse scratch capacity. Ellipsis queries select only the current display payload and preserve the independent raw geometry.

Internal scalar rectangles and copied font-run metadata also feed [native accessibility text](AccessibilityServer.md#native-text). This integration retains the existing buffer/font ownership and Unicode scalar contract; it adds no public text-server API.
