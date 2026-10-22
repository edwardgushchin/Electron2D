# VisualShaderNodeRandomRange API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeRandomRange.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeRandomRange.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNode](VisualShaderNode.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeRandomRange`](../../classes/VisualShaderNodeRandomRange.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeRandomRange`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeRandomRange.xml) | [`public sealed class Electron2D.VisualShaderNodeRandomRange`](../../classes/VisualShaderNodeRandomRange.md) | Implemented | ADRs 0001/0004/0013/0028: fixed Vector3 seed and scalar bounds execute the pinned row-vector hash and unclamped mix through existing canvas, particle and writable-texture graph import. Zero-seed extrapolation, equal/reversed bounds, connected/default inputs, shared aliases and typed archives are covered by the focused public consumer. |
