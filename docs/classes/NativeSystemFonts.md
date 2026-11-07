# NativeSystemFonts

Last updated: 2026-11-07

**Visibility:** internal · **Source:** [NativeSystemFonts.cs](../../src/Servers/Text/NativeSystemFonts.cs) · **Component:** [System fonts](../components/system-fonts.md)

## Responsibilities and invariants

Owns lazy optional Linux Fontconfig configuration or [Windows DirectWrite state](../../src/Servers/Text/NativeSystemFonts.Windows.cs), serialized catalog queries, copied sorted family aliases and bounded cold family/text/locale/style results. WindowsCatalog retains its factory/collection/fallback objects; its generated IAnalysisSource/AnalysisSource pins text/locale only during synchronous matching. Transient interfaces and pins retire on failure and success. Unsupported profiles do not import these providers in static mobile/browser builds.

## Verification and limits

SystemFontTests exercises Linux host and isolated fixture/empty catalogs. WindowsSystemFontChecks compares native SDK aliases, exact files/collection indices, fallback order and design advances, then exercises shared resource and render consumers. Source/package/AOT/root identities and target/native-heap limits belong to [the component](../components/system-fonts.md#windows-native-verification) under ADR 0046.

Type 1 PFB/PFA sources share native shaping and raster/MSDF consumers. The common file reader resolves optional PFM/AFM metrics and preserves their byte snapshot in variations and reopened native faces. Linux catalog discovery admits scalable Type 1 sources. See [source-pair behavior and verification](../components/text.md#type-1-outline-and-auxiliary-metric-sources).

## Host platform integration

IsAvailable serializes the existing one-time provider initialization and reports whether a usable configuration exists. OS.HasFeature(system_fonts) uses this internal observation. Profiles without the native provider report unavailable; this does not manufacture fonts or initialize a renderer.
