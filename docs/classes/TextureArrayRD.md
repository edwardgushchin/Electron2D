# TextureArrayRD

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.TextureArrayRD`. **Source:** [TextureArrayRD.cs](../../src/Scene/Resources/TextureArrayRD.cs). **Component:** [Local compute](../components/local-compute.md#device-images-in-canvas).

**Inherits:** [TextureLayeredRD](TextureLayeredRD.md).

## Description

Concrete independent device image-array consumer. TextureRDRID and all metadata/readback behavior are inherited from TextureLayeredRD; the constructor creates an unbound array. Integer canvas sampling validates matching scalar descriptors; light/mask composition still needs its typed shader adapter.

The binding borrows device pixels; disposing the resource or freeing a server wrapper never frees the source image. Freeing the device image empties live metadata; renderer shutdown expires the device. Canvas drawing samples native storage directly with no CPU frame bridge. Snapshots require CPURead or CanCopyFrom, synchronize device work and copy every mip. Representable formats retain encoded data; other sampled color formats convert on GPU to floating RGBA. Resource archives reconstruct an unbound object; duplication borrows the same image. Mutating bindings and disposing live bindings obey device owner/submission guards.

## Example

The snippet runs inside an active GPU session with an already-created sampled global-device image; snapshot access additionally requires a readable role.

```csharp
using var layers = new TextureArrayRD { TextureRDRID = sampledArrayImage };
shader.SetDefaultLayeredTextureParameter("layers", layers);
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`public TextureArrayRD()`](#member-96f2c183edb1) | constructor | Creates an unbound device image array. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-df94a6acf48b) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |

## Member reference

<a id="member-96f2c183edb1"></a>

### .ctor

```csharp
public TextureArrayRD()
```

Creates an unbound device image array.


<a id="member-df94a6acf48b"></a>

### CreateDuplicateInstance

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

Uses the inherited [TextureLayeredRD](TextureLayeredRD.md) contract with the device image behavior described above.


Integer device image consumers now validate float/signed/unsigned sampled scalar types and array roles, migrate reload bindings and use nearest native integer sampling. Cold snapshots expose raw-valued float32 RGBA; byte device readback preserves all integer bits. See [the integer canvas checkpoint](../components/local-compute.md#integer-canvas-sampling-checkpoint) for executable evidence and the connected scalar-aware lighting and alpha-mask behavior.

## Renderer allocation interop

RenderingServer.TextureGetRDTexture exposes actual uploaded ordinary/array storage and completed viewport images to the retained device. Compatible updates keep identity; source allocation replacement/disposal or viewport target retirement expires it. Device writes affect GPU pixels while authored CPU snapshots remain unchanged. TextureRD/TextureArrayRD can sample an exported RID directly. Wrapper disposal does not own the source. See [ordering, lifetime and executed checks](../components/local-compute.md#renderer-texture-export-checkpoint).

[Native array storage](../components/graphics-storage-images.md#compute-and-array-consumers) executes typed compute/vertex/fragment loads, stores and signed/unsigned R32 atomics across independent layers, including one-layer arrays. Canvas sampling borrows the same GPU storage; private views retain one mip and follow original/renderer lifetime.
