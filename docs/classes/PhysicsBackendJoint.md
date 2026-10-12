# PhysicsBackendJoint

Last updated: 2026-10-12

**Declaration:** `public abstract class PhysicsBackendJoint` · **Source:** [PhysicsBackendJoint.cs](../../src/Servers/Physics/PhysicsBackendJoint.cs) · **Component:** [Registered solver adapter](../components/physics-registered-adapter.md)

## Description

Defines one constraint attachment claimed once. Attach receives engine-assigned first/second body RIDs (empty second means the world frame), immutable local frames and numerical policy. All live setters and portable-frame replacement execute through the same attachment.

The engine retains authoring, logical identity, owner-thread/solver guards and observer timing. Backend-private simulation state stays with the implementation. Borrowed spans expire on return. Factories and hooks cannot transfer/release their current world or attachments. Solver/history objects reject repeated claims; retiring attachments preserve matching replacements.

## Protected implementation hooks

| Declaration | Contract |
| --- | --- |
| `protected internal abstract bool IsAttached { get` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void Attach(RID first, RID second, in PhysicsJointConfiguration settings)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void Detach()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void ApplySolverPolicy(in PhysicsJointConfiguration settings)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void ApplyPortableFrames(Transform first, Transform second)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetCollideConnected(bool value)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetPinLimits(bool enabled, float lower, float upper)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetPinMotorEnabled(bool value)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetPinMotorVelocity(float value)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetPinMotorMaxTorque(float value)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetGrooveLimits(float lower, float upper)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |

## Verification and limits

[Independent solver probe](../../tests/Electron2D.Tests/PhysicsRegisteredBackendTests.cs) exercises scene/raw integration, ordinary views, queries, Area events, contacts, spring policy, local/portable restore, guarded lifetime and terminal solve failure. The test solver implements a bounded circle/spring model; it is a separate public-only test consumer, not a production backend. Arbitrary custom geometry and unexercised operation acceptance remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).


The complete typed facade parameter/flag family projects retained common authoring into Attach, ApplySolverPolicy and the concrete pin/groove hooks. Initial/live hooks cannot recursively change scene/raw paths, geometry or scalars. The independent consumer verifies profile propagation and spring motion; registered pin/groove numerical response remains open.


Connection clear and replacement also reject numerical borrowing during CreateJoint, Attach, live policy and Detach. The independent consumer checks pending endpoints, fresh attachment reentry, kind/role/geometry rejection, factory/null/reuse/attach/detach errors and explicit recovery for all three roles. Registered pin/groove numerical response remains open.
