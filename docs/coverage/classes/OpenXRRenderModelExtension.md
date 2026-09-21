# OpenXRRenderModelExtension API coverage

Last updated: 2026-09-22

Godot source: [modules/openxr/doc_classes/OpenXRRenderModelExtension.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [OpenXRExtensionWrapper](OpenXRExtensionWrapper.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class OpenXRRenderModelExtension`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method is_active() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`method render_model_create(int render_model_id) -> RID`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method render_model_destroy(RID render_model) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method render_model_get_all() -> RID[]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method render_model_get_animatable_node_count(RID render_model) -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method render_model_get_animatable_node_name(RID render_model, int index) -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method render_model_get_animatable_node_transform(RID render_model, int index) -> Transform3D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method render_model_get_confidence(RID render_model) -> int [XRPose.TrackingConfidence]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method render_model_get_root_transform(RID render_model) -> Transform3D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method render_model_get_subaction_paths(RID render_model) -> PackedStringArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method render_model_get_top_level_path(RID render_model) -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method render_model_is_animatable_node_visible(RID render_model, int index) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method render_model_new_scene_instance(RID render_model) -> Node3D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`signal render_model_added(RID render_model) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`signal render_model_removed(RID render_model) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`signal render_model_top_level_path_changed(RID render_model) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/openxr/doc_classes/OpenXRRenderModelExtension.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
