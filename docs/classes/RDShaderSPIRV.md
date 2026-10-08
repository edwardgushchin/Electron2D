# RDShaderSPIRV

Last updated: 2026-10-09

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDShaderSPIRV`. **Source:** [RDShaderSPIRV.cs](../../src/Servers/Rendering/Device/RDShaderSPIRV.cs). **Component:** [Local compute](../components/local-compute.md).

**Inherits:** [Resource](Resource.md).

## Description

Owns independent copies of compute-stage SPIR-V bytes and its compiler diagnostic. Setters emit Changed. Typed descriptors, Resource duplication/copy and .e2dres serialization preserve the stored values. Device shader creation rejects a nonempty diagnostic or invalid/unsupported module. Other graphics/ray-tracing stages are not exposed by this compute resource slice. The resource owns no GPU handles; device-created shaders copy their module.

## Example

The snippet assumes the shown shader bindings and caller-provided compiled bytecode/buffer identities. The complete executable consumer is [WaterSimulation.GPU.cs](../../examples/WaterPlayground/WaterSimulation.GPU.cs).

```csharp
using var compiled = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes("water.comp.spv") };
using var copy = (RDShaderSPIRV)compiled.Duplicate();
var shader = device.ShaderCreateFromSPIRV(copy, "Water");
```

## API

| Declaration | Contract |
| --- | --- |
| `public RDShaderSPIRV()` | Creates an empty compiled shader resource. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Overrides the inherited typed lifetime, property-metadata or Resource-copy hook. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Overrides the inherited typed lifetime, property-metadata or Resource-copy hook. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Overrides the inherited typed lifetime, property-metadata or Resource-copy hook. |
| `public System.Byte[] GetStageBytecode(Electron2D.RenderingDevice.ShaderStage stage)` | Returns copied bytecode for a supported stage. |
| `public System.String GetStageCompileError(Electron2D.RenderingDevice.ShaderStage stage)` | Returns the compiler diagnostic for a supported stage. |
| `public System.Void SetStageBytecode(Electron2D.RenderingDevice.ShaderStage stage, System.Byte[] bytecode)` | Replaces copied bytecode for a supported stage. |
| `public System.Void SetStageCompileError(Electron2D.RenderingDevice.ShaderStage stage, System.String compileError)` | Replaces the compiler diagnostic for a supported stage. |
| `public System.Byte[] BytecodeCompute { get; set; }` | Gets or replaces copied compute-stage bytecode. |
| `public System.String CompileErrorCompute { get; set; }` | Gets or sets the compute compiler diagnostic. |

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

<a id="member-996dcafb86f9"></a>
### CreateDuplicateInstance()

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-59e972049cdd"></a>
### GetPropertyDescriptors()

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-43799afa56f0"></a>
### GetStageBytecode(Electron2D.RenderingDevice.ShaderStage)

```csharp
public System.Byte[] GetStageBytecode(Electron2D.RenderingDevice.ShaderStage stage)
```

Returns copied bytecode for a supported stage.

A copy of the compute module.

`stage`: Compute; other pipeline domains are not integrated.

<a id="member-8c1f34a802f8"></a>
### GetStageCompileError(Electron2D.RenderingDevice.ShaderStage)

```csharp
public System.String GetStageCompileError(Electron2D.RenderingDevice.ShaderStage stage)
```

Returns the compiler diagnostic for a supported stage.

The diagnostic, or an empty string.

`stage`: Compute.

<a id="member-321557e9aaf9"></a>
### SetStageBytecode(Electron2D.RenderingDevice.ShaderStage, System.Byte[])

```csharp
public System.Void SetStageBytecode(Electron2D.RenderingDevice.ShaderStage stage, System.Byte[] bytecode)
```

Replaces copied bytecode for a supported stage.

`stage`: Compute.

`bytecode`: Compiled module bytes.

<a id="member-e1378740d071"></a>
### SetStageCompileError(Electron2D.RenderingDevice.ShaderStage, System.String)

```csharp
public System.Void SetStageCompileError(Electron2D.RenderingDevice.ShaderStage stage, System.String compileError)
```

Replaces the compiler diagnostic for a supported stage.

`stage`: Compute.

`compileError`: The diagnostic, or an empty string.

<a id="member-062f4aaa06dd"></a>
### BytecodeCompute

```csharp
public System.Byte[] BytecodeCompute { get; set; }
```

Gets or replaces copied compute-stage bytecode.

An independent SPIR-V byte array; empty initially.

<a id="member-a0fad7f77cc7"></a>
### CompileErrorCompute

```csharp
public System.String CompileErrorCompute { get; set; }
```

Gets or sets the compute compiler diagnostic.

An empty string means no reported compiler error.

## Verification and limits

[RenderingDeviceTests](../../tests/Electron2D.Tests/RenderingDeviceTests.cs) checks actual native computation, buffer/descriptor/barrier semantics, resource copying/storage and invalid lifetime/thread access. [Local compute](../components/local-compute.md) records the supported module/resource profile. Broader device graphics APIs, asynchronous transfer APIs and additional platforms are not established by this slice.
