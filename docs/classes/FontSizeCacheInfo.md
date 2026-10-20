# FontSizeCacheInfo

Last updated: 2026-10-20

A caller-owned nominal size/outline cache snapshot with record/page counts, explicit atlas byte estimate and realized raster factors.

Source: [FontGlyphContours.cs](../../src/Servers/Text/FontGlyphContours.cs). See the complete [owned font-cache contract](../components/text-server.md#owned-font-sources-and-caches). Returned arrays are independent cold snapshots. These values own no native resources.
