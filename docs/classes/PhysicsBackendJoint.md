# PhysicsBackendJoint

Last updated: 2026-10-11

**Declaration:** `internal abstract class PhysicsBackendJoint` · **Source:** [PhysicsBackendJoint.cs](../../src/Servers/Physics/PhysicsBackendJoint.cs) · **Component:** [Registered solver adapter](../components/physics-registered-adapter.md)

## Description

Defines one constraint attachment claimed once. Attach receives engine-assigned first/second body RIDs (empty second means the world frame), immutable local frames and numerical policy. All live setters and portable-frame replacement execute through the same attachment.

The engine retains authoring, logical identity, owner-thread/solver guards and observer timing. Backend-private simulation state stays with the implementation. Borrowed spans expire on return. Factories and hooks cannot transfer/release their current world or attachments. Solver/history objects reject repeated claims; retiring attachments preserve matching replacements.

## Internal operation family

| Declaration | Contract |
| --- | --- |
| `internal void Claim()` | Permanently claims one implementation context; repeated ownership rejects before use. |
| `internal abstract bool IsAttached { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void Attach(RID first, RID second, in PhysicsJointConfiguration settings)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void Detach()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void ApplySolverPolicy(in PhysicsJointConfiguration settings)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void ApplyPortableFrames(Transform first, Transform second)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetCollideConnected(bool value)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetPinLimits(bool enabled, float lower, float upper)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetPinMotorEnabled(bool value)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetPinMotorVelocity(float value)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetPinMotorMaxTorque(float value)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetGrooveLimits(float lower, float upper)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |

## Verification and limits

[Independent solver probe](../../tests/Electron2D.Tests/PhysicsRegisteredBackendTests.cs) exercises scene/raw integration, ordinary views, queries, Area events, contacts, spring policy, local/portable restore, guarded lifetime and terminal solve failure. The test solver implements a bounded circle/spring model; it is not a production backend or a public-only consumer. Unexercised operation families and complete public registration/custom geometry remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).
