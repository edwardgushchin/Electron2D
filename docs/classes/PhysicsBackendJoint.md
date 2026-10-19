# PhysicsBackendJoint

Last updated: 2026-10-19

## Lifecycle request ownership

After clear passes its access preflight, cleanup always commits Empty and forgets
endpoints, including selected detach or contribution-invalidation errors. The RID
and common settings survive. A server clear retains scene override ownership even
on failure, so endpoint reentry cannot restore the retired scene connection.
Validated server make requests commit role, sampled frames, endpoints and scene
ownership before selected factory/attachment/contribution effects. A failed
publication retains that request for explicit retry or endpoint reentry; explicit
scene authoring can still reclaim it. Rejected validation/borrowing preserves the
prior request. Failure detaching an old connection precedes the new request commit.
Selected providers release their resources before reporting Detach errors; the
engine retires that attachment and preserves the original cleanup errors.
See [complete joint lifecycle operations](../components/physics-registered-adapter.md#complete-joint-lifecycle-operations)
for executed scope and limits.

## Typed parameter publication

A selected publication error retains the requested typed parameter channel.
Equal setters, live world queries, local/portable capture and active positive
steps retry before numerical use. Configured reads and zero/inactive intervals
do not repair. Solver policy, pin limits, each motor channel and groove limits
retire their debt independently; a later channel failure does not replay an
already completed channel. Publication reentry rejects live preparation.
Detach/clear/free discards the old attachment's debt; reentry applies retained
configuration to a fresh attachment. Existing per-interval refresh remains separate.
See [complete joint parameter operations](../components/physics-registered-adapter.md#complete-joint-parameter-operations)
for actual scope, response checks and remaining acceptance limits.


## Collision exception publication

Explicit body exceptions and independently counted active-joint contributions
share one deduplicated input union. A failed joint collision-policy publication
retains the requested flag and complete common contribution, then retries before
a world query, local/portable capture or active positive step. An equal setter
also retries; configured flag and scoped exception-input reads do not repair.
Clear, free, endpoint departure and scene disposal remove only that attachment's
contribution and pending request. Reentry applies the retained flag to a fresh
attachment. The selected SetCollideConnected hook applies a complete value and
may be retried after a publication error.
See [complete exception operations](../components/physics-registered-adapter.md#complete-exception-operations)
for threading, response, snapshots, cleanup checks and remaining limits.


[Portable joint continuation](../components/physics-snapshots.md#portable-joint-continuation)
now verifies CPU/registered scene/raw sampled frames, silent correction, atomic
policy guards, local replay and real pin/groove/spring response. Warm complete-frame
transfers retain zero managed allocation; wider graphs and GPU/native gates remain open.

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
