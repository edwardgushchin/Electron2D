# TileCustomDataType

Last updated: 2026-10-11

Internal production type in [TileCustomDataType.cs](../../src/Scene/Resources/TileCustomDataType.cs).

Abstract internal direct typed factory, stable portable ID, configured defaults and cold conversion. Registration uses ResourceLoader's retained schema registry under its existing gate; built-in codec initialization completes before lookup. Failed conversion prepares no authored mutation.

Owned by the [tile component](../components/tiles.md#typed-custom-data) under [ADR 0101](../decisions/tiles.md#typed-custom-tile-data). TileCustomDataTests covers conversions, schema failure/ownership, typed storage/copy and runtime consumers. No public construction or independent service lifetime exists.
