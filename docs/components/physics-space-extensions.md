# Caller-created space-query extensions

Last updated: 2026-10-14

## Responsibility and owned types

[PhysicsDirectSpaceStateExtension](../classes/PhysicsDirectSpaceStateExtension.md) derives from the actual direct-space base view. It binds one live engine-assigned space RID and executes the complete six-query family through protected typed hooks. Scalar, caller-owned array and span overloads retain the base API. Built-in views and selected CPU/GPU algorithms keep their current dispatch.

## Runtime flow and invariants

Public input validation and authored body/Area/shape preparation precede every hook. Shape queries require a live resource identity. Query hooks receive value inputs and borrowed spans; pointer outputs map to nullable typed hits, bounded counts, contact-point pairs and a safe/unsafe tuple. The world owner must call queries outside solving or terminal failure.

Each nested invocation has independent retained scratch and sampled filters/exclusions. `IsBodyExcludedFromQuery` uses the innermost invocation and returns false outside queries. Finally clears retained result references, restores exclusion context and releases the world borrow. Active stepping, world rebinding/release, checkpoint capture/restore and view disposal reject while a hook runs. The disposal validator is sealed; implementations may release their own resources through the ordinary disposal override.

Before caller output changes, the library checks counts, ordered unique RID/shape hits, current active slot/world membership, layer/body/Area filters, exact point-query canvas identity, exclusions, finite contact values and ordered finite fractions in [0,1]. It resamples current weak object associations from real collider ownership. Invalid output or a hook exception leaves caller spans unchanged, including unused odd contact tails. Geometry correctness and selection of the nearest/deepest contact remain the implementation's responsibility.

Unchanged warmed span/scalar calls reuse storage. A larger destination, deeper nesting, construction, structural edits, user callback work and nonempty caller-owned arrays can allocate. The query implementation may retain its own typed geometry; `ShapeGetData` deliberately returns a copied resource and is not an allocation-free hot-path lookup.

## Verification and limits

[The separate consumer project](../../tests/PhysicsSpaceExtension.Consumer/PhysicsSpaceExtension.Consumer.csproj) references only public Electron2D API, with no friend assembly or backend dependencies. Its analytic circle implementation executes all six algorithms against current scene/raw body and Area poses, changes query policy, supports masks/canvas/exclusions, and observes a real physical step. It checks inherited arrays/spans, nested same-family queries and failures, borrowed disposal/world binding/step guards, stale object associations, foreign/transferred/disabled results, invalid counts/fractions/points, atomic output, off-owner access and expired views. Query points/fractions use .0001 scene-unit/fraction tolerances for the analytic fixture; identities and counts are exact.

CPU, GPU and a CPU process without display/GPU access are separate profiles. The warmed fixture repeats 64 complete six-query cycles and measures owner/all-thread managed bytes. Native allocation and foreign-platform execution are separate gates.

The internal [selected-owner factory route](physics-backends.md#selected-direct-state-construction) now provides a cached extension to World, server acquisition and body-space consumers. Creation runs on the world owner outside the service registry lock; validation and recursive/lifetime guards precede cache publication. A disposed cache permits a new selected view; invalid foreign/null/disposed factory results leave it unchanged. CPU/GPU/no-device ownership fixtures execute actual queries through this route.

Caller-created query extensions execute. Named backend registration and the shared operation bridge now execute under [ADR 0103](../decisions/physics-extensions.md#adr-0103); full numerical-extension and platform/native acceptance remain open. Class coverage stays Partial. [ADR 0103](../decisions/physics-extensions.md#adr-0103) owns the complete boundary.


[Registered factory-selected views](physics-registered-adapter.md#factory-selected-direct-views) now have a public-only check of body/query factory dispatch, cache identity, initially empty contact snapshots, immutable publication before callbacks, failed factories and retry, attachment retirement and zero warm managed allocations. Wider numerical-extension acceptance stays open.


## Query-shape borrowing (2026-10-14)

All four shape-query hooks borrow the resolved resource or owned server geometry
before its immutable geometry is prepared, through hook completion, output
validation and publication. Equal/changed geometry or policy writes, inherited
copy/reset, disposal and owned RID replacement/retirement reject before mutation.
Nested calls retain independent frame/resource borrows; readonly access remains
available. Finally clears the borrowed reference and restores the world/exclusion
scope after geometry-supplier, hook or output-validation failures. The existing
Shape authoring rules still apply; this is not a general concurrent transaction.
Pose, motion, margin and filters are sampled before a procedural geometry supplier
runs; supplier edits to query parameters affect only a later invocation.

[ShapeLifetimeChecks](../../tests/PhysicsSpaceExtension.Consumer/ShapeLifetimeChecks.cs)
checks resource and owned-RID inputs across array/span/scalar overloads, shared
nested queries, worker-time refusal, inherited metadata/notification preservation,
all four throwing hooks, invalid output, procedural geometry preparation failure
and subsequent edits/queries. CPU/GPU serial/separate fresh processes retain the
six-query analytic fixture and real motion. An additional 128 warm/128 measured
complete frames plus four scalar/span shape queries require strict 0/0 owner/all-
thread managed bytes; a moving raw body travels 256 units over 256 ticks within
.02 units. Query geometry/fractions retain .0001 tolerances. Separate lanes are
observed in process state. CPU has no display/GPU; GPU uses CPU-only Vulkan.
Hardware/native/platform/full numerical-extension and performance gates stay open.

[The complete direct-space audit](physics-space-query-contract.md) reconciles all six inherited query methods and seven extension operations, input sampling, nonempty nested results, selected factories and current verification limits.
