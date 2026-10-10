# NavigationMapState

Last updated: 2026-10-10

- Visibility: internal
- Source: [NavigationServer.cs](../../src/Servers/Navigation/NavigationServer.cs)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

Staged map flags/settings, owner and latest committed iteration.

## Verification

[NavigationTests](../../tests/Electron2D.Tests/NavigationTests.cs) exercises the concrete geometry/query/lifetime consumers. The [Authored navigation maps](../components/navigation-maps.md) contract records limits.

LinkRadius is initially four world units and consumes strict endpoint-to-polygon attachment in map construction.

CellSize=1 and RasterScale=0.1 stage real raster dimensions and rebuild attached regions. Its immutable iteration carries topology profiling and margin pathways.
