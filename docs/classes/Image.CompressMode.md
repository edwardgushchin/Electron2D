# Image.CompressMode

Last updated: 2026-10-14

**Inherits:** `System.Enum`

**Inherited By:** none

- **Source:** [`src/Core/IO/Image.cs`](../../src/Core/IO/Image.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Image.CompressMode`

> Identifies the block-compression family requested by Image.Compress.

## Description

Image.Compress and CompressFromChannels accept S3tc and Bptc. Etc, Etc2 and Astc require separate codecs and fail before publication. Max is a sentinel, not a valid request.

## Examples

```csharp
Image.CompressMode preferredFamily = Image.CompressMode.Astc;
```

## Enumeration values

| Member | Value | Description |
| --- | ---: | --- |
| [`S3tc`](#s3tc) | 0 | S3TC/BC1-BC5 family. |
| [`Etc`](#etc) | 1 | ETC1 family. |
| [`Etc2`](#etc2) | 2 | ETC2/EAC family. |
| [`Bptc`](#bptc) | 3 | BPTC/BC6-BC7 family. |
| [`Astc`](#astc) | 4 | ASTC family. |
| [`Max`](#max) | 5 | Sentinel; not a valid mode. |

## Enumeration Descriptions

<a id="s3tc"></a>
### `S3tc = 0`

Selects the S3TC/BC family commonly represented by DXT1, DXT3, DXT5, BC4, and BC5 storage.

<a id="etc"></a>
### `Etc = 1`

Selects ETC1 RGB compression.

<a id="etc2"></a>
### `Etc2 = 2`

Selects ETC2/EAC color, alpha, and one/two-channel compression.

<a id="bptc"></a>
### `Bptc = 3`

Selects BPTC/BC6H or BC7 compression for HDR RGB or normalized RGBA data.

<a id="astc"></a>
### `Astc = 4`

Selects ASTC compression; [`Image.ASTCFormat`](Image.ASTCFormat.md) chooses the supported block footprint.

<a id="max"></a>
### `Max = 5`

Counts valid families and is not a usable compression mode.

## Invariants and threading

The enum is immutable and thread-safe. Image compression rejects undefined values and Max; S3tc/Bptc execute, and unavailable families fail before publication.

## Dependencies, verification, and limitations

Numeric identities are verified by the executable harness. No encoder, capability selection, renderer upload, or fallback behavior is implemented. Those start only at the exact trigger in [ADR 0039](../decisions/resources.md#adr-0039).
