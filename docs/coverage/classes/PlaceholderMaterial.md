# PlaceholderMaterial API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/PlaceholderMaterial.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PlaceholderMaterial.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Material](Material.md). Electron2D type: [`public sealed class Electron2D.PlaceholderMaterial`](../../classes/PlaceholderMaterial.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class PlaceholderMaterial`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PlaceholderMaterial.xml) | [`public sealed class Electron2D.PlaceholderMaterial`](../../classes/PlaceholderMaterial.md) | Implemented | ADRs 0013/0014/0028: fresh caller-owned program-free canvas placeholders work across the complete material family. Version-8 typed Material recovery retains original factory/opaque fields, aliases/cycles, scene-local ownership and missing-payload provenance through copies, worker requests and file bundles; fresh processes restore the actual blend material. Strict default, legacy versions 1–7, cache rollback/replacement, concrete subtype and corrupt role/provenance boundaries are verified. Linux software GPU/Compatibility pixels and 64 prepared RID switches per backend have separate physical-GPU/native-heap/foreign/owner gates. Material runtime inheritance, factory and borrowed identity are executable; editor inspection/extension hooks and 3D exclusions retain their individual dependency rows. |
