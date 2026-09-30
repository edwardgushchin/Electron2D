# PhysicsServerShape

Last updated: 2026-09-30

**Declaration:** `internal sealed class PhysicsServerShape` · **Inherits:** System.Object

**Source:** [PhysicsServerCollider.cs](../../src/Servers/Physics/PhysicsServerCollider.cs) · **Component:** [Physics server and queries](../components/physics-queries.md)

## Description and runtime flow

RID entry for owned or borrowed Shape geometry. Owned copies bind their resource-query identity to this same RID; ShapeSetData swaps the copy and retires the held old view. Free removes current users before releasing geometry. Managed resource entries retain resource-owned lifetime.

Consumer examples and complete public operations are on [PhysicsServer](PhysicsServer.md#shape-slots). Scene group and raw body/Area slots share native geometry, queries, one-way pre-solve/motion, and mass calculation. Shared shape mutation/free checks all related active worlds before changes. Structural work may allocate; indexed reads and unchanged settings reuse state.

## Internal state

| Signature | Contract |
| --- | --- |
| `PhysicsServerShape(RID rid, Shape geometry, bool ownsGeometry = true)` | Wrap owned/borrowed geometry under one stable identity. |
| `RID RID { get; }` | Registry identity. |
| `Shape Geometry { get; set; }` | Current geometry copy/reference; owned replacement retires the old view. |
| `bool OwnsGeometry { get; }` | RID owns the copy, or managed resource owns its borrowed registration. |

## Lifetime and verification

PhysicsServer owns the registry entry; colliders borrow shapes and do not release them. Owned geometry cannot be disposed outside its RID owner. Shape data replacement preserves identity and retires a held view; free removes users and unregisters even after a disposal observer failure. World removal retains collider configuration. [PhysicsServerShapeSlotTests](../../tests/Electron2D.Tests/PhysicsServerShapeSlotTests.cs) verifies scene/server geometry, policy, reindexing, ownership and native response with zero managed bytes over 64 warmed read/unchanged-write/solver frames on Linux/.NET 10. Native allocation, structural-edit budgets, other platforms and owner acceptance remain unverified. [ADR 0088](../decisions/physics-shape-slots.md#adr-0088) owns this integration.
