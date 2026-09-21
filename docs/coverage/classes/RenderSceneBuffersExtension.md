# RenderSceneBuffersExtension API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/RenderSceneBuffersExtension.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersExtension.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RenderSceneBuffers](RenderSceneBuffers.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class RenderSceneBuffersExtension`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersExtension.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`method _configure(RenderSceneBuffersConfiguration config) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersExtension.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`method _set_anisotropic_filtering_level(int anisotropic_filtering_level) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersExtension.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`method _set_fsr_sharpness(float fsr_sharpness) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersExtension.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`method _set_texture_mipmap_bias(float texture_mipmap_bias) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersExtension.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method _set_use_debanding(bool use_debanding) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneBuffersExtension.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
