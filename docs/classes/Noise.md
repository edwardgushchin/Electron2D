# Noise

Last updated: 2026-09-23

**Inherits:** [Resource](Resource.md) → [ElectronObject](ElectronObject.md)

**Inherited By:** [FastNoiseLite](FastNoiseLite.md) and application samplers.

- **Source:** [Noise.cs](../../src/Core/IO/Noise.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class Noise : Resource`

## Description

`Noise` supplies the shared 1D/2D sampling contract and creates independent grayscale images from a concrete sampler. A derived class supplies `GetNoise1D` and `GetNoise2D`. The vector query forwards to the two-coordinate query. Image generation uses integer pixel coordinates, so a concrete generator controls frequency, seed and any other sampling policy. The base has no mutable generator state or native backend. It inherits managed Resource lifetime and requires a derived type to implement its own duplication hooks if duplication is needed.

Only the two-dimensional branch of the reference class applies to Electron2D. The 3D-space image option, 3D noise samples and volume images are outside the accepted product boundary.

## Example

This partial snippet defines a concrete sampler; an application supplies its own noise function:

```csharp
using var noise = new RampNoise();
using Image pixels = noise.GetImage(64, 64);

sealed class RampNoise : Noise
{
    public override float GetNoise1D(float x) => x;
    public override float GetNoise2D(float x, float y) => x + y;
}
```

## Constructors

| Member | Contract |
| --- | --- |
| `protected Noise()` | Initializes the abstract managed resource base. |

## Methods

| Member | Contract |
| --- | --- |
| `public abstract float GetNoise1D(float x)` | Samples one coordinate. |
| `public abstract float GetNoise2D(float x, float y)` | Samples two coordinates. |
| `public virtual float GetNoise2DV(Vector2 position)` | Forwards the vector's components to `GetNoise2D`. |
| `public virtual Image GetImage(int width, int height, bool invert = false, bool normalize = true)` | Creates a copied L8 image. |
| `public virtual Image GetSeamlessImage(int width, int height, bool invert = false, float skirt = 0.1f, bool normalize = true)` | Blends an overlap from a larger sampled image into a copied seamless image. |

## Method descriptions

### `GetNoise1D`, `GetNoise2D`, `GetNoise2DV`

Concrete subclasses own the 1D and 2D sample values. The default vector overload calls `GetNoise2D(position.X, position.Y)` and rejects use after disposal. Implementations of the abstract methods should likewise reject disposed access if they hold derived state. The built-in FastNoiseLite generator supplies one implementation.

### `GetImage`

Requires positive width and height of at most 16,384 pixels each. Samples at every integer `(x, y)`. With `normalize = true`, the minimum sampled value maps to byte 0 and the maximum to byte 255; a constant sample set maps to 0. With `normalize = false`, the expected range `[-1, 1]` maps to `[0, 255]` using truncation after clamping. `invert` subtracts each byte from 255 after conversion. The returned L8 `Image` owns an independent copy of its pixels. Nonfinite samples throw `InvalidOperationException`; invalid dimensions throw `ArgumentOutOfRangeException`. An image allocation failure can still occur for large valid dimensions. Derived classes can override this method to supply a different image representation.

### `GetSeamlessImage`

Requests a larger image through `GetImage`, adding at least one pixel of overlap in each dimension. It rearranges quadrants and blends the overlapping strips across their joins. `skirt` is a nonnegative finite fraction; dimensions must also accommodate the enlarged source. The default path accepts L8 or RGBA8 images without mipmaps and disposes the temporary source after copying the result. Incompatible derived images throw. A derived generator may override the method with its own seamless algorithm. Exact pixel parity for unusual skirt values and derived image formats remains unaudited.

## Lifecycle, verification and limits

`NoiseTests` checks vector dispatch, L8 normalization, inversion, constant samples, seamless generation, invalid sizes, nonfinite values and disposed use. These are base-resource managed checks; FastNoiseLiteTests separately checks the built-in generator. [Noise coverage](../coverage/classes/Noise.md) records the partial pixel-parity audit and the excluded 3D methods. [ADR 0004](../decisions/product.md#adr-0004) owns the 2D boundary, and [ADR 0013](../decisions/resources.md#adr-0013) owns Resource copying.
