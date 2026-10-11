# TileCustomDataValue

Last updated: 2026-10-11

Internal production type in [TileCustomDataType.cs](../../src/Scene/Resources/TileCustomDataType.cs).

Abstract internal per-layer holder with actual schema identity, cold resource projection/copy and typed property descriptor construction. It has no public dynamic value API.

Owned by the [tile component](../components/tiles.md#typed-custom-data) under [ADR 0101](../decisions/tiles.md#typed-custom-tile-data). TileCustomDataTests covers conversions, schema failure/ownership, typed storage/copy and runtime consumers. No public construction or independent service lifetime exists.
