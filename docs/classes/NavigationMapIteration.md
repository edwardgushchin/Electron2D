# NavigationMapIteration

Last updated: 2026-11-13

- Visibility: internal
- Source: [NavigationMapIteration.cs](../../src/Servers/Navigation/NavigationMapIteration.cs)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

Immutable committed cells and portals support projection, weighted corridor search and funnel output. Each directed portal and exact entry position retain their best search arrival with predecessor and portal point. Different positions remain alternatives even at equal cost; the same state is replaced only by a strictly cheaper arrival. Queue ties prefer greater traveled cost, then published polygon order and discovery order. Goal arrivals compare the complete weighted cost. The polygon budget counts distinct processed polygons.

## Verification

[NavigationTests](../../tests/Electron2D.Tests/NavigationTests.cs) exercises the concrete geometry/query/lifetime consumers. The [Authored navigation maps](../components/navigation-maps.md) contract records limits.

Synthetic link cells and point portals participate in weighted corridors; surface projection and closest-reachable region selection exclude them. Attachment uses nearest region surfaces strictly inside the configured map radius.

The shared [Query kernel](../../src/Servers/Navigation/NavigationMapIteration.Query.cs) consumes filters/search budgets and produces primitive provenance, three output modes, RDP and length/radius clips. MapGetPath delegates to the same kernel with metadata disabled. A fallback search uses the reached portal entry to select a walkable polygon and rebuilds a corridor to the original target projected onto it.

The [topology builder](../../src/Servers/Navigation/NavigationMapIteration.Topology.cs) groups region and map raster edges, retains the first two owners and publishes directed margin arrays plus five complete geometry counters. Disabled cells remain authored data but query traversal excludes them. The [sampling cache](../../src/Servers/Navigation/NavigationMapIteration.Sampling.cs) prepares contiguous region groups and surface areas once per snapshot; random reads allocate no managed scratch. Raw paths preserve physical target-edge midpoint data separately from funnel pathways.
