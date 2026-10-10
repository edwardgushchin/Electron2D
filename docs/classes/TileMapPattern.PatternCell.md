# TileMapPattern.PatternCell

Last updated: 2026-10-11

**Source:** [TileMapPattern.cs](../../src/Scene/Resources/TileMapPattern.cs). Internal immutable value record; not exported.

Stores SourceID (int), Atlas (Vector2i) and Alternative (int) for one authored pattern entry. Values belong to the existing tile-identity storage domain and may contain -1 sentinels. Dictionary keys store independent nonnegative pattern coordinates. Copies and snapshots copy the values; this record owns no resource/native handles and declares no user-facing API. Validation, portable schema, notifications and lifetime belong to [TileMapPattern](TileMapPattern.md).
