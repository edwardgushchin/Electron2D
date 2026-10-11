# RenderingDevice

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RenderingDevice`. **Source:** [RenderingDevice.cs](../../src/Servers/Rendering/Device/RenderingDevice.cs), [RenderingDevice.Readback.cs](../../src/Servers/Rendering/Device/RenderingDevice.Readback.cs). **Component:** [Local compute](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A caller-owned, owner-thread compute device created by RenderingServer.CreateLocalRenderingDevice. It supports native compute independently of the canvas rendering method. Each device owns its buffer/image/sampler/shader/pipeline/set RIDs; cross-device and freed identities reject. An active list is exclusive. End lists before submission, reads, frees or disposal; Submit starts recorded work, Sync waits, and BufferGetData synchronizes pending writes before returning. Disposal cancels unsubmitted commands and waits for submitted work. Buffer range/type, descriptor layout and thread/lifetime errors throw typed exceptions.

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
| `public System.Void BufferCopy(Electron2D.RID sourceBuffer, Electron2D.RID destinationBuffer, System.UInt32 sourceOffset, System.UInt32 destinationOffset, System.UInt32 sizeBytes)` | Copies disjoint validated byte ranges; storage copies stay on the GPU timeline. |
| `public System.Void BufferClear(Electron2D.RID buffer, System.UInt32 offset, System.UInt32 sizeBytes)` | Zeros a range with a size divisible by four. |
| `public System.Void BufferGetDataAsync(Electron2D.RID buffer, System.Action<System.Byte[]> callback, System.UInt32 offsetBytes = 0, System.UInt32 sizeBytes = 0)` | Queues a request-time snapshot and delivers an owned array at local completion. |
| `public System.Void BufferGetDataAsync(Electron2D.RID buffer, System.Memory<System.Byte> destination, System.Action callback, System.UInt32 offsetBytes = 0)` | Queues a snapshot into caller-owned storage; prepared queue/transfer resources are reused. |
| `public System.Void BufferUpdate(Electron2D.RID buffer, System.UInt32 offset, System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data)` | Updates a validated byte range of a buffer. |
| `public System.Void ComputeListAddBarrier(System.Int64 computeList)` | Establishes a full dependency after prior dispatches in this list. |
| `public System.Int64 ComputeListBegin()` | Begins an owner-thread compute list. |
| `public System.Void ComputeListBindComputePipeline(System.Int64 computeList, Electron2D.RID computePipeline)` | Selects a compute pipeline for subsequent dispatches. |
| `public System.Void ComputeListBindUniformSet(System.Int64 computeList, Electron2D.RID uniformSet, System.UInt32 setIndex)` | Binds a resource set for subsequent dispatches. |
| `public System.Void ComputeListDispatch(System.Int64 computeList, System.UInt32 xGroups, System.UInt32 yGroups, System.UInt32 zGroups)` | Records a compute dispatch with barriers against previous dispatches. |
| `public System.Void ComputeListDispatchIndirect(System.Int64 computeList, Electron2D.RID buffer, System.UInt32 offset)` | Reads X/Y/Z group counts from a storage buffer created for indirect dispatch. |
| `public System.Void ComputeListEnd()` | Ends the active list without submitting it. |
| `public Electron2D.RID ComputePipelineCreate(Electron2D.RID shader)` | Creates an executable pipeline borrowing a live compute shader. |
| `public System.Boolean ComputePipelineIsValid(Electron2D.RID pipeline)` | Reports whether a pipeline and its shader remain alive. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Overrides the inherited typed lifetime, property-metadata or Resource-copy hook. |
| `public System.Void FreeRID(Electron2D.RID rid)` | Frees a resource after completing pending work. |
| `public System.String GetDeviceName()` | Returns the selected compute-device name. |
| `public Electron2D.RID ShaderCreateFromSPIRV(Electron2D.RDShaderSPIRV spirvData, System.String name = "")` | Validates and creates a compute shader from compiled bytecode. |
| `public Electron2D.RID StorageBufferCreate(System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data = default, Electron2D.RenderingDevice.StorageBufferUsage usage = 0)` | Creates a zero-initialized read/write storage buffer with an optional indirect-command role. |
| `public System.Void Submit()` | Submits all recorded uploads and compute lists. |
| `public System.Void Sync()` | Waits for submitted work, releases its fence and delivers completed readback callbacks. |
| `public Electron2D.RID UniformBufferCreate(System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data = default)` | Creates a constant uniform buffer. |
| `public Electron2D.RID UniformSetCreate(System.ReadOnlySpan<Electron2D.RDUniform> uniforms, Electron2D.RID shader, System.UInt32 shaderSet)` | Creates a copied set of validated buffer/image/sampler bindings. |
| `public System.Boolean UniformSetIsValid(Electron2D.RID uniformSet)` | Reports whether a set and every borrowed resource remain alive. |
| `protected override System.Void ValidateDisposal()` | Overrides the inherited typed lifetime, property-metadata or Resource-copy hook. |
| `public Electron2D.RID SamplerCreate(Electron2D.RDSamplerState state)` | Creates a device-owned sampler from copied filtering, addressing and LOD state. |
| `public Electron2D.RID TextureCreate(Electron2D.RDTextureFormat format, Electron2D.RDTextureView view, System.ReadOnlySpan<System.Byte[]> data = default)` | Creates a device-owned two-dimensional image or image array with copied mip data. |
| `public System.Byte[] TextureGetData(Electron2D.RID texture, System.UInt32 layer)` | Synchronizes and returns an independent tightly packed layer mip chain. |
| `public System.Void TextureGetData(Electron2D.RID texture, System.UInt32 layer, System.Span<System.Byte> destination)` | Synchronizes and reads one layer mip chain into caller-owned storage. |
| `public System.Void TextureGetDataAsync(Electron2D.RID texture, System.UInt32 layer, System.Action<System.Byte[]> callback)` | Queues a request-time layer snapshot and delivers an independent mip chain at local completion. |
| `public System.Void TextureGetDataAsync(Electron2D.RID texture, System.UInt32 layer, System.Memory<System.Byte> destination, System.Action callback)` | Queues a layer snapshot into reusable caller-owned storage. |
| `public Electron2D.RDTextureFormat TextureGetFormat(Electron2D.RID texture)` | Copies the actual texture allocation description. |
| `public System.Boolean TextureIsFormatSupportedForUsage(Electron2D.RenderingDevice.DataFormat format, Electron2D.RenderingDevice.TextureUsageBits usageFlags)` | Reports whether the selected format supports the requested two-dimensional texture usage. |
| `public System.Boolean TextureIsValid(Electron2D.RID texture)` | Reports whether an identity is a live texture of this device. |
| `public System.Void TextureUpdate(Electron2D.RID texture, System.UInt32 layer, System.ReadOnlySpan<System.Byte> data)` | Updates one complete layer including every mip level. |

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
public Electron2D.RID StorageBufferCreate(System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data = default, Electron2D.RenderingDevice.StorageBufferUsage usage = 0)
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

Waits for submitted work, releases its fence and delivers completed readback callbacks.

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

Creates a copied set of validated buffer/image/sampler bindings.

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

[RenderingDeviceTests](../../tests/Electron2D.Tests/RenderingDeviceTests.cs) checks actual native computation, buffer/descriptor/barrier semantics, resource copying/storage and invalid lifetime/thread access. [Local compute](../components/local-compute.md) records the supported module/resource profile. Broader device graphics APIs, asynchronous texture transfer APIs and additional platforms are not established by this slice.

## Buffer transfer and indirect dispatch descriptions

`BufferCopy` takes source/destination RID identities, byte offsets and a byte size. All storage/uniform combinations execute; storage-to-uniform synchronizes, uniform-to-storage snapshots the host bytes, and storage-to-storage needs no CPU readback. Same-buffer overlapping ranges throw `ArgumentException`. Unaligned byte copies are supported. `BufferClear` takes a byte offset and a four-byte-divisible size; other sizes throw `ArgumentOutOfRangeException`. Both reject active compute lists, wrong/freed/foreign identities and ranges outside either buffer, including invalid empty operations.

`StorageBufferCreate` accepts [StorageBufferUsage](RenderingDevice.StorageBufferUsage.md); zero is the ordinary read/write role, and DispatchIndirect also permits command reads. Unknown flag bits reject before resource creation. `ComputeListDispatchIndirect` takes an active list, an indirect-capable storage buffer and an aligned byte offset. Twelve bytes must fit. The three uint counts remain on the GPU; the producer must stay within device limits. Zero groups skip execution. Pipeline and set validation is shared with direct dispatch; each closes its native compute pass to order subsequent consumers. [RenderingDeviceTests.Transfers](../../tests/Electron2D.Tests/RenderingDeviceTests.Transfers.cs) verifies the complete chain and edge cases on the native GPU backend.

## Asynchronous readback descriptions

BufferGetDataAsync validates the owning thread, buffer identity/kind, range and absence of an active compute list before queuing. Both callbacks must be nonnull. The array overload uses `sizeBytes == 0` for the remainder, and gives the callback its independent result. The Memory overload uses destination length for the range; retain valid storage without reading or modifying it until the callback. Empty and uniform-buffer requests still defer callback delivery until submission and completion.

```csharp
byte[] output = new byte[1024];
device.BufferGetDataAsync(buffer, output.AsMemory(), () => Consume(output));
device.Submit();
DoOtherCPUWork();
device.Sync();
```

The request records the snapshot before subsequent buffer updates or dispatches; it waits for no result of its own. Automatic waits for prior device work collect data without running callbacks. Sync, nonempty synchronous storage reads and FreeRID deliver completions in FIFO order on the owner thread. A synchronous read copies its result before invoking callbacks. FreeRID removes its resource before invoking callbacks. Nested Sync queues new completions behind the existing batch. Callback exceptions do not replay callbacks or suppress other completed callbacks: delivery throws AggregateException containing all original failures. A callback may dispose the device; remaining undelivered callbacks then cancel. Ordinary disposal also cancels requests, finishes submitted GPU work and releases the retained transfer pool.

[RenderingDeviceTests.Readback](../../tests/Electron2D.Tests/RenderingDeviceTests.Readback.cs) verifies real compute snapshots, source updates, independent retained arrays, caller storage, queue/transfer reuse, native owner-thread delivery, host/empty requests, invalid inputs, nested operations, freeing sources, callback failures and disposal. Prepare the largest range and concurrent count for zero managed infrastructure allocations with the Memory overload; output arrays are intentional allocations. This establishes Linux/Vulkan behavior; other platforms and native/driver allocator measurements remain unverified.

Caller-defined MemoryManager accessors cannot reenter this device while its destination span is being acquired. Attempts to synchronize, mutate or dispose it throw InvalidOperationException before native mapping or state mutation. A failed destination acquisition retains the submitted request for retry or cancellation during disposal. The native readback fixture verifies this guard and invalidated caller memory.


## Device image and sampler members

<a id="member-fa590dca9f47"></a>
### SamplerCreate(Electron2D.RDSamplerState)

```csharp
public Electron2D.RID SamplerCreate(Electron2D.RDSamplerState state)
```

Creates a device-owned sampler from copied filtering, addressing and LOD state.

An owned sampler identity.

`state`: Caller-owned sampler configuration.

Throws `System.NotSupportedException`: Border, mirror-clamp or unnormalized sampling requires a shader adapter.
<a id="member-e8e129d5af5f"></a>
### TextureCreate(Electron2D.RDTextureFormat, Electron2D.RDTextureView, System.ReadOnlySpan<System.Byte[]>)

```csharp
public Electron2D.RID TextureCreate(Electron2D.RDTextureFormat format, Electron2D.RDTextureView view, System.ReadOnlySpan<System.Byte[]> data = default)
```

Creates a device-owned two-dimensional image or image array with copied mip data.

A texture RID belonging to this device.

`format`: Allocation description copied before returning.

`view`: Initial view description; native format reinterpretation and swizzles require a view adapter.

`data`: One tightly packed mip chain per layer, or empty for zero initialization.

Throws `System.NotSupportedException`: The requested format, view or allocation role is not supported.
<a id="member-a3f596fd4b89"></a>
### TextureGetData(Electron2D.RID, System.UInt32)

```csharp
public System.Byte[] TextureGetData(Electron2D.RID texture, System.UInt32 layer)
```

Synchronizes and returns an independent tightly packed layer mip chain.

A copied layer mip chain.

`texture`: Live texture owned by this device.

`layer`: Zero-based layer.
<a id="member-5814cdb3b991"></a>
### TextureGetData(Electron2D.RID, System.UInt32, System.Span<System.Byte>)

```csharp
public System.Void TextureGetData(Electron2D.RID texture, System.UInt32 layer, System.Span<System.Byte> destination)
```

Synchronizes and reads one layer mip chain into caller-owned storage.

`texture`: Live texture owned by this device.

`layer`: Zero-based layer.

`destination`: Storage with the exact layer byte capacity.
<a id="member-909d746c6568"></a>
### TextureGetDataAsync(Electron2D.RID, System.UInt32, System.Action<System.Byte[]>)

```csharp
public System.Void TextureGetDataAsync(Electron2D.RID texture, System.UInt32 layer, System.Action<System.Byte[]> callback)
```

Queues a request-time layer snapshot and delivers an independent mip chain at local completion.

End the compute list, queue the read, Submit, then Sync. This shares buffer readback FIFO, callback-error, reentrancy and cancellation behavior. Later texture updates cannot replace the snapshot.

`texture`: A live readable texture of this device.

`layer`: Zero-based layer.

`callback`: Receives tight mip-chain bytes on the creating thread.
<a id="member-a05e2b60b188"></a>
### TextureGetDataAsync(Electron2D.RID, System.UInt32, System.Memory<System.Byte>, System.Action)

```csharp
public System.Void TextureGetDataAsync(Electron2D.RID texture, System.UInt32 layer, System.Memory<System.Byte> destination, System.Action callback)
```

Queues a layer snapshot into reusable caller-owned storage.

Preparation must cover the largest physical mip-chain size and peak concurrent request count. Device disposal cancels undelivered callbacks. Memory accessors cannot reenter this device.

`texture`: A live readable texture of this device.

`layer`: Zero-based layer.

`destination`: Exact tight mip-chain capacity; keep valid and untouched until completion.

`callback`: Runs after the destination is filled on the owner thread.
<a id="member-5495c94e7264"></a>
### TextureGetFormat(Electron2D.RID)

```csharp
public Electron2D.RDTextureFormat TextureGetFormat(Electron2D.RID texture)
```

Copies the actual texture allocation description.

A caller-owned description independent of the allocation.

`texture`: Live texture owned by this device.
<a id="member-1c2cef0b1c48"></a>
### TextureIsFormatSupportedForUsage(Electron2D.RenderingDevice.DataFormat, Electron2D.RenderingDevice.TextureUsageBits)

```csharp
public System.Boolean TextureIsFormatSupportedForUsage(Electron2D.RenderingDevice.DataFormat format, Electron2D.RenderingDevice.TextureUsageBits usageFlags)
```

Reports whether the selected format supports the requested two-dimensional texture usage.

False for formats or roles unavailable on this backend.

`format`: A concrete format to query.

`usageFlags`: Requested allocation and transfer roles.
<a id="member-b883b624c27c"></a>
### TextureIsValid(Electron2D.RID)

```csharp
public System.Boolean TextureIsValid(Electron2D.RID texture)
```

Reports whether an identity is a live texture of this device.

True only for a live owned texture.

`texture`: The identity to inspect.
<a id="member-f116e57f58db"></a>
### TextureUpdate(Electron2D.RID, System.UInt32, System.ReadOnlySpan<System.Byte>)

```csharp
public System.Void TextureUpdate(Electron2D.RID texture, System.UInt32 layer, System.ReadOnlySpan<System.Byte> data)
```

Updates one complete layer including every mip level.

`texture`: Live texture owned by this device.

`layer`: Zero-based layer.

`data`: Exact tightly packed layer mip-chain bytes.

Image data uses exact tightly packed mip chains per layer with hidden native offsets. The array and Memory asynchronous overloads share buffer readback completion, FIFO, cancellation and reentrancy rules. See [image profile and remaining dependencies](../components/local-compute.md#device-images-and-samplers).
