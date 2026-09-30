# AtlasTexture

Last updated: 2026-10-01

- Declaration: `public sealed class AtlasTexture : Texture`
- Source: [AtlasTexture.cs](../../src/Scene/Resources/AtlasTexture.cs)
- Inherits: [Texture](Texture.md), [Resource](Resource.md)
- Component: [Shader materials](../components/shader-materials.md)

## Description

A rectangular view of a borrowed texture with optional drawing margins. Views can be nested. Neither assigning, duplicating nor disposing an ordinary view transfers ownership of its source. Drawing delegates through the source's virtual region hook and ultimately retains the original texture; it does not allocate a cropped GPU image. A named material binding resolves to the full underlying texture and shares its native allocation with direct use of that source. CPU GetImage instead crops the immediate source image.

Atlas state and graph traversal use one reentrant graph gate, including source queries and virtual drawing. Each operation captures its view configuration before calling a source, so reentrant source changes affect subsequent operations. This prevents concurrent assignments from introducing cycles and provides consistent view state during traversal. It serializes independent atlas graphs; custom source callbacks must not wait for another thread to operate on an atlas while executing under this gate. Resource copying still follows the base caller-coordination contract. Changed is normally published after a setter releases the gate; reentrant changes inside a source callback can publish before the outer traversal releases it. Native drawing remains restricted to the recording node's owner thread.

## Example

```csharp
using var image = Image.CreateEmpty(64, 32, false, Image.Format.Rgba8);
image.Fill(Colors.White);
using var sheet = ImageTexture.CreateFromImage(image);
using var tile = new AtlasTexture
{
    Atlas = sheet,
    Region = new Rect2(16, 0, 16, 16),
    FilterClip = true,
};
using var sprite = new Sprite { Texture = tile };
```

This constructs resources and a detached node; scene attachment uses the normal Node/Window API.

## API summary

