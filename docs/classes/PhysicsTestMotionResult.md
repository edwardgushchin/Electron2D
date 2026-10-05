# PhysicsTestMotionResult

Last updated: 2026-10-05

**Inherits:** ElectronObject · **Source:** [PhysicsTestMotion.cs](../../src/Servers/Physics/PhysicsTestMotion.cs) · **Component:** [Physics server and direct queries](../components/physics-queries.md)

## Description

Caller-owned output updated by [PhysicsServer.BodyTestMotion](PhysicsServer.md). The result copies collider identity, shape-owner indices, contact geometry, fractions and travel. A completed no-hit test clears the preceding collider and records full requested travel. The object does not own the bodies or their fixtures.

## Example

Partial snippet with a registered `bodyRID` and live `parameters`:

```csharp
using var result = new PhysicsTestMotionResult();
if (PhysicsServer.BodyTestMotion(bodyRID, parameters, result))
    Console.WriteLine(result.GetCollisionNormal());
```

## API summary

| Member | Contract |
| --- | --- |
| `public PhysicsTestMotionResult()` | Empty caller-owned output. |
| `public ElectronObject? GetCollider()` | Live scene collider, or null for server-only/freed bodies. |
| `public ulong GetColliderID()` / `public RID GetColliderRID()` | Sampled collider identities. |
| `public int GetColliderShape()` / `GetCollisionLocalShape()` | Direct collider/moving shape-owner indices. |
| `public Vector2 GetColliderVelocity()` | Collider point velocity, scene units per second. |
| `public float GetCollisionDepth()` | Contact penetration depth in scene units. |
| `public Vector2 GetCollisionNormal()` / `GetCollisionPoint()` | Global outward normal and point on collider. |
| `public float GetCollisionSafeFraction()` / `GetCollisionUnsafeFraction()` | First impact bracket from zero through one. |
| `public Vector2 GetTravel()` / `GetRemainder()` | Travel including recovery and untraveled requested motion. |

## Method descriptions

`GetCollider` resolves current scene ownership at read time. `GetColliderID` and `GetColliderRID` retain their sampled values after disposal; server-only bodies have ID zero. Shape indices refer to direct owners, not compound backend fixtures. The point and normal are global, depth and travel use scene units, and velocity uses scene units per second. On a completed miss, fractions are `(1, 1)`, collider identity and contact values clear, and remainder is zero. A newly constructed result yields zero/default values until the first test. Disposed result access rejects.

## Verification and limits

[PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks server-only and scene identities, shape indices, point/normal, fractions, travel/remainder, exclusions and no-hit reset. Virtual tile collision objects remain [Partial](../coverage/classes/PhysicsTestMotionResult2D.md). Native allocation, other platforms and owner acceptance are unverified. See [ADR 0063](../decisions/physics.md#adr-0063).
