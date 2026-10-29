# RDSamplerState

Last updated: 2026-10-29

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDSamplerState`. **Component:** [Local device](../components/local-compute.md#depthstencil-graphics-and-sampling). **Source:** [RDSamplerState.cs](../../src/Servers/Rendering/Device/RDSamplerState.cs).

**Inherits:** [ElectronObject](ElectronObject.md). Configuration is copied at resource creation.

## Description

Describes filtering, normalized or texel-coordinate addressing and mip selection for device-owned samplers and copied shader bindings.

The current profile supports repeat, mirrored repeat, clamp-to-edge, native Vulkan clamp-to-border and negotiated [mirror-clamp](../components/sampler-mirror-clamp.md). Unsupported native capabilities fail during creation. Integer image sampling requires nearest filters without effective anisotropy; a maximum of one disables anisotropy. Nonzero LOD bias fails on a backend that cannot apply it.

`BorderColor` selects one of six predefined [floating-point or integer colors](RenderingDevice.SamplerBorderColor.md), initially FloatOpaqueBlack. It is copied with the rest of the configuration, appears in typed property descriptors and is ignored when neither axis uses ClampToBorder. The color scalar class must match the sampled image. See [native border sampling](../components/sampler-borders.md).

`UnnormalizedUVW` selects native texel coordinates, initially false. See [sampler coordinates](../components/sampler-coordinates.md) for the complete state, operation and actual-view requirements. Shader descriptions copy this flag; archives preserve it without retaining the configuration object.

## Example

Partial configuration snippet; [the executable public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) supplies the device, modules and targets.

```csharp
using var sampler = new RDSamplerState { EnableCompare = true, CompareOp = RenderingDevice.CompareOperator.Less };
```

## API

| Declaration | Contract |
| --- | --- |
| [`public System.Boolean UnnormalizedUVW { get; set; }`](#unnormalized-coordinates) | Selects texel coordinates; initially false. |
| [`public Electron2D.RenderingDevice.SamplerBorderColor BorderColor { get; set; }`](#border-color) | Selects the clamp-to-border color; initially FloatOpaqueBlack. |
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
| [`public Electron2D.RenderingDevice.SamplerRepeatMode RepeatU { get; set; }`](#member-8e24410bc722) | Gets or sets horizontal coordinate addressing. |
| [`public Electron2D.RenderingDevice.SamplerRepeatMode RepeatV { get; set; }`](#member-3c8621061881) | Gets or sets vertical coordinate addressing. |
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

Initially 1; copied when a device sampler or shader binding is configured.

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

Initially 0; copied when a device sampler or shader binding is configured.

<a id="member-116efb58d0d5"></a>
### MagFilter

```csharp
public Electron2D.RenderingDevice.SamplerFilter MagFilter { get; set; }
```

Gets or sets the within-mip filter used for magnification.

Initially RenderingDevice.SamplerFilter.Nearest; copied when a device sampler or shader binding is configured.

<a id="member-2bfe2e10cd15"></a>
### MaxLOD

```csharp
public System.Single MaxLOD { get; set; }
```

Gets or sets the maximum mip level used for sampling.

Initially 1e20f; copied when a device sampler or shader binding is configured.

<a id="member-642eaeb354fb"></a>
### MinFilter

```csharp
public Electron2D.RenderingDevice.SamplerFilter MinFilter { get; set; }
```

Gets or sets the within-mip filter used for minification.

Initially RenderingDevice.SamplerFilter.Nearest; copied when a device sampler or shader binding is configured.

<a id="member-a252f84f2d08"></a>
### MinLOD

```csharp
public System.Single MinLOD { get; set; }
```

Gets or sets the minimum mip level, with zero identifying the base level.

Initially 0; copied when a device sampler or shader binding is configured.

<a id="member-a549b7947f32"></a>
### MIPFilter

```csharp
public Electron2D.RenderingDevice.SamplerFilter MIPFilter { get; set; }
```

Gets or sets how adjacent mip levels are selected or blended.

Initially RenderingDevice.SamplerFilter.Nearest; copied when a device sampler or shader binding is configured.

<a id="member-8e24410bc722"></a>
### RepeatU

```csharp
public Electron2D.RenderingDevice.SamplerRepeatMode RepeatU { get; set; }
```

Gets or sets horizontal coordinate addressing.

Initially RenderingDevice.SamplerRepeatMode.ClampToEdge; copied when a device sampler or shader binding is configured.

<a id="member-3c8621061881"></a>
### RepeatV

```csharp
public Electron2D.RenderingDevice.SamplerRepeatMode RepeatV { get; set; }
```

Gets or sets vertical coordinate addressing.

Initially RenderingDevice.SamplerRepeatMode.ClampToEdge; copied when a device sampler or shader binding is configured.

<a id="member-685ce1197486"></a>
### UseAnisotropy

```csharp
public System.Boolean UseAnisotropy { get; set; }
```

Gets or sets whether the sampler uses anisotropic filtering.

Initially false; copied when a device sampler or shader binding is configured.

<a id="border-color"></a>
### BorderColor

```csharp
public Electron2D.RenderingDevice.SamplerBorderColor BorderColor { get; set; }
```

Gets or sets the color used by clamp-to-border axes. Undefined values and Max reject immediately. Creation copies the value. Disposed configuration access throws ObjectDisposedException. Floating-point variants apply to normalized, floating-point and depth images; integer variants apply to signed and unsigned integer images. Shader authoring and device binding reject a mismatched scalar class before native sampling.

<a id="unnormalized-coordinates"></a>
### UnnormalizedUVW

```csharp
public System.Boolean UnnormalizedUVW { get; set; }
```

Selects texel coordinates without changing other description fields. Creation/Shader authoring require equal min/mag filters, nearest mip selection, zero MinLOD/MaxLOD, clamp-to-edge/border U/V, disabled effective anisotropy and disabled comparison. Supported native bindings have one ordinary layer and one mip level. Illegal state combinations and shader/view roles reject before native sampling. Disposed access throws ObjectDisposedException. The getter, typed descriptor, copied state and shader metadata preserve the authored flag.

## Lifecycle, errors and verification

Copied descriptions borrow no native state and can be disposed after creation. Device RIDs remain owner-thread resources. Unknown enum selectors and nonfinite bias/clear values reject. References must fit eight bits for visible stencil faces; both visible faces require equal effective masks/reference. Depth-bounds and raw aspect-transfer adapters remain explicit prerequisites in coverage. [RenderingDeviceDepthTests](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) verifies native pixels and warmed managed allocation on Linux/Vulkan; foreign platforms and native allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028).

[Shader.SetTextureSamplerState](Shader.md#texture-sampler-state) copies this configuration without borrowing the object. Its getter returns a fresh disposable copy. High-level shader images reject enabled comparison; device depth images retain comparison support. Setting UseAnisotropy with AnisotropyMax equal to one disables the native anisotropy flag and remains legal for nearest integer sampling and relaxed devices. [SamplerConfigurationTests](../../tests/Electron2D.Tests/SamplerConfigurationTests.cs) checks this exact projection and backend guards; [the public consumer](../../tests/ShaderSamplers.Consumer/ShaderSamplers.Consumer.csproj) checks persistence and real sampled pixels.

Explicit MSAA sample loads ignore filtering/address/border/comparison state. Combined descriptors still borrow a live sampler; ordinary sampler creation validation remains in effect. See [multisample shader reads](../components/local-compute.md#multisample-shader-reads).
