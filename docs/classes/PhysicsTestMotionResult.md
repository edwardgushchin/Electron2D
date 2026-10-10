# PhysicsTestMotionResult

Last updated: 2026-10-10

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
| `public void SetMotion(Vector2 travel)` | Replaces output with completed unobstructed travel. |
| `public void SetCollision(RID body, int localShape, RID collider, int colliderShape, Vector2 point, Vector2 normal, float depth, Vector2 colliderVelocity, Vector2 travel, Vector2 remainder, float safeFraction, float unsafeFraction)` | Validates and replaces a supplied motion collision. |
| `public ElectronObject? GetCollider()` | Live sampled object association, or null when unassigned/disposed/collected. |
| `public ulong GetColliderID()` / `public RID GetColliderRID()` | Sampled collider identities. |
| `public int GetColliderShape()` / `GetCollisionLocalShape()` | Direct collider/moving shape-owner indices. |
| `public Vector2 GetColliderVelocity()` | Collider point velocity, scene units per second. |
| `public float GetCollisionDepth()` | Contact penetration depth in scene units. |
| `public Vector2 GetCollisionNormal()` / `GetCollisionPoint()` | Global outward normal and point on collider. |
| `public float GetCollisionSafeFraction()` / `GetCollisionUnsafeFraction()` | First impact bracket from zero through one. |
| `public Vector2 GetTravel()` / `GetRemainder()` | Travel including recovery and untraveled requested motion. |

## Method descriptions

<a id="setmotion"></a>
### SetMotion

Sets finite global `travel` in scene units, clears collision identity, indices, point, normal, depth and collider velocity, zeroes remainder and sets both fractions to one. Nonfinite travel rejects with ArgumentOutOfRangeException; disposed results reject with ObjectDisposedException. This fills existing storage and performs no motion test.

<a id="setcollision"></a>
### SetCollision

`body` and `collider` must identify different live bodies attached to the same usable physics space. Areas, wrong/stale RIDs and self collisions reject with ArgumentException; detached or different-space bodies and wrong-thread/solver/failed-world access reject with InvalidOperationException. `localShape` and `colliderShape` select logical shape-owner indices; indices, nonfinite vectors, negative/nonfinite depth and fractions outside `0 <= safeFraction <= unsafeFraction <= 1` reject with ArgumentOutOfRangeException. Disposed shapes or this result reject with ObjectDisposedException. All validation occurs before replacing the previous result.

`point` and `normal` are global; depth, travel and remainder use scene units; `colliderVelocity` is the global contact-point velocity in scene units per second. Supplied finite geometry is preserved. The method performs no motion test: an implementation computes these values, while the library samples the collider association and protects its identity/lifetime. Calls reuse storage without warmed managed allocation. [The separate public consumer](../../examples/PhysicsResultConstruction/ResultConstructionChecks.cs) exercises construction, validation, atomic replacement, hit-to-miss reuse and ordinary BodyTestMotion on both built-ins. Full backend extension dispatch remains open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).


`GetCollider` resolves the sampled weak object association. `GetColliderID` and `GetColliderRID` retain their sampled values after disposal; unassigned bodies have ID zero. Shape indices refer to direct owners, not compound backend fixtures. The point and normal are global, depth and travel use scene units, and velocity uses scene units per second. On a completed miss, fractions are `(1, 1)`, collider identity and contact values clear, and remainder is zero. A newly constructed result yields zero/default values until the first test. Disposed result access rejects.

## Verification and limits

[PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks server-only and scene identities, shape indices, point/normal, fractions, travel/remainder, exclusions and no-hit reset. Virtual tile collision objects remain [Partial](../coverage/classes/PhysicsTestMotionResult2D.md). Native allocation, other platforms and owner acceptance are unverified. See [ADR 0063](../decisions/physics.md#adr-0063).


Assigned object identity is sampled with each query result, including raw server objects.
Rebinding does not retarget earlier results; disposal/collection makes object resolution null
without erasing the sampled ID. See [object associations](../components/physics-object-bindings.md).
