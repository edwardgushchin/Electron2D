# PhysicsBackendStatistics

Last updated: 2026-10-12

**Declaration:** `public readonly record struct PhysicsBackendStatistics` · **Source:** [PhysicsBackendObservations.cs](../../src/Servers/Physics/PhysicsBackendObservations.cs) · **Component:** [Registered solver adapter](../components/physics-registered-adapter.md)

## Description

Completed active-body, collision-candidate and constraint-island counts supplied by a registered implementation. The value carries no solver handles or world lifetime. ReadStatistics supplies one complete sample after successful numerical synchronization; the adapter rejects any negative field before publishing it. Constructing a value does not validate it.

## Constructor and properties

| Signature | Contract |
| --- | --- |
| `public PhysicsBackendStatistics(int Active, int Pairs, int Islands)` | Store the three signed integer counts. The engine rejects negative counts and Islands greater than Active before publishing the returned sample. |
| `public int Active { get; init; }` | Awake nonstatic bodies, including kinematic bodies and excluding Areas. |
| `public int Pairs { get; init; }` | Collision candidates, independently of event or contact-point caps. |
| `public int Islands { get; init; }` | Active dynamic groups containing contacts or joints, at most Active. Unconstrained bodies and shared static supports do not add or merge islands. |

The record's ordinary value equality, copying and deconstruction retain all three fields. Default construction creates zero counts. A returned value is an immutable completed sample; later solving does not rewrite it. These counts are dimensions of work, not scene units or a cross-backend traversal/equality guarantee.

## Publication and errors

PhysicsServer.GetProcessInfo reads common cached samples without stepping or reentering the implementation and is available on another thread. Local inactive worlds are omitted; global suspension and zero-time intervals preserve samples. A failed numerical interval retains the previous successful sample. Aggregate signed overflow rejects rather than wrapping. Body/query observation continues to use its own owner and failed-world guards.

## Verification and limits

The [public-only lifecycle consumer](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.Lifecycle.cs) verifies all three negative fields and both zero/nonzero Active samples with excessive Islands, explicit/scene publication order, cached foreign-thread reads, pause/failure/retirement and zero warmed full-frame allocations. [Registered lifecycle census](../components/physics-registered-adapter.md#registered-lifecycle-census) records tolerances and the remaining complete numerical-extension, native/foreign and performance gates.

The [completed-count consumer](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.Statistics.cs)
checks mixed scene/raw dynamics, compound static support, sensors, kinematic activity,
pin joining, sleep/wake, local suspension, replay and all three Performance projections
on CPU, software GPU and an independent registered implementation. Each serial/separate
profile measures 128 complete frames plus reads after 128 warm frames and collection
at strict 0/0 owner/all-thread bytes. [Evidence and limits](../components/physics-registered-adapter.md#completed-numerical-statistics)
retain wider numerical, hardware, native-memory and platform gates.
