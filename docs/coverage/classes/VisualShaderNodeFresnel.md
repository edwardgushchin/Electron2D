# VisualShaderNodeFresnel API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeFresnel.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeFresnel.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNode](VisualShaderNode.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeFresnel`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeFresnel.xml) | — | Unimplemented | ADR 0028: the pinned generator uses implicit NORMAL for an unconnected canvas normal input (visual_shader_nodes.cpp, ed1daf0bf001b61586d9930840f2f1394092c079, lines 7320-7372). The current graph input translator has no implicit canvas-normal producer. Implement that producer and the default/connected normal-view-invert-power contract before an executing Fresnel slice; explicit inputs alone do not complete it. |
