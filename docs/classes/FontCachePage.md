# FontCachePage

Last updated: 2026-10-12

**Visibility:** internal · **Source:** [FontCache.cs](../../src/Servers/Text/FontCache.cs) · **Component:** [Bitmap fonts](../components/bitmap-fonts.md)

## Responsibilities and invariants

Owns immutable copied RGBA bytes, dimensions and copied shelf tuples, with a lazy retained ordinary ImageTexture. Retirement drops residency without invalidating already recorded immutable image snapshots. FontData serializes native realization; FontFile serializes publication and callbacks run after resource locks. Source/cache disposal preserves active reads.

## Integration and verification

[FontCacheTests](../../tests/Electron2D.Tests/FontCacheTests.cs) exercises authored/dynamic/import/packing/rollback/storage and prepared native rendering on current Linux GPU/compatibility. Native allocator counts and foreign execution remain unverified. [ADR 0046](../decisions/rendering.md#adr-0046) owns the integration.

## Authored and generated page ownership

Font-cache version two stores explicit authored-page ownership and native packing permission; version one conservatively treats decoded pages as authored. Raster-policy publication discards generated records and unreferenced automatic pages while preserving authored images and glyph references. Native packing uses separate/reusable automatic slots for protected authored glyph content and continues to honor explicitly supplied free shelf metadata. MIP generation occurs when an owned texture is realized; immutable source pixels and retired recorded snapshots remain valid. FontRasterPolicyTests covers mixed-page switching, legacy decoding and corrupt ownership rejection.

Generated native atlas pages carry one raster factor and pack only glyphs with that factor. Level retirement removes only matching generated pages; authored pages remain independent.
Explicit authored packing pages still accept native glyphs at caller-supplied shelf positions. Their authored pixels and ownership survive automatic level retirement; only matching generated glyph records retire.
