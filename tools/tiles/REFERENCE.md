# Grid geometry reference fixture

The producer `layout_reference.gd` runs with the official pinned 4.7.2 stable executable (`ed1daf0bf001b61586d9930840f2f1394092c079`). It calls the actual TileMapLayer geometry methods with TileSize (30,22) across all 48 shape/layout/axis combinations. The fixture contains 3,888 centers and surrounding arrays, 27,216 named neighbors and 32,352 local-position classifications. Negative/positive parity, all legal directions, square bias, exact sloping boundaries and both sides of selected boundaries are included.

```sh
/path/to/pinned/executable --headless --path /tmp/oracle-project \
  --script /absolute/checkout/tools/tiles/layout_reference.gd -- \
  /absolute/checkout/tests/Electron2D.Tests/Fixtures/Tiles/layout-reference.json
```

The project directory needs an empty `project.godot`. SHA-256 of the generated fixture: `67486f58b25ce2001b28d234e808d502a5dcc0f0b8db547d4228bf0eb0e8edd8`.

TileLayoutTests compares exact integer results, float centers and neighbor order, checks reciprocal directions and rejects shape-inapplicable directions. Overflow is a typed Electron2D adaptation: neighbor intermediates widen to signed 64-bit values and final out-of-range coordinates throw. Missing sets/nonfinite input reject explicitly. Same-value setters retain the repository's quiet change-notification policy. These tests do not copy native overflow or invalid-direction error returns.

## Terrain painting

`terrain_reference.gd` produces `Fixtures/Tiles/terrain-reference.json` with the same official pinned runtime. It covers all 144 shape/layout/axis/matching-mode configurations and eight connect/path/erase/clear stages each, including negative cells, repeated cells, center/peering values and exact unique-pattern source/atlas/alternative identities. The catalog includes every binary empty/terrain-zero combination, with both empty and painted centers. SHA-256: `94983708a12ce1ad91d6b90284d0d2e1302cb531f4cec35a845d0a45b50c13f0`.

The native path implementation logs invalid-neighbor errors for corner directions on offset shapes and returns the input coordinate. Electron2D enumerates actual shared-corner cells instead; the ordinary complete-catalog fixture outputs still agree. Its explicit non-neighbor path exception and preflight overflow checks replace logged/default error handling.

Coherent metadata remapping deliberately corrects pinned defects in [tile_set.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/resources/2d/tile_set.cpp): TileData add/move_terrain omit the center, remove_terrain_set leaves it stale, and forward move_terrain/move_terrain_set shift other identities against the insertion position before accounting for removal. Tests preserve assigned center/peering meaning through insertion, forward/backward movement, removal and clearing; ADR 0101 accepts these pre-release corrections. Inactive peering values remain authored state and typed restoration keeps them.
