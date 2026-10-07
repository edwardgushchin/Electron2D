# NavigationPolygonData

Last updated: 2026-10-07

- Visibility: internal
- Source: [NavigationPolygon.cs](../../src/Navigation/2D/NavigationPolygon.cs)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

Immutable resource version retaining private vertex/index arrays.

## Verification

[NavigationTests](../../tests/Electron2D.Tests/NavigationTests.cs) exercises the concrete geometry/query/lifetime consumers. The [Authored navigation maps](../components/navigation-maps.md) contract records limits.
