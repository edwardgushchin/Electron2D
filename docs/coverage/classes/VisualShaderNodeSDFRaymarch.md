# VisualShaderNodeSDFRaymarch API coverage

Last updated: 2026-10-21

Godot source: [modules/visual_shader/doc_classes/VisualShaderNodeSDFRaymarch.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeSDFRaymarch.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VisualShaderNode](VisualShaderNode.md). Electron2D type: [`public sealed class Electron2D.VisualShaderNodeSDFRaymarch`](../../classes/VisualShaderNodeSDFRaymarch.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VisualShaderNodeSDFRaymarch`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/visual_shader/doc_classes/VisualShaderNodeSDFRaymarch.xml) | [`public sealed class Electron2D.VisualShaderNodeSDFRaymarch`](../../classes/VisualShaderNodeSDFRaymarch.md) | Implemented | ADRs 0001/0004/0013/0028: five canvas SDF graph families execute through the shared offline importer and renderer-owned pixel-space field. Preserve signed distances, central-difference normals, distance/hit/endpoint ray outputs, implicit screen UV, typed default absence, aliases, portable archives and live renderer policies. Reachable vertex use rejects; Light graph execution retains its existing dependency. Physical GPU, native heap, foreign hosts and owner visual acceptance remain separate. |
