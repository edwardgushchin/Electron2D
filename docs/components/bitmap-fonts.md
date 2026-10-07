# Bitmap and indexed font authoring

Last updated: 2026-10-07

## Scope and owned types

[FontFile](../classes/FontFile.md) owns encoded scalable source and indexed authored [FontCache](../classes/FontCache.md), [FontCacheSize](../classes/FontCacheSize.md), [FontCacheGlyph](../classes/FontCacheGlyph.md) and [FontCachePage](../classes/FontCachePage.md) records. [FixedSizeScaleMode](../classes/FixedSizeScaleMode.md) expresses the bitmap sizing policy. Existing [FontData](../classes/FontData.md), [NativeFontPrecision](../classes/NativeFontPrecision.md), FontVariation and [Text](text.md) remain the sole shaping/render path.

## Runtime flow and invariants

Text or binary v3 BMFont parsing validates required records, lengths, source sizes, Unicode scalars, contiguous pages, image rectangles and duplicate fields before committing. Unicode and nine single-byte OEM codepage families execute through existing BCL encodings. Image pages, packed monochrome channels, combined glyph/outline data and color pages enter copied ordinary image textures. File input and aggregate converted pages are capped at 64 MiB; size dimensions are bounded to current canvas limits. Import failure retains source and old rendering. Type-specific ResourceLoader discovery recognizes fnt/font beside dynamic font extensions; GetDependencies parses descriptor pages and returns their paths with optional ImageTexture type IDs.

Bitmap faces supply nominal scalar glyph mapping, horizontal/vertical advances and bounds through public HarfBuzz font callbacks on FontThread, using a mutable table-empty face with an explicit glyph count and UPEM. They reuse the existing Unicode segmentation/bidi, shaping runs, rounding, fallback, metrics, clipping and retained canvas consumers. No second renderer or hand-coded Unicode shaper exists. Scalable faces retain FreeType/HarfBuzz and native phase/oversampling behavior.

Manual cache state edits are cold authoring operations: copy, validate/native realize, publish, invalidate consumers, notify outside locks, then retire old data after active readers finish. Native pre-rendering populates actual glyph/page metadata and uses x/y/free-width/height shelf tuples, including supplied authoring shelves. UV rectangles remain pixels and draw through shared texture-region clipping. Image queries are independent caller-owned copies; borrowed inputs are copied before their disposal.

Source/size/instance removal has explicit lifetime and index behavior documented on FontFile. Independent configured entries feed matching retained FontVariation resources; common controls observe subsequent file changes. The stored typed hidden snapshot copies scalar records and RGBA pixels in a bounded versioned payload; no native face, callback pointer, thread or GPU identity enters the archive. Resource duplication and fresh-process loading rebuild independent owned native state.

## Verification and limits

FontCacheTests covers real bitmap/dynamic advances, supplementary scalars, metrics, terminal spacing, kerning, fixed-size policies and exact numeric enum values, active-reader retirement, source reverse mapping, copied image/packing queries, native atlas preparation and edge clamp coverage, authored shelves, independent entries/matching variants, failed native realization, page/glyph removal, packed channels, malformed binary block rollback, descriptor page dependencies, observer publication failure, text/binary v3 import, missing-page rollback, resource copies and a fresh process. Prepared measurement and native canvas/Label/RichTextLabel render intervals are separately counted. Current native GPU and compatibility checks verify pixels and 64 prepared intervals with zero managed allocation; native allocator counts, foreign targets and owner visual acceptance remain separate.

Non-SFNT Type 1 dynamic sources still require their FreeType/HarfBuzz nominal/kerning callback integration and fixtures. SystemFont discovery/fallback, language/script overrides, extra antialiasing/embedded-bitmap/autohinter/mipmap modes and MSDF are still exact coverage obligations. This slice does not add inert properties for those mechanisms or claim a complete TextServer service.

Primary format: [AngelCode BMFont specification](https://www.angelcode.com/products/bmfont/doc/file_format.html); native shaping callbacks: [HarfBuzz font API](https://harfbuzz.github.io/harfbuzz-hb-font.html). [ADR 0046](../decisions/rendering.md#adr-0046) defines ownership, units and backend gates.
