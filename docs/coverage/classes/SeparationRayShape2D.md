# SeparationRayShape2D API coverage

Last updated: 2026-09-26

Godot source: [doc/classes/SeparationRayShape2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SeparationRayShape2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Shape2D](Shape2D.md). Electron2D type: [`public sealed class Electron2D.SeparationRayShape`](../../classes/SeparationRayShape.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class SeparationRayShape2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SeparationRayShape2D.xml) | [`public sealed class Electron2D.SeparationRayShape`](../../classes/SeparationRayShape.md) | Partial | Directed resource, queries, Area sensing and body/character motion execute. Required trigger: alternative directed solver manifolds before constraint creation, including ordinary dynamic impulses, friction/restitution, sleep and contact reports; these enter that integration first slice (ADR 0068, SeparationRayShapeTests). |
| [`property float length = 20.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SeparationRayShape2D.xml) | [`public System.Single Length { get; set; }`](../../classes/SeparationRayShape.md) | Implemented | Typed length=20/slope=false state, finite nonnegative geometry, padded bounds, independent copying and server creation execute; directed query/body-motion behavior is checked by SeparationRayShapeTests (ADR 0068). |
| [`property bool slide_on_slope = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SeparationRayShape2D.xml) | [`public System.Boolean SlideOnSlope { get; set; }`](../../classes/SeparationRayShape.md) | Implemented | Typed length=20/slope=false state, finite nonnegative geometry, padded bounds, independent copying and server creation execute; directed query/body-motion behavior is checked by SeparationRayShapeTests (ADR 0068). |
