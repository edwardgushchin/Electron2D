# TileSet.CustomLayerDefinition

Last updated: 2026-10-11

Internal production type in [TileSet.CustomData.cs](../../src/Scene/Resources/TileSet.CustomData.cs).

Internal ordered definition with exact name and nullable TileCustomDataType factory. Copies are independent; structural/name/type edits stage all definitions, names and tile values before notification.

Owned by the [tile component](../components/tiles.md#typed-custom-data) under [ADR 0101](../decisions/tiles.md#typed-custom-tile-data). TileCustomDataTests covers conversions, schema failure/ownership, typed storage/copy and runtime consumers. No public construction or independent service lifetime exists.
