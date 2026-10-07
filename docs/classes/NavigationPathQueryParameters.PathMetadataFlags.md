# NavigationPathQueryParameters.PathMetadataFlags

Last updated: 2026-10-07

- Source: [NavigationPathQueryParameters.cs](../../src/Navigation/2D/NavigationPathQueryParameters.cs)
- Parent: [NavigationPathQueryParameters](NavigationPathQueryParameters.md)
- Declaration: [Flags] public enum PathMetadataFlags

## Description

Optional parallel point metadata arrays. Values retain their numeric query contract and are validated by the owning object. Metadata flags may be combined within IncludeAll; other query modes accept only declared values.

## Enumeration values

| Member | Contract |
| --- | --- |
| [`IncludeAll = 7`](#includeall) | All supported arrays. |
| [`IncludeNone = 0`](#includenone) | No metadata arrays. |
| [`IncludeOwners = 4`](#includeowners) | Logical scene owner identities. |
| [`IncludeRIDs = 2`](#includerids) | Owning region/link RIDs. |
| [`IncludeTypes = 1`](#includetypes) | Region/link point types. |

## Member descriptions

<a id="includeall"></a>
### `IncludeAll = 7`

All supported arrays.

<a id="includenone"></a>
### `IncludeNone = 0`

No metadata arrays.

<a id="includeowners"></a>
### `IncludeOwners = 4`

Logical scene owner identities.

<a id="includerids"></a>
### `IncludeRIDs = 2`

Owning region/link RIDs.

<a id="includetypes"></a>
### `IncludeTypes = 1`

Region/link point types.

## Verification

[NavigationQueryTests](../../tests/Electron2D.Tests/NavigationQueryTests.cs) exercises enum identities and their actual query consumers.
