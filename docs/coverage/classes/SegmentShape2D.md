# SegmentShape2D API coverage

Last updated: 2026-09-25

Godot source: [doc/classes/SegmentShape2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SegmentShape2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Shape2D](Shape2D.md). Electron2D type: [`public sealed class Electron2D.SegmentShape`](../../classes/SegmentShape.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class SegmentShape2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SegmentShape2D.xml) | [`public sealed class Electron2D.SegmentShape`](../../classes/SegmentShape.md) | Implemented | Two-sided segment fixtures, exact endpoints/bounds, copying and live body/area contacts execute under ADR 0061; SegmentShapeTests covers rotated and dynamic geometry, degenerate fallback, mass/inertia, packing, failures and warm allocation. |
| [`property Vector2 a = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SegmentShape2D.xml) | [`public Electron2D.Vector2 A { get; set; }`](../../classes/SegmentShape.md) | Implemented | Two-sided segment fixtures, exact endpoints/bounds, copying and live body/area contacts execute under ADR 0061; SegmentShapeTests covers rotated and dynamic geometry, degenerate fallback, mass/inertia, packing, failures and warm allocation. |
| [`property Vector2 b = Vector2(0, 10)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SegmentShape2D.xml) | [`public Electron2D.Vector2 B { get; set; }`](../../classes/SegmentShape.md) | Implemented | Two-sided segment fixtures, exact endpoints/bounds, copying and live body/area contacts execute under ADR 0061; SegmentShapeTests covers rotated and dynamic geometry, degenerate fallback, mass/inertia, packing, failures and warm allocation. |
