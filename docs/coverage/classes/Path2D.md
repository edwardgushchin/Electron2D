# Path2D API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/Path2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Path2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node2D](Node2D.md). Electron2D type: [`public class Electron2D.Path`](../../classes/Path.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Path2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Path2D.xml) | [`public class Electron2D.Path`](../../classes/Path.md) | Implemented | Path/PathFollow are direct Entity subclasses under ADR 0008, mapping the runtime curve/follower API with the same defaults and policy timing. PathTests verifies loop/clamp endpoints, ratio semantics, offsets, rotation/scale/skew, lifecycle/direct-parent binding, both reparent policies, typed PackedScene ownership, curve replacement, worker deferred delivery, reentrant/callback failure handling and warm movement allocation. PathRenderingTests verifies seven pixel-readback stages on Linux Wayland compatibility/GPU and dummy/software. Typed exceptions, owner-thread dispatch and sibling failure aggregation follow ADRs 0001/0006/0011/0013/0023. Node warnings and SceneTree debug paths are now implemented and verified by SceneDiagnosticsTests and PathRenderingTests; editor curve authoring remains absent; see docs/components/scene-paths.md. |
| [`property Curve2D curve`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Path2D.xml) | [`public Electron2D.Curve2D Curve { get; set; }`](../../classes/Path.md) | Implemented | Path/PathFollow are direct Entity subclasses under ADR 0008, mapping the runtime curve/follower API with the same defaults and policy timing. PathTests verifies loop/clamp endpoints, ratio semantics, offsets, rotation/scale/skew, lifecycle/direct-parent binding, both reparent policies, typed PackedScene ownership, curve replacement, worker deferred delivery, reentrant/callback failure handling and warm movement allocation. PathRenderingTests verifies seven pixel-readback stages on Linux Wayland compatibility/GPU and dummy/software. Typed exceptions, owner-thread dispatch and sibling failure aggregation follow ADRs 0001/0006/0011/0013/0023. Node warnings and SceneTree debug paths are now implemented and verified by SceneDiagnosticsTests and PathRenderingTests; editor curve authoring remains absent; see docs/components/scene-paths.md. |
