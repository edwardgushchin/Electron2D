# File-backed image arrays

Last updated: 2026-10-16

## Scope and public resources

`CompressedTextureLayered : TextureLayered` is the abstract file-backed branch;
`CompressedTextureArray` is its concrete independent-array resource. It loads
`.ctexarray` assets through `Load`, the stored `LoadPath` setter, or ResourceLoader.
It does not inherit the mutable `CreateFromImages`/`UpdateLayer` surface.
Ordinary texture drawing and cube/volume sampling retain their separate boundaries.

## Container and pixels

The bounded GSTL reader accepts versions zero and one, array role zero, a positive
layer count, and an exact complete file. Each layer stores a base image or a full
mip chain. Raw pixels retain their source format and bytes; PNG/WebP payloads
decode each declared mip and restore its declared source format. Basis payloads
select legacy `.basis` or KTX2 through their high layout bit. The six channel roles
are RG, RGB, RGBA, RA-as-RG, R and HDR RGB; the latter retains half precision and
values above one. These imported pixels retain their encoded transfer values;
the public Image KTX RGB8 sRGB conversion is a separate input contract.

Every layer must agree in dimensions, source format and mip configuration.
Unknown encodings, malformed or partial levels, trailing bytes and heterogeneous
layers fail before publication. Cube/volume roles and sampling formats without
an integrated decoder fail explicitly. The four coverage rows are now Partial rather than
Blocked: shared ETC1/ETC2/EAC/ASTC decoding now executes. Their remaining pixel-format dependency is integer-sampled source formats. Streaming/size hints do not discard mips:
this API has no requested-size argument. Files are bounded to 64 MiB; source and
prepared upload pixels share the existing 256 MiB array budget. Device limits
remain enforced by the renderer.

## Publication and ownership

Parsing, decoding and snapshot construction finish before replacing resource
state. A failed load preserves pixels, LoadPath and RID. Successful publication
notifies PropertyListChanged before Changed outside the resource lock; observer
exceptions retain the committed state. GetLayerData returns independent images.
Metadata and prepared renderer snapshots require no repeated decoding/copying.
Individual calls synchronize; callers coordinate compound edits and disposal.

Full file replacement retains the resource RID and replaces native allocation
identity. Borrowed RenderingDevice wrappers expire when old storage retires.
Direct Load gives the renderer a temporary source label; ResourcePath assignments,
including clearing, update that label. LoadPath remains the last successful input.
Duplication shares immutable snapshots and has independent later publication.
ResourceLoader discovery, weak caching, Replace and threaded loading reuse the
ordinary format extension pipeline. Typed archives embed original layer pixels,
mips and LoadPath and restore them without reopening the imported source file.

## Native boundary

The selected immutable KTX/Basis codec from ADRs 0012/0039 supplies both containers.
The fourth private export, `e2d_basis_create`, shares existing level-copy and owner
destruction with KTX. It validates the exact legacy file size, checksums, flat role,
slice/mip metadata and conservative 64 MiB expansion before transcoding. A private
owned buffer places legacy slices at aligned offsets and updates only private
container offsets/checksums; this avoids native UASTC block alignment assumptions
without changing input bytes or codec source. The materialized container also
stays within 64 MiB. A shared
once-only initializer covers concurrent first use from both entry points. The
exact-preimage build adapter changes initialization ownership and existing error
propagation; codec algorithms and the fetched archive remain unchanged.

## Example

Requires an existing imported array file and a compiled shader with an array sampler.

```csharp
using var layers = ResourceLoader.Load<CompressedTextureArray>("res://terrain.ctexarray");
using var material = new ShaderMaterial { Shader = shader };
material.SetShaderLayeredParameter("layers", layers);
using var pixels = layers.GetLayerData(0); // Caller-owned copy including all mips.
```

## Verification and limits

[Managed tests](../../tests/Electron2D.Tests/CompressedArrayTests.cs) exercise raw,
PNG/WebP, independent legacy/KTX2 Basis LDR/HDR oracles, every channel role,
complete odd-sized mips, malformed/truncated/heterogeneous input, publication,
events, cache replacement, worker loading, copying and fresh-process embedded archives after deleting the original source.
[Native tests](../../tests/Electron2D.Tests/CompressedArrayTests.Rendering.cs)
verify actual GPU layer/mip texel fetches, native readback, HDR before framebuffer
clamping, replacement/borrowed lifetime, shader defaults/overrides and cleanup.
Named canvas samplers keep the accepted linear/base-level/clamp policy; explicit
texel fetches exercise stored mips without adding configurable named samplers.
Compatibility rejects custom shader use. Foreign execution, native allocation
totals, integer sampling, public refreshed bundles and human
acceptance require their own evidence.

The Linux Wayland GPU run completes 24 warmup and 64 active frames with zero
owner-thread managed bytes. Fresh source, ProjectReference and NuGet consumers
execute legacy Basis LDR/HDR and archives with native compilers blocked. A separate
NativeAOT package consumer executes the same public workflow. The self-contained
JIT publish passes the four-export native ABI audit and 72-ELF/92-notice audit.
Shared and single-archive native consumers pass the nine KTX and three legacy
pixel oracles, all truncations and deterministic corruptions. Legacy mutations
include repaired checksums and unaligned borrowed pointers. ASan/UBSan/LSan pass
that corpus, and eight concurrent threads create 128 mixed KTX/Basis owners at
cold initialization. These are local verification results under ADR 0021.

See [image arrays](texture-arrays.md), [KTX input](image-ktx.md),
[ADR 0028](../decisions/rendering.md#adr-0028) and
[ADR 0039](../decisions/resources.md#adr-0039).
