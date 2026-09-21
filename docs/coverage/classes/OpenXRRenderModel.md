# OpenXRRenderModel API coverage

Last updated: 2026-09-22

Godot source: [modules/openxr/doc_classes/OpenXRRenderModel.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModel.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node3D](Node3D.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class OpenXRRenderModel`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModel.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method get_top_level_path() -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModel.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property RID render_model = RID()`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModel.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`signal render_model_top_level_path_changed() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModel.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
