# NavigationAvoidance.V

Last updated: 2026-10-07

- Visibility: internal
- Source: [NavigationAvoidance.cs](../../src/Servers/Navigation/NavigationAvoidance.cs)
- Component: [Navigation avoidance](../components/navigation-maps.md#reciprocal-agent-and-obstacle-avoidance)

## Description

Internal double vector preserves finite world-space differences, dot/determinant products and normalization without changing public Vector2.

## Verification

[NavigationAvoidanceTests](../../tests/Electron2D.Tests/NavigationAvoidanceTests.cs) checks analytic constraints, independent pinned C++ outputs, guards/lifetime/arrays and warmed stepping. Native consumers separately verify actual movement and pixels.
