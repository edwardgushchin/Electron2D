# CapsuleShape2D API coverage

Last updated: 2026-09-25

Godot source: [doc/classes/CapsuleShape2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CapsuleShape2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Shape2D](Shape2D.md). Electron2D type: [`public sealed class Electron2D.CapsuleShape`](../../classes/CapsuleShape.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CapsuleShape2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CapsuleShape2D.xml) | [`public sealed class Electron2D.CapsuleShape`](../../classes/CapsuleShape.md) | Implemented | Coupled dimensions, bounds, resource copying and live body/area fixtures execute under ADR 0059; CapsuleShapeTests covers defaults, degenerate values, rotation, contacts, packing, callback failure and warm allocation. |
| [`property float height = 30.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CapsuleShape2D.xml) | [`public System.Single Height { get; set; }`](../../classes/CapsuleShape.md) | Implemented | Coupled dimensions, bounds, resource copying and live body/area fixtures execute under ADR 0059; CapsuleShapeTests covers defaults, degenerate values, rotation, contacts, packing, callback failure and warm allocation. |
| [`property float mid_height`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CapsuleShape2D.xml) | [`public System.Single MidHeight { get; set; }`](../../classes/CapsuleShape.md) | Implemented | Coupled dimensions, bounds, resource copying and live body/area fixtures execute under ADR 0059; CapsuleShapeTests covers defaults, degenerate values, rotation, contacts, packing, callback failure and warm allocation. |
| [`property float radius = 10.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CapsuleShape2D.xml) | [`public System.Single Radius { get; set; }`](../../classes/CapsuleShape.md) | Implemented | Coupled dimensions, bounds, resource copying and live body/area fixtures execute under ADR 0059; CapsuleShapeTests covers defaults, degenerate values, rotation, contacts, packing, callback failure and warm allocation. |
