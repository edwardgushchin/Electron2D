# VisualShaderNodeGlobalExpression API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeGlobalExpression.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeGlobalExpression.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNodeExpression](VisualShaderNodeExpression.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeGlobalExpression`](../../classes/VisualShaderNodeGlobalExpression.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeGlobalExpression`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeGlobalExpression.xml) | [`public sealed class Electron2D.VisualShaderNodeGlobalExpression`](../../classes/VisualShaderNodeGlobalExpression.md) | Implemented | ADRs 0001/0004/0013/0028: dynamic typed ports, source layout and local/shared GLSL expressions execute through the existing offline canvas importer. Preserve port ordering, numeric default presence/conversion, aliases, portable archives and native control flow, including return/discard. Validate source/port budgets, scope and native ABI before replacing artifacts. Editor widgets, separate shader domains, compiled custom providers, physical GPU, native heap, foreign platforms and owner acceptance retain independent gates. |
