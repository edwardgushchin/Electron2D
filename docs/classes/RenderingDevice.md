# RenderingDevice

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RenderingDevice`. **Source:** [RenderingDevice.cs](../../src/Servers/Rendering/Device/RenderingDevice.cs), [RenderingDevice.Readback.cs](../../src/Servers/Rendering/Device/RenderingDevice.Readback.cs). **Component:** [Local compute](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

An owner-thread graphics and compute device created locally by RenderingServer.CreateLocalRenderingDevice or retained by the active GPU renderer through RenderingServer.GetRenderingDevice. The retained renderer device flushes after canvas authoring and before drawing, and completes queued reads before FramePostDraw; Submit, Sync and public disposal are local-only. Renderer-owned image RIDs bind directly to TextureRD/TextureArrayRD canvas resources. It supports native compute independently of the canvas rendering method. Each device owns its buffer/image/sampler/shader/pipeline/set RIDs; cross-device and freed identities reject. An active list is exclusive. End lists before submission, reads, frees or disposal; Submit starts recorded work, Sync waits, and BufferGetData synchronizes pending writes before returning. Disposal cancels unsubmitted commands and waits for submitted work. Buffer range/type, descriptor layout and thread/lifetime errors throw typed exceptions.

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
| `public Electron2D.RID ShaderCreateFromSPIRV(Electron2D.RDShaderSPIRV spirvData, System.String name = "")` | Validates and creates a compute shader or a paired graphics shader from compiled bytecode. |
| `public Electron2D.RID StorageBufferCreate(System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data = default, Electron2D.RenderingDevice.StorageBufferUsage usage = 0)` | Creates a zero-initialized read/write storage buffer with an optional indirect-command role. |
| `public System.Void Submit()` | Submits all recorded uploads and compute lists. |
| `public System.Void Sync()` | Waits for submitted work, releases its fence and delivers completed readback callbacks. |
| `public Electron2D.RID UniformBufferCreate(System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data = default)` | Creates a constant uniform buffer. |
| `public Electron2D.RID UniformSetCreate(System.ReadOnlySpan<Electron2D.RDUniform> uniforms, Electron2D.RID shader, System.UInt32 shaderSet)` | Creates a copied set of validated buffer/image/sampler bindings. |
| `public System.Boolean UniformSetIsValid(Electron2D.RID uniformSet)` | Reports whether a set and every borrowed resource remain alive. |
| `protected override System.Void ValidateDisposal()` | Overrides the inherited typed lifetime, property-metadata or Resource-copy hook. |
| [`public Electron2D.RID SamplerCreate(Electron2D.RDSamplerState state)`](#member-fa590dca9f47) | Creates a device-owned sampler from copied filtering, addressing and LOD state. |
| [`public Electron2D.RID TextureCreate(Electron2D.RDTextureFormat format, Electron2D.RDTextureView view, System.ReadOnlySpan<System.Byte[]> data = default)`](#member-e8e129d5af5f) | Creates a device-owned two-dimensional image or image array with copied mip data. |
| `public System.Byte[] TextureGetData(Electron2D.RID texture, System.UInt32 layer)` | Synchronizes and returns an independent tightly packed layer mip chain. |
| `public System.Void TextureGetData(Electron2D.RID texture, System.UInt32 layer, System.Span<System.Byte> destination)` | Synchronizes and reads one layer mip chain into caller-owned storage. |
| `public System.Void TextureGetDataAsync(Electron2D.RID texture, System.UInt32 layer, System.Action<System.Byte[]> callback)` | Queues a request-time layer snapshot and delivers an independent mip chain at local completion. |
| `public System.Void TextureGetDataAsync(Electron2D.RID texture, System.UInt32 layer, System.Memory<System.Byte> destination, System.Action callback)` | Queues a layer snapshot into reusable caller-owned storage. |
| `public Electron2D.RDTextureFormat TextureGetFormat(Electron2D.RID texture)` | Copies the actual texture allocation description. |
| `public System.Boolean TextureIsFormatSupportedForUsage(Electron2D.RenderingDevice.DataFormat format, Electron2D.RenderingDevice.TextureUsageBits usageFlags)` | Reports whether the selected format supports the requested two-dimensional texture usage. |
| `public System.Boolean TextureIsValid(Electron2D.RID texture)` | Reports whether an identity is a live texture of this device. |
| `public System.Void TextureUpdate(Electron2D.RID texture, System.UInt32 layer, System.ReadOnlySpan<System.Byte> data)` | Updates one complete layer including every mip level. |
| `public System.Void TextureClear(Electron2D.RID texture, Electron2D.Color color, System.UInt32 baseMipmap, System.UInt32 mipmapCount, System.UInt32 baseLayer, System.UInt32 layerCount)` | Records a color clear of complete selected image layers and mip levels. |
| `public System.Void TextureCopy(Electron2D.RID fromTexture, Electron2D.RID toTexture, Electron2D.Vector3 fromPosition, Electron2D.Vector3 toPosition, Electron2D.Vector3 size, System.UInt32 sourceMipmap, System.UInt32 destinationMipmap, System.UInt32 sourceLayer, System.UInt32 destinationLayer)` | Records a raw rectangular image copy on the GPU timeline. |

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

Validates and creates a compute shader or a paired graphics shader from compiled bytecode.

A shader identity owned by this device.

`spirvData`: Copied modules with no compiler errors: either compute or both vertex and fragment.

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

| Parameter | Contract |
| --- | --- |
| `state` | Caller-owned sampler configuration. |

- `T:System.NotSupportedException`: Border, mirror-clamp or unnormalized sampling requires a shader adapter.
<a id="member-e8e129d5af5f"></a>
### TextureCreate(Electron2D.RDTextureFormat, Electron2D.RDTextureView, System.ReadOnlySpan<System.Byte[]>)

```csharp
public Electron2D.RID TextureCreate(Electron2D.RDTextureFormat format, Electron2D.RDTextureView view, System.ReadOnlySpan<System.Byte[]> data = default)
```

Creates a device-owned two-dimensional image or image array with copied mip data.

A texture RID belonging to this device.

| Parameter | Contract |
| --- | --- |
| `format` | Allocation description copied before returning. |
| `view` | Initial view description; native format reinterpretation and swizzles require a view adapter. |
| `data` | One tightly packed color mip chain per layer, or empty for zero initialization. Depth/stencil images require empty data and initialize every layer/mip to depth zero and stencil zero. |

- `T:System.NotSupportedException`: The requested format, view or allocation role is not supported.
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

## Regional image transfer members

<a id="member-52ac7894a778"></a>
### TextureClear(Electron2D.RID, Electron2D.Color, System.UInt32, System.UInt32, System.UInt32, System.UInt32)

```csharp
public System.Void TextureClear(Electron2D.RID texture, Electron2D.Color color, System.UInt32 baseMipmap, System.UInt32 mipmapCount, System.UInt32 baseLayer, System.UInt32 layerCount)
```

Records a color clear of complete selected image layers and mip levels.

Unselected subresources retain their contents. A retained private color target supports transfer-only destinations without changing their public usage. It is reused after preparation. Encoded block formats and unverified integer-clear backends fail explicitly. Narrow signed lanes must be representable; unsigned lanes narrow by discarding higher bits. The clear stays on the GPU timeline and neither submits nor delivers callbacks.

`texture`: Live destination with CanCopyTo usage.

`color`: Clear components; integer formats use their raw 32-bit lane bits on Vulkan.

`baseMipmap`: First zero-based mip level.

`mipmapCount`: Positive number of mip levels to clear.

`baseLayer`: First zero-based image layer.

`layerCount`: Positive number of layers to clear.

Throws `System.ArgumentOutOfRangeException`: The selected range is empty/out of bounds, or a signed clear lane exceeds its component width.

Throws `System.InvalidOperationException`: The copy destination role or owner/list state is invalid, or native recording fails.

Throws `System.NotSupportedException`: The format/backend has no executable color-clear path.

<a id="member-8be0893e02f9"></a>
### TextureCopy(Electron2D.RID, Electron2D.RID, Electron2D.Vector3, Electron2D.Vector3, Electron2D.Vector3, System.UInt32, System.UInt32, System.UInt32, System.UInt32)

```csharp
public System.Void TextureCopy(Electron2D.RID fromTexture, Electron2D.RID toTexture, Electron2D.Vector3 fromPosition, Electron2D.Vector3 toPosition, Electron2D.Vector3 size, System.UInt32 sourceMipmap, System.UInt32 destinationMipmap, System.UInt32 sourceLayer, System.UInt32 destinationLayer)
```

Records a raw rectangular image copy on the GPU timeline.

Coordinates are truncated to integral texels after bounds validation. Matching encoded texel/block footprints may be reinterpreted on Vulkan; other backends currently require the same format. Compressed offsets must begin on blocks and partial blocks must reach both image edges. Same-subresource rectangles must not overlap. A prepared native scratch image preserves disjoint self copies. This operation neither submits nor invokes readback callbacks.

`fromTexture`: Live source with CanCopyFrom usage.

`toTexture`: Live destination with CanCopyTo usage.

`fromPosition`: Source texel coordinates; Z must be zero.

`toPosition`: Destination texel coordinates; Z must be zero.

`size`: Positive texel extent with Z equal to one.

`sourceMipmap`: Zero-based source mip level.

`destinationMipmap`: Zero-based destination mip level.

`sourceLayer`: Zero-based source image layer.

`destinationLayer`: Zero-based destination image layer.

Throws `System.ArgumentException`: The texture, region, format footprint or overlap is invalid.

Throws `System.ArgumentOutOfRangeException`: A coordinate, mip level or layer is out of range.

Throws `System.InvalidOperationException`: A required usage is absent, the device state is invalid or native recording fails.

Throws `System.NotSupportedException`: This backend cannot reinterpret the selected formats.

The following snippet requires live source/destination image RIDs with the documented transfer roles. The complete native public-API consumer is [RenderingDeviceTextureTransferTests](../../tests/Electron2D.Tests/RenderingDeviceTextureTransferTests.cs).

```csharp
device.TextureClear(destination, new Color(0, 0, 0, 1), 0, 1, 0, 1);
device.TextureCopy(source, destination, new Vector3(0, 0, 0),
    new Vector3(4, 4, 0), new Vector3(8, 8, 1), 0, 0, 0, 0);
