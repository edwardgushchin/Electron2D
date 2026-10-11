# TileCustomDataType.Generic

Last updated: 2026-10-11

Internal production type in [TileCustomDataType.cs](../../src/Scene/Resources/TileCustomDataType.cs).

Concrete per-T factory/default and conversion implementation. Schema authoring may box values and allocate; stable runtime getters/setters access typed holders directly. Invariant scalar/text and applicable math/color conversions, reset defaults, checked narrowing and bounded nested array text execute.

Owned by the [tile component](../components/tiles.md#typed-custom-data) under [ADR 0101](../decisions/tiles.md#typed-custom-tile-data). TileCustomDataTests covers conversions, schema failure/ownership, typed storage/copy and runtime consumers. No public construction or independent service lifetime exists.
