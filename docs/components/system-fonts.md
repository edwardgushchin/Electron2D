# System font matching and automatic fallback

Last updated: 2026-10-07

## Scope and ownership

[SystemFont](../classes/SystemFont.md) connects installed family/style/collection/variable matching to ordinary Font text consumers. [OS](../classes/OS.md) statically exposes GetSystemFonts, GetSystemFontPath and GetSystemFontPathForText through its permanent retained object. Internal [NativeSystemFonts](../classes/NativeSystemFonts.md) owns an optional host Fontconfig catalog and bounded cold request cache; [SystemFontMatch](../classes/SystemFontMatch.md) carries source path/native face/family. No source paths are installed or modified and no shell font command runs in production.

## Runtime flow

Linux enumeration filters scalable TrueType/CFF fonts and returns copied ordered family aliases. Style matching uses configured family preferences, OpenType weight conversion, width and slant. Text matching adds scalar coverage and locale, returning ordered file/face records contributing actual requested coverage. Fontconfig native pattern/charset/object/font-set handles are released on every path; the catalog retains its owned configuration. Missing optional library/catalog or unsupported providers produce absent query results.

SystemFont loads bounded source bytes, scores all collection faces for family/weight/width/italic, retains tied logical faces, and applies supported variable axes when the match is not exact. Its independent native data uses the existing FontThread owner, unhinted shaping, rounding, phase-aware raster, clipping and retained texture lifecycle. Matching FontVariation instances map logical indices and merge system default coordinates with explicit override precedence. Unresolved requests clone native data from the borrowed theme font, watch it weakly and follow theme replacement without disposing the theme.

FontFile.AllowSystemFallback defaults to true and applies across source replacement, variants, duplication and bitmap loading. Shared layout first tries complete grapheme coverage in explicit sources, then a complete system source before scalar-level missing fallback. Automatic faces are owned by their parent FontData; disabling/changing policies clears future lookup and retires old faces after active parent reads finish. Source retirement recursively frees owned faces. HasChar/GetSupportedChars continue describing retained explicit source coverage; shaped/drawn/character-size queries may additionally resolve platform fallback.

Cold source/callback requests allocate. Prepared scalar/cluster lookups, cached measurement and active rendering reuse native source and texture storage. Request metadata is bounded to 4096 catalog keys, 4096 cluster keys and 65536 scalar keys; a parent retains at most 64 native fallback sources. These ceilings do not imply arbitrary-font/whole-world performance acceptance.

## Storage and verification

Ten typed SystemFont preferences and FontFile's fallback permission are stored with the existing resource schemas. Duplication reconstructs owned native state; fresh-process archives rematch their host instead of persisting machine-specific font paths. Tests isolate Fontconfig via child-only FONTCONFIG_FILE and temporary fixture/cache directories, preserving the workstation catalog. Separate variable and collection catalogs avoid ambiguous fixture styles; empty catalogs verify theme fallback and replacement.

SystemFontTests checks real native matches, variable glyph advance changes, tied logical faces, explicit coordinates, active retirement, copied arrays, invalid queries, silent equal setters, inherited policy behavior and prepared measurement. Current Linux GPU/compatibility capture verifies canvas/Label/RichTextLabel and a distinct automatic Arabic source, with 64 prepared render intervals per backend at zero managed bytes. Native allocators, foreign platforms, physical/editor and owner acceptance remain separate. Missing CoreText/DirectWrite and native raster/MSDF layers are recorded per coverage row.

Primary native matching source: [Fontconfig manual](https://fontconfig.pages.freedesktop.org/fontconfig/fontconfig-user.html). [ADR 0046](../decisions/rendering.md#adr-0046) defines text ownership; [ADR 0095](../decisions/singleton-services.md#adr-0095) preserves static OS access over its retained service.
