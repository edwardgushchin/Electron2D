# TileSetAtlasSource.Tile

Last updated: 2026-10-10

Internal/private implementation. **Source:** [TileSetAtlasSource.cs](../../src/Scene/Resources/TileSetAtlasSource.cs).

## Description

Atlas cell extent and sorted source-owned TileData alternatives.

## Ownership and verification

Owned by the corresponding tile resource or layer; no backend type or public helper API is exposed. [Tile resources and physical layers](../components/tiles.md) records compilation, release, copy, errors and CPU/GPU verification. These records do not allocate on unchanged prepared frames.
