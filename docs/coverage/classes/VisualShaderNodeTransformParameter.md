# VisualShaderNodeTransformParameter API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeTransformParameter.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeTransformParameter.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNodeParameter](VisualShaderNodeParameter.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeTransformParameter`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeTransformParameter.xml) | — | Blocked | Trigger: typed matrix graph parameter declarations, symbol/scope resolution, qualifier/instance layout and imported default/hint metadata over the existing raw material matrix backend and now-executing Matrix4x4 graph operand contract under ADR 0028. Numeric matrix shader data is applicable independently of excluded 3D scene types. |
| [`property Transform3D default_value = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeTransformParameter.xml) | — | Blocked | Trigger: typed matrix graph parameter declarations, symbol/scope resolution, qualifier/instance layout and imported default/hint metadata over the existing raw material matrix backend and now-executing Matrix4x4 graph operand contract under ADR 0028. Numeric matrix shader data is applicable independently of excluded 3D scene types. |
| [`property bool default_value_enabled = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeTransformParameter.xml) | — | Blocked | Trigger: typed matrix graph parameter declarations, symbol/scope resolution, qualifier/instance layout and imported default/hint metadata over the existing raw material matrix backend and now-executing Matrix4x4 graph operand contract under ADR 0028. Numeric matrix shader data is applicable independently of excluded 3D scene types. |
