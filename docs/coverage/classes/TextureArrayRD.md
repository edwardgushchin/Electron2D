# TextureArrayRD API coverage

Last updated: 2026-10-11

Godot source: [doc/classes/Texture2DArrayRD.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2DArrayRD.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [TextureLayeredRD](TextureLayeredRD.md). Electron2D type: [`public sealed class Electron2D.TextureArrayRD`](../../classes/TextureArrayRD.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Texture2DArrayRD`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2DArrayRD.xml) | [`public sealed class Electron2D.TextureArrayRD`](../../classes/TextureArrayRD.md) | Implemented | ADR 0028: retained-device ordinary/array float, signed and unsigned sampled images execute native direct/atlas/proxy and GLSL/DXC-HLSL material descriptors. Signedness/shape/default/override/reload checks and 8/16/32-bit raw-valued float32 mip snapshots execute; lossless integer bits remain in device byte readback. IntegerCanvasTests verifies real Linux/Vulkan pixels and 24 warm plus 32 active frames with zero owner-thread managed bytes. IntegerLightingTests verifies all 27 diffuse/normal/specular scalar combinations against float references, raw-valued signed/unsigned point-light atlas masks and alpha clipping, live clear/free/rebind/resize and zero owner-thread managed bytes. ShaderMaterial light receivers execute automatic SPIR-V composition with validated total native sampler capacity; foreign platforms and native allocator behavior remain unverified. |
