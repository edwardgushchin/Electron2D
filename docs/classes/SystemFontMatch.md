# SystemFontMatch

Last updated: 2026-11-07

**Visibility:** internal · **Source:** [NativeSystemFonts.cs](../../src/Servers/Text/NativeSystemFonts.cs) · **Component:** [System fonts](../components/system-fonts.md)

## Responsibilities and invariants

Internal immutable typed source selection: file path, native collection/named face index and family. It is discovery metadata, never a stored game-facing resource or native handle.

## Verification and limits

SystemFontTests exercises Linux host and isolated fixture/empty catalogs. WindowsSystemFontChecks independently verifies DirectWrite local paths, nonzero collection indices, actual text sources, resources and prepared rendering. Exact profile and native allocator/foreign-platform limits remain in [the component](../components/system-fonts.md#windows-native-verification) under ADR 0046.
