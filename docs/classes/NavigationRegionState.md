# NavigationRegionState

Last updated: 2026-10-10

- Visibility: internal
- Source: [NavigationServer.cs](../../src/Servers/Navigation/NavigationServer.cs)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

Staged region geometry, costs, layers, transform, source ownership and resource subscriptions.

A dirty authored geometry/configuration version builds an independent committed region iteration before map assembly. Bounds and closest-point queries retain that geometry when detached/disabled. Resource subscriptions stage edits; weak scene identities are swept at synchronization.

## Verification

[NavigationTests](../../tests/Electron2D.Tests/NavigationTests.cs) exercises the concrete geometry/query/lifetime consumers. The [Authored navigation maps](../components/navigation-maps.md) contract records limits.

PublishedMap preserves pathway membership until synchronization. IterationID records a nonzero wrapping uint identity, returned through the static ulong query; geometry/configuration publication remains transactional.
