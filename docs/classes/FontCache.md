# FontCache

Last updated: 2026-10-07

**Visibility:** internal · **Source:** [FontCache.cs](../../src/Servers/Text/FontCache.cs) · **Component:** [Bitmap fonts](../components/bitmap-fonts.md)

## Responsibilities and invariants

Owns copied instance/spacing/fixed-size and indexed size records; matching coordinates and spacing connect file authoring to FontVariation. Published authored changes clone records before native realization. FontData serializes native realization; FontFile serializes publication and callbacks run after resource locks. Source/cache disposal preserves active reads.

## Integration and verification

[FontCacheTests](../../tests/Electron2D.Tests/FontCacheTests.cs) exercises authored/dynamic/import/packing/rollback/storage and prepared native rendering on current Linux GPU/compatibility. Native allocator counts and foreign execution remain unverified. [ADR 0046](../decisions/rendering.md#adr-0046) owns the integration.
