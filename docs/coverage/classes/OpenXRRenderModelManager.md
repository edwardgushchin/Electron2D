# OpenXRRenderModelManager API coverage

Last updated: 2026-09-22

Godot source: [modules/openxr/doc_classes/OpenXRRenderModelManager.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelManager.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node3D](Node3D.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class OpenXRRenderModelManager`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelManager.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`enum RenderModelTracker`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelManager.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`enum_value RENDER_MODEL_TRACKER_ANY [RenderModelTracker] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelManager.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum_value RENDER_MODEL_TRACKER_LEFT_HAND [RenderModelTracker] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelManager.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum_value RENDER_MODEL_TRACKER_NONE_SET [RenderModelTracker] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelManager.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum_value RENDER_MODEL_TRACKER_RIGHT_HAND [RenderModelTracker] = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelManager.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`property String make_local_to_pose = ""`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelManager.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property int tracker [OpenXRRenderModelManager.RenderModelTracker] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelManager.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`signal render_model_added(OpenXRRenderModel render_model) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelManager.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`signal render_model_removed(OpenXRRenderModel render_model) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelManager.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
