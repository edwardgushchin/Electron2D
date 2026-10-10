# PhysicsColliderBackend

Last updated: 2026-10-10

**Declaration:** `internal sealed partial class PhysicsColliderBackend`

**Source:** [PhysicsColliderBackend.cs](../../src/Servers/Physics/PhysicsColliderBackend.cs),
[live state](../../src/Servers/Physics/PhysicsColliderBackend.State.cs),
[dynamics](../../src/Servers/Physics/PhysicsColliderBackend.Dynamics.cs),
[surface motion](../../src/Servers/Physics/PhysicsColliderBackend.Surface.cs),
[contact projection](../../src/Servers/Physics/PhysicsColliderBackend.Contacts.cs)
**Component:** [Scene physics bodies](../components/physics-bodies.md)

## Responsibility and ownership

One retained component owns the current backend body, world attachment and shape
ID collection for a scene CollisionObject or a server-created collider. PhysicsBody,
Area and PhysicsServerCollider share it. It owns either CPU storage or an independent
resident GPU attachment according to the selected world; vendor handles remain
internal to the adapter.

The component keeps the stable public collider RID and a weak scene owner. Raw
server colliders have no scene owner. Logical shape indices come from the owning
slot registry; compound backend pieces retain that one index. Caller-owned Shape
resources are borrowed during rebuild and are never disposed here.

[Authored GPU integration edits](../components/gpu-host-preparation.md) synchronize
before the world's shared pre-step snapshot. Prepared status belongs to one
attachment and is invalidated by role/lock/gravity changes and replay. Direct
transform reads reuse the qualified GPU basis while CPU keeps its angle convention.

## Operations and invariants

| Operation | Contract |
| --- | --- |
| `Attach` | Accept scene-unit pose and PhysicsBodyConfiguration; apply backend unit conventions internally, create the CPU or GPU body and commit space ownership with a fresh AttachmentVersion. Reject a second attachment; a prechecked version increment cannot wrap. |
| `HasMotionMode`, `SetMotionMode` | Compare/change the existing engine body mode through the backend; motion matching is independent of angular locking. |
| `GetPose`, `SetPose`, `SetTargetPose` | Transfer engine-unit position/rotation and derive kinematic target motion; cached backend references live only for the current attachment. |
| `GetTransform` | Read the current GPU basis directly or preserve the CPU published-angle convention; shared by server/direct/scene pose consumers. |
| `GetSolverMotion`, velocity/sleep accessors | Return raw integrated velocity for scene publication or combined contact velocity for server state, retaining virtual surface semantics. |
| `SavePose`, `RestorePose` | Keep a private exact backend pose for frozen kinematic query restoration; avoid decoded-angle round trips while idle. |
| Velocity, gravity, sleep and rotation setters; force/torque application | Convert units in one adapter while the scene/server caller retains validation and role policy. |
| `CanvasInstanceID` | Authored 64-bit point-query association, retained across fixture rebuild and server reattachment. Scene canvas notifications replace it; zero denotes the default canvas. |
| `AttachmentVersion` | Monotonic per-collider attachment epoch used by common views and queued callbacks, independent of native handle reuse. |
| `ApplyMassProfile`, COM/inverse getters | Compile current fixtures, retain reusable CPU mass scratch and return neutral scene-unit mass properties. |
| `ApplyImpulse`, `ApplyFieldMotion`, `ClearTransientForces` | Preflight CPU numeric candidates, apply already-resolved body policy and preserve current force/omission/wake ordering. |
| `SetSurfaceVelocity`, `WakeTouching`, `GetPointVelocity` | Keep virtual surface motion, support wakeup and center-aware point velocity behind the attachment boundary. |
| `CaptureViewContacts`, `CaptureViewContact` | Project CPU solved/frame contacts or resident GPU reports into existing engine contact values; common runtime contains no fixture tags or backend contact records. |
| `Detach` | Destroy the body and its fixtures, clear IDs and space, retain list capacity. If the world has failed, skip backend calls and leave final backend cleanup to world disposal. |
| `RebuildShapes` | Validate all active slot transforms before removing existing fixtures; preserve disabled/disposed-slot indexing and append the current active geometry. |
| `UpdateFilter` | Update existing CPU/GPU fixture metadata, preserve geometry identity and wake body/contact neighbors when requested; query/failed-world guards precede mutation. |
| `Shapes`, `BodyID`, `Space` | Internal borrowed backend state; shape IDs can change on rebuild, body IDs on reattachment. Public RID identity is independent. |

Scene and server slot overloads use one shared shape-definition/append path. It
installs category/mask bits, sensor/density policy, surface material, one-way
metadata and exception pre-solve eligibility. Shape resources supply their existing
borrowed scene-unit geometry through PhysicsShapeBackend. Definitions are temporary locals; the component does not retain
a large per-collider shape-definition snapshot. Mass updates are deferred until
the owning body applies its complete profile; Areas remain massless sensors.

Callers preserve their owner-thread/phase guards, body-view invalidation, joint
release, Area reset, solved-pose capture and authored dirty/revision state. These
operations occur around attachment/destruction in their existing order. The
component does not introduce callbacks, asynchronous work or a second world copy.
Geometry, query, joint and world methods still have other backend-specific
dependencies recorded by the [physics audit](../components/physics-contract-audit.md).

## Verification

