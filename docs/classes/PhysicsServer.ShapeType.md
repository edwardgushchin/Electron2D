# PhysicsServer.ShapeType

Last updated: 2026-10-09

**Owner:** [PhysicsServer](PhysicsServer.md) · **Source:** [PhysicsServer.Resources.cs](../../src/Servers/Physics/PhysicsServer.Resources.cs)

Logical geometry identity returned by `PhysicsServer.ShapeGetType(RID)`. It reports
the authored resource kind even when a short segment, capsule or ray compiles to
a point/circle fixture. It does not expose backend fixture IDs.

| Value | Integer | Resource |
| --- | ---: | --- |
| `WorldBoundary` | 0 | WorldBoundaryShape |
| `SeparationRay` | 1 | SeparationRayShape |
| `Segment` | 2 | SegmentShape |
| `Circle` | 3 | CircleShape |
| `Rectangle` | 4 | RectangleShape |
| `Capsule` | 5 | CapsuleShape |
| `ConvexPolygon` | 6 | ConvexPolygonShape |
| `ConcavePolygon` | 7 | ConcavePolygonShape |
| `Custom` | 8 | Reserved extension identity; no executable custom-shape factory. |

The getter validates live shape ownership and rejects stale/foreign/non-shape RIDs.
WorldBoundaryTests checks all eight built-in identities and released-handle rejection.
