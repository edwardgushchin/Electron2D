# PhysicsColliderImplementation

Last updated: 2026-10-10

**Declaration:** `internal abstract class PhysicsColliderImplementation` · **Source:** [PhysicsColliderImplementation.cs](../../src/Servers/Physics/PhysicsColliderImplementation.cs) · **Component:** [Physics backends](../components/physics-backends.md#runtime-flow-and-ownership)

## Responsibility and flow

The selected world creates this engine-unit operation boundary for one current collider attachment. The owner identity facade and physics space are captured once; reattachment constructs another implementation. It dispatches body/Area creation and release, shape rebuild/filtering, pose/motion/sleep/lock/force/mass/surface/contact operations and portable pose/motion/piece validation. CPU/GPU-specific diagnostic and checkpoint facets remain concrete internal operations.

Common resource RID, object/canvas association, collision priority and monotonically increasing AttachmentVersion stay in [PhysicsColliderBackend](PhysicsColliderBackend.md). Library guards retain owner-thread, solver and failed-world access rules. Factory selection uses the actual implementation, including CPU startup fallback; native handles never become public resource identity.

## Lifecycle and verification

A failed attachment attempts cleanup and leaves the common facade detached. Retiring a concrete implementation clears its handles; a disposed view cannot retarget to a new attachment. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks invalid creation followed by reuse, four body/Area world transfers, native retirement, stable physical/object IDs, current motion/mass/force and query geometry. Existing shared physics, GPU publication, private/portable checkpoint and network checks establish separate behavior limits.

## Limits and decisions

Only internal CPU/GPU implementations are currently constructed. Public backend registration, server/direct-state extension contexts and custom geometry remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103); this type is not a public plugin API. Native allocator, foreign-platform and real-window FPS acceptance remain separate gates.
