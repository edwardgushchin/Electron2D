# NavigationMapIteration

Last updated: 2026-10-07

- Visibility: internal
- Source: [NavigationMapIteration.cs](../../src/Servers/Navigation/NavigationMapIteration.cs)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

Immutable committed cells/portals, projection, weighted corridor search and funnel output.

## Verification

[NavigationTests](../../tests/Electron2D.Tests/NavigationTests.cs) exercises the concrete geometry/query/lifetime consumers. The [Authored navigation maps](../components/navigation-maps.md) contract records limits.

Synthetic link cells and point portals participate in weighted corridors; surface projection and closest-reachable region selection exclude them. Attachment uses nearest region surfaces strictly inside the configured map radius.
