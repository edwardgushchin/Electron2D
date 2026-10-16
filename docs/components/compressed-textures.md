# Imported image textures

Last updated: 2026-10-16

## Scope and dependencies

[CompressedTexture](../classes/CompressedTexture.md) directly extends Texture and
loads ordinary `.ctex` image assets through Load, LoadPath or ResourceLoader.
Sprite, TextureRect, direct canvas drawing and ordinary named shader samplers
consume it through the existing public Texture contract. ImageTexture remains
the separately mutable branch. The reader shares image-payload validation and
the selected private codecs with [imported arrays](compressed-arrays.md).

## File and pixel contract

The exact bounded GST2 container accepts versions zero and one. Its positive
logical width/height use signed engine dimensions; the physical image header
stores independent positive sixteen-bit dimensions. One image payload follows:
raw bytes, individually length-prefixed PNG/WebP mips, or length-prefixed Basis.
Base-only and exact full mip chains execute. Unknown modes/versions, truncated
or extra bytes, invalid dimensions, inconsistent encoded mip dimensions and
partial chains fail before publication. Size/streaming/detection hints do not
discard levels or introduce editor callbacks into this runtime API.

Raw formats retain exact source bytes; encoded mip images restore their declared
source format. Legacy Basis and KTX2 use the six imported RG/RGB/RGBA/RA-as-RG/R/HDR
RGB channel roles and preserve encoded transfer values. BC, ETC1/ETC2/EAC and ASTC
source blocks remain original while prepared snapshots supply sampling/opacity.
Signed EAC retains normalized floating-point values; ASTC HDR retains half range.
R16I/Rg16I/Rgb16I/Rgba16I preserve original lanes and mips, using raw unsigned
GPU sampling through prepared RGBA16 storage. See [integer sources](integer-textures.md)
for sampler, missing-channel and Compatibility boundaries.

Source files are capped at 64 MiB. Source plus prepared upload snapshots use the
existing 256 MiB texture-storage budget; codec expansion retains its conservative
64 MiB working limit. Physical texture and driver limits are checked by existing
image preparation and rendering paths. Logical dimensions do not expand pixels.

## Ownership and publication

Preparation finishes before one locked state replacement. Failure preserves the
previous pixels, size, LoadPath and resource RID. Notifications occur after the
lock, PropertyListChanged before Changed; observer failure leaves committed state.
GetImage copies original physical pixels/mips. Metadata and opacity use prepared
snapshots without repeated decoding. HasAlpha reports false for this imported
type while IsPixelOpaque tests actual alpha above 0.1 in clamped logical coordinates.

Reload retains the resource RID and retires replaced native storage; borrowed
RenderingDevice wrappers expire. Direct Load supplies a temporary renderer label
when ResourcePath is empty; visible ResourcePath edits, including clearing, update
that label. LoadPath separately retains the last successful input argument.

The dedicated format loader recognizes ctex independently of array roles. Weak
cache reuse, Replace and worker preparation use ResourceLoader's existing pipeline.
Duplication and scene-local graph copying share immutable snapshots with independent
later publication. Typed resource/scene archives embed source pixels, logical size
and LoadPath, and restore without opening the original `.ctex` file.

## Verification and limits

[Managed tests](../../tests/Electron2D.Tests/CompressedTextureTests.cs) cover separate
logical/physical sizes, raw/PNG/WebP mips, independent legacy/KTX2 Basis LDR/HDR and
thirteen block-codec oracles, actual alpha, every container truncation, oversized
source, invalid headers, atomic failures, events, cache/worker preparation, copies,
scene-local sharing and fresh-process embedded archives after source deletion.
[Native tests](../../tests/Electron2D.Tests/CompressedTextureTests.Rendering.cs)
compare 17 GPU and 13 Compatibility source/oracle pairs across Sprite, TextureRect,
direct/transposed/region drawing and reload/failure replay. GPU also verifies named
sampler defaults/overrides, exact physical full-mip readback and borrowed storage
expiration. BC may retain native compressed upload storage. Both Linux Wayland
backends complete 24 warmup plus 64 active frames with zero owner-thread managed
bytes; captures are independently inspected. Compatibility floating-point driver
limits and custom-shader limits remain explicit.

These are local executable checks under ADR 0021. Foreign platforms, native
allocator totals, public bundle publication and human acceptance require separate
evidence. No texture importer/editor authoring UI or streaming API is implied.
Fresh local NuGet JIT and NativeAOT consumers also execute the public raw/PNG/WebP
and thirteen block-format import workflows, logical size, exact source mips,
duplication and embedded archives. They use the previously verified native bundle;
the image codec binaries match the source-test inputs. This does not refresh or
publish a native bundle and does not claim audio acceptance from image tests.
See [images](images.md), [block decoding](image-block-decoding.md) and ADRs
[0028](../decisions/rendering.md#adr-0028)/[0039](../decisions/resources.md#adr-0039).
