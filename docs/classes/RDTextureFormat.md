# RDTextureFormat

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDTextureFormat`. **Source:** [RDTextureFormat.cs](../../src/Servers/Rendering/Device/RDTextureFormat.cs). **Component:** [Local compute](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A caller-owned allocation description copied during TextureCreate. Width and Height are texels; ArrayLayers counts independently sampled 2D images; Mipmaps includes the base level. Values are validated on assignment and again as a complete allocation. One ordinary image has one layer. Data is one tightly packed encoded mip chain per layer, or omitted for zero encoded bytes. Native alignment remains internal. The format vocabulary is queried against the current device; unsupported allocations throw before returning an identity. Alternate shareable formats require a native view adapter and are rejected explicitly. ColorAttachment allocations now feed executable device color passes and retained canvas consumers. Depth, multisample and transient/resolve lifetimes remain coverage dependencies.

## Example

Device examples require a live caller-owned RenderingDevice; view examples also require an allocation format. See the [complete executable consumer](../../tests/Electron2D.Tests/RenderingDeviceTextureTests.cs).

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
| `public System.Void AddShareableFormat(Electron2D.RenderingDevice.DataFormat format)` | Adds an explicitly allowed alternate view format. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited typed metadata projection. |
| `public System.Void RemoveShareableFormat(Electron2D.RenderingDevice.DataFormat format)` | Removes an allowed alternate view format. |
| `public System.UInt32 ArrayLayers { get; set; }` | Gets or sets the number of independent image layers. |
| `public Electron2D.RenderingDevice.DataFormat Format { get; set; }` | Gets or sets the encoded pixel or compressed-block format. |
| `public System.UInt32 Height { get; set; }` | Gets or sets the base mip height in texels. |
| `public System.UInt32 Mipmaps { get; set; }` | Gets or sets the mip count, including the base level. |
| `public Electron2D.RenderingDevice.TextureType TextureType { get; set; }` | Gets or sets ordinary-image or independently sampled array storage. |
| `public Electron2D.RenderingDevice.TextureUsageBits UsageBits { get; set; }` | Gets or sets required allocation, sampling and transfer roles. |
| `public System.UInt32 Width { get; set; }` | Gets or sets the base mip width in texels. |

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

Adds an explicitly allowed alternate view format.

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

Removes an allowed alternate view format.

`format`: The concrete format to remove.

<a id="member-420733d3e765"></a>
### ArrayLayers

```csharp
public System.UInt32 ArrayLayers { get; set; }
```

Gets or sets the number of independent image layers.

Initially 1; copied when the device creates its resource.

<a id="member-5cbfd47c39ec"></a>
### Format

```csharp
public Electron2D.RenderingDevice.DataFormat Format { get; set; }
```

Gets or sets the encoded pixel or compressed-block format.

Initially RenderingDevice.DataFormat.R8UNORM; copied when the device creates its resource.

<a id="member-0a014c720327"></a>
### Height

```csharp
public System.UInt32 Height { get; set; }
```

Gets or sets the base mip height in texels.

Initially 1; copied when the device creates its resource.

<a id="member-8c41e7c2204d"></a>
### Mipmaps

```csharp
public System.UInt32 Mipmaps { get; set; }
```

Gets or sets the mip count, including the base level.

Initially 1; copied when the device creates its resource.

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

<a id="member-4a857a4857e4"></a>
### Width

```csharp
public System.UInt32 Width { get; set; }
```

Gets or sets the base mip width in texels.

Initially 1; copied when the device creates its resource.

## Lifetime and verification

Configurations are independent caller-owned ElectronObject values; disposing a description does not free an already-created RID. Device identities belong to the creating thread/device and are freed through FreeRID or device disposal. Borrowed sampler/image bindings expire when either identity is freed. Enum values own no native state.

Native GLSL/HLSL and image/filter/mip tests run through ELECTRON2D_TEST_DEVICE_TEXTURES on Linux/Vulkan. Other backends, native allocator measurements, implicit LOD-bias/anisotropy calibration and canvas/device sharing remain unverified. See [Local compute](../components/local-compute.md) and [ADR 0028](../decisions/rendering.md#adr-0028).

Depth/stencil formats accept only DepthStencilAttachment and optional Sampling usage on this profile. Empty data initializes every depth layer/mip to zero through native passes. Raw aspect transfers and depth-bound state retain exact adapters in coverage. See [depth/stencil behavior](../components/local-compute.md#depthstencil-graphics-and-sampling).
