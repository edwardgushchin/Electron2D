# PhysicsWorldBackend

Last updated: 2026-10-10

**Declaration:** `internal abstract class PhysicsWorldBackend : IDisposable` · **Source:** [PhysicsWorldBackend.cs](../../src/Servers/Physics/PhysicsWorldBackend.cs) · **Component:** [Physics backends](../components/physics-backends.md)

## Description and internal flow

Owns selection metadata and full interval dispatch for one internal implementation. PhysicsSpace constructs it after validating sampled world settings. Create makes a fresh CPUPhysicsWorldBackend or GPUPhysicsWorldBackend; optional startup fallback constructs an actual CPU implementation and retains the GPU request and failure message. It never changes a running implementation.

Kind, Requested and FallbackReason describe immutable selection. EnsureAccess performs implementation-specific idle/failure/lifetime checks. Step dispatches the complete CPU/GPU interval; StepNative is the CPU discrete/CCD endpoint. WorldID, Tasks and diagnostic stage controls are internal CPU-only facets and reject for a resident GPU implementation. GPUStore is present only for the resident implementation. Dispose releases owned solver resources.

IntersectRay, CollectPointHits, CollectShapeHits, CastMotion, CollectShapeContacts and GetRestInfo dispatch every direct-space query to the same selected owner. Public access guards and output projection stay in PhysicsDirectSpaceState; concrete implementations own retained query scratch. These internal operations do not yet expose the public extension context under ADR 0103.

CreateCollider constructs a fresh selected CPU/GPU PhysicsColliderImplementation for each body/Area attachment. The retained collider identity facade delegates all engine-unit body and shape operations to that concrete owner; reattachment never retargets its immutable world.

CreateJoint creates a fresh selected CPU/GPU PhysicsJointImplementation for each constraint attachment. Shared settings and physical RID stay in PhysicsJointRuntime. Generic policy/setters/portable-frame/release operations dispatch to that attachment.

## Ownership and verification

The space owner thread controls lifecycle and stepping. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks fresh implementations, distinct physical stores, unchanged requested/actual diagnostics, real published motion, independent ticks, warmed full-step allocation, native lifetime, injected worker-cleanup failure and a no-device child process. It also checks all six direct-space operations, access/lifetime/failure guards and 64 warmed hit/miss query cycles at zero owner/all-thread allocation. Existing common physics, checkpoint and network checks remain separate acceptance evidence.

## Limits and decisions

PhysicsSpace retains membership, authoring, observer state and event/callback ordering. This internal factory is currently limited to the two built-ins. It is not public backend registration and does not implement custom geometry or the server/direct-state extension families. Native allocator, foreign-platform execution, GPU speed advantage and real-window FPS remain separate gates. See [ADR 0054](../decisions/physics-backends.md#adr-0054) and [ADR 0103](../decisions/physics-extensions.md#adr-0103).
