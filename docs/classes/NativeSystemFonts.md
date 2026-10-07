# NativeSystemFonts

Last updated: 2026-10-07

**Visibility:** internal · **Source:** [NativeSystemFonts.cs](../../src/Servers/Text/NativeSystemFonts.cs) · **Component:** [System fonts](../components/system-fonts.md)

## Responsibilities and invariants

Owns lazy optional Linux Fontconfig configuration, serialized catalog queries, copied sorted family enumeration and bounded cold family/text/locale/style request results. Every transient native allocation is released with finally; unsupported profiles do not import Fontconfig in static mobile/browser builds.

## Verification and limits

SystemFontTests exercises host and isolated fixture/empty catalogs, native sources, lifecycle and current prepared consumers. Current installed discovery is Linux-only; platform provider, native allocator and foreign execution gates remain explicit under [ADR 0046](../decisions/rendering.md#adr-0046).
