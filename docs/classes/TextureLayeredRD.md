# TextureLayeredRD

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.TextureLayeredRD`. **Source:** [TextureLayeredRD.cs](../../src/Scene/Resources/TextureLayeredRD.cs). **Component:** [Local compute](../components/local-compute.md#device-images-in-canvas).

**Inherits:** [TextureLayered](TextureLayered.md).

## Description

Abstract borrowed device image-array resource. Independent array storage requires at least two layers and a sampled float, normalized or integer image of the active retained rendering device. Arrays stay separate from ordinary rectangle drawing and bind through typed shader sampler defaults/overrides.

The binding borrows device pixels; disposing the resource or freeing a server wrapper never frees the source image. Freeing the device image empties live metadata; renderer shutdown expires the device. Canvas drawing samples native storage directly with no CPU frame bridge. Snapshots require CPURead or CanCopyFrom, synchronize device work and copy every mip. Representable formats retain encoded data; other sampled color formats convert on GPU to floating RGBA. Resource archives reconstruct an unbound object; duplication borrows the same image. Mutating bindings and disposing live bindings obey device owner/submission guards.

## Example

The snippet runs inside an active GPU session with an already-created sampled global-device image; snapshot access additionally requires a readable role.

```csharp
using TextureLayeredRD layers = new TextureArrayRD { TextureRDRID = sampledArrayImage };
using var layer = layers.GetLayerData(1); // Explicit caller-owned mip snapshot.
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`protected TextureLayeredRD()`](#member-27bc613bbda0) | constructor | Initializes an unbound independent image-array view. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)`](#member-0c2a5c0592ab) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-a801b790d863) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public override Electron2D.Image.Format GetFormat()`](#member-bd902fc09b9a) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public override System.Int32 GetHeight()`](#member-086bba6d6b1a) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public override Electron2D.Image GetLayerData(System.Int32 layerIndex)`](#member-a998a626374b) | method | Copies every mip of one readable array layer into a caller-owned image. |
| [`public override System.Int32 GetLayers()`](#member-c3c918f68c0f) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-1588e383f755) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public override System.Int32 GetWidth()`](#member-19128ecc05f4) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public override System.Boolean HasMipmaps()`](#member-571ece64ebe2) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`protected override System.Void ValidateDisposal()`](#member-6aaf32b0d8b6) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public Electron2D.RID TextureRDRID { get; set; }`](#member-1b72f3ce81ce) | property | Gets or sets a sampled image-array identity of the active rendering device. |

## Member reference

<a id="member-27bc613bbda0"></a>

### .ctor

```csharp
protected TextureLayeredRD()
```

Initializes an unbound independent image-array view.


<a id="member-0c2a5c0592ab"></a>

### CopyCustomStateTo

```csharp
protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)
```

Uses the inherited [TextureLayered](TextureLayered.md) contract with the device image behavior described above.

<a id="member-a801b790d863"></a>

### Dispose

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

Uses the inherited [TextureLayered](TextureLayered.md) contract with the device image behavior described above.

<a id="member-bd902fc09b9a"></a>

### GetFormat

```csharp
public override Electron2D.Image.Format GetFormat()
```

Uses the inherited [TextureLayered](TextureLayered.md) contract with the device image behavior described above.

<a id="member-086bba6d6b1a"></a>

### GetHeight

```csharp
public override System.Int32 GetHeight()
```

Uses the inherited [TextureLayered](TextureLayered.md) contract with the device image behavior described above.

<a id="member-a998a626374b"></a>

### GetLayerData

```csharp
public override Electron2D.Image GetLayerData(System.Int32 layerIndex)
```

Copies every mip of one readable array layer into a caller-owned image.

- `layerIndex`: Zero-based layer; invalid layers reject even for unbound resources.

<a id="member-c3c918f68c0f"></a>

### GetLayers

```csharp
public override System.Int32 GetLayers()
```

Uses the inherited [TextureLayered](TextureLayered.md) contract with the device image behavior described above.

<a id="member-1588e383f755"></a>

### GetPropertyDescriptors

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Uses the inherited [TextureLayered](TextureLayered.md) contract with the device image behavior described above.

<a id="member-19128ecc05f4"></a>

### GetWidth

```csharp
public override System.Int32 GetWidth()
```

Uses the inherited [TextureLayered](TextureLayered.md) contract with the device image behavior described above.

<a id="member-571ece64ebe2"></a>

### HasMipmaps

```csharp
public override System.Boolean HasMipmaps()
```

Uses the inherited [TextureLayered](TextureLayered.md) contract with the device image behavior described above.

<a id="member-6aaf32b0d8b6"></a>

### ValidateDisposal

```csharp
protected override System.Void ValidateDisposal()
```

Uses the inherited [TextureLayered](TextureLayered.md) contract with the device image behavior described above.

<a id="member-1b72f3ce81ce"></a>

### TextureRDRID

```csharp
public Electron2D.RID TextureRDRID { get; set; }
```

Gets or sets a sampled image-array identity of the active rendering device.

An empty identity initially. Bound arrays require at least two image layers.

Device identities remain borrowed and transient; archives do not store this binding.



Integer device image consumers now validate float/signed/unsigned sampled scalar types and array roles, migrate reload bindings and use nearest native integer sampling. Cold snapshots expose raw-valued float32 RGBA; byte device readback preserves all integer bits. See [the integer canvas checkpoint](../components/local-compute.md#integer-canvas-sampling-checkpoint) for executable evidence and the connected scalar-aware lighting and alpha-mask behavior.

## Renderer allocation interop

RenderingServer.TextureGetRDTexture exposes actual uploaded ordinary/array storage and completed viewport images to the retained device. Compatible updates keep identity; source allocation replacement/disposal or viewport target retirement expires it. Device writes affect GPU pixels while authored CPU snapshots remain unchanged. TextureRD/TextureArrayRD can sample an exported RID directly. Wrapper disposal does not own the source. See [ordering, lifetime and executed checks](../components/local-compute.md#renderer-texture-export-checkpoint).
