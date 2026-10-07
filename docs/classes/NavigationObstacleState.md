# NavigationObstacleState

Last updated: 2026-10-07

- Visibility: internal
- Source: [NavigationServer.Obstacles.cs](../../src/Servers/Navigation/NavigationServer.Obstacles.cs)
- Component: [Navigation avoidance](../components/navigation-maps.md#reciprocal-agent-and-obstacle-avoidance)

## Description

Weak node/caller-owned RID with source map/pause/enabled/layers/position/velocity/radius and copied contour. Circle prediction is moving; contour prediction remains static.

## Verification

[NavigationAvoidanceTests](../../tests/Electron2D.Tests/NavigationAvoidanceTests.cs) checks analytic constraints, independent pinned C++ outputs, guards/lifetime/arrays and warmed stepping. Native consumers separately verify actual movement and pixels.
