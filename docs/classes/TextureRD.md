# TextureRD

Last updated: 2026-10-25

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.TextureRD`. **Source:** [TextureRD.cs](../../src/Scene/Resources/TextureRD.cs). **Component:** [Local compute](../components/local-compute.md#device-images-in-canvas).

**Inherits:** [Texture](Texture.md).

## Description

A borrowed ordinary sampled image of the retained GPU rendering device. Binding TextureRDRID validates the owner thread, ended compute list, live global-device identity and sampled image role before publication. A successful assignment emits Changed. Local-device images reject. Signed/unsigned integer image roles execute with nearest native sampling and matching typed descriptors; scalar-aware light receivers, normal/specular channels, light-mask atlases and alpha clipping execute on the GPU.

The binding borrows device pixels; disposing the resource or freeing a server wrapper never frees the source image. Freeing the device image empties live metadata; renderer shutdown expires the device. Canvas drawing samples native storage directly with no CPU frame bridge. Sampled component maps captured by the device image follow ordinary/layered shader, lighting and particle consumers; snapshots retain raw image components. Snapshots require CPURead or CanCopyFrom, synchronize device work and copy every mip. Representable formats retain encoded data; other sampled color formats convert on GPU to floating RGBA. Resource archives reconstruct an unbound object; duplication borrows the same image. Mutating bindings and disposing live bindings obey device owner/submission guards.

## Example

The snippet runs inside an active GPU session with an already-created sampled global-device image; snapshot access additionally requires a readable role.

```csharp
using var texture = new TextureRD { TextureRDRID = sampledDeviceImage };
using var snapshot = texture.GetImage(); // Explicit caller-owned mip snapshot.
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`public TextureRD()`](#member-09d36af27f0b) | constructor | Creates an unbound device texture with zero logical dimensions. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)`](#member-da8491ee010a) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-fa2506a70702) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-d2d7ac196d41) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public override System.Int32 GetHeight()`](#member-2ec164ee792b) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public override Electron2D.Image GetImage()`](#member-ba9c0c76dd78) | method | Returns an independent encoded image snapshot, or null for an unbound/unreadable source. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-bc4d8f7b1b48) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public override System.Int32 GetWidth()`](#member-597ac3a433a5) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`protected override System.Void ValidateDisposal()`](#member-0e1b9bd396e4) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public System.Boolean HasAlpha { get;  }`](#member-42f37d9cf423) | property | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public System.Boolean HasMIPMaps { get;  }`](#member-d0e14e79288f) | property | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public System.Int32 MIPMapCount { get;  }`](#member-8ba65286d921) | property | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public Electron2D.Image.Format PixelFormat { get;  }`](#member-121532aebfe6) | property | Overrides the inherited resource metadata/readback or managed lifetime contract. |
| [`public Electron2D.RID TextureRDRID { get; set; }`](#member-ed6b6f7f74f4) | property | Gets or sets the borrowed image identity of the current rendering device. |

## Member reference

<a id="member-09d36af27f0b"></a>

### .ctor

```csharp
public TextureRD()
```

Creates an unbound device texture with zero logical dimensions.


<a id="member-da8491ee010a"></a>

### CopyCustomStateTo

```csharp
protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)
```

Uses the inherited [Texture](Texture.md) contract with the device image behavior described above.

<a id="member-fa2506a70702"></a>

### CreateDuplicateInstance

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

Uses the inherited [Texture](Texture.md) contract with the device image behavior described above.

<a id="member-d2d7ac196d41"></a>

### Dispose

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

Uses the inherited [Texture](Texture.md) contract with the device image behavior described above.

<a id="member-2ec164ee792b"></a>

### GetHeight

```csharp
public override System.Int32 GetHeight()
```

Uses the inherited [Texture](Texture.md) contract with the device image behavior described above.

<a id="member-ba9c0c76dd78"></a>

### GetImage

```csharp
public override Electron2D.Image GetImage()
```

Returns an independent encoded image snapshot, or null for an unbound/unreadable source.

This cold readback synchronizes pending device work. Formats without a direct Image representation convert on the GPU into floating-point RGBA; ordinary canvas drawing performs no CPU pixel copy.


<a id="member-bc4d8f7b1b48"></a>

### GetPropertyDescriptors

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Uses the inherited [Texture](Texture.md) contract with the device image behavior described above.

<a id="member-597ac3a433a5"></a>

### GetWidth

```csharp
public override System.Int32 GetWidth()
```

Uses the inherited [Texture](Texture.md) contract with the device image behavior described above.

<a id="member-0e1b9bd396e4"></a>

### ValidateDisposal

```csharp
protected override System.Void ValidateDisposal()
```

Uses the inherited [Texture](Texture.md) contract with the device image behavior described above.

<a id="member-42f37d9cf423"></a>

### HasAlpha

```csharp
public System.Boolean HasAlpha { get;  }
```

Uses the inherited [Texture](Texture.md) contract with the device image behavior described above.

<a id="member-d0e14e79288f"></a>

### HasMIPMaps

```csharp
public System.Boolean HasMIPMaps { get;  }
```

Uses the inherited [Texture](Texture.md) contract with the device image behavior described above.

<a id="member-8ba65286d921"></a>

### MIPMapCount

```csharp
public System.Int32 MIPMapCount { get;  }
```

Uses the inherited [Texture](Texture.md) contract with the device image behavior described above.

<a id="member-121532aebfe6"></a>

### PixelFormat

```csharp
public Electron2D.Image.Format PixelFormat { get;  }
```

Uses the inherited [Texture](Texture.md) contract with the device image behavior described above.

<a id="member-ed6b6f7f74f4"></a>

### TextureRDRID

```csharp
public Electron2D.RID TextureRDRID { get; set; }
```

Gets or sets the borrowed image identity of the current rendering device.

An empty identity until bound; local-device and incompatible image identities reject.

Validation precedes publication; a committed assignment emits Changed without owning the image.



Integer device image consumers now validate float/signed/unsigned sampled scalar types and array roles, migrate reload bindings and use nearest native integer sampling. Cold snapshots expose raw-valued float32 RGBA; byte device readback preserves all integer bits. See [the integer canvas checkpoint](../components/local-compute.md#integer-canvas-sampling-checkpoint) for executable evidence and the connected scalar-aware lighting and alpha-mask behavior.

## Renderer allocation interop

RenderingServer.TextureGetRDTexture exposes actual uploaded ordinary/array storage and completed viewport images to the retained device. Compatible updates keep identity; source allocation replacement/disposal or viewport target retirement expires it. Device writes affect GPU pixels while authored CPU snapshots remain unchanged. TextureRD/TextureArrayRD can sample an exported RID directly. Wrapper disposal does not own the source. See [ordering, lifetime and executed checks](../components/local-compute.md#renderer-texture-export-checkpoint).
