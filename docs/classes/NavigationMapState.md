# NavigationMapState

Last updated: 2026-10-07

- Visibility: internal
- Source: [NavigationServer.cs](../../src/Servers/Navigation/NavigationServer.cs)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

Staged map flags/settings, owner and latest committed iteration.

## Verification

[NavigationTests](../../tests/Electron2D.Tests/NavigationTests.cs) exercises the concrete geometry/query/lifetime consumers. The [Authored navigation maps](../components/navigation-maps.md) contract records limits.
