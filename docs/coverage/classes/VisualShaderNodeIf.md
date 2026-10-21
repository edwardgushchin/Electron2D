# VisualShaderNodeIf API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeIf.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeIf.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNode](VisualShaderNode.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeIf`](../../classes/VisualShaderNodeIf.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeIf`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeIf.xml) | [`public sealed class Electron2D.VisualShaderNodeIf`](../../classes/VisualShaderNodeIf.md) | Implemented | ADRs 0001/0004/0013/0028/0051: native canvas graph comparison, selection, three-way branches and floating classification retain typed defaults, dormant tolerance, condition presence, transactional type conversion, selectors and copy/archive state. Compare and Switch share VisualShaderValueType; the broader PortType contract also includes the separate sampled-image role. Public authoring/import/material consumers exercise all applicable types/functions/reductions and both stages, exact integer selection bytes and UV-driven control. Nonfinite producer results are software-backend evidence, not a portable arithmetic-domain guarantee. |
