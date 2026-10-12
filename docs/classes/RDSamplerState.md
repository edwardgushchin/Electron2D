# RDSamplerState

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDSamplerState`. **Component:** [Local device](../components/local-compute.md#depthstencil-graphics-and-sampling). **Source:** [RDSamplerState.cs](../../src/Servers/Rendering/Device/RDSamplerState.cs).

**Inherits:** [ElectronObject](ElectronObject.md). Configuration is copied at resource creation.

## Description

Describes filtering, normalized-coordinate addressing and mip selection for a device-owned sampler.

The current profile supports repeat, mirrored repeat and clamp-to-edge. Other addressing modes fail during creation. Integer image sampling requires nearest filters without anisotropy. Nonzero LOD bias fails on a backend that cannot apply it.

## Example

Partial configuration snippet; [the executable public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) supplies the device, modules and targets.

```csharp
using var sampler = new RDSamplerState { EnableCompare = true, CompareOp = RenderingDevice.CompareOperator.Less };
```

## API

| Declaration | Contract |
| --- | --- |
| [`public RDSamplerState()`](#member-a8bd01fce139) | Creates the default SamplerState configuration. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-6b12dd7b8fb0) |  |
| [`public System.Single AnisotropyMax { get; set; }`](#member-6985464d4589) | Gets or sets the anisotropic filtering limit from one through sixteen. |
| [`public Electron2D.RenderingDevice.CompareOperator CompareOp { get; set; }`](#member-d2dcf21fc847) | Selects comparison of a sample reference against stored depth. |
| [`public System.Boolean EnableCompare { get; set; }`](#member-2200aaf8cb47) | Enables comparison sampling for a depth image. |
| [`public System.Single LODBias { get; set; }`](#member-2cd90dd88a1b) | Gets or sets the bias added to the implicit mip selection. |
| [`public Electron2D.RenderingDevice.SamplerFilter MagFilter { get; set; }`](#member-116efb58d0d5) | Gets or sets the within-mip filter used for magnification. |
| [`public System.Single MaxLOD { get; set; }`](#member-2bfe2e10cd15) | Gets or sets the maximum mip level used for sampling. |
| [`public Electron2D.RenderingDevice.SamplerFilter MinFilter { get; set; }`](#member-642eaeb354fb) | Gets or sets the within-mip filter used for minification. |
| [`public System.Single MinLOD { get; set; }`](#member-a252f84f2d08) | Gets or sets the minimum mip level, with zero identifying the base level. |
| [`public Electron2D.RenderingDevice.SamplerFilter MIPFilter { get; set; }`](#member-a549b7947f32) | Gets or sets how adjacent mip levels are selected or blended. |
| [`public Electron2D.RenderingDevice.SamplerRepeatMode RepeatU { get; set; }`](#member-8e24410bc722) | Gets or sets horizontal normalized-coordinate addressing. |
| [`public Electron2D.RenderingDevice.SamplerRepeatMode RepeatV { get; set; }`](#member-3c8621061881) | Gets or sets vertical normalized-coordinate addressing. |
| [`public System.Boolean UseAnisotropy { get; set; }`](#member-685ce1197486) | Gets or sets whether the sampler uses anisotropic filtering. |

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

<a id="member-6985464d4589"></a>
### AnisotropyMax

```csharp
public System.Single AnisotropyMax { get; set; }
```

Gets or sets the anisotropic filtering limit from one through sixteen.

Initially 1; copied when the device creates its resource.

<a id="member-d2dcf21fc847"></a>
### CompareOp

```csharp
public Electron2D.RenderingDevice.CompareOperator CompareOp { get; set; }
```

Selects comparison of a sample reference against stored depth.

Initially RenderingDevice.CompareOperator.Always; copied at resource creation.

<a id="member-2200aaf8cb47"></a>
### EnableCompare

```csharp
public System.Boolean EnableCompare { get; set; }
```

Enables comparison sampling for a depth image.

Initially false; copied at resource creation.

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
### MIPFilter

```csharp
public Electron2D.RenderingDevice.SamplerFilter MIPFilter { get; set; }
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

## Lifecycle, errors and verification

Copied descriptions borrow no native state and can be disposed after creation. Device RIDs remain owner-thread resources. Unknown enum selectors and nonfinite bias/clear values reject. References must fit eight bits for visible stencil faces; both visible faces require equal effective masks/reference. Depth-bounds and raw aspect-transfer adapters remain explicit prerequisites in coverage. [RenderingDeviceDepthTests](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) verifies native pixels and warmed managed allocation on Linux/Vulkan; foreign platforms and native allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028).
