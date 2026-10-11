# TileCustomDataNumbers

Last updated: 2026-10-11

Internal production type in [TileCustomDataType.cs](../../src/Scene/Resources/TileCustomDataType.cs).

Cold schema text parser: integer scans ASCII digits to the first decimal, toggles pre-number minus signs, ignores other characters and saturates Int64; float reads an invariant signed decimal/exponent prefix and retains the numeric prefix before an incomplete exponent.

Owned by the [tile component](../components/tiles.md#typed-custom-data) under [ADR 0101](../decisions/tiles.md#typed-custom-tile-data). TileCustomDataTests covers conversions, schema failure/ownership, typed storage/copy and runtime consumers. No public construction or independent service lifetime exists.
