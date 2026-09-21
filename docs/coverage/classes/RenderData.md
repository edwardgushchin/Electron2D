# RenderData API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/RenderData.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderData.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class RenderData`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderData.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`method get_camera_attributes() -> RID`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderData.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`method get_environment() -> RID`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderData.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`method get_render_scene_buffers() -> RenderSceneBuffers`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderData.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method get_render_scene_data() -> RenderSceneData`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderData.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