`ELECTRON2D_TEST_COLLIDER_BACKEND=1` runs the existing body/Area, shape-family,
polygon/one-way/owner, material/mass, server-slot, monitoring/fields, disable-mode,
body-view, joint, World, platform/character/freeze and physical-bone tests together. They cover real solver/query results,
identity, reentry, malformed-transform recovery, borrowed lifetime and their
existing warmed allocation checks. PhysicsBodyStateTests also verifies velocity
writes made by synchronous solved-pose notifications: motion is sampled after
publishing the pose, so those writes survive in both scene and direct state.
PhysicsParallelTests checks publication for 288 bodies over 256 fixed steps and
serial/parallel transitions. The GPU suite exercises the current stage
host and failed-step/world teardown. Neither suite establishes a standalone GPU
implementation, network replay, foreign-platform acceptance or a throughput gain.

The grouped collider checks additionally run server forces, typed state, body
parameters, surface velocity and full contact impulse tests. They exercise both
attached adapter operations and detached neutral mass geometry. This extraction
changes no public declaration and is not independent GPU selection; that binding
must still connect the resident store to the common world and query/event flow.


The 2026-10-09 extraction passes all 37 grouped CPU suites, the independent/stage-hosted
GPU suite and the 288-body/256-step parallel contact check. The grouped CPU suite
was rerun after making attached and detached mass scratch lazy and retained.
Existing force/field/view/surface/contact warmup assertions retain zero owner-thread
managed allocations. No new throughput, native-allocation or cross-platform claim
is inferred from the refactor. Source inspection verifies that PhysicsBodyRuntime
partials and the callback-state partial contain no concrete vendor types or calls.


## Complete convex motion geometry

Body-motion recovery, initial penetration and impact geometry use a complete convex
contour when a shape exceeds the backend piece limit. Directed queries reject ray
origins inside the complete contour or its swept region before selecting pieces.
The fixture tag borrows the contour weakly; shared resource collision scratch stays
allocation-free after warmup. PhysicsMotionTests covers both polygon roles and
both directed-containment directions. See [body-motion verification](../components/gpu-resident-motion-queries.md).

Attach reads the current space's linear quiet threshold when creating its CPU body.
World angular threshold and quiet duration are shared by existing/new attachments;
[PhysicsSleepPolicyTests](../../tests/Electron2D.Tests/PhysicsSleepPolicyTests.cs)
checks settings, body-size independence and scene/server behavior.

Contact-policy preparation compares Shape.SolverPolicyEpoch separately from geometry. Weak fixture sources refresh per-shape bias without shape IDs, mass or history changes; changed policy wakes the body and touching neighbours. Body, Area and server-collider preparation share this path.

CollisionPriority retains the shared scene/server recovery weight (default one)
across Attach/Detach and role edits. SetCollisionPriority validates finite positive
input and the current world's owner/step boundary. No fixture rebuild, body wake
or vendor state edit is required; motion candidates capture the current value.


ObjectIdentity retains the authored weak instance association independently of the physical scene owner and body/fixture lifetime. AttachObject preserves solver state and validates before replacement. External Node membership observation exists only while attached. See [object associations](../components/physics-object-bindings.md).

## Independent GPU attachment

The same adapter now owns either CPU fixtures or resident GPU body/shape handles,
selected by its PhysicsSpace. GPU paths retain RID/object/canvas and logical shape
identity, mass/policy/force state, sampled direct contacts and attachment versions.
A 64-byte observable cache serves scene/server getters; shared epoch invalidation
avoids a per-edit scan. Immediate wake publication uses the solved device graph.
See [shared backend flow and limits](../components/physics-backends.md).

Linear and angular setters now use independent resident command components, avoiding
`GetSolverMotion` readback solely to preserve the untouched value. They still
invalidate the shared GPU snapshot epoch and queue wake propagation. Actual reads,
axis projection and other observation paths keep their existing freshness behavior.
The CPU path and public signatures are unchanged. [Component-write checks](../components/gpu-resident-bodies.md#component-velocity-writes)
cover scene/server and direct-state consumers plus custom integration callbacks.

The GPU cache also tracks whether it matches the world's latest change publication.
Selected reads after authored edits are not automatically revalidated by a later
empty publication; the next observation refreshes them. Matching-epoch selected
reads regain compatibility without adding a second state copy. RotationLocked reads
validated authored resident role/policy metadata, avoiding a pose readback during
ordinary preparation. [Publication tests](../components/physics-backends.md#conditional-body-publication)
cover intermediate reads restored to the prior final pose.

GPU parameter preparation validates and resolves the runtime's owner once, then
uses that same scene role for omission and persistent force/torque values. For
non-rigid owners, rotation locking reads the current attachment directly. Existing
GPU handle/access validation and per-step authored parameter sampling remain;
this removes repeated owner resolution without a new cache or changed public API.
See [parameter preparation measurements](../components/physics-backends.md#parameter-owner-resolution).

[Directional filter mutation](../components/physics-filters.md) shares scene/server/tile metadata and query rules across CPU/GPU; it updates existing fixtures, preserving shape/resource identity. Body assignments wake the body and contact neighbors, including unchanged bits; Area snapshots update on the next step.

[Report-only contacts](../components/physics-report-only.md) now include kinematic/static and kinematic/kinematic pairs on CPU/GPU when either endpoint enables reporting. Impulses and positional response are zero; two static bodies remain ineligible. Caps, captured owners/indices, frozen-body scene events and checkpoint metadata share the existing contact contract.

Idle kinematic velocity clearing and unchanged targets do not generate a wake
when real motion is already zero. Actual platform movement/stopping still wakes
contacts, while explicit assignments retain their wake and ordering semantics.
See [resident kinematics](../components/gpu-resident-kinematic.md#idle-platform-wake-policy).
