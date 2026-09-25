# World2D

Last updated: 2026-09-25

**Inherits:** [Resource](Resource.md), ElectronObject

- **Source:** [World2D.cs](../../src/Scene/Resources/World2D.cs)
- **Declaration:** `public sealed class World2D : Resource`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

A scene tree's typed view of its existing two-dimensional physics space. [CanvasItem.GetWorld2D](CanvasItem.md) returns one shared World2D per attached scene tree; detached canvas items return null. The world is created lazily when first accessed or when a physics body or Area enters the tree. This Resource does not own a second solver world. Its `Space` and `DirectSpaceState` stop working when the tree releases the space. Canvas and navigation-map RIDs remain separate backend integrations.

## Example

Partial snippet inside a scene callback where `body` is attached to a SceneTree:

```csharp
var world = body.GetWorld2D();
if (world is not null)
{
    using var ray = PhysicsRayQueryParameters2D.Create(new(0, 0), new(0, 100));
    PhysicsRayResult2D? hit = world.DirectSpaceState.IntersectRay(ray);
}
```

## API summary

| Member | Contract |
| --- | --- |
| `public RID Space { get; }` | Stable RID of this tree's live physics space. |
| `public PhysicsDirectSpaceState2D DirectSpaceState { get; }` | Cached live query view; a disposed view is recreated. |
| `protected override Resource CreateDuplicateInstance()` | Resource duplicate borrows the same space RID. |

## Lifecycle and limits

The owning SceneTree disposes its cached World2D on teardown and unregisters its space RID. A caller may dispose its wrapper; a later `GetWorld2D` creates a fresh wrapper for the same live space. A duplicate wrapper does not copy solver state. `Space` validates liveness, and `DirectSpaceState` queries validate thread ownership and world-lock state. `Canvas` and `NavigationMap` remain [Blocked](../coverage/classes/World2D.md) until their own server resource lifetimes exist. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) verifies shared identity, server access and teardown. See [ADR 0063](../decisions/physics.md#adr-0063).
