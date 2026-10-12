# FontSupportPolicy

Last updated: 2026-10-12

**Declaration:** `internal sealed class FontSupportPolicy` · **Source:** [FontSupportPolicy.cs](../../src/Servers/Text/FontSupportPolicy.cs) · **Component:** [Text](../components/text.md)

## Responsibility and invariants

Owns immutable ordinal language/script dictionary snapshots and precompiled ISO15924 tags. Empty snapshots are shared safely. With copies only the changed dictionary; Get queries exact values, Keys copies insertion-order keys, and Priority resolves script before language. Explicit overrides precede wildcard entries; language defaults true and script defaults to native metadata. Unsupported scripts remain the last fallback tier.

## Storage and lifetime

Save/Load encode versioned strict UTF8 dictionaries in the stored FontFile byte-array descriptor. Keys are NUL-free and bounded to 65536 UTF16 units; complete data is bounded to 64 MiB. Decode checks encoded lengths before allocation, version, counts, boolean values, duplicate keys and trailing bytes before publication. No native/resource ownership is introduced. FontFile serializes snapshot publication and native-data propagation before notifications.

## Verification

FontSupportTests covers pure queries, snapshot independence, concurrent mutations, callback failures, malformed data, actual fallback advances and fresh-process archives. The native host verifies both existing renderers and prepared active/idle managed allocation limits. Foreign and native allocation gates remain separate.
