# NativeSystemFonts

Last updated: 2026-10-21

**Visibility:** internal · **Source:** [NativeSystemFonts.cs](../../src/Servers/Text/NativeSystemFonts.cs) · **Component:** [System fonts](../components/system-fonts.md)

## Responsibilities and invariants

Owns lazy optional Linux Fontconfig configuration, serialized catalog queries, copied sorted family enumeration and bounded cold family/text/locale/style request results. Every transient native allocation is released with finally; unsupported profiles do not import Fontconfig in static mobile/browser builds.

## Verification and limits

SystemFontTests exercises host and isolated fixture/empty catalogs, native sources, lifecycle and current prepared consumers. Current installed discovery is Linux-only; platform provider, native allocator and foreign execution gates remain explicit under [ADR 0046](../decisions/rendering.md#adr-0046).

Type 1 PFB/PFA sources share native shaping and raster/MSDF consumers. The common file reader resolves optional PFM/AFM metrics and preserves their byte snapshot in variations and reopened native faces. Linux catalog discovery admits scalable Type 1 sources. See [source-pair behavior and verification](../components/text.md#type-1-outline-and-auxiliary-metric-sources).

## Host platform integration

IsAvailable serializes the existing one-time provider initialization and reports whether a usable configuration exists. OS.HasFeature(system_fonts) uses this internal observation. Profiles without the native provider report unavailable; this does not manufacture fonts or initialize a renderer.
