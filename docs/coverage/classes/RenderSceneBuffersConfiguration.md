# RenderSceneBuffersConfiguration API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/RenderSceneBuffersConfiguration.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersConfiguration.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class RenderSceneBuffersConfiguration`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersConfiguration.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`property int anisotropic_filtering_level [RenderingServer.ViewportAnisotropicFiltering] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersConfiguration.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`property float fsr_sharpness = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersConfiguration.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`property Vector2i internal_size = Vector2i(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersConfiguration.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`property int msaa_3d [RenderingServer.ViewportMSAA] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersConfiguration.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`property RID render_target = RID()`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersConfiguration.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`property int scaling_3d_mode [RenderingServer.ViewportScaling3DMode] = 255`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersConfiguration.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`property int screen_space_aa [RenderingServer.ViewportScreenSpaceAA] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersConfiguration.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`property Vector2i target_size = Vector2i(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersConfiguration.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`property float texture_mipmap_bias = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersConfiguration.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`property int view_count = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersConfiguration.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
