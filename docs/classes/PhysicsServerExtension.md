# PhysicsServerExtension

Last updated: 2026-10-14

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
