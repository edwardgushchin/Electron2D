# ConvexPolygonShape2D API coverage

Last updated: 2026-09-25

Godot source: [doc/classes/ConvexPolygonShape2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ConvexPolygonShape2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Shape2D](Shape2D.md). Electron2D type: [`public sealed class Electron2D.ConvexPolygonShape`](../../classes/ConvexPolygonShape.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ConvexPolygonShape2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ConvexPolygonShape2D.xml) | [`public sealed class Electron2D.ConvexPolygonShape`](../../classes/ConvexPolygonShape.md) | Implemented | Solid convex hull resource, point-cloud generation and compound fixtures above eight vertices execute under ADR 0062; ConvexPolygonShapeTests covers bounds, winding, invalid input, copies, rotated/dynamic contacts, area monitoring, live edits, packing and warm allocation. |
| [`method set_point_cloud(PackedVector2Array point_cloud) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ConvexPolygonShape2D.xml) | [`public System.Void SetPointCloud(System.ReadOnlySpan<Electron2D.Vector2> pointCloud)`](../../classes/ConvexPolygonShape.md) | Implemented | Solid convex hull resource, point-cloud generation and compound fixtures above eight vertices execute under ADR 0062; ConvexPolygonShapeTests covers bounds, winding, invalid input, copies, rotated/dynamic contacts, area monitoring, live edits, packing and warm allocation. |
| [`property PackedVector2Array points = PackedVector2Array()`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ConvexPolygonShape2D.xml) | [`public Electron2D.Vector2[] Points { get; set; }`](../../classes/ConvexPolygonShape.md) | Implemented | Solid convex hull resource, point-cloud generation and compound fixtures above eight vertices execute under ADR 0062; ConvexPolygonShapeTests covers bounds, winding, invalid input, copies, rotated/dynamic contacts, area monitoring, live edits, packing and warm allocation. |
