# FontCacheGlyph

Last updated: 2026-10-07

**Visibility:** internal · **Source:** [FontCache.cs](../../src/Servers/Text/FontCache.cs) · **Component:** [Bitmap fonts](../components/bitmap-fonts.md)

## Responsibilities and invariants

Stores logical baseline offset/dimensions, pixel UV rectangle, page index, color classification and authored/native provenance. Missing page references retain advance while drawing no image. FontData serializes native realization; FontFile serializes publication and callbacks run after resource locks. Source/cache disposal preserves active reads.

## Integration and verification

[FontCacheTests](../../tests/Electron2D.Tests/FontCacheTests.cs) exercises authored/dynamic/import/packing/rollback/storage and prepared native rendering on current Linux GPU/compatibility. Native allocator counts and foreign execution remain unverified. [ADR 0046](../decisions/rendering.md#adr-0046) owns the integration.
