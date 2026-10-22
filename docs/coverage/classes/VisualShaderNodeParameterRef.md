# VisualShaderNodeParameterRef API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeParameterRef.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeParameterRef.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNode](VisualShaderNode.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeParameterRef`](../../classes/VisualShaderNodeParameterRef.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeParameterRef`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeParameterRef.xml) | [`public sealed class Electron2D.VisualShaderNodeParameterRef`](../../classes/VisualShaderNodeParameterRef.md) | Implemented | ADRs 0001/0004/0005/0013/0028/0033: typed numeric graph symbols and references execute through shared material, registered global and sixteen-slot per-item storage. Offline imports preserve typed defaults and immutable advisory range/enum/color metadata; graph-local references preserve RGB/alpha and matrix roles, copies and portable archives. Matrix scene types and sampled parameter producers retain their separate ownership and integration boundaries. |
| [`property String parameter_name = "[None]"`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeParameterRef.xml) | [`public System.String ParameterName { get; set; }`](../../classes/VisualShaderNodeParameterRef.md) | Implemented | ADRs 0001/0004/0005/0013/0028/0033: typed numeric graph symbols and references execute through shared material, registered global and sixteen-slot per-item storage. Offline imports preserve typed defaults and immutable advisory range/enum/color metadata; graph-local references preserve RGB/alpha and matrix roles, copies and portable archives. Matrix scene types and sampled parameter producers retain their separate ownership and integration boundaries. |
