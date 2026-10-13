# Caller-created body-state extensions

Last updated: 2026-10-13

## Responsibility and types

[PhysicsDirectBodyStateExtension](../classes/PhysicsDirectBodyStateExtension.md) inherits the real live body-state API and binds a current scene/raw body attachment. The complete pinned 48-operation family maps to 38 typed hooks; eleven contact value/object getters share one immutable [PhysicsBodyContact](../classes/PhysicsBodyContact.md) result hook. No capability is dropped by that typed projection.

## Runtime flow and invariants

Construction prepares the selected body's current geometry/state and records its attachment generation without acquiring, reopening or replacing its cached built-in view. The ordinary server getter uses the same preparation before explicit view acquisition. Cached built-in view disposal does not retire a separate extension. Detach, transfer, free and same-world reentry permanently invalidate the old context. Owner/solver/failure guards execute before user code and validate the context again after it returns.

Public inherited scalar properties, force/impulse operations, sleep, layers/masks, point velocity, integration and direct-space access invoke their own hooks. The library rejects nonfinite inputs before invocation, validates finite output vectors/scalars, nonnegative inverse mass/inertia/step, rigid transforms, bounded contact count and matching live space view. Signed finite damping is preserved. Static lambda dispatch caches callbacks outside warmed intervals; no closure or boxing is created per call.

Nested operations borrow current body/world lifetime. Sealed view-disposal validation and shared body membership preflight prevent disposal, transfer or release before mutation. Scene hierarchy/node disposal also preflights before lifecycle/child-order changes. Active recursive stepping, world rebinding/release and checkpoint capture/restore reject until finally releases the hook. Live velocity/force/filter/pose mutation remains available. Ordinary built-in integration callbacks retain their existing lifecycle policy.

PhysicsBodyContact construction validates live same-world body RIDs/logical slots, finite geometry/velocities/impulse and samples collider weak object association plus observed-body attachment generation. Retained values, shape indices and collider ID survive later geometry/rebind/free; expired object targets become null. A default, wrong-body or previous-generation contact cannot enter the current view. Geometry, impulse accumulation and contact selection remain the implementation's responsibility; complete built-in physics stays unchanged.

## Verification and limits

[Binding ownership checks](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.ExtensionBinding.cs) observe the protected constructor before the consumer explicitly acquires an ordinary view. Scene/raw bodies start without a cached view; selected geometry and dirty authored mass are already prepared. A disposed cache remains disposed through binding. Separate extensions retain real impulse access through ordinary view reopening and reject off-owner, detached and stale attachments. CPU solver-lock and CPU/GPU failed-world checks also reject construction. These checks run in CPU, GPU and no-device ownership profiles.

[The separate public-only consumer](../../tests/PhysicsBodyExtension.Consumer/PhysicsBodyExtension.Consumer.csproj) implements every hook and verifies a hook doubles a real impulse over a two-kilogram raw body. Its scene fixture captures actual solved floor contacts and projects every retained contact field. It covers full hook reachability, inherited setters/forces/state, finite/identity/count/space rejection, nested calls/failure, borrowed release/binding/hierarchy/step/checkpoint guards, owner thread, cached-view disposal, raw transfer/reentry/free, scene reentry and previous-generation contact rejection. Identity/snapshot values are exact; physical velocity/force checks use .001 scene-unit tolerances.

Release fixtures warm all operations for at least one second in 64-cycle batches after collection, then measure 64 full unchanged cycles at zero owner/all-thread managed bytes. User allocations and structural/cold paths remain outside that interval. CPU, GPU and CPU without display/GPU availability are separate profiles. Native allocation, foreign platforms and new full-step/window performance measurements remain separate gates.

The internal [selected-owner factory route](physics-backends.md#selected-direct-state-construction) now returns these views to actual scene integration and raw force/sync callbacks, retaining real completed contacts and one cached attachment identity. Factory publication rejects wrong/stale/disposed/null contexts and borrowing prevents recursive creation or release. Exceptions before solving leave the tick unchanged; late factory errors permit later callbacks. CPU/GPU/no-device ownership fixtures measure 64 warmed steps at 0/0 managed bytes.

Caller-created views are implemented. Registered factories and the typed operation bridge now execute, including the bounded public-only factory-view fixture; complete numerical acceptance keeps class coverage Partial. Custom solver algorithms and platform/native acceptance remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).

## Selected contact publication

Factory-returned cached extensions now supply GetContactCountCore/GetContactCore to real scene contact monitoring and raw callback snapshots. Native CPU workers capture private input first; user contact hooks run on the world owner after solving/workers finish. The common step stages every projected value and commits the complete bounded snapshot before monitor events and callbacks. The existing uncached caller-created view still invokes its hooks through public reads.

Cached contact getters read immutable published values rather than reinvoking hooks. This prevents repeated transformations and later association resampling. Logical slots, weak collider identity, full-step impulse, geometry and velocities come from the validated hook result. Contact-limit assignment clears count immediately; common replay restores the same published values. Native input and projected output use distinct retained arrays. A bad count, thrown hook, default/wrong/stale body contact leaves the receiver's previous snapshot and monitor state intact. Other receivers and post-solver callbacks continue; the already committed tick stays committed and a user projection error does not poison the GPU world. Native solver/device failures retain their terminal policy.

An engine-only qualified publication scope lets PhysicsBodyContact validate current same-world identities after solver completion while the interval remains borrowed. General physics queries, state mutation, ordinary query-result construction, hierarchy/binding changes and disposal still reject. This scope does not make cached views or worlds publicly retargetable.

[Contact publication checks](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.ContactPublication.cs) cover doubled actual reported impulses without double application, scene monitor suppression/departure/reentry, raw callback delivery, immutable associations, invalid count/value/throw/stale attachment recovery and owner-thread hooks with real CPU worker capture. The fixture has 301 scene rigid bodies plus one raw body (and a static floor), 64 warmup/64 measured full 1/60-second frames plus cached contact reads, asserting zero owner/all-thread managed allocation on CPU/GPU. The existing ownership no-device child includes it. Dedicated cold probes are outside the measured hook path; their earlier capturing lambdas allocated 128 bytes per receiver/frame even with the probe disabled, and were moved to a separate probe method without changing the measured operation or budget.

Public named server factories/full operation bridge and custom algorithms remain open under ADR 0103. These internal selected factory tests do not claim public third-party solver acceptance, native allocation, foreign-platform or final GPU/window performance acceptance.


[Contact reporting depth](physics-registered-adapter.md#contact-reporting-depth) adds an explicit signed scene-unit PhysicsBodyContact.Depth sample to numerical and factory projection. Cached projection filters its own sampled depths before committing; physical solving and completed-snapshot lifetime remain unchanged.


[Registered factory-selected views](physics-registered-adapter.md#factory-selected-direct-views) now have a public-only check of body/query factory dispatch, cache identity, initially empty contact snapshots, immutable publication before callbacks, failed factories and retry, attachment retirement and zero warm managed allocations. Wider numerical-extension acceptance stays open.

[Retained slot history](physics-snapshots.md#capture-after-topology-edits) now samples
private logical-slot lifetimes for contacts, overlaps and collided motion. Capture
rejects removal/reindex/readdition before replacing bytes while preserving sampled
public data and events. In-slot replacement stays valid. CPU/registered serial/
separate twelve-case verification executes; destination apply remaps tokens, which
never enter wire data. Current GPU/native and broader topology acceptance stay open.
