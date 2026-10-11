# RDSamplerState

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDSamplerState`. **Source:** [RDSamplerState.cs](../../src/Servers/Rendering/Device/RDSamplerState.cs). **Component:** [Local compute](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A caller-owned sampler description copied during SamplerCreate. Normalized U/V addressing supports repeat, mirrored repeat and clamp-to-edge. Nearest/linear texel and mip filters, LOD limits, bias and anisotropy feed the native sampler. Integer sampled images require nearest filtering without anisotropy. Creation validates finite settings and a nonnegative ordered LOD interval. Nonzero bias is rejected on Metal; border and mirror-clamp addressing need a shader adapter. Comparison, border-color and unnormalized-coordinate members remain absent until their executing dependency exists.

## Example

Device examples require a live caller-owned RenderingDevice; view examples also require an allocation format. See the [complete executable consumer](../../tests/Electron2D.Tests/RenderingDeviceTextureTests.cs).

```csharp
using var state = new RDSamplerState
{
    MinFilter = RenderingDevice.SamplerFilter.Linear,
    MagFilter = RenderingDevice.SamplerFilter.Linear,
    RepeatU = RenderingDevice.SamplerRepeatMode.Repeat
};
var sampler = device.SamplerCreate(state);
```

## API

| Declaration | Contract |
| --- | --- |
| `public RDSamplerState()` | Creates the default SamplerState configuration. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited typed metadata projection. |
| `public System.Single AnisotropyMax { get; set; }` | Gets or sets the anisotropic filtering limit from one through sixteen. |
| `public System.Single LODBias { get; set; }` | Gets or sets the bias added to the implicit mip selection. |
| `public Electron2D.RenderingDevice.SamplerFilter MagFilter { get; set; }` | Gets or sets the within-mip filter used for magnification. |
| `public System.Single MaxLOD { get; set; }` | Gets or sets the maximum mip level used for sampling. |
| `public Electron2D.RenderingDevice.SamplerFilter MinFilter { get; set; }` | Gets or sets the within-mip filter used for minification. |
| `public System.Single MinLOD { get; set; }` | Gets or sets the minimum mip level, with zero identifying the base level. |
| `public Electron2D.RenderingDevice.SamplerFilter MipFilter { get; set; }` | Gets or sets how adjacent mip levels are selected or blended. |
| `public Electron2D.RenderingDevice.SamplerRepeatMode RepeatU { get; set; }` | Gets or sets horizontal normalized-coordinate addressing. |
| `public Electron2D.RenderingDevice.SamplerRepeatMode RepeatV { get; set; }` | Gets or sets vertical normalized-coordinate addressing. |
| `public System.Boolean UseAnisotropy { get; set; }` | Gets or sets whether the sampler uses anisotropic filtering. |

## Member descriptions

<a id="member-a8bd01fce139"></a>
### RDSamplerState()

```csharp
public RDSamplerState()
```

Creates the default SamplerState configuration.

<a id="member-6b12dd7b8fb0"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Extends inherited ElectronObject metadata with typed, validated configuration properties. It owns no native resource.

<a id="member-6985464d4589"></a>
### AnisotropyMax

```csharp
public System.Single AnisotropyMax { get; set; }
```

Gets or sets the anisotropic filtering limit from one through sixteen.

Initially 1; copied when the device creates its resource.

<a id="member-2cd90dd88a1b"></a>
### LODBias

```csharp
public System.Single LODBias { get; set; }
```

Gets or sets the bias added to the implicit mip selection.

Initially 0; copied when the device creates its resource.

<a id="member-116efb58d0d5"></a>
### MagFilter

```csharp
public Electron2D.RenderingDevice.SamplerFilter MagFilter { get; set; }
```

Gets or sets the within-mip filter used for magnification.

Initially RenderingDevice.SamplerFilter.Nearest; copied when the device creates its resource.

<a id="member-2bfe2e10cd15"></a>
### MaxLOD

```csharp
public System.Single MaxLOD { get; set; }
```

Gets or sets the maximum mip level used for sampling.

Initially 1e20f; copied when the device creates its resource.

<a id="member-642eaeb354fb"></a>
### MinFilter

```csharp
public Electron2D.RenderingDevice.SamplerFilter MinFilter { get; set; }
```

Gets or sets the within-mip filter used for minification.

Initially RenderingDevice.SamplerFilter.Nearest; copied when the device creates its resource.

<a id="member-a252f84f2d08"></a>
### MinLOD

```csharp
public System.Single MinLOD { get; set; }
```

Gets or sets the minimum mip level, with zero identifying the base level.

Initially 0; copied when the device creates its resource.

<a id="member-a549b7947f32"></a>
### MipFilter

```csharp
public Electron2D.RenderingDevice.SamplerFilter MipFilter { get; set; }
```

Gets or sets how adjacent mip levels are selected or blended.

Initially RenderingDevice.SamplerFilter.Nearest; copied when the device creates its resource.

<a id="member-8e24410bc722"></a>
### RepeatU

```csharp
public Electron2D.RenderingDevice.SamplerRepeatMode RepeatU { get; set; }
```

Gets or sets horizontal normalized-coordinate addressing.

Initially RenderingDevice.SamplerRepeatMode.ClampToEdge; copied when the device creates its resource.

<a id="member-3c8621061881"></a>
### RepeatV

```csharp
public Electron2D.RenderingDevice.SamplerRepeatMode RepeatV { get; set; }
```

Gets or sets vertical normalized-coordinate addressing.

Initially RenderingDevice.SamplerRepeatMode.ClampToEdge; copied when the device creates its resource.

<a id="member-685ce1197486"></a>
### UseAnisotropy

```csharp
public System.Boolean UseAnisotropy { get; set; }
```

Gets or sets whether the sampler uses anisotropic filtering.

Initially false; copied when the device creates its resource.

## Lifetime and verification

Configurations are independent caller-owned ElectronObject values; disposing a description does not free an already-created RID. Device identities belong to the creating thread/device and are freed through FreeRID or device disposal. Borrowed sampler/image bindings expire when either identity is freed. Enum values own no native state.

Native GLSL/HLSL and image/filter/mip tests run through ELECTRON2D_TEST_DEVICE_TEXTURES on Linux/Vulkan. Other backends, native allocator measurements, implicit LOD-bias/anisotropy calibration and canvas/device sharing remain unverified. See [Local compute](../components/local-compute.md) and [ADR 0028](../decisions/rendering.md#adr-0028).
