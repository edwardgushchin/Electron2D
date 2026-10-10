# Caller-created body-state extensions

Last updated: 2026-10-10

## Responsibility and types

[PhysicsDirectBodyStateExtension](../classes/PhysicsDirectBodyStateExtension.md) inherits the real live body-state API and binds a current scene/raw body attachment. The complete pinned 48-operation family maps to 38 typed hooks; eleven contact value/object getters share one immutable [PhysicsBodyContact](../classes/PhysicsBodyContact.md) result hook. No capability is dropped by that typed projection.

## Runtime flow and invariants

Construction prepares the selected body's current geometry/state and records its attachment generation. Cached built-in view disposal does not retire a separate extension. Detach, transfer, free and same-world reentry permanently invalidate the old context. Owner/solver/failure guards execute before user code and validate the context again after it returns.

Public inherited scalar properties, force/impulse operations, sleep, layers/masks, point velocity, integration and direct-space access invoke their own hooks. The library rejects nonfinite inputs before invocation, validates finite output vectors/scalars, nonnegative inverse mass/inertia/step, rigid transforms, bounded contact count and matching live space view. Signed finite damping is preserved. Static lambda dispatch caches callbacks outside warmed intervals; no closure or boxing is created per call.

Nested operations borrow current body/world lifetime. Sealed view-disposal validation and shared body membership preflight prevent disposal, transfer or release before mutation. Scene hierarchy/node disposal also preflights before lifecycle/child-order changes. Active recursive stepping, world rebinding/release and checkpoint capture/restore reject until finally releases the hook. Live velocity/force/filter/pose mutation remains available. Ordinary built-in integration callbacks retain their existing lifecycle policy.

PhysicsBodyContact construction validates live same-world body RIDs/logical slots, finite geometry/velocities/impulse and samples collider weak object association plus observed-body attachment generation. Retained values, shape indices and collider ID survive later geometry/rebind/free; expired object targets become null. A default, wrong-body or previous-generation contact cannot enter the current view. Geometry, impulse accumulation and contact selection remain the implementation's responsibility; complete built-in physics stays unchanged.

## Verification and limits

[The separate public-only consumer](../../tests/PhysicsBodyExtension.Consumer/PhysicsBodyExtension.Consumer.csproj) implements every hook and verifies a hook doubles a real impulse over a two-kilogram raw body. Its scene fixture captures actual solved floor contacts and projects every retained contact field. It covers full hook reachability, inherited setters/forces/state, finite/identity/count/space rejection, nested calls/failure, borrowed release/binding/hierarchy/step/checkpoint guards, owner thread, cached-view disposal, raw transfer/reentry/free, scene reentry and previous-generation contact rejection. Identity/snapshot values are exact; physical velocity/force checks use .001 scene-unit tolerances.

Release fixtures warm all operations for at least one second in 64-cycle batches after collection, then measure 64 full unchanged cycles at zero owner/all-thread managed bytes. User allocations and structural/cold paths remain outside that interval. CPU, GPU and CPU without display/GPU availability are separate profiles. Native allocation, foreign platforms and new full-step/window performance measurements remain separate gates.

Caller-created views are implemented. Registered backend factories do not yet return these body-state views to scene/server callbacks; class coverage remains Partial. Server backend registration/operation and custom Shape families remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).
