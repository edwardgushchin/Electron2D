# KinematicCollision2D

Last updated: 2026-09-26

**Inherits:** ElectronObject · **Source:** [KinematicCollision2D.cs](../../src/Scene/2D/KinematicCollision2D.cs) · **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

Caller-owned typed snapshot of one [PhysicsBody.MoveAndCollide](PhysicsBody.md) or `TestMove` contact. The snapshot contains the moving body's travel and remainder, contact geometry and collider identity. It does not own either body or its shapes. Server-only colliders have a RID and no scene object. Shape-owner indices remain stable across compound fixture pieces.

## Example

Partial snippet with an attached `body`:

```csharp
using var hit = body.MoveAndCollide(new Vector2(0, 100));
if (hit is not null) Console.WriteLine(hit.GetNormal());
```

## API summary

| Member | Contract |
| --- | --- |
| `public KinematicCollision2D()` | Empty reusable output for `TestMove`. |
| `public float GetAngle(Vector2? upDirection = null)` | Positive angle to up, radians; default (0, -1). |
| `public ElectronObject? GetLocalShape()` | Moving scene body's direct CollisionShape or CollisionPolygon owner. |
| `public ElectronObject? GetCollider()` / `GetColliderShape()` | Live scene body/shape owner, or null after disposal or for a server-only body. |
| `public ulong GetColliderID()` / `public RID GetColliderRID()` | Sampled instance/RID identity; zero ID for server-only. |
| `public int GetColliderShapeIndex()` | Collider's global logical shape index. |
| `public Vector2 GetColliderVelocity()` | Collider point velocity in scene units per second. |
| `public float GetDepth()` | Penetration along the normal in scene units. |
| `public Vector2 GetNormal()` / `GetPosition()` | Global outward normal and collider contact point. |
| `public Vector2 GetTravel()` / `GetRemainder()` | Displacement before collision, including recovery, and untraveled requested motion. |

## Method descriptions

`GetAngle` returns a nonnegative radian angle between `GetNormal()` and a finite nonzero up direction; null selects `(0, -1)` and zero/nonfinite input rejects. `GetLocalShape` and `GetColliderShape` resolve direct scene shape owners at read time and return null if an owner was disposed. `GetCollider` also resolves at read time. `GetColliderID` and `GetColliderRID` preserve the sampled identity even when the object later disappears. A server-only collider has ID zero and null scene owners. `GetDepth`, `GetNormal`, `GetPosition`, `GetColliderVelocity`, `GetTravel` and `GetRemainder` are copied values; an initially empty output yields zero values. Disposed result access rejects.

## Verification and limits

[PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks scene/server identities, direct shape-owner lookup, contact angle/normal/point, safe travel and remainder, test-only movement and recovery. Virtual tile collision owners remain [Partial](../coverage/classes/KinematicCollision2D.md); native allocation, other platforms and owner acceptance are unverified. See [ADR 0063](../decisions/physics.md#adr-0063).

The [CollisionObject owner registry](CollisionObject.md#createshapeowner) now supplies logical shape slots for both child and manual groups. Query/contact indices identify global slots, while ShapeFindOwner returns the distinct group ID; removal shifts later indices. Motion owner accessors resolve weak configured objects as well as child nodes. See [ADR 0071](../decisions/physics.md#adr-0071).
