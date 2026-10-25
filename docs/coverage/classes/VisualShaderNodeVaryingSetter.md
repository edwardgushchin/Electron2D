# VisualShaderNodeVaryingSetter API coverage

Last updated: 2026-10-25

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeVaryingSetter.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeVaryingSetter.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNodeVarying](VisualShaderNodeVarying.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeVaryingSetter`](../../classes/VisualShaderNodeVaryingSetter.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeVaryingSetter`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeVaryingSetter.xml) | [`public sealed class Electron2D.VisualShaderNodeVaryingSetter`](../../classes/VisualShaderNodeVaryingSetter.md) | Implemented | ADRs 0001/0004/0013/0028/0051: typed graph declarations and getter/setter resources transport all eight numeric roles from vertex into fragment through private native locations 12..15. Preserve copied declarations, borrowed aliases, type-before-default restoration, selected-edge/root dependency order, smooth floating/matrix and flat integer/Boolean values, typed unwritten defaults, last imported programs and atomic rejection. Public CLR software Wayland/X11/HDR/MSAA consumers execute 26 graphs and GLSL/HLSL pairs over four geometry streams, automatic lighting and live shared-stage values; prepared active/idle managed allocation remains zero. Light-graph authoring, hardware, native heap and foreign-platform acceptance retain separate prerequisites. |
