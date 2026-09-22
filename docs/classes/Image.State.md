# Image.State

Last updated: 2026-09-22

- Declaration: `internal readonly record struct Image.State`
- Source: [Image.cs](../../src/Core/IO/Image.cs)
- Component: [images](../components/images.md)
- Visibility: internal; unavailable to engine consumers.

## Description

A managed snapshot record used by Image processing and the texture bridge. The record copies metadata by value but contains a byte-array reference. Image.CopyPixels clones the array before handing it to TexturePixels; the texture pipeline treats published snapshots as immutable. No native memory or public mutable array view is exposed.

## Member summary

| Declaration | Contract |
| --- | --- |
| `State(int Width, int Height, Image.Format Format, bool HasMipmaps, byte[] Data)` | [Construction and values](#construction-and-values) |

## Member descriptions

### Construction and values

`State(int Width, int Height, Image.Format Format, bool HasMipmaps, byte[] Data)`

Width/Height, Format, HasMipmaps and Data are the positional record properties. Image validates payload size and mip layout before publication. A default Image may be empty; texture construction rejects empty snapshots. Internal transformation functions return complete replacement states; ordinary Image single-pixel mutation uses its own lock rather than this shared texture state.

## Verification and limits

Image tests verify copied public data, format conversion and mip dimensions; RenderingTextureTests verifies source/output independence, Update identities and HDR retention.
