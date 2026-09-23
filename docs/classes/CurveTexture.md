# CurveTexture

Last updated: 2026-09-23

- Declaration: `public sealed class CurveTexture : Texture`
- Source: [CurveTexture.cs](../../src/Scene/Resources/CurveTexture.cs)
- Inherits: [Texture](Texture.md), [Resource](Resource.md)
- Component: [Curves](../components/curves.md)

## Description

Generates a one-row floating-point texture from a borrowed scalar [Curve](Curve.md). RGB mode repeats each sample in three channels; Red mode stores just red. Texel i uses `Curve.SampleBaked(i / (float)Width)`, including zero and excluding one. This does not remap the curve's domain. Values remain signed and may exceed one; no color-space conversion or clamp occurs. Scalar sampling's derived IEEE nonfinite behavior is retained, without a guarantee of meaningful GPU colors for such values.

CPU construction, baking and GetImage need no window. The resource owns copied pixel snapshots and borrows its Curve. Renderers own native allocations. Disposing the texture disconnects its source; it never disposes the curve. An active source change rebakes synchronously, then emits Changed outside the texture gate. Compatible rebakes retain native allocation identity; width or format changes replace it. Allocation/upload remains lazy until rendering.

Operations are serialized. Each row samples one coherent curve state. Events execute on the editing thread; subscriber failure propagates after publication. Multi-call edits, cross-resource snapshots, copy and disposal require caller coordination. Failed validation/sampling preserves the preceding texture settings and pixels. A borrowed curve must stay alive while assigned; disposing it does not erase the last published pixels, but a later rebake fails until it is removed/replaced.

## Example

```csharp
using var curve = new Curve();
curve.AddPoint(Vector2.Zero, rightMode: Curve.TangentMode.Linear);
curve.AddPoint(Vector2.One, leftMode: Curve.TangentMode.Linear);
using var texture = new CurveTexture { Curve = curve, Width = 32 };
using var pixels = texture.GetImage()!;
// pixels.GetPixel(31, 0).R is 31 / 32f.
```

The analytic sampling contract is exercised in CurveTextureTests. A ShaderMaterial texture parameter can borrow this texture through the ordinary Texture binding API.

## API summary

| Declaration | Contract |
| --- | --- |
| `CurveTexture()` | Width 256, RGB, null curve, no image. |
| `int Width { get; set; }` | Horizontal sample count, 32..4096. |
| `Curve? Curve { get; set; }` | Borrowed scalar source; initially null. |
| `TextureModeEnum TextureMode { get; set; }` | RGB (0) or Red (1). |
| `override int GetWidth()` | Current Width. |
| `override int GetHeight()` | Always one. |
| `override Image? GetImage()` | Independent original float image, or null before initialization. |

Inherited GetSize returns (Width, 1). PixelFormat is L8 before initialization and Rgbf/Rf afterward; HasAlpha and HasMipmaps are false, MipmapCount is zero, and IsPixelOpaque returns true. Drawing and shader bindings follow [Texture](Texture.md). ResourceLocalToScene defaults to false.

## Property descriptions

### Width

Default 256. Invalid values throw ArgumentOutOfRangeException before mutation. A changed value synchronously rebuilds, even without a curve (zero pixels), and emits once. Equal assignments are silent. The base query is GetWidth(), so the mutable property does not hide a read-only base property. The valid runtime range is 32..4096 even though the upstream editor hint starts at one.

### Curve

Null initially. Identity reassignment is silent. A new live curve is subscribed before sampling to avoid losing a racing edit; the old source disconnects after successful replacement. Null fills stored channels with zero once initialized. Disposed inputs throw ObjectDisposedException. The Curve.BakeResolution setter alone does not emit Changed; the texture rebakes on an actual source notification or changed texture setting, preserving the source event contract.

### TextureMode

Defaults to TextureModeEnum.RGB. Undefined values throw ArgumentOutOfRangeException. Actual changes rebuild and emit once. RGB uses Image.Format.Rgbf (12 source bytes/texel); Red uses Image.Format.Rf (4), sampled as (R, 0, 0, 1). Both currently expand to RGBA32Float for GPU upload: **Red reduces CPU source storage, not current GPU memory**. The enum suffix avoids the C# collision with this property.

## Method descriptions

### GetWidth

Returns the current Width under the texture lock. Throws after disposal.

### GetHeight

Returns one, including before initialization. Throws after disposal.

### GetImage

Returns a caller-owned copied image in the original format, without mipmaps. Editing or disposing it never mutates the texture. Returns null until a changed setting or source notification generates pixels. Equal default assignments do not initialize it. Binding an uninitialized texture fails explicitly rather than supplying a fake image. Throws after disposal.

## Enumeration descriptions

See [CurveTexture.TextureModeEnum](CurveTexture.TextureModeEnum.md) for both stable values.

## Protected hooks and lifecycle

GetPropertyDescriptors supplies typed Width/Curve/TextureMode metadata plus inherited resource/pixel metadata. CreateDuplicateInstance creates this exact type. CopyCustomStateTo preserves width/mode/initialized state, follows the inherited shallow/deep graph policy for curves, and rebuilds independent snapshots from the resulting curves. CopyFromResource replaces target subscriptions and coalesces Changed; copying defaults resets the image to null. Resource identity/path rules remain inherited. PackedScene owns only its local duplicates. Dispose(bool) detaches subscriptions and drops pixels before base cleanup.

## Verification and limits

[CurveTextureTests](../../tests/Electron2D.Tests/CurveTextureTests.cs) and [RenderingCurveTextureTests](../../tests/Electron2D.Tests/RenderingCurveTextureTests.cs) verify managed state, events, copies, local ownership, failures, coherent concurrent publication and native Linux Wayland GPU canvas/HLSL/GLSL samples. Compatibility drivers tested on Wayland and dummy/software reject unsupported float precision explicitly; no byte fallback is used. Other platforms, owner visual acceptance, disk import/editor authoring and a fresh AOT/self-contained publish are unverified. Shared Texture/Resource coverage gaps remain on their own pages. See ADRs [0013](../decisions/resources.md#adr-0013) and [0028](../decisions/rendering.md#adr-0028).
