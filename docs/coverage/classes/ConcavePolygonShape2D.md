# ConcavePolygonShape2D API coverage

Last updated: 2026-09-25

Godot source: [doc/classes/ConcavePolygonShape2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ConcavePolygonShape2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Shape2D](Shape2D.md). Electron2D type: [`public sealed class Electron2D.ConcavePolygonShape`](../../classes/ConcavePolygonShape.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ConcavePolygonShape2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ConcavePolygonShape2D.xml) | [`public sealed class Electron2D.ConcavePolygonShape`](../../classes/ConcavePolygonShape.md) | Implemented | Hollow paired-segment resource and multi-fixture body/area integration execute under ADR 0064; ConcavePolygonShapeTests covers ownership, bounds, inside-versus-edge detection, point fallback, live edits, packing and warm allocation. |
| [`property PackedVector2Array segments = PackedVector2Array()`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ConcavePolygonShape2D.xml) | [`public Electron2D.Vector2[] Segments { get; set; }`](../../classes/ConcavePolygonShape.md) | Implemented | Hollow paired-segment resource and multi-fixture body/area integration execute under ADR 0064; ConcavePolygonShapeTests covers ownership, bounds, inside-versus-edge detection, point fallback, live edits, packing and warm allocation. |
