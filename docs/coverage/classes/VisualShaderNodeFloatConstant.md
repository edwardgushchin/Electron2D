# VisualShaderNodeFloatConstant API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeFloatConstant.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeFloatConstant.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNodeConstant](VisualShaderNodeConstant.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeFloatConstant`](../../classes/VisualShaderNodeFloatConstant.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeFloatConstant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeFloatConstant.xml) | [`public sealed class Electron2D.VisualShaderNodeFloatConstant`](../../classes/VisualShaderNodeFloatConstant.md) | Implemented | ADRs 0001/0004/0028: typed numeric canvas graph authoring, immutable default/connection snapshots, compiled registered factories and owned-output/borrowed-definition lifetime; portable graph archives enter the existing offline ShaderImport and ordinary ShaderMaterial renderer. Scalar and vector operations execute in 57 public consumer programs, both canvas stages, software Wayland/X11 and HDR/MSAA with zero prepared managed bytes. Source edits require reimport; no runtime compiler or universal value wrapper. |
| [`property float constant = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeFloatConstant.xml) | [`public System.Single Constant { get; set; }`](../../classes/VisualShaderNodeFloatConstant.md) | Implemented | ADRs 0001/0004/0028: typed numeric canvas graph authoring, immutable default/connection snapshots, compiled registered factories and owned-output/borrowed-definition lifetime; portable graph archives enter the existing offline ShaderImport and ordinary ShaderMaterial renderer. Scalar and vector operations execute in 57 public consumer programs, both canvas stages, software Wayland/X11 and HDR/MSAA with zero prepared managed bytes. Source edits require reimport; no runtime compiler or universal value wrapper. |
