# PhysicsServerExtension

Last updated: 2026-10-12

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
