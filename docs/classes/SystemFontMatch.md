# SystemFontMatch

Last updated: 2026-10-07

**Visibility:** internal · **Source:** [NativeSystemFonts.cs](../../src/Servers/Text/NativeSystemFonts.cs) · **Component:** [System fonts](../components/system-fonts.md)

## Responsibilities and invariants

Internal immutable typed source selection: file path, native collection/named face index and family. It is discovery metadata, never a stored game-facing resource or native handle.

## Verification and limits

SystemFontTests exercises host and isolated fixture/empty catalogs, native sources, lifecycle and current prepared consumers. Current installed discovery is Linux-only; platform provider, native allocator and foreign execution gates remain explicit under [ADR 0046](../decisions/rendering.md#adr-0046).
