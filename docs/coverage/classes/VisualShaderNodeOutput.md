# VisualShaderNodeOutput API coverage

Last updated: 2026-10-25

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeOutput.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeOutput.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNode](VisualShaderNode.md). Electron2D type: [`public class Electron2D.VisualShaderNodeOutput`](../../classes/VisualShaderNodeOutput.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeOutput`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeOutput.xml) | [`public class Electron2D.VisualShaderNodeOutput`](../../classes/VisualShaderNodeOutput.md) | Partial | Canvas position/UV/color/alpha and authored light RGB/alpha/shadow-modulation outputs execute. Normal, Normal Map and Normal Map Depth first require separately retained original Model/Canvas normal bases across ordinary retained geometry, CPU/GPU instances, skinned/procedural meshes and particle/trail streams, plus typed fragment-output capture and scene-light composition. Current geometry only retains their combined transform; UV derivatives or normalization of combined columns do not preserve the nonuniform-scale/skew contract. Point-size, light/shadow-position outputs retain their separate canvas ABI integrations. Unsupported selected outputs reject import atomically. |
