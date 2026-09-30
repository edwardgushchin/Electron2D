# World2D

Last updated: 2026-09-30

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
| `public PhysicsDirectSpaceState DirectSpaceState { get; }` | Cached live query view; a disposed view is recreated. |
| `protected override Resource CreateDuplicateInstance()` | Resource duplicate borrows the same space RID. |

## Lifecycle and limits

The owning SceneTree disposes its cached World2D on teardown and unregisters its space RID. A caller may dispose its wrapper; a later `GetWorld2D` creates a fresh wrapper for the same live space. A duplicate wrapper does not copy solver state. `Space` validates liveness, and `DirectSpaceState` queries validate thread ownership and world-lock state. `Canvas` and `NavigationMap` remain [Blocked](../coverage/classes/World2D.md) until their own server resource lifetimes exist. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) verifies shared identity, server access and teardown. See [ADR 0063](../decisions/physics.md#adr-0063).

Typed PhysicsServer Area field getter/setters now share scene/server profiles and address a space default Area by its Space RID. Bounded fields use current exact overlap and descending mixed priority; monitor flags/callbacks are independent. Initial space defaults sample ProjectSettings, then explicit live edits apply on the next nonzero step without changing other worlds. Point/default gravity, body damping/scaling, wakeup and failure recovery are verified by [PhysicsServerAreaFieldTests](../../tests/Electron2D.Tests/PhysicsServerAreaFieldTests.cs) under [ADR 0056](../decisions/physics-fields.md#adr-0056). Native allocation, other platforms and owner visual acceptance remain unverified.

[Physics world activity](PhysicsServer.md#activity) is now independent of scene scheduling under [ADR 0089](../decisions/physics-activity.md#adr-0089). SceneTree activates its lazily created world; explicit SpaceCreate defaults inactive and requires SpaceSetActive(true). Global/local false skips simulation, force consumption and solver callbacks without clearing native state or accumulating elapsed time. Queries/configuration/cleanup and scene callbacks/timers continue. [PhysicsActivityTests](../../tests/Electron2D.Tests/PhysicsActivityTests.cs) checks the profile and warmed allocation on Linux/.NET 10.
