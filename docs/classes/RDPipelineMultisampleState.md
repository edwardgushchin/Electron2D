# RDPipelineMultisampleState

Last updated: 2026-10-28

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDPipelineMultisampleState`. **Component:** [Local device](../components/local-compute.md#multisample-graphics-and-resolve).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Describes copied native sample coverage, fragment frequency and alpha configuration.

SampleCount defaults to Samples1 and must equal every raster target. EnableAlphaToCoverage defaults to false and converts fragment alpha to native sample coverage. Native pipelines retain copied state. SampleMasks copies reads/writes and keeps the count-sized public vector shape. Sample shading/fraction and alpha-to-one execute through independently enabled native features. See [sample controls](../components/multisample-state.md).

## Example

Partial configuration; [the executable public consumer](../../tests/Electron2D.Tests/RenderingDeviceMultisampleTests.cs) supplies device-owned images and shader modules.

```csharp
using var samples = new RDPipelineMultisampleState { SampleCount = RenderingDevice.TextureSamples.Samples4, EnableAlphaToCoverage = true, SampleMasks = [0b0101u, 0, 0, 0] };
```

## API

| Declaration | Contract |
| --- | --- |
| [`public RDPipelineMultisampleState()`](#member-e574244aca50) | Creates the default configuration. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-46f0bb79372c) | Projects inherited metadata followed by the executing configuration fields. |
| [`public System.Boolean EnableAlphaToCoverage { get; set; }`](#member-4fe261be55ad) | Converts fragment alpha into sample coverage. |
| [`public Electron2D.RenderingDevice.TextureSamples SampleCount { get; set; }`](#member-2b0d0be16ec4) | Selects the exact raster sample count. |
| [`public System.Boolean EnableAlphaToOne { get; set; }`](#member-enablealphatoone) | Replaces surviving color alpha with one after coverage and before blending; false initially. |
| [`public System.Boolean EnableSampleShading { get; set; }`](#member-enablesampleshading) | Enables a minimum fraction of distinct fragment sample invocations; false initially. |
| [`public System.Single MinSampleShading { get; set; }`](#member-minsampleshading) | Finite fraction zero through one; zero initially, effective when sample shading is enabled. |
| [`public System.UInt32[] SampleMasks { get; set; }`](#member-samplemasks) | Copied raw mask words; empty enables all samples, otherwise array length equals raster sample count. |

## Member descriptions

<a id="member-e574244aca50"></a>
### RDPipelineMultisampleState()

```csharp
public RDPipelineMultisampleState()
```

Creates the default configuration.

<a id="member-46f0bb79372c"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects inherited metadata followed by the executing configuration fields.

Typed configuration descriptors with their default values.

<a id="member-4fe261be55ad"></a>
### EnableAlphaToCoverage

```csharp
public System.Boolean EnableAlphaToCoverage { get; set; }
```

Converts fragment alpha into sample coverage.

Initially false; copied when the device consumes this configuration.

<a id="member-2b0d0be16ec4"></a>
### SampleCount

```csharp
public Electron2D.RenderingDevice.TextureSamples SampleCount { get; set; }
```

Selects the exact raster sample count.

Initially RenderingDevice.TextureSamples.Samples1; copied when the device consumes this configuration.

<a id="member-enablealphatoone"></a>
### EnableAlphaToOne

```csharp
public System.Boolean EnableAlphaToOne { get; set; }
```

Replaces surviving color alpha with one after coverage and before blending; false initially. Pipeline creation copies the value; later description changes do not affect existing pipelines.

<a id="member-enablesampleshading"></a>
### EnableSampleShading

```csharp
public System.Boolean EnableSampleShading { get; set; }
```

Enables a minimum fraction of distinct fragment sample invocations; false initially. Pipeline creation copies the value; later description changes do not affect existing pipelines.

<a id="member-minsampleshading"></a>
### MinSampleShading

```csharp
public System.Single MinSampleShading { get; set; }
```

Finite fraction zero through one; zero initially, effective when sample shading is enabled. Pipeline creation copies the value; later description changes do not affect existing pipelines.

<a id="member-samplemasks"></a>
### SampleMasks

```csharp
public System.UInt32[] SampleMasks { get; set; }
```

Copied raw mask words; empty enables all samples, otherwise array length equals raster sample count. Pipeline creation copies the value; later description changes do not affect existing pipelines.

SampleMasks bit b of word w selects sample 32*w+b. Current one/two/four/eight-sample pipelines consume only the first word; unused trailing entries and bits are ignored. Nonempty array length must equal raster sample count. Null assignments reject, and reads/writes copy. MinSampleShading rejects nonfinite or out-of-range assignments. Requested missing sample-shading/alpha-to-one features reject at pipeline creation; mask coverage needs neither optional feature. Disposed access throws ObjectDisposedException. Sample-index/position shader inputs independently force full frequency. See [native semantics and executable profiles](../components/multisample-state.md).

## Lifecycle, errors and verification

Descriptions borrow no native resources and can be disposed after creation. Invalid indices, repeated roles, mismatched formats/dimensions/sample counts and unavailable native counts reject before framebuffer or pipeline creation. Device RIDs retain owner-thread lifetime and feedback validation. [RenderingDeviceMultisampleTests](../../tests/Electron2D.Tests/RenderingDeviceMultisampleTests.cs) verifies native pixels, resolve/clear/canvas integration and warmed managed allocation on Linux/Vulkan. Foreign platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028).

The complete property family is exercised by [DeviceGraphicsStorage.Consumer](../../tests/DeviceGraphicsStorage.Consumer/GraphicsStorage.Samples.cs), including per-sample writes, binary publication refusal, copied views/passes/variants and retained canvas in CLR/strict NativeAOT. Native/driver heap, throughput, foreign backend/platform and owner acceptance remain separate.
