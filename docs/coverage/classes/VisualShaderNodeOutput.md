# VisualShaderNodeOutput API coverage

Last updated: 2026-10-25

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeOutput.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeOutput.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNode](VisualShaderNode.md). Electron2D type: [`public class Electron2D.VisualShaderNodeOutput`](../../classes/VisualShaderNodeOutput.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeOutput`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeOutput.xml) | [`public class Electron2D.VisualShaderNodeOutput`](../../classes/VisualShaderNodeOutput.md) | Partial | Canvas position/UV/color/alpha, all five fragment normal/map/depth/light-position/shadow-position outputs, and authored light RGB/alpha/shadow modulation execute. Original draw/canvas normal columns remain separate across ordinary geometry, CPU/GPU instances, skin, procedural meshes and GPU particle/trail streams. Typed private finalization feeds automatic/authored scene lights with unclamped map depth and independent shadow positions. Remaining Partial dependency is native point-size output: current composition fixes size at one and needs a portable raster/capability contract. Unsupported selected point output rejects import atomically; software pixel/managed-allocation checks retain hardware, native-heap, foreign-platform and owner gates. |
