# WorldRuntime

Last updated: 2026-10-07

- Visibility: internal
- Source: [WorldRuntime.cs](../../src/Scene/Resources/WorldRuntime.cs)
- Component: [Canvas and physics worlds](../components/worlds.md)

## Description

Retains complete world identity independently of public resource wrappers: canvas, lazy registered physics space, resource references, scene driver lease and default-scene expiration policy. Lifetime changes share a cold gate; Alive uses a volatile read from the canvas registry to avoid inverse lock acquisition. It borrows no native backend handle into the public API. Physical access and world-binding changes keep the existing PhysicsSpace thread/callback guards.

Last-resource release uses the physics release guard so a failed GPU interval
does not prevent disposal. Owner-thread, active-solver and live body callback
restrictions remain enforced. Binding a failed world still rejects.

## Verification

[WorldTests](../../tests/Electron2D.Tests/WorldTests.cs) checks sharing, transfer, wrapper replacement, query identity, notifications, once-per-tick simulation and real GPU/compatibility canvas lifetime. [The world contract](../components/worlds.md) records current limits.

## Navigation map integration

World.NavigationMap now lazily owns an active borrowed map in the same runtime lifetime as canvas/physics. Scene NavigationRegion nodes and server-owned regions use that same map storage; the physics lane commits staged topology. NavigationServer is available through Engine named-service lookup. [The navigation contract](../components/navigation-maps.md) records implemented behavior and remaining dependencies.
