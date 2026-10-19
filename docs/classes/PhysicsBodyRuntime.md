# PhysicsBodyRuntime

Last updated: 2026-10-19

## Contact reporting policy guards

Configured depth reads remain non-preparing during selected hooks and actual Solve.
Numerical write preflight rejects equal/changed assignments before selected status
observation, scene participation checks or pending body preparation. The common
body guard supplies this order across scene/raw roles. Reporting changes preserve
completed snapshots, explicit sleep and physical response; the next publication
uses the retained scene-unit threshold before caps, with equality eligible.
See [complete reporting threshold operations](../components/physics-registered-adapter.md#complete-reporting-threshold-operations)
for actual verification and remaining limits.

[Gravity publication and terminal parameter guards](../components/physics-registered-adapter.md#gravity-publication-and-terminal-parameter-guards) retry the latest effective native gravity/omission request after a selected error and discard it on attachment retirement. Scene damping, omission and inherited material setters reject failed worlds before changing authoring while healthy post-solver callbacks retain writes. Public CPU/software-GPU/registered JIT/NativeAOT profiles verify real motion, history boundaries and strict zero managed-byte frames; wider acceptance remains open.

[Mass profile publication recovery](../components/physics-registered-adapter.md#mass-profile-publication-recovery) preserves preceding authoring after selected mass/inertia/center/reset errors and prepares it before numerical use. Cached views, space/force/checkpoint/portable boundaries, attachment retirement, in-flight rejection and post-commit notification behavior execute across all scene/raw roles. Public JIT/strict NativeAOT CPU/software-GPU/registered full-frame checks retain strict zero managed allocation; wider native/platform/network/performance gates remain open.

## Prepared body view acquisition

Body view acquisition now finishes pending direct-mode preparation before reading resolved fields, selecting a cached view or invoking its selected factory. The same boundary serves caller-created body-state extensions. A repeated preparation error creates no view, publishes no fields and does not advance the tick. Successful recovery preserves cached identity or creates one generation-qualified view; stale attachments remain rejected. See [the preparation contract](../components/physics-registered-adapter.md#body-view-acquisition-after-mode-failure).

**Declaration:** `internal sealed partial class PhysicsBodyRuntime`

**Source:** [PhysicsBodyRuntime.cs](../../src/Servers/Physics/PhysicsBodyRuntime.cs),
[forces](../../src/Servers/Physics/PhysicsBodyRuntime.Forces.cs),
[parameters](../../src/Servers/Physics/PhysicsBodyRuntime.Parameters.cs),
[direct-view adapter](../../src/Servers/Physics/PhysicsBodyRuntime.View.cs).
**Component:** [Physics server and queries](../components/physics-queries.md).

[GPU publication for current consumers](../components/gpu-demand-publication.md) now keeps unobserved raw bodies on device. Explicit getters select one current snapshot; scene consumers, live raw views, contact receivers and kinematic completion retain fresh data. Portable preparation batches its explicit state demand. Cached values are qualified by the completed interval epoch; callback ordering and failure/lifetime guards remain unchanged.

## Ownership and flow

PhysicsServer keeps one runtime per body RID. A scene owner is weakly referenced;
a server collider is retained until RID release. Owners resolve to the current
attachment. The runtime retains an attachment adapter and its monotonically increasing version
for each cached direct view. It validates the current view, world and attachment
version and clears the cached adapter on detach. It contains no vendor body/world
IDs, mass records or contact traversal. Reattachment on the same RID
and space cannot revive an old view.
The runtime shares mass/material/field profiles, pending and constant forces,
contact limits, callbacks and live state between scene and server operations.
It is an internal implementation; applications use PhysicsServer, PhysicsBody
subclasses and PhysicsDirectBodyState.

## Internal operations

| Operation/state | Contract |
| --- | --- |
| `Owners`, `Space`, `Backend` | Resolve the live scene/server owner and its attachment adapter. |
| `GetTransform`/`SetTransform`, velocity/sleep pairs, `SetAxisVelocity`, `RestoreSceneState` | Shared typed state, non-preparing reads, numerical write guards, static-support wakeup and role-specific retained configuration; disabling sleep wakes detached/live direct dynamic scene profiles. |
| `EnsureMutable`, `GetView`, `InvalidateView`, `ValidateView` | Enforce world phase/current attachment identity; replace a disposed view and invalidate all older attachments. |
| `View*`, `CaptureViewContacts` | Forward live state/contact capture through the validated attachment adapter; retain no concrete solver references. |
| `ApplyMassProfile`, `SetMassProfile`, `SetAttachedMassProfile`, `MassProperties` | Share profile validation and scene projection; retain mass, inertia and center in scene units. Detached calls reuse PhysicsMass.Geometry; attached compilation belongs to the adapter. |
| `ApplyBeforeStep`, pending/constant force and torque | Preserve common activity/reporting gates, then dispatch actual force preparation to the selected collider. |
| Parameter operations and observed gravity/damping | Retain shared authoring and published totals; concrete CPU/GPU implementations apply field integration. |
| `GPUParametersDirty`, authored force/field/omission setters | Mark GPU integration edits; clear only after successful attachment synchronization, preserving authored values including signed zero. |
| `ApplyImpulse`, `AddForce`, velocity/mass access | Preserve dynamic-role, lock, finite-value and detached-state behavior. |

Virtual surface velocity is applied by PhysicsColliderBackend and preserves the
existing scene/server stored values, role policy and wake behavior. It does not
contribute to pose integration or actual kinematic subdivision distance.

Resolved gravity and damping live once in this runtime for both RigidBody and raw
server bodies. RigidBody retains authored settings; its GetGravity and direct-state
field views read these resolved values. Custom integration skips automatic field
forces but still observes changed fields and wakes an eligible dynamic body.
Frozen, disabled-static and inactive physical-bone roles retain their motion gates.
Validation completes before publishing field values or applying motion changes.

## Verification

PhysicsBodyStateTests covers current-view identity, repeated same-space scene/server
reattachment, caller-disposed view replacement, stale writes, callbacks and warmed
allocation. PhysicsParallelTests exercises contact publication for 288 bodies over
256 fixed steps with retained workers. The state/surface/contact suites also pass
through the existing GPU stage host; this is not independent GPU acceptance.

PhysicsSurfaceVelocityTests covers surface contacts, query snapshots, character
carry, inherited target motion, owner/phase/lifecycle rejection, packing and warmed
allocation. Existing mass/force/field/direct-state suites cover the other runtime
profiles through their public APIs. Native allocation and foreign-device acceptance
remain separate from the checked Linux/.NET 10/Vulkan behavior.

The [typed state partial](../../src/Servers/Physics/PhysicsBodyRuntime.State.cs)
shares transform, linear/angular velocity, sleep, automatic sleep policy and axis
projection across scene/server owners and direct views. Non-rigid scene contact
velocity is distinct from character movement input and restored at attachment.
PhysicsServerStateTests covers CPU/GPU behavior and warmed allocation.

ContactLimit accepts zero through 4095 for both scene and raw owners. The common
limit constant keeps their validation aligned; reconfiguration clears retained point
counts before the next solve. Selected object/shape monitoring follows direct-state
point selection. PhysicsContactImpulseTests covers boundary and replacement behavior.

Queued callback entries also use adapter identity plus attachment version, so
removal/reentry on the same RID and world cannot consume the prior attachment's
callback. PhysicsBodyStateTests explicitly replaces both a later scene body and a
raw server body during an earlier callback, then verifies only their next-frame
callbacks execute and both older direct views stay invalid. Independent GPU worlds
now use this runtime through the selected attachment adapter; complete conformance
remains tracked by the [physics audit](../components/physics-contract-audit.md).


## Continuous collision policy

The shared public CCDMode now supplies the scene/server setting and resident GPU
configuration. CPU worlds inspect solved motion before publication and preserve
remaining tick time and nominal force/joint budgets through impact continuations.
The runtime stores policy across attachments and roles. Shared policy edits mark
resident GPU configuration for synchronization before the next pre-step snapshot.
[Host preparation tests](../components/gpu-host-preparation.md) exercise live CCD,
force, damping and role edits through explicit public CPU/GPU worlds.
See [implementation and verification](../components/cpu-continuous-collision.md).

[Report-only contacts](../components/physics-report-only.md) now include kinematic/static and kinematic/kinematic pairs on CPU/GPU when either endpoint enables reporting. Impulses and positional response are zero; two static bodies remain ineligible. Caps, captured owners/indices, frozen-body scene events and checkpoint metadata share the existing contact contract.

[Caller-created body-state extensions](../components/physics-body-extensions.md) now execute the complete typed state/force/space/contact family. Body context preparation is independent of cached view acquisition and qualified by current attachment generation; nested hooks borrow body/world lifetime and preserve ordinary live mutation. Contact projection uses immutable engine-validated PhysicsBodyContact values. Registered server factories and factory-returned scene/server callback integration remain open under ADR 0103.

## Selected integration preparation

ApplyBeforeStep now owns only common contact-reporting, role/activity gates and observed pre-step activity before dispatching PrepareDefaultForces to the selected attachment. CPU field resolution has moved to its collider implementation; authored policy and observed gravity/damping totals remain shared. [Contract and checks](../components/physics-backends.md#selected-body-integration-preparation).

## Selected fields, exceptions and portable observation

PublishFields validates complete immutable [PhysicsBodyFields](PhysicsBodyFields.md) values before assignment. It borrows body/world lifetime and rejects recursion or nonfinite fields. Completed-step publication uses a separate owner-only solved phase while public queries remain closed.

## Factory contact publication

A factory-returned cached body extension now projects its contact count/values on the world owner after native capture and before scene/raw observer delivery. Complete validated snapshots commit atomically; cached getters retain immutable data and do not reinvoke contact hooks. Independent caller-created views keep ordinary guarded direct hook dispatch. Qualified publication permits immutable PhysicsBodyContact identity construction while ordinary solver queries/mutation stay blocked. Bad receivers retain their prior published state while other callbacks and committed ticks continue. [The executing contract and verification](../components/physics-body-extensions.md#selected-contact-publication) retain public registration/native/platform/performance limits.

## Value-only integration preparation

[Force and policy inputs](../components/physics-backends.md#value-only-body-integration-inputs) now separate engine-owned authoring/command state from numerical preparation. Default forces use PhysicsBodyForceConfiguration; CPU fields and GPU parameters use PhysicsBodyIntegrationConfiguration. Pending command retirement and GPU dirty flags stay in the binding layer; numerical methods receive finite authored values and return complete field/preparation results. CPU/GPU/no-device, replay, parameter and full operation checks remain the acceptance boundaries; public registration executes through the selected adapter; wider acceptance remains open.


Retains scene/raw ContactsReportedDepthThreshold independently of numerical storage. It survives detach/reentry and participates in immutable checkpoint configuration and portable schema. Publication filters signed scene-unit candidate depths; changing policy preserves the completed snapshot.

Prepared authoring reads can bypass the scene owner check only inside this world's exact authorized numerical implementation scope. Live state reads, authoring and lifetime operations keep their original owner/phase guards. Ordinary threads and foreign attached bodies gain no access. The [public-only consumer](../components/physics-thread.md) exercises mass/geometry/exclusion reads and write/query/lifetime rejection.

[Body-pair publication recovery](../components/physics-shape-pairs.md#body-pair-publication-recovery-and-composed-pose-validation)
validates the slot/resources, then prepares that body's pending motion/mode/pose
before selected pair dispatch. The composed pose validates before that hook. Errors preserve caller points/count and the tick;
resource-only queries remain independent, and later kinematic targets retain their
step boundary. Expanded public profiles keep strict zero-allocation complete
frames and the existing GPU pose-only transfer boundary. Full acceptance is separate.


[Complete axis velocity](../components/physics-registered-adapter.md#complete-axis-velocity-operation)
shares scene/server current-motion projection, preserving unsynchronized perpendicular
impulses and inactive native future configuration. Exact configured values, bounded
live error, nine body owners/four direct modes, publication recovery and 1536 complete
warmed frames at 0/0 owner/all-thread managed bytes pass in the 58-profile
JIT/NativeAOT CPU/software-GPU/registered cohort. This closes the extension axis
operation; broader numerical, networking and hardware acceptance remain open.
