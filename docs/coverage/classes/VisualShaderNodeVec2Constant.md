# VisualShaderNodeVec2Constant API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeVec2Constant.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeVec2Constant.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNodeConstant](VisualShaderNodeConstant.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeVec2Constant`](../../classes/VisualShaderNodeVec2Constant.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeVec2Constant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeVec2Constant.xml) | [`public sealed class Electron2D.VisualShaderNodeVec2Constant`](../../classes/VisualShaderNodeVec2Constant.md) | Implemented | ADRs 0001/0004/0028: typed numeric canvas graph authoring, immutable default/connection snapshots, compiled registered factories and owned-output/borrowed-definition lifetime; portable graph archives enter the existing offline ShaderImport and ordinary ShaderMaterial renderer. Scalar and vector operations execute in 57 public consumer programs, both canvas stages, software Wayland/X11 and HDR/MSAA with zero prepared managed bytes. Source edits require reimport; no runtime compiler or universal value wrapper. |
| [`property Vector2 constant = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeVec2Constant.xml) | [`public Electron2D.Vector2 Constant { get; set; }`](../../classes/VisualShaderNodeVec2Constant.md) | Implemented | ADRs 0001/0004/0028: typed numeric canvas graph authoring, immutable default/connection snapshots, compiled registered factories and owned-output/borrowed-definition lifetime; portable graph archives enter the existing offline ShaderImport and ordinary ShaderMaterial renderer. Scalar and vector operations execute in 57 public consumer programs, both canvas stages, software Wayland/X11 and HDR/MSAA with zero prepared managed bytes. Source edits require reimport; no runtime compiler or universal value wrapper. |
