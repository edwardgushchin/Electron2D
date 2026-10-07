# NavigationPathQueryParameters.PathPostProcessing

Last updated: 2026-10-07

- Source: [NavigationPathQueryParameters.cs](../../src/Navigation/2D/NavigationPathQueryParameters.cs)
- Parent: [NavigationPathQueryParameters](NavigationPathQueryParameters.md)
- Declaration: public enum PathPostProcessing

## Description

Path corridor output processing modes. Values retain their numeric query contract and are validated by the owning object. Metadata flags may be combined within IncludeAll; other query modes accept only declared values.

## Enumeration values

| Member | Contract |
| --- | --- |
| [`CorridorFunnel = 0`](#corridorfunnel) | Funnel corners with crossed-edge provenance. |
| [`EdgeCentered = 1`](#edgecentered) | Crossed portal midpoints. |
| [`None = 2`](#none) | Search entry positions without geometric postprocessing. |

## Member descriptions

<a id="corridorfunnel"></a>
### `CorridorFunnel = 0`

Funnel corners with crossed-edge provenance.

<a id="edgecentered"></a>
### `EdgeCentered = 1`

Crossed portal midpoints.

<a id="none"></a>
### `None = 2`

Search entry positions without geometric postprocessing.

## Verification

[NavigationQueryTests](../../tests/Electron2D.Tests/NavigationQueryTests.cs) exercises enum identities and their actual query consumers.
