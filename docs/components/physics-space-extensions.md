# Caller-created space-query extensions

Last updated: 2026-10-10

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

This executes caller-created query extensions. Registered server factories do not yet return these views to scene/server consumers; registered body-state callback integration and custom Shape families remain open. The class coverage stays Partial for that integration. [ADR 0103](../decisions/physics-extensions.md#adr-0103) owns the complete boundary.
