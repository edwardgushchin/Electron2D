# NavigationMapIteration.Cell

Last updated: 2026-10-10

- Visibility: internal
- Source: [NavigationMapIteration.cs](../../src/Servers/Navigation/NavigationMapIteration.cs)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

One transformed convex cell with committed region identity and routing values.

## Verification

[NavigationTests](../../tests/Electron2D.Tests/NavigationTests.cs) exercises the concrete geometry/query/lifetime consumers. The [Authored navigation maps](../components/navigation-maps.md) contract records limits.

IsLink distinguishes synthetic off-surface owner/cost/layer cells from convex walkable regions; synthetic cells are excluded from surface projection.

OwnerID and link endpoint region RIDs are copied into immutable cells for consistent result metadata and include/exclude traversal filters. RegionSetOwnerID stages a rebuild; result readers retain the prior version until sync.

Enabled is committed alongside geometry, masks and costs; disabled authored cells remain countable but are excluded from map path/projection and random sampling.
