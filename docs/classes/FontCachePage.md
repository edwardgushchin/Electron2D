# FontCachePage

Last updated: 2026-10-07

**Visibility:** internal · **Source:** [FontCache.cs](../../src/Servers/Text/FontCache.cs) · **Component:** [Bitmap fonts](../components/bitmap-fonts.md)

## Responsibilities and invariants

Owns immutable copied RGBA bytes, dimensions and copied shelf tuples, with a lazy retained ordinary ImageTexture. Retirement drops residency without invalidating already recorded immutable image snapshots. FontData serializes native realization; FontFile serializes publication and callbacks run after resource locks. Source/cache disposal preserves active reads.

## Integration and verification

[FontCacheTests](../../tests/Electron2D.Tests/FontCacheTests.cs) exercises authored/dynamic/import/packing/rollback/storage and prepared native rendering on current Linux GPU/compatibility. Native allocator counts and foreign execution remain unverified. [ADR 0046](../decisions/rendering.md#adr-0046) owns the integration.
