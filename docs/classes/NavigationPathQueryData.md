# NavigationPathQueryData

Last updated: 2026-10-07

- Visibility: internal readonly record struct
- Source: [NavigationPathQueryResult.cs](../../src/Navigation/2D/NavigationPathQueryResult.cs)
- Component: [Navigation maps and queries](../components/navigation-maps.md)

## Description

Complete immutable array/length version published under the result gate. Public getters/setters clone arrays; reset/disposal replace the version with empty data. The query and result use the same real map/corridor kernel; no mutable callback or RID-owned geometry is stored in this value.

## Verification

[NavigationQueryTests](../../tests/Electron2D.Tests/NavigationQueryTests.cs) exercises the consumers, copying, lifecycle and publication boundaries.
