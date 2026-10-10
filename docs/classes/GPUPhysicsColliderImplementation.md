# GPUPhysicsColliderImplementation

Last updated: 2026-10-10

**Declaration:** `internal sealed partial class GPUPhysicsColliderImplementation : PhysicsColliderImplementation` · **Source:** [GPUPhysicsColliderImplementation.cs](../../src/Servers/Physics/GPUPhysicsColliderImplementation.cs) · **Component:** [Physics backends](../components/physics-backends.md#runtime-flow-and-ownership)

An explicitly requested compact pose has an independent epoch-qualified cache. It can satisfy scalar pose access without treating velocity, fields or sleep as current. Local replay captures/restores this cache; portable motion application invalidates it. Scene presentation remains owned by the scene transform.

## Responsibility and flow

Owns one resident body handle, shape handles/query leases, sensor/mask policy, constant/surface metadata and epoch-qualified state/publication/replay caches. Implements all engine-unit collider operations through the independent store. Existing query projection and common frame publication can address its retained identity facade; no CPU body/world cache is created. Detach attempts every shape lease, resident removal and common lookup retirement, then clears handles and aggregates failures.

Common resource RID, object/canvas association, collision priority and monotonically increasing AttachmentVersion stay in [PhysicsColliderBackend](PhysicsColliderBackend.md). Library guards retain owner-thread, solver and failed-world access rules. Factory selection uses the actual implementation, including CPU startup fallback; native handles never become public resource identity.

Joint local-frame sampling now uses this same selected body attachment. CPU keeps native local-point/rotation math without a decoded-angle round trip; GPU samples its current typed pose and validates the existing anchor extent.

## Lifecycle and verification

A failed attachment attempts cleanup and leaves the common facade detached. Retiring a concrete implementation clears its handles; a disposed view cannot retarget to a new attachment. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks invalid creation followed by reuse, four body/Area world transfers, native retirement, stable physical/object IDs, current motion/mass/force and query geometry. Existing shared physics, GPU publication, private/portable checkpoint and network checks establish separate behavior limits.

## Limits and decisions

Only internal CPU/GPU implementations are currently constructed. Public backend registration, server/direct-state extension contexts and custom geometry remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103); this type is not a public plugin API. Native allocator, foreign-platform and real-window FPS acceptance remain separate gates.

World-point platform velocity now dispatches through the current selected attachment. Common lookup uses the shared scene/raw RID registry and checks current world ownership, including transfer/detach/free. CPU keeps direct world-point native conversion; GPU keeps resident center-aware surface motion. See [motion ownership and checks](PhysicsWorldBackend.md#body-motion-and-platform-lookup).

## Selected integration preparation

[Integration preparation](../../src/Servers/Physics/GPUPhysicsColliderImplementation.Integration.cs) consumes eligible pending force/torque through resident sparse edits and skips them under omission. Constants, fields and damping remain device integration policies; no additional host simulation path is introduced. [Contract and verification](../components/physics-backends.md#selected-body-integration-preparation).

Explicit sleep first synchronizes pending authored integration/constant policies
and flushes their older wake work, then writes the sleep command. A later authored
change can still wake the receiver; an older deferred policy update cannot override
the explicit sleep. The shared scene/raw ownership fixture exercises this order.

## Procedural resource geometry

Runtime geometry decisions now use ShapeGeometry's represented primitive rather than concrete resource classes. Caller-defined Shape resources retain the same full-contour, infinite-boundary, directed-ray and zero-geometric-mass policies. Compound CPU contour metadata borrows any Shape with a large convex payload. Scene/raw geometry construction is preflighted before retiring existing CPU/GPU fixtures, so a failing protected hook leaves preparation retryable. See [procedural shape verification](../components/physics-shapes.md#caller-defined-shape-resources).
