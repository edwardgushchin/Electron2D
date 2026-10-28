# RenderingDevice

[Native HDR output](../components/native-hdr-output.md) selects a linear half-float
screen stage while active. ScreenGetFramebufferFormat identifies that engine-owned
stage rather than the private final HDR10/extended-linear swapchain. Pending screen
contents retain their recorded encoding across output changes and transfer on GPU;
new screen lists use the current format. Existing owner/list/window lifetime guards
and framebuffer-format identities remain in effect.

Last updated: 2026-10-28

Compute storage-array lengths use each actual bound byte range, including
different bindings across dispatches in one submission. The internal
[ComputePipeline](ComputePipeline.md) supplies Metal's size table; the logical
eight-storage/four-uniform interface and exported portable modules retain their
existing roles. See [Metal storage-buffer lengths](../components/local-compute.md#metal-storage-buffer-lengths)
for ownership and verification limits.

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RenderingDevice`. **Source:** [RenderingDevice.cs](../../src/Servers/Rendering/Device/RenderingDevice.cs), [RenderingDevice.Binary.cs](../../src/Servers/Rendering/Device/RenderingDevice.Binary.cs), [RenderingDevice.Readback.cs](../../src/Servers/Rendering/Device/RenderingDevice.Readback.cs), [RenderingDevice.TexelBuffers.cs](../../src/Servers/Rendering/Device/RenderingDevice.TexelBuffers.cs), [RenderingDevice.Screens.cs](../../src/Servers/Rendering/Device/RenderingDevice.Screens.cs). **Component:** [Local compute](../components/local-compute.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

An owner-thread graphics and compute device created locally by RenderingServer.CreateLocalRenderingDevice or retained by the active GPU renderer through RenderingServer.GetRenderingDevice. The retained renderer device flushes after canvas authoring and before drawing, and completes queued reads before FramePostDraw; Submit, Sync and public disposal are local-only. Renderer-owned image RIDs bind directly to TextureRD/TextureArrayRD canvas resources. RenderingServer.TextureGetRDTexture exports actual renderer-managed ordinary uploads, independent arrays and completed viewport images to the retained device without CPU copying. Compatible authored updates keep the RID; replacing allocation, disposing the source, rebinding the viewport or retiring its target expires it. A viewport RID resolves the completed ping-pong image when each command is recorded. FreeRID releases only the borrowed wrapper; queued native operations retain the old allocation through completion. Device writes change GPU storage, while authored CPU snapshots remain unchanged; the next authored update overwrites device changes. Sampling, raw update/copy/read and supported storage roles reflect actual native allocation flags. Prepare changed uploads before starting a draw list. Empty storage, unrendered targets and the compatibility renderer return an empty RID. Distinct sRGB views still require native format-view ownership. It supports native compute independently of the canvas rendering method. Each device owns its buffer/image/sampler/shader/pipeline/set RIDs (renderer exports own only wrappers); cross-device and freed identities reject. An active list is exclusive. End lists before submission, reads, frees or disposal; Submit starts recorded work, Sync waits, and BufferGetData synchronizes pending writes before returning. Disposal cancels unsubmitted commands and waits for submitted work. Buffer range/type, descriptor layout and thread/lifetime errors throw typed exceptions.

Layered framebuffers execute ordered native passes for each view, with vertex/fragment view indices, copied draw state, per-layer depth/resolve/input operations and untouched extra layers. See [layered multiview](../components/local-compute.md#layered-multiview) for ownership and verification limits.

Readonly graphics storage images retain their Image role and current GPU contents across draws. Sampling/Storage combinations use a private GPU snapshot; see [image coherence](../components/local-compute.md#readonly-graphics-image-coherence).

Independent Texture and Sampler sets execute native compute and graphics through reusable pair bindings, including plain/array and signed/unsigned images, comparison sampling, fetch/query, helpers and control flow. Combined declarations retain sampler-first IDs. See [independent sampled bindings](../components/local-compute.md#independent-sampled-resource-bindings) for validation timing, native capacity and verification.

Fixed descriptor arrays execute across image, sampler, texel and input roles with ordered copied IDs and scalar native capacity; see [device arrays](../components/local-compute.md#fixed-device-descriptor-arrays).

Formatted texel buffers execute indexed compute/graphics reads, compute stores and 32-bit integer atomics while preserving raw byte transfer/readback. Their factory counts elements; see [formatted texel buffers](../components/local-compute.md#formatted-texel-buffers).

Shader binaries and reserved shader RIDs use the same validated runtime program path; see [container ownership and loading](../components/local-compute.md#shader-binaries-and-deferred-identities).

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
| [`public System.Void ComputeListSetPushConstant(System.Int64 computeList, System.ReadOnlySpan<System.Byte> buffer, System.UInt32 sizeBytes)`](#member-330889262253) | Copies binary push data for subsequent dispatches in the selected compute pipeline. |
| `public System.Void ComputeListEnd()` | Ends the active list without submitting it. |
| [`public Electron2D.RID ComputePipelineCreate(Electron2D.RID shader, System.ReadOnlySpan<Electron2D.RDPipelineSpecializationConstant> specializationConstants = default)`](#member-d98c9dd7346a) | Creates an executable compute pipeline with copied typed specialization values. |
| `public System.Boolean ComputePipelineIsValid(Electron2D.RID pipeline)` | Reports whether a pipeline and its shader remain alive. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Overrides the inherited typed lifetime, property-metadata or Resource-copy hook. |
| `public System.Void FreeRID(Electron2D.RID rid)` | Frees a resource after completing pending work. |
| `public System.String GetDeviceName()` | Returns the selected compute-device name. |
| [`public System.Byte[] ShaderCompileBinaryFromSPIRV(Electron2D.RDShaderSPIRV spirvData, System.String name = "")`](#member-shader-binary-compile) | Validates and packages copied stages and a strict UTF-8 diagnostic name. |
| [`public Electron2D.RID ShaderCreateFromBytecode(System.ReadOnlySpan<System.Byte> binaryData, Electron2D.RID placeholderRID = default)`](#member-shader-binary-load) | Loads a complete container and optionally initializes one reserved shader identity. |
| [`public Electron2D.RID ShaderCreatePlaceholder()`](#member-shader-binary-placeholder) | Reserves an owned identity for later bytecode loading. |
| `public Electron2D.RID ShaderCreateFromSPIRV(Electron2D.RDShaderSPIRV spirvData, System.String name = "")` | Validates and creates a compute shader or a paired graphics shader from compiled bytecode. |
| `public Electron2D.RID TextureBufferCreate(System.UInt32 sizeElements, Electron2D.RenderingDevice.DataFormat format, System.ReadOnlySpan<System.Byte> data = default)` | Creates an owned formatted buffer; size counts elements and nonempty data covers its complete encoded byte capacity. |
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
| [`public Electron2D.RDShaderSPIRV ShaderCompileSPIRVFromSource(Electron2D.RDShaderSource shaderSource, System.Boolean allowCache = true)`](#member-c396c1ad031e) | Compiles copied stage sources through the separately installed authoring tool. |

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

<a id="member-d98c9dd7346a"></a>
### ComputePipelineCreate(Electron2D.RID, System.ReadOnlySpan<Electron2D.RDPipelineSpecializationConstant>)

```csharp
public Electron2D.RID ComputePipelineCreate(Electron2D.RID shader, System.ReadOnlySpan<Electron2D.RDPipelineSpecializationConstant> specializationConstants = default)
```

Creates an executable compute pipeline with copied typed specialization values.

A pipeline borrowing the shader identity and owning any specialized native program.

| Parameter | Contract |
| --- | --- |
| `shader` | A live compute shader on this device. |
| `specializationConstants` | Distinct declared scalar identifiers and typed values; omitted values keep module defaults. |

- `T:System.ArgumentException`: An identifier/type is invalid or the resulting module/layout is invalid.
- `T:System.NotSupportedException`: A specialized shader capability exceeds the native profile.
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

<a id="member-e4c6793fb7d5"></a>
### TextureBufferCreate(System.UInt32, Electron2D.RenderingDevice.DataFormat, System.ReadOnlySpan<System.Byte>)

```csharp
public Electron2D.RID TextureBufferCreate(System.UInt32 sizeElements, Electron2D.RenderingDevice.DataFormat format, System.ReadOnlySpan<System.Byte> data = default)
```

Creates a device-owned formatted GPU buffer. `sizeElements` is a positive element count; byte capacity is count times the format's encoded element size. Empty `data` clears every encoded byte; nonempty data must cover the exact complete capacity and is copied before returning. Unsupported compressed, depth, planar and wider-than-32-bit component encodings throw `NotSupportedException`; invalid size/format values throw `ArgumentOutOfRangeException`, and incomplete initial data throws `ArgumentException`. End any list before creation and use the owner thread.

TextureBuffer and ImageBuffer borrow this RID. SamplerWithTextureBuffer borrows sampler first, then this buffer; indexed fetches ignore filtering. Compute stores and supported signed/unsigned R32 image atomics change the original encoded bytes. Ordinary update/copy/clear and synchronous/queued readback use byte offsets in that data, excluding private format metadata. FreeRID invalidates dependent sets and retires owned native storage; identities never cross devices. Graphics uses readonly declarations. See the component for native capacity, format conversion, allocation checks and remaining runtime/nested-descriptor and precision gates.

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

- `T:System.NotSupportedException`: Border and unnormalized sampling require the pinned Vulkan adapter; mirror-clamp requires its native capability.
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
| `view` | Copied initial sampled component mapping; alternate pixel formats require a shared native format view. |
| `data` | One tightly packed color mip chain per layer, or empty for zero initialization. Depth/stencil images require empty data and initialize every layer/mip to depth zero and stencil zero. Multisample targets require empty data and initialize every sample through native clears. |

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

The [graphics component contract](../components/local-compute.md#color-graphics-and-compute-integration) defines copied descriptions, borrowed resource identities, color/depth passes with exact multisample state and optional resolve destinations, shader interface matching, stage resources, direct/indexed/instanced/procedural/indirect work, and retained canvas consumption. Compute and draw lists share one owner-thread timeline; only one list is active.

| Declaration | Contract |
| --- | --- |
| [`public System.Int64 DrawListBegin(Electron2D.RID framebuffer, Electron2D.RenderingDevice.DrawFlags drawFlags = DefaultAll, System.ReadOnlySpan<Electron2D.Color> clearColorValues = default, Electron2D.Rect2 region = default, System.Single clearDepthValue = 1f, System.Int32 clearStencilValue = 0)`](#member-c6018364e892) | Begins an ordered framebuffer draw list at its first pass. |
| `public System.Void DrawListBindIndexArray(System.Int64 drawList, Electron2D.RID indexArray)` | Binds a live borrowed index range for subsequent indexed draws. |
| `public System.Void DrawListBindRenderPipeline(System.Int64 drawList, Electron2D.RID renderPipeline)` | Binds a graphics pipeline matching the active framebuffer. |
| `public System.Void DrawListBindUniformSet(System.Int64 drawList, Electron2D.RID uniformSet, System.UInt32 setIndex)` | Binds a complete resource set at its logical shader set index. |
| `public System.Void DrawListBindVertexArray(System.Int64 drawList, Electron2D.RID vertexArray)` | Binds a live borrowed vertex array for subsequent draws. |
| `public System.Void DrawListBindVertexBuffersFormat(System.Int64 drawList, System.Int32 vertexFormat, System.UInt32 vertexCount, System.ReadOnlySpan<Electron2D.RID> vertexBuffers, System.ReadOnlySpan<System.UInt32> offsets = default)` | Binds copied buffer identities and offsets directly for a vertex layout. |
| `public System.Void DrawListDisableScissor(System.Int64 drawList)` | Disables custom scissoring by restoring the complete framebuffer. |
| `public System.Void DrawListDraw(System.Int64 drawList, System.Boolean useIndices, System.UInt32 instances, System.UInt32 proceduralVertexCount = 0)` | Records a procedural, vertex-array or indexed draw with validated instance capacity. |
| `public System.Void DrawListDrawIndirect(System.Int64 drawList, System.Boolean useIndices, Electron2D.RID buffer, System.UInt32 offset = 0, System.UInt32 drawCount = 1, System.UInt32 stride = 0)` | Records GPU-directed draws from validated indirect command ranges. |
| `public System.Void DrawListEnableScissor(System.Int64 drawList, Electron2D.Rect2 rect = default)` | Clips subsequent draws to the rectangle intersected with the framebuffer. |
| [`public System.Void DrawListSetPushConstant(System.Int64 drawList, System.ReadOnlySpan<System.Byte> buffer, System.UInt32 sizeBytes)`](#member-d36881d1257d) | Copies shared binary push data for subsequent vertex and fragment work in the selected graphics pipeline. |
| [`public System.Int64 DrawListSwitchToNextPass()`](#member-switch-next-pass) | Advances the active framebuffer to its next ordered pass and resets draw bindings. |
| `public System.Void DrawListEnd()` | Ends the active draw list, preserving its results for subsequent device work. |
| `public System.Void DrawListSetBlendConstants(System.Int64 drawList, Electron2D.Color color)` | Sets the finite blend constant for subsequent draws. |
| [`public Electron2D.RID FramebufferCreate(System.ReadOnlySpan<Electron2D.RID> textures, System.Int32 validateWithFormat = -1, System.Int32 viewCount = 1)`](#member-b1f57fe724ef) | Creates a framebuffer borrowing device-owned color and depth/stencil images. |
| [`public Electron2D.RID FramebufferCreateMultipass(System.ReadOnlySpan<Electron2D.RID> textures, System.ReadOnlySpan<Electron2D.RDFramebufferPass> passes, System.Int32 validateWithFormat = -1, System.Int32 viewCount = 1)`](#member-69eb0fea8573) | Creates a framebuffer borrowing the images selected by ordered copied passes. |
| [`public System.Int32 FramebufferFormatCreateMultipass(System.ReadOnlySpan<Electron2D.RDAttachmentFormat> attachments, System.ReadOnlySpan<Electron2D.RDFramebufferPass> passes, System.Int32 viewCount = 1)`](#member-4b3991d9d1c4) | Creates a copied explicit pass layout, including per-color resolve destinations. |
| [`public System.Void TextureResolveMultisample(Electron2D.RID fromTexture, Electron2D.RID toTexture)`](#member-9949b16cc93d) | Resolves a complete multisample color image into its marked single-sample destination on the GPU timeline. |
| [`public System.Int32 FramebufferFormatCreate(System.ReadOnlySpan<Electron2D.RDAttachmentFormat> attachments, System.Int32 viewCount = 1)`](#member-8709272c4e6f) | Creates a copied single-pass color and depth/stencil attachment layout. |
| [`public Electron2D.RenderingDevice.TextureSamples FramebufferFormatGetTextureSamples(System.Int32 framebufferFormat, System.UInt32 renderPass = 0)`](#member-4807409af35f) | Reports the exact raster sample count of a framebuffer pass. |
| `public System.Int32 FramebufferGetFormat(Electron2D.RID framebuffer)` | Reads the framebuffer's matching layout identity. |
| `public System.Boolean FramebufferIsValid(Electron2D.RID framebuffer)` | Reports whether a framebuffer and all its borrowed attachments remain alive. |
| `public Electron2D.RID IndexArrayCreate(Electron2D.RID indexBuffer, System.UInt32 indexOffset, System.UInt32 indexCount)` | Creates a borrowed index range. |
| `public Electron2D.RID IndexBufferCreate(System.UInt32 sizeIndices, Electron2D.RenderingDevice.IndexBufferFormat format, System.ReadOnlySpan<System.Byte> data = default, System.Boolean useRestartIndices = false, Electron2D.RenderingDevice.BufferCreationBits creationBits = 0)` | Creates a copied unsigned index buffer. |
| [`public Electron2D.RID RenderPipelineCreate(Electron2D.RID shader, System.Int32 framebufferFormat, System.Int32 vertexFormat, Electron2D.RenderingDevice.RenderPrimitive primitive, Electron2D.RDPipelineRasterizationState rasterizationState, Electron2D.RDPipelineColorBlendState colorBlendState, Electron2D.RenderingDevice.PipelineDynamicStateFlags dynamicStateFlags = 0, Electron2D.RDPipelineDepthStencilState depthStencilState = null, Electron2D.RDPipelineMultisampleState multisampleState = null, System.ReadOnlySpan<Electron2D.RDPipelineSpecializationConstant> specializationConstants = default, System.UInt32 forRenderPass = 0)`](#member-997ab2bef274) | Creates an executable color and depth/stencil graphics pipeline with copied multisample state. |
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

Begins an ordered framebuffer draw list at its first pass.

Clears affect the complete attachment. The region restricts draws. Results become available to subsequent compute, copy and canvas passes after DrawListEnd.

The active owner-thread draw-list identity.

| Parameter | Contract |
| --- | --- |
| `framebuffer` | A live framebuffer with matching raster sample counts and optional resolve destinations. |
| `drawFlags` | Per-target clear or discard load flags; default loads existing pixels. |
| `clearColorValues` | Clear values index every color-attachment image, including resolve images, in framebuffer order; finite float components or raw integer lane bits as in TextureClear. |
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


<a id="member-b1f57fe724ef"></a>
### FramebufferCreate(System.ReadOnlySpan<Electron2D.RID>, System.Int32, System.Int32)

```csharp
public Electron2D.RID FramebufferCreate(System.ReadOnlySpan<Electron2D.RID> textures, System.Int32 validateWithFormat = -1, System.Int32 viewCount = 1)
```

Creates a framebuffer borrowing device-owned color and depth/stencil images.

A framebuffer invalidated when any borrowed image is freed.

| Parameter | Contract |
| --- | --- |
| `textures` | Distinct single-mip images with matching dimensions and enough layers for every view; empty RID slots remain unused by every pass. |
| `validateWithFormat` | An existing matching layout including resolve roles, or -1 for an implicit render-only pass. |
| `viewCount` | One through 256 ordered views, each targeting its corresponding attachment layer; an explicit format must agree. |
<a id="member-8709272c4e6f"></a>
### FramebufferFormatCreate(System.ReadOnlySpan<Electron2D.RDAttachmentFormat>, System.Int32)

```csharp
public System.Int32 FramebufferFormatCreate(System.ReadOnlySpan<Electron2D.RDAttachmentFormat> attachments, System.Int32 viewCount = 1)
```

Creates a copied single-pass color and depth/stencil attachment layout.

A device-local framebuffer format identifier.

| Parameter | Contract |
| --- | --- |
| `attachments` | Up to eight render color targets and one depth/stencil target with the same exact sample count. |
| `viewCount` | One through 256 ordered views, each targeting its corresponding attachment layer; an explicit format must agree. |

- `T:System.NotSupportedException`: A format or exact sample count is unavailable.
<a id="member-4807409af35f"></a>
### FramebufferFormatGetTextureSamples(System.Int32, System.UInt32)

```csharp
public Electron2D.RenderingDevice.TextureSamples FramebufferFormatGetTextureSamples(System.Int32 framebufferFormat, System.UInt32 renderPass = 0)
```

Reports the exact raster sample count of a framebuffer pass.

The copied raster sample count.

| Parameter | Contract |
| --- | --- |
| `framebufferFormat` | A live copied framebuffer format identifier. |
| `renderPass` | The zero-based copied pass index. |
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


<a id="member-997ab2bef274"></a>
### RenderPipelineCreate(Electron2D.RID, System.Int32, System.Int32, Electron2D.RenderingDevice.RenderPrimitive, Electron2D.RDPipelineRasterizationState, Electron2D.RDPipelineColorBlendState, Electron2D.RenderingDevice.PipelineDynamicStateFlags, Electron2D.RDPipelineDepthStencilState, Electron2D.RDPipelineMultisampleState, System.ReadOnlySpan<Electron2D.RDPipelineSpecializationConstant>, System.UInt32)

```csharp
public Electron2D.RID RenderPipelineCreate(Electron2D.RID shader, System.Int32 framebufferFormat, System.Int32 vertexFormat, Electron2D.RenderingDevice.RenderPrimitive primitive, Electron2D.RDPipelineRasterizationState rasterizationState, Electron2D.RDPipelineColorBlendState colorBlendState, Electron2D.RenderingDevice.PipelineDynamicStateFlags dynamicStateFlags = 0, Electron2D.RDPipelineDepthStencilState depthStencilState = null, Electron2D.RDPipelineMultisampleState multisampleState = null, System.ReadOnlySpan<Electron2D.RDPipelineSpecializationConstant> specializationConstants = default, System.UInt32 forRenderPass = 0)
```

Creates an executable color and depth/stencil graphics pipeline with copied multisample state.

Depth-bounds tests and sample shading require their native state adapters. Ordered passes select forRenderPass. Typed specialization freezes constants before native creation.

A pipeline borrowing the shader identity and owning specialized native stages when needed.

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
| `multisampleState` | Copied sample count and alpha-to-coverage state; null selects one sample. |
| `specializationConstants` | Copied scalar overrides shared by the paired stages; omitted values retain defaults. |
| `forRenderPass` | Zero-based pass index in the copied framebuffer format; validated again against the active draw pass. |

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

<a id="member-69eb0fea8573"></a>
### FramebufferCreateMultipass(System.ReadOnlySpan<Electron2D.RID>, System.ReadOnlySpan<Electron2D.RDFramebufferPass>, System.Int32, System.Int32)

```csharp
public Electron2D.RID FramebufferCreateMultipass(System.ReadOnlySpan<Electron2D.RID> textures, System.ReadOnlySpan<Electron2D.RDFramebufferPass> passes, System.Int32 validateWithFormat = -1, System.Int32 viewCount = 1)
```

Creates a framebuffer borrowing the images selected by ordered copied passes.

A framebuffer borrowing all images used by its ordered passes.

| Parameter | Contract |
| --- | --- |
| `textures` | Render, marked resolve and input images in caller-defined attachment order; empty RID slots remain unused by every pass. |
| `passes` | One or more ordered color/depth/resolve/input/preservation passes. |
| `validateWithFormat` | An existing matching layout, or -1 to create its copied format. |
| `viewCount` | One through 256 ordered views, each targeting its corresponding attachment layer; an explicit format must agree. |

- `T:System.ArgumentException`: Images, sample counts, roles or requested layout disagree.
- `T:System.NotSupportedException`: An attachment role requires its native adapter.


<a id="member-4b3991d9d1c4"></a>
### FramebufferFormatCreateMultipass(System.ReadOnlySpan<Electron2D.RDAttachmentFormat>, System.ReadOnlySpan<Electron2D.RDFramebufferPass>, System.Int32)

```csharp
public System.Int32 FramebufferFormatCreateMultipass(System.ReadOnlySpan<Electron2D.RDAttachmentFormat> attachments, System.ReadOnlySpan<Electron2D.RDFramebufferPass> passes, System.Int32 viewCount = 1)
```

Creates a copied explicit pass layout, including per-color resolve destinations.

A device-local layout retaining copied indices and formats.

| Parameter | Contract |
| --- | --- |
| `attachments` | Render and resolve attachment formats in caller-defined order. |
| `passes` | One or more ordered copied executable passes. |
| `viewCount` | One through 256 ordered views, each targeting its corresponding attachment layer; an explicit format must agree. |

- `T:System.ArgumentException`: Indices, formats, roles or sample counts disagree.
- `T:System.NotSupportedException`: An exact sample count or attachment role is unavailable.


<a id="member-9949b16cc93d"></a>
### TextureResolveMultisample(Electron2D.RID, Electron2D.RID)

```csharp
public System.Void TextureResolveMultisample(Electron2D.RID fromTexture, Electron2D.RID toTexture)
```

Resolves a complete multisample color image into its marked single-sample destination on the GPU timeline.

Preserves the source samples for later passes or resolves. Results are available to subsequent sampling, copies and readback. Depth resolve requires a native aspect adapter.

| Parameter | Contract |
| --- | --- |
| `fromTexture` | An ordinary multisample color target with CanCopyFrom usage. |
| `toTexture` | A matching marked resolve color target with CanCopyTo usage. |

- `T:System.ArgumentException`: Dimensions, formats, roles or sample counts disagree.
- `T:System.InvalidOperationException`: A transfer role, owner thread or device state is invalid.

<a id="member-330889262253"></a>
### ComputeListSetPushConstant(System.Int64, System.ReadOnlySpan<System.Byte>, System.UInt32)

```csharp
public System.Void ComputeListSetPushConstant(System.Int64 computeList, System.ReadOnlySpan<System.Byte> buffer, System.UInt32 sizeBytes)
```

Copies binary push data for subsequent dispatches in the selected compute pipeline.

The original SPIR-V member offsets and array/matrix strides determine the byte layout. Native uniform translation preserves those bytes. Blocks are limited to 128 bytes and 32-bit scalar data or enabled native FP64/Int64 data. Bind a pipeline before setting data; changing pipeline or beginning another list requires another set. Recorded dispatches retain their own snapshot.

| Parameter | Contract |
| --- | --- |
| `computeList` | The active owner-thread compute list. |
| `buffer` | At least sizeBytes source bytes; the selected prefix is copied before returning. |
| `sizeBytes` | Exact size of the shader's reflected push block, or zero for a shader without one. |

- `T:System.ArgumentException`: The list, byte count or source capacity is invalid.
- `T:System.InvalidOperationException`: No pipeline is selected or the creating thread/device state is invalid.


<a id="member-d36881d1257d"></a>
### DrawListSetPushConstant(System.Int64, System.ReadOnlySpan<System.Byte>, System.UInt32)

```csharp
public System.Void DrawListSetPushConstant(System.Int64 drawList, System.ReadOnlySpan<System.Byte> buffer, System.UInt32 sizeBytes)
```

Copies shared binary push data for subsequent vertex and fragment work in the selected graphics pipeline.

Both stages consume the same source bytes through their own original SPIR-V layouts, within the 128-byte portable limit. Data remains valid until pipeline changes or the list ends; previously recorded draws retain their own snapshot.

| Parameter | Contract |
| --- | --- |
| `drawList` | The active owner-thread draw list. |
| `buffer` | At least sizeBytes source bytes; the selected prefix is copied before returning. |
| `sizeBytes` | Maximum reflected push-block extent of the paired stages, or zero when neither has a block. |

- `T:System.ArgumentException`: The list, byte count or source capacity is invalid.
- `T:System.InvalidOperationException`: No pipeline is selected or the creating thread/device state is invalid.


## Binary push recording

[Binary push parameters](../components/local-compute.md#binary-push-parameters) preserve original 32-bit and enabled Half/Int8/UInt8/Int16/UInt16/FP64/Int64 compact layouts through a private uniform block, including arrays, matrix orientation and nested structs. Select a pipeline, copy its exact reflected block size (at most 128 bytes), then issue draw/dispatch work. Pipeline changes and new lists require another setter. Source span mutation and later setters cannot replace prior recorded snapshots. Push storage uses one of four native uniform slots per used stage; logical RDUniform bindings stay independent. Narrow-integer precision, 64-bit image atomics and pointer address/arithmetic require their native shader adapters. [Native FP64](../components/local-compute.md#native-fp64-arithmetic-and-data) supplies double arithmetic, raw data and typed specialization when the device feature is enabled. See [the executable consumer](../../tests/Electron2D.Tests/RenderingDevicePushTests.cs) for paired language data, real pixels and verified allocation limits.

## Pipeline specialization

[Typed scalar descriptions](RDPipelineSpecializationConstant.Generic.md) select independent executable compute/graphics variants while preserving the source shader identity and public uniform sets. Values resolve before native workgroup/layout validation and push translation; pipelines copy descriptions and own private native programs. See [the complete specialization contract](../components/local-compute.md#typed-pipeline-specialization) for defaults, identifier/type checks, specialized array sizes, lifetime and native verification.

<a id="member-c396c1ad031e"></a>
### ShaderCompileSPIRVFromSource(Electron2D.RDShaderSource, System.Boolean)

```csharp
public Electron2D.RDShaderSPIRV ShaderCompileSPIRVFromSource(Electron2D.RDShaderSource shaderSource, System.Boolean allowCache = true)
```

Compiles copied stage sources through the separately installed authoring tool.

This owner-thread cold authoring operation creates no GPU program and does not submit or complete pending work. Configure ELECTRON2D_SHADER_IMPORT_TOOL with the published ShaderImport executable, or install it beside the host. Relative includes resolve from the current project directory. Invoking the executable requires process-launch support on the host. Games loading compiled modules require no source compiler.

A new resource with independent stage bytecode and compiler diagnostics. Failed stages remain empty; other stages are still attempted.

| Parameter | Contract |
| --- | --- |
| `shaderSource` | Non-null vertex, fragment and compute sources with one language. |
| `allowCache` | Permits bounded successful-module caching; false bypasses both lookup and storage. Sources with preprocessing dependencies are compiled every time. |

- `T:System.NotSupportedException`: The separate import executable is unavailable.
- `T:System.ArgumentException`: The language/source payload is invalid.


<a id="source-authoring"></a>
## Source authoring

[RDShaderSource](RDShaderSource.md) and [ShaderLanguage](RenderingDevice.ShaderLanguage.md) feed the separately installed ShaderImport tool through explicit cold compilation. Returned bytecode/diagnostics are copied per stage, and ordinary device shader/pipeline creation remains responsible for executable interfaces and native validation. The [authoring contract](../components/local-compute.md#source-authoring-and-build-imports) defines cache bypass, dependency handling, output/deadline bounds and verified native behavior.

Command recording, queued readbacks, resource retirement and device disposal conservatively invalidate the active canvas service. This also covers caller-owned device work that can feed retained texture consumers. Existing device ownership, submission and canvas compatibility guards remain in force. See [low-processor rendering](../components/low-processor-rendering.md).

## Renderer-owned instance storage

RenderingServer's MultiMesh buffer getters return actual global-device storage with ordinary buffer upload/read/copy/compute bindings. Live buffers retain resource ownership, so FreeRID rejects them until expiration. Uniform sets borrow their lifetime; local devices reject foreign identities. Native writes retain authored CPU snapshots and are consumed by the canvas conversion/indirect path without host readback. Private previous buffers retire after pending device work completes. See [instance layout, submission order and checks](../components/meshes.md#writable-instance-device-buffers).

<a id="member-switch-next-pass"></a>
### DrawListSwitchToNextPass()

```csharp
public System.Int64 DrawListSwitchToNextPass()
```

Advances the active framebuffer to its next ordered pass, returning the retained list identity. Pipeline, resource, vertex/index and push bindings reset; viewport and scissor return to the original region. No active list or advancing past the final pass throws InvalidOperationException. See [ordered framebuffer passes](../components/local-compute.md#ordered-framebuffer-passes-and-inputs) for copied layouts, GPU input snapshots, preservation, clear ordering and verified limits.

## Sparse framebuffer outputs

Color slots retain logical shader output indices and may contain leading, intermediate or trailing AttachmentUnused holes. Fragment outputs must exactly match the active slot mask and scalar formats. Blend arrays cover every logical slot; unused entries and extra trailing entries are ignored. The device compacts native target/blend arrays and remaps a private copy of fragment SPIR-V output locations when creating a pipeline, preserving input varyings, grouped location decorations, descriptors and specialization. Original module descriptions remain unchanged. No dummy color textures or runtime source compiler are needed. Resolve slots remain logical; a resolve beside an unused color rejects. Sample count comes from the first active color or the depth target. All-unused colors work with a depth target; attachmentless passes retain their separate prerequisite.

See [native verification](../components/local-compute.md#sparse-framebuffer-outputs) for pixels, state and allocation evidence.

Ordinary signed/unsigned R32 Image descriptors now execute compute atomics, including fixed arrays. StorageAtomic validates allocation capability, while Image binding independently requires Storage. See [integer image atomics](../components/local-compute.md#integer-image-atomics) for ordering, lifetime and native verification limits.

<a id="member-shader-binary-compile"></a>
### ShaderCompileBinaryFromSPIRV

Accepts the ordinary factory's successful compute or paired vertex/fragment descriptions and optional diagnostic name of at most 4096 strict UTF-8 bytes. Returns an independent versioned Electron2D container preserving original stage bytes, including symbolic specialization. Concrete native programs are validated and released; specialization-dependent checks occur when a pipeline is built. Invalid names, modules or compile diagnostics throw ArgumentException; unsupported native capabilities throw NotSupportedException. There is no source frontend invocation or driver pipeline-cache export. Save the returned bytes with ordinary file I/O and preserve original modules for format changes.

<a id="member-shader-binary-load"></a>
### ShaderCreateFromBytecode

Accepts the complete binary span returned by ShaderCompileBinaryFromSPIRV, copies its stages and creates a normal shader program. Default placeholderRID allocates a new RID; an owned uninitialized placeholder is populated without changing its identity. Full parsing, checks and native creation precede publication; failure leaves a placeholder retryable. Invalid lengths, contents, checksum, UTF-8, reserved flags or RID ownership/kind throw ArgumentException; unsupported version/capability throws NotSupportedException. Live initialized shaders cannot be replaced. Caller mutation after return cannot change the shader. The runtime repeats module/reflection/lowering/native validation without a source compiler; specialization and borrowed set/pipeline lifetime match ShaderCreateFromSPIRV.

<a id="member-shader-binary-placeholder"></a>
### ShaderCreatePlaceholder

Reserves a real owned uninitialized RID, usable only for later bytecode initialization or FreeRID. Pipeline/set factories and interface queries reject it until loading succeeds. FreeRID or device disposal releases an unused placeholder. All three operations require the owner thread and no active command list; disposed devices reject. They do not complete queued commands or invoke readback callbacks.

For an executable storage/load and rendered consumer example, see [RenderingDeviceBinaryTests](../../tests/Electron2D.Tests/RenderingDeviceBinaryTests.cs) and [its graphics consumer](../../tests/Electron2D.Tests/RenderingDeviceBinaryTests.Graphics.cs).

Local device samplers and high-level drawable Shader image bindings share SamplerConfiguration validation. SamplerCreate still creates an independently owned RID/native resource. High-level drawable bindings use a private value cache; renderer-owned devices advance at root frame preparation, local devices after submission. Effective anisotropy is disabled at a maximum of one. See [explicit image sampler state](../components/shader-materials.md#explicit-image-sampler-state).

## Native window drawing

| Member | Contract |
| --- | --- |
| [`public const int InvalidID = -1`](#invalidid) | No screen on a local device. |
| [`public int ScreenGetWidth(int screen = 0)`](#screengetwidth) | Current native client pixel width. |
| [`public int ScreenGetHeight(int screen = 0)`](#screengetheight) | Current native client pixel height. |
| [`public int ScreenGetFramebufferFormat(int screen = 0)`](#screengetframebufferformat) | Cached device-local color-only, single-sample layout matching the actual swapchain format. |
| [`public long DrawListBeginForScreen(int screen = 0, Color? clearColor = null)`](#drawlistbeginforscreen) | Begins a cleared screen graphics list; null clears to opaque black. |

Screen IDs are native window identities: main is zero, positive values identify live native children. All four methods return InvalidID on a local device. Global queries reject missing/retired IDs and wrong-thread/disposed access. Begin also validates the finite color and the shared active-list guard. Use the returned layout with RenderPipelineCreate and finish with DrawListEnd before rendering.

Completed screen work replaces that window's next presentation once. Viewport texture content remains the independently rendered canvas. Graphics uses a private attachment with the exact native format, the existing device submission and a GPU copy into the final presentation image; no CPU frame copy or extra presentation is introduced. FramePostDraw recording reaches the next frame. ForceDraw(false) and an unavailable/minimized swapchain keep pending screen work for a later native presentation; later ordinary frames resume canvas composition. Resize samples the client pixels again, preserving compatible pipeline layouts. Child retirement releases its private attachments before the native window; active-list rejection occurs before window state changes. Session shutdown cancels interrupted lists and releases remaining screen resources.

See [the full screen contract](../components/local-compute.md#native-screen-drawing), including the existing Wayland main-window remap boundary. Native verification results are separate from hardware and platform acceptance.

### InvalidID

`public const int InvalidID = -1`

Identifies an unavailable screen/list result on a local device. It does not identify a RID resource.

### ScreenGetWidth

`public int ScreenGetWidth(int screen = 0)`

Returns the current client width in graphics pixels. The default selects the main window; a positive identity comes from a live native Window.GetWindowID(). Local devices return InvalidID. Missing/retired global IDs raise ArgumentOutOfRangeException; owner-thread and disposed-device guards apply.

### ScreenGetHeight

`public int ScreenGetHeight(int screen = 0)`

Returns the matching native client pixel height, with the same selection, ownership and error rules as ScreenGetWidth. Neither query reports monitor size or logical layout units.

### ScreenGetFramebufferFormat

`public int ScreenGetFramebufferFormat(int screen = 0)`

Returns the current native color format as a cached device-local framebuffer layout with one color attachment, one view and one sample. Repeated calls and windows with the same format reuse its identity; changing pixel dimensions alone does not invalidate a matching pipeline. Local devices return InvalidID. Queries remain valid during an active list; invalid IDs and owner/disposal violations fail as above.

### DrawListBeginForScreen

`public long DrawListBeginForScreen(int screen = 0, Color? clearColor = null)`

Begins the selected window's color list. Null selects opaque black; an explicit transparent Color remains transparent. Nonfinite colors fail before recording; competing compute/graphics lists and wrong-thread or native-submission access fail without beginning a list. A local device returns InvalidID. Bind a pipeline matching ScreenGetFramebufferFormat, use the ordinary list operations, then call DrawListEnd. Renderer submission and the next presentation own execution; direct Submit/Sync and public disposal remain local-only.

This partial snippet requires an active GPU renderer and its owner thread before rendering:

```csharp
var device = RenderingServer.GetRenderingDevice()
    ?? throw new InvalidOperationException("A GPU renderer is required.");
var list = device.DrawListBeginForScreen(clearColor: Colors.Blue);
device.DrawListEnd();
```

The clear appears once in the native main window. Later canvas output resumes unless another screen list is recorded. See the shared screen section for size changes, ForceDraw(false), window retirement and the backend visibility boundary.

## Canvas point integration

See [canvas point programs](../components/canvas-points.md) for native topology, the shared sized-point device capability and cold program validation. Authored sizes are preserved after the one-pixel default initialization; optional capabilities retain explicit rejection and device fallback.

[Device resource caches](../components/device-resource-caches.md) add retained UniformSetCacheRD and FramebufferCacheRD services over the active GPU device, with copied keys, safe dependency eviction and device-owned lifetime.

## Native render allocation memory

See [native render memory](../components/render-memory.md) for actual allocation regions, retained physical reservation, shared compute/physics leases, compatibility ownership and explicit native-adapter limits. Performance.GetMonitor retains its separate current/peak runtime-memory and other pipeline-source gaps.

`public long GetMemoryUsage(MemoryType type)` returns checked native byte counts under ordinary device owner/lifetime guards. [MemoryType](RenderingDevice.MemoryType.md) selects Textures=0, Buffers=1 or Total=2.

## LimitGet

`public long LimitGet(RenderingDevice.Limit limit)` reads a cached effective native limit under ordinary device owner/lifetime guards. Undefined values reject; unsupported cores/drivers fail explicitly. It does not submit or wait. [The canonical component](../components/device-limits.md) defines each resource/profile constraint, sparse logical set count, combined stage budgets and the executable consumer. [Limit](RenderingDevice.Limit.md) lists the 34 implemented selectors.

## SamplerIsFormatSupportedForFilter

`public bool SamplerIsFormatSupportedForFilter(DataFormat format, SamplerFilter samplerFilter)` reads the shared native format feature snapshot without image allocation or GPU submission. Undefined arguments and Max reject; mapped unsupported and unmapped formats return false. Owner/lifetime guards remain active. The current query adapter requires pinned SDL 3.4.18 Vulkan. [Native format filter support](../components/format-filter-support.md) owns capability meanings, configured binding validation, lifetime and verification limits.

[Native 64-bit integer execution](../components/local-compute.md#native-64-bit-integer-arithmetic-and-data) connects raw buffers, original push data, typed overrides and wide graphics varyings, with independent feature fallback and explicit format/compiler limits.

[Native 64-bit compute atomics](../components/local-compute.md#native-64-bit-buffer-and-workgroup-atomics) execute buffer/workgroup operations with independently negotiated features, exact copied data and explicit unsupported-storage guards.

[Native Half execution](../components/local-compute.md#native-half-arithmetic-and-packed-data) connects packed buffers, compact push data, typed values and graphics arithmetic with independently negotiated storage features.

[Native Int16/UInt16 execution](../components/local-compute.md#native-int16-and-uint16-arithmetic-and-packed-data) connects packed data, typed specialization and compute/graphics arithmetic with independently enabled storage and exact push reconstruction.

[Native byte shaders](../components/local-compute.md#native-int8-and-uint8-arithmetic-and-packed-data) execute packed data, specialization and compute/graphics arithmetic through independent features. Native8 GLSL uses the Vulkan 1.2 producer; the pinned HLSL native8 type remains a frontend dependency.

Ordinary writable vertex/fragment storage and formatted ImageBuffer resources execute with independently enabled native stage features and ordered pass-boundary dependencies. Shared descriptors, AsStorage vertex/index buffers, GPU-produced indirect commands and following compute/readback use the existing API. See [writable graphics storage](../components/local-compute.md#writable-graphics-storage-buffers) for ownership, synchronization and remaining adapters.

Formatted ImageBuffer stores and signed/unsigned R32 atomics now execute in independently enabled vertex/fragment stages, using the same encoded allocation as compute and sampled reads. See [formatted graphics producers](../components/local-compute.md#writable-formatted-graphics-producers) for format, array, synchronization, HLSL frontend and verification boundaries.

## Framebuffers without attachments

`FramebufferFormatCreateEmpty(samples = Samples1)` and `FramebufferCreateEmpty(size, samples = Samples1, validateWithFormat = -1)` execute vertex/fragment storage-producing passes without color/depth images. Extents must be positive and native-limited; an explicit format must be empty and have the same exact sample count. The pinned Vulkan adapter supports its available 1/2/4/8 counts. Missing adapters or unsupported counts throw NotSupportedException. FreeRID completes pending work before releasing the native framebuffer. See [attachmentless graphics](../components/local-compute.md#attachmentless-graphics) for shader roles, executable checks and platform limits.

## Vertex-only graphics

Vertex-only source/SPIR-V programs now execute with copied `RDPipelineRasterizationState.DiscardPrimitives`. Paired programs can also suppress raster/fragment execution. Native pipeline variants and framebuffers retain ordinary ordering and lifetime. Vertex-only binary containers use version two; compute/paired version-one writing and reading remain. See [primitive discard](../components/raster-discard.md).
