# GPUTexture

Last updated: 2026-10-16

- Declaration: `internal sealed unsafe class GPUTexture : IDisposable`
- Source: [GPUTexture.cs](../../src/Servers/Rendering/GPUTexture.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

[Image-array resources](../components/texture-arrays.md) execute copied homogeneous layers, typed samplers/defaults, shape-aware reload, source archives, actual GPU layer/mip upload and owned/proxy RIDs. Ordinary texture drawing retains its separate resource branch. Current native evidence is Linux Wayland GPU; compatibility rejects shader use. BC/ETC/EAC/ASTC sampling and UInt16 integer sources execute; foreign/native-allocation acceptance remains separate.

## Description

Owns a sampled native 2D texture or image-array texture and upload transfer buffer under a retained GPU device. It holds managed TexturePixels snapshots but never owns the public Texture resource. GPUCanvasBackend replaces this allocation when the snapshot allocation identity changes; a compatible ImageTexture.Update or RenderingServer.Texture2DUpdate replaces Pixels instead. TextureReplace transfers a different allocation token when needed and triggers ordinary backend replacement.

BC1–BC7 sources retain their original format, complete mip chain and exact block bytes. Prepared CPU sampling decodes BC1–BC5/BC7 to RGBA8 and signed/unsigned BC6 to RGB floating point. GPU allocates native BC storage when the device supports it; otherwise it uploads prepared decoded pixels. Compatibility uses the decoded snapshot; BC6 requires an HDR-capable driver and the current Linux driver rejects that role. The RA-as-RG role uses decoded upload to preserve channel semantics. UInt16 sources use raw unsigned RGBA16 upload; ETC/EAC/ASTC use prepared decoded upload.

NativeTextureFormat reports the actual allocation to RenderingDevice exports. Compressed transfer offsets use each format’s block sizes across layers/mips. Update retains allocation identity and upload version commits after submission.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
gpuTexture.Upload(commandBuffer);
// After successful submission:
gpuTexture.CommitUpload();
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal GPUTexture(RenderHandle device, TexturePixels pixels)` | [Construction](#construction) |
| `internal GPUTexture(RenderHandle device, LayeredTexturePixels pixels)` | Creates real array storage and bounded transfer capacity. |
| `internal LayeredTexturePixels? ArrayPixels` | Current immutable array snapshot; upload version commits after submission. |
| `internal TexturePixels Pixels` | [Pending pixels](#pending-pixels) |
| `internal RenderHandle Allocation { get; }` | Retained SafeHandle leased by device exports. |
| `internal bool IsDisposed { get; }` | True after allocation disposal. |
| `internal RenderingDevice.TextureUsageBits DeviceUsage { get; }` | Actual native sampling/update/copy and supported storage roles. |
| `internal bool NeedsUpload { get; }` | Current authored version differs from the submitted version. |
| `internal bool NeedsRecording { get; }` | Current authored version is neither submitted nor already recorded. |
| `internal void RecordUpload()` | Retains the encoded authored version until submission. |
| `internal void CancelUpload()` | Clears the cancelled recorded version. |
| `internal nint Handle { get; }` | [Native handle](#native-handle) |
| `internal void Upload(nint command)` | [Upload](#upload) |
| `internal void CommitUpload()` | [Commit upload](#commit-upload) |
| `public void Dispose()` | [Disposal](#disposal) |

## Member descriptions

### Construction

`internal GPUTexture(RenderHandle device, TexturePixels pixels)`

Checks native sampling format support, creates an RGBA8 or RGBA32Float texture with every stored mip level, and allocates matching upload storage. Transfer creation failure releases the texture. Unsupported sampling raises NotSupportedException.

### Pending pixels

`internal TexturePixels Pixels`

Current immutable snapshot to upload. Replacing it requires an unchanged allocation identity and pixel layout, enforced by the owning backend.

### Native handle

`internal nint Handle { get; }`

Borrows the native texture pointer internally. Invalid after disposal; callers retain the backend/device lifetime.

### Upload

`internal void Upload(nint command)`

Skips the already committed snapshot. Otherwise maps/copies upload bytes, encodes all mip levels, cycles the native texture only at the first level, and closes the copy pass. Does not mark success until the containing command has been submitted. Native mapping/pass failures throw.

### Commit upload

`internal void CommitUpload()`

Marks the recorded snapshot (or current backend upload snapshot) uploaded after successful command submission. Never call for cancelled/failed frames.

### Disposal

`public void Dispose()`

Disposes texture and transfer SafeHandles; device retention is released with them. Repeated disposal is safe.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.

RID proxy batches resolve to their root Texture before cache preparation; each root gets one allocation regardless of alias count. Prepared server-owned source allocations remain cached until free/shutdown. Retargeting among already prepared roots retains their native handles. [Proxy tests](../../tests/Electron2D.Tests/RenderingTextureProxyTests.cs) check sharing and stable handles on both backends.

## Shared renderer-device ownership

Ordinary/array allocation leases retain the texture after backend retirement until pending device commands complete. Storage usage is added only when the complete native graphics/compute read/write flag combination is supported. Raw update/copy use native transfer roles. RecordUpload/CancelUpload distinguish encoded work from successful submission; CommitUpload commits that exact version. Device writes do not mutate authored snapshots and are preserved until a later authored upload. See [renderer export checks](../components/local-compute.md#renderer-texture-export-checkpoint).

Prepared mutable pixels compare identity and revision. Upload records the revision actually copied; CommitUpload publishes that recorded value after submission, so a later in-place update remains pending. The same allocation and transfer capacity are reused for video frames.

[UInt16 image texture sources](../components/integer-textures.md) now retain all four original layouts and mip chains, use raw unsigned RGBA16 GPU storage, validate ordinary/layered sampler scalars and reject unsupported integer drawing on Compatibility.