| Declaration | Contract |
| --- | --- |
| `AtlasTexture()` | No source, empty region/margin, FilterClip false, logical size `(1, 1)`. |
| `Texture? Atlas { get; set; }` | Borrowed source, initially null. |
| `Rect2 Region { get; set; }` | Stored source rectangle; calculations floor only its size. |
| `Rect2 Margin { get; set; }` | Drawing offset and total extra size. |
| `bool FilterClip { get; set; }` | Restrict sampling to the selected region's texel centers. |
| [`public override RID GetRID()`](#rendering-identity) | Borrows the current source RID, or empty without a source. |
| `override int GetWidth()` | Effective logical width including applicable margin. |
| `override int GetHeight()` | Effective logical height including applicable margin. |
| `override Vector2 GetSize()` | Dimensions from one serialized traversal. |
| `override bool HasAlpha { get; }` | Source alpha capability, false without a source. |
| `override Image.Format PixelFormat { get; }` | Image.Format.Max: the view has no own pixel format. |
| `override bool HasMipmaps { get; }` | False for the view. |
| `override int MipmapCount { get; }` | Zero for the view. |
| `override Image? GetImage()` | Independent cropped image without margin padding. |
| `override bool IsPixelOpaque(int x, int y)` | Translated alpha query within full source bounds. |
| `override void Draw(CanvasItem canvasItem, Vector2 position, Color? modulate = null, bool transpose = false)` | Draw at effective region size with margin offset. |
| `override void DrawRect(CanvasItem canvasItem, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)` | Stretch with proportional margins; tile is ignored. |
| `override void DrawRectRegion(CanvasItem canvasItem, Rect2 rect, Rect2 sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)` | Translate and clip source/destination geometry; FilterClip overrides clipUV. |

## Property descriptions

### Atlas

Null draws nothing. Identity reassignment is a no-op. A replacement validates the complete atlas chain and rejects direct or indirect cycles with ArgumentException, including competing concurrent assignments; disposed sources throw ObjectDisposedException. Validation occurs before detaching the old source.

Only nested AtlasTexture sources forward Changed. Replacing or disposing the view disconnects that subscription. ImageTexture and custom Texture changes do not forward: updated pixels are still observed by the renderer, but source size changes require an explicit redraw when recorded geometry depends on that size. A view's own changes request a Sprite redraw; arbitrary custom canvas commands require QueueRedraw to change their recorded region. Retained commands borrow their recorded underlying texture, so disposing a view alone does not erase previously recorded drawing.

### Region

Starts at zero. The public property retains the exact finite rectangle. Internal calculations floor Size independently on each axis and retain fractional Position. An effective zero size axis uses the immediate source's full logical dimension. Assigning an equal rectangle emits no change. Nonfinite rectangles throw ArgumentException without mutation. Negative sizes are retained; rectangle intersection does not normalize them into a positive crop.

### Margin

Position offsets the region within the logical view; Size is the total additional size, not the far-edge coordinate. Finite signed/fractional values are retained. Equal assignment is a no-op. Margin.Size is ignored on an axis whose rounded Region size is zero. Margins affect geometry, not the pixels returned by GetImage. Invalid nonfinite input throws ArgumentException before mutation.

### FilterClip

False by default. Every assignment emits Changed, even if unchanged. This controls the half-texel sampling boundary; it does not enable geometry clipping, which always clips the region. It overrides the DrawRectRegion caller flag. When nesting views, the innermost view controls the flag reaching the actual source texture.

### HasAlpha

Delegates to the immediate source's virtual HasAlpha, or false for null. The other metadata queries describe the view itself, as detailed below.

### PixelFormat

Returns Image.Format.Max, the unspecified-format sentinel, because the view has no own image storage. Query the source or returned Image for its actual format. This does not limit the formats the renderer samples.

### HasMipmaps

Always false for the view, even when its source has mipmaps. Drawing and material binding retain the source's full mip chain, subject to the selected backend/sampler capability.

### MipmapCount

Always zero for the view. Query Atlas.MipmapCount for the immediate source's metadata. All three metadata overrides validate view lifetime and throw after disposal.

## Method descriptions

### GetWidth

If the floored region width is zero, returns the source width or one without a source. Otherwise adds Margin.Size.X to the floored width, then truncates toward zero. An unrepresentable integer sum throws OverflowException. Signed invalid drawing dimensions are not silently normalized.

### GetHeight

Uses the GetWidth rules independently for the vertical axis.

### GetSize

Reads both dimensions under the graph gate. Source metadata can change independently when the source is a custom texture; custom resources retain their own synchronization obligations.

### GetImage

Calls the immediate source's GetImage and disposes that temporary image on success or failure. Null remains null. Crops its effective region after checked integer truncation of position and size. Image.GetRegion clips against actual image bounds and returns an empty image for an empty intersection; it removes mipmaps. Margin offset/size and logical source-size overrides do not resample the CPU image. Nested views crop the already cropped immediate source image. Integer overflow throws OverflowException; other source/image errors propagate. The caller owns the returned image.

### IsPixelOpaque

Adds Region.Position minus Margin.Position to the input coordinate and truncates toward zero. Bounds checking uses the full immediate source size, not the selected region or logical view size. Thus a visual margin can query a neighboring opaque source pixel. Outside source bounds returns false; null source returns true. Coordinate arithmetic is widened before checking bounds to avoid integer overflow. In-range queries delegate to the source's virtual method.

### Draw

Delegates the effective region at `position + Margin.Position`, using the region's size. Margin.Size adds no stretching or pixels. Transposition and modulation are passed through; null modulation means white. FilterClip supplies the sampling policy.

### DrawRect

Uses the logical GetWidth()/GetHeight() as the source rectangle, maps the region and margins proportionally into the destination, and delegates a source-region draw. Negative destination dimensions mirror the corresponding axis without moving its drawing origin. The tile parameter intentionally has no effect: an atlas view stretches once.

### DrawRectRegion

The supplied source is in logical view coordinates. If both size axes are zero, uses rounded Region.Size, then Atlas.GetSize() if both remain zero. A remaining zero axis produces no geometry. Adds Region.Position minus Margin.Position, intersects with the effective region, and proportionally adjusts the destination, including negative destination scales. An empty intersection produces no draw. It does not normalize signed source rectangles. Nested sources perform the same mapping at each layer; FilterClip replaces the caller's clipUV.

All drawing entry points validate lifetime, non-null CanvasItem, finite rectangles/colors and the canvas recording scope even when no atlas is assigned. Invalid arguments throw ArgumentException/ArgumentNullException, wrong scope/thread throws InvalidOperationException, disposal throws ObjectDisposedException. Derived geometry overflow is rejected before delegation. Zero-area geometry produces no pixels. Custom source implementations may impose additional requirements.

## Resource lifecycle and protected hooks

`protected override Resource CreateDuplicateInstance()` creates an empty exact-type view. `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` copies Region, Margin and FilterClip and passes Atlas through the duplication session when deep. Shallow copies borrow it; deep modes preserve the base internal/external resource policy and graph aliases. `CopyFromResource` coalesces notifications and preserves target identity; failure is not transactional. Observer exceptions propagate after setter state commits.

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` appends stored typed descriptors for the four view properties. Inherited ResourceLocalToScene defaults to false; packed scenes duplicate local view/source graphs through the existing resource session and own the resulting resources with the scene root. Ordinary duplicates remain independently caller-owned.

`protected override void Dispose(bool disposing)` disconnects the nested source, clears the borrowed edge and delegates base cleanup. It does not dispose that source or another view. Public use after disposal throws ObjectDisposedException. Traversal of a disposed source also fails when its data is needed.

## Verification and limits

[AtlasTextureTests](../../tests/Electron2D.Tests/AtlasTextureTests.cs) verifies defaults, fractional and zero-axis sizes, crop isolation, opacity, nested changes, cycle rejection, concurrent assignments, shallow/deep/internal/all duplication, local-scene ownership, callback failure, mapping/flip/transpose/clip flags and disposal. Native readback exercises retained commands, Sprite redraw, nested atlas updates, source pixel updates, HLSL/GLSL canvas drawing and full-storage named material bindings, including empty-source failure cleanup.

The native gate is Linux Wayland with SDL compatibility and GPU/Vulkan, plus the dummy/software compatibility path. This does not establish Windows, macOS, other platform, owner-visual or performance acceptance. Unsupported pixel formats and sampler capabilities retain the existing Texture/backend limits. General asset import, atlas packing and disk serialization are not added by this resource.

The pinned SDL software triangle input truncates source UVs to integer texels as well as destination vertices. Half-texel clipping boundaries can therefore shift the boundary between adjacent atlas colors: AtlasTextureTests explicitly checks the software/hardware difference and the shared edge sample. This is a fallback precision limit under ADR 0028; it does not promise identical nearest-sampling pixels across drivers. See [SDL software geometry input](https://github.com/libsdl-org/SDL/blob/release-3.4.16/src/render/software/SDL_render_sw.c).

## Polygon consumers

CanvasItem.DrawPolygon and DrawColoredPolygon capture the immediate Region as normalized UV mapping and borrow the ultimate source. They ignore Margin and FilterClip; a zero region size collapses UVs, and nested regions are not composed. Missing UVs sample the full source at zero. DrawPrimitive samples the full source without remapping. An empty atlas uses white sampling for these raw geometry commands. Metadata changes require redraw; existing recorded commands retain their source even if this view is replaced or disposed. [CanvasItem](CanvasItem.md#drawpolygon) documents validation, reentrant size callbacks and lifetime.

[NinePatchRect](../classes/NinePatchRect.md) now records one retained panel command with fixed borders, independent Stretch/Tile/TileFit axes and optional center. Live base/atlas dimensions resolve before splitting; ordinary atlas region drawing reuses that resolver. Signed margins drive Control intrinsic minimum size and inherited pointer filtering defaults to Ignore. All nine native axis combinations, center/flip/atlas/constant UV and 64 warmed resized frames are checked by [NinePatchRenderingTests](../../tests/Electron2D.Tests/NinePatchRenderingTests.cs), under [ADR 0079](../decisions/rendering.md#adr-0079). Dense CPU geometry limits, native allocator counts, other platforms and owner acceptance remain explicit.

## Rendering identity

`public override RID GetRID()`

Forwards the current source's RID under the atlas graph gate, recursively for nested views. An empty atlas returns empty. The identity describes the full underlying texture; region/margin/filter clipping apply when drawing the AtlasTexture object. RID overloads draw the resolved source rather than the view. Source assignment changes the forwarded identity; view disposal does not release that identity. This view and source disposal guards apply. [RenderingTextureRIDTests](../../tests/Electron2D.Tests/RenderingTextureRIDTests.cs) checks nested/empty/source-change/view-disposal behavior and backing image/format queries.
