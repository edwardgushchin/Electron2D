# VisualShaderNodeTransformCompose API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeTransformCompose.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeTransformCompose.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNode](VisualShaderNode.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeMatrixCompose`](../../classes/VisualShaderNodeMatrixCompose.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeTransformCompose`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeTransformCompose.xml) | [`public sealed class Electron2D.VisualShaderNodeMatrixCompose`](../../classes/VisualShaderNodeMatrixCompose.md) | Implemented | ADRs 0001/0004/0013/0028/0051: these nodes operate on ordinary numeric matrix shader data, projected to Matrix-named resources and copied System.Numerics.Matrix4x4 values; no 3D scene transform API is introduced. All sixteen components retain portable row-order archives and column-order GLSL literals. Constant/arithmetic/inverse/transpose/composition/decomposition/determinant/outer/vector products and matrix comparison/selection execute in both canvas stages through the existing importer/material backend. Native singular/division-domain boundaries remain separate import/load checks. Independent numeric oracles, all-component copy/archive and spatial UV control verify the dataflow; hardware/platform/native-heap acceptance remains separate. |
