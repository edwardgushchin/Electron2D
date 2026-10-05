# FontData

Last updated: 2026-10-05

**Visibility:** internal · **Source:** [FontData.cs](../../src/Servers/Text/FontData.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Owns immutable encoded bytes, one native precision face, metadata, prepared metric/index/advance caches and CPU glyph textures. Calls are synchronized and dispatched through FontThread. Raster keys include size, outline, effective oversampling, hinting and fractional phase. Source replacement retires the complete old data object. Read leases keep its native face alive through in-flight shaping or rasterization; the last reader explicitly frees the face, encoded bytes and caches. Retirement clears each glyph image’s renderer-residency request without invalidating immutable pixels retained by recorded commands. An unused frame then releases the native texture payload. The finalizer can retire unreferenced native state independently of renderer texture keys.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.

Nonempty data accepts the packaged desktop and Android process architectures, including Windows/Android x86 and Android ARM32, iOS/tvOS ARM64 and x64 simulator hosts, and browser Wasm32 with static native references. A matching native library and working host remain required; accepting the architecture does not establish target execution. The browser test host executes public WOFF2 resources, precision/raster fixtures and dictionary layout on its nonthreaded owner.
