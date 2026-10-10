# NavigationMapIteration.Portal

Last updated: 2026-10-10

- Visibility: internal
- Source: [NavigationMapIteration.cs](../../src/Servers/Navigation/NavigationMapIteration.cs)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

Copied corridor endpoint pair and target cell index.

## Verification

[NavigationTests](../../tests/Electron2D.Tests/NavigationTests.cs) exercises the concrete geometry/query/lifetime consumers. The [Authored navigation maps](../components/navigation-maps.md) contract records limits.

Centered optionally stores the physical target-edge midpoint, separate from margin/funnel A/B pathways; synthetic link point portals keep their point fallback.
