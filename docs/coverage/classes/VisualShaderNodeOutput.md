# VisualShaderNodeOutput API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeOutput.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeOutput.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNode](VisualShaderNode.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeOutput`](../../classes/VisualShaderNodeOutput.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeOutput`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeOutput.xml) | [`public sealed class Electron2D.VisualShaderNodeOutput`](../../classes/VisualShaderNodeOutput.md) | Partial | Canvas position/UV/color/alpha outputs execute. Point-size and normal-map/light/shadow-position graph outputs require their actual canvas ABI integration; unsupported selected outputs reject import atomically. |
