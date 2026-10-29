# FontData

Last updated: 2026-10-29

**Visibility:** internal · **Source:** [FontData.cs](../../src/Servers/Text/FontData.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Owns immutable encoded bytes, one native precision face, metadata, prepared metric/index/advance caches and CPU glyph textures. Calls are synchronized and dispatched through FontThread. Raster keys include size, outline, effective oversampling, hinting and fractional phase. Source replacement retires the complete old data object. Read leases keep its native face alive through in-flight shaping or rasterization; the last reader explicitly frees the face, encoded bytes and caches. Retirement clears each glyph image’s renderer-residency request without invalidating immutable pixels retained by recorded commands. An unused frame then releases the native texture payload. The finalizer can retire unreferenced native state independently of renderer texture keys.

A same-native-thread allocation refusal retires only the mutable precision face. Immutable source/configuration and previously completed managed caches remain available. A later native cache miss recreates and configures the face on its existing text owner; name queries also reopen a retired face. Native failure is surfaced as OutOfMemoryException before the operation result is published. Complete source retirement retains its separate reader and texture lifetime rules.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.

Nonempty data accepts the packaged desktop and Android process architectures, including Windows/Android x86 and Android ARM32, iOS/tvOS ARM64 and x64 simulator hosts, and browser Wasm32 with static native references. A matching native library and working host remain required; accepting the architecture does not establish target execution. The browser test host executes public WOFF2 resources, precision/raster fixtures and dictionary layout on its nonthreaded owner.

## Bitmap/indexed font integration

[Bitmap font authoring](../components/bitmap-fonts.md) connects FontFile indexed image/glyph/kerning/metric records and matching configured FontVariation resources to the existing HarfBuzz and common canvas/control path. Copied pixel UV regions preserve clipping and recorded image snapshots; authored publication retires native data after active readers finish. Text/binary v3 import, typed archive/fresh-process restoration and current Linux GPU/compatibility prepared output are exercised. Source policies and other platform/native-allocator gates remain explicit.

## System font integration

[System font matching](../components/system-fonts.md) adds installed families/styles/logical collection faces and owned automatic text fallback over the shared native owner and canvas path. FontFile.AllowSystemFallback defaults to true; explicit resources retain precedence and explicit support queries remain distinct from automatic rendered coverage. Active parent readers retain retired fallback faces through policy changes. SystemFont archives store preferences and rematch the host. The current Linux catalog and both canvas consumers are exercised; CoreText/DirectWrite, extra raster/MSDF, native allocator and foreign acceptance gates remain explicit.

## Support metadata

Retains an immutable FontSupportPolicy snapshot and the native face's automatic script set. Primary support queries serialize with data lifetime checks; layout priority retains supported-script/supported-language, supported-script/unsupported-language and last-resort tiers. Color faces have preferred emoji presentation. Configuration copies and FontVariation realizations carry policy; FontFile commits changes to every realized indexed data object before notifying consumers. Fallback-priority language lookup borrows a read-only span from the layout; public exact language queries and native font ownership remain unchanged.

## Raster policy snapshots

Owns native monochrome-strike, forced-autohinter and MIP-generation policy. FontFile publishes replacement snapshots on changes; configured variations and automatic system sources copy effective settings. Native raster calls select the real FreeType flags, while metrics keep unhinted precision. Both standalone images and atlas pages generate complete MIP chains before texture publication. Generated page retirement preserves authored images, recorded pixels and active old readers.

The primary realized face supplies OpenType metadata through `GetSupportedFeatureList()` and `GetOTNameStrings()`. The feature snapshot contains numeric tags and typed optional labels, boolean/integer selection and hidden flags. GSUB is read before GPOS, whose duplicate descriptions win. Name strings are copied by native language spelling and standard role; unknown IDs retain `unknown_N` keys. These cold snapshots allocate independently and omit fallback metadata or authored family overrides. Feature labels retain the tool locale sampled during native realization; source replacement or cache retirement rebuilds metadata.

Localized name capture is lazy, runs on the existing serialized FontThread operation slot, publishes only a completed map and enforces a 64 MiB expanded-text boundary. Required feature descriptions are captured with the native face.

Retirement clears supported-feature and localized-name maps only after the final retained reader releases the source. Returned public snapshots remain independent of that cleanup.

Antialiasing is copied into configured variants and owned automatic fallback sources. Native raster results publish actual LCD coverage flags into ordinary and packed glyph snapshots. Source replacement discards generated pages while preserving authored records and active old readers.

## MSDF integration

MSDF source settings propagate through owned variants and applicable automatic fallbacks. Canonical source/zero-outline images reuse residency across drawn sizes; returned value snapshots carry range, scale and outline metadata. Authored metrics project to requested size, while unhinted shaping remains independent. Color/non-scalable owned fallbacks retain ordinary images.

Type 1 PFB/PFA sources share native shaping and raster/MSDF consumers. The common file reader resolves optional PFM/AFM metrics and preserves their byte snapshot in variations and reopened native faces. Linux catalog discovery admits scalable Type 1 sources. See [source-pair behavior and verification](../components/text.md#type-1-outline-and-auxiliary-metric-sources).

## Source registration and automatic fallback reads

SourceOwner weakly identifies the owning FontFile cache. Automatic fallback entries retain private FontFile owners and one current data read, with retired reads deferred across active parent readers. Child-generation checks invalidate coherent reads even before a Changed callback completes. Managed file retirement is queued to the owning Font and drained outside font/native gates; native cleanup only enqueues. Private baseline-shift measurement cancels the source's quantized native baseline when an independent linked projection supplies its own offset.

Automatic fallback realization keys its cache by the effective four spacing values and baseline, so linked projections sharing a native source retain independent derived font identities. Derived files inherit the source instance's coordinates, embolden, transform, fixed-size policy and raster configuration. Parent spacing/baseline edits invalidate derived caches; direct child cache edits remain real runtime overrides until that invalidation. The existing sixty-four derived-source bound applies to realized parameter sets. Fallback discovery uses each span's language. Apply the effective baseline in the derived native instance, correcting the pinned factory's captured-but-unused baseline field. Keep public virtual GetSpacing policies active while projecting actual source-cache spacing.

Native data reads and managed source disposal have separate endpoints. Retirement marks lookups unavailable immediately, while per-thread deferred owner lists drain managed files after the outermost font read exits. Native cleanup and source-file gates defer delivery; reentrant reads keep independent contexts. Resource projections and owned resource-linked entries retain their original owner until disposal, then release those references. Weak RID lookup alone never retains a resource.

Automatic native glyphs register matching active raster levels. Last-owner release removes that automatic factor’s cache records and generated pages; explicitly requested factors remain source-owned until cache retirement, and retained canvas commands keep immutable pixels until renderer eviction. MSDF and authored bitmap data retain their existing policies.

The concrete Fallback text profile selects a separate nominal scalar glyph path in the existing direct layout and font worker. Retained glyph scratch storage uses real FreeType/bitmap advances, native pair kerning and existing authored-cache overrides; it performs no contextual substitutions. Advanced shaping retains the existing path. See [concrete text profiles](../components/text-server.md#concrete-implementation-profiles).
