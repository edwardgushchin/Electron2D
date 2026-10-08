# RenderingDevice

Last updated: 2026-10-09

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RenderingDevice`. **Source:** [RenderingDevice.cs](../../src/Servers/Rendering/Device/RenderingDevice.cs). **Component:** [Local compute](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A caller-owned, owner-thread compute device created by RenderingServer.CreateLocalRenderingDevice. It supports native compute independently of the canvas rendering method. Each device owns its buffer/shader/pipeline/set RIDs; cross-device and freed identities reject. An active list is exclusive. End lists before submission, reads, frees or disposal; Submit starts recorded work, Sync waits, and BufferGetData synchronizes pending writes before returning. Disposal cancels unsubmitted commands and waits for submitted work. Buffer range/type, descriptor layout and thread/lifetime errors throw typed exceptions.

## Example

The snippet assumes the shown shader bindings and caller-provided compiled bytecode/buffer identities. The complete executable consumer is [WaterSimulation.GPU.cs](../../examples/WaterPlayground/WaterSimulation.GPU.cs).

```csharp
using var device = RenderingServer.CreateLocalRenderingDevice();
using var code = new RDShaderSPIRV { BytecodeCompute = compiledComputeBytes };
var shader = device.ShaderCreateFromSPIRV(code);
var pipeline = device.ComputePipelineCreate(shader);
var buffer = device.StorageBufferCreate(1024);
using var uniform = new RDUniform { Binding = 0, UniformType = RenderingDevice.UniformType.StorageBuffer };
uniform.AddID(buffer);
var set = device.UniformSetCreate([uniform], shader, 0);
var list = device.ComputeListBegin();
device.ComputeListBindComputePipeline(list, pipeline);
device.ComputeListBindUniformSet(list, set, 0);
device.ComputeListDispatch(list, 4, 1, 1);
device.ComputeListEnd();
device.Submit();
device.Sync();
byte[] result = device.BufferGetData(buffer);
```

## API

| Declaration | Contract |
| --- | --- |
| `public System.Void BufferGetData(Electron2D.RID buffer, System.Span<System.Byte> destination, System.UInt32 offsetBytes = 0)` | Synchronizes and copies a buffer range into caller-provided storage. |
| `public System.Byte[] BufferGetData(Electron2D.RID buffer, System.UInt32 offsetBytes = 0, System.UInt32 sizeBytes = 0)` | Synchronizes and copies a buffer range into an independent array. |
| `public System.Void BufferUpdate(Electron2D.RID buffer, System.UInt32 offset, System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data)` | Updates a validated byte range of a buffer. |
| `public System.Void ComputeListAddBarrier(System.Int64 computeList)` | Establishes a full dependency after prior dispatches in this list. |
| `public System.Int64 ComputeListBegin()` | Begins an owner-thread compute list. |
| `public System.Void ComputeListBindComputePipeline(System.Int64 computeList, Electron2D.RID computePipeline)` | Selects a compute pipeline for subsequent dispatches. |
| `public System.Void ComputeListBindUniformSet(System.Int64 computeList, Electron2D.RID uniformSet, System.UInt32 setIndex)` | Binds a resource set for subsequent dispatches. |
| `public System.Void ComputeListDispatch(System.Int64 computeList, System.UInt32 xGroups, System.UInt32 yGroups, System.UInt32 zGroups)` | Records a compute dispatch with barriers against previous dispatches. |
| `public System.Void ComputeListEnd()` | Ends the active list without submitting it. |
| `public Electron2D.RID ComputePipelineCreate(Electron2D.RID shader)` | Creates an executable pipeline borrowing a live compute shader. |
| `public System.Boolean ComputePipelineIsValid(Electron2D.RID pipeline)` | Reports whether a pipeline and its shader remain alive. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Overrides the inherited typed lifetime, property-metadata or Resource-copy hook. |
| `public System.Void FreeRID(Electron2D.RID rid)` | Frees a resource after completing pending work. |
| `public System.String GetDeviceName()` | Returns the selected compute-device name. |
| `public Electron2D.RID ShaderCreateFromSPIRV(Electron2D.RDShaderSPIRV spirvData, System.String name = "")` | Validates and creates a compute shader from compiled bytecode. |
| `public Electron2D.RID StorageBufferCreate(System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data = default)` | Creates a zero-initialized read/write storage buffer. |
| `public System.Void Submit()` | Submits all recorded uploads and compute lists. |
| `public System.Void Sync()` | Waits for the submitted work and releases its fence. |
| `public Electron2D.RID UniformBufferCreate(System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data = default)` | Creates a constant uniform buffer. |
| `public Electron2D.RID UniformSetCreate(System.ReadOnlySpan<Electron2D.RDUniform> uniforms, Electron2D.RID shader, System.UInt32 shaderSet)` | Creates a copied set of validated buffer bindings. |
| `public System.Boolean UniformSetIsValid(Electron2D.RID uniformSet)` | Reports whether a set and every borrowed resource remain alive. |
| `protected override System.Void ValidateDisposal()` | Overrides the inherited typed lifetime, property-metadata or Resource-copy hook. |

## Member descriptions

<a id="member-036f43cc63f1"></a>
### BufferGetData(Electron2D.RID, System.Span<System.Byte>, System.UInt32)

```csharp
public System.Void BufferGetData(Electron2D.RID buffer, System.Span<System.Byte> destination, System.UInt32 offsetBytes = 0)
```

Synchronizes and copies a buffer range into caller-provided storage.

`buffer`: A live buffer.

`destination`: Determines the number of bytes to read.

`offsetBytes`: First byte to read.

<a id="member-b41be5aa98cd"></a>
### BufferGetData(Electron2D.RID, System.UInt32, System.UInt32)

```csharp
public System.Byte[] BufferGetData(Electron2D.RID buffer, System.UInt32 offsetBytes = 0, System.UInt32 sizeBytes = 0)
```

Synchronizes and copies a buffer range into an independent array.

An independent array containing the completed device data.

`buffer`: A live buffer.

`offsetBytes`: First byte to read.

`sizeBytes`: Bytes to read, or zero for the remaining buffer.

<a id="member-68d362219def"></a>
### BufferUpdate(Electron2D.RID, System.UInt32, System.UInt32, System.ReadOnlySpan<System.Byte>)

```csharp
public System.Void BufferUpdate(Electron2D.RID buffer, System.UInt32 offset, System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data)
```

Updates a validated byte range of a buffer.

`buffer`: A live storage or uniform buffer.

`offset`: First byte to replace.

`sizeBytes`: Number of bytes copied from data.

`data`: At least sizeBytes source bytes; copied before returning.

<a id="member-ddba4dc9502f"></a>
### ComputeListAddBarrier(System.Int64)

```csharp
public System.Void ComputeListAddBarrier(System.Int64 computeList)
```

Establishes a full dependency after prior dispatches in this list.

Every dispatch already closes its native compute pass, providing this dependency.

`computeList`: The active list.

<a id="member-cade0a386fee"></a>
### ComputeListBegin()

```csharp
public System.Int64 ComputeListBegin()
```

Begins an owner-thread compute list.

The active list identity; only one list may be active.

<a id="member-36e98d0b82ee"></a>
### ComputeListBindComputePipeline(System.Int64, Electron2D.RID)

```csharp
public System.Void ComputeListBindComputePipeline(System.Int64 computeList, Electron2D.RID computePipeline)
```

Selects a compute pipeline for subsequent dispatches.

`computeList`: The active list.

`computePipeline`: A live executable pipeline.

<a id="member-3d39c14b32ff"></a>
### ComputeListBindUniformSet(System.Int64, Electron2D.RID, System.UInt32)

```csharp
public System.Void ComputeListBindUniformSet(System.Int64 computeList, Electron2D.RID uniformSet, System.UInt32 setIndex)
```

Binds a resource set for subsequent dispatches.

`computeList`: The active list.

`uniformSet`: A complete live uniform set.

`setIndex`: Its descriptor-set index.

<a id="member-254cdc8b6f12"></a>
### ComputeListDispatch(System.Int64, System.UInt32, System.UInt32, System.UInt32)

```csharp
public System.Void ComputeListDispatch(System.Int64 computeList, System.UInt32 xGroups, System.UInt32 yGroups, System.UInt32 zGroups)
```

Records a compute dispatch with barriers against previous dispatches.

`computeList`: The active list.

`xGroups`: Positive X group count, at most 65535.

`yGroups`: Positive Y group count, at most 65535.

`zGroups`: Positive Z group count, at most 65535.

<a id="member-0e791287a63d"></a>
### ComputeListEnd()

```csharp
public System.Void ComputeListEnd()
```

Ends the active list without submitting it.

<a id="member-7a5a76a8573d"></a>
### ComputePipelineCreate(Electron2D.RID)

```csharp
public Electron2D.RID ComputePipelineCreate(Electron2D.RID shader)
```

Creates an executable pipeline borrowing a live compute shader.

A pipeline identity invalidated when its shader is freed.

`shader`: A shader on this device.

<a id="member-a0c6f69b98c1"></a>
### ComputePipelineIsValid(Electron2D.RID)

```csharp
public System.Boolean ComputePipelineIsValid(Electron2D.RID pipeline)
```

Reports whether a pipeline and its shader remain alive.

True for a live executable pipeline of this device.

`pipeline`: The identity to inspect.

<a id="member-0ec79fd50f82"></a>
### Dispose(System.Boolean)

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

<a id="member-811b05b8347c"></a>
### FreeRID(Electron2D.RID)

```csharp
public System.Void FreeRID(Electron2D.RID rid)
```

Frees a resource after completing pending work.

Borrowers become invalid; they may still be freed explicitly.

`rid`: A live identity owned by this device.

<a id="member-6a41660f977d"></a>
### GetDeviceName()

```csharp
public System.String GetDeviceName()
```

Returns the selected compute-device name.

The native device description.

<a id="member-b577960c0617"></a>
### ShaderCreateFromSPIRV(Electron2D.RDShaderSPIRV, System.String)

```csharp
public Electron2D.RID ShaderCreateFromSPIRV(Electron2D.RDShaderSPIRV spirvData, System.String name = "")
```

Validates and creates a compute shader from compiled bytecode.

A shader identity owned by this device.

`spirvData`: The copied compute stage, with no compiler error.

`name`: Optional diagnostic label.

<a id="member-8126118533ad"></a>
### StorageBufferCreate(System.UInt32, System.ReadOnlySpan<System.Byte>)

```csharp
public Electron2D.RID StorageBufferCreate(System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data = default)
```

Creates a zero-initialized read/write storage buffer.

A device-owned buffer identity.

`sizeBytes`: Positive byte capacity, divisible by four.

`data`: Optional initial prefix; remaining bytes are zero.

<a id="member-1c36e8462a4b"></a>
### Submit()

```csharp
public System.Void Submit()
```

Submits all recorded uploads and compute lists.

<a id="member-f19d85cb4659"></a>
### Sync()

```csharp
public System.Void Sync()
```

Waits for the submitted work and releases its fence.

<a id="member-2488ae168080"></a>
### UniformBufferCreate(System.UInt32, System.ReadOnlySpan<System.Byte>)

```csharp
public Electron2D.RID UniformBufferCreate(System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data = default)
```

Creates a constant uniform buffer.

A device-owned buffer identity.

`sizeBytes`: Positive capacity divisible by four, at most 16 KiB.

`data`: Optional initial prefix; remaining bytes are zero.

<a id="member-ac776b4e8d39"></a>
### UniformSetCreate(System.ReadOnlySpan<Electron2D.RDUniform>, Electron2D.RID, System.UInt32)

```csharp
public Electron2D.RID UniformSetCreate(System.ReadOnlySpan<Electron2D.RDUniform> uniforms, Electron2D.RID shader, System.UInt32 shaderSet)
```

Creates a copied set of validated buffer bindings.

A set identity borrowing its shader and buffers.

`uniforms`: Descriptors with one buffer identity per binding.

`shader`: The shader whose layout owns this set.

`shaderSet`: The shader descriptor-set number.

<a id="member-1243db305d9a"></a>
### UniformSetIsValid(Electron2D.RID)

```csharp
public System.Boolean UniformSetIsValid(Electron2D.RID uniformSet)
```

Reports whether a set and every borrowed resource remain alive.

True for an intact set of this device.

`uniformSet`: The identity to inspect.

<a id="member-011a1350f97f"></a>
### ValidateDisposal()

```csharp
protected override System.Void ValidateDisposal()
```

## Verification and limits

[RenderingDeviceTests](../../tests/Electron2D.Tests/RenderingDeviceTests.cs) checks actual native computation, buffer/descriptor/barrier semantics, resource copying/storage and invalid lifetime/thread access. [Local compute](../components/local-compute.md) records the supported module/resource profile. Broader device graphics APIs, asynchronous transfer APIs and additional platforms are not established by this slice.
