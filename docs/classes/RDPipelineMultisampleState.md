# RDPipelineMultisampleState

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDPipelineMultisampleState`. **Component:** [Local device](../components/local-compute.md#multisample-graphics-and-resolve).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Describes copied native sample count and alpha-to-coverage configuration.

SampleCount defaults to Samples1 and must equal every raster target. EnableAlphaToCoverage defaults to false and converts fragment alpha to native sample coverage. Native pipelines retain copied state. Sample masks, alpha-to-one and sample shading remain unavailable; no inert properties are exposed.

## Example

Partial configuration; [the executable public consumer](../../tests/Electron2D.Tests/RenderingDeviceMultisampleTests.cs) supplies device-owned images and shader modules.

```csharp
using var samples = new RDPipelineMultisampleState { SampleCount = RenderingDevice.TextureSamples.Samples4, EnableAlphaToCoverage = true };
```

## API

| Declaration | Contract |
| --- | --- |
| [`public RDPipelineMultisampleState()`](#member-e574244aca50) | Creates the default configuration. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-46f0bb79372c) | Projects inherited metadata followed by the executing configuration fields. |
| [`public System.Boolean EnableAlphaToCoverage { get; set; }`](#member-4fe261be55ad) | Converts fragment alpha into sample coverage. |
| [`public Electron2D.RenderingDevice.TextureSamples SampleCount { get; set; }`](#member-2b0d0be16ec4) | Selects the exact raster sample count. |

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

## Lifecycle, errors and verification

Descriptions borrow no native resources and can be disposed after creation. Invalid indices, repeated roles, mismatched formats/dimensions/sample counts and unavailable native counts reject before framebuffer or pipeline creation. Device RIDs retain owner-thread lifetime and feedback validation. [RenderingDeviceMultisampleTests](../../tests/Electron2D.Tests/RenderingDeviceMultisampleTests.cs) verifies native pixels, resolve/clear/canvas integration and warmed managed allocation on Linux/Vulkan. Foreign platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028).
