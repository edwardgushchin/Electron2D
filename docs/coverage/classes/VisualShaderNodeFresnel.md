# VisualShaderNodeFresnel API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeFresnel.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeFresnel.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNode](VisualShaderNode.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeFresnel`](../../classes/VisualShaderNodeFresnel.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeFresnel`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeFresnel.xml) | [`public sealed class Electron2D.VisualShaderNodeFresnel`](../../classes/VisualShaderNodeFresnel.md) | Implemented | ADR 0028: complete typed Fresnel normal/view/invert/power operation, constructor defaults and portable Resource factory. All eight current graph stages execute; an absent canvas-fragment normal now reads the actual normal channel with per-command flip/transpose and local Z reconstruction. Public native tests cover default/connected/stored values, clamped dot without normalization, scalar powers, live values/reset, raw RGBAF, batch/mesh isolation and zero prepared managed allocation. Physical/native-heap/foreign-platform acceptance stays separate. |
