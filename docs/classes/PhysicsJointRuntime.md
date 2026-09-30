# PhysicsJointRuntime

Last updated: 2026-09-30

**Inherits:** System.Object · **Declaration:** `internal sealed partial class PhysicsJointRuntime`

**Source:** [PhysicsJointRuntime.cs](../../src/Servers/Physics/PhysicsJointRuntime.cs), [PhysicsJointRuntime.Spring.cs](../../src/Servers/Physics/PhysicsJointRuntime.Spring.cs) · **Component:** [Physics joints](../components/physics-joints.md)

## Description

One internal configuration and native lifetime record per caller-owned or scene-owned joint RID. PhysicsServer owns the registry; scene owners are weak references, and PhysicsSpace owns active native handles. The same record supplies PinJoint/GrooveJoint/DampedSpringJoint properties and raw server scalar methods, so both access paths execute the same kernels. It owns no endpoint body or shape. Consumer code uses [Joint.GetRID](Joint.md) and [PhysicsServer joint methods](PhysicsServer.md#joints), not this type.

## Internal state and operations

| State/operation | Purpose |
| --- | --- |
| `PhysicsJointRuntime(RID rid, Joint? scene = null, PhysicsServer.JointType? declaredType = null)` | Stable RID, optional weak scene owner and immutable concrete scene role. |
| `RID`, `Scene`, `DeclaredType`, `Type` | Identity, ownership and configured role; Empty starts with no endpoints. |
| `Space`, `BodyA`, `BodyB`, `BackendID`, `BodyAID`, `BodyBID` | Current owner world, public endpoint identities and current attachment-generation native handles. |
| Stored local frames and translation limits | Preserve sampled geometry across pending detach/reentry; one-body pin B uses fixed world coordinates. |
| `DisableCollision` and pin limit/motor scalar settings | Shared native contact/angle/motor configuration. |
| Spring rest/coefficient/automatic-length settings | Literal server rest or scene zero fallback; Hooke/drag parameters. |
| `EnsureAccess()`, `Require(JointType type)` | Validate all related world threads/phases and concrete role. |
| `ConfigurePin`, `ConfigureGroove`, `ConfigureSceneGroove`, `ConfigureSpring` | Validate input and sample frames before replacing the old connection; optionally retain scene scalars. |
| `RefreshSpace()` | Select active owner, suspend mismatch or create revolute/wheel/filter joint using fresh native IDs. |
| `BodyLeaving(RID body)`, `DetachSpace()`, `Clear()` | Release native handle before body/world destruction; clear additionally forgets role/endpoints. |
| Scalar setters | Validate finite/range values, update shared settings and native solver without resampling anchors. |
| `PrepareSolverStep`, `ValidateSolverStep`, `ApplySolverStep` | Compute axial elastic/drag impulse, preflight cumulative velocities and apply equal opposite anchor impulses. |

## Lifecycle and invariants

Configuration samples body-local frames from actual native transforms or detached typed poses. Every local anchor radius is bounded inclusively to 100,000 backend meters before native replacement, preventing finite lever-arm overflow. Pin alone permits empty B and a hidden shape-free static anchor. Groove/spring require distinct live body RIDs. Pending endpoints can join one world later; temporary departure/mismatch preserves frames, endpoint final free clears them, and world free retains caller-owned configuration. A scene raw override cannot replace its declared role or active scene world. Cold membership changes scan the registry; active fixed steps iterate the existing world runtime list.

Active disabled connections add one independent pair exception contribution. Native destruction/toggling removes only that contribution; explicit exceptions and other joints remain. Before any mutation or disposal, related active worlds must allow owner-thread access. Scene disposal unregisters its stable RID. The registry periodically removes dead detached weak scene entries.

Spring force uses world anchors, rotational leverage, Hooke impulse and exponential axial drag. Prepare/validate all spring impulses before applying any; invalid numeric totals cannot partially apply earlier springs. Each kinematic native interval uses its own duration. Near-coincident anchors provide no direction. State and solver temporary vectors are reused after warmup.

## Verification and limits

Scene pin/groove/spring tests and [PhysicsServerJointTests](../../tests/Electron2D.Tests/PhysicsServerJointTests.cs) cover shared native response, settings, pending/replacement worlds, owner/phase/lifetime rejection, collision contribution accounting and zero managed bytes across 64 warmed active server spring frames on Linux/.NET 10. Native allocations, broader stability/performance, other platforms and owner visual acceptance remain unverified. [ADR 0087](../decisions/physics-joints.md#adr-0087) defines ownership; ADRs 0084–0086 define the kernels.
