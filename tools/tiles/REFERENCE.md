# Grid geometry reference fixture

The producer `layout_reference.gd` runs with the official pinned 4.7.2 stable executable (`ed1daf0bf001b61586d9930840f2f1394092c079`). It calls the actual TileMapLayer geometry methods with TileSize (30,22) across all 48 shape/layout/axis combinations. The fixture contains 3,888 centers and surrounding arrays, 27,216 named neighbors and 32,352 local-position classifications. Negative/positive parity, all legal directions, square bias, exact sloping boundaries and both sides of selected boundaries are included.

```sh
/path/to/pinned/executable --headless --path /tmp/oracle-project \
  --script /absolute/checkout/tools/tiles/layout_reference.gd -- \
  /absolute/checkout/tests/Electron2D.Tests/Fixtures/Tiles/layout-reference.json
```

The project directory needs an empty `project.godot`. SHA-256 of the generated fixture: `67486f58b25ce2001b28d234e808d502a5dcc0f0b8db547d4228bf0eb0e8edd8`.

TileLayoutTests compares exact integer results, float centers and neighbor order, checks reciprocal directions and rejects shape-inapplicable directions. Overflow is a typed Electron2D adaptation: neighbor intermediates widen to signed 64-bit values and final out-of-range coordinates throw. Missing sets/nonfinite input reject explicitly. Same-value setters retain the repository's quiet change-notification policy. These tests do not copy native overflow or invalid-direction error returns.
