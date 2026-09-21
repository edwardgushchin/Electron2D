# VisualShaderNodeVec3Parameter API coverage

Last updated: 2026-09-22

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeVec3Parameter.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeVec3Parameter.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNodeParameter](VisualShaderNodeParameter.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeVec3Parameter`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeVec3Parameter.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`property Vector3 default_value = Vector3(0, 0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeVec3Parameter.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`property bool default_value_enabled = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeVec3Parameter.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
