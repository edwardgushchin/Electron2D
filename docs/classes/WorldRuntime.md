# WorldRuntime

Last updated: 2026-10-07

- Visibility: internal
- Source: [WorldRuntime.cs](../../src/Scene/Resources/WorldRuntime.cs)
- Component: [Canvas and physics worlds](../components/worlds.md)

## Description

Retains complete world identity independently of public resource wrappers: canvas, lazy registered physics space, resource references, scene driver lease and default-scene expiration policy. Lifetime changes share a cold gate; Alive uses a volatile read from the canvas registry to avoid inverse lock acquisition. It borrows no native backend handle into the public API. Physical access and world-binding changes keep the existing PhysicsSpace thread/callback guards.

## Verification

[WorldTests](../../tests/Electron2D.Tests/WorldTests.cs) checks sharing, transfer, wrapper replacement, query identity, notifications, once-per-tick simulation and real GPU/compatibility canvas lifetime. [The world contract](../components/worlds.md) records current limits.
