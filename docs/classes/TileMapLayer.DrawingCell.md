# TileMapLayer.DrawingCell

Last updated: 2026-10-11

Internal production record in [TileMapLayer.DrawingPolicy.cs](../../src/Scene/2D/TileMapLayer.DrawingPolicy.cs). Cold planning record for cell/source/data/texture and retained command preparation.

Owned by [tile drawing](../components/tiles.md#per-tile-materials-and-draw-order) under ADR 0101. Planning allocates when authored data changes; stable frames retain prepared canvas items. TileDrawingTests exercises native ordering, all grid placements and failure preflight. No public construction or persistent identity exists.
