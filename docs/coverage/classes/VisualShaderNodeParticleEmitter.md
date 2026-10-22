# VisualShaderNodeParticleEmitter API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeParticleEmitter.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeParticleEmitter.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNode](VisualShaderNode.md). Electron2D type: [`public abstract class Electron2D.VisualShaderNodeParticleEmitter`](../../classes/VisualShaderNodeParticleEmitter.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeParticleEmitter`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeParticleEmitter.xml) | [`public abstract class Electron2D.VisualShaderNodeParticleEmitter`](../../classes/VisualShaderNodeParticleEmitter.md) | Implemented | ADRs 0001/0004/0013/0028/0033/0051/0090: particle lifecycle and writable-texture graphs compile offline into existing compute/blit programs. Preserve canonical stages, authored/compiled mode separation, lifecycle/side-effect ordering, current channels, exact device birth numbers, numeric Vector3/Matrix4x4 arithmetic and planar physical projection. Borrow planar Mesh and bake aligned paged numeric images; preserve defaults, ownership, copies, schema and source failure boundaries. Shared floating layouts and supplied-field masks reuse their semantic enum identities. Physical GPU, native heap, foreign platforms and editor/owner acceptance remain separate gates. |
| [`property bool mode_2d = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeParticleEmitter.xml) | — | Excluded | ADR 0004/0028: physical emission is always planar; the spatial dimension selector has no applicable value domain. All planar mesh and shape emitters execute without that switch. |
