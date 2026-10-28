# RDPipelineRasterizationState

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDPipelineRasterizationState`. **Component:** [Local device](../components/local-compute.md#depthstencil-graphics-and-sampling). **Source:** [RDPipelineRasterizationState.cs](../../src/Servers/Rendering/Device/RDPipelineRasterizationState.cs).

**Inherits:** [ElectronObject](ElectronObject.md). Configuration is copied at resource creation.

## Description

Describes copied PipelineRasterizationState configuration for graphics resources.



Constant/slope depth bias and clamp execute in the native pipeline. Invalid/nonfinite values fail before native creation; unsupported native features propagate failure. Other raster states retain their exact backend prerequisites.

## Example

Partial configuration snippet; [the executable public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) supplies the device, modules and targets.

```csharp
using var raster = new RDPipelineRasterizationState { DepthBiasEnabled = true, DepthBiasConstantFactor = 1, DepthBiasSlopeFactor = 1 };
```

## API

| Declaration | Contract |
| --- | --- |
| [`public RDPipelineRasterizationState()`](#member-35065472c003) | Creates the default configuration. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-2603f8905045) | Projects the caller-owned configuration through inherited typed property metadata. |
| [`public Electron2D.RenderingDevice.PolygonCullMode CullMode { get; set; }`](#member-52a815bc9638) | Gets or sets the face culling selection. |
| [`public System.Single DepthBiasClamp { get; set; }`](#member-30c609f67529) | Bounds depth bias: positive values cap it, negative values floor it, and zero disables clamping. |
| [`public System.Single DepthBiasConstantFactor { get; set; }`](#member-1a4d122f9ca9) | Scales the minimum representable depth increment. |
| [`public System.Boolean DepthBiasEnabled { get; set; }`](#member-32e2d324281a) | Enables constant and slope-dependent depth bias. |
| [`public System.Single DepthBiasSlopeFactor { get; set; }`](#member-70e2e438e5e7) | Scales the maximum primitive depth slope. |
| [`public System.Boolean EnableDepthClamp { get; set; }`](#member-0249a8f655fc) | Gets or sets depth clamping rather than clip-depth rejection. |
| [`public Electron2D.RenderingDevice.PolygonFrontFace FrontFace { get; set; }`](#member-ecfde3f806ff) | Gets or sets the winding treated as front-facing. |
| [`public System.Boolean Wireframe { get; set; }`](#member-b33a02d6143b) | Gets or sets outline rather than filled rasterization. |

## Member descriptions

<a id="member-35065472c003"></a>
### RDPipelineRasterizationState()

```csharp
public RDPipelineRasterizationState()
```

Creates the default configuration.

<a id="member-2603f8905045"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects the caller-owned configuration through inherited typed property metadata.

Inherited descriptors followed by this description's properties; configuration is copied when consumed.

<a id="member-52a815bc9638"></a>
### CullMode

```csharp
public Electron2D.RenderingDevice.PolygonCullMode CullMode { get; set; }
```

Gets or sets the face culling selection.

Initially RenderingDevice.PolygonCullMode.Disabled; validated and copied by its device consumer.

<a id="member-30c609f67529"></a>
### DepthBiasClamp

```csharp
public System.Single DepthBiasClamp { get; set; }
```

Bounds depth bias: positive values cap it, negative values floor it, and zero disables clamping.

Initially 0; copied at resource creation.

<a id="member-1a4d122f9ca9"></a>
### DepthBiasConstantFactor

```csharp
public System.Single DepthBiasConstantFactor { get; set; }
```

Scales the minimum representable depth increment.

Initially 0; copied at resource creation.

<a id="member-32e2d324281a"></a>
### DepthBiasEnabled

```csharp
public System.Boolean DepthBiasEnabled { get; set; }
```

Enables constant and slope-dependent depth bias.

Initially false; copied at resource creation.

<a id="member-70e2e438e5e7"></a>
### DepthBiasSlopeFactor

```csharp
public System.Single DepthBiasSlopeFactor { get; set; }
```

Scales the maximum primitive depth slope.

Initially 0; copied at resource creation.

<a id="member-0249a8f655fc"></a>
### EnableDepthClamp

```csharp
public System.Boolean EnableDepthClamp { get; set; }
```

Gets or sets depth clamping rather than clip-depth rejection.

Initially false; validated and copied by its device consumer.

<a id="member-ecfde3f806ff"></a>
### FrontFace

```csharp
public Electron2D.RenderingDevice.PolygonFrontFace FrontFace { get; set; }
```

Gets or sets the winding treated as front-facing.

Initially RenderingDevice.PolygonFrontFace.Clockwise; validated and copied by its device consumer.

<a id="member-b33a02d6143b"></a>
### Wireframe

```csharp
public System.Boolean Wireframe { get; set; }
```

Gets or sets outline rather than filled rasterization.

Initially false; validated and copied by its device consumer.

## Lifecycle, errors and verification

Copied descriptions borrow no native state and can be disposed after creation. Device RIDs remain owner-thread resources. Unknown enum selectors and nonfinite bias/clear values reject. References must fit eight bits for visible stencil faces; both visible faces require equal effective masks/reference. Depth-bounds and raw aspect-transfer adapters remain explicit prerequisites in coverage. [RenderingDeviceDepthTests](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) verifies native pixels and warmed managed allocation on Linux/Vulkan; foreign platforms and native allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028).

## Primitive discard

`DiscardPrimitives` defaults to false and is copied at graphics pipeline creation. True executes vertex work without rasterization or fragment execution, including vertex-only programs without Position output. Its inherited property descriptor retains ordinary get/set/disposal behavior. See [the executing contract and checks](../components/raster-discard.md).
