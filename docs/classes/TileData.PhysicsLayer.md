# TileData.PhysicsLayer

Last updated: 2026-10-10

Internal/private implementation. **Source:** [TileData.cs](../../src/Scene/Resources/TileData.cs).

## Description

Owned polygon list and finite constant linear/angular surface velocity.

## Ownership and verification

Owned by the corresponding tile resource or layer; no backend type or public helper API is exposed. [Tile resources and physical layers](../components/tiles.md) records compilation, release, copy, errors and CPU/GPU verification. These records do not allocate on unchanged prepared frames.
