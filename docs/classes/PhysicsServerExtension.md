# PhysicsServerExtension

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

## Resource retirement errors

Caller-owned retirement attempts every required cleanup stage before propagating
failure. A lone error retains its original exception instance and throwing stack,
including selected Finish/disposal and the outer world release. Multiple errors
aggregate after all attempts. Freed RIDs and borrowed views expire even on errors;
preflight owner/borrow rejection preserves live resources. Scene-owned identities
remain with their owners. See [complete resource retirement operations](../components/physics-registered-adapter.md#complete-resource-retirement-operations)
for actual coverage and remaining limits.

## Joint lifecycle ownership

Selected creation/attachment errors retain validated server role, endpoints and
scene override for retry. A started clear commits Empty even when cleanup reports
errors; RID/common settings and original errors survive. Selected providers release
owned resources before propagating Detach failures. Input/borrow rejection preserves
the previous request; explicit scene edits may reclaim ownership.
See [complete lifecycle operations](../components/physics-registered-adapter.md#complete-joint-lifecycle-operations).

## Typed parameter publication

A selected publication error retains the requested typed parameter channel.
Equal setters, live world queries, local/portable capture and active positive
steps retry before numerical use. Configured reads and zero/inactive intervals
do not repair. Solver policy, pin limits, each motor channel and groove limits
retire their debt independently; a later channel failure does not replay an
already completed channel. Publication reentry rejects live preparation.
Detach/clear/free discards the old attachment's debt; reentry applies retained
configuration to a fresh attachment. Existing per-interval refresh remains separate.
See [complete joint parameter operations](../components/physics-registered-adapter.md#complete-joint-parameter-operations)
for actual scope, response checks and remaining acceptance limits.


## Collision exception publication

Explicit body exceptions and independently counted active-joint contributions
share one deduplicated input union. A failed joint collision-policy publication
retains the requested flag and complete common contribution, then retries before
a world query, local/portable capture or active positive step. An equal setter
also retries; configured flag and scoped exception-input reads do not repair.
Clear, free, endpoint departure and scene disposal remove only that attachment's
contribution and pending request. Reentry applies the retained flag to a fresh
attachment. The selected SetCollideConnected hook applies a complete value and
may be retried after a publication error.
See [complete exception operations](../components/physics-registered-adapter.md#complete-exception-operations)
for threading, response, snapshots, cleanup checks and remaining limits.


## Force operation family

All thirteen central/positioned/torque transient, impulse and persistent operations
share retained scene/raw authoring and selected numerical response. Persistent
facade getters can read prepared inputs in implementation hooks; direct-state
views keep their separate attachment and query-phase guards. Every force write
rejects foreign-thread, numerical-borrow and terminal-world access before mutation.
Scene ConstantForce/ConstantTorque setters apply the same terminal guard even for
zero and equal assignments. Configured scene reads remain non-preparing.
See [complete force operations](../components/physics-registered-adapter.md#complete-force-operations)
for the actual response, lifetime and failure checks and their remaining limits.


## Configured world parameter reads

The nine typed world-policy getters read common retained configuration without
selected numerical queries or pending publication. A borrowed implementation can
read those inputs on its world's owner or captured solver thread, including actual
Solve and failure-status hooks. Ordinary reads retain owner, lifetime and failed-world
guards. This permission does not allow policy writes, live space queries or access
from another thread. All setters keep their existing validation and recovery rules.
See [complete world parameter operations](../components/physics-registered-adapter.md#complete-world-parameter-operations)
for the exercised policy family and verification limits.

## Selected Area overlap publication

Selected monitoring combines scene observation with either server callback;
monitorability independently gates other receivers and never disables this Area's
own callback lanes. Overlap output uses live same-world RIDs and enabled global
logical indices passing the receiver mask and Area monitorability. Validation
finishes before replacing a receiver's committed history; duplicate numerical
pieces collapse into one logical pair. Exact compound/custom data, failed output,
callback continuation, identity, reentry and private correction execute through
[the public publication checks](../components/physics-registered-adapter.md#area-overlap-publication-operation-audit).
CPU/software-GPU/registered JIT/NativeAOT consumers cover both numerical policies
with strict zero warmed managed bytes; broader acceptance remains separate.

## Collider construction and reentry

BodyCreate/AreaCreate allocate fresh detached logical identities without a selected
numerical call. Binding invokes the world owner's factory with the engine RID and
body/Area role. Return a fresh non-null collider for every attachment: live and
retired instances remain permanently claimed and cannot be reused. Rejected
creation preserves common authoring and unrelated owners. Departure invalidates
the cached direct-body view; healthy reentry retains the RID with a fresh numerical
attachment. Failed live insertion remains structurally in the tree and explicitly
removable; failed whole-tree construction releases membership/readiness and permits
a healthy retry of the original root. See [the creation checks](../components/physics-registered-adapter.md#collider-creation-operation-audit)
for public CPU/software-GPU/registered JIT/NativeAOT, error and allocation scope.

Caller-derived scoped numerical implementation base inheriting the full protected PhysicsServerBackend family. Implement real body, joint, geometry, query, observation and history operations; the world supplies engine identities and owns lifetime.

See [registered backend contract](../components/physics-registered-adapter.md) for ownership, construction, verification and remaining acceptance.

The [complete force and axis family](../components/physics-registered-adapter.md#force-impulse-and-axis-response) uses common validated scene/raw authoring and selected numerical hooks. Shared CPU/GPU/registered public-only checks exercise actual response, lifetime, invalid inputs and 128 edited frames after collection at zero managed allocation. Full numerical-extension acceptance remains open.

Inherited `GetCollisionExceptions(RID)` provides scoped retained input. Body/joint exception authoring rejects numerical hooks before mutation; [shared conformance](../components/physics-registered-adapter.md#collision-exception-authoring-and-response) covers CPU/GPU/registered response, reads and 128 edited frames at zero managed allocation.


[Area pose and monitor publication](../components/physics-registered-adapter.md#area-pose-and-monitor-publication) uses selected collider/overlap operations with common retained identity and authoring. CPU/GPU/registered public-only checks exercise reset/reentry, failure continuation and 64 active transition frames at zero managed allocation; arbitrary registered geometry remains open.


[Contact reporting depth](../components/physics-registered-adapter.md#contact-reporting-depth) uses common retained scene/raw policy and selected signed PhysicsBodyContact.Depth. Both extension threshold operations execute; arbitrary registered capped contact topology and full numerical acceptance remain open.

Inherited public `IsBodyExcludedFromMotionTest(RID)` and
`IsObjectExcludedFromMotionTest(ulong)` expose guarded current-motion input
membership. Their complete contracts are documented on
[PhysicsServerBackend](PhysicsServerBackend.md#public-methods), with executable
CPU/GPU/registered conformance in [Motion-test exclusions](../components/physics-registered-adapter.md#motion-test-exclusions).

[Notification delivery status](../components/physics-registered-adapter.md#notification-delivery-status)
now shares world-scoped lifecycle state across CPU/GPU and registered implementations.
Body integration/state callbacks, contacts, Areas and sleep events are included;
exceptions and nested calls preserve the existing delivery/order/cleanup guarantees.


Inherited `GetShapeGeometry(RID)` provides exact immutable authored data for the
selected `CollideShapes` and `BodyCollideShape` hooks, now reachable through public
PhysicsServer pair queries. See [public pair dispatch](../components/physics-registered-adapter.md#public-shape-pair-dispatch)
for identity, atomic output, nested borrowing and verification limits.


[Motion result receiver identity](../components/physics-registered-adapter.md#motion-result-receiver-identity)
preserves the moving RID authored by SetCollision through selected-query validation.
A same-world result for another moving body rejects before caller publication;
no-result calls enforce the same rule. SetMotion misses retain normal requested-body
binding, and rejection restores scope for the next valid query. The numerical
motion/geometry acceptance remains broader than this identity guarantee.


[Registered lifecycle census](../components/physics-registered-adapter.md#registered-lifecycle-census)
now verifies initialization, local/global activation, complete interval order,
completed statistics, failure/cleanup paths and 128 moving full frames per scene/raw
role at zero managed allocation through a public-only independent implementation.
Eight former unmapped rows have their actual typed projections; complete numerical
extension and platform/performance acceptance remain open.


[Selected diagnostic preparation](../components/physics-registered-adapter.md#diagnostic-capacity-and-publication) passes the contact-point cap to the numerical owner before solving, including registered implementations. Zero disables capture and remains available for failed-world cleanup; synchronized point/count reads retain the bounded shared snapshot. Full numerical and platform acceptance remain separate.


[Registered factory-selected views](../components/physics-registered-adapter.md#factory-selected-direct-views) now execute through a separate public-only consumer: cache identity, an initially empty contact snapshot, synchronized immutable publication before callbacks, result/lifetime validation, recreation and zero warm managed allocations. Independent caller-created body views keep direct hook dispatch; wider numerical-extension acceptance stays open.

[Shape correction policy](../components/physics-registered-adapter.md#borrowed-shape-correction-policy)
uses common Shape metadata supplied in the selected collider descriptor. Reads are
available during numerical borrowing; policy writes preflight every scene/raw
borrower before mutation. Public-only CPU/GPU/registered owner/borrow/reentry checks
and zero-allocation full frames execute. Arbitrary registered numerical correction
and complete extension acceptance remain open.

[Shared shape factories and data lifetime](../components/physics-registered-adapter.md#shared-shape-construction-and-data-lifetime) now execute through the
public-only registered/CPU/GPU consumer. Replacement errors preserve original
geometry, continue every rollback/retirement and retain all failures; incomplete
borrowers retry during guarded preparation. Ten formerly unmapped extension rows
project to the common resource role as Partial. Full numerical-extension and final
physical/network/performance acceptance remain open.

[Body construction and continuous policy](../components/physics-registered-adapter.md#body-construction-and-continuous-policy)
now share selected CPU/GPU/registered ownership, detached/live modes, automatic
mass reset and guarded typed configuration. Valid CCD writes avoid enum metadata
allocation after collection; the public-only consumer checks 128 edited full
steps at 0/0 managed bytes. Registered numerical CCD and full acceptance remain open.

[Registered pointer delivery](../components/physics-registered-adapter.md#registered-pointer-delivery)
now verifies scene Body/Area policy through selected point queries, numerical
guards and 128 changed-policy full frames at zero managed allocation. Native
CPU/GPU/registered worlds each pass with both renderers and target pixel checks;
full registered geometry and final acceptance remain open.

[Registered rectangle numerics](../components/physics-registered-adapter.md#registered-rectangle-numerics)
now exercise transformed rectangular regions in the independent provider through
scene/raw queries, contacts, mass, CCD and replay. This is executable evidence for
its owned implementation boundary; broader shape families and numerical acceptance
remain open. The runtime declaration family and signatures are unchanged.

[Registered capsule numerics](../components/physics-registered-adapter.md#registered-capsule-numerics) now extend the independent public-only provider with rounded segments,
analytic area/inertia, side and cap queries, margin-preserving sweeps, contacts and
linear/angular CCD. Shared CPU/software-GPU serial/separate profiles retain full
frame impulses and silent replay at zero warmed managed bytes. Wider geometry,
coupling, inherited/network/native/hardware and performance acceptance remain open.
Production signatures and the accepted geometry/lifetime decisions are unchanged.

[Registered segment numerics](../components/physics-registered-adapter.md#registered-segment-numerics)
now execute two-sided line contours, length-weighted zero-area mass, queries,
contacts and linear/angular CCD in the independent public-only provider. Shared
CPU/software-GPU serial/separate checks retain full impulses, silent private replay
and zero warmed managed allocation. Wider contours, extension/inherited/network/
native/hardware and performance acceptance remain open.

[Registered convex numerics](../components/physics-registered-adapter.md#registered-convex-numerics)
now preserve whole accepted convex contours in the independent public-only provider,
with signed projections, centroid/inertia, queries, contacts and linear/angular CCD.
Shared CPU/software-GPU serial/separate profiles retain zero warmed managed bytes.
Asymmetric/winding/live-empty and twelve-vertex controls extend evidence; concave,
boundary/ray, inherited/network/native/hardware and final acceptance remain open.

[Registered concave numerics](../components/physics-registered-adapter.md#registered-concave-numerics)
now preserve paired hollow edges, one logical shape slot, rod mass, queries,
contacts and linear/angular CCD in the independent public-only provider. Shared
CPU/software-GPU serial/separate profiles retain zero warmed managed bytes, with
two corner features and one logical event. Boundary/ray, inherited/network/native/
hardware and final acceptance remain open.


[Registered infinite-boundary numerics](../components/physics-registered-adapter.md#registered-boundary-numerics)
now exercise analytic planes in the independent public-only provider, all six
query families and reverse query source, Area/contacts, zero geometric mass,
material/surface impulses, private replay, sleep/lifecycle and translating/rotating
CCD. Twenty full-frame brackets per CPU/GPU/provider serial/separate profile
require strict 0/0 warmed managed bytes. CPU authored body/query/joint angles now
use ordinary sine/cosine; the far rotated query retains its .005-unit bound rather
than absorbing the former approximate-angle drift. Directed-ray, arbitrary/coupled,
complete inherited/extension/network/native/platform and hardware-performance
acceptance remain open; coverage states are unchanged.


[Registered directed-ray numerics](../components/physics-registered-adapter.md#registered-directed-numerics)
now execute surface entry, axis/slope separation, zero geometric mass, all query
families and reverse source, raw recovery and character snap, material/surface
impulses, silent replay and linear/angular CCD through an independent public-only
provider. Twenty-three full-frame brackets per CPU/GPU/provider serial/separate
profile require 128 warmup/128 measured frames at 0/0 managed bytes. Directional
response and stable long-ray/circle roots are corrected in the shared consumer.
Arbitrary custom/coupled, complete inherited/extension/network/native/platform
and hardware-performance acceptance remain open; coverage states are unchanged.


[Registered priority recovery](../components/physics-registered-adapter.md#registered-priority-recovery)
now exercises weighted motion recovery independently through the public API,
including finite/directed families, bounded contact planes, exclusions, one-way
controls, character movement and unchanged rigid momentum. The shared CPU/GPU/
registered profile covers five 128-warmup/128-measured full-frame brackets at zero
managed allocation. Wider registered contact-cache settings and arbitrary coupled correction and complete numerical,
native/network/platform/performance acceptance remain open.


[Registered contact numerics](../components/physics-registered-adapter.md#registered-contact-numerics)
now exercises actual world/shape bias and penetration slack, complete contact/joint
sweeps, full physical impulses and identity-preserving live policy through the
public-only provider. Seven source families, dense contacts, CCD durations and
contact/pin convergence use nineteen complete 128-warmup/128-measured brackets
at zero managed allocation. Registered contact-cache, arbitrary coupled/custom,
full inherited/extension/network and native/platform/performance acceptance remain
open.

## Pending server geometry

The shared factory/type/data family preserves unconfigured raw RIDs. The selected implementation receives inactive logical slots until ShapeSetData commits and GetShapeGeometry rejects pending query sources. See [raw shape initialization](../components/physics-registered-adapter.md#raw-shape-initialization).


The three selected debug-space operations now have a complete bounded
[capacity/point/count audit](../components/physics-registered-adapter.md#complete-diagnostic-contact-operations),
including actual scene/raw contacts, terminal rejection/cleanup, owner/separate
execution, validated count bounds and rendered marker consumers. Preparation errors
preserve prior implementation policy; tree-wide rollback attempts every request and
retains original errors. This closes those operations, not the whole numerical family.
