# NavigationPathQuerySettings

Last updated: 2026-10-07

- Visibility: internal readonly record struct
- Source: [NavigationPathQueryParameters.cs](../../src/Navigation/2D/NavigationPathQueryParameters.cs)
- Component: [Navigation maps and queries](../components/navigation-maps.md)

## Description

Copied scalar settings and immutable filter array versions captured under the parameter gate. Defaults include A-star/funnel/all metadata, layer one and 4096 processed polygons. The query and result use the same real map/corridor kernel; no mutable callback or RID-owned geometry is stored in this value.

## Verification

[NavigationQueryTests](../../tests/Electron2D.Tests/NavigationQueryTests.cs) exercises the consumers, copying, lifecycle and publication boundaries.
