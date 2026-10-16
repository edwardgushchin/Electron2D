# CompressedTextureArray API coverage

Last updated: 2026-10-16

Godot source: [doc/classes/CompressedTexture2DArray.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompressedTexture2DArray.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [CompressedTextureLayered](CompressedTextureLayered.md). Electron2D type: [`public sealed class Electron2D.CompressedTextureArray`](../../classes/CompressedTextureArray.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CompressedTexture2DArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompressedTexture2DArray.xml) | [`public sealed class Electron2D.CompressedTextureArray`](../../classes/CompressedTextureArray.md) | Implemented | ADRs 0004/0013/0014/0028/0039: executable array-only GSTL resource with bounded raw/PNG/WebP and legacy/KTX2 Basis LDR/HDR channel-role decoding, complete homogeneous mips, atomic Load/LoadPath and stable resource RID, independent reads/copies, typed cache/Replace/worker loading and embedded source archives. Independent pixel oracles and Linux Wayland GPU layer/mip texel fetches, HDR/readback/replacement lifetime and warmed frames verify the consumer; Compatibility explicitly rejects custom shader use. Shared ETC1/ETC2/EAC/ASTC decoding now prepares every channel/HDR/mip role; independent public file/resource/archive tests and actual GPU array sampling/readback verify those source layouts. R16I/Rg16I/Rgb16I/Rgba16I now execute raw unsigned RGBA16 prepared/native upload with exact source lanes and every mip, ordinary/array typed sampler defaults/overrides and reload, imports/worker/cache/copies/archives, GPU GLSL/HLSL/server texel fetch and exact layered readback, source update/replacement/device-write lifetime and zero owner-thread managed bytes over 64 active frames after 24 warmup. Integer filtering is nearest; Compatibility rejects integer drawing explicitly. Native totals, foreign platforms and user acceptance remain unverified. |
