# TileMapLayer.GeneratedBody

Last updated: 2026-10-10

Internal/private implementation. **Source:** [TileMapLayer.Physics.cs](../../src/Scene/2D/TileMapLayer.Physics.cs).

## Description

Layer-owned server body RID, quadrant identity, generated shape resources and retained drawing transform.

## Ownership and verification

Owned by the corresponding tile resource or layer; no backend type or public helper API is exposed. [Tile resources and physical layers](../components/tiles.md) records compilation, release, copy, errors and CPU/GPU verification. These records do not allocate on unchanged prepared frames.
