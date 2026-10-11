# RDPipelineDepthStencilState

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDPipelineDepthStencilState`. **Component:** [Local device](../components/local-compute.md#depthstencil-graphics-and-sampling). **Source:** [RDPipelineDepthStencilState.cs](../../src/Servers/Rendering/Device/RDPipelineDepthStencilState.cs).

**Inherits:** [ElectronObject](ElectronObject.md). Configuration is copied at resource creation.

## Description

Describes copied depth testing, depth writes and eight-bit two-sided stencil operations.

Both visible faces must have equal effective masks and references on the current backend. Culling allows different unused-face values. Depth writes require an enabled depth test. Depth-bounds testing requires a separate native adapter and is not exposed as inert configuration.

## Example

Partial configuration snippet; [the executable public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) supplies the device, modules and targets.

```csharp
using var depth = new RDPipelineDepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, DepthCompareOperator = RenderingDevice.CompareOperator.Less };
```

## API

| Declaration | Contract |
| --- | --- |
| [`public RDPipelineDepthStencilState()`](#member-83fbc1d87c71) | Creates the default disabled tests and zeroing stencil operations. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-3cf8a688cdb3) | Projects inherited metadata followed by the copied depth/stencil configuration properties. |
| [`public Electron2D.RenderingDevice.CompareOperator BackOpCompare { get; set; }`](#member-be4752a2df9d) | Selects the back-face comparison. |
| [`public System.Int32 BackOpCompareMask { get; set; }`](#member-2b3140c16568) | Selects the back-face comparison mask; only its low eight bits are used. |
| [`public Electron2D.RenderingDevice.StencilOperation BackOpDepthFail { get; set; }`](#member-89caf1d6ef15) | Selects the back-face operation when depth fails. |
| [`public Electron2D.RenderingDevice.StencilOperation BackOpFail { get; set; }`](#member-ec9ef09cafc9) | Selects the back-face operation when stencil fails. |
| [`public Electron2D.RenderingDevice.StencilOperation BackOpPass { get; set; }`](#member-8961e7deab43) | Selects the back-face operation when both tests pass. |
| [`public System.Int32 BackOpReference { get; set; }`](#member-286cd10b66d3) | Selects the back-face reference from zero through 255. |
| [`public System.Int32 BackOpWriteMask { get; set; }`](#member-1a232f71b8e8) | Selects the back-face write mask; only its low eight bits are used. |
| [`public Electron2D.RenderingDevice.CompareOperator DepthCompareOperator { get; set; }`](#member-c1afe5ed4ce8) | Selects the depth comparison. |
| [`public System.Boolean EnableDepthTest { get; set; }`](#member-90d515620584) | Enables comparison of incoming depth against stored depth. |
| [`public System.Boolean EnableDepthWrite { get; set; }`](#member-92cdef245af6) | Enables depth writes for fragments passing an enabled depth test. |
| [`public System.Boolean EnableStencil { get; set; }`](#member-7ae1fecfe60f) | Enables front and back stencil tests. |
| [`public Electron2D.RenderingDevice.CompareOperator FrontOpCompare { get; set; }`](#member-7e19e2fddcc2) | Selects the front-face comparison. |
| [`public System.Int32 FrontOpCompareMask { get; set; }`](#member-1d3f77436429) | Selects the front-face comparison mask; only its low eight bits are used. |
| [`public Electron2D.RenderingDevice.StencilOperation FrontOpDepthFail { get; set; }`](#member-b856a04fa973) | Selects the front-face operation when depth fails. |
| [`public Electron2D.RenderingDevice.StencilOperation FrontOpFail { get; set; }`](#member-be198b057677) | Selects the front-face operation when stencil fails. |
| [`public Electron2D.RenderingDevice.StencilOperation FrontOpPass { get; set; }`](#member-24c3d897cbab) | Selects the front-face operation when both tests pass. |
| [`public System.Int32 FrontOpReference { get; set; }`](#member-cd80e4c050c8) | Selects the front-face reference from zero through 255. |
| [`public System.Int32 FrontOpWriteMask { get; set; }`](#member-219802895014) | Selects the front-face write mask; only its low eight bits are used. |

## Member descriptions

<a id="member-83fbc1d87c71"></a>
### RDPipelineDepthStencilState()

```csharp
public RDPipelineDepthStencilState()
```

Creates the default disabled tests and zeroing stencil operations.

<a id="member-3cf8a688cdb3"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects inherited metadata followed by the copied depth/stencil configuration properties.

Typed configuration descriptors with the pinned defaults.

<a id="member-be4752a2df9d"></a>
### BackOpCompare

```csharp
public Electron2D.RenderingDevice.CompareOperator BackOpCompare { get; set; }
```

Selects the back-face comparison.

Initially RenderingDevice.CompareOperator.Always; validated and copied at pipeline creation.

<a id="member-2b3140c16568"></a>
### BackOpCompareMask

```csharp
public System.Int32 BackOpCompareMask { get; set; }
```

Selects the back-face comparison mask; only its low eight bits are used.

Initially 0; validated and copied at pipeline creation.

<a id="member-89caf1d6ef15"></a>
### BackOpDepthFail

```csharp
public Electron2D.RenderingDevice.StencilOperation BackOpDepthFail { get; set; }
```

Selects the back-face operation when depth fails.

Initially RenderingDevice.StencilOperation.Zero; validated and copied at pipeline creation.

<a id="member-ec9ef09cafc9"></a>
### BackOpFail

```csharp
public Electron2D.RenderingDevice.StencilOperation BackOpFail { get; set; }
```

Selects the back-face operation when stencil fails.

Initially RenderingDevice.StencilOperation.Zero; validated and copied at pipeline creation.

<a id="member-8961e7deab43"></a>
### BackOpPass

```csharp
public Electron2D.RenderingDevice.StencilOperation BackOpPass { get; set; }
```

Selects the back-face operation when both tests pass.

Initially RenderingDevice.StencilOperation.Zero; validated and copied at pipeline creation.

<a id="member-286cd10b66d3"></a>
### BackOpReference

```csharp
public System.Int32 BackOpReference { get; set; }
```

Selects the back-face reference from zero through 255.

Initially 0; validated and copied at pipeline creation.

<a id="member-1a232f71b8e8"></a>
### BackOpWriteMask

```csharp
public System.Int32 BackOpWriteMask { get; set; }
```

Selects the back-face write mask; only its low eight bits are used.

Initially 0; validated and copied at pipeline creation.

<a id="member-c1afe5ed4ce8"></a>
### DepthCompareOperator

```csharp
public Electron2D.RenderingDevice.CompareOperator DepthCompareOperator { get; set; }
```

Selects the depth comparison.

Initially RenderingDevice.CompareOperator.Always; validated and copied at pipeline creation.

<a id="member-90d515620584"></a>
### EnableDepthTest

```csharp
public System.Boolean EnableDepthTest { get; set; }
```

Enables comparison of incoming depth against stored depth.

Initially false; validated and copied at pipeline creation.

<a id="member-92cdef245af6"></a>
### EnableDepthWrite

```csharp
public System.Boolean EnableDepthWrite { get; set; }
```

Enables depth writes for fragments passing an enabled depth test.

Initially false; validated and copied at pipeline creation.

<a id="member-7ae1fecfe60f"></a>
### EnableStencil

```csharp
public System.Boolean EnableStencil { get; set; }
```

Enables front and back stencil tests.

Initially false; validated and copied at pipeline creation.

<a id="member-7e19e2fddcc2"></a>
### FrontOpCompare

```csharp
public Electron2D.RenderingDevice.CompareOperator FrontOpCompare { get; set; }
```

Selects the front-face comparison.

Initially RenderingDevice.CompareOperator.Always; validated and copied at pipeline creation.

<a id="member-1d3f77436429"></a>
### FrontOpCompareMask

```csharp
public System.Int32 FrontOpCompareMask { get; set; }
```

Selects the front-face comparison mask; only its low eight bits are used.

Initially 0; validated and copied at pipeline creation.

<a id="member-b856a04fa973"></a>
### FrontOpDepthFail

```csharp
public Electron2D.RenderingDevice.StencilOperation FrontOpDepthFail { get; set; }
```

Selects the front-face operation when depth fails.

Initially RenderingDevice.StencilOperation.Zero; validated and copied at pipeline creation.

<a id="member-be198b057677"></a>
### FrontOpFail

```csharp
public Electron2D.RenderingDevice.StencilOperation FrontOpFail { get; set; }
```

Selects the front-face operation when stencil fails.

Initially RenderingDevice.StencilOperation.Zero; validated and copied at pipeline creation.

<a id="member-24c3d897cbab"></a>
### FrontOpPass

```csharp
public Electron2D.RenderingDevice.StencilOperation FrontOpPass { get; set; }
```

Selects the front-face operation when both tests pass.

Initially RenderingDevice.StencilOperation.Zero; validated and copied at pipeline creation.

<a id="member-cd80e4c050c8"></a>
### FrontOpReference

```csharp
public System.Int32 FrontOpReference { get; set; }
```

Selects the front-face reference from zero through 255.

Initially 0; validated and copied at pipeline creation.

<a id="member-219802895014"></a>
### FrontOpWriteMask

```csharp
public System.Int32 FrontOpWriteMask { get; set; }
```

Selects the front-face write mask; only its low eight bits are used.

Initially 0; validated and copied at pipeline creation.

## Lifecycle, errors and verification

Copied descriptions borrow no native state and can be disposed after creation. Device RIDs remain owner-thread resources. Unknown enum selectors and nonfinite bias/clear values reject. References must fit eight bits for visible stencil faces; both visible faces require equal effective masks/reference. Depth-bounds and raw aspect-transfer adapters remain explicit prerequisites in coverage. [RenderingDeviceDepthTests](../../tests/Electron2D.Tests/RenderingDeviceDepthTests.cs) verifies native pixels and warmed managed allocation on Linux/Vulkan; foreign platforms and native allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028).
