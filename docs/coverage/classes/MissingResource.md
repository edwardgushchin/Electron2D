# MissingResource API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/MissingResource.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MissingResource.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: [`public sealed class Electron2D.MissingResource`](../../classes/MissingResource.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class MissingResource`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MissingResource.xml) | [`public sealed class Electron2D.MissingResource`](../../classes/MissingResource.md) | Implemented | ADRs 0013/0014/0092: version-4 generic/mesh/ordinary-texture missing-resource metadata and original factory/graph identity survive copying, local instantiation and saving. Missing-payload provenance retains typed tolerant placeholders and strict rejection across bundles, while unbundled dependencies can revive. Scalar byte order, actual references, aliases/cycles, original IDs, cache rollback and later compiled restoration remain bounded; physical/foreign/native-allocation/owner gates are separate. |
| [`property String original_class`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MissingResource.xml) | [`public System.String OriginalClass { get; set; }`](../../classes/MissingResource.md) | Implemented | ADRs 0013/0014/0092: version-4 generic/mesh/ordinary-texture missing-resource metadata and original factory/graph identity survive copying, local instantiation and saving. Missing-payload provenance retains typed tolerant placeholders and strict rejection across bundles, while unbundled dependencies can revive. Scalar byte order, actual references, aliases/cycles, original IDs, cache rollback and later compiled restoration remain bounded; physical/foreign/native-allocation/owner gates are separate. |
| [`property bool recording_properties`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MissingResource.xml) | [`public System.Boolean RecordingProperties { get; set; }`](../../classes/MissingResource.md) | Implemented | ADRs 0013/0014/0092: version-4 generic/mesh/ordinary-texture missing-resource metadata and original factory/graph identity survive copying, local instantiation and saving. Missing-payload provenance retains typed tolerant placeholders and strict rejection across bundles, while unbundled dependencies can revive. Scalar byte order, actual references, aliases/cycles, original IDs, cache rollback and later compiled restoration remain bounded; physical/foreign/native-allocation/owner gates are separate. |
