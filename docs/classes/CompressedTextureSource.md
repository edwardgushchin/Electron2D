# CompressedTextureSource

Last updated: 2026-10-16

Internal static reader in [CompressedTextureSource.cs](../../src/Core/IO/CompressedTextureSource.cs).
ReadTexture accepts a GST2 asset path and returns immutable TexturePixels plus its
logical Vector2i size. ReadArray accepts GSTL and returns LayeredTexturePixels.
Both use the same bounded ReadImage payload reader for raw/PNG/WebP/Basis images
and exact complete mips. The private Reader ref struct advances little-endian
spans, rejects truncated/oversized payloads and never exposes borrowed storage.
MaximumBytes is 64 MiB; source/upload snapshots share the existing 256 MiB budget.
Temporary images are disposed after copying; resource publication happens later.
See [ordinary imports](../components/compressed-textures.md) and
[array imports](../components/compressed-arrays.md).
