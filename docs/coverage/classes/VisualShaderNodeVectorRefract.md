# VisualShaderNodeVectorRefract API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeVectorRefract.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeVectorRefract.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNodeVectorBase](VisualShaderNodeVectorBase.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeVectorRefract`](../../classes/VisualShaderNodeVectorRefract.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeVectorRefract`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeVectorRefract.xml) | [`public sealed class Electron2D.VisualShaderNodeVectorRefract`](../../classes/VisualShaderNodeVectorRefract.md) | Implemented | ADRs 0001/0013/0028: executable numeric vector dataflow uses the existing typed graph, resource archive, offline compiler and retained material. Compose/decompose preserve component order and dormant scalar defaults; fixed-three-component dot and width-selected length/distance/refraction retain their port semantics. Refraction keeps scalar eta distinct across width changes. Actual graph consumers cover all widths, multi-output fan-out, alpha, vertex/fragment paths and valid/error boundaries. |
