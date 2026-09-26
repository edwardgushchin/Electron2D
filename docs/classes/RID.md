# RID

Last updated: 2026-09-26

**Source:** [RID.cs](../../src/Core/Object/RID.cs) · **Declaration:** `public readonly struct RID : IEquatable<RID>, IComparable<RID>`

## Description

An opaque, backend-neutral identity for a server resource. `default(RID)` and `new RID()` are empty. A nonzero value remains nonzero after its resource is freed; the owning server checks kind and liveness separately. New identities increase within a process session and are never assigned again after release, even if a backend slot is reused. Copying a RID copies its value, not ownership.

## Example

```csharp
using var body = new StaticBody();
RID collider = body.GetRID();
if (collider.IsValid()) Console.WriteLine(collider.GetID());
// Disposing the body releases its server registration.
```

## API summary

| Member | Contract |
| --- | --- |
| `public RID()` | Empty value with numeric identity zero. |
| `public RID(RID from)` | Copies the identity value. |
| `public long GetID()` | Returns zero or a positive session-local ID. |
| `public bool IsValid()` | Tests nonzero identity, without checking server liveness. |
| `public bool Equals(RID other)` / `Equals(object? obj)` | Typed and object equality by numeric identity. |
| `public int CompareTo(RID other)` | Numeric ordering for typed collections. |
| `public override int GetHashCode()` | Hash of the immutable identity. |
| `public override string ToString()` | Diagnostic `RID(id)` text. |
| `public static bool operator ==`, `!=`, `<`, `<=`, `>`, `>=` | Equality and total numeric ordering. |

## Lifetime and verification

Only engine servers allocate nonzero values. `PhysicsServer.FreeRID` invalidates its registry entry, while copies retain their numeric value and `IsValid()` still returns true; using that stale value with the server throws. Scene-owned RIDs remain stable across fixture rebuild and scene reentry until their collision object is disposed. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) covers empty/copy/order, scene and server identities, shape rebuild and non-reuse after free. See [ADR 0063](../decisions/physics.md#adr-0063).
