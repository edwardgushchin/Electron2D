# RDShaderSource

Last updated: 2026-10-28

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDShaderSource`. **Component:** [Source authoring](../components/local-compute.md#source-authoring-and-build-imports).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Describes caller-owned vertex, fragment and compute sources for explicit shader authoring.

Compilation uses the separately installed import tool. Pipelines consume the resulting RDShaderSPIRV through the ordinary validated device path.

The source family supports the executing vertex, fragment and compute roles. GLSL is numeric zero and the default; HLSL is numeric one. Empty stage strings omit those stages. Null strings and invalid stage/language values reject. Typed property metadata/reversion preserves defaults. Tessellation requires its backend stage adapter; ray stages remain outside the product boundary.

## Example

The authoring host configures the separate ShaderImport executable; the resulting resource follows ordinary shader/pipeline creation.

```csharp
using var source = new RDShaderSource
{
    Language = RenderingDevice.ShaderLanguage.GLSL,
    SourceCompute = "#version 450\nlayout(local_size_x=1) in;\nvoid main() {}"
};
using var compiled = device.ShaderCompileSPIRVFromSource(source);
if (compiled.CompileErrorCompute.Length != 0)
    throw new InvalidOperationException(compiled.CompileErrorCompute);
var shader = device.ShaderCreateFromSPIRV(compiled);
var pipeline = device.ComputePipelineCreate(shader);
```

## API

| Declaration | Contract |
| --- | --- |
| [`public RDShaderSource()`](#member-9147efa636de) | Creates empty GLSL stage descriptions. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-ef4b4d150bff) | Projects inherited metadata followed by copied source and language descriptions. |
| [`public System.String GetStageSource(Electron2D.RenderingDevice.ShaderStage stage)`](#member-99d34ea83c1a) | Returns the source of an executable stage. |
| [`public System.Void SetStageSource(Electron2D.RenderingDevice.ShaderStage stage, System.String source)`](#member-42e4e4626ee3) | Replaces the source of an executable stage. |
| [`public Electron2D.RenderingDevice.ShaderLanguage Language { get; set; }`](#member-8a1c8d4805ee) | Gets or sets the source language shared by every stage. |
| [`public System.String SourceCompute { get; set; }`](#member-aecba9f69081) | Gets or sets the compute source. |
| [`public System.String SourceFragment { get; set; }`](#member-20087b2a424e) | Gets or sets the fragment source. |
| [`public System.String SourceVertex { get; set; }`](#member-bd0a5d5af674) | Gets or sets the vertex source. |

## Member descriptions

<a id="member-9147efa636de"></a>
### RDShaderSource()

```csharp
public RDShaderSource()
```

Creates empty GLSL stage descriptions.

<a id="member-ef4b4d150bff"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects inherited metadata followed by copied source and language descriptions.

Typed fields with empty source and GLSL defaults.

<a id="member-99d34ea83c1a"></a>
### GetStageSource(Electron2D.RenderingDevice.ShaderStage)

```csharp
public System.String GetStageSource(Electron2D.RenderingDevice.ShaderStage stage)
```

Returns the source of an executable stage.

The stage source, initially empty.

| Parameter | Contract |
| --- | --- |
| `stage` | Vertex, fragment or compute. |

- `T:System.ArgumentOutOfRangeException`: The stage has no executing source profile.

<a id="member-42e4e4626ee3"></a>
### SetStageSource(Electron2D.RenderingDevice.ShaderStage, System.String)

```csharp
public System.Void SetStageSource(Electron2D.RenderingDevice.ShaderStage stage, System.String source)
```

Replaces the source of an executable stage.

| Parameter | Contract |
| --- | --- |
| `stage` | Vertex, fragment or compute. |
| `source` | Non-null source; empty omits the stage. |

- `T:System.ArgumentOutOfRangeException`: The stage has no executing source profile.

<a id="member-8a1c8d4805ee"></a>
### Language

```csharp
public Electron2D.RenderingDevice.ShaderLanguage Language { get; set; }
```

Gets or sets the source language shared by every stage.

GLSL initially. Unknown language values reject when consumed.

<a id="member-aecba9f69081"></a>
### SourceCompute

```csharp
public System.String SourceCompute { get; set; }
```

Gets or sets the compute source.

Initially empty; null is invalid.

<a id="member-20087b2a424e"></a>
### SourceFragment

```csharp
public System.String SourceFragment { get; set; }
```

Gets or sets the fragment source.

Initially empty; null is invalid.

<a id="member-bd0a5d5af674"></a>
### SourceVertex

```csharp
public System.String SourceVertex { get; set; }
```

Gets or sets the vertex source.

Initially empty; null is invalid.

## Compilation, ownership and verification

Descriptions are snapshotted at authoring; returned stage bytes are independent of descriptions, cache storage and later mutations/disposal. The compiler returns stage diagnostics and preserves successful siblings; native creation retains program-role/interface/capability checks. Source compilation is a cold operation through an optional installed tool and is not required by games loading compiled assets. [RenderingDeviceSourceTests](../../tests/Electron2D.Tests/RenderingDeviceSourceTests.cs) covers defaults, errors, copied/cache results, include changes, real GPU compute/graphics/canvas and prepared zero managed recording on Linux/Vulkan. See [the component contract](../components/local-compute.md#source-authoring-and-build-imports) for bounds, cache bypass, toolkit delivery and unverified platforms/native allocations.

[Half modules](../components/local-compute.md#native-half-arithmetic-and-packed-data) use independently enabled native arithmetic/storage and the existing device execution path. Direct HLSL Half specialization retains the pinned frontend prerequisite.

[Native Int16/UInt16 execution](../components/local-compute.md#native-int16-and-uint16-arithmetic-and-packed-data) connects packed data, typed specialization and compute/graphics arithmetic with independently enabled storage and exact push reconstruction.

[Native byte shaders](../components/local-compute.md#native-int8-and-uint8-arithmetic-and-packed-data) execute packed data, specialization and compute/graphics arithmetic through independent features. Native8 GLSL uses the Vulkan 1.2 producer; the pinned HLSL native8 type remains a frontend dependency.

Ordinary writable vertex/fragment storage and formatted ImageBuffer resources execute with independently enabled native stage features and ordered pass-boundary dependencies. Shared descriptors, AsStorage vertex/index buffers, GPU-produced indirect commands and following compute/readback use the existing API. See [writable graphics storage](../components/local-compute.md#writable-graphics-storage-buffers) for ownership, synchronization and remaining adapters.

Formatted ImageBuffer stores and signed/unsigned R32 atomics now execute in independently enabled vertex/fragment stages, using the same encoded allocation as compute and sampled reads. See [formatted graphics producers](../components/local-compute.md#writable-formatted-graphics-producers) for format, array, synchronization, HLSL frontend and verification boundaries.
