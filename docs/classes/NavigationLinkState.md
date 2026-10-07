# NavigationLinkState

Last updated: 2026-10-07

- Visibility: internal
- Source: [NavigationServer.Links.cs](../../src/Servers/Navigation/NavigationServer.Links.cs)
- Component: [Authored navigation maps](../components/navigation-maps.md)

## Description

One caller- or node-owned link RID, weak scene owner, staged map membership, direction/enable/layer/cost/global endpoint settings, logical instance owner and dirty/version state. The retained server owns storage; disposal/weak sweep releases identities. Changed attached links publish their nonzero 32-bit iteration counter only after all map builds succeed. Map deletion detaches surviving links. Immutable map cells retain copied link geometry/settings for readers; staged edits do not mutate returned paths.

## Verification

[NavigationLinkTests](../../tests/Electron2D.Tests/NavigationLinkTests.cs) exercises consumers, version rollback/wrap, copying, ownership and lifecycle.
