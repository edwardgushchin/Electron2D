# CompressedArraySource

Last updated: 2026-10-16

Internal static reader in [CompressedArraySource.cs](../../src/Core/IO/CompressedArraySource.cs).
Read accepts an asset path or bounded bytes and returns a validated immutable
LayeredTexturePixels snapshot. Its private Reader ref struct advances bounded
little-endian spans and rejects truncated/oversized payloads. No span escapes.
ReadImage restores raw/PNG/WebP/Basis layer state and exact complete mips.
MaximumBytes is 64 MiB; source/upload storage shares the existing array budget.
Every layer is disposed after copying, and publication belongs to the resource.
See [container validation and tests](../components/compressed-arrays.md).
