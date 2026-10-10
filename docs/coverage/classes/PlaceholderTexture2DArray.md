# PlaceholderTexture2DArray API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/PlaceholderTexture2DArray.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PlaceholderTexture2DArray.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [PlaceholderTextureLayered](PlaceholderTextureLayered.md). Electron2D type: [`public sealed class Electron2D.PlaceholderTextureArray`](../../classes/PlaceholderTextureArray.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class PlaceholderTexture2DArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PlaceholderTexture2DArray.xml) | [`public sealed class Electron2D.PlaceholderTextureArray`](../../classes/PlaceholderTextureArray.md) | Implemented | ADRs 0004/0013/0014/0028: separate typed layered resources and array-only identity execute copied homogeneous image layers, stable borrowed/owned RIDs, atomic compatible updates, custom producer snapshots, metadata placeholders, typed default/override descriptors, reload shape migration, duplication and bounded resource archives. Native Wayland GPU checks both HLSL/GLSL, all two-layer mip data, compatible allocation reuse, HDR reallocation, RID proxies/replacement and 64 active frames after 24 warmup with zero owner-thread managed bytes. Compatibility rejects shader use explicitly; native allocations and foreign/human acceptance remain unverified. |
