# ZIPCompressionLevel

Last updated: 2026-10-14

**Namespace:** Electron2D. **Source:** [ZIPAppend.cs](../../src/Core/IO/ZIPAppend.cs). **Component:** [ZIP archives](../components/zip-archives.md).

- `public const Electron2D.ZIPCompressionLevel Best = 9`
- `public const Electron2D.ZIPCompressionLevel Default = -1`
- `public const Electron2D.ZIPCompressionLevel Fast = 1`
- `public const Electron2D.ZIPCompressionLevel None = 0`

Names common deflate levels. ZIPPacker.CompressionLevel remains an integer because every level -1..9 is valid; GetCompressionLevel reads only the speed hint stored in ZIP flags.
