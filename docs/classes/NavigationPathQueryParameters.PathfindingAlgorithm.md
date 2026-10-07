# NavigationPathQueryParameters.PathfindingAlgorithm

Last updated: 2026-10-07

- Source: [NavigationPathQueryParameters.cs](../../src/Navigation/2D/NavigationPathQueryParameters.cs)
- Parent: [NavigationPathQueryParameters](NavigationPathQueryParameters.md)
- Declaration: public enum PathfindingAlgorithm

## Description

Supported path search algorithms. Values retain their numeric query contract and are validated by the owning object. Metadata flags may be combined within IncludeAll; other query modes accept only declared values.

## Enumeration values

| Member | Contract |
| --- | --- |
| [`AStar = 0`](#astar) | Weighted A-star corridor search. |

## Member descriptions

<a id="astar"></a>
### `AStar = 0`

Weighted A-star corridor search.

## Verification

[NavigationQueryTests](../../tests/Electron2D.Tests/NavigationQueryTests.cs) exercises enum identities and their actual query consumers.
