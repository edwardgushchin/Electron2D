# TileSet.PhysicsLayer

Last updated: 2026-10-10

Internal/private implementation. **Source:** [TileSet.cs](../../src/Scene/Resources/TileSet.cs).

## Description

Mutable collision layer/mask, positive recovery priority and borrowed PhysicsMaterial entry.

## Ownership and verification

Owned by the corresponding tile resource or layer; no backend type or public helper API is exposed. [Tile resources and physical layers](../components/tiles.md) records compilation, release, copy, errors and CPU/GPU verification. These records do not allocate on unchanged prepared frames.
