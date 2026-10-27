# RDShaderSPIRV

Last updated: 2026-10-27

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDShaderSPIRV`. **Source:** [RDShaderSPIRV.cs](../../src/Servers/Rendering/Device/RDShaderSPIRV.cs). **Component:** [Local device](../components/local-compute.md).

**Inherits:** [Resource](Resource.md).

## Description

A Resource owning copied stage bytecode and diagnostics. Every bytecode getter returns an independent array; setters copy and emit Changed. Duplication and resource archives preserve vertex, fragment and compute modules independently. RenderingDevice creation accepts either compute or both vertex and fragment; mixed, failed or incomplete programs reject. Geometry/tessellation and raytracing stages remain precise backend/product boundaries.

Compiled GLSL/HLSL fragment input attachments now execute single/multisample loads through private pixel-coordinate image-fetch translation, preserving the original public module bytes. The [ordered framebuffer input contract](../components/local-compute.md#ordered-framebuffer-passes-and-inputs) defines descriptor indices, scalar/sample validation and verified native limits.

## Example

Partial snippet requiring a live device and, for bytecode examples, compiled module arrays. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises this configuration.

```csharp
using var stages = new RDShaderSPIRV { BytecodeVertex = vertexBytes, BytecodeFragment = fragmentBytes };
var shader = device.ShaderCreateFromSPIRV(stages);
```

## API

| Declaration | Contract |
| --- | --- |
| `public RDShaderSPIRV()` | Creates an empty compiled shader resource. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Preserves the declaring type's lifecycle contract. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Preserves the declaring type's lifecycle contract. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Projects the inherited typed property metadata and this description's values. |
| `public System.Byte[] GetStageBytecode(Electron2D.RenderingDevice.ShaderStage stage)` | Returns copied bytecode for an integrated stage. |
| `public System.String GetStageCompileError(Electron2D.RenderingDevice.ShaderStage stage)` | Returns the diagnostic for an integrated stage. |
| `public System.Void SetStageBytecode(Electron2D.RenderingDevice.ShaderStage stage, System.Byte[] bytecode)` | Replaces copied bytecode for an integrated stage. |
| `public System.Void SetStageCompileError(Electron2D.RenderingDevice.ShaderStage stage, System.String compileError)` | Replaces the diagnostic for an integrated stage. |
| `public System.Byte[] BytecodeCompute { get; set; }` | Gets or replaces copied compute-stage bytecode. |
| `public System.Byte[] BytecodeFragment { get; set; }` | Gets or replaces copied fragment-stage bytecode. |
| `public System.Byte[] BytecodeVertex { get; set; }` | Gets or replaces copied vertex-stage bytecode. |
| `public System.String CompileErrorCompute { get; set; }` | Gets or sets the compute compiler diagnostic. |
| `public System.String CompileErrorFragment { get; set; }` | Gets or sets the fragment compiler diagnostic. |
| `public System.String CompileErrorVertex { get; set; }` | Gets or sets the vertex compiler diagnostic. |

## Member descriptions

<a id="member-b2b6e870f4fe"></a>
### RDShaderSPIRV()

```csharp
public RDShaderSPIRV()
```

Creates an empty compiled shader resource.


<a id="member-514c36168f4a"></a>
### CopyCustomStateTo(Electron2D.Resource, System.Boolean, Electron2D.DeepDuplicateMode, System.Func<Electron2D.Resource, Electron2D.Resource>, System.Func<Electron2D.Resource, Electron2D.Resource>)

```csharp
protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)
```

Preserves the declaring type's lifecycle contract.


<a id="member-996dcafb86f9"></a>
### CreateDuplicateInstance()

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

Preserves the declaring type's lifecycle contract.


<a id="member-59e972049cdd"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

Projects the inherited typed property metadata and this description's values.


<a id="member-43799afa56f0"></a>
### GetStageBytecode(Electron2D.RenderingDevice.ShaderStage)

```csharp
public System.Byte[] GetStageBytecode(Electron2D.RenderingDevice.ShaderStage stage)
```

Returns copied bytecode for an integrated stage.

An independent module copy.

`stage`: Vertex, fragment or compute.

`System.ArgumentOutOfRangeException`: The stage is invalid.


<a id="member-8c1f34a802f8"></a>
### GetStageCompileError(Electron2D.RenderingDevice.ShaderStage)

```csharp
public System.String GetStageCompileError(Electron2D.RenderingDevice.ShaderStage stage)
```

Returns the diagnostic for an integrated stage.

The compiler diagnostic, empty when absent.

`stage`: Vertex, fragment or compute.

`System.ArgumentOutOfRangeException`: The stage is invalid.


<a id="member-321557e9aaf9"></a>
### SetStageBytecode(Electron2D.RenderingDevice.ShaderStage, System.Byte[])

```csharp
public System.Void SetStageBytecode(Electron2D.RenderingDevice.ShaderStage stage, System.Byte[] bytecode)
```

Replaces copied bytecode for an integrated stage.

`stage`: Vertex, fragment or compute.

`bytecode`: Compiled module bytes.

`System.ArgumentOutOfRangeException`: The stage is invalid.


<a id="member-e1378740d071"></a>
### SetStageCompileError(Electron2D.RenderingDevice.ShaderStage, System.String)

```csharp
public System.Void SetStageCompileError(Electron2D.RenderingDevice.ShaderStage stage, System.String compileError)
```

Replaces the diagnostic for an integrated stage.

`stage`: Vertex, fragment or compute.

`compileError`: The diagnostic, empty when absent.

`System.ArgumentOutOfRangeException`: The stage is invalid.


<a id="member-062f4aaa06dd"></a>
### BytecodeCompute

```csharp
public System.Byte[] BytecodeCompute { get; set; }
```

Gets or replaces copied compute-stage bytecode.

An independent SPIR-V byte array; empty initially.


<a id="member-02aa24515db9"></a>
### BytecodeFragment

```csharp
public System.Byte[] BytecodeFragment { get; set; }
```

Gets or replaces copied fragment-stage bytecode.

An independent SPIR-V byte array; empty initially.


<a id="member-a1e79e81c613"></a>
### BytecodeVertex

```csharp
public System.Byte[] BytecodeVertex { get; set; }
```

Gets or replaces copied vertex-stage bytecode.

An independent SPIR-V byte array; empty initially.


<a id="member-a0fad7f77cc7"></a>
### CompileErrorCompute

```csharp
public System.String CompileErrorCompute { get; set; }
```

Gets or sets the compute compiler diagnostic.

An empty string means no reported compiler error.


<a id="member-bc2e221a82c8"></a>
### CompileErrorFragment

```csharp
public System.String CompileErrorFragment { get; set; }
```

Gets or sets the fragment compiler diagnostic.

An empty string means no reported compiler error.


<a id="member-5765982a02f2"></a>
### CompileErrorVertex

```csharp
public System.String CompileErrorVertex { get; set; }
```

Gets or sets the vertex compiler diagnostic.

An empty string means no reported compiler error.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RDShaderSPIRV.md) for absent roles.

Compiled compute/vertex/fragment modules may declare one binary push block per stage. The device preserves original 32-bit member offsets, fixed arrays, matrix strides/orientation and nested structs through raw-word translation. The executing profile supports at most 128 bytes and uses one native uniform slot; wider precision and pointer address/arithmetic require their own adapters. Push storage is separate from RDUniform descriptors. See [binary push parameters](../components/local-compute.md#binary-push-parameters) for recording, capacity, copied snapshots and verification.

Specialization declarations retain copied symbolic templates until pipeline creation resolves typed values. Workgroup dimensions and uniform/push fixed arrays use the resolved constants before native creation. See [typed pipeline specialization](../components/local-compute.md#typed-pipeline-specialization) for validation and ownership.

[ShaderCompileSPIRVFromSource](RenderingDevice.md#source-authoring) returns an independent compiled resource with diagnostics per attempted stage. Failed stages stay empty while successful siblings survive. Descriptions and returned byte arrays never expose cache storage; native creation still uses the common device capability, lifetime and program-role checks.

[RDShaderFile](RDShaderFile.md) borrows these resources under named versions and preserves aliases, copied stage bytecode and diagnostics in saved graphs. Selecting a file version supplies this same module to the ordinary device factories; replacing a version does not dispose its predecessor.

[Half modules](../components/local-compute.md#native-half-arithmetic-and-packed-data) use independently enabled native arithmetic/storage and the existing device execution path. Direct HLSL Half specialization retains the pinned frontend prerequisite.

[Native Int16/UInt16 execution](../components/local-compute.md#native-int16-and-uint16-arithmetic-and-packed-data) connects packed data, typed specialization and compute/graphics arithmetic with independently enabled storage and exact push reconstruction.

[Native byte shaders](../components/local-compute.md#native-int8-and-uint8-arithmetic-and-packed-data) execute packed data, specialization and compute/graphics arithmetic through independent features. Native8 GLSL uses the Vulkan 1.2 producer; the pinned HLSL native8 type remains a frontend dependency.
