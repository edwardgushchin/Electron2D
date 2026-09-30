# PhysicsSpace

Last updated: 2026-09-30

**Declaration:** `internal sealed partial class PhysicsSpace : IDisposable`

**Source:** [PhysicsSpace.cs](../../src/Servers/Physics/PhysicsSpace.cs), [PhysicsSpace.BodyState.cs](../../src/Servers/Physics/PhysicsSpace.BodyState.cs), [PhysicsSpace.Kinematic.cs](../../src/Servers/Physics/PhysicsSpace.Kinematic.cs) · **Component:** [Physics server and queries](../components/physics-queries.md)

## Description and internal flow

One owner-thread Box2D world shared by scene bodies/Areas/joints and caller-owned colliders. SceneTree and PhysicsServer host stepping use this same simulation lane; public consumers use [PhysicsServer](PhysicsServer.md#activity) and World2D. No second scheduler exists.

| State/operation | Contract |
| --- | --- |
| `PhysicsSpace()` | Native world plus sampled default fields; initially inactive. |
| `RID`, `WorldID`, body/Area/server-collider/joint lists | Stable server identity and current native generation/membership. |
| `bool IsActive { get; private set; }`, `SetActive(bool active)` | Local interval policy, owner/solver guard; SceneTree sets true on registration. |
| `EnsureQueryAccess()`, `PrepareForQuery()` | Owner/lifetime/solver guard and pending fixture/pose preparation, including inactive worlds. |
| `Step(double delta)` | Gate on local/global activity/nonzero delta; prepare fields/body states/joints, solve native intervals, capture state and dispatch callbacks/events. |
| `LastStep`, cached body-state callback list | Last actual interval and generation-aware delivery; skipped intervals retain data. |
| `Add` / `Remove` scene/server objects | Native membership, dependent joint/monitor lifetime and identity. |
| `GetJointWorldBody()` | Hidden shape-free world anchor for single-body server pin. |
| `Dispose()` | Destroy joints before bodies/world, detach caller configuration and invalidate views. |

## Invariants and verification

Local control and native queries require the owner outside solving. Global activity is sampled atomically at each world interval boundary. Inactive intervals leave state, force queues, contact/sensor snapshots and handles intact; resume does not replay skipped time. Running intervals finish queued callbacks; post-solver control affects later intervals. Scene scheduling remains separate. Structural changes may allocate; prepared frames and activity switches reuse storage.

PhysicsActivityTests checks defaults, actual native motion/spring, skipped callbacks/forces, queries/configuration, related scheduling, failure/thread/lifetime boundaries and zero managed bytes over 64 warmed policy/solver cycles on Linux/.NET 10. Physics body/joint/shape/monitor suites cover the shared kernels and lifecycle. Native allocations, broad-world throughput, other platforms and owner acceptance remain unverified. [ADR 0089](../decisions/physics-activity.md#adr-0089) owns the gate; the [physics decision index](../decisions/index.md) routes its kernels.
