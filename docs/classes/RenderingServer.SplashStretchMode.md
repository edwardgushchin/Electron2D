# RenderingServer.SplashStretchMode

Last updated: 2026-10-20

- Declaration: `public enum RenderingServer.SplashStretchMode`
- Source: [RenderingServer.Boot.cs](../../src/Servers/Rendering/RenderingServer.Boot.cs)
- Component: [Loading images](../components/boot-images.md)

Controls the destination of SetBootImageWithStretch in the main window's native client pixels. All modes center the image. Overflow is cropped at the client boundary; the supplied background fills uncovered pixels. Only Disabled rounds the centered origin down to whole pixels.

| Value | Number | Behavior |
| --- | ---: | --- |
| Disabled | 0 | Original image dimensions, centered at a whole-pixel origin. |
| Keep | 1 | Fit client height in landscape; fit width in portrait and square clients. Preserve image aspect; this can crop. |
| KeepWidth | 2 | Fit client width and preserve image aspect. |
| KeepHeight | 3 | Fit client height and preserve image aspect. |
| Cover | 4 | Fill both client axes while preserving aspect and cropping excess. |
| Ignore | 5 | Fill both client axes independently. |

For example, a 64×32 image in a 256×192 client uses a centered 384×192 destination with Keep or Cover, while KeepWidth uses 256×128 with 32-pixel top and bottom margins. A square client uses width for Keep. Values outside zero through five reject before presentation.

The active native session owns presentation; this enum adds no scene setting or independent lifetime. See [RenderingServer](RenderingServer.md#setbootimagewithstretch) for call ordering, copied image ownership, errors and verification limits.

## Value descriptions

### Disabled

Keeps source dimensions. The centered origin is rounded down, including a negative half-pixel origin for oversized images.

### Keep

Landscape clients select their height; portrait and square clients select their width. Image aspect is preserved. This rule can crop a wide image in a landscape client.

### KeepWidth

The destination width equals the client width. Height follows source aspect and can extend beyond the client.

### KeepHeight

The destination height equals the client height. Width follows source aspect and can extend beyond the client.

### Cover

Selects the larger aspect-preserving scale. Both client dimensions are covered and excess is cropped symmetrically.

### Ignore

Both destination dimensions equal the client. The image's aspect can change.
