# TextureRD API coverage

Last updated: 2026-10-11

Godot source: [doc/classes/Texture2DRD.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2DRD.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Texture2D](Texture.md#godot-texture2d). Electron2D type: [`public sealed class Electron2D.TextureRD`](../../classes/TextureRD.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Texture2DRD`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2DRD.xml) | [`public sealed class Electron2D.TextureRD`](../../classes/TextureRD.md) | Partial | ADR 0028: renderer-retained owner-thread device images execute directly in canvas, atlas/proxy and HLSL/GLSL array samplers without a CPU frame bridge. Borrowed image lifetime, atomic binding/list/thread checks, native mip/layer readback, GPU floating conversion, transient archives and duplicate bindings execute. RenderingDeviceCanvasTests verifies real Linux/Vulkan pixels, queued callbacks, session/error cleanup and warmed owner-thread allocations. Integer canvas sampling still requires matching integer material descriptors/pipelines; foreign platforms and native allocations remain unverified. |
| [`property bool resource_local_to_scene = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2DRD.xml) | [`public System.Boolean ResourceLocalToScene { get; set; }`](../../classes/Resource.md) | Partial | ADRs 0028/0095: static access retains one renderer-owned GPU device per session, returns null for absent/compatibility rendering, submits after canvas authoring and completes queued reads before FramePostDraw; resources retain inherited managed scene-local metadata with default false. Native frame/lifecycle/error fixtures execute. |
| [`property RID texture_rd_rid`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2DRD.xml) | [`public Electron2D.RID TextureRDRID { get; set; }`](../../classes/TextureRD.md) | Partial | ADR 0028: renderer-retained owner-thread device images execute directly in canvas, atlas/proxy and HLSL/GLSL array samplers without a CPU frame bridge. Borrowed image lifetime, atomic binding/list/thread checks, native mip/layer readback, GPU floating conversion, transient archives and duplicate bindings execute. RenderingDeviceCanvasTests verifies real Linux/Vulkan pixels, queued callbacks, session/error cleanup and warmed owner-thread allocations. Integer canvas sampling still requires matching integer material descriptors/pipelines; foreign platforms and native allocations remain unverified. |
