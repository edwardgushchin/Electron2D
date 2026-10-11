# RDTextureView

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDTextureView`. **Source:** [RDTextureView.cs](../../src/Servers/Rendering/Device/RDTextureView.cs). **Component:** [Local compute](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A caller-owned initial view description. DataFormat.Max retains the allocation format. The executing local profile accepts matching formats and identity/channel-preserving mappings. Nonidentity swizzles and format reinterpretation require the native-view adapter and throw during creation. This description owns no native identity and does not extend texture lifetime.

## Example

Device examples require a live caller-owned RenderingDevice; view examples also require an allocation format. See the [complete executable consumer](../../tests/Electron2D.Tests/RenderingDeviceTextureTests.cs).

```csharp
using var view = new RDTextureView();
var texture = device.TextureCreate(format, view);
```

## API

| Declaration | Contract |
| --- | --- |
| `public RDTextureView()` | Creates the default TextureView configuration. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited typed metadata projection. |
| `public Electron2D.RenderingDevice.DataFormat FormatOverride { get; set; }` | Gets or sets the view pixel format, or Max to retain the allocation format. |
| `public Electron2D.RenderingDevice.TextureSwizzle SwizzleA { get; set; }` | Gets or sets the source of the sampled alpha component. |
| `public Electron2D.RenderingDevice.TextureSwizzle SwizzleB { get; set; }` | Gets or sets the source of the sampled blue component. |
| `public Electron2D.RenderingDevice.TextureSwizzle SwizzleG { get; set; }` | Gets or sets the source of the sampled green component. |
| `public Electron2D.RenderingDevice.TextureSwizzle SwizzleR { get; set; }` | Gets or sets the source of the sampled red component. |

## Member descriptions

<a id="member-f133c1e76074"></a>
### RDTextureView()

```csharp
public RDTextureView()
```

Creates the default TextureView configuration.

<a id="member-63f6f666328b"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Extends inherited ElectronObject metadata with typed, validated configuration properties. It owns no native resource.

<a id="member-c64cc3b63248"></a>
### FormatOverride

```csharp
public Electron2D.RenderingDevice.DataFormat FormatOverride { get; set; }
```

Gets or sets the view pixel format, or Max to retain the allocation format.

Initially RenderingDevice.DataFormat.Max; copied when the device creates its resource.

<a id="member-f43ee496d688"></a>
### SwizzleA

```csharp
public Electron2D.RenderingDevice.TextureSwizzle SwizzleA { get; set; }
```

Gets or sets the source of the sampled alpha component.

Initially RenderingDevice.TextureSwizzle.A; copied when the device creates its resource.

<a id="member-319c735f12dc"></a>
### SwizzleB

```csharp
public Electron2D.RenderingDevice.TextureSwizzle SwizzleB { get; set; }
```

Gets or sets the source of the sampled blue component.

Initially RenderingDevice.TextureSwizzle.B; copied when the device creates its resource.

<a id="member-625038b6b09b"></a>
### SwizzleG

```csharp
public Electron2D.RenderingDevice.TextureSwizzle SwizzleG { get; set; }
```

Gets or sets the source of the sampled green component.

Initially RenderingDevice.TextureSwizzle.G; copied when the device creates its resource.

<a id="member-950ad99bf778"></a>
### SwizzleR

```csharp
public Electron2D.RenderingDevice.TextureSwizzle SwizzleR { get; set; }
```

Gets or sets the source of the sampled red component.

Initially RenderingDevice.TextureSwizzle.R; copied when the device creates its resource.

## Lifetime and verification

Configurations are independent caller-owned ElectronObject values; disposing a description does not free an already-created RID. Device identities belong to the creating thread/device and are freed through FreeRID or device disposal. Borrowed sampler/image bindings expire when either identity is freed. Enum values own no native state.

Native GLSL/HLSL and image/filter/mip tests run through ELECTRON2D_TEST_DEVICE_TEXTURES on Linux/Vulkan. Other backends, native allocator measurements, implicit LOD-bias/anisotropy calibration and canvas/device sharing remain unverified. See [Local compute](../components/local-compute.md) and [ADR 0028](../decisions/rendering.md#adr-0028).
