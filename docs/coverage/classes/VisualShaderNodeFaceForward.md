# VisualShaderNodeFaceForward API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeFaceForward.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeFaceForward.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNodeVectorBase](VisualShaderNodeVectorBase.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeFaceForward`](../../classes/VisualShaderNodeFaceForward.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeFaceForward`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeFaceForward.xml) | [`public sealed class Electron2D.VisualShaderNodeFaceForward`](../../classes/VisualShaderNodeFaceForward.md) | Implemented | ADRs 0001/0004/0013/0028: all two/three/four-component inputs execute strict negative incident/reference dot selection, including zero and non-normalized inputs, through canvas, five particle stages and writable textures. Inherited width/default/copy and exact typed archive factories are exercised by the public consumer. |
