# PhysicsBackendStatistics

Last updated: 2026-10-12

**Declaration:** `public readonly record struct PhysicsBackendStatistics` · **Source:** [PhysicsBackendObservations.cs](../../src/Servers/Physics/PhysicsBackendObservations.cs) · **Component:** [Registered solver adapter](../components/physics-registered-adapter.md)

## Description

Completed active-body, collision-candidate and constraint-island counts supplied by a registered implementation. The value carries no solver handles or world lifetime. ReadStatistics supplies one complete sample after successful numerical synchronization; the adapter rejects any negative field before publishing it. Constructing a value does not validate it.

## Constructor and properties

| Signature | Contract |
| --- | --- |
| `public PhysicsBackendStatistics(int Active, int Pairs, int Islands)` | Store the three signed integer counts. The engine validates the returned sample. |
| `public int Active { get; init; }` | Active physical objects according to the selected solver's completed work. |
| `public int Pairs { get; init; }` | Collision candidates, independently of event or contact-point caps. |
| `public int Islands { get; init; }` | Active constraint groups according to that solver's actual grouping. |

The record's ordinary value equality, copying and deconstruction retain all three fields. Default construction creates zero counts. A returned value is an immutable completed sample; later solving does not rewrite it. These counts are dimensions of work, not scene units or a cross-backend traversal/equality guarantee.

## Publication and errors

PhysicsServer.GetProcessInfo reads common cached samples without stepping or reentering the implementation and is available on another thread. Local inactive worlds are omitted; global suspension and zero-time intervals preserve samples. A failed numerical interval retains the previous successful sample. Aggregate signed overflow rejects rather than wrapping. Body/query observation continues to use its own owner and failed-world guards.

## Verification and limits

The [public-only lifecycle consumer](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.Lifecycle.cs) verifies all three negative fields, explicit/scene publication order, cached foreign-thread reads, pause/failure/retirement and zero warmed full-frame allocations. [Registered lifecycle census](../components/physics-registered-adapter.md#registered-lifecycle-census) records tolerances and the remaining complete numerical-extension, native/foreign and performance gates.
