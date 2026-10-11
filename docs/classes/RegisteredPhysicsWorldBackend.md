# RegisteredPhysicsWorldBackend

Last updated: 2026-10-11

**Declaration:** `internal sealed partial class RegisteredPhysicsWorldBackend : PhysicsWorldBackend` · **Source:** [RegisteredPhysicsWorldBackend.cs](../../src/Servers/Physics/RegisteredPhysicsWorldBackend.cs) · **Component:** [Registered solver adapter](../components/physics-registered-adapter.md)

## Description

Adapts a single claimed PhysicsBackendContract to the complete common scene/server pipeline. It owns logical attachment tables, reusable portable RID/episode/query scratch and the reusable motion result. The implementation owns numerical simulation and geometry. Kind and Requested are Extension; enum-based built-in factories still accept CPU/GPU only.

Numerical collider and joint hooks borrow the world on the owner. Collider output rejects invalid vectors/scalars/rigid poses/counts/mass. Contact data validates the whole supplied prefix before committing to the cached view. Read/projection failure isolates the receiver and retains its prior point count; later callbacks and committed tick continue. A failure during Solve or result synchronization is terminal with no built-in replay. Dispose attempts each attachment and Finish even after errors, and retains a cleanup-only context after common space retirement.

## Internal adapters

| Adapter | Role |
| --- | --- |
| `Collider : PhysicsColliderImplementation` | Numerical body/Area values, immutable policy, shapes, contacts and portable motion; no runtime/scene objects cross the solver contract. |
| `Joint : PhysicsJointImplementation` | Engine-assigned joint/body RIDs, local frames, live policy/setters and matching retirement. |
| `History : PhysicsBackendCheckpoint` | Claims fresh solver history, captures before first publication, borrows each capture/validation/restore/release and retires after cleanup failure. |

Portable one-way records translate between local logical RIDs and engine-owned network IDs. Explicit and joint-derived exceptions are filtered to current bodies in this world and sent as typed RID pairs; dirty state commits after successful synchronization. Area results validate current world, enabled shape slots, filtering and monitorability before tracker mutation.

## Verification and limits

[PhysicsRegisteredBackendTests](../../tests/Electron2D.Tests/PhysicsRegisteredBackendTests.cs) uses an independent bounded circle/spring numerical model, never a built-in solver wrapper. It exercises real scene/raw motion and force response, callbacks, queries, Area events, contact publication/failure, joint policy and private/portable restore. Warmed full frames reuse managed storage. This is internal integration evidence; public registered factories, complete arbitrary geometry and a separate public-only consumer remain open. Native allocation and foreign/device/visual/performance acceptance are separate.
