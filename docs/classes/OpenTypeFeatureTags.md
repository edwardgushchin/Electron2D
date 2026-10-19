# OpenTypeFeatureTags

Last updated: 2026-10-20

**Visibility:** internal · **Source:** [OpenTypeFeatureTags.cs](../../src/Servers/Text/OpenTypeFeatureTags.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Converts registered human-readable feature aliases and custom tag spellings into native four-byte OpenType tags. FontFile keeps caller-owned typed snapshots; negative feature values remain stored but do not enable a native feature. FontFileTests checks aliases, unknown/custom spellings and shaping effects.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. TextServer.NameToTag/TagToName use the same typed uint tags and canonical alias tables as real shaping. Reverse names are cached once, including 99 character variants and 20 stylistic sets; the italic axis name wins over the duplicate feature alias. Unknown reverse values produce custom_ plus Latin-1 bytes through the first zero. TextServerUnicodeTests exercises aliases, byte edges, padding/truncation, numbered variants and warmed zero-byte queries; native Unicode hosts consume a round-tripped kerning name in real shaped drawing. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.

The primary realized face supplies OpenType metadata through `GetSupportedFeatureList()` and `GetOTNameStrings()`. The feature snapshot contains numeric tags and typed optional labels, boolean/integer selection and hidden flags. GSUB is read before GPOS, whose duplicate descriptions win. Name strings are copied by native language spelling and standard role; unknown IDs retain `unknown_N` keys. These cold snapshots allocate independently and omit fallback metadata or authored family overrides. Feature labels retain the tool locale sampled during native realization; source replacement or cache retirement rebuilds metadata.
