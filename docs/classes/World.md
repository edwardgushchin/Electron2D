# World

Last updated: 2026-10-09

- Declaration: `public sealed class World : Resource`
- Source: [World.cs](../../src/Scene/Resources/World.cs)
- Inherits: [Resource](Resource.md)
- Component: [Canvas and physics worlds](../components/worlds.md)

## Description

Combines a stable rendering canvas and lazily created physics space. Viewport.World selects its live runtime and CanvasItem.GetWorld resolves that selection. Independent viewports isolate canvas and physics; explicitly shared worlds expose the same content, body identities and direct queries. A scene without viewports uses its SceneTree fallback world.

## API summary

| Member | Contract |
| --- | --- |
| `World()` | Caller-owned independent runtime with logical canvas and lazy physics. |
| `World(PhysicsServer.Backend backend, bool allowCPUFallback = false)` | Explicit lazy physics selection; invalid enum values reject immediately. |
| `PhysicsServer.Backend RequestedPhysicsBackend { get; }` | Original choice without starting physics. |
| `PhysicsServer.Backend PhysicsBackend { get; }` | Actual selection; initializes physics if needed. |
| `string? PhysicsFallbackReason { get; }` | Startup failure diagnostic after allowed CPU fallback, otherwise null; initializes physics if needed. |
| `RID Canvas { get; }` | Stable borrowed canvas identity before native startup. |
| `RID Space { get; }` | Stable borrowed registered physics identity, allocated on first physics use. |
| `PhysicsDirectSpaceState DirectSpaceState { get; }` | Cached live query view; disposed views are recreated. |
| `CreateDuplicateInstance()` / `CopyCustomStateTo(...)` | Borrow the complete runtime without cloning solver state; bound target replacement rejects. |
| `ValidateDisposal()` / `Dispose(bool)` | Validate unbound final native ownership and release the wrapper's runtime reference, including after a failed GPU interval. Owner/solver/live-body-callback guards remain enforced. |

## Lifecycle and limits

[Physics backend selection](../components/physics-backends.md) defines CPU/GPU creation,
startup fallback and failure semantics. Parameterless creation remains CPU. Duplicates
share the selected runtime, and Viewport.World replacement retains normal identity and
attachment behavior. Selection does not change with the renderer.

[The world contract](../components/worlds.md) defines scene/default versus explicit resource lifetime, wrapper recreation, shared once-per-tick simulation, world replacement and failure recovery. Scene-default identities expire with the tree; caller worlds can remain alive across hosts. RIDs remain borrowed by their owning servers. One runtime has one scene driver and one physics thread. NavigationMap supplies a real lazy map; further navigation topology/baking/avoidance features retain their own exact prerequisites.

[WorldTests](../../tests/Electron2D.Tests/WorldTests.cs) covers headless behavior and actual native shared/independent pixels; [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) covers established direct queries and fallback-world teardown. See [ADR 0063](../decisions/physics.md#adr-0063) and [ADR 0028](../decisions/rendering.md#adr-0028).

## Navigation map integration

World.NavigationMap now lazily owns an active borrowed map in the same runtime lifetime as canvas/physics. Scene NavigationRegion nodes and server-owned regions use that same map storage; the physics lane commits staged topology. NavigationServer is available through Engine named-service lookup. [The navigation contract](../components/navigation-maps.md) records implemented behavior and remaining dependencies.


[Shared public CPU/GPU scene checks](../components/physics-backends.md#public-scene-motion-conformance)
now run WorldTests with explicit backend selection. The linked record
separates verified motion/lifetime/allocation cases from remaining physics coverage.