device.TextureGetDataAsync(destination, 0, bytes => Consume(bytes));
device.Submit();
device.Sync();
```

Recorded copies and clears obey queued snapshot ordering and retain private native scratch storage until completion/free. Vulkan native results include normalized quantization ties, linear-to-sRGB conversion and raw integer clear lanes. Exact byte preservation, malformed ranges, roles/lifetimes, self copies, compressed edges and prepared zero owner-thread managed allocation are checked. Other native drivers, encoded-block clears, native allocator measurements and canvas/device sharing retain explicit verification limits/dependencies. See [regional profile](../components/local-compute.md#image-region-copies-and-clears).

## Color graphics API

The [graphics component contract](../components/local-compute.md#color-graphics-and-compute-integration) defines copied descriptions, borrowed resource identities, single-sample color passes, shader interface matching, stage resources, direct/indexed/instanced/procedural/indirect work, and retained canvas consumption. Compute and draw lists share one owner-thread timeline; only one list is active.

| Declaration | Contract |
| --- | --- |
| [`public System.Int64 DrawListBegin(Electron2D.RID framebuffer, Electron2D.RenderingDevice.DrawFlags drawFlags = DefaultAll, System.ReadOnlySpan<Electron2D.Color> clearColorValues = default, Electron2D.Rect2 region = default, System.Single clearDepthValue = 1f, System.Int32 clearStencilValue = 0)`](#member-c6018364e892) | Begins an ordered color and depth/stencil render pass on borrowed framebuffer images. |
| `public System.Void DrawListBindIndexArray(System.Int64 drawList, Electron2D.RID indexArray)` | Binds a live borrowed index range for subsequent indexed draws. |
| `public System.Void DrawListBindRenderPipeline(System.Int64 drawList, Electron2D.RID renderPipeline)` | Binds a graphics pipeline matching the active framebuffer. |
| `public System.Void DrawListBindUniformSet(System.Int64 drawList, Electron2D.RID uniformSet, System.UInt32 setIndex)` | Binds a complete resource set at its logical shader set index. |
| `public System.Void DrawListBindVertexArray(System.Int64 drawList, Electron2D.RID vertexArray)` | Binds a live borrowed vertex array for subsequent draws. |
| `public System.Void DrawListBindVertexBuffersFormat(System.Int64 drawList, System.Int32 vertexFormat, System.UInt32 vertexCount, System.ReadOnlySpan<Electron2D.RID> vertexBuffers, System.ReadOnlySpan<System.UInt32> offsets = default)` | Binds copied buffer identities and offsets directly for a vertex layout. |
| `public System.Void DrawListDisableScissor(System.Int64 drawList)` | Disables custom scissoring by restoring the complete framebuffer. |
| `public System.Void DrawListDraw(System.Int64 drawList, System.Boolean useIndices, System.UInt32 instances, System.UInt32 proceduralVertexCount = 0)` | Records a procedural, vertex-array or indexed draw with validated instance capacity. |
| `public System.Void DrawListDrawIndirect(System.Int64 drawList, System.Boolean useIndices, Electron2D.RID buffer, System.UInt32 offset = 0, System.UInt32 drawCount = 1, System.UInt32 stride = 0)` | Records GPU-directed draws from validated indirect command ranges. |
| `public System.Void DrawListEnableScissor(System.Int64 drawList, Electron2D.Rect2 rect = default)` | Clips subsequent draws to the rectangle intersected with the framebuffer. |
| `public System.Void DrawListEnd()` | Ends the active draw list, preserving its results for subsequent device work. |
| `public System.Void DrawListSetBlendConstants(System.Int64 drawList, Electron2D.Color color)` | Sets the finite blend constant for subsequent draws. |
| [`public Electron2D.RID FramebufferCreate(System.ReadOnlySpan<Electron2D.RID> textures, System.Int32 validateWithFormat = -1)`](#member-a56b363d17da) | Creates a framebuffer borrowing device-owned color and depth/stencil images. |
| [`public System.Int32 FramebufferFormatCreate(System.ReadOnlySpan<Electron2D.RDAttachmentFormat> attachments)`](#member-d154729327fd) | Creates a copied single-pass color and depth/stencil attachment layout. |
| `public Electron2D.RenderingDevice.TextureSamples FramebufferFormatGetTextureSamples(System.Int32 framebufferFormat, System.UInt32 renderPass = 0)` | Reports the sample count of a framebuffer pass. |
| `public System.Int32 FramebufferGetFormat(Electron2D.RID framebuffer)` | Reads the framebuffer's matching layout identity. |
| `public System.Boolean FramebufferIsValid(Electron2D.RID framebuffer)` | Reports whether a framebuffer and all its borrowed attachments remain alive. |
| `public Electron2D.RID IndexArrayCreate(Electron2D.RID indexBuffer, System.UInt32 indexOffset, System.UInt32 indexCount)` | Creates a borrowed index range. |
| `public Electron2D.RID IndexBufferCreate(System.UInt32 sizeIndices, Electron2D.RenderingDevice.IndexBufferFormat format, System.ReadOnlySpan<System.Byte> data = default, System.Boolean useRestartIndices = false, Electron2D.RenderingDevice.BufferCreationBits creationBits = 0)` | Creates a copied unsigned index buffer. |
| [`public Electron2D.RID RenderPipelineCreate(Electron2D.RID shader, System.Int32 framebufferFormat, System.Int32 vertexFormat, Electron2D.RenderingDevice.RenderPrimitive primitive, Electron2D.RDPipelineRasterizationState rasterizationState, Electron2D.RDPipelineColorBlendState colorBlendState, Electron2D.RenderingDevice.PipelineDynamicStateFlags dynamicStateFlags = 0, Electron2D.RDPipelineDepthStencilState depthStencilState = null)`](#member-22b7a5a537d5) | Creates an executable single-sample color and depth/stencil graphics pipeline. |
| `public System.Boolean RenderPipelineIsValid(Electron2D.RID renderPipeline)` | Reports whether a graphics pipeline and its borrowed shader are alive. |
| `public System.UInt64 ShaderGetVertexInputAttributeMask(Electron2D.RID shader)` | Returns the mask of vertex input locations declared by a graphics shader. |
| `public Electron2D.RID VertexArrayCreate(System.UInt32 vertexCount, System.Int32 vertexFormat, System.ReadOnlySpan<Electron2D.RID> sourceBuffers, System.ReadOnlySpan<System.UInt32> offsets = default)` | Creates a borrowed vertex-buffer array. |
| `public Electron2D.RID VertexBufferCreate(System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data = default, Electron2D.RenderingDevice.BufferCreationBits creationBits = 0)` | Creates a zero-filled vertex buffer with a copied initial prefix. |
| `public System.Int32 VertexFormatCreate(System.ReadOnlySpan<Electron2D.RDVertexAttribute> vertexDescriptions)` | Creates a copied, validated vertex layout. |

## Graphics member descriptions

<a id="member-c6018364e892"></a>
### DrawListBegin(Electron2D.RID, Electron2D.RenderingDevice.DrawFlags, System.ReadOnlySpan<Electron2D.Color>, Electron2D.Rect2, System.Single, System.Int32)

```csharp
public System.Int64 DrawListBegin(Electron2D.RID framebuffer, Electron2D.RenderingDevice.DrawFlags drawFlags = DefaultAll, System.ReadOnlySpan<Electron2D.Color> clearColorValues = default, Electron2D.Rect2 region = default, System.Single clearDepthValue = 1f, System.Int32 clearStencilValue = 0)
```

Begins an ordered color and depth/stencil render pass on borrowed framebuffer images.

Clears affect the complete attachment. The region restricts draws. Results become available to subsequent compute, copy and canvas passes after DrawListEnd.

The active owner-thread draw-list identity.

| Parameter | Contract |
| --- | --- |
| `framebuffer` | A live single-sample framebuffer. |
| `drawFlags` | Per-target clear or discard load flags; default loads existing pixels. |
| `clearColorValues` | One clear value per attachment when any clear flag is set; finite float components or raw integer lane bits as in TextureClear. |
| `region` | Viewport and initial scissor; zero selects the whole framebuffer. |
| `clearDepthValue` | Finite depth clear from zero through one; defaults to one. |
| `clearStencilValue` | Eight-bit stencil clear; defaults to zero. |
<a id="member-88d7ab8e682d"></a>
### DrawListBindIndexArray(System.Int64, Electron2D.RID)

```csharp
public System.Void DrawListBindIndexArray(System.Int64 drawList, Electron2D.RID indexArray)
```

Binds a live borrowed index range for subsequent indexed draws.

`drawList`: The active draw-list identity.

`indexArray`: A live borrowed range with its source buffer intact.

`System.ArgumentException`: A list, resource identity, descriptor index or value is invalid.

`System.InvalidOperationException`: The creating thread, selected pipeline or resource dependencies are invalid.


<a id="member-e7ccb29b43a5"></a>
### DrawListBindRenderPipeline(System.Int64, Electron2D.RID)

```csharp
public System.Void DrawListBindRenderPipeline(System.Int64 drawList, Electron2D.RID renderPipeline)
```

Binds a graphics pipeline matching the active framebuffer.

`drawList`: The active draw-list identity.

`renderPipeline`: A live matching pipeline of this device.

`System.ArgumentException`: A list, resource identity, descriptor index or value is invalid.

`System.InvalidOperationException`: The creating thread, selected pipeline or resource dependencies are invalid.


<a id="member-2a8067379751"></a>
### DrawListBindUniformSet(System.Int64, Electron2D.RID, System.UInt32)

```csharp
public System.Void DrawListBindUniformSet(System.Int64 drawList, Electron2D.RID uniformSet, System.UInt32 setIndex)
```

Binds a complete resource set at its logical shader set index.

`drawList`: The active draw-list identity.

`uniformSet`: A complete live set belonging to the selected shader.

`setIndex`: The set logical descriptor index.

`System.ArgumentException`: A list, resource identity, descriptor index or value is invalid.

`System.InvalidOperationException`: The creating thread, selected pipeline or resource dependencies are invalid.


<a id="member-f8ef4caed14f"></a>
### DrawListBindVertexArray(System.Int64, Electron2D.RID)

```csharp
public System.Void DrawListBindVertexArray(System.Int64 drawList, Electron2D.RID vertexArray)
```

Binds a live borrowed vertex array for subsequent draws.

`drawList`: The active draw-list identity.

`vertexArray`: A live borrowed array with all source buffers intact.

`System.ArgumentException`: A list, resource identity, descriptor index or value is invalid.

`System.InvalidOperationException`: The creating thread, selected pipeline or resource dependencies are invalid.


<a id="member-7b4ca82180b7"></a>
### DrawListBindVertexBuffersFormat(System.Int64, System.Int32, System.UInt32, System.ReadOnlySpan<Electron2D.RID>, System.ReadOnlySpan<System.UInt32>)

```csharp
public System.Void DrawListBindVertexBuffersFormat(System.Int64 drawList, System.Int32 vertexFormat, System.UInt32 vertexCount, System.ReadOnlySpan<Electron2D.RID> vertexBuffers, System.ReadOnlySpan<System.UInt32> offsets = default)
```

Binds copied buffer identities and offsets directly for a vertex layout.

This convenience binding copies arrays. Use a prepared VertexArray for allocation-sensitive draws.

`drawList`: The active draw list.

`vertexFormat`: A device-local vertex layout.

`vertexCount`: Available vertex count.

`vertexBuffers`: One live buffer per layout binding.

`offsets`: Optional byte offsets copied before returning.


<a id="member-aea5a5fd17ba"></a>
### DrawListDisableScissor(System.Int64)

```csharp
public System.Void DrawListDisableScissor(System.Int64 drawList)
```

Disables custom scissoring by restoring the complete framebuffer.

`drawList`: The active draw-list identity.

`System.ArgumentException`: A list, resource identity, descriptor index or value is invalid.

`System.InvalidOperationException`: The creating thread, selected pipeline or resource dependencies are invalid.


<a id="member-3758c88cffcd"></a>
### DrawListDraw(System.Int64, System.Boolean, System.UInt32, System.UInt32)

```csharp
public System.Void DrawListDraw(System.Int64 drawList, System.Boolean useIndices, System.UInt32 instances, System.UInt32 proceduralVertexCount = 0)
```

Records a procedural, vertex-array or indexed draw with validated instance capacity.

Index values produced or changed by GPU work must refer to vertices in the bound array. Index contents are not synchronously read back.

`drawList`: The active draw list.

`useIndices`: Draw the bound index range.

`instances`: Positive instance count.

`proceduralVertexCount`: Positive count when drawing without a vertex layout; otherwise zero.


<a id="member-5599d5d805fd"></a>
### DrawListDrawIndirect(System.Int64, System.Boolean, Electron2D.RID, System.UInt32, System.UInt32, System.UInt32)

```csharp
public System.Void DrawListDrawIndirect(System.Int64 drawList, System.Boolean useIndices, Electron2D.RID buffer, System.UInt32 offset = 0, System.UInt32 drawCount = 1, System.UInt32 stride = 0)
```

Records GPU-directed draws from validated indirect command ranges.

Nonindexed commands contain four unsigned words: vertex count, instance count, first vertex and first instance. Indexed commands contain five words: index count, instance count, first index, signed vertex offset and first instance. The GPU producer must keep counts and offsets within bound vertex, instance and index capacities. Contents stay on the GPU. Keep first vertex/instance zero when shaders depend on portable built-in vertex/instance identifiers.

`drawList`: The active draw list.

`useIndices`: Use the bound index range.

`buffer`: A storage buffer created with DispatchIndirect usage.

`offset`: Four-byte-aligned first command byte offset.

`drawCount`: Command count; zero validates without drawing.

`stride`: Four-byte-aligned command spacing, or zero for tightly packed commands.


<a id="member-55eb3fcba774"></a>
### DrawListEnableScissor(System.Int64, Electron2D.Rect2)

```csharp
public System.Void DrawListEnableScissor(System.Int64 drawList, Electron2D.Rect2 rect = default)
```

Clips subsequent draws to the rectangle intersected with the framebuffer.

`drawList`: The active draw-list identity.

`rect`: A finite nonnegative rectangle, intersected with the framebuffer; zero clips all pixels.

`System.ArgumentException`: A list, resource identity, descriptor index or value is invalid.

`System.InvalidOperationException`: The creating thread, selected pipeline or resource dependencies are invalid.


<a id="member-637824d02788"></a>
### DrawListEnd()

```csharp
public System.Void DrawListEnd()
```

Ends the active draw list, preserving its results for subsequent device work.


<a id="member-fe1e70626f75"></a>
### DrawListSetBlendConstants(System.Int64, Electron2D.Color)

```csharp
public System.Void DrawListSetBlendConstants(System.Int64 drawList, Electron2D.Color color)
```

Sets the finite blend constant for subsequent draws.

`drawList`: The active draw-list identity.

`color`: Finite components used by enabled constant blend factors.

`System.ArgumentException`: A list, resource identity, descriptor index or value is invalid.

`System.InvalidOperationException`: The creating thread, selected pipeline or resource dependencies are invalid.


<a id="member-a56b363d17da"></a>
### FramebufferCreate(System.ReadOnlySpan<Electron2D.RID>, System.Int32)

```csharp
public Electron2D.RID FramebufferCreate(System.ReadOnlySpan<Electron2D.RID> textures, System.Int32 validateWithFormat = -1)
```

Creates a framebuffer borrowing device-owned color and depth/stencil images.

A framebuffer identity invalidated when an attachment is freed.

| Parameter | Contract |
| --- | --- |
| `textures` | Distinct ordinary single-layer, single-mip images; up to eight color targets and one depth/stencil target. |
| `validateWithFormat` | An existing matching format, or -1 to create its layout. |
<a id="member-d154729327fd"></a>
### FramebufferFormatCreate(System.ReadOnlySpan<Electron2D.RDAttachmentFormat>)

```csharp
public System.Int32 FramebufferFormatCreate(System.ReadOnlySpan<Electron2D.RDAttachmentFormat> attachments)
```

Creates a copied single-pass color and depth/stencil attachment layout.

A device-local framebuffer format identifier.

| Parameter | Contract |
| --- | --- |
| `attachments` | Up to eight single-sample color targets and at most one depth/stencil target, in any order; at least one target is required. |

- `T:System.NotSupportedException`: An attachment role or sample count is unavailable.
<a id="member-4807409af35f"></a>
### FramebufferFormatGetTextureSamples(System.Int32, System.UInt32)

```csharp
public Electron2D.RenderingDevice.TextureSamples FramebufferFormatGetTextureSamples(System.Int32 framebufferFormat, System.UInt32 renderPass = 0)
```

Reports the sample count of a framebuffer pass.

The format sample count.

`framebufferFormat`: A live single-pass format identifier.

`renderPass`: Zero for the supported single pass.


<a id="member-1d252c9c44a2"></a>
### FramebufferGetFormat(Electron2D.RID)

```csharp
public System.Int32 FramebufferGetFormat(Electron2D.RID framebuffer)
```

Reads the framebuffer's matching layout identity.

A device-local format identifier.

`framebuffer`: A live framebuffer with live borrowed attachments.


<a id="member-eeb8089a3f6a"></a>
### FramebufferIsValid(Electron2D.RID)

```csharp
public System.Boolean FramebufferIsValid(Electron2D.RID framebuffer)
```

Reports whether a framebuffer and all its borrowed attachments remain alive.

True for a live framebuffer with intact dependencies on this device.

`framebuffer`: The identity to inspect.


<a id="member-671070e0471a"></a>
### IndexArrayCreate(Electron2D.RID, System.UInt32, System.UInt32)

```csharp
public Electron2D.RID IndexArrayCreate(Electron2D.RID indexBuffer, System.UInt32 indexOffset, System.UInt32 indexCount)
```

Creates a borrowed index range.

A device-owned range identity invalidated by the source buffer.

`indexBuffer`: A live index buffer.

`indexOffset`: First index.

`indexCount`: Positive index count.


<a id="member-c8b5dc0ced55"></a>
### IndexBufferCreate(System.UInt32, Electron2D.RenderingDevice.IndexBufferFormat, System.ReadOnlySpan<System.Byte>, System.Boolean, Electron2D.RenderingDevice.BufferCreationBits)

```csharp
public Electron2D.RID IndexBufferCreate(System.UInt32 sizeIndices, Electron2D.RenderingDevice.IndexBufferFormat format, System.ReadOnlySpan<System.Byte> data = default, System.Boolean useRestartIndices = false, Electron2D.RenderingDevice.BufferCreationBits creationBits = 0)
```

Creates a copied unsigned index buffer.

A device-owned index buffer identity.

`sizeIndices`: Positive index capacity.

`format`: Unsigned 16 or 32-bit indices.

`data`: Initial byte prefix.

`useRestartIndices`: Enables maximum unsigned index values as strip restart markers.

`creationBits`: Optional storage role.


<a id="member-22b7a5a537d5"></a>
### RenderPipelineCreate(Electron2D.RID, System.Int32, System.Int32, Electron2D.RenderingDevice.RenderPrimitive, Electron2D.RDPipelineRasterizationState, Electron2D.RDPipelineColorBlendState, Electron2D.RenderingDevice.PipelineDynamicStateFlags, Electron2D.RDPipelineDepthStencilState)

```csharp
public Electron2D.RID RenderPipelineCreate(Electron2D.RID shader, System.Int32 framebufferFormat, System.Int32 vertexFormat, Electron2D.RenderingDevice.RenderPrimitive primitive, Electron2D.RDPipelineRasterizationState rasterizationState, Electron2D.RDPipelineColorBlendState colorBlendState, Electron2D.RenderingDevice.PipelineDynamicStateFlags dynamicStateFlags = 0, Electron2D.RDPipelineDepthStencilState depthStencilState = null)
```

Creates an executable single-sample color and depth/stencil graphics pipeline.

Depth-bounds tests, sample shading, subpasses and pipeline specialization require additional attachment and shader interfaces.

A pipeline borrowing the shader identity.

| Parameter | Contract |
| --- | --- |
| `shader` | A live paired vertex/fragment shader. |
| `framebufferFormat` | A color and optional depth/stencil layout on this device. |
| `vertexFormat` | A copied vertex layout, or -1 for procedural vertices. |
| `primitive` | Point, line or triangle topology. |
| `rasterizationState` | Copied rasterization configuration. |
| `colorBlendState` | Copied blending configuration, with one entry per target. |
| `dynamicStateFlags` | Allows dynamic blend constants; other dynamic states require their native adapters. |
| `depthStencilState` | Copied depth/stencil configuration; null disables both tests and depth writes. |

- `T:System.ArgumentException`: Shader interfaces, layouts or configurations disagree.
<a id="member-3196828dde59"></a>
### RenderPipelineIsValid(Electron2D.RID)

```csharp
public System.Boolean RenderPipelineIsValid(Electron2D.RID renderPipeline)
```

Reports whether a graphics pipeline and its borrowed shader are alive.

True for an intact executable pipeline of this device.

`renderPipeline`: The identity to inspect.


<a id="member-3172a816f389"></a>
### ShaderGetVertexInputAttributeMask(Electron2D.RID)

```csharp
public System.UInt64 ShaderGetVertexInputAttributeMask(Electron2D.RID shader)
```

Returns the mask of vertex input locations declared by a graphics shader.

A bit per declared location; procedural shaders return zero.

`shader`: A live paired graphics shader.


<a id="member-0b72eb527f6d"></a>
### VertexArrayCreate(System.UInt32, System.Int32, System.ReadOnlySpan<Electron2D.RID>, System.ReadOnlySpan<System.UInt32>)

```csharp
public Electron2D.RID VertexArrayCreate(System.UInt32 vertexCount, System.Int32 vertexFormat, System.ReadOnlySpan<Electron2D.RID> sourceBuffers, System.ReadOnlySpan<System.UInt32> offsets = default)
```

Creates a borrowed vertex-buffer array.

A device-owned array identity invalidated by its source buffers.

`vertexCount`: Number of available vertices.

`vertexFormat`: A live vertex layout.

`sourceBuffers`: One device-owned vertex buffer per layout binding.

`offsets`: Optional byte offset per buffer.


<a id="member-87c976b5fe22"></a>
### VertexBufferCreate(System.UInt32, System.ReadOnlySpan<System.Byte>, Electron2D.RenderingDevice.BufferCreationBits)

```csharp
public Electron2D.RID VertexBufferCreate(System.UInt32 sizeBytes, System.ReadOnlySpan<System.Byte> data = default, Electron2D.RenderingDevice.BufferCreationBits creationBits = 0)
```

Creates a zero-filled vertex buffer with a copied initial prefix.

A device-owned buffer identity.

`sizeBytes`: Positive byte capacity.

`data`: Initial byte prefix.

`creationBits`: Optional storage role.


<a id="member-56a2ae97c2fa"></a>
### VertexFormatCreate(System.ReadOnlySpan<Electron2D.RDVertexAttribute>)

```csharp
public System.Int32 VertexFormatCreate(System.ReadOnlySpan<Electron2D.RDVertexAttribute> vertexDescriptions)
```

Creates a copied, validated vertex layout.

A device-local format identifier.

`vertexDescriptions`: Locations, buffer bindings, strides and formats.

`System.ArgumentException`: Bindings, locations or ranges are inconsistent.

`System.NotSupportedException`: An encoding is not supported.
