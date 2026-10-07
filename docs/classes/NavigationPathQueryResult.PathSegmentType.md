# NavigationPathQueryResult.PathSegmentType

Last updated: 2026-10-07

- Source: [NavigationPathQueryResult.cs](../../src/Navigation/2D/NavigationPathQueryResult.cs)
- Parent: [NavigationPathQueryResult](NavigationPathQueryResult.md)
- Declaration: public enum PathSegmentType

## Description

Primitive owning a returned path point. Values retain their numeric query contract and are validated by the owning object. Metadata flags may be combined within IncludeAll; other query modes accept only declared values.

## Enumeration values

| Member | Contract |
| --- | --- |
| [`Link = 1`](#link) | Off-surface navigation link. |
| [`Region = 0`](#region) | Walkable navigation region. |

## Member descriptions

<a id="link"></a>
### `Link = 1`

Off-surface navigation link.

<a id="region"></a>
### `Region = 0`

Walkable navigation region.

## Verification

[NavigationQueryTests](../../tests/Electron2D.Tests/NavigationQueryTests.cs) exercises enum identities and their actual query consumers.
