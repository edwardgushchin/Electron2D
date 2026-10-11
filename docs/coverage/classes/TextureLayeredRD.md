# TextureLayeredRD API coverage

Last updated: 2026-10-11

Godot source: [doc/classes/TextureLayeredRD.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TextureLayeredRD.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [TextureLayered](TextureLayered.md). Electron2D type: [`public abstract class Electron2D.TextureLayeredRD`](../../classes/TextureLayeredRD.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class TextureLayeredRD`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TextureLayeredRD.xml) | [`public abstract class Electron2D.TextureLayeredRD`](../../classes/TextureLayeredRD.md) | Partial | ADR 0028: retained-device ordinary/array float, signed and unsigned sampled images execute native direct/atlas/proxy and GLSL/DXC-HLSL material descriptors. Signedness/shape/default/override/reload checks and 8/16/32-bit raw-valued float32 mip snapshots execute; lossless integer bits remain in device byte readback. IntegerCanvasTests verifies real Linux/Vulkan pixels and 24 warm plus 32 active frames with zero owner-thread managed bytes. Integer light/normal/specular/mask composition still requires scalar-aware composition shaders; foreign platforms and native allocator behavior remain unverified. |
| [`property RID texture_rd_rid`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TextureLayeredRD.xml) | [`public Electron2D.RID TextureRDRID { get; set; }`](../../classes/TextureLayeredRD.md) | Partial | ADR 0028: retained-device ordinary/array float, signed and unsigned sampled images execute native direct/atlas/proxy and GLSL/DXC-HLSL material descriptors. Signedness/shape/default/override/reload checks and 8/16/32-bit raw-valued float32 mip snapshots execute; lossless integer bits remain in device byte readback. IntegerCanvasTests verifies real Linux/Vulkan pixels and 24 warm plus 32 active frames with zero owner-thread managed bytes. Integer light/normal/specular/mask composition still requires scalar-aware composition shaders; foreign platforms and native allocator behavior remain unverified. |
