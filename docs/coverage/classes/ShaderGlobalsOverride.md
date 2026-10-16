# ShaderGlobalsOverride API coverage

Last updated: 2026-10-16

Godot source: [doc/classes/ShaderGlobalsOverride.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ShaderGlobalsOverride.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node](Node.md). Electron2D type: [`public class Electron2D.ShaderGlobalsOverride`](../../classes/ShaderGlobalsOverride.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ShaderGlobalsOverride`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ShaderGlobalsOverride.xml) | [`public class Electron2D.ShaderGlobalsOverride`](../../classes/ShaderGlobalsOverride.md) | Implemented | ADRs 0028/0023/0004/0090: executable typed ShaderGlobalsOverride scene authoring for all 25 supported global roles, first actual tree-order priority, observable groups, ancestor reorder/transfer and failed-exit/disposal restoration. Exact portable role/carrier descriptors restore fresh files and borrowed image aliases. ShaderGlobalsOverrideTests checks guards, failed activation/repair and zero-byte prepared updates; GLSL/HLSL run 160 software-Vulkan frames on Linux Wayland/X11, with 12 pixel phases and 64 active/64 idle owner intervals at 0 managed bytes. A public-only GLSL JIT/NativeAOT consumer verifies fresh schema, image/matrix data, pixels and base restoration. Other platforms/native allocator/hardware/owner acceptance remain separate. |
