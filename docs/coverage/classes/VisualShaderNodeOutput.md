# VisualShaderNodeOutput API coverage

Last updated: 2026-10-25

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeOutput.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeOutput.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNode](VisualShaderNode.md). Electron2D type: [`public class Electron2D.VisualShaderNodeOutput`](../../classes/VisualShaderNodeOutput.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeOutput`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeOutput.xml) | [`public class Electron2D.VisualShaderNodeOutput`](../../classes/VisualShaderNodeOutput.md) | Implemented | All applicable canvas vertex position/UV/color/alpha/point-size, fragment normal/map/depth/light/shadow-position, light RGB/alpha/shadow modulation and writable-texture outputs execute under ADR 0028; particle roots use their dedicated type. Native point size starts at one before authored code, preserves defaults/side effects/early returns and actual Points across indexed meshes, CPU/hardware/device-written instances and particles/trails. Optional sized-point and coordinate capabilities reject unsupported devices before native shader publication. Public graph/raw GLSL/HLSL native pixels, paired reload, independent module validation and zero warmed managed-byte brackets are recorded in bin/canvas-point-inputs/2026-10-25/. Hardware/native-heap/foreign/owner acceptance remains separate under ADR 0021. |
