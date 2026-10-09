# WorldBoundaryShape2D API coverage

Last updated: 2026-10-08

Godot source: [doc/classes/WorldBoundaryShape2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorldBoundaryShape2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Shape2D](Shape2D.md). Electron2D type: [`public sealed class Electron2D.WorldBoundaryShape`](../../classes/WorldBoundaryShape.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class WorldBoundaryShape2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorldBoundaryShape2D.xml) | [`public sealed class Electron2D.WorldBoundaryShape`](../../classes/WorldBoundaryShape.md) | Implemented | Analytic infinite half-plane resource, typed creation/data/lifetime, logical ShapeType identities, CPU scene/server response/sensing/sleep and internal GPU contact/query/CCD paths execute (ADR 0054, WorldBoundaryTests, shared GPU query matrices). Custom preserves only its enum identity; extension geometry and public GPU binding remain separate open capabilities. |
| [`property float distance = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorldBoundaryShape2D.xml) | [`public System.Single Distance { get; set; }`](../../classes/WorldBoundaryShape.md) | Implemented | Analytic infinite half-plane resource, typed creation/data/lifetime, logical ShapeType identities, CPU scene/server response/sensing/sleep and internal GPU contact/query/CCD paths execute (ADR 0054, WorldBoundaryTests, shared GPU query matrices). Custom preserves only its enum identity; extension geometry and public GPU binding remain separate open capabilities. |
| [`property Vector2 normal = Vector2(0, -1)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorldBoundaryShape2D.xml) | [`public Electron2D.Vector2 Normal { get; set; }`](../../classes/WorldBoundaryShape.md) | Implemented | Analytic infinite half-plane resource, typed creation/data/lifetime, logical ShapeType identities, CPU scene/server response/sensing/sleep and internal GPU contact/query/CCD paths execute (ADR 0054, WorldBoundaryTests, shared GPU query matrices). Custom preserves only its enum identity; extension geometry and public GPU binding remain separate open capabilities. |
