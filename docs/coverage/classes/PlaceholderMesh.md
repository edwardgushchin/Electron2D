# PlaceholderMesh API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/PlaceholderMesh.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PlaceholderMesh.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Mesh](Mesh.md). Electron2D type: [`public sealed class Electron2D.PlaceholderMesh`](../../classes/PlaceholderMesh.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class PlaceholderMesh`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PlaceholderMesh.xml) | [`public sealed class Electron2D.PlaceholderMesh`](../../classes/PlaceholderMesh.md) | Implemented | ADRs 0013/0014/0092: version-4 generic/mesh/ordinary-texture missing-resource metadata and original factory/graph identity survive copying, local instantiation and saving. Missing-payload provenance retains typed tolerant placeholders and strict rejection across bundles, while unbundled dependencies can revive. Scalar byte order, actual references, aliases/cycles, original IDs, cache rollback and later compiled restoration remain bounded; physical/foreign/native-allocation/owner gates are separate. |
| [`property AABB aabb = AABB(0, 0, 0, 0, 0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PlaceholderMesh.xml) | [`public Electron2D.Rect2 AABB { get; set; }`](../../classes/PlaceholderMesh.md) | Implemented | ADRs 0013/0014/0092: version-4 generic/mesh/ordinary-texture missing-resource metadata and original factory/graph identity survive copying, local instantiation and saving. Missing-payload provenance retains typed tolerant placeholders and strict rejection across bundles, while unbundled dependencies can revive. Scalar byte order, actual references, aliases/cycles, original IDs, cache rollback and later compiled restoration remain bounded; physical/foreign/native-allocation/owner gates are separate. |
