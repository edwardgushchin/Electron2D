# TileSetScenesCollectionSource

Last updated: 2026-10-11

[Resources domain](../domains/resources.md) · [Tiles component](../components/tiles.md#scene-collections) · [TileSetSource](TileSetSource.md)

Owns one tile-coordinate slot at (0,0) and ordered scene alternatives. Templates are borrowed nullable PackedScene resources with nonempty CanvasItem roots. Root validation reads stored compiled factory metadata, without running factory callbacks. Definitions have full nonnegative IDs; automatic IDs start at one and wrap at 1073741823. Cell placement retains the existing identity/flag encoding. Exact authored scene lookup takes precedence over transformed fallback; the layer resolves cell transforms explicitly.

Create/replace rejects invalid/disposed templates before mutation. Successful creation publishes one complete Changed; repeated scene assignment publishes too. Sorted indexing, rename/removal, next-ID cursor, shared template changes/disposal, shallow/deep copies and fresh typed resource/scene graphs execute. Definitions and source disposal do not dispose borrowed templates. Template disposal clears references while retaining nullable definitions.

Inherited source queries expose one coordinate slot, count every definition (including null), and recognize cell transform flags in HasAlternativeTile/GetAlternativeTileID where applicable. Scene-root children are owned by their consuming layer's runtime hierarchy; they are absent from authored ownership storage. Physics/navigation layer callbacks affect atlas data only.

The public runtime methods are CreateSceneTile, GetNextSceneTileID, GetSceneTileID, GetSceneTileScene, GetSceneTilesCount, HasSceneTileID, RemoveSceneTile, SetSceneTileID and SetSceneTileScene, plus the complete inherited source identity family. Editor placeholder accessors are intentionally unexposed until the editor tile workspace, host/hint and actual placeholder drawing exist. Physics descendants retain the existing unit-global-scale/zero-skew contract.

See the component for authoring, lifetime and verification details.

## Own runtime methods

| Member | Behavior |
| --- | --- |
| CreateSceneTile | Validates a borrowed nullable template, chooses/claims an identity, then publishes. |
| GetNextSceneTileID | Returns the retained automatic identity cursor. |
| GetSceneTileID / GetSceneTilesCount | Sorted identity indexing and definition count, including null definitions. |
| GetSceneTileScene | Exact authored lookup, then transformed-cell fallback; returns a borrowed nullable template. |
| HasSceneTileID | Exact authored identity membership. |
| RemoveSceneTile | Removes the definition and subscriptions, preserving borrowed template lifetime. |
| SetSceneTileID | Renames to a free nonnegative identity; existing authored cells are not remapped. |
| SetSceneTileScene | Validated assignment with coherent notification, including repeated assignments. |

All instance methods reject disposed sources. Invalid indices/negative identities, occupied identities, missing definitions and invalid/disposed template roots produce typed exceptions before ordinary mutation. Shader, rendering, physics and navigation limitations belong to the scene's existing descendant types.
