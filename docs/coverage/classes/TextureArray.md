# TextureArray API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/Texture2DArray.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2DArray.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [ImageTextureLayered](ImageTextureLayered.md). Electron2D type: [`public sealed class Electron2D.TextureArray`](../../classes/TextureArray.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Texture2DArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2DArray.xml) | [`public sealed class Electron2D.TextureArray`](../../classes/TextureArray.md) | Partial | ADRs 0004/0013/0014/0028: separate typed layered resources and array-only identity execute copied homogeneous image layers, stable borrowed/owned RIDs, atomic compatible updates, custom producer snapshots, metadata placeholders, typed default/override descriptors, reload shape migration, duplication and bounded resource archives. Native Wayland GPU checks both HLSL/GLSL, all two-layer mip data, compatible allocation reuse, HDR reallocation, RID proxies/replacement and 64 active frames after 24 warmup with zero owner-thread managed bytes. Compatibility rejects shader use explicitly; native allocations and foreign/human acceptance remain unverified. Compressed and integer-sampled source formats remain Partial: require operation-specific compressed array upload/decoding and integer sampler reflection/binding over the existing float TexturePixels profile; CPU metadata and float/RGBA storage/sampling execute. |
| [`method create_placeholder() -> Resource`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2DArray.xml) | [`public Electron2D.PlaceholderTextureArray CreatePlaceholder()`](../../classes/TextureArray.md) | Implemented | ADRs 0004/0013/0014/0028: separate typed layered resources and array-only identity execute copied homogeneous image layers, stable borrowed/owned RIDs, atomic compatible updates, custom producer snapshots, metadata placeholders, typed default/override descriptors, reload shape migration, duplication and bounded resource archives. Native Wayland GPU checks both HLSL/GLSL, all two-layer mip data, compatible allocation reuse, HDR reallocation, RID proxies/replacement and 64 active frames after 24 warmup with zero owner-thread managed bytes. Compatibility rejects shader use explicitly; native allocations and foreign/human acceptance remain unverified. |
