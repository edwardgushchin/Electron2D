# Collision shapes component

Last updated: 2026-10-08

## Scope and owned types

[`Shape`](../classes/Shape.md) is the abstract managed geometry resource. [`CircleShape`](../classes/CircleShape.md), [`CapsuleShape`](../classes/CapsuleShape.md), [`SegmentShape`](../classes/SegmentShape.md), [`SeparationRayShape`](../classes/SeparationRayShape.md), [`ConvexPolygonShape`](../classes/ConvexPolygonShape.md), [`ConcavePolygonShape`](../classes/ConcavePolygonShape.md) and [`RectangleShape`](../classes/RectangleShape.md) provide concrete fixtures. [`CollisionShape`](../classes/CollisionShape.md) borrows one Shape as a direct `PhysicsBody` or `Area` child. [`CollisionPolygon`](../classes/CollisionPolygon.md) is a sibling scene child that owns its generated solid or hollow geometry; [`PolygonBuildMode`](../classes/PolygonBuildMode.md) chooses its construction mode. None of these public types expose backend handles.

## Runtime flow

Every built-in resource supplies a temporary [PhysicsShapeGeometry](../classes/PhysicsShapeGeometry.md)
view of scene-unit endpoints, radius or borrowed vertices. Resource classes contain
no backend types. [PhysicsShapeBackend](../classes/PhysicsShapeBackend.md) constructs
current CPU fixtures and query proxies and retains compiled convex hulls in a weak
resource-keyed cache. The common collider adapter calls it for scene/server slots;
mass and direct queries use the same compilation. Standalone Shape collision reads
the view directly, preserving whole convex contours. Compiled polygon validation
still runs during authoring; independent GPU geometry ownership remains open.

Circle radius defaults to 10 scene units, capsule radius/full height to 10/30, segment endpoints to (0, 0)/(0, 10), and rectangle size to (20, 20). Capsule height and radius stay linked; MidHeight sets their central separation. The capsule accepts finite nonnegative dimensions, including zero and line/circle limits. Circle and rectangle require finite positive dimensions; segment endpoints and their difference must be finite and may coincide. These shapes report local bounds, publish geometry changes and support independent resource duplication; equal capsule radius/height writes do not publish, while an equal MidHeight write does. A CollisionShape starts with no resource, `Disabled=false`, `OneWayCollision=false` and local direction `(0, 1)`; assigning a live resource or changing either flag updates its direct collision owner's fixtures before the next physics step. Local position and rotation offset the fixture; scale/skew are rejected while active. The parent body or area owns its backend fixtures, while the caller owns the Shape resource.

A convex polygon starts empty, accepts convex perimeter arrays or a point cloud, and can append several solid fixtures beyond eight vertices while preserving one public resource. A concave polygon stores independent endpoint pairs and appends hollow line fixtures; a body fully inside closed edges does not overlap them. A body child can mark all its fixtures one-way, choose a normalized local pass-through direction and set a finite recovery-depth margin; the physics world keeps each pair's initial contact side until separation. An Area child keeps the flag for scene state but remains a two-sided sensor. Shape `Changed` and `Disposed` notifications mark the attached owner's fixture set dirty. Shape setters validate geometry before mutation; rebuild validates active transforms before destroying old fixtures. Configuration warnings report a missing direct collision owner, a missing/disposed resource, or an ineffective Area one-way setting. A detached CollisionShape is still a normal Entity and can be packed into a scene with its borrowed resource association.

A direct CollisionPolygon copies its local vertices and owns generated resources. Solids mode decomposes a valid contour into convex parts, while Segments mode closes the vertex list into hollow edges. Empty, insufficient or undecomposable contours contribute no geometry and report warnings; finite input validation rejects before mutation. Mode, contour, disabled and transform changes mark the same body or Area fixture owner dirty. Polygon one-way body contacts reuse the current pre-solve side selection; its margin also gates typed body motion recovery; an Area still senses from both sides. The node packs its typed contour and rebuilds its owned geometry after scene instantiation.

A [PhysicsShapeQueryParameters](../classes/PhysicsShapeQueryParameters.md) may borrow any concrete Shape for direct overlap, motion and contact queries. The borrowed RID is allocated lazily, remains stable through geometry edits, and is released by the owning Shape's disposal. A server collider may also use that RID; edits mark its fixtures dirty before the next direct query. Compound convex pieces and hollow paired edges preserve the same direct shape-owner identity. Standalone Shape operations consume the same source geometry through their separate resource-only collision kernel.

## Dependencies and verification

