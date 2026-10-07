# FontData

Last updated: 2026-10-07

**Visibility:** internal · **Source:** [FontData.cs](../../src/Servers/Text/FontData.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Owns immutable encoded bytes, one native precision face, metadata, prepared metric/index/advance caches and CPU glyph textures. Calls are synchronized and dispatched through FontThread. Raster keys include size, outline, effective oversampling, hinting and fractional phase. Source replacement retires the complete old data object. Read leases keep its native face alive through in-flight shaping or rasterization; the last reader explicitly frees the face, encoded bytes and caches. Retirement clears each glyph image’s renderer-residency request without invalidating immutable pixels retained by recorded commands. An unused frame then releases the native texture payload. The finalizer can retire unreferenced native state independently of renderer texture keys.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.

Nonempty data accepts the packaged desktop and Android process architectures, including Windows/Android x86 and Android ARM32, iOS/tvOS ARM64 and x64 simulator hosts, and browser Wasm32 with static native references. A matching native library and working host remain required; accepting the architecture does not establish target execution. The browser test host executes public WOFF2 resources, precision/raster fixtures and dictionary layout on its nonthreaded owner.

## Bitmap/indexed font integration

[Bitmap font authoring](../components/bitmap-fonts.md) connects FontFile indexed image/glyph/kerning/metric records and matching configured FontVariation resources to the existing HarfBuzz and common canvas/control path. Copied pixel UV regions preserve clipping and recorded image snapshots; authored publication retires native data after active readers finish. Text/binary v3 import, typed archive/fresh-process restoration and current Linux GPU/compatibility prepared output are exercised. Source policies and other platform/native-allocator gates remain explicit.

## System font integration

[System font matching](../components/system-fonts.md) adds installed families/styles/logical collection faces and owned automatic text fallback over the shared native owner and canvas path. FontFile.AllowSystemFallback defaults to true; explicit resources retain precedence and explicit support queries remain distinct from automatic rendered coverage. Active parent readers retain retired fallback faces through policy changes. SystemFont archives store preferences and rematch the host. The current Linux catalog and both canvas consumers are exercised; CoreText/DirectWrite, extra raster/MSDF, native allocator and foreign acceptance gates remain explicit.
