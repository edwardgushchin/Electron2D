# Image.ASTCFormat

Last updated: 2026-09-24

**Inherits:** `System.Enum`

**Inherited By:** none

- **Source:** [`src/Core/IO/Image.cs`](../../src/Core/IO/Image.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Image.ASTCFormat`

> Selects the pixel-block footprint requested from a future ASTC encoder.

## Description

ASTC always stores 16 bytes per block. A 4×4 footprint favors quality; an 8×8 footprint favors size. The enum is available for stable typed configuration, but encoding remains deferred.

## Examples

```csharp
Image.ASTCFormat footprint = Image.ASTCFormat.Format4X4;
```

## Enumeration values

| Member | Value | Description |
| --- | ---: | --- |
| [`Format4X4`](#format4x4) | 0 | 4×4 pixels per 16-byte block. |
| [`Format8X8`](#format8x8) | 1 | 8×8 pixels per 16-byte block. |

## Enumeration Descriptions

<a id="format4x4"></a>
### `Format4X4 = 0`

Requests 4×4 blocks, matching `Image.Format.Astc4X4` or `Image.Format.Astc4X4Hdr` output.

<a id="format8x8"></a>
### `Format8X8 = 1`

Requests 8×8 blocks, matching `Image.Format.Astc8X8` or `Image.Format.Astc8X8Hdr` output.

## Invariants and threading

The enum is immutable and thread-safe. A future encoder must reject undefined values; no current API consumes it.

## Dependencies, verification, and limitations

Both numeric identities are verified. No ASTC encoder or target capability decision exists yet; work begins only under [ADR 0039](../decisions/resources.md#adr-0039).
