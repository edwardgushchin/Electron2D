# TileCustomDataValue.Generic

Last updated: 2026-10-11

Internal production type in [TileCustomDataType.cs](../../src/Scene/Resources/TileCustomDataType.cs).

Per-T mutable actual value holder. Exact repeated assignments reuse the holder. Runtime/shallow copies own separate holders with borrowed values; deep copies use StoredPropertyValue resource remapping and array snapshots. Typed file descriptors retain actual T.

Owned by the [tile component](../components/tiles.md#typed-custom-data) under [ADR 0101](../decisions/tiles.md#typed-custom-tile-data). TileCustomDataTests covers conversions, schema failure/ownership, typed storage/copy and runtime consumers. No public construction or independent service lifetime exists.
