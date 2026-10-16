# Typed server shape data

Last updated: 2026-10-16

PhysicsServer.ShapeSetData accepts any live Shape whose immutable represented
ShapeType matches the server-owned RID. A procedural subclass does not need a
resource duplicate factory. The server consumes its ShapeGeometry into a concrete
CircleShape, CapsuleShape, RectangleShape, SegmentShape, WorldBoundaryShape,
SeparationRayShape, ConvexPolygonShape, ConcavePolygonShape or CustomShape.
Existing concrete inputs retain their ordinary resource duplication contract.

ShapeGetData returns an independent concrete resource for both owned server data
and a borrowed procedural Shape.GetRID(). Canonical geometry and solver bias
survive later source edits and disposal. Common ResourceName, ResourceLocalToScene,
CanTranslateMessages and TranslationDomain state is retained. ResourcePath,
ResourceSceneUniqueID and arbitrary subclass fields are not procedural server data.
Immutable Custom geometry retains its existing canonical format/mass/drawing/pair
algorithm contract; built-in worlds still reject enabling unsupported Custom data.

Access preflights every using scene/raw body and Area, including disabled slots.
While evaluating/copying data, the common layer borrows each distinct using world
as a numerical callback and borrows both geometry resources. Nested physics,
hierarchy and physical-world lifetime changes reject. The scope releases in
finally after supplier errors. Wrapper disposal still follows ordinary World
lease ownership; borrowing does not turn a retained wrapper into the runtime.
Only a complete validated copy enters the existing replacement transaction.
Rebuild rollback, dirty retry, private-view retirement and committed cleanup errors
keep their existing semantics. These are cold explicit copy operations; no work
is added to ordinary simulation frames.

The public-only --procedural-data profile uses a Shape subclass without a duplicate
factory. Nine kinds pass import/export, common metadata and source independence.
Throwing suppliers preserve the original error and private view; wrong kinds
reject without replacement. Two worlds and scene/raw/disabled borrowers exercise
nested authoring, replacement, release and source-geometry guards. Logical slot 0
is disabled and selected query results identify slot 1. Resource-pair collision
and scene body/raw body/raw Area shape queries execute for all eight built-in
kinds on CPU/GPU and all nine on the independent registered provider. Custom data
round-trips disabled on the built-ins and enabling it rejects before mutation.

Each enabled kind runs 128 warm frames then 128 measured frames, with three
caller-buffer shape queries and a complete physics interval per frame. A real
awake moving body advances 12.8 units within .01 units. Owner/all-thread managed
allocation is strictly zero. Primitive values/indices/identity are exact; derived
bounds use the existing approximate Vector2 comparison and re-normalized boundary
distance allows .00001 units. These tolerances cover float normalization only.
Evidence lives in bin/physics-procedural-data/2026-10-16/. This does not establish
arbitrary solver algorithms, native/driver budgets, foreign platforms, networking
or target-device/window performance acceptance.
