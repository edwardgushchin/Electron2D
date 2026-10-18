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
Owner/phase checks first run without selected status callbacks. The common layer
borrows every participating world before status hooks, so those observers cannot
change another participant during preflight. While evaluating/copying data it borrows each distinct using world
as a numerical callback and borrows both geometry resources. Nested physics,
hierarchy and physical-world lifetime changes reject. The scope releases in
finally after supplier errors. Wrapper disposal still follows ordinary World
lease ownership; borrowing does not turn a retained wrapper into the runtime.
Only a complete validated copy enters the existing replacement transaction.
All participating worlds stay borrowed through numerical rebuilds, rollback and
rejected-copy retirement; a provider cannot edit a different participating world
from its rebuild or failed-retirement callback. World borrowing ends before the
old committed view retires, preserving ordinary committed callback mutation.
Rebuild rollback, dirty retry, private-view retirement and committed cleanup errors
keep their existing semantics. The rollback fixture selects its first actually
published borrower rather than assuming RID enumeration order, retaining exact
apply/rollback/retirement errors after earlier tests recycle identities.
These are cold explicit copy operations; no work
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

## Shared CPU/GPU/registered replacement

The public-only --shared-data profile shares one owned circle RID between three
worlds: CPU, CPU/GPU and an independent registered provider. Each world contains a
scene body, scene Area, raw body and raw Area, with logical slots 0/2 and a disabled
gap. Creation order and solver-thread policy vary independently. Explicit backend
assertions and real numerical point results verify each participant.

Both concrete and no-factory procedural inputs exercise eight partial failures per
profile: application failure, optional rollback failure and optional rejected-view
retirement failure. Rebuild and rejected-retirement hooks attempt scene/raw filter,
space reassignment, raw release and tree teardown in all participating worlds.
Original errors remain exact; rollback restores original private scene views,
repairs numerical geometry before queries and leaves ticks unchanged. Subsequent
real steps retain exact observer histories without duplicate effects. Successful
replacement publishes expanded geometry to all twelve roles; committed-retirement
handlers retain ordinary mutation access. These are logical observer-history
checks, not private-checkpoint validity through numerical geometry recompilation.

After 128 warm groups, 128 measured groups each execute 24 caller-buffer point
queries and three full physical intervals. Each real awake body moves 12.8 units
within .01, retains RID and delivers 128 force callbacks. Owner/all-thread managed
bytes remain strictly zero. Explicit copy/failure paths remain cold. Evidence is
retained under bin/physics-shared-data/2026-10-16/. Full custom/numerical/native/
foreign/network and hardware/window performance acceptance remains open.

The --shared-data fixture also exercises a fourth, unrelated CPU world containing
scene/raw Body/Area destinations. Status, geometry, rebuild, rollback and rejected
retirement hooks try enabled/disabled adds, replacement and borrowed-resource
owner/child bindings, including hierarchy entry of a prepared borrowed child.
Rejection preserves exact slot counts, old resource RIDs,
empty owner contents and child property identity. Detached participants reject
clear/enable/release and recursive shared data/free. Status/supplier errors retain
their exact identity; borrowed procedural reads retry after supplier failure.
Committed retirement can attach a new consumer. Existing 0/0 warmed full-interval
gates remain unchanged. Evidence: bin/physics-shape-participants/2026-10-16/.

A cold detached-only preflight runs before any world is attached. It rejects
scene writes/disposal and raw release from the supplier, then permits ordinary
mutation after the copy completes. Data borrowing keeps scene ownership checks
active even while the usual global attachment fast path is empty.

## Shared shape release

FreeRID preflights every owned shape user without invoking selected status hooks,
then borrows the resource's consumer membership and all participating worlds.
Status failure leaves geometry and slots intact. Once removal starts, every raw
and scene user and private geometry retirement is attempted; cleanup failures
retain their original identity and the RID is unregistered. The borrow continues
through retirement callbacks, preventing recursive release, incoming binding and
writes/transfer/free/teardown in another participating world. Unrelated worlds
retain ordinary authored access. Remaining numerical preparation failures keep
removed authoring dirty and retry before subsequent query or step publication.

The public-only --shared-free fixture shares one owned circle across CPU,
CPU/GPU and an independent provider. Twelve scene/raw Body/Area roles each have
one unrelated slot plus target slots 1/3 and a disabled gap. Both creation orders
and solver-thread policies exercise status failure followed by each of four
rebuild/retirement error combinations. Every original receiver observes exactly
one addition/removal at its original logical slot, without duplicate effects.
All matching slots disappear, the unrelated RID/index/geometry survives, held
private views retire once and stale RIDs reject. Rebuild deliberately poisons the
provider's partial geometry before failing; public queries verify repair.
After 128 warm groups, 128 measured groups run 24 caller-buffer point queries and
three full intervals at strict 0/0 owner/all-thread managed bytes. Awake motion,
RID and force-callback counts remain checked. Evidence is retained under
bin/physics-shared-free/2026-10-16/. Native/driver budgets, foreign execution,
network integration and hardware/window performance acceptance remain open.
