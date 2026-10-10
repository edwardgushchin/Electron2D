# GPUPhysicsColliderImplementation

Last updated: 2026-10-10

**Declaration:** `internal sealed partial class GPUPhysicsColliderImplementation : PhysicsColliderImplementation` · **Source:** [GPUPhysicsColliderImplementation.cs](../../src/Servers/Physics/GPUPhysicsColliderImplementation.cs) · **Component:** [Physics backends](../components/physics-backends.md#runtime-flow-and-ownership)

## Responsibility and flow

Owns one resident body handle, shape handles/query leases, sensor/mask policy, constant/surface metadata and epoch-qualified state/publication/replay caches. Implements all engine-unit collider operations through the independent store. Existing query projection and common frame publication can address its retained identity facade; no CPU body/world cache is created. Detach attempts every shape lease, resident removal and common lookup retirement, then clears handles and aggregates failures.

Common resource RID, object/canvas association, collision priority and monotonically increasing AttachmentVersion stay in [PhysicsColliderBackend](PhysicsColliderBackend.md). Library guards retain owner-thread, solver and failed-world access rules. Factory selection uses the actual implementation, including CPU startup fallback; native handles never become public resource identity.

Joint local-frame sampling now uses this same selected body attachment. CPU keeps native local-point/rotation math without a decoded-angle round trip; GPU samples its current typed pose and validates the existing anchor extent.

## Lifecycle and verification

A failed attachment attempts cleanup and leaves the common facade detached. Retiring a concrete implementation clears its handles; a disposed view cannot retarget to a new attachment. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks invalid creation followed by reuse, four body/Area world transfers, native retirement, stable physical/object IDs, current motion/mass/force and query geometry. Existing shared physics, GPU publication, private/portable checkpoint and network checks establish separate behavior limits.

## Limits and decisions

Only internal CPU/GPU implementations are currently constructed. Public backend registration, server/direct-state extension contexts and custom geometry remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103); this type is not a public plugin API. Native allocator, foreign-platform and real-window FPS acceptance remain separate gates.
