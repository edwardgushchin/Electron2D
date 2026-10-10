# TileMapLayer.GeneratedNavigation

Last updated: 2026-10-10

Internal/private implementation. **Source:** [TileMapLayer.Navigation.cs](../../src/Scene/2D/TileMapLayer.Navigation.cs).

## Description

Node-owned region RID, borrowed effective polygon and cell-local drawing transform.

Owned by the corresponding tile data or layer. [Tile navigation](../components/tiles.md#navigation-layers) records lifetime, copying, storage and verification. Transient records are not serialized.
