# TextureArrayRD API coverage

Last updated: 2026-10-11

Godot source: [doc/classes/Texture2DArrayRD.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2DArrayRD.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [TextureLayeredRD](TextureLayeredRD.md). Electron2D type: [`public sealed class Electron2D.TextureArrayRD`](../../classes/TextureArrayRD.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Texture2DArrayRD`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2DArrayRD.xml) | [`public sealed class Electron2D.TextureArrayRD`](../../classes/TextureArrayRD.md) | Partial | ADR 0028: renderer-retained owner-thread device images execute directly in canvas, atlas/proxy and HLSL/GLSL array samplers without a CPU frame bridge. Borrowed image lifetime, atomic binding/list/thread checks, native mip/layer readback, GPU floating conversion, transient archives and duplicate bindings execute. RenderingDeviceCanvasTests verifies real Linux/Vulkan pixels, queued callbacks, session/error cleanup and warmed owner-thread allocations. Integer canvas sampling still requires matching integer material descriptors/pipelines; foreign platforms and native allocations remain unverified. |
