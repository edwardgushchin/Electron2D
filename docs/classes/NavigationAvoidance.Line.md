# NavigationAvoidance.Line

Last updated: 2026-10-07

- Visibility: internal
- Source: [NavigationAvoidance.cs](../../src/Servers/Navigation/NavigationAvoidance.cs)
- Component: [Navigation avoidance](../components/navigation-maps.md#reciprocal-agent-and-obstacle-avoidance)

## Description

One oriented velocity half-plane with point and normalized direction for the solver.

## Verification

[NavigationAvoidanceTests](../../tests/Electron2D.Tests/NavigationAvoidanceTests.cs) checks analytic constraints, independent pinned C++ outputs, guards/lifetime/arrays and warmed stepping. Native consumers separately verify actual movement and pixels.
