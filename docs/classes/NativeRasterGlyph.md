# NativeRasterGlyph

Last updated: 2026-10-14

**Visibility:** internal · **Source:** [NativeFontPrecision.Raster.cs](../../src/Servers/Text/NativeFontPrecision.Raster.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and invariants

Owned RGBA byte snapshot with dimensions, bitmap bearings and an intrinsic-color flag, returned on a glyph cache miss. FreeType BGRA premultiplication is converted to the engine straight-alpha contract. The font owner copies the snapshot into an ordinary ImageTexture.

## Integration and verification

Used through [Font](Font.md), [FontFile](FontFile.md) and canvas/Label consumers. These helpers do not add a public compatibility API. The [text component](../components/text.md#verification-boundaries) distinguishes source, managed, native pixel, allocation and platform evidence. [ADR 0046](../decisions/rendering.md#adr-0046) owns the backend and integration boundary.

LCD distinguishes normalized RGB component coverage from intrinsic color. Horizontal raw width or vertical raw row count is divided by three before publishing logical dimensions; BGR layouts reverse red/blue and encoded alpha is opaque. Mono/Gray/BGRA retain their existing decoding. An independent C FreeType oracle checks 60 exact raster cases.
