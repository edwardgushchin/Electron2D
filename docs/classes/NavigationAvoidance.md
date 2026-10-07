# NavigationAvoidance

Last updated: 2026-10-07

- Visibility: internal
- Source: [NavigationAvoidance.cs](../../src/Servers/Navigation/NavigationAvoidance.cs)
- Component: [Navigation avoidance](../components/navigation-maps.md#reciprocal-agent-and-obstacle-avoidance)

## Description

Managed Apache-2.0 ORCA adaptation. Reused nearest-neighbor/edge constraints and linear-program workspaces compute from simultaneous pre-step state; doubles, zero-horizon handling and deterministic coincident normals prevent nonfinite publication. Quadratic scan is the current measured scaling boundary.

## Verification

[NavigationAvoidanceTests](../../tests/Electron2D.Tests/NavigationAvoidanceTests.cs) checks analytic constraints, independent pinned C++ outputs, guards/lifetime/arrays and warmed stepping. Native consumers separately verify actual movement and pixels.
