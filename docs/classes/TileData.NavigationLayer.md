# TileData.NavigationLayer

Last updated: 2026-10-10

Internal/private implementation. **Source:** [TileData.Navigation.cs](../../src/Scene/Resources/TileData.Navigation.cs).

## Description

Borrowed original polygon, seven transformed resource slots and subscription/cache cleanup.

Owned by the corresponding tile data or layer. [Tile navigation](../components/tiles.md#navigation-layers) records lifetime, copying, storage and verification. Transient records are not serialized.
