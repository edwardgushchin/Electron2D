# VisualShaderNodeCurveTexture API coverage

Last updated: 2026-10-25

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeCurveTexture.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeCurveTexture.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNodeResizableBase](VisualShaderNodeResizableBase.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeCurveTexture`](../../classes/VisualShaderNodeCurveTexture.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeCurveTexture`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeCurveTexture.xml) | [`public sealed class Electron2D.VisualShaderNodeCurveTexture`](../../classes/VisualShaderNodeCurveTexture.md) | Implemented | ADRs 0004/0013/0028: scalar and RGB curve lookup nodes execute through existing floating-point images and the shared offline canvas, particle and writable-texture importer. Preserve scalar zero defaults, explicit absence, connected-input priority, borrowed image/curve aliases, numeric signed/HDR values, sampler policies, shared copies and portable factories. Pixel rebakes of bound curves refresh existing materials; source edits require reimport. Native heap, hardware, foreign platforms and editor acceptance remain separate. |
| [`property CurveTexture texture`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeCurveTexture.xml) | [`public Electron2D.CurveTexture Texture { get; set; }`](../../classes/VisualShaderNodeCurveTexture.md) | Implemented | ADRs 0004/0013/0028: scalar and RGB curve lookup nodes execute through existing floating-point images and the shared offline canvas, particle and writable-texture importer. Preserve scalar zero defaults, explicit absence, connected-input priority, borrowed image/curve aliases, numeric signed/HDR values, sampler policies, shared copies and portable factories. Pixel rebakes of bound curves refresh existing materials; source edits require reimport. Native heap, hardware, foreign platforms and editor acceptance remain separate. |
