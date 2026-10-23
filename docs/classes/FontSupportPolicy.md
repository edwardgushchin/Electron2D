# FontSupportPolicy

Last updated: 2026-10-23

**Declaration:** `internal sealed class FontSupportPolicy` · **Source:** [FontSupportPolicy.cs](../../src/Servers/Text/FontSupportPolicy.cs) · **Component:** [Text](../components/text.md)

## Responsibility and invariants

Owns immutable ordinal language/script dictionary snapshots and precompiled ISO15924 tags. Empty snapshots are shared safely. With copies only the changed dictionary; Get queries exact values, Keys copies insertion-order keys, and Priority resolves script before language. Explicit overrides precede wildcard entries; language defaults true and script defaults to native metadata. Unsupported scripts remain the last fallback tier.

Fallback language keys preserve the first three UTF16 units with underscores
removed. TextLayout supplies three stack characters; dictionary lookup borrows
that span with the ordinal alternate-key API. Long region/script tags therefore
create no per-grapheme substring or replacement string. Exact-case overrides,
wildcards, language/script tier order and authored fallback order are unchanged.

## Storage and lifetime

Save/Load encode versioned strict UTF8 dictionaries in the stored FontFile byte-array descriptor. Keys are NUL-free and bounded to 65536 UTF16 units; complete data is bounded to 64 MiB. Decode checks encoded lengths before allocation, version, counts, boolean values, duplicate keys and trailing bytes before publication. No native/resource ownership is introduced. FontFile serializes snapshot publication and native-data propagation before notifications.

## Verification

FontSupportTests covers pure queries, snapshot independence, concurrent mutations, callback failures, malformed data, actual fallback advances and fresh-process archives. The native host verifies both existing renderers and prepared active/idle managed allocation limits. Foreign and native allocation gates remain separate.
