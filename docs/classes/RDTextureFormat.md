# RDTextureFormat

Last updated: 2026-11-06

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDTextureFormat`. **Source:** [RDTextureFormat.cs](../../src/Servers/Rendering/Device/RDTextureFormat.cs). **Component:** [Local compute](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A caller-owned allocation description copied during `TextureCreate`. Width and height are in texels. The four unsigned counts accept 1 through `Int32.MaxValue`. The setters reject undefined format, type and usage values before changing the description. An ordinary image requires exactly one layer. A single-layer array retains the array role.

`TextureCreate` checks the full description against device limits, format/usage support and the mip-chain length. Select at least one usage bit before creating a texture. Supply one tightly packed encoded mip chain per layer, or omit data to initialize every layer and mip to zero. A nonempty shareable list includes the exact allocation format. Compatible alternate formats execute through [shared texture views](../components/shared-texture-views.md).

`Samples` defaults to `Samples1`. `IsResolveBuffer` defaults to false and marks a single-sample color or depth/stencil resolve destination. Multisample images require empty single-mip target or color-storage allocations, including arrays. Explicit per-sample shader access executes through the [multisample storage contract](../components/graphics-storage-images.md#multisample-storage-images). CPU readback requires resolving first. Discardable lifetime hints require last-use analysis.

InputAttachment supports single-mip color and depth images and arrays, including exact multisample targets. An input array or combining it with Sampling retains a private readonly single-layer GPU snapshot for the input pass; see [ordered framebuffer inputs](../components/local-compute.md#ordered-framebuffer-passes-and-inputs). [Depth framebuffer inputs](../components/depth-inputs.md) execute through the ABI2 aspect/view adapter. Input-only depth retains private initialization targets without gaining public render/copy roles.

## Example

Device examples require a live caller-owned RenderingDevice. See the [public consumer](../../tests/TextureFormat.Consumer/Program.cs).

```csharp
using var format = new RDTextureFormat
{
    Width = 64, Height = 64,
    Format = RenderingDevice.DataFormat.R8G8B8A8UNORM,
    UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CPURead
};
using var view = new RDTextureView();
var texture = device.TextureCreate(format, view);
```

## API

| Declaration | Contract |
| --- | --- |
| `public RDTextureFormat()` | Creates a single-layer, one-texel R8 normalized image description. |
| `public System.Void AddShareableFormat(Electron2D.RenderingDevice.DataFormat format)` | Adds a permitted native view format; a nonempty list must include the allocation format. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited typed metadata projection. |
| `public System.Void RemoveShareableFormat(Electron2D.RenderingDevice.DataFormat format)` | Removes a permitted native view format before allocation; existing textures retain their copied list. |
| `public System.UInt32 ArrayLayers { get; set; }` | Gets or sets the number of independent image layers. |
| `public Electron2D.RenderingDevice.DataFormat Format { get; set; }` | Gets or sets the encoded pixel or compressed-block format. |
| `public System.UInt32 Height { get; set; }` | Gets or sets the base mip height in texels. |
| `public System.UInt32 MIPMaps { get; set; }` | Gets or sets the mip count, including the base level. |
| `public Electron2D.RenderingDevice.TextureType TextureType { get; set; }` | Gets or sets ordinary-image or independently sampled array storage. |
| `public Electron2D.RenderingDevice.TextureUsageBits UsageBits { get; set; }` | Gets or sets required allocation, sampling and transfer roles. |
| `public System.UInt32 Width { get; set; }` | Gets or sets the base mip width in texels. |
| [`public System.Boolean IsResolveBuffer { get; set; }`](#member-17c8de36cbaa) | Marks a single-sample color image or array as a resolve destination. |
| [`public Electron2D.RenderingDevice.TextureSamples Samples { get; set; }`](#member-d37fe0333034) | Selects the exact allocation sample count. |

## Member descriptions

<a id="member-ecf3a73939c8"></a>
### RDTextureFormat()

```csharp
public RDTextureFormat()
```

Creates a single-layer, one-texel R8 normalized image description.

<a id="member-3b0ec46f4521"></a>
### AddShareableFormat(Electron2D.RenderingDevice.DataFormat)

```csharp
public System.Void AddShareableFormat(Electron2D.RenderingDevice.DataFormat format)
```

Adds a permitted native view format; a nonempty list must include the allocation format.

`format`: A concrete format identifier.

<a id="member-791eb07378f7"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Extends inherited ElectronObject metadata with typed, validated configuration properties. It owns no native resource.

<a id="member-287500d8e022"></a>
### RemoveShareableFormat(Electron2D.RenderingDevice.DataFormat)

```csharp
public System.Void RemoveShareableFormat(Electron2D.RenderingDevice.DataFormat format)
```

Removes a permitted native view format before allocation; existing textures retain their copied list.

`format`: The concrete format to remove.

<a id="member-420733d3e765"></a>
### ArrayLayers

```csharp
public System.UInt32 ArrayLayers { get; set; }
```

Gets or sets the number of independent image layers.

Initially 1; copied when the device creates its resource.

An ordinary image requires one layer. Arrays use the selected device's layer limit.

<a id="member-5cbfd47c39ec"></a>
### Format

```csharp
public Electron2D.RenderingDevice.DataFormat Format { get; set; }
```

Gets or sets the encoded pixel or compressed-block format.

Initially RenderingDevice.DataFormat.R8UNORM; copied when the device creates its resource.

Allocation requires native support for the format, usage and sample count.

<a id="member-0a014c720327"></a>
### Height

```csharp
public System.UInt32 Height { get; set; }
```

Gets or sets the base mip height in texels.

Initially 1; copied when the device creates its resource.

<a id="member-8c41e7c2204d"></a>
### MIPMaps

```csharp
public System.UInt32 MIPMaps { get; set; }
```

Gets or sets the mip count, including the base level.

Initially 1; copied when the device creates its resource.

The full chain has `1 + floor(log2(max(Width, Height)))` levels. Input, resolve and multisample roles require a single mip.

<a id="member-23a6baee3262"></a>
### TextureType

```csharp
public Electron2D.RenderingDevice.TextureType TextureType { get; set; }
```

Gets or sets ordinary-image or independently sampled array storage.

Initially RenderingDevice.TextureType.Texture; copied when the device creates its resource.

<a id="member-1a8cedc909b4"></a>
### UsageBits

```csharp
public Electron2D.RenderingDevice.TextureUsageBits UsageBits { get; set; }
```

Gets or sets required allocation, sampling and transfer roles.

Initially 0; copied when the device creates its resource.

`TextureCreate` requires at least one usage bit. Transfer-only textures retain exactly the public roles specified here.

<a id="member-4a857a4857e4"></a>
### Width

```csharp
public System.UInt32 Width { get; set; }
```

Gets or sets the base mip width in texels.

Initially 1; copied when the device creates its resource.

## Lifetime and verification

Disposing a description leaves the texture RID live. `TextureGetFormat` returns a new description that retains the stored allocation format, shareable formats and the selected view's dimensions, layers and mip count. The returned description stays usable after the RID is freed. Freeing the allocation owner retires its dependent views.

See [Texture description verification](../components/local-compute.md#texture-description-verification) and [ADR 0028](../decisions/rendering.md#adr-0028).

Depth/stencil formats accept queried attachment/input/resolve roles and independent Sampling and transfer permissions. [Stencil-only S8UINT](../components/stencil-textures.md) uses one byte per texel and unsigned sampling. Empty data initializes every depth layer/mip to zero through native passes. Single-sample native aspect transfers execute through the [depth transfer adapter](../components/depth-texture-transfers.md); copied depth-bound state uses the [native depth/stencil feature](../components/depth-stencil-state.md). See [depth/stencil behavior](../components/local-compute.md#depthstencil-graphics-and-sampling).

<a id="member-17c8de36cbaa"></a>
### IsResolveBuffer

```csharp
public System.Boolean IsResolveBuffer { get; set; }
```

Marks a single-sample color image or array as a resolve destination.

Initially false; copied and validated at allocation.


<a id="member-d37fe0333034"></a>
### Samples

```csharp
public Electron2D.RenderingDevice.TextureSamples Samples { get; set; }
```

Selects the exact allocation sample count. [Equal-count copies](../components/multisample-copies.md) preserve every native color/depth/stencil sample; CPU sample upload/readback remains separate.

Initially RenderingDevice.TextureSamples.Samples1; copied and validated at allocation.

Combining Sampling and Storage preserves a private readonly GPU snapshot for graphics-image consumers, while writes and public snapshots address the original allocation. The native sampling allocation carries no conflicting graphics-read flag; the [device coherence contract](../components/local-compute.md#readonly-graphics-image-coherence) describes copy and lifetime boundaries.

Ordinary signed/unsigned R32 Image descriptors now execute compute atomics, including fixed arrays. StorageAtomic validates allocation capability, while Image binding independently requires Storage. See [integer image atomics](../components/local-compute.md#integer-image-atomics) for ordering, lifetime and native verification limits.

Samples may be combined with the color/depth attachment and Sampling roles for explicit per-sample shader reads. Empty data, one mip, exact native count support and the native image/view adapter remain required. See [multisample shader reads](../components/local-compute.md#multisample-shader-reads).

Single-sample depth/stencil images accept copied encoded mip chains and native CanUpdate/CanCopyFrom/CanCopyTo/CPURead roles. Empty data still initializes every aspect to zero. D32 floats require finite unit-range data; readback canonicalizes unused bits. See [the encoded byte layout and transfer lifetime](../components/depth-texture-transfers.md).
