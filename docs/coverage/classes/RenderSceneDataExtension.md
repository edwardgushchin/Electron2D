# RenderSceneDataExtension API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/RenderSceneDataExtension.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneDataExtension.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RenderSceneData](RenderSceneData.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class RenderSceneDataExtension`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneDataExtension.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`method _get_cam_projection() -> Projection`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneDataExtension.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method _get_cam_transform() -> Transform3D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneDataExtension.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method _get_uniform_buffer() -> RID`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneDataExtension.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`method _get_view_count() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneDataExtension.xml) | — | Blocked | Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. |
| [`method _get_view_eye_offset(int view) -> Vector3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneDataExtension.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method _get_view_projection(int view) -> Projection`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RenderSceneDataExtension.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