Shapes use Core Vector2/Rect2/Resource contracts and the Entity scene hierarchy; backend geometry compilation resides in PhysicsShapeBackend. [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) checks defaults, validation, bounds, duplication, live circle resizing, disabled fixtures, PackedScene restoration and borrowed lifetime; [CapsuleShapeTests](../../tests/Electron2D.Tests/CapsuleShapeTests.cs) checks coupled dimensions, rotation, body/area fixtures and warmed allocations. [SegmentShapeTests](../../tests/Electron2D.Tests/SegmentShapeTests.cs) checks two-sided terrain, dynamic and sensor fixtures, mass and warmed allocations. [ConvexPolygonShapeTests](../../tests/Electron2D.Tests/ConvexPolygonShapeTests.cs) checks hulls, compound fixtures, dynamic contacts, area sensing and warmed allocations. [ConcavePolygonShapeTests](../../tests/Electron2D.Tests/ConcavePolygonShapeTests.cs) checks paired edges, hollow contact and sensors. [OneWayCollisionTests](../../tests/Electron2D.Tests/OneWayCollisionTests.cs) checks initial contact side, rotation, live changes, packing, Area sensing and warmed allocation. [PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks one-way margins on borrowed and owned scene shapes. [AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks direct area ownership and sensor edits. The capsule and segment sub-slop fixture adaptations are defined in [ADRs 0059 and 0061](../decisions/physics.md#adr-0061); [ADR 0065](../decisions/physics.md#adr-0065) defines one-way body contacts. Debug drawing and custom solver bias retain their open rows in [coverage](../coverage/index.md).

[CollisionPolygonTests](../../tests/Electron2D.Tests/CollisionPolygonTests.cs) checks the direct scene polygon's solid/segment modes, concave and hollow sensing, one-way contact, errors, packing and warmed allocation. [ADR 0066](../decisions/physics.md#adr-0066) defines its owned geometry and direct slot. Debug drawing and custom solver bias retain their open rows in [coverage](../coverage/index.md).

The geometry extraction passes the expanded 28-suite collider group and the full
current GPU stage-host suite. The new shared/copy polygon regression checks live
fixture and query updates, rejected-edit preservation and 128 warmed queries with
zero managed allocation. This is current-backend preservation, not acceptance of
an independent GPU implementation or a throughput claim.

## Decision

- [0054: First Box2D-backed scene-body profile](../decisions/physics.md#adr-0054)
- [0055: Directional scene area monitoring](../decisions/physics.md#adr-0055)
- [0059: Capsule collision geometry](../decisions/physics.md#adr-0059)
- [0061: Segment collision geometry](../decisions/physics.md#adr-0061)
- [0062: Compound convex fixtures](../decisions/physics.md#adr-0062)
- [0064: Hollow paired-segment resource](../decisions/physics.md#adr-0064)
- [0065: One-way scene-body contacts](../decisions/physics.md#adr-0065)
- [0066: Direct scene collision polygons](../decisions/physics.md#adr-0066)

SeparationRayShape defaults to length 20 and SlideOnSlope false, reports padded drawing bounds, duplicates independently and contributes a zero-density sensor fixture with exact directed metadata. Query/body-motion and Area sensing use the special kernel under [ADR 0068](../decisions/physics.md#adr-0068); ray/point queries exclude it. Rays contribute no inertia. Ordinary dynamic ray response remains an exact required solver integration gap. [SeparationRayShapeTests](../../tests/Electron2D.Tests/SeparationRayShapeTests.cs) verifies current behavior and that remaining boundary.

Standalone Shape methods test two resources without registering server RIDs or creating a world. The internal PhysicsShapeCollision kernel uses original scene-unit support geometry, independently swept convex regions, separating axes and clipped boundary supports. Full convex contours retain their external boundary rather than fixture partitions; concave/ray pairs retain their pinned special-motion rules. Its private per-thread buffers reuse proxy and hull capacity without retaining resources. [ShapeCollisionTests](../../tests/Electron2D.Tests/ShapeCollisionTests.cs) verifies active/full-contour/empty-contact calls without warmed managed allocation, under [ADR 0069](../decisions/physics.md#adr-0069).

[CollisionObject shape-owner groups](../classes/CollisionObject.md#createshapeowner) unify manual borrowed resources and direct collision children. Sorted group IDs are separate from append-order global logical shape slots; structural removals reindex later slots. Body/Area preparation uses slot resource revisions, owner pose/policy and shared native fixture construction. Arbitrary weak owner identity reaches KinematicCollision results. ShapeOwnerTests verifies query/motion/solver/sensor integration and warmed zero managed allocation under [ADR 0071](../decisions/physics.md#adr-0071).
